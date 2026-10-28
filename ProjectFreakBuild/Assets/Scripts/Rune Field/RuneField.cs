using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//the rules of a rune field: placing runes, bridges, power, ability nodes, and compiling what the field does to its shade
//this is a plain C# class, not a MonoBehaviour. It doesn't live on a GameObject, it's created with "new RuneField(...)" by whatever shows the field
//the view (2D or 3D) only draws what this class says and sends player actions to it, so the rules work the same no matter how the field looks
public class RuneField
{
    public const int CoreID = -1; //the core is a bridge end like any rune, it just always has this ID
    public const int NoRuneID = -2; //means "no rune", like a rune that hasn't been placed yet
    const int ClampPasses = 10; //how many times the backup clamp re-checks every bridge when the bridges can't all be reached at once (see ClampByPasses)
    const float LeashTolerance = 0.01f; //a frozen rune clamped right onto its leash can land a hair past it from rounding, this lets that through
    const int MinSearchSpots = 8; //the fewest spots FindDropSpot tries on one ring, so the first small rings still check every direction

    //local variables
    RuneFieldData _data; //the field being edited. Usually a copy of the saved data (see RuneFieldData.Clone)
    RuneFieldSettingsSO _settings; //core reach, field radius, snap radius etc, shared by every shade slot
    Dictionary<int, AbilityNodeEntry> _nodes = new Dictionary<int, AbilityNodeEntry>(); //node index -> node. A Dictionary finds an entry by its key, so gaps in the indexes are fine
    List<int> _nodeOrder = new List<int>(); //every node index, smallest first, so nodes are always checked in the same order
    int _maxPower; //how much power the core has (shade level, plus a core fragment later)
    int _rank; //how many times the shade has evolved (Bound = 0). Zones up to this are closed, zone rank + 1 is the open zone
    int _powerUsed; //worked out by CalculatePower
    HashSet<int> _poweredRunes = new HashSet<int>(); //IDs of runes that got their power. A HashSet is a list that's fast at "is this in here?" and can't hold duplicates

    public event System.Action OnFieldChanged; //fires after every recalculation so the view can redraw
    public event System.Action<List<RuneBridgeEntry>> OnBridgesCut; //fires when saving cuts bridges across the open ring, with copies of the bridges that were cut (for the break effect later)

    public RuneField(RuneFieldData data, RuneFieldSettingsSO settings, List<AbilityNodeEntry> nodes, int maxPower, int rank = 0)
    {
        //sets up a field from saved data. Bad entries in the data get cleaned out, then everything is worked out once
        //nodes can be the full list from the field scene, or just the snapshots a shade slot saved (only the nodes it plugged into)
        //rank is the shade's rank (ShadeSO.GetRank). Leaving it out means Bound
        _data = data;
        _settings = settings;
        _maxPower = Mathf.Max(0, maxPower);
        _rank = Mathf.Max(0, rank);

        if (_data == null) { Debug.LogWarning("Warning! Rune field was given no data, starting with an empty field..."); _data = new RuneFieldData(); }
        if (_settings == null)
        {
            Debug.LogWarning("Warning! Rune field was given no settings asset, using the default settings...");
            _settings = ScriptableObject.CreateInstance<RuneFieldSettingsSO>(); //a temporary asset with the default values, not saved anywhere
        }

        SetUpNodes(nodes);
        CleanUpData();

        //fields saved before the frozen lists existed have nothing in them. An evolved shade always has at least its gate rune frozen,
        //so empty lists past Bound means old data: they're built once from positions (the old zone rule), and saved with the field next time
        if (_rank > 0 && HasNoFrozenLists()) Freeze();
        Recalculate();
    }

    #region Getters
    public RuneFieldData GetData()
    {
        return _data;
    }

    public RuneFieldSettingsSO GetSettings()
    {
        return _settings;
    }

    public int GetMaxPower()
    {
        return _maxPower;
    }

    public int GetPowerUsed()
    {
        return _powerUsed;
    }

    public int GetPowerLeft()
    {
        return _maxPower - _powerUsed;
    }

    public PlacedRuneEntry GetRune(int runeID)
    {
        //returns the rune with this ID, or null if there isn't one
        for (int i = 0; i < _data._Runes.Count; i++)
        {
            if (_data._Runes[i]._ID == runeID) return _data._Runes[i];
        }
        return null;
    }

    public bool IsRunePowered(int runeID)
    {
        return _poweredRunes.Contains(runeID);
    }

    public bool HasRunesWaitingForPower()
    {
        //true if any rune that could have power doesn't (the core can't afford it, or it isn't bridged to the core). The core's look uses this
        //planning runes past the open zone don't count, they never get power
        for (int i = 0; i < _data._Runes.Count; i++)
        {
            int runeID = _data._Runes[i]._ID;
            if (IsRunePowered(runeID)) continue;
            if (IsRuneFrozen(runeID) == false && IsPastOpenZone(runeID)) continue; //a planning rune
            return true;
        }
        return false;
    }

    public Vector2 GetPosition(int id)
    {
        //returns where a rune (or the core) sits on the field
        if (id == CoreID) return Vector2.zero;

        PlacedRuneEntry rune = GetRune(id);
        if (rune == null) return Vector2.zero;
        return rune._Position;
    }

    public List<int> GetConnections(int id)
    {
        //returns the IDs of everything bridged to this rune (or the core)
        return GetConnectionsIn(_data._Bridges, id);
    }

    static List<int> GetConnectionsIn(List<RuneBridgeEntry> bridges, int id)
    {
        //returns the IDs of everything a bridge list joins to this rune (or the core). Shared by the bridges and the frozen bridges
        List<int> connections = new List<int>();
        for (int i = 0; i < bridges.Count; i++)
        {
            if (bridges[i]._A == id) connections.Add(bridges[i]._B);
            else if (bridges[i]._B == id) connections.Add(bridges[i]._A);
        }
        return connections;
    }

    public Dictionary<int, int> GetCoreSteps()
    {
        //function that works out how many bridges each rune is from the core: the core is 0, a rune bridged to it is 1, and so on
        //walks out one layer at a time like the power pass, but along every bridge and with no power rules, so it's only for looks
        //(the 3D view uses it to point each bridge's A end at the core). Runes with no bridge path to the core aren't in the result
        Dictionary<int, int> steps = new Dictionary<int, int>(); //rune ID -> bridges from the core
        steps.Add(CoreID, 0);
        List<int> currentLayer = new List<int>();
        currentLayer.Add(CoreID);
        int step = 0;

        while (currentLayer.Count > 0)
        {
            step++;
            List<int> nextLayer = new List<int>();
            for (int i = 0; i < currentLayer.Count; i++)
            {
                List<int> connections = GetConnections(currentLayer[i]);
                for (int j = 0; j < connections.Count; j++)
                {
                    if (steps.ContainsKey(connections[j])) continue; //already reached by a path that's as short or shorter
                    steps.Add(connections[j], step);
                    nextLayer.Add(connections[j]);
                }
            }
            currentLayer = nextLayer;
        }
        return steps;
    }

    public bool AreConnected(int idA, int idB)
    {
        return FindBridgeIndex(idA, idB) != -1;
    }

    public int GetPluggedNode(int runeID)
    {
        //returns the index of the ability node this rune is plugged into, or -1
        for (int i = 0; i < _data._Plugs.Count; i++)
        {
            if (_data._Plugs[i]._RuneID == runeID) return _data._Plugs[i]._NodeIndex;
        }
        return -1;
    }

    public int GetRuneInNode(int nodeIndex)
    {
        //returns the ID of the rune plugged into this ability node, or -1
        for (int i = 0; i < _data._Plugs.Count; i++)
        {
            if (_data._Plugs[i]._NodeIndex == nodeIndex) return _data._Plugs[i]._RuneID;
        }
        return -1;
    }

    public bool IsNodeActive(int nodeIndex)
    {
        return _data._ActiveNodes.Contains(nodeIndex);
    }

    public AbilityNodeEntry GetNode(int nodeIndex)
    {
        //returns the node with this index, or null if this field doesn't know it
        if (_nodes.TryGetValue(nodeIndex, out AbilityNodeEntry node)) return node; //TryGetValue looks the key up and gives the value back through "out" if it's there
        return null;
    }

    public bool HasNode(int nodeIndex)
    {
        return _nodes.ContainsKey(nodeIndex);
    }

    public float GetReach(int id)
    {
        //how far this rune (or the core) reaches to make a bridge, in field units
        if (id == CoreID) return _settings._CoreReach;

        PlacedRuneEntry rune = GetRune(id);
        if (rune == null) return 0f;
        return rune._Element.connectionDistance;
    }

    public float GetBaseReach(int idA, int idB)
    {
        //how long a NEW bridge between these two can be: whichever of the two reaches is bigger
        return Mathf.Max(GetReach(idA), GetReach(idB));
    }

    public float GetBridgeReach(int idA, int idB)
    {
        //how long an EXISTING bridge can be before it counts as stretched
        //snapping into a node is allowed to stretch bridges a little (settings Snap Stretch), so a rune sitting in a node gets that extra length
        return GetBridgeReach(idA, idB, NoRuneID);
    }

    float GetBridgeReach(int idA, int idB, int movingRuneID)
    {
        //same as above, but the rune being dragged counts as out of its node, so dragging it away uses normal reach
        bool plugged = (idA != movingRuneID && GetPluggedNode(idA) != -1) || (idB != movingRuneID && GetPluggedNode(idB) != -1);
        float reach = GetBaseReach(idA, idB);
        if (plugged) reach += _settings._SnapStretch;
        return reach;
    }

    public int GetMaxBridges(int id)
    {
        //how many bridges this rune (or the core) can have
        if (id == CoreID)
        {
            if (_settings._CoreMaxBridges <= 0) return int.MaxValue; //0 means no limit
            return _settings._CoreMaxBridges;
        }

        PlacedRuneEntry rune = GetRune(id);
        if (rune == null) return 0;
        return rune._Element.connectionsAllowed;
    }

    public int GetPowerNeeded(int runeID)
    {
        PlacedRuneEntry rune = GetRune(runeID);
        if (rune == null) return 0;
        return Mathf.Max(0, rune._Element.powerNeeded);
    }

    public bool HasFreeBridgeSlot(int id)
    {
        return GetConnections(id).Count < GetMaxBridges(id);
    }

    public int GetRank()
    {
        return _rank;
    }
    #endregion

    #region Zones
    public int GetOpenZone()
    {
        //the zone the shade evolves out of next: the one past the last zone it evolved out of (a Bound shade uses zone 1)
        //runes can go in any zone, but only gates in this zone can evolve, and nothing past it gets power unless it's frozen
        return Mathf.Clamp(_rank + 1, 1, _settings._ZoneCount);
    }

    public int GetZone(Vector2 position)
    {
        //which zone a spot is in. See RuneFieldSettingsSO.GetZone
        return _settings.GetZone(position.magnitude);
    }

    public bool IsInsideField(Vector2 position)
    {
        //checks a spot is inside the outer ring (with the small ring tolerance, so a spot on the edge still counts)
        return position.magnitude <= _settings.GetFieldEdge() + RuneFieldSettingsSO.RingTolerance;
    }

    public bool IsInOpenZone(Vector2 position)
    {
        //checks a spot is inside the field and in the open zone (the zone the shade can evolve out of next). Zones further out are locked, zones further in are closed
        return IsInsideField(position) && GetZone(position) == GetOpenZone();
    }

    public bool IsPastOpenZone(Vector2 position)
    {
        //checks a spot is past the open zone's outer ring (a locked zone). Runes there are planning runes: they can be placed, but never get power
        return GetZone(position) > GetOpenZone();
    }

    public bool IsPastOpenZone(int id)
    {
        //same as above for a rune where it sits. The core never is
        if (id == CoreID) return false;
        PlacedRuneEntry rune = GetRune(id);
        if (rune == null) return false;
        return IsPastOpenZone(rune._Position);
    }

    public bool CrossesOpenRing(int idA, int idB)
    {
        //checks a bridge between these two crosses the open zone's outer ring: one end past it and the other end inside it
        //a bridge with both ends past the ring doesn't count, so a planned chain out there keeps its shape
        return IsPastOpenZone(idA) != IsPastOpenZone(idB);
    }
    #endregion

    #region Frozen
    //frozen is saved on the field (RuneFieldData), not worked out from where a rune sits, so a frozen rune can be moved without thawing
    //the rules that keep an evolution gate from ever losing power:
    //A. a locked-in rune (a frozen rune plugged into a node) never moves or unplugs
    //B. a frozen bridge is never cut, so it's a hard leash on the frozen rune being dragged
    //C. frozen runes get power first, along frozen bridges only, so their place in the power order never changes

    public bool IsRuneFrozen(int id)
    {
        //a frozen rune can be moved, but never removed, and gets its power first
        //the core counts as frozen, so a bridge from the core can be a frozen bridge
        if (id == CoreID) return true;
        return _data._FrozenRunes.Contains(id);
    }

    public bool IsBridgeFrozen(int idA, int idB)
    {
        //a frozen bridge never tears or gets cut. A bridge made later between two frozen runes stays normal until the next evolve
        return FindFrozenBridgeIndex(idA, idB) != -1;
    }

    public bool IsRuneLockedIn(int runeID)
    {
        //a locked-in rune is a frozen rune plugged into a node for good: it never moves or unplugs
        return _data._LockedRunes.Contains(runeID);
    }

    public void FreezeClosedZones()
    {
        //function the Shade Manager runs right after the shade evolves, so the zone it left (and everything further in) freezes
        Freeze();
        Recalculate();
    }

    void Freeze()
    {
        //does the freezing without recalculating:
        //1. every rune in a zone up to the rank (the closed zones) is frozen. Runes frozen before stay frozen, wherever they've been moved to
        //2. every bridge with both ends frozen right now becomes a frozen bridge (the core counts as frozen)
        //3. every frozen rune sitting in a node is locked in
        //it only ever adds, so running it twice changes nothing
        for (int i = 0; i < _data._Runes.Count; i++)
        {
            PlacedRuneEntry rune = _data._Runes[i];
            if (GetZone(rune._Position) > _rank || IsRuneFrozen(rune._ID)) continue;
            _data._FrozenRunes.Add(rune._ID);
        }

        for (int i = 0; i < _data._Bridges.Count; i++)
        {
            RuneBridgeEntry bridge = _data._Bridges[i];
            if (IsRuneFrozen(bridge._A) == false || IsRuneFrozen(bridge._B) == false) continue;
            if (IsBridgeFrozen(bridge._A, bridge._B)) continue; //already frozen

            RuneBridgeEntry frozenBridge = new RuneBridgeEntry(); //its own copy, so the two lists never share an entry
            frozenBridge._A = bridge._A;
            frozenBridge._B = bridge._B;
            _data._FrozenBridges.Add(frozenBridge);
        }

        AddLockedRunes();
    }

    public List<int> GetFrozenRunesToLockIn()
    {
        //returns the frozen runes plugged into a node in this draft that aren't locked in yet
        //saving locks them in for good, so the view can list them and ask first
        List<int> runes = new List<int>();
        for (int i = 0; i < _data._Plugs.Count; i++)
        {
            int runeID = _data._Plugs[i]._RuneID;
            if (IsRuneFrozen(runeID) && IsRuneLockedIn(runeID) == false) runes.Add(runeID);
        }
        return runes;
    }

    public void LockInFrozenPlugs()
    {
        //function the Shade Manager runs when the field is saved: every frozen rune sitting in a node is locked in for good
        //GetFrozenRunesToLockIn gives the same list beforehand, for asking the player
        if (AddLockedRunes()) Recalculate(); //nothing about power changes, this just lets the view redraw the locked look
    }

    bool AddLockedRunes()
    {
        //adds every frozen rune plugged into a node to the locked list. Returns true if any were added. Doesn't recalculate, the caller does that
        List<int> runes = GetFrozenRunesToLockIn();
        _data._LockedRunes.AddRange(runes);
        return runes.Count > 0;
    }

    bool HasNoFrozenLists()
    {
        //true if nothing on this field is frozen, locked in or a frozen bridge (a Bound shade, or data saved before the frozen lists existed)
        return _data._FrozenRunes.Count == 0 && _data._FrozenBridges.Count == 0 && _data._LockedRunes.Count == 0;
    }

    List<int> GetFrozenConnections(int id)
    {
        //returns the IDs of everything joined to this rune (or the core) by a frozen bridge
        return GetConnectionsIn(_data._FrozenBridges, id);
    }

    int FindFrozenBridgeIndex(int idA, int idB)
    {
        //returns where the frozen bridge between these two is in the frozen bridge list, or -1. Works in either order
        return FindBridgeIndexIn(_data._FrozenBridges, idA, idB);
    }

    bool IsWithinFrozenLeash(int runeID, Vector2 position)
    {
        //checks every frozen bridge on this rune still reaches this spot. Frozen bridges never tear, so they're a hard leash
        //the rune counts as out of its node (normal reach), the same as ClampToBridges, which keeps the dragged view inside the leash
        //landing in a node is checked by CanSnapToNode instead, which allows the usual Snap Stretch
        List<int> connections = GetFrozenConnections(runeID);
        for (int i = 0; i < connections.Count; i++)
        {
            float distance = Vector2.Distance(position, GetPosition(connections[i]));
            if (distance > GetBridgeReach(runeID, connections[i], runeID) + LeashTolerance) return false;
        }
        return true;
    }
    #endregion

    #region Rune Editing
    public bool TryPlaceRune(ElementItemSO element, Vector2 position, out int runeID)
    {
        //function that puts a new rune on the field. It doesn't make any bridges, call ConnectNearby after
        //returns false (and runeID = NoRuneID) if the rune can't go there
        //a spot on an empty node snaps the rune to its center and plugs it in. Any other spot has to be clear of every footprint
        runeID = NoRuneID;
        if (element == null) { Debug.LogWarning("Warning! Tried to place a rune with no element, nothing placed..."); return false; }
        if (CanDropAt(NoRuneID, position, out Vector2 finalPosition, out int nodeIndex) == false) return false;

        PlacedRuneEntry rune = new PlacedRuneEntry();
        rune._ID = _data._NextRuneID;
        rune._Element = element;
        rune._Position = finalPosition;

        _data._NextRuneID++;
        _data._Runes.Add(rune);
        runeID = rune._ID;

        if (nodeIndex != -1) AddPlug(runeID, nodeIndex);
        Recalculate();
        return true;
    }

    public bool TryDropRune(int runeID, Vector2 position)
    {
        //function for a dragged rune being let go. Moves it (snapping into a node if it's on one) if the spot is allowed
        //returns false and changes nothing if it isn't, so the rune stays at its last spot. Call ConnectNearby after a successful drop
        PlacedRuneEntry rune = GetRune(runeID);
        if (rune == null) return false;
        if (IsRuneLockedIn(runeID)) return false; //rule A: a locked-in rune never moves. Other frozen runes can
        if (CanDropAt(runeID, position, out Vector2 finalPosition, out int nodeIndex) == false) return false;

        int oldNode = GetPluggedNode(runeID);
        if (oldNode != -1 && oldNode != nodeIndex) RemovePlug(runeID); //it left its node
        rune._Position = finalPosition;
        if (nodeIndex != -1 && oldNode != nodeIndex) AddPlug(runeID, nodeIndex);

        Recalculate();
        return true;
    }

    public bool CanDropAt(int runeID, Vector2 position, out Vector2 finalPosition, out int nodeIndex)
    {
        //checks if a rune could go here. runeID is the rune being moved (NoRuneID for a new one), so it doesn't block itself
        //1. an empty node in snap range: allowed if the node can take this rune (see CanPlugInto), and it lands on the node's center
        //2. anywhere else: allowed if it's inside the field (any zone), it doesn't overlap a rune, the core or a node (see IsSpotFree),
        //   and a frozen rune's frozen bridges still reach it (rule B, the hard leash)
        //a locked-in rune can't go anywhere but where it is (rule A)
        //the view uses this while dragging to show where the rune would land, or that it can't go there
        finalPosition = position;
        nodeIndex = -1;
        if (IsRuneLockedIn(runeID)) return false;

        nodeIndex = FindNodeAt(position, runeID);

        if (nodeIndex != -1)
        {
            if (CanPlugInto(runeID, nodeIndex))
            {
                finalPosition = _nodes[nodeIndex]._Position;
                return true;
            }
            nodeIndex = -1;
            return false; //right on top of a node that can't take it (locked, locked out, or too far to snap): refused
        }

        if (IsWithinFrozenLeash(runeID, position) == false) return false;
        return IsSpotFree(runeID, position);
    }

    public bool IsSpotFree(int runeID, Vector2 position)
    {
        //checks a rune's footprint here doesn't overlap another rune, the core or a node, and is inside the field
        //any zone is fine: closed zones take normal runes again, and locked zones take planning runes (see IsPastOpenZone)
        //footprints are circles: two runes overlap if their centers are closer than one rune size
        if (IsInsideField(position) == false) return false;

        float runeSize = _settings._RuneSize;
        if (position.magnitude < (_settings._CoreSize + runeSize) * 0.5f) return false; //the core sits at (0, 0). Half of each size = the gap two circles need

        for (int i = 0; i < _data._Runes.Count; i++)
        {
            PlacedRuneEntry other = _data._Runes[i];
            if (other._ID == runeID) continue; //a rune can't block itself
            if (Vector2.Distance(position, other._Position) < runeSize) return false;
        }

        for (int i = 0; i < _nodeOrder.Count; i++)
        {
            if (Vector2.Distance(position, _nodes[_nodeOrder[i]]._Position) < runeSize) return false; //nodes hold exactly one rune, so they're rune sized
        }
        return true;
    }

    public bool RemoveRune(int runeID)
    {
        //function that takes a rune off the field along with its bridges and plug. Returns false if it can't (missing or frozen)
        PlacedRuneEntry rune = GetRune(runeID);
        if (rune == null) return false;
        if (IsRuneFrozen(runeID)) return false; //frozen runes stay on the field for good (they can still be moved). This also keeps their frozen bridges safe

        //go backwards so removing an entry doesn't skip the next one
        for (int i = _data._Bridges.Count - 1; i >= 0; i--)
        {
            if (_data._Bridges[i]._A == runeID || _data._Bridges[i]._B == runeID) _data._Bridges.RemoveAt(i);
        }

        for (int i = _data._Plugs.Count - 1; i >= 0; i--)
        {
            if (_data._Plugs[i]._RuneID == runeID) _data._Plugs.RemoveAt(i);
        }

        _data._Runes.Remove(rune);
        Recalculate();
        return true;
    }
    #endregion

    #region Bridges
    public bool CanBridge(int idA, int idB)
    {
        //checks every rule for a bridge between two things where they sit right now
        return CanBridgeFrom(idA, GetPosition(idA), idB);
    }

    public bool CanBridgeFrom(int idA, Vector2 positionA, int idB)
    {
        //checks every rule for a bridge if A were at positionA. Used while dragging, before the rune has actually moved
        if (idA == idB) return false;
        if (idA != CoreID && GetRune(idA) == null) return false;
        if (idB != CoreID && GetRune(idB) == null) return false;
        if (AreConnected(idA, idB)) return false;
        if (HasFreeBridgeSlot(idA) == false || HasFreeBridgeSlot(idB) == false) return false;

        float distance = Vector2.Distance(positionA, GetPosition(idB));
        return distance <= GetBaseReach(idA, idB); //new bridges always use normal reach
    }

    public List<int> FindBridgeTargets(int runeID, Vector2 position)
    {
        //function that returns what this rune would bridge to if dropped at this spot
        //closest first, and only as many as the rune has free bridge slots
        List<int> candidates = new List<int>();

        if (CanBridgeFrom(runeID, position, CoreID)) candidates.Add(CoreID);
        for (int i = 0; i < _data._Runes.Count; i++)
        {
            int otherID = _data._Runes[i]._ID;
            if (CanBridgeFrom(runeID, position, otherID)) candidates.Add(otherID);
        }

        //sort closest first. The "(a, b) => ..." part is a small inline function that tells Sort how to compare two entries
        candidates.Sort((a, b) => Vector2.Distance(position, GetPosition(a)).CompareTo(Vector2.Distance(position, GetPosition(b))));

        int freeSlots = GetMaxBridges(runeID) - GetConnections(runeID).Count;
        if (freeSlots < 0) freeSlots = 0;
        if (candidates.Count > freeSlots) candidates.RemoveRange(freeSlots, candidates.Count - freeSlots); //keep only the closest ones that fit

        return candidates;
    }

    public int ConnectNearby(int runeID)
    {
        //function that bridges a rune to everything it can reach where it sits (what happens when the player drops it)
        //returns how many bridges were made
        List<int> targets = FindBridgeTargets(runeID, GetPosition(runeID));
        for (int i = 0; i < targets.Count; i++)
        {
            AddBridge(runeID, targets[i]);
        }

        if (targets.Count > 0) Recalculate();
        return targets.Count;
    }

    public bool Connect(int idA, int idB)
    {
        //function that makes one bridge if the rules allow it
        if (CanBridge(idA, idB) == false) return false;

        AddBridge(idA, idB);
        Recalculate();
        return true;
    }

    public void Disconnect(int idA, int idB)
    {
        //function that removes a bridge (what happens when one tears). Frozen bridges never come off
        int index = FindBridgeIndex(idA, idB);
        if (index == -1) return;
        if (IsBridgeFrozen(idA, idB)) return;

        _data._Bridges.RemoveAt(index);
        Recalculate();
    }

    public void RestoreBridges(int runeID, List<int> otherIDs)
    {
        //function that puts back bridges a rune had before (like ones torn during a drag that ended on a bad spot)
        //there's no reach check, since they were already bridged from this exact spot. Missing ends and full slots are still skipped
        if (GetRune(runeID) == null || otherIDs == null) return;

        bool changed = false;
        for (int i = 0; i < otherIDs.Count; i++)
        {
            int otherID = otherIDs[i];
            if (otherID == runeID || AreConnected(runeID, otherID)) continue;
            if (otherID != CoreID && GetRune(otherID) == null) continue; //the other end is gone
            if (HasFreeBridgeSlot(runeID) == false || HasFreeBridgeSlot(otherID) == false) continue;

            AddBridge(runeID, otherID);
            changed = true;
        }

        if (changed) Recalculate();
    }

    public List<RuneBridgeEntry> GetBridgesToCut()
    {
        //returns copies of the bridges saving would cut: every bridge crossing the open zone's outer ring that isn't frozen
        //bridges with both ends past the ring stay, so a planned chain keeps its shape until its zone opens
        List<RuneBridgeEntry> toCut = new List<RuneBridgeEntry>();
        for (int i = 0; i < _data._Bridges.Count; i++)
        {
            RuneBridgeEntry bridge = _data._Bridges[i];
            if (CrossesOpenRing(bridge._A, bridge._B) == false) continue;
            if (IsBridgeFrozen(bridge._A, bridge._B)) continue; //rule B: frozen bridges are never cut

            RuneBridgeEntry copy = new RuneBridgeEntry(); //a copy, so whoever gets the list can't change the field through it
            copy._A = bridge._A;
            copy._B = bridge._B;
            toCut.Add(copy);
        }
        return toCut;
    }

    public List<RuneBridgeEntry> CutOpenRingBridges()
    {
        //function the Shade Manager runs when the field is saved: cuts every bridge GetBridgesToCut lists and fires OnBridgesCut with them
        //returns the cut bridges (an empty list if there were none). Power doesn't change, since these bridges never carried power across the ring
        List<RuneBridgeEntry> cut = GetBridgesToCut();
        if (cut.Count == 0) return cut;

        for (int i = 0; i < cut.Count; i++)
        {
            int index = FindBridgeIndex(cut[i]._A, cut[i]._B);
            if (index != -1) _data._Bridges.RemoveAt(index);
        }

        Recalculate();
        OnBridgesCut?.Invoke(cut);
        return cut;
    }

    void AddBridge(int idA, int idB)
    {
        RuneBridgeEntry bridge = new RuneBridgeEntry();
        bridge._A = idA;
        bridge._B = idB;
        _data._Bridges.Add(bridge);
    }

    int FindBridgeIndex(int idA, int idB)
    {
        //returns where the bridge between these two is in the bridge list, or -1. Works in either order
        return FindBridgeIndexIn(_data._Bridges, idA, idB);
    }

    static int FindBridgeIndexIn(List<RuneBridgeEntry> bridges, int idA, int idB)
    {
        //returns where the bridge between these two is in a bridge list, or -1. Works in either order. Shared by the bridges and the frozen bridges
        for (int i = 0; i < bridges.Count; i++)
        {
            RuneBridgeEntry bridge = bridges[i];
            if ((bridge._A == idA && bridge._B == idB) || (bridge._A == idB && bridge._B == idA)) return i;
        }
        return -1;
    }
    #endregion

    #region Dragging Limits
    public Vector2 ClampToBridges(int runeID, Vector2 desiredPosition)
    {
        //function that returns the closest spot to where the player is dragging that keeps every bridge within reach, and stays inside the field's edge bleed
        //each bridge is a circle around its other end, and the rune has to stay inside all of them (the field's edge is one more circle, around the core)
        //the closest spot is always one of these:
        //1. where the player is pointing, if it's already inside every circle
        //2. straight back onto one circle's edge, toward the pointer
        //3. a corner where two circles cross (a rune pulled to max length by two bridges at once ends up here)
        List<ReachCircle> circles = GetReachCircles(runeID);
        if (IsInsideCircles(circles, desiredPosition)) return desiredPosition;

        List<Vector2> candidates = new List<Vector2>();

        for (int i = 0; i < circles.Count; i++)
        {
            Vector2 offset = desiredPosition - circles[i]._Center;
            if (offset.sqrMagnitude < 0.0001f) continue; //right on the center there's no direction to push it, the other options cover it
            candidates.Add(circles[i]._Center + offset.normalized * circles[i]._Radius);
        }

        for (int i = 0; i < circles.Count; i++)
        {
            for (int j = i + 1; j < circles.Count; j++)
            {
                if (GetCircleCrossings(circles[i], circles[j], out Vector2 first, out Vector2 second) == false) continue;
                candidates.Add(first);
                candidates.Add(second);
            }
        }

        //of the spots that fit inside every circle, the one closest to the pointer wins
        bool found = false;
        Vector2 best = desiredPosition;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (IsInsideCircles(circles, candidates[i]) == false) continue;
            float distance = Vector2.Distance(desiredPosition, candidates[i]);
            if (distance >= bestDistance) continue;
            best = candidates[i];
            bestDistance = distance;
            found = true;
        }

        if (found) return best;
        return ClampByPasses(circles, desiredPosition); //no spot fits every bridge (only an old, overstretched save could do this), so it gets as close as it can
    }

    public bool FindDropSpot(int runeID, Vector2 position, out Vector2 finalPosition, out int nodeIndex)
    {
        //function for letting go of a dragged rune: finds the closest spot to "position" it can actually land on
        //the spot itself is tried first (snapping into a node if it's on one, see CanDropAt). If that's refused (overlapping a rune or the core,
        //a node that can't take it, past the field's edge), it tries rings of spots around it, one Drop Search Step further out each time,
        //up to the settings' Drop Search Distance. Every spot it tries keeps all of the rune's bridges within reach, so landing never stretches one
        //returns false if nothing fits (then the rune goes back to where it was). TryDropRune with finalPosition then moves it
        if (CanDropAt(runeID, position, out finalPosition, out nodeIndex)) return true;
        finalPosition = position;
        nodeIndex = -1;
        if (IsRuneLockedIn(runeID)) return false; //rule A: it isn't going anywhere, so there's nothing to search for

        float step = _settings._DropSearchStep;
        float maxDistance = _settings._DropSearchDistance;
        if (step <= 0f || maxDistance <= 0f) return false; //searching is turned off

        List<ReachCircle> circles = GetReachCircles(runeID);
        for (float distance = step; distance <= maxDistance + 0.001f; distance += step) //the 0.001 makes sure the last ring isn't skipped by float rounding
        {
            //bigger rings get more spots, so the spots stay about one step apart all the way around
            int spotCount = Mathf.Max(MinSearchSpots, Mathf.CeilToInt(2f * Mathf.PI * distance / step));
            bool foundOnRing = false;
            float bestOffset = float.MaxValue;

            for (int i = 0; i < spotCount; i++)
            {
                float angle = i * 2f * Mathf.PI / spotCount; //in radians, which is what Mathf.Cos and Mathf.Sin use
                Vector2 spot = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                if (IsInsideCircles(circles, spot) == false) continue; //would stretch a bridge
                if (CanDropAt(runeID, spot, out Vector2 landing, out int landingNode) == false) continue;

                //a spot in a node's snap range lands on the node's center, so it's compared by where it would really end up
                float offset = Vector2.Distance(position, landing);
                if (offset >= bestOffset) continue;
                bestOffset = offset;
                finalPosition = landing;
                nodeIndex = landingNode;
                foundOnRing = true;
            }

            if (foundOnRing) return true; //rings go outward, so the first ring with a spot that fits has the closest one
        }

        finalPosition = position;
        nodeIndex = -1;
        return false;
    }

    public float GetBridgeStretch(int runeID, Vector2 desiredPosition, int otherID)
    {
        //how far past its reach a bridge would be if the rune were dragged here. 0 or less means it isn't stretched
        //the view uses this to decide how fast a bridge tears. A frozen bridge never tears (rule B), so it's never stretched
        if (IsBridgeFrozen(runeID, otherID)) return 0f;
        float distance = Vector2.Distance(desiredPosition, GetPosition(otherID));
        return distance - GetBridgeReach(runeID, otherID, runeID); //the dragged rune counts as out of its node
    }

    public bool IsBridgeTaut(int runeID, Vector2 heldPosition, int otherID, float padding)
    {
        //is this bridge one of the ones holding the dragged rune back: the rune sits at the end of its reach (within padding, in field units)
        //heldPosition is where ClampToBridges stopped the rune. The view only tears taut bridges, so the direction the player pulls picks which bridges tear
        float distance = Vector2.Distance(heldPosition, GetPosition(otherID));
        return distance >= GetBridgeReach(runeID, otherID, runeID) - padding; //the dragged rune counts as out of its node
    }

    List<ReachCircle> GetReachCircles(int runeID)
    {
        //the circles a dragged rune has to stay inside: one around the other end of each of its bridges, plus the field's edge bleed around the core
        //past the field's ring itself the drop is refused (red), but the rune can still be pulled a little way out
        List<ReachCircle> circles = new List<ReachCircle>();
        circles.Add(new ReachCircle(Vector2.zero, _settings.GetFieldEdge() + _settings._EdgeBleed)); //the core sits at (0, 0)

        List<int> connections = GetConnections(runeID);
        for (int i = 0; i < connections.Count; i++)
        {
            circles.Add(new ReachCircle(GetPosition(connections[i]), GetBridgeReach(runeID, connections[i], runeID))); //the dragged rune counts as out of its node
        }
        return circles;
    }

    bool IsInsideCircles(List<ReachCircle> circles, Vector2 position)
    {
        //checks a spot is inside every circle. Leash Tolerance lets through spots a hair outside from float rounding (like a crossing point)
        for (int i = 0; i < circles.Count; i++)
        {
            if (Vector2.Distance(position, circles[i]._Center) > circles[i]._Radius + LeashTolerance) return false;
        }
        return true;
    }

    static bool GetCircleCrossings(ReachCircle a, ReachCircle b, out Vector2 first, out Vector2 second)
    {
        //the two points where the edges of two circles cross. False if they don't cross (too far apart, one inside the other, or the same center)
        //how it works: the crossings sit on a line straight across the gap between the centers, "along" from a's center,
        //and "across" to either side of the line joining the two centers
        first = Vector2.zero;
        second = Vector2.zero;

        Vector2 gap = b._Center - a._Center;
        float distance = gap.magnitude;
        if (distance < 0.0001f) return false; //same center: either no crossing or the whole edge is shared
        if (distance > a._Radius + b._Radius) return false; //too far apart to touch
        if (distance < Mathf.Abs(a._Radius - b._Radius)) return false; //one circle sits inside the other

        float along = (a._Radius * a._Radius - b._Radius * b._Radius + distance * distance) / (2f * distance);
        float across = Mathf.Sqrt(Mathf.Max(0f, a._Radius * a._Radius - along * along)); //Max stops a tiny negative from rounding turning into NaN
        Vector2 direction = gap / distance;
        Vector2 side = new Vector2(-direction.y, direction.x); //the direction turned 90 degrees

        Vector2 middle = a._Center + direction * along;
        first = middle + side * across;
        second = middle - side * across;
        return true;
    }

    Vector2 ClampByPasses(List<ReachCircle> circles, Vector2 desiredPosition)
    {
        //backup clamp for when no spot is inside every circle: pulls the spot back inside each circle in turn, a few times over
        //it settles somewhere close, it just isn't always the closest spot (that's what ClampToBridges works out when the circles do overlap)
        Vector2 result = desiredPosition;
        for (int pass = 0; pass < ClampPasses; pass++)
        {
            bool moved = false;
            for (int i = 0; i < circles.Count; i++)
            {
                if (Vector2.Distance(result, circles[i]._Center) <= circles[i]._Radius) continue;
                result = circles[i]._Center + (result - circles[i]._Center).normalized * circles[i]._Radius;
                moved = true;
            }

            if (moved == false) break; //inside every circle, done
        }
        return result;
    }
    #endregion

    #region Ability Nodes
    public int FindNodeAt(Vector2 position)
    {
        //returns the index of the closest empty ability node whose snap radius covers this spot, or -1
        return FindNodeAt(position, NoRuneID);
    }

    int FindNodeAt(Vector2 position, int movingRuneID)
    {
        //same as above, but a node holding the rune being moved counts as empty, so it can be dropped back in its own node
        int closest = -1;
        float closestDistance = float.MaxValue;
        for (int i = 0; i < _nodeOrder.Count; i++)
        {
            int nodeIndex = _nodeOrder[i];
            int runeInNode = GetRuneInNode(nodeIndex);
            if (runeInNode != -1 && runeInNode != movingRuneID) continue; //already has another rune in it

            float distance = Vector2.Distance(position, _nodes[nodeIndex]._Position);
            if (distance <= _settings._NodeSnapRadius && distance < closestDistance)
            {
                closest = nodeIndex;
                closestDistance = distance;
            }
        }
        return closest;
    }

    public bool TryPlugRune(int runeID)
    {
        //function that plugs a rune into the node it's sitting on (what happens when the player drops it on one)
        //the rune snaps to the node's center
        PlacedRuneEntry rune = GetRune(runeID);
        if (rune == null) return false;
        if (GetPluggedNode(runeID) != -1) return false; //already plugged in somewhere (this covers locked-in runes too)

        int nodeIndex = FindNodeAt(rune._Position);
        if (nodeIndex == -1) return false;
        if (CanPlugInto(runeID, nodeIndex) == false) return false;

        AddPlug(runeID, nodeIndex);
        rune._Position = _nodes[nodeIndex]._Position;
        Recalculate();
        return true;
    }

    public bool CanSnapToNode(int runeID, int nodeIndex)
    {
        //checks that moving the rune to the node's center wouldn't stretch any of its bridges more than the settings' Snap Stretch past their reach
        if (HasNode(nodeIndex) == false) return false;
        if (runeID == NoRuneID) return true; //a new rune has no bridges yet

        Vector2 nodePosition = _nodes[nodeIndex]._Position;
        List<int> connections = GetConnections(runeID);
        for (int i = 0; i < connections.Count; i++)
        {
            float distance = Vector2.Distance(nodePosition, GetPosition(connections[i]));
            if (distance - GetBaseReach(runeID, connections[i]) > _settings._SnapStretch) return false;
        }
        return true;
    }

    public void UnplugRune(int runeID)
    {
        //function that takes a rune out of its node. Locked-in runes stay plugged (rule A). A frozen rune plugged in this draft can still come out
        if (IsRuneLockedIn(runeID)) return;
        if (RemovePlug(runeID)) Recalculate();
    }

    public bool CanPlugInto(int runeID, int nodeIndex)
    {
        //checks a node could take this rune: nothing plugged in locks it out, and snapping wouldn't overstretch the rune's bridges
        //nodes in any zone take runes: closed zones' nodes work again, and locked zones' nodes take planning runes (with no power they stay off)
        //runeID is the rune being moved (NoRuneID for a new one). Its own plug doesn't count, since it's leaving that node
        if (HasNode(nodeIndex) == false) return false;
        if (IsNodeBlocked(nodeIndex, runeID)) return false;

        //a powered frozen rune is the only thing that could turn on a node past the open zone early, so it can't plug in out there
        if (IsRuneFrozen(runeID) && IsPastOpenZone(_nodes[nodeIndex]._Position)) return false;
        return CanSnapToNode(runeID, nodeIndex);
    }

    public bool IsNodeBlocked(int nodeIndex)
    {
        //checks if a node can't take a rune right now: it's locked out (by a rival's plug, like the gates a shade didn't evolve through)
        //the view shows blocked nodes with their Locked background
        return IsNodeBlocked(nodeIndex, NoRuneID);
    }

    bool IsNodeBlocked(int nodeIndex, int movingRuneID)
    {
        //same as above, ignoring the plug of the rune being moved
        if (HasNode(nodeIndex) == false) return true;
        return IsNodeLockedOut(nodeIndex, movingRuneID);
    }

    void AddPlug(int runeID, int nodeIndex)
    {
        //records a rune sitting in a node. Doesn't move the rune or recalculate, the caller does that
        NodePlugEntry plug = new NodePlugEntry();
        plug._NodeIndex = nodeIndex;
        plug._RuneID = runeID;
        _data._Plugs.Add(plug);
    }

    bool RemovePlug(int runeID)
    {
        //removes a rune's plug. Returns false if it wasn't plugged in. Doesn't recalculate, the caller does that
        for (int i = _data._Plugs.Count - 1; i >= 0; i--)
        {
            if (_data._Plugs[i]._RuneID == runeID)
            {
                _data._Plugs.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
    #endregion

    #region Recalculate
    public void SetMaxPower(int maxPower)
    {
        //function used when the shade's level (or core fragment) changes
        _maxPower = Mathf.Max(0, maxPower);
        Recalculate();
    }

    public void Recalculate()
    {
        //works out power and ability nodes from scratch, then tells the view to redraw
        CalculatePower();
        CalculateNodes();
        OnFieldChanged?.Invoke();
    }

    void CalculatePower()
    {
        //hands out the core's power in two passes:
        //1. frozen runes first, walking out from the core along frozen bridges only (rule C). Otherwise a new rune bridged close to the core could
        //   take their power and cut the evolution gate off, which would be a devolve. Frozen bridges never change, so neither does this pass's order
        //2. then everything else, one "layer" at a time: runes 1 bridge from the core, then 2 bridges, and so on
        //inside a layer, the rune placed first goes first. A rune the core can't afford stays dark and doesn't pass power on
        //past the open zone: runes that aren't frozen (planning runes) never get power, and bridges crossing the open ring only carry power if they're frozen,
        //so nothing can power a locked zone early
        _poweredRunes.Clear();
        _powerUsed = 0;
        int powerLeft = _maxPower;

        powerLeft = PowerLayers(powerLeft, true);
        PowerLayers(powerLeft, false);
    }

    int PowerLayers(int powerLeft, bool frozenOnly)
    {
        //one power pass outward from the core. Returns the power left over
        //frozenOnly: only walks along frozen bridges, so a newer bridge between two frozen runes can't change their order
        //otherwise it walks every bridge, and runes already powered (the frozen ones) pass power on for free
        HashSet<int> visited = new HashSet<int>();
        visited.Add(CoreID);
        List<int> currentLayer = GetPowerConnections(CoreID, frozenOnly);

        while (currentLayer.Count > 0)
        {
            currentLayer.Sort(); //IDs go up in placement order, so sorting them puts the oldest rune first
            List<int> nextLayer = new List<int>();

            for (int i = 0; i < currentLayer.Count; i++)
            {
                int runeID = currentLayer[i];
                if (visited.Contains(runeID)) continue; //already reached by a shorter or earlier path
                visited.Add(runeID);
                if (IsRuneFrozen(runeID) == false)
                {
                    if (frozenOnly) continue; //the first pass stays on the frozen chain
                    if (IsPastOpenZone(runeID)) continue; //a planning rune: never powered, and doesn't pass power on
                }

                if (_poweredRunes.Contains(runeID) == false)
                {
                    int powerNeeded = GetPowerNeeded(runeID);
                    if (powerNeeded > powerLeft) continue; //not enough power. Stays dark, and power doesn't flow through it

                    powerLeft -= powerNeeded;
                    _powerUsed += powerNeeded;
                    _poweredRunes.Add(runeID);
                }

                //everything bridged to this rune is one bridge further out
                List<int> connections = GetPowerConnections(runeID, frozenOnly);
                for (int j = 0; j < connections.Count; j++)
                {
                    if (visited.Contains(connections[j]) == false && nextLayer.Contains(connections[j]) == false) nextLayer.Add(connections[j]);
                }
            }

            currentLayer = nextLayer;
        }
        return powerLeft;
    }

    List<int> GetPowerConnections(int id, bool frozenOnly)
    {
        //returns everything power can flow to from this rune (or the core) during a power pass
        //frozen pass: frozen bridges only. Normal pass: every bridge, except ones crossing the open ring that aren't frozen (they don't carry power across)
        if (frozenOnly) return GetFrozenConnections(id);

        List<int> connections = GetConnections(id);
        for (int i = connections.Count - 1; i >= 0; i--) //backwards, so removing an entry doesn't skip the next one
        {
            if (CrossesOpenRing(id, connections[i]) && IsBridgeFrozen(id, connections[i]) == false) connections.RemoveAt(i);
        }
        return connections;
    }

    void CalculateNodes()
    {
        //turns ability nodes on and off to match their rules (power, unlocks, and not being locked out)
        int maxPasses = _nodeOrder.Count + 1; //a normal setup settles within one pass per node, plus one to confirm nothing changed

        for (int pass = 0; pass < maxPasses; pass++)
        {
            bool changed = false;

            //turn off nodes that don't meet their rules anymore
            for (int i = _data._ActiveNodes.Count - 1; i >= 0; i--)
            {
                if (MeetsNodeRules(_data._ActiveNodes[i]) == false || IsNodeLockedOut(_data._ActiveNodes[i]))
                {
                    _data._ActiveNodes.RemoveAt(i);
                    changed = true;
                }
            }

            //turn on nodes that meet their rules and aren't locked out
            for (int i = 0; i < _nodeOrder.Count; i++)
            {
                int nodeIndex = _nodeOrder[i];
                if (IsNodeActive(nodeIndex)) continue;
                if (MeetsNodeRules(nodeIndex) == false || IsNodeLockedOut(nodeIndex)) continue;

                _data._ActiveNodes.Add(nodeIndex);
                changed = true;
            }

            if (changed == false) return; //every node is settled
        }

        Debug.LogWarning("Warning! Ability nodes kept switching on and off. Check the field scene for nodes that lock and unlock each other. Leaving them as they are...");
    }

    bool MeetsNodeRules(int nodeIndex)
    {
        //checks the Power and Unlock rules from the Ability Node GDD page (lockouts are checked separately)
        AbilityNodeEntry node = GetNode(nodeIndex);
        if (node == null) return false;

        int runeID = GetRuneInNode(nodeIndex);
        if (runeID == -1) return false; //nothing plugged in
        if (IsRunePowered(runeID) == false) return false;

        List<int> unlocks = node._Unlocks;
        if (unlocks == null) return true; //no unlock list means nothing else has to be on first
        for (int i = 0; i < unlocks.Count; i++)
        {
            if (unlocks[i] == nodeIndex) continue; //a node can't unlock itself
            if (IsNodeActive(unlocks[i]) == false) return false;
        }
        return true;
    }

    public bool IsNodeLockedOut(int nodeIndex)
    {
        //a node is locked out as soon as a rune is plugged into a node that locks it (power doesn't matter, see LocksOut)
        return IsNodeLockedOut(nodeIndex, NoRuneID);
    }

    bool IsNodeLockedOut(int nodeIndex, int movingRuneID)
    {
        //same as above, ignoring the plug of the rune being moved (it's leaving its node)
        //if this node has a rune plugged in too, only nodes plugged BEFORE it count, so the first node plugged always wins
        int ownPlug = FindPlugIndex(nodeIndex);

        for (int i = 0; i < _data._Plugs.Count; i++)
        {
            if (ownPlug != -1 && i >= ownPlug) break; //plugs go into the list in the order they happened
            NodePlugEntry plug = _data._Plugs[i];
            if (plug._NodeIndex == nodeIndex || plug._RuneID == movingRuneID) continue;

            if (LocksOut(plug._NodeIndex, nodeIndex)) return true;
        }
        return false;
    }

    bool LocksOut(int lockerIndex, int nodeIndex)
    {
        //checks if one node locks another: it's in the locker's lockout list, or both are evolution gates in the same zone (a branch choice, automatic)
        AbilityNodeEntry locker = GetNode(lockerIndex);
        AbilityNodeEntry node = GetNode(nodeIndex);
        if (locker == null || node == null) return false;

        if (locker._Lockouts != null && locker._Lockouts.Contains(nodeIndex)) return true;
        return locker.IsGate() && node.IsGate() && GetZone(locker._Position) == GetZone(node._Position);
    }

    int FindPlugIndex(int nodeIndex)
    {
        //returns where this node's plug is in the plug list, or -1 if nothing is plugged into it
        for (int i = 0; i < _data._Plugs.Count; i++)
        {
            if (_data._Plugs[i]._NodeIndex == nodeIndex) return i;
        }
        return -1;
    }
    #endregion

    #region Evolution
    public int GetPluggedGate()
    {
        //returns the evolution gate in the open zone that has a rune plugged in (powered or not), or -1
        //gates in the same zone lock each other out, so there's only ever one
        for (int i = 0; i < _nodeOrder.Count; i++)
        {
            int nodeIndex = _nodeOrder[i];
            if (IsOpenGate(nodeIndex) && GetRuneInNode(nodeIndex) != -1 && IsNodeLockedOut(nodeIndex) == false) return nodeIndex;
        }
        return -1;
    }

    public int GetEvolvingGate()
    {
        //returns the evolution gate in the open zone that's on (its rune has power), or -1. The shade evolves through this gate
        for (int i = 0; i < _nodeOrder.Count; i++)
        {
            int nodeIndex = _nodeOrder[i];
            if (IsOpenGate(nodeIndex) && IsNodeActive(nodeIndex)) return nodeIndex;
        }
        return -1;
    }

    bool IsOpenGate(int nodeIndex)
    {
        //an evolution gate the shade could still go through: it's in the open zone
        AbilityNodeEntry node = GetNode(nodeIndex);
        return node != null && node.IsGate() && IsInOpenZone(node._Position);
    }
    #endregion

    #region Effects
    public List<RuneEffect> CompileEffects()
    {
        //function that gathers everything this field does to its shade into one list: stat boosts from every powered rune, then the effects of every active node
        //everything in the list is a copy, so changing it never changes the rune assets or the nodes
        //this is the list the slot will store when the field is saved (Rune Field Overhaul Plan, step 4)
        List<RuneEffect> compiled = new List<RuneEffect>();

        //powered runes, in placement order
        for (int i = 0; i < _data._Runes.Count; i++)
        {
            PlacedRuneEntry rune = _data._Runes[i];
            if (IsRunePowered(rune._ID) == false) continue;

            List<StatChangeEffect> boosts = rune._Element.GetStatBoosts();
            for (int j = 0; j < boosts.Count; j++)
            {
                AddCompiledEffect(compiled, boosts[j]);
            }
        }

        //active nodes, in the order they turned on
        for (int i = 0; i < _data._ActiveNodes.Count; i++)
        {
            AbilityNodeEntry node = GetNode(_data._ActiveNodes[i]);
            if (node == null || node._Effects == null) continue;

            for (int j = 0; j < node._Effects.Count; j++)
            {
                AddCompiledEffect(compiled, node._Effects[j]);
            }
        }

        return compiled;
    }

    void AddCompiledEffect(List<RuneEffect> compiled, RuneEffect effect)
    {
        //adds a copy of one effect to a compiled list. Empty or broken effects are skipped (the inspector already warns about them)
        if (effect == null || effect.IsSetUp() == false) return;
        compiled.Add(effect.Clone());
    }

    public Dictionary<DamageType.StatType, int> GetStatTotals()
    {
        //adds up every stat change in the compiled list (powered runes and active nodes)
        return RuneEffect.AddUpStats(CompileEffects());
    }

    public List<AbilityNodeEntry> GetPluggedNodeSnapshots()
    {
        //function that copies every node with a rune plugged in (powered or not), smallest index first
        //the shade slot saves these so the rules can run later without the field scene (see SetUpNodes)
        List<AbilityNodeEntry> snapshots = new List<AbilityNodeEntry>();
        for (int i = 0; i < _nodeOrder.Count; i++)
        {
            if (GetRuneInNode(_nodeOrder[i]) == -1) continue;
            snapshots.Add(_nodes[_nodeOrder[i]].Clone());
        }
        return snapshots;
    }
    #endregion

    #region Setup Checks
    public static string FixOneWayLockouts(List<AbilityNodeEntry> nodes)
    {
        //function that makes every lockout two-way: if X locks Y but Y doesn't list X, Y gets X added so the game plays right
        //returns a line per fix (or "" if there were none) so the field scene can log them and the scene can be fixed. Run once when the scene builds its nodes
        string problems = "";
        if (nodes == null) return problems;

        for (int i = 0; i < nodes.Count; i++)
        {
            AbilityNodeEntry node = nodes[i];
            if (node == null || node._Lockouts == null) continue;

            for (int j = 0; j < node._Lockouts.Count; j++)
            {
                AbilityNodeEntry other = FindNode(nodes, node._Lockouts[j]);
                if (other == null || other == node) continue;
                if (other._Lockouts == null) other._Lockouts = new List<int>();
                if (other._Lockouts.Contains(node._NodeIndex)) continue; //already two-way

                other._Lockouts.Add(node._NodeIndex);
                problems += $"{node._Name} locks out {other._Name}, but {other._Name} doesn't lock out {node._Name}. Fixed for this run, add it to {other._Name}'s Lockouts\n";
            }
        }
        return problems;
    }

    public static string CheckGates(List<AbilityNodeEntry> nodes, RuneFieldSettingsSO settings)
    {
        //function that checks every evolution gate sits on a ring the shade can evolve through (not off a ring, and not on the outer edge)
        //returns a line per problem, or "" if they're all fine
        string problems = "";
        if (nodes == null || settings == null) return problems;

        for (int i = 0; i < nodes.Count; i++)
        {
            AbilityNodeEntry node = nodes[i];
            if (node == null || node.IsGate() == false) continue;

            if (settings.IsOnRing(node._Position.magnitude, out int ring) == false)
                problems += $"Gate {node._Name} isn't on a zone ring, so it isn't the same distance from the core as the other gates. Use its Snap buttons\n";
            else if (ring > settings.GetLastGateRing())
                problems += $"Gate {node._Name} is on ring {ring}, the field's outer edge. There's nothing to evolve into from there, gates go on rings 1 to {settings.GetLastGateRing()}\n";
        }
        return problems;
    }

    static AbilityNodeEntry FindNode(List<AbilityNodeEntry> nodes, int nodeIndex)
    {
        //finds a node in a list by its index, or null
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i]._NodeIndex == nodeIndex) return nodes[i];
        }
        return null;
    }
    #endregion

    #region Initialize
    void SetUpNodes(List<AbilityNodeEntry> nodes)
    {
        //fills the node lookup from the list this field was given. Empty entries and repeated indexes are skipped
        _nodes.Clear();
        _nodeOrder.Clear();
        if (nodes == null) return; //no nodes is fine, the field just has nothing to plug into

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null) continue;
            if (_nodes.ContainsKey(nodes[i]._NodeIndex)) { Debug.LogWarning($"Warning! Two rune field nodes use index {nodes[i]._NodeIndex}, keeping the first one..."); continue; }

            _nodes.Add(nodes[i]._NodeIndex, nodes[i]);
            _nodeOrder.Add(nodes[i]._NodeIndex);
        }
        _nodeOrder.Sort();
    }

    void CleanUpData()
    {
        //removes anything in the saved data that points at something that doesn't exist, so the rest of the code can trust it
        HashSet<int> runeIDs = new HashSet<int>();

        //runes: no element, or a repeated ID
        for (int i = _data._Runes.Count - 1; i >= 0; i--)
        {
            PlacedRuneEntry rune = _data._Runes[i];
            if (rune == null || rune._Element == null || runeIDs.Contains(rune._ID))
            {
                Debug.LogWarning("Warning! Saved rune field had a rune with no element or a repeated ID, removing it...");
                _data._Runes.RemoveAt(i);
                continue;
            }
            runeIDs.Add(rune._ID);
            if (rune._ID >= _data._NextRuneID) _data._NextRuneID = rune._ID + 1; //makes sure new runes never reuse an ID
        }

        //bridges: missing ends, joined to itself, or repeated
        for (int i = _data._Bridges.Count - 1; i >= 0; i--)
        {
            RuneBridgeEntry bridge = _data._Bridges[i];
            bool endAExists = bridge._A == CoreID || runeIDs.Contains(bridge._A);
            bool endBExists = bridge._B == CoreID || runeIDs.Contains(bridge._B);
            bool repeated = FindBridgeIndex(bridge._A, bridge._B) != i; //an earlier entry already joins these two

            if (endAExists == false || endBExists == false || bridge._A == bridge._B || repeated) _data._Bridges.RemoveAt(i);
        }

        //plugs: missing rune or node, or a node/rune used twice
        HashSet<int> usedNodes = new HashSet<int>();
        HashSet<int> usedRunes = new HashSet<int>();
        for (int i = 0; i < _data._Plugs.Count; i++)
        {
            NodePlugEntry plug = _data._Plugs[i];
            bool valid = HasNode(plug._NodeIndex) && runeIDs.Contains(plug._RuneID) && usedNodes.Contains(plug._NodeIndex) == false && usedRunes.Contains(plug._RuneID) == false;

            if (valid == false)
            {
                _data._Plugs.RemoveAt(i);
                i--; //the next entry slid into this spot, so check this index again
                continue;
            }
            usedNodes.Add(plug._NodeIndex);
            usedRunes.Add(plug._RuneID);
        }

        //active nodes: a node this field doesn't know
        for (int i = _data._ActiveNodes.Count - 1; i >= 0; i--)
        {
            if (HasNode(_data._ActiveNodes[i]) == false) _data._ActiveNodes.RemoveAt(i);
        }

        CleanUpFrozenLists(runeIDs);
    }

    void CleanUpFrozenLists(HashSet<int> runeIDs)
    {
        //cleans the frozen lists the same way. Runs after the runes, bridges and plugs are cleaned, since it checks against them
        //data saved before these lists existed can have them missing, so they're made first
        if (_data._FrozenRunes == null) _data._FrozenRunes = new List<int>();
        if (_data._FrozenBridges == null) _data._FrozenBridges = new List<RuneBridgeEntry>();
        if (_data._LockedRunes == null) _data._LockedRunes = new List<int>();

        //frozen runes: a rune that doesn't exist, or listed twice
        HashSet<int> seen = new HashSet<int>();
        for (int i = 0; i < _data._FrozenRunes.Count; i++)
        {
            int runeID = _data._FrozenRunes[i];
            if (runeIDs.Contains(runeID) && seen.Contains(runeID) == false) { seen.Add(runeID); continue; }

            _data._FrozenRunes.RemoveAt(i);
            i--; //the next entry slid into this spot, so check this index again
        }

        //frozen bridges: empty entries first, so the repeat check below never reads one
        for (int i = _data._FrozenBridges.Count - 1; i >= 0; i--)
        {
            if (_data._FrozenBridges[i] == null) _data._FrozenBridges.RemoveAt(i);
        }

        //then repeated, an end that isn't frozen, or no real bridge behind it
        //the last two shouldn't happen (frozen bridges can't be cut), so they're dropped with a warning instead of putting the bridge back,
        //since putting it back could go over a rune's bridge limit
        for (int i = _data._FrozenBridges.Count - 1; i >= 0; i--)
        {
            RuneBridgeEntry bridge = _data._FrozenBridges[i];
            if (FindFrozenBridgeIndex(bridge._A, bridge._B) != i) { _data._FrozenBridges.RemoveAt(i); continue; } //an earlier entry already has these two

            if (IsRuneFrozen(bridge._A) == false || IsRuneFrozen(bridge._B) == false || FindBridgeIndex(bridge._A, bridge._B) == -1)
            {
                Debug.LogWarning($"Warning! Saved rune field had a frozen bridge ({bridge._A} to {bridge._B}) with a missing or unfrozen end, or no bridge behind it. Removing it from the frozen bridges...");
                _data._FrozenBridges.RemoveAt(i);
            }
        }

        //locked runes: has to be a frozen rune that's plugged into a node, listed once
        seen.Clear();
        for (int i = 0; i < _data._LockedRunes.Count; i++)
        {
            int runeID = _data._LockedRunes[i];
            if (IsRuneFrozen(runeID) && GetPluggedNode(runeID) != -1 && seen.Contains(runeID) == false) { seen.Add(runeID); continue; }

            _data._LockedRunes.RemoveAt(i);
            i--;
        }
    }
    #endregion
}

//one circle a dragged rune has to stay inside: a bridge's reach around its other end, or the field's edge. Only used inside RuneField
class ReachCircle
{
    public Vector2 _Center;
    public float _Radius;

    public ReachCircle(Vector2 center, float radius)
    {
        _Center = center;
        _Radius = radius;
    }
}
