using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//the rules of a rune field: placing runes, bridges, power and ability nodes
//this is a plain C# class, not a MonoBehaviour. It doesn't live on a GameObject, it's created with "new RuneField(...)" by whatever shows the field
//the view (2D or 3D) only draws what this class says and sends player actions to it, so the rules work the same no matter how the field looks
public class RuneField
{
    public const int CoreID = -1; //the core is a bridge end like any rune, it just always has this ID
    const int ClampPasses = 10; //how many times ClampToBridges re-checks every bridge when a rune is pulled by more than one

    //local variables
    RuneFieldData _data; //the field being edited. Usually a copy of the saved data (see RuneFieldData.Clone)
    RuneFieldLayoutSO _layout; //core settings and ability nodes, shared by every shade slot
    int _maxPower; //how much power the core has (shade level, plus a core fragment later)
    int _powerUsed; //worked out by CalculatePower
    HashSet<int> _poweredRunes = new HashSet<int>(); //IDs of runes that got their power. A HashSet is a list that's fast at "is this in here?" and can't hold duplicates

    public event System.Action OnFieldChanged; //fires after every recalculation so the view can redraw

    public RuneField(RuneFieldData data, RuneFieldLayoutSO layout, int maxPower)
    {
        //sets up a field from saved data. Bad entries in the data get cleaned out, then everything is worked out once
        _data = data;
        _layout = layout;
        _maxPower = Mathf.Max(0, maxPower);

        if (_data == null) { Debug.LogWarning("Warning! Rune field was given no data, starting with an empty field..."); _data = new RuneFieldData(); }
        //no layout is allowed on purpose: the Shade Manager can still work out power and stats before the rune field UI has given it one
        //in that mode ability nodes are left exactly as saved, and new bridges to the core can't be made (0 reach)

        CleanUpData();
        Recalculate();
    }

    #region Getters
    public RuneFieldData GetData()
    {
        return _data;
    }

    public RuneFieldLayoutSO GetLayout()
    {
        return _layout;
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

    public int GetNodeCount()
    {
        if (_layout == null) return 0;
        return _layout._AbilityNodes.Count;
    }

    public float GetReach(int id)
    {
        //how far this rune (or the core) reaches to make a bridge, in field units
        if (id == CoreID) return _layout != null ? _layout._CoreReach : 0f; //"a ? b : c" means "if a then b, otherwise c"

        PlacedRuneEntry rune = GetRune(id);
        if (rune == null) return 0f;
        return rune._Element.connectionDistance;
    }

    public float GetBridgeReach(int idA, int idB)
    {
        //how long a bridge between these two can be: whichever of the two reaches is bigger
        return Mathf.Max(GetReach(idA), GetReach(idB));
    }

    public int GetMaxBridges(int id)
    {
        //how many bridges this rune (or the core) can have
        if (id == CoreID)
        {
            if (_layout == null || _layout._CoreMaxBridges <= 0) return int.MaxValue; //0 means no limit
            return _layout._CoreMaxBridges;
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
    #endregion

    #region Rune Editing
    public bool TryPlaceRune(ElementItemSO element, Vector2 position, out int runeID)
    {
        //function that puts a new rune on the field. It doesn't make any bridges, call ConnectNearby after
        //returns false (and runeID = -2) if the rune can't go there
        runeID = -2;
        if (element == null) { Debug.LogWarning("Warning! Tried to place a rune with no element, nothing placed..."); return false; }
        if (IsInsideField(position) == false) return false;

        PlacedRuneEntry rune = new PlacedRuneEntry();
        rune._ID = _data._NextRuneID;
        rune._Element = element;
        rune._Position = position;

        _data._NextRuneID++;
        _data._Runes.Add(rune);
        runeID = rune._ID;

        Recalculate();
        return true;
    }

    public void MoveRune(int runeID, Vector2 position)
    {
        //function that moves a rune. It doesn't check bridges, use ClampToBridges first if the rune has any
        //power only cares about bridges, not positions, so this doesn't recalculate (unless it pulls the rune out of a node)
        PlacedRuneEntry rune = GetRune(runeID);
        if (rune == null) return;

        rune._Position = ClampToField(position);
        if (GetPluggedNode(runeID) != -1) UnplugRune(runeID); //a moved rune isn't sitting in its node anymore
    }

    public void RemoveRune(int runeID)
    {
        //function that takes a rune off the field along with its bridges and plug
        PlacedRuneEntry rune = GetRune(runeID);
        if (rune == null) return;

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
    }

    public bool IsInsideField(Vector2 position)
    {
        //checks if a spot is within the field's radius
        if (_layout == null || _layout._FieldRadius <= 0f) return true; //0 means no limit
        return position.magnitude <= _layout._FieldRadius;
    }

    Vector2 ClampToField(Vector2 position)
    {
        //pulls a spot back inside the field's radius if it's outside
        if (IsInsideField(position)) return position;
        return position.normalized * _layout._FieldRadius;
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
        return distance <= GetBridgeReach(idA, idB);
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
        //function that removes a bridge (what happens when one tears)
        int index = FindBridgeIndex(idA, idB);
        if (index == -1) return;

        _data._Bridges.RemoveAt(index);
        Recalculate();
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
                float reach = GetBridgeReach(runeID, connections[i]);

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
        return distance - GetBridgeReach(runeID, otherID);
    }
    #endregion

    #region Ability Nodes
    public int FindNodeAt(Vector2 position)
    {
        //returns the index of the closest empty ability node whose snap radius covers this spot, or -1
        if (_layout == null) return -1;

        int closest = -1;
        float closestDistance = float.MaxValue;
        for (int i = 0; i < _layout._AbilityNodes.Count; i++)
        {
            AbilityNodeEntry node = _layout._AbilityNodes[i];
            if (node == null) continue;
            if (GetRuneInNode(i) != -1) continue; //already has a rune in it

            float distance = Vector2.Distance(position, node._Position);
            if (distance <= node._SnapRadius && distance < closestDistance)
            {
                closest = i;
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

        int nodeIndex = FindNodeAt(rune._Position);
        if (nodeIndex == -1) return false;
        if (CanSnapToNode(runeID, nodeIndex) == false) return false;

        NodePlugEntry plug = new NodePlugEntry();
        plug._NodeIndex = nodeIndex;
        plug._RuneID = runeID;
        _data._Plugs.Add(plug);

        rune._Position = _layout._AbilityNodes[nodeIndex]._Position;
        Recalculate();
        return true;
    }

    public bool CanSnapToNode(int runeID, int nodeIndex)
    {
        //checks that moving the rune to the node's center wouldn't stretch any of its bridges past their reach
        //the view can also use this while dragging to decide whether to show the snap
        if (nodeIndex < 0 || nodeIndex >= GetNodeCount()) return false;

        Vector2 nodePosition = _layout._AbilityNodes[nodeIndex]._Position;
        List<int> connections = GetConnections(runeID);
        for (int i = 0; i < connections.Count; i++)
        {
            if (GetBridgeStretch(runeID, nodePosition, connections[i]) > 0f) return false;
        }
        return true;
    }

    public void UnplugRune(int runeID)
    {
        //function that takes a rune out of its node (what happens when the player starts dragging it)
        for (int i = _data._Plugs.Count - 1; i >= 0; i--)
        {
            if (_data._Plugs[i]._RuneID == runeID)
            {
                _data._Plugs.RemoveAt(i);
                Recalculate();
                return;
            }
        }
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
        //hands out the core's power one "layer" at a time: runes 1 bridge from the core, then 2 bridges, and so on
        //inside a layer, the rune placed first goes first. A rune the core can't afford stays dark and doesn't pass power on
        _poweredRunes.Clear();
        _powerUsed = 0;
        int powerLeft = _maxPower;

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

                int powerNeeded = GetPowerNeeded(runeID);
                if (powerNeeded > powerLeft) continue; //not enough power. Stays dark, and power doesn't flow through it

                powerLeft -= powerNeeded;
                _powerUsed += powerNeeded;
                _poweredRunes.Add(runeID);

                //everything bridged to this rune is one bridge further out
                List<int> connections = GetConnections(runeID);
                for (int j = 0; j < connections.Count; j++)
                {
                    if (visited.Contains(connections[j]) == false && nextLayer.Contains(connections[j]) == false) nextLayer.Add(connections[j]);
                }
            }

            currentLayer = nextLayer;
        }
    }

    void CalculateNodes()
    {
        //turns ability nodes on and off to match their rules
        //nodes that are already on keep their spot, so when two nodes lock each other, the one that turned on first stays the winner
        if (_layout == null) return; //no layout, so the saved node states are left alone (see the constructor)
        int nodeCount = GetNodeCount();
        int maxPasses = nodeCount + 1; //a normal setup settles within one pass per node, plus one to confirm nothing changed

        for (int pass = 0; pass < maxPasses; pass++)
        {
            bool changed = false;

            //turn off nodes that don't meet their rules anymore
            for (int i = _data._ActiveNodes.Count - 1; i >= 0; i--)
            {
                if (MeetsNodeRules(_data._ActiveNodes[i]) == false)
                {
                    _data._ActiveNodes.RemoveAt(i);
                    changed = true;
                }
            }

            //turn on nodes that meet their rules and aren't locked out
            for (int i = 0; i < nodeCount; i++)
            {
                if (IsNodeActive(i)) continue;
                if (MeetsNodeRules(i) == false || IsNodeLockedOut(i)) continue;

                _data._ActiveNodes.Add(i);
                changed = true;
            }

            if (changed == false) return; //every node is settled
        }

        Debug.LogWarning("Warning! Ability nodes kept switching on and off. Check the layout for nodes that lock and unlock each other. Leaving them as they are...");
    }

    bool MeetsNodeRules(int nodeIndex)
    {
        //checks the Power and Unlock rules from the Ability Node GDD page (lockouts are checked separately)
        if (nodeIndex < 0 || nodeIndex >= GetNodeCount()) return false;

        int runeID = GetRuneInNode(nodeIndex);
        if (runeID == -1) return false; //nothing plugged in
        if (IsRunePowered(runeID) == false) return false;

        List<int> unlocks = _layout._AbilityNodes[nodeIndex]._Unlocks;
        for (int i = 0; i < unlocks.Count; i++)
        {
            if (unlocks[i] == nodeIndex) continue; //a node can't unlock itself (the layout warns about this)
            if (IsNodeActive(unlocks[i]) == false) return false;
        }
        return true;
    }

    public bool IsNodeLockedOut(int nodeIndex)
    {
        //a node is locked out if any node that's on has it in its lockout list
        if (_layout == null) return false;
        for (int i = 0; i < _data._ActiveNodes.Count; i++)
        {
            int activeIndex = _data._ActiveNodes[i];
            if (activeIndex == nodeIndex) continue;
            if (_layout._AbilityNodes[activeIndex]._Lockouts.Contains(nodeIndex)) return true;
        }
        return false;
    }

    public Dictionary<DamageType.StatType, int> GetStatTotals()
    {
        //adds up the stat boosts from every powered rune
        //todo: evolution multiplier, the minimum of 1 per stat, and Hazen's stats get applied when this is hooked up to the Shade Manager
        Dictionary<DamageType.StatType, int> totals = new Dictionary<DamageType.StatType, int>();

        for (int i = 0; i < _data._Runes.Count; i++)
        {
            PlacedRuneEntry rune = _data._Runes[i];
            if (IsRunePowered(rune._ID) == false) continue;

            List<statBoostPackage> boosts = rune._Element.GetStatBoostPackage();
            if (boosts == null) continue;

            for (int j = 0; j < boosts.Count; j++)
            {
                if (boosts[j] == null) continue;
                DamageType.StatType stat = boosts[j]._statToChange;

                if (totals.ContainsKey(stat)) totals[stat] += boosts[j]._ChangeAmount;
                else totals.Add(stat, boosts[j]._ChangeAmount);
            }
        }
        return totals;
    }
    #endregion

    #region Initialize
    void CleanUpData()
    {
        //removes anything in the saved data that points at something that doesn't exist, so the rest of the code can trust it
        int nodeCount = GetNodeCount();
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

        if (_layout == null) return; //can't check plugs or active nodes without the node list, so they're kept as saved

        //plugs: missing rune or node, or a node/rune used twice
        HashSet<int> usedNodes = new HashSet<int>();
        HashSet<int> usedRunes = new HashSet<int>();
        for (int i = 0; i < _data._Plugs.Count; i++)
        {
            NodePlugEntry plug = _data._Plugs[i];
            bool valid = plug._NodeIndex >= 0 && plug._NodeIndex < nodeCount && runeIDs.Contains(plug._RuneID) && usedNodes.Contains(plug._NodeIndex) == false && usedRunes.Contains(plug._RuneID) == false;

            if (valid == false)
            {
                _data._Plugs.RemoveAt(i);
                i--; //the next entry slid into this spot, so check this index again
                continue;
            }
            usedNodes.Add(plug._NodeIndex);
            usedRunes.Add(plug._RuneID);
        }

        //active nodes: index outside the layout's node list
        for (int i = _data._ActiveNodes.Count - 1; i >= 0; i--)
        {
            if (_data._ActiveNodes[i] < 0 || _data._ActiveNodes[i] >= nodeCount) _data._ActiveNodes.RemoveAt(i);
        }
    }
    #endregion
}
