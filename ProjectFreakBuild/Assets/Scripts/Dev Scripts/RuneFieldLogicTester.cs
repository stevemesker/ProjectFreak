using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//dev tool that runs the rune field rules through a set of small made-up fields and checks the results
//put it on any object, then press the button in the inspector (works outside play mode). Everything it makes is temporary and cleaned up after
public class RuneFieldLogicTester : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Also log every check that passes, not just the failures")]
    [SerializeField] bool _LogPasses = false;

    //local variables
    int _passed;
    int _failed;
    List<Object> _madeAssets = new List<Object>(); //temporary elements and settings assets, destroyed when the tests finish

    #region Test Tools
    [Button("Run Rune Field Tests"), GUIColor(0.4f, 1f, 0.4f)]
    void RunTests()
    {
        //runs every test and logs a summary
        _passed = 0;
        _failed = 0;

        TestBridgeReach();
        TestMaxBridges();
        TestPowerPriority();
        TestExpensiveRuneBlocksChain();
        TestLoopTear();
        TestClampToBridges();
        TestNodeLockoutsAndUnlocks();
        TestMaxPowerAndRemoval();
        TestCloneAndCleanUp();
        TestSnapshots();
        TestCompileEffects();
        TestGates();
        TestCountRunes();
        TestFootprints();
        TestDropRefused();
        TestSnapStretch();
        TestRestoreBridges();
        TestZoneMath();
        TestZonePlacement();
        TestFrozen();
        TestFrozenPowerFirst();
        TestPlugLockouts();
        TestGateZones();
        TestSetupChecks();

        CleanUp();

        if (_failed == 0) Debug.Log($"Rune field tests: all {_passed} checks passed", this);
        else Debug.LogError($"Error! Rune field tests: {_failed} failed, {_passed} passed. See the errors above", this);
    }
    #endregion

    #region Tests
    void TestBridgeReach()
    {
        //a rune inside the core's reach bridges to it, one far away bridges to nothing
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 10);

        int near = Place(field, element, new Vector2(100, 0));
        int far = Place(field, element, new Vector2(600, 0));

        Check(field.AreConnected(near, RuneField.CoreID), "Reach: a rune 100 from the core bridges to it");
        Check(field.GetConnections(far).Count == 0, "Reach: a rune 600 away bridges to nothing");
        Check(field.IsRunePowered(near) && field.IsRunePowered(far) == false, "Reach: only the bridged rune is powered");
    }

    void TestMaxBridges()
    {
        //a rune with 2 max bridges dropped next to 4 free runes only bridges to 2 of them
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 10);

        Place(field, element, new Vector2(0, 400), false);
        Place(field, element, new Vector2(100, 300), false);
        Place(field, element, new Vector2(-100, 300), false);
        Place(field, element, new Vector2(0, 200), false);
        int middle = Place(field, element, new Vector2(0, 300));

        Check(field.GetConnections(middle).Count == 2, "Max bridges: a rune with 2 max bridges only makes 2, even with 4 in reach");
    }

    void TestPowerPriority()
    {
        //with 2 power, the two runes 1 bridge from the core get it, even though the 2 bridge rune was placed earlier
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 2);

        int a = Place(field, element, new Vector2(100, 0));  //1 bridge out, placed 1st
        int b = Place(field, element, new Vector2(200, 0));  //2 bridges out (through a), placed 2nd
        int c = Place(field, element, new Vector2(-100, 0)); //1 bridge out, placed 3rd

        Check(field.IsRunePowered(a) && field.IsRunePowered(c), "Power priority: both 1 bridge runes are powered");
        Check(field.IsRunePowered(b) == false, "Power priority: the 2 bridge rune is dark even though it was placed before c");
        Check(field.GetPowerUsed() == 2 && field.GetPowerLeft() == 0, "Power priority: 2 of 2 power used");
    }

    void TestExpensiveRuneBlocksChain()
    {
        //a rune the core can't afford stays dark, the rune past it gets nothing, and a cheaper rune elsewhere still gets power
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO cheap = MakeElement(1, 2, 150f);
        ElementItemSO expensive = MakeElement(5, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 3);

        int big = Place(field, expensive, new Vector2(100, 0));
        int behindBig = Place(field, cheap, new Vector2(200, 0));
        int other = Place(field, cheap, new Vector2(-100, 0));

        Check(field.IsRunePowered(big) == false, "Blocked chain: a 5 power rune on a 3 power core stays dark");
        Check(field.IsRunePowered(behindBig) == false, "Blocked chain: power doesn't flow through a dark rune");
        Check(field.IsRunePowered(other), "Blocked chain: a cheap rune on another branch still gets power");
        Check(field.GetPowerUsed() == 1, "Blocked chain: only 1 power used");
    }

    void TestLoopTear()
    {
        //core - a - b - c - core loop. Tearing a-b keeps everything powered, then tearing c-core cuts b and c off
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 10);

        int a = Place(field, element, new Vector2(100, 0), false);
        int b = Place(field, element, new Vector2(100, 100), false);
        int c = Place(field, element, new Vector2(0, 100), false);
        field.Connect(RuneField.CoreID, a);
        field.Connect(a, b);
        field.Connect(b, c);
        field.Connect(c, RuneField.CoreID);

        Check(field.IsRunePowered(a) && field.IsRunePowered(b) && field.IsRunePowered(c), "Loop: all three runes powered");

        field.Disconnect(a, b);
        Check(field.IsRunePowered(a) && field.IsRunePowered(b) && field.IsRunePowered(c), "Loop: tearing a-b keeps everything powered (b still reaches the core through c)");

        field.Disconnect(c, RuneField.CoreID);
        Check(field.IsRunePowered(a) && field.IsRunePowered(b) == false && field.IsRunePowered(c) == false, "Loop: tearing c-core cuts b and c off");
        Check(field.GetPowerUsed() == 1, "Loop: power from the cut runes went back to the core");
    }

    void TestClampToBridges()
    {
        //a rune bridged to the core can't be dragged past its reach, and the bridge reports how stretched it is
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 10);

        int a = Place(field, element, new Vector2(100, 0));
        Vector2 clamped = field.ClampToBridges(a, new Vector2(400, 0));

        Check(Mathf.Abs(clamped.x - 150f) < 0.01f && Mathf.Abs(clamped.y) < 0.01f, "Clamp: dragging to 400 stops at the 150 reach");
        Check(Mathf.Abs(field.GetBridgeStretch(a, new Vector2(400, 0), RuneField.CoreID) - 250f) < 0.01f, "Clamp: the bridge is stretched 250 past its reach");
    }

    void TestNodeLockoutsAndUnlocks()
    {
        //node 0 and node 1 lock each other, node 2 needs node 0 on
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        int node0 = AddNode(nodes, new Vector2(100, 0));
        int node1 = AddNode(nodes, new Vector2(-100, 0));
        int node2 = AddNode(nodes, new Vector2(0, 100));
        nodes[node0]._Lockouts.Add(node1);
        nodes[node1]._Lockouts.Add(node0);
        nodes[node2]._Unlocks.Add(node0);

        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 10);

        int r0 = Place(field, element, new Vector2(100, 0));
        Check(field.IsNodeActive(node0), "Nodes: a powered rune turns node 0 on");

        int r1 = Place(field, element, new Vector2(-100, 0));
        Check(r1 == RuneField.NoRuneID && field.IsNodeActive(node1) == false, "Nodes: a rune can't go into node 1 while node 0 (which locks it) has a rune");

        Place(field, element, new Vector2(0, 100));
        Check(field.IsNodeActive(node2), "Nodes: node 2 turns on because node 0 is on");

        field.RemoveRune(r0);
        Check(field.IsNodeActive(node0) == false, "Nodes: taking the rune away turns node 0 off");
        Check(field.IsNodeActive(node2) == false, "Nodes: node 2 turns off without node 0");

        r1 = Place(field, element, new Vector2(-100, 0));
        Check(r1 != RuneField.NoRuneID && field.IsNodeActive(node1), "Nodes: once node 0 is empty, node 1 takes a rune and turns on");
        Check(field.IsNodeBlocked(node0), "Nodes: now node 0 is the locked one");
    }

    void TestMaxPowerAndRemoval()
    {
        //dropping the core to 0 power turns everything off, and removing a rune cuts off what was behind it
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        int node0 = AddNode(nodes, new Vector2(100, 0));
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 10);

        int a = Place(field, element, new Vector2(100, 0));
        field.TryPlugRune(a);
        int b = Place(field, element, new Vector2(200, 0));

        field.SetMaxPower(0);
        Check(field.GetPowerUsed() == 0 && field.IsNodeActive(node0) == false, "Max power: a 0 power core powers nothing and the node turns off");

        field.SetMaxPower(10);
        field.RemoveRune(a);
        Check(field.GetRune(a) == null && field.GetConnections(b).Count == 0, "Remove: the rune and its bridges are gone");
        Check(field.IsRunePowered(b) == false, "Remove: the rune behind it lost power");
    }

    void TestCloneAndCleanUp()
    {
        //editing a clone doesn't touch the original, and broken saved data gets cleaned up when a field is made
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 10);
        Place(field, element, new Vector2(100, 0));

        RuneFieldData copy = field.GetData().Clone();
        RuneField copyField = new RuneField(copy, settings, nodes, 10);
        Place(copyField, element, new Vector2(-100, 0));
        Check(field.GetData()._Runes.Count == 1 && copy._Runes.Count == 2, "Clone: adding to the copy doesn't change the original");

        //a bridge to a rune that doesn't exist, and a plug into a node that doesn't exist
        RuneFieldData broken = field.GetData().Clone();
        RuneBridgeEntry badBridge = new RuneBridgeEntry();
        badBridge._A = 0;
        badBridge._B = 99;
        broken._Bridges.Add(badBridge);
        NodePlugEntry badPlug = new NodePlugEntry();
        badPlug._NodeIndex = 5;
        badPlug._RuneID = 0;
        broken._Plugs.Add(badPlug);

        new RuneField(broken, settings, nodes, 10);
        Check(broken._Bridges.Count == 1 && broken._Plugs.Count == 0, "Clean up: bad bridges and plugs are removed when a field loads");
    }

    void TestSnapshots()
    {
        //a slot only saves snapshots of the nodes it plugged into. A field built from just those must work out the same as the full field
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        int node0 = AddNode(nodes, new Vector2(100, 0));
        int node1 = AddNode(nodes, new Vector2(-100, 0));
        AddNode(nodes, new Vector2(0, 100)); //node 2 never gets a rune
        nodes[node0]._Effects.Add(MakeStatChange(DamageType.StatType.Strength, 2));
        nodes[node1]._Unlocks.Add(node0);

        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 10);
        int a = Place(field, element, new Vector2(100, 0));
        field.TryPlugRune(a);
        int b = Place(field, element, new Vector2(-100, 0));
        field.TryPlugRune(b);

        List<AbilityNodeEntry> snapshots = field.GetPluggedNodeSnapshots();
        Check(snapshots.Count == 2, "Snapshots: only the 2 plugged nodes are saved, not the empty one");
        Check(snapshots[0] != nodes[node0], "Snapshots: they're copies, not the scene's nodes");

        RuneField fromSnapshots = new RuneField(field.GetData().Clone(), settings, snapshots, 10);
        Check(fromSnapshots.IsNodeActive(node0) && fromSnapshots.IsNodeActive(node1), "Snapshots: both nodes are still on without the full node list");
        Check(fromSnapshots.GetStatTotals().ContainsKey(DamageType.StatType.Strength), "Snapshots: node effects still compile from the snapshots");

        fromSnapshots.SetMaxPower(1); //a smaller core: only a (placed first) gets power, so b goes dark and node 1 turns off
        Check(fromSnapshots.IsNodeActive(node0) && fromSnapshots.IsNodeActive(node1) == false, "Snapshots: the rules still re-run (less power turns node 1 off)");
    }

    void TestCompileEffects()
    {
        //node 0 is on (powered rune), node 1 has a rune but no power. Only powered runes and active nodes compile, and broken effects are skipped
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        int node0 = AddNode(nodes, new Vector2(100, 0));
        int node1 = AddNode(nodes, new Vector2(600, 0)); //too far from the core to get power
        StatChangeEffect nodeStrength = MakeStatChange(DamageType.StatType.Strength, 2);
        nodes[node0]._Effects.Add(nodeStrength);
        nodes[node0]._Effects.Add(new GrantAbilityEffect()); //no ability assigned, should be skipped
        nodes[node0]._Effects.Add(null); //empty entry, should be skipped
        nodes[node1]._Effects.Add(MakeStatChange(DamageType.StatType.Defense, 5));

        ElementItemSO element = MakeElement(1, 2, 150f);
        element.GetStatBoosts().Add(MakeStatChange(DamageType.StatType.Health, 3));
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 10);

        int powered = Place(field, element, new Vector2(100, 0));
        field.TryPlugRune(powered);
        int dark = Place(field, element, new Vector2(600, 0));
        field.TryPlugRune(dark);

        List<RuneEffect> compiled = field.CompileEffects();
        Dictionary<DamageType.StatType, int> totals = field.GetStatTotals();

        Check(compiled.Count == 2, "Compile: 1 powered rune boost + 1 working node effect (broken and empty effects skipped)");
        Check(totals.ContainsKey(DamageType.StatType.Health) && totals[DamageType.StatType.Health] == 3, "Compile: only the powered rune's Health +3 counts, not the dark rune's");
        Check(totals.ContainsKey(DamageType.StatType.Strength) && totals[DamageType.StatType.Strength] == 2, "Compile: the active node's Strength +2 counts");
        Check(totals.ContainsKey(DamageType.StatType.Defense) == false, "Compile: the node without power adds nothing");
        Check(compiled.Contains(nodeStrength) == false, "Compile: the list holds copies, not the node's own effect");

        //changing the compiled copy must not change the node
        for (int i = 0; i < compiled.Count; i++)
        {
            StatChangeEffect statChange = compiled[i] as StatChangeEffect;
            if (statChange != null) statChange._Amount = 99;
        }
        Check(nodeStrength._Amount == 2 && element.GetStatBoosts()[0]._Amount == 3, "Compile: changing the compiled list doesn't change the node or the rune asset");
    }

    void TestGates()
    {
        //a node is a gate only if it has an Evolve effect
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        int gate = AddNode(nodes, new Vector2(100, 0));
        int normal = AddNode(nodes, new Vector2(-100, 0));
        nodes[gate]._Effects.Add(new EvolveEffect());
        nodes[normal]._Effects.Add(MakeStatChange(DamageType.StatType.Agility, 1));

        Check(nodes[gate].IsGate(), "Gates: a node with an Evolve effect is a gate");
        Check(nodes[normal].IsGate() == false, "Gates: a node without one isn't");
    }

    void TestCountRunes()
    {
        //the save flow counts runes per type to work out the inventory difference
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO fire = MakeElement(1, 2, 150f);
        ElementItemSO water = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 10);

        Place(field, fire, new Vector2(100, 0));
        Place(field, fire, new Vector2(-100, 0));
        int waterRune = Place(field, water, new Vector2(0, 100));

        Dictionary<ElementItemSO, int> counts = field.GetData().CountRunes();
        Check(counts[fire] == 2 && counts[water] == 1 && counts.Count == 2, "Count: 2 of one rune type and 1 of another");

        field.RemoveRune(waterRune);
        Check(field.GetData().CountRunes(water) == 0 && field.GetData().CountRunes().ContainsKey(water) == false, "Count: a removed rune isn't counted");
    }

    void TestFootprints()
    {
        //runes can't overlap each other, the core or a node. Landing near an empty node snaps into it
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        int node0 = AddNode(nodes, new Vector2(0, -200));
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 10);

        int first = Place(field, element, new Vector2(100, 0));
        Check(first != RuneField.NoRuneID, "Footprints: a rune on a clear spot is placed");
        Check(Place(field, element, new Vector2(150, 0)) == RuneField.NoRuneID, "Footprints: a rune 50 from another (rune size 100) is refused");
        Check(Place(field, element, new Vector2(200, 0)) != RuneField.NoRuneID, "Footprints: a rune exactly one rune size away is allowed");
        Check(Place(field, element, new Vector2(-50, 0)) == RuneField.NoRuneID, "Footprints: a rune overlapping the core is refused");

        int snapped = Place(field, element, new Vector2(0, -160));
        Check(snapped != RuneField.NoRuneID && field.GetPluggedNode(snapped) == node0 && field.GetPosition(snapped) == new Vector2(0, -200), "Footprints: landing near an empty node snaps to its center and plugs in");
        Check(Place(field, element, new Vector2(0, -140)) == RuneField.NoRuneID, "Footprints: a rune next to a filled node is refused");
    }

    void TestDropRefused()
    {
        //a drag that ends on a bad spot changes nothing: the rune keeps its spot and its node
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        int node0 = AddNode(nodes, new Vector2(120, 0));
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 10);

        int plugged = Place(field, element, new Vector2(120, 0));
        int other = Place(field, element, new Vector2(-120, 0));

        Check(field.TryDropRune(plugged, new Vector2(-110, 20)) == false, "Drop: dropping onto another rune is refused");
        Check(field.GetPosition(plugged) == new Vector2(120, 0) && field.GetPluggedNode(plugged) == node0 && field.IsNodeActive(node0), "Drop: the refused rune stays in its node and the node stays on");

        Check(field.TryDropRune(other, new Vector2(0, 150)), "Drop: dropping on a clear spot works");
        Check(field.GetPosition(other) == new Vector2(0, 150), "Drop: the rune moved there");
    }

    void TestSnapStretch()
    {
        //snapping may stretch a bridge up to Snap Stretch (30) past its reach, and the extra length lasts while the rune sits in the node
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        int nearNode = AddNode(nodes, new Vector2(0, 170)); //20 past the core's 150 reach
        int farNode = AddNode(nodes, new Vector2(-230, 0)); //80 past
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 10);

        int rune = Place(field, element, new Vector2(100, 0));
        Check(field.AreConnected(rune, RuneField.CoreID), "Snap stretch: the rune is bridged to the core");
        Check(field.CanSnapToNode(rune, farNode) == false, "Snap stretch: a node 80 past reach is too far to snap into");

        Vector2 dropSpot = field.ClampToBridges(rune, new Vector2(0, 170)); //what dragging toward the node gives: stopped at the 150 reach
        Check(field.TryDropRune(rune, dropSpot) && field.GetPluggedNode(rune) == nearNode, "Snap stretch: a node 20 past reach still snaps");

        float length = Vector2.Distance(field.GetPosition(rune), Vector2.zero);
        Check(length <= field.GetBridgeReach(rune, RuneField.CoreID), "Snap stretch: while plugged, the 170 long bridge doesn't count as stretched");
        Check(field.GetBridgeStretch(rune, field.GetPosition(rune), RuneField.CoreID) > 0f, "Snap stretch: dragging it out of the node uses normal reach again");
    }

    void TestRestoreBridges()
    {
        //bridges torn during a refused drag are put back
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 10);

        int rune = Place(field, element, new Vector2(100, 0));
        List<int> bridgesBefore = field.GetConnections(rune);
        field.Disconnect(rune, RuneField.CoreID); //torn while dragging
        Check(field.IsRunePowered(rune) == false, "Restore: tearing the bridge cuts its power");

        field.RestoreBridges(rune, bridgesBefore);
        Check(field.AreConnected(rune, RuneField.CoreID) && field.IsRunePowered(rune), "Restore: the bridge and its power are back");
    }

    void TestZoneMath()
    {
        //which zone a distance is in, which ring it sits on, and which ring is next out. Zones are 300 wide, 4 of them
        RuneFieldSettingsSO settings = MakeZoneSettings();

        Check(settings.GetZone(0f) == 1 && settings.GetZone(150f) == 1, "Zone math: the core and the middle of zone 1 are zone 1");
        Check(settings.GetZone(300f) == 1 && settings.GetZone(300.5f) == 1, "Zone math: a spot on ring 1 (or a hair past it) counts as zone 1, so gates there belong to zone 1");
        Check(settings.GetZone(302f) == 2 && settings.GetZone(900f) == 3, "Zone math: past ring 1 is zone 2, ring 3 is zone 3");
        Check(settings.GetZone(5000f) == 4, "Zone math: anything past the edge counts as the last zone");

        Check(settings.IsOnRing(600f, out int ring) && ring == 2, "Zone math: 600 is on ring 2");
        Check(settings.IsOnRing(299.5f, out ring) && ring == 1, "Zone math: 299.5 is close enough to count as on ring 1");
        Check(settings.IsOnRing(450f, out ring) == false, "Zone math: 450 isn't on a ring");

        Check(settings.GetNextRingOut(0f) == 1 && settings.GetNextRingOut(200f) == 1, "Next ring: inside zone 1 the next ring out is ring 1");
        Check(settings.GetNextRingOut(300f) == 2 && settings.GetNextRingOut(299.5f) == 2, "Next ring: a node already on ring 1 goes up to ring 2");
        Check(settings.GetLastGateRing() == 3 && Mathf.Approximately(settings.GetFieldEdge(), 1200f), "Zone math: gates go on rings 1-3, ring 4 (1200) is the edge");
    }

    void TestZonePlacement()
    {
        //runes only go in the open zone (rank + 1). Further out is locked, further in is frozen, past the edge is off the field
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 10, 0);
        Check(CanDrop(bound, new Vector2(200, 0)) && CanDrop(bound, new Vector2(400, 0)) == false, "Zone placement: a Bound shade can use zone 1 but not zone 2");

        RuneField unbound = new RuneField(new RuneFieldData(), settings, nodes, 10, 1);
        Check(CanDrop(unbound, new Vector2(200, 0)) == false && CanDrop(unbound, new Vector2(400, 0)), "Zone placement: after evolving once, zone 1 is closed and zone 2 is open");

        RuneField legend = new RuneField(new RuneFieldData(), settings, nodes, 10, 3);
        Check(CanDrop(legend, new Vector2(1150, 0)) && CanDrop(legend, new Vector2(1250, 0)) == false, "Zone placement: the last zone stops at the outer ring");
    }

    void TestFrozen()
    {
        //runes in a zone the shade evolved out of can't move, be removed, or lose their bridges. New runes can still bridge to them
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO element = MakeElement(1, 2, 150f);

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 10, 0);
        int a = Place(bound, element, new Vector2(100, 0));
        int b = Place(bound, element, new Vector2(220, 0));
        Check(bound.IsRuneFrozen(a) == false, "Frozen: nothing is frozen on a Bound shade");

        RuneField field = new RuneField(bound.GetData().Clone(), settings, nodes, 10, 1);
        Check(field.IsRuneFrozen(a) && field.IsRuneFrozen(b), "Frozen: after evolving, the zone 1 runes are frozen");
        Check(field.TryDropRune(a, new Vector2(150, 80)) == false && field.GetPosition(a) == new Vector2(100, 0), "Frozen: a frozen rune can't be moved");
        Check(field.RemoveRune(b) == false && field.GetRune(b) != null, "Frozen: a frozen rune can't be removed");

        field.Disconnect(a, RuneField.CoreID);
        Check(field.AreConnected(a, RuneField.CoreID) && field.IsBridgeFrozen(a, RuneField.CoreID), "Frozen: a frozen bridge can't tear");

        int c = Place(field, element, new Vector2(360, 0));
        Check(field.AreConnected(b, c), "Frozen: a new zone 2 rune can still bridge to a frozen rune with a free slot");
        Check(field.IsBridgeFrozen(b, c) == false, "Frozen: a bridge to a new rune isn't frozen");

        field.Disconnect(b, c);
        Check(field.AreConnected(b, c) == false, "Frozen: the new bridge can still tear");
    }

    void TestFrozenPowerFirst()
    {
        //core - a - b is frozen. A new rune bridged straight to the core would normally get power before b (fewer bridges), but frozen runes go first
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO element = MakeElement(1, 2, 150f);
        ElementItemSO longReach = MakeElement(1, 2, 400f);

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 10, 0);
        int a = Place(bound, element, new Vector2(100, 0));
        int b = Place(bound, element, new Vector2(220, 0));

        RuneField field = new RuneField(bound.GetData().Clone(), settings, nodes, 2, 1); //only 2 power: enough for 2 of the 3 runes
        int c = Place(field, longReach, new Vector2(0, 350));
        Check(field.AreConnected(c, RuneField.CoreID), "Frozen power: the new rune is bridged straight to the core");
        Check(field.IsRunePowered(a) && field.IsRunePowered(b), "Frozen power: the frozen chain keeps its power");
        Check(field.IsRunePowered(c) == false && field.GetPowerUsed() == 2, "Frozen power: the new rune waits, even though it's closer to the core than b");
    }

    void TestPlugLockouts()
    {
        //lockouts trigger on plugging (not power), get fixed to two-way, and a rune can move straight from one rival node to the other
        RuneFieldSettingsSO settings = MakeSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        int node0 = AddNode(nodes, new Vector2(100, 0));
        int node1 = AddNode(nodes, new Vector2(-100, 0));
        nodes[node0]._Lockouts.Add(node1); //one-way on purpose

        string problems = RuneField.FixOneWayLockouts(nodes);
        Check(problems != "" && nodes[node1]._Lockouts.Contains(node0), "Lockouts: a one-way lockout is reported and filled in both ways");
        Check(RuneField.FixOneWayLockouts(nodes) == "", "Lockouts: once fixed, there's nothing left to report");

        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), settings, nodes, 0); //no power at all
        int r0 = Place(field, element, new Vector2(100, 0));
        Check(field.GetPluggedNode(r0) == node0 && field.IsNodeActive(node0) == false, "Lockouts: the rune is plugged into node 0 but has no power");
        Check(Place(field, element, new Vector2(-100, 0)) == RuneField.NoRuneID, "Lockouts: node 1 is locked by the plug alone, no power needed");

        Check(field.TryDropRune(r0, new Vector2(-100, 0)) && field.GetPluggedNode(r0) == node1, "Lockouts: the plugged rune can move straight over to the rival node");
    }

    void TestGateZones()
    {
        //gates in the same zone lock each other, only gates in the open zone can evolve, and the taken gate stays on after evolving
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        int gate0 = AddGate(nodes, new Vector2(300, 0)); //ring 1
        int gate1 = AddGate(nodes, new Vector2(0, 300)); //ring 1, the rival branch
        int gate2 = AddGate(nodes, new Vector2(600, 0)); //ring 2
        ElementItemSO longReach = MakeElement(1, 2, 400f);

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 10, 0);
        Check(bound.GetPluggedGate() == -1 && bound.GetEvolvingGate() == -1, "Gates: an empty field has no gate to evolve through");

        Place(bound, longReach, new Vector2(300, 0));
        Check(bound.GetPluggedGate() == gate0 && bound.GetEvolvingGate() == gate0, "Gates: a powered rune in a ring 1 gate is ready to evolve");
        Check(Place(bound, longReach, new Vector2(0, 300)) == RuneField.NoRuneID && bound.IsNodeBlocked(gate1), "Gates: the other ring 1 gate is locked out automatically");
        Check(bound.IsNodeBlocked(gate2), "Gates: the ring 2 gate is in a locked zone");

        RuneField evolved = new RuneField(bound.GetData().Clone(), settings, nodes, 10, 1);
        Check(evolved.GetEvolvingGate() == -1 && evolved.IsNodeActive(gate0), "Gates: after evolving, the taken gate stays on but can't evolve again");
        Check(evolved.IsNodeBlocked(gate1) && evolved.IsNodeBlocked(gate2) == false, "Gates: the rival gate is closed for good and the ring 2 gate opens");

        Place(evolved, longReach, new Vector2(600, 0));
        Check(evolved.GetEvolvingGate() == gate2, "Gates: a powered rune in the ring 2 gate is ready for the next evolve");
    }

    void TestSetupChecks()
    {
        //gates have to sit on a gate ring. Normal nodes can go anywhere
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        nodes[AddGate(nodes, new Vector2(0, 600))]._Name = "GoodGate";
        nodes[AddGate(nodes, new Vector2(0, 450))]._Name = "OffRingGate";
        nodes[AddGate(nodes, new Vector2(1200, 0))]._Name = "EdgeGate";
        nodes[AddNode(nodes, new Vector2(50, 450))]._Name = "NormalNode";

        string problems = RuneField.CheckGates(nodes, settings);
        Check(problems.Contains("OffRingGate") && problems.Contains("EdgeGate"), "Setup checks: a gate off a ring and a gate on the outer edge are reported");
        Check(problems.Contains("GoodGate") == false && problems.Contains("NormalNode") == false, "Setup checks: a gate on ring 2 and a normal node are fine");
    }
    #endregion

    #region Tools
    RuneFieldSettingsSO MakeSettings()
    {
        //makes a temporary settings asset with a 150 core reach and a 70 snap radius
        //zones are 1000 wide, so the older tests (all within 600 of the core) stay in zone 1. The zone tests change it to 300
        RuneFieldSettingsSO settings = ScriptableObject.CreateInstance<RuneFieldSettingsSO>();
        settings._CoreReach = 150f;
        settings._CoreMaxBridges = 0;
        settings._ZoneCount = 4;
        settings._ZoneWidth = 1000f;
        settings._EdgeBleed = 100f;
        settings._NodeSnapRadius = 70f;
        settings._RuneSize = 100f;
        settings._CoreSize = 100f;
        settings._SnapStretch = 30f;
        _madeAssets.Add(settings);
        return settings;
    }

    RuneFieldSettingsSO MakeZoneSettings()
    {
        //same as MakeSettings, with 300 wide zones (rings at 300, 600, 900, edge at 1200) for the zone tests
        RuneFieldSettingsSO settings = MakeSettings();
        settings._ZoneWidth = 300f;
        return settings;
    }

    int AddGate(List<AbilityNodeEntry> nodes, Vector2 position)
    {
        //adds a node with an Evolve effect (an evolution gate). No form is set, the tests only need it to be a gate
        int index = AddNode(nodes, position);
        nodes[index]._Effects.Add(new EvolveEffect());
        return index;
    }

    bool CanDrop(RuneField field, Vector2 position)
    {
        //checks if a new rune could be placed here, without placing it
        return field.CanDropAt(RuneField.NoRuneID, position, out Vector2 finalPosition, out int nodeIndex);
    }

    int AddNode(List<AbilityNodeEntry> nodes, Vector2 position)
    {
        //adds an ability node to a temporary node list and returns its index
        AbilityNodeEntry node = new AbilityNodeEntry();
        node._NodeIndex = nodes.Count;
        node._Position = position;
        nodes.Add(node);
        return node._NodeIndex;
    }

    ElementItemSO MakeElement(int powerNeeded, int maxBridges, float reach)
    {
        //makes a temporary element rune type
        ElementItemSO element = ScriptableObject.CreateInstance<ElementItemSO>();
        element.powerNeeded = powerNeeded;
        element.connectionsAllowed = maxBridges;
        element.connectionDistance = reach;
        _madeAssets.Add(element);
        return element;
    }

    StatChangeEffect MakeStatChange(DamageType.StatType stat, int amount)
    {
        //makes a stat change effect for a test node or rune
        StatChangeEffect effect = new StatChangeEffect();
        effect._Stat = stat;
        effect._Amount = amount;
        return effect;
    }

    int Place(RuneField field, ElementItemSO element, Vector2 position, bool connect = true)
    {
        //places a rune and (by default) bridges it like a player dropping it. Returns its ID
        field.TryPlaceRune(element, position, out int runeID);
        if (connect) field.ConnectNearby(runeID);
        return runeID;
    }

    void Check(bool passed, string description)
    {
        //records one check and logs it if it failed
        if (passed)
        {
            _passed++;
            if (_LogPasses) Debug.Log($"Passed: {description}", this);
        }
        else
        {
            _failed++;
            Debug.LogError($"Error! Rune field test failed: {description}", this);
        }
    }

    void CleanUp()
    {
        //destroys the temporary elements and settings assets the tests made
        for (int i = 0; i < _madeAssets.Count; i++)
        {
            if (_madeAssets[i] != null) DestroyImmediate(_madeAssets[i]);
        }
        _madeAssets.Clear();
    }
    #endregion
}
