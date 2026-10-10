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
    const int ClampPasses = 10; //how many times ClampToBridges re-checks every bridge when a rune is pulled by more than one

    //local variables
    RuneFieldData _data; //the field being edited. Usually a copy of the saved data (see RuneFieldData.Clone)
    RuneFieldSettingsSO _settings; //core reach, field radius, snap radius etc, shared by every shade slot
    Dictionary<int, AbilityNodeEntry> _nodes = new Dictionary<int, AbilityNodeEntry>(); //node index -> node. A Dictionary finds an entry by its key, so gaps in the indexes are fine
    List<int> _nodeOrder = new List<int>(); //every node index, smallest first, so nodes are always checked in the same order
    int _maxPower; //how much power the core has (shade level, plus a core fragment later)
    int _rank; //how many times the shade has evolved (Bound = 0). Zones up to this are frozen, zone rank + 1 is the one the player can build in
    int _powerUsed; //worked out by CalculatePower
    HashSet<int> _poweredRunes = new HashSet<int>(); //IDs of runes that got their power. A HashSet is a list that's fast at "is this in here?" and can't hold duplicates

    public event System.Action OnFieldChanged; //fires after every recalculation so the view can redraw

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
        List<int> connections = new List<int>();
        for (int i = 0; i < _data._Bridges.Count; i++)
        {
            if (_data._Bridges[i]._A == id) connections.Add(_data._Bridges[i]._B);
            else if (_data._Bridges[i]._B == id) connections.Add(_data._Bridges[i]._A);
        }
        return connections;
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
        //the one zone the player can build in: the one past the last zone the shade evolved out of (a Bound shade uses zone 1)
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
        //checks a rune could sit here zone-wise: inside the field and in the open zone. Zones further out are locked, zones further in are frozen
        return IsInsideField(position) && GetZone(position) == GetOpenZone();
    }

    public bool IsRuneFrozen(int id)
    {
        //a rune is frozen once the shade has evolved out of its zone: it can't be moved, removed or unplugged anymore
        //the core counts as frozen, so a bridge from the core to a frozen rune is frozen too
        if (id == CoreID) return true;

        PlacedRuneEntry rune = GetRune(id);
        if (rune == null) return false;
        return GetZone(rune._Position) <= _rank;
    }

    public bool IsBridgeFrozen(int idA, int idB)
    {
        //a bridge is frozen if both of its ends are, so it can't tear. A bridge from a frozen rune to a new one is still normal
        return IsRuneFrozen(idA) && IsRuneFrozen(idB);
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
        if (IsRuneFrozen(runeID)) return false; //frozen runes never move
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
        //2. anywhere else: allowed if it's in the open zone and doesn't overlap a rune, the core or a node (see IsSpotFree)
        //the view uses this while dragging to show where the rune would land, or that it can't go there
        finalPosition = position;
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

        return IsSpotFree(runeID, position);
    }

    public bool IsSpotFree(int runeID, Vector2 position)
    {
        //checks a rune's footprint here doesn't overlap another rune, the core or a node, and is in the open zone
        //footprints are circles: two runes overlap if their centers are closer than one rune size
        if (IsInOpenZone(position) == false) return false;

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
        if (IsRuneFrozen(runeID)) return false; //frozen runes stay for good

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

    Vector2 ClampToField(Vector2 position)
    {
        //pulls a dragged spot back inside the outer ring plus the edge bleed. Past the ring itself the drop is refused (red), but the rune can still be pulled a little way out
        float maxDistance = _settings.GetFieldEdge() + _settings._EdgeBleed;
        if (position.magnitude <= maxDistance) return position;
        return position.normalized * maxDistance;
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
        for (int i = 0; i < _data._Bridges.Count; i++)
        {
            RuneBridgeEntry bridge = _data._Bridges[i];
            if ((bridge._A == idA && bridge._B == idB) || (bridge._A == idB && bridge._B == idA)) return i;
        }
        return -1;
    }
    #endregion

    #region Dragging Limits
    public Vector2 ClampToBridges(int runeID, Vector2 desiredPosition)
    {
        //function that returns the closest spot to where the player is dragging that keeps every bridge within reach
        //each bridge is a circle around the other end, and the rune has to stay inside all of them
        Vector2 result = ClampToField(desiredPosition);
        List<int> connections = GetConnections(runeID);

        //pulling back inside one circle can push the rune out of another, so it checks a few times
        for (int pass = 0; pass < ClampPasses; pass++)
        {
            bool moved = false;
            for (int i = 0; i < connections.Count; i++)
            {
                Vector2 center = GetPosition(connections[i]);
                float reach = GetBridgeReach(runeID, connections[i], runeID); //the dragged rune counts as out of its node

                if (Vector2.Distance(result, center) > reach)
                {
                    result = center + (result - center).normalized * reach;
                    moved = true;
                }
            }

            if (moved == false) break; //inside every circle, done
        }

        return result;
    }

    public float GetBridgeStretch(int runeID, Vector2 desiredPosition, int otherID)
    {
        //how far past its reach a bridge would be if the rune were dragged here. 0 or less means it isn't stretched
        //the view uses this to decide how fast a bridge tears
        float distance = Vector2.Distance(desiredPosition, GetPosition(otherID));
        return distance - GetBridgeReach(runeID, otherID, runeID); //the dragged rune counts as out of its node
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
        if (GetPluggedNode(runeID) != -1) return false; //already plugged in somewhere
        if (IsRuneFrozen(runeID)) return false;

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
        //function that takes a rune out of its node. Frozen runes stay plugged
        if (IsRuneFrozen(runeID)) return;
        if (RemovePlug(runeID)) Recalculate();
    }

    public bool CanPlugInto(int runeID, int nodeIndex)
    {
        //checks a node could take this rune: it's in the open zone, nothing plugged in locks it out, and snapping wouldn't overstretch the rune's bridges
        //runeID is the rune being moved (NoRuneID for a new one). Its own plug doesn't count, since it's leaving that node
        if (HasNode(nodeIndex) == false) return false;
        if (IsNodeBlocked(nodeIndex, runeID)) return false;
        return CanSnapToNode(runeID, nodeIndex);
    }

    public bool IsNodeBlocked(int nodeIndex)
    {
        //checks if a node can't take a rune right now: it's outside the open zone (locked, or frozen like an evolution gate that wasn't taken), or it's locked out
        //the view shows blocked nodes with their Locked background
        return IsNodeBlocked(nodeIndex, NoRuneID);
    }

    bool IsNodeBlocked(int nodeIndex, int movingRuneID)
    {
        //same as above, ignoring the plug of the rune being moved
        AbilityNodeEntry node = GetNode(nodeIndex);
        if (node == null) return true;
        if (IsInOpenZone(node._Position) == false) return true;
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
        //1. frozen runes first, walking out from the core along the frozen chain. Otherwise a new rune bridged close to the core could
        //   take their power and cut the evolution gate off, which would be a devolve
        //2. then everything else, one "layer" at a time: runes 1 bridge from the core, then 2 bridges, and so on
        //inside a layer, the rune placed first goes first. A rune the core can't afford stays dark and doesn't pass power on
        _poweredRunes.Clear();
        _powerUsed = 0;
        int powerLeft = _maxPower;

        powerLeft = PowerLayers(powerLeft, true);
        PowerLayers(powerLeft, false);
    }

    int PowerLayers(int powerLeft, bool frozenOnly)
    {
        //one power pass outward from the core. Returns the power left over
        //frozenOnly: only walks through frozen runes. Otherwise runes already powered (the frozen ones) pass power on for free
        HashSet<int> visited = new HashSet<int>();
        visited.Add(CoreID);
        List<int> currentLayer = GetConnections(CoreID);

        while (currentLayer.Count > 0)
        {
            currentLayer.Sort(); //IDs go up in placement order, so sorting them puts the oldest rune first
            List<int> nextLayer = new List<int>();

            for (int i = 0; i < currentLayer.Count; i++)
            {
                int runeID = currentLayer[i];
                if (visited.Contains(runeID)) continue; //already reached by a shorter or earlier path
                visited.Add(runeID);
                if (frozenOnly && IsRuneFrozen(runeID) == false) continue; //the first pass stays on the frozen chain

                if (_poweredRunes.Contains(runeID) == false)
                {
                    int powerNeeded = GetPowerNeeded(runeID);
                    if (powerNeeded > powerLeft) continue; //not enough power. Stays dark, and power doesn't flow through it

                    powerLeft -= powerNeeded;
                    _powerUsed += powerNeeded;
                    _poweredRunes.Add(runeID);
                }

                //everything bridged to this rune is one bridge further out
                List<int> connections = GetConnections(runeID);
                for (int j = 0; j < connections.Count; j++)
                {
                    if (visited.Contains(connections[j]) == false && nextLayer.Contains(connections[j]) == false) nextLayer.Add(connections[j]);
                }
            }

            currentLayer = nextLayer;
        }
        return powerLeft;
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
    }
    #endregion
}
