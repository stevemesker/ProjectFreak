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
        TestFreezeOnEvolve();
        TestFrozenLeash();
        TestLockedIn();
        TestFrozenPastOpenZone();
        TestFrozenChainKeepsPower();
        TestFrozenPowerOrder();
        TestOldSlotFrozenLists();
        TestFrozenListsCleanUp();
        TestPlanningRunes();
        TestOldZoneRunes();
        TestCutOnSave();
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
        Check(field.HasRunesWaitingForPower(), "Power priority: the field knows a rune is waiting for power (for the core's look)");
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
        //runes go in any zone inside the field: closed zones take normal runes again, locked zones take planning runes. Past the edge is off the field
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 10, 0);
        Check(CanDrop(bound, new Vector2(200, 0)) && CanDrop(bound, new Vector2(400, 0)), "Zone placement: a Bound shade can use zone 1, and zone 2 for planning runes");
        Check(bound.IsPastOpenZone(new Vector2(400, 0)) && bound.IsPastOpenZone(new Vector2(300, 0)) == false, "Zone placement: zone 2 is past a Bound shade's open zone, the ring itself isn't");

        RuneField unbound = new RuneField(new RuneFieldData(), settings, nodes, 10, 1);
        Check(CanDrop(unbound, new Vector2(200, 0)) && CanDrop(unbound, new Vector2(400, 0)), "Zone placement: after evolving once, the closed zone 1 and the open zone 2 both take runes");

        RuneField legend = new RuneField(new RuneFieldData(), settings, nodes, 10, 3);
        Check(CanDrop(legend, new Vector2(1150, 0)) && CanDrop(legend, new Vector2(1250, 0)) == false, "Zone placement: the last zone stops at the outer ring");
    }

    void TestFrozen()
    {
        //runes in a zone the shade evolved out of can be moved, but can't be removed or lose their bridges. New runes can still bridge to them
        //(this builds the evolved field straight from the Bound data, so the frozen lists come from the old-slot fill-in, see TestOldSlotFrozenLists)
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO element = MakeElement(1, 2, 150f);

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 10, 0);
        int a = Place(bound, element, new Vector2(100, 0));
        int b = Place(bound, element, new Vector2(220, 0));
        Check(bound.IsRuneFrozen(a) == false, "Frozen: nothing is frozen on a Bound shade");

        RuneField field = new RuneField(bound.GetData().Clone(), settings, nodes, 10, 1);
        Check(field.IsRuneFrozen(a) && field.IsRuneFrozen(b), "Frozen: after evolving, the zone 1 runes are frozen");
        Check(field.TryDropRune(a, new Vector2(100, 60)) && field.GetPosition(a) == new Vector2(100, 60), "Frozen: a frozen rune can be moved inside its closed zone");
        Check(field.IsRuneFrozen(a), "Frozen: moving a frozen rune doesn't thaw it");
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

    void TestFreezeOnEvolve()
    {
        //evolving freezes the zone it left: its runes, every bridge with both ends frozen at that moment, and frozen runes in nodes (locked in)
        //a bridge made later between two frozen runes stays normal until the next evolve
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        int node = AddNode(nodes, new Vector2(450, 0)); //zone 2
        ElementItemSO element = MakeElement(1, 2, 150f);
        ElementItemSO longReach = MakeElement(1, 2, 400f);

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 10, 0);
        int a = Place(bound, element, new Vector2(100, 0));
        int e = Place(bound, element, new Vector2(0, 100)); //bridges to the core and to a
        bound.Disconnect(a, e); //torn, so a and e aren't bridged when the shade evolves

        RuneField unbound = new RuneField(bound.GetData().Clone(), settings, nodes, 10, 1);
        unbound.FreezeClosedZones();
        Check(unbound.IsRuneFrozen(a) && unbound.IsRuneFrozen(e), "Freeze: evolving freezes the zone 1 runes");
        Check(unbound.IsBridgeFrozen(a, RuneField.CoreID) && unbound.IsBridgeFrozen(e, RuneField.CoreID), "Freeze: their bridges to the core freeze");

        Check(unbound.Connect(a, e) && unbound.IsBridgeFrozen(a, e) == false, "Freeze: a new bridge between two frozen runes stays normal");

        int c = Place(unbound, longReach, new Vector2(360, -100)); //zone 2, bridges to the core
        int p = Place(unbound, longReach, new Vector2(450, 0));    //zone 2, snaps into the node and bridges to c
        Check(unbound.GetPluggedNode(p) == node && unbound.IsRuneFrozen(p) == false && unbound.IsRuneLockedIn(p) == false, "Freeze: a zone 2 rune in a node is a normal rune before the next evolve");

        RuneField ascendant = new RuneField(unbound.GetData().Clone(), settings, nodes, 10, 2);
        Check(ascendant.IsRuneFrozen(c) == false, "Freeze: a field that already has frozen lists isn't filled in from positions");

        ascendant.FreezeClosedZones();
        Check(ascendant.IsRuneFrozen(c) && ascendant.IsRuneFrozen(p), "Freeze: evolving again freezes the zone 2 runes");
        Check(ascendant.IsBridgeFrozen(a, e), "Freeze: the newer bridge between the zone 1 runes freezes on the next evolve");
        Check(ascendant.IsBridgeFrozen(c, RuneField.CoreID) && ascendant.IsBridgeFrozen(p, c), "Freeze: the zone 2 bridges freeze");
        Check(ascendant.IsRuneLockedIn(p) && ascendant.IsRuneLockedIn(c) == false, "Freeze: the frozen rune in a node is locked in, the one outside a node isn't");
    }

    void TestFrozenLeash()
    {
        //a frozen bridge never tears, so it's a hard leash: a frozen rune can go anywhere its frozen bridges reach, and no further
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO longReach = MakeElement(1, 2, 400f);

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 10, 0);
        int a = Place(bound, longReach, new Vector2(100, 0));
        RuneField field = new RuneField(bound.GetData().Clone(), settings, nodes, 10, 1);

        Check(field.TryDropRune(a, new Vector2(350, 0)) && field.IsRuneFrozen(a), "Leash: a frozen rune can be moved into the open zone and stays frozen");
        Check(field.TryDropRune(a, new Vector2(0, -450)) == false && field.GetPosition(a) == new Vector2(350, 0), "Leash: dropping it past its frozen bridge's reach is refused");

        Vector2 clamped = field.ClampToBridges(a, new Vector2(0, -450)); //what dragging there gives: stopped at the 400 reach
        Check(field.TryDropRune(a, clamped), "Leash: dropping it right at the end of the leash works");
        Check(field.GetBridgeStretch(a, new Vector2(0, -450), RuneField.CoreID) == 0f, "Leash: pulling past the leash never stretches (tears) a frozen bridge");

        field.Disconnect(a, RuneField.CoreID);
        Check(field.AreConnected(a, RuneField.CoreID), "Leash: the frozen bridge is still there after being moved");
    }

    void TestLockedIn()
    {
        //rule A: a locked-in rune never moves or unplugs. A frozen rune plugged in during the draft can still come out until the field is saved
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        int zone1Node = AddNode(nodes, new Vector2(200, 0));
        int zone2Node = AddNode(nodes, new Vector2(0, 350));
        ElementItemSO longReach = MakeElement(1, 2, 400f);

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 10, 0);
        int r = Place(bound, longReach, new Vector2(200, 0));  //plugged into the zone 1 node
        int q = Place(bound, longReach, new Vector2(-250, 0)); //not in a node
        RuneField field = new RuneField(bound.GetData().Clone(), settings, nodes, 10, 1);

        Check(field.IsRuneLockedIn(r) && field.IsRuneLockedIn(q) == false, "Locked in: after evolving, the frozen rune in a node is locked in");
        Check(field.TryDropRune(r, new Vector2(200, 120)) == false && field.GetPosition(r) == new Vector2(200, 0), "Locked in: a locked-in rune can't be moved");

        field.UnplugRune(r);
        Check(field.GetPluggedNode(r) == zone1Node, "Locked in: a locked-in rune can't be unplugged");
        Check(field.RemoveRune(r) == false, "Locked in: a locked-in rune can't be removed");

        Check(field.TryDropRune(q, new Vector2(0, 350)) && field.GetPluggedNode(q) == zone2Node, "Locked in: a frozen rune can be plugged into an open zone node in the draft");
        List<int> toLock = field.GetFrozenRunesToLockIn();
        Check(field.IsRuneLockedIn(q) == false && toLock.Count == 1 && toLock.Contains(q), "Locked in: it isn't locked in yet, and it's the one rune waiting to be");

        field.UnplugRune(q);
        Check(field.GetPluggedNode(q) == -1, "Locked in: before saving, it can still come back out");

        field.TryDropRune(q, new Vector2(0, 350));
        field.LockInFrozenPlugs(); //what saving does
        Check(field.IsRuneLockedIn(q) && field.TryDropRune(q, new Vector2(-250, 0)) == false, "Locked in: once saved, it's locked in and can't move");
    }

    void TestFrozenPastOpenZone()
    {
        //a frozen rune can be moved past the open zone and keeps its power there, but can't plug into a node out there
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        int zone3Node = AddNode(nodes, new Vector2(700, 0));
        ElementItemSO veryLongReach = MakeElement(1, 2, 700f);

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 10, 0);
        int f = Place(bound, veryLongReach, new Vector2(100, 0));
        RuneField field = new RuneField(bound.GetData().Clone(), settings, nodes, 10, 1);

        Check(field.TryDropRune(f, new Vector2(650, 100)) && field.GetZone(field.GetPosition(f)) > field.GetOpenZone(), "Past open zone: a frozen rune can be moved into a locked zone");
        Check(field.IsRunePowered(f), "Past open zone: it keeps its power out there");
        Check(field.TryDropRune(f, new Vector2(700, 0)) == false && field.GetPluggedNode(f) == -1 && field.GetRuneInNode(zone3Node) == -1, "Past open zone: it can't plug into a node out there");

        ElementItemSO element = MakeElement(1, 2, 150f);
        int p = Place(field, element, new Vector2(700, 0)); //a planning rune, bridges to f (f's 700 reach covers it)
        Check(field.GetPluggedNode(p) == zone3Node, "Past open zone: a normal (planning) rune can plug into that node, only frozen runes can't");
        Check(field.AreConnected(p, f) && field.IsRunePowered(p) == false && field.IsNodeActive(zone3Node) == false, "Past open zone: the frozen rune doesn't power the planning rune bridged to it, so the node stays off");
    }

    void TestPlanningRunes()
    {
        //runes past the open zone are planning runes: they never get power, can snap into nodes (which stay off), and their plugs still lock out rivals
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        int futureNode = AddNode(nodes, new Vector2(450, 0)); //zone 2
        int gateA = AddGate(nodes, new Vector2(600, 0));      //ring 2
        int gateB = AddGate(nodes, new Vector2(0, 600));      //ring 2, gateA's rival
        ElementItemSO element = MakeElement(1, 2, 150f);
        ElementItemSO longReach = MakeElement(1, 2, 400f);
        ElementItemSO longReach3 = MakeElement(1, 3, 400f);

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 10, 0);
        int a = Place(bound, element, new Vector2(100, 0));      //zone 1, on the core
        int p1 = Place(bound, longReach3, new Vector2(350, 0)); //zone 2, bridges to a and straight to the core
        int p2 = Place(bound, longReach, new Vector2(450, 0));  //zone 2, snaps into the node, bridges to p1
        int p3 = Place(bound, longReach, new Vector2(600, 0));  //ring 2, snaps into gateA, bridges to p2

        Check(p1 != RuneField.NoRuneID && bound.AreConnected(p1, RuneField.CoreID) && bound.AreConnected(p1, a), "Planning: a rune can be placed past the open zone and bridges like any rune");
        Check(bound.IsRunePowered(a) && bound.IsRunePowered(p1) == false && bound.IsRunePowered(p2) == false && bound.IsRunePowered(p3) == false, "Planning: planning runes never get power, even bridged straight to the core");
        Check(bound.GetPowerUsed() == 1, "Planning: only the zone 1 rune uses power");
        Check(bound.HasRunesWaitingForPower() == false, "Planning: planning runes don't count as waiting for power");
        Check(bound.GetPluggedNode(p2) == futureNode && bound.IsNodeActive(futureNode) == false, "Planning: a planning rune snaps into a node past the open zone, but the node stays off");

        Check(bound.GetPluggedNode(p3) == gateA && bound.IsNodeLockedOut(gateB) && CanDrop(bound, new Vector2(0, 600)) == false, "Planning: a planning rune in a future gate locks out its rival");
        Check(bound.GetPluggedGate() == -1 && bound.GetEvolvingGate() == -1, "Planning: a future gate is never the gate to evolve through");

        bound.RemoveRune(p3);
        Check(bound.IsNodeLockedOut(gateB) == false, "Planning: taking the planning rune out frees the rival gate");
    }

    void TestOldZoneRunes()
    {
        //after evolving, the closed zone takes normal runes again (and its ability nodes work again), but its gates stay closed
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        int gate0 = AddGate(nodes, new Vector2(300, 0)); //ring 1
        int gate1 = AddGate(nodes, new Vector2(0, 300)); //ring 1, the rival branch
        int oldNode = AddNode(nodes, new Vector2(-200, 0)); //zone 1
        ElementItemSO element = MakeElement(1, 2, 150f);
        ElementItemSO longReach = MakeElement(1, 2, 400f);

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 10, 0);
        Place(bound, longReach, new Vector2(300, 0)); //the gate the shade evolves through
        RuneField evolved = new RuneField(bound.GetData().Clone(), settings, nodes, 10, 1);

        int n = Place(evolved, longReach, new Vector2(-200, 0));
        Check(n != RuneField.NoRuneID && evolved.GetPluggedNode(n) == oldNode && evolved.IsNodeActive(oldNode), "Old zone: an ability node in the closed zone takes a rune again and turns on");
        Check(evolved.IsRuneFrozen(n) == false, "Old zone: a rune placed there is a normal rune");

        int m = Place(evolved, element, new Vector2(0, -150));
        Check(m != RuneField.NoRuneID && evolved.IsRunePowered(m), "Old zone: a rune can go on a free spot in the closed zone and gets power");
        Check(evolved.RemoveRune(m), "Old zone: it can be taken off again (it isn't frozen)");

        Check(CanDrop(evolved, new Vector2(0, 300)) == false && evolved.IsNodeBlocked(gate1), "Old zone: the rival gate stays closed, locked out by the taken gate's locked-in rune");
        Check(evolved.IsNodeActive(gate0) && evolved.GetEvolvingGate() == -1, "Old zone: the taken gate stays on but can't evolve again");
    }

    void TestCutOnSave()
    {
        //saving cuts every normal bridge crossing the open zone's outer ring. Frozen bridges and bridges between two planning runes stay
        //crossing bridges never carry power across the ring, even from a frozen rune, so cutting them doesn't change power
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO element = MakeElement(1, 2, 150f);
        ElementItemSO longReach = MakeElement(1, 2, 400f);
        ElementItemSO longReach3 = MakeElement(1, 3, 400f);
        ElementItemSO veryLongReach3 = MakeElement(1, 3, 700f);

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 10, 0);
        int f = Place(bound, veryLongReach3, new Vector2(100, 0));
        RuneField field = new RuneField(bound.GetData().Clone(), settings, nodes, 10, 1); //open zone 2, its outer ring is at 600
        field.TryDropRune(f, new Vector2(650, 0)); //the frozen rune moves past the ring. Its frozen bridge to the core now crosses it

        //bridged by hand, so each bridge is exactly the one being tested
        int n = Place(field, longReach3, new Vector2(350, 0), false);  //open zone
        int p = Place(field, longReach, new Vector2(700, 150), false); //past the ring
        int q = Place(field, longReach, new Vector2(750, 300), false); //past the ring
        int m = Place(field, element, new Vector2(560, -120), false);  //open zone, just inside the ring
        field.Connect(n, RuneField.CoreID);
        field.Connect(n, p); //crosses the ring
        field.Connect(p, q); //both past the ring
        field.Connect(f, m); //crosses the ring, from the frozen rune

        Check(field.GetZone(field.GetPosition(f)) == 3 && field.IsRunePowered(f) && field.IsRunePowered(n), "Cut: the frozen rune past the ring and the open zone rune have power");
        Check(field.AreConnected(f, m) && field.IsRunePowered(m) == false, "Cut: a bridge crossing the ring doesn't carry power across, even from a frozen rune");
        Check(field.IsRunePowered(p) == false && field.IsRunePowered(q) == false, "Cut: planning runes stay dark, even bridged to a powered rune");
        Check(field.GetBridgesToCut().Count == 2, "Cut: two bridges are waiting to be cut");

        List<RuneBridgeEntry> reported = null;
        field.OnBridgesCut += (cutBridges) => reported = cutBridges; //a small inline function that keeps the list the event sends
        List<RuneBridgeEntry> cut = field.CutOpenRingBridges();

        Check(cut.Count == 2 && field.AreConnected(n, p) == false && field.AreConnected(f, m) == false, "Cut: both normal bridges crossing the ring are cut");
        Check(field.AreConnected(f, RuneField.CoreID), "Cut: the frozen bridge crossing the ring stays");
        Check(field.AreConnected(p, q), "Cut: the bridge between two planning runes stays");
        Check(reported != null && reported.Count == 2, "Cut: OnBridgesCut fires with the cut bridges");
        Check(field.IsRunePowered(f) && field.IsRunePowered(n) && field.GetPowerUsed() == 2, "Cut: power didn't change");
        Check(field.CutOpenRingBridges().Count == 0, "Cut: cutting again finds nothing");

        RuneField reloaded = new RuneField(field.GetData().Clone(), settings, nodes, 10, 1);
        Check(reloaded.GetRune(p) != null && reloaded.GetRune(q) != null && reloaded.AreConnected(p, q), "Cut: loading the saved field keeps the runes and bridges past the open zone");
    }

    void TestFrozenChainKeepsPower()
    {
        //the playtest example, shortened: a 6 rune frozen chain ends in a node. At the next level a new 2 rune chain goes straight to the core
        //without frozen-first power, the 2 new runes (1 bridge out) would be powered before the far end of the chain and the node would lose power
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO element = MakeElement(1, 2, 150f);
        ElementItemSO longReach = MakeElement(1, 2, 400f);

        //the chain: two runes out from the core, then 4 more around a 220 circle, each 120 from the last (a 120 chord on a 220 circle is 2 x asin(60 / 220))
        List<Vector2> chainSpots = new List<Vector2>();
        chainSpots.Add(new Vector2(100, 0));
        chainSpots.Add(new Vector2(220, 0));
        float step = 2f * Mathf.Asin(60f / 220f); //in radians, which is what Mathf.Cos and Mathf.Sin use
        for (int i = 1; i <= 4; i++)
        {
            chainSpots.Add(new Vector2(Mathf.Cos(step * i), Mathf.Sin(step * i)) * 220f);
        }
        int endNode = AddNode(nodes, chainSpots[chainSpots.Count - 1]); //the last rune lands in this node, standing in for the gate

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 6, 0); //level 6: exactly enough for the chain
        List<int> chain = new List<int>();
        for (int i = 0; i < chainSpots.Count; i++)
        {
            chain.Add(Place(bound, element, chainSpots[i]));
        }
        Check(bound.IsNodeActive(endNode), "Frozen chain: the 6 rune chain powers its end node");

        RuneField field = new RuneField(bound.GetData().Clone(), settings, nodes, 7, 1); //evolved, and one level up
        int n1 = Place(field, longReach, new Vector2(0, -320));   //zone 2, straight to the core
        int n2 = Place(field, longReach, new Vector2(110, -380)); //zone 2, to the core and n1
        Check(field.AreConnected(n1, RuneField.CoreID) && field.AreConnected(n2, RuneField.CoreID), "Frozen chain: both new runes are 1 bridge from the core");

        bool chainPowered = true;
        for (int i = 0; i < chain.Count; i++)
        {
            if (field.IsRunePowered(chain[i]) == false) chainPowered = false;
        }
        Check(chainPowered && field.IsNodeActive(endNode), "Frozen chain: the whole frozen chain keeps its power, so its node stays on");
        Check(field.IsRunePowered(n1) && field.IsRunePowered(n2) == false, "Frozen chain: the new runes share what's left over (1 power)");
    }

    void TestFrozenPowerOrder()
    {
        //rule C: the frozen pass walks frozen bridges only, so a newer bridge between two frozen runes can't change which of them gets power first
        //chain core - a - b - c, plus d on the core. c was placed before b, so a shortcut a - c would put c ahead of b
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO element = MakeElement(1, 3, 150f);

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 10, 0);
        int a = Place(bound, element, new Vector2(100, 0), false);
        int c = Place(bound, element, new Vector2(120, 140), false); //141 from a, in reach, but only bridged to b for now
        int b = Place(bound, element, new Vector2(200, 60), false);
        int d = Place(bound, element, new Vector2(-100, 0), false);
        bound.Connect(a, RuneField.CoreID);
        bound.Connect(a, b);
        bound.Connect(b, c);
        bound.Connect(d, RuneField.CoreID);

        RuneField field = new RuneField(bound.GetData().Clone(), settings, nodes, 3, 1); //evolved, but only 3 power (a level down) for 4 frozen runes
        Check(field.Connect(a, c) && field.IsBridgeFrozen(a, c) == false, "Power order: the shortcut a - c is a normal bridge");
        Check(field.IsRunePowered(a) && field.IsRunePowered(d) && field.IsRunePowered(b), "Power order: a, d and b get power, the same as before the shortcut");
        Check(field.IsRunePowered(c) == false, "Power order: c stays dark, the shortcut didn't move it ahead of b");
    }

    void TestOldSlotFrozenLists()
    {
        //fields saved before the frozen lists existed get them filled in once from positions, but only past Bound
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO element = MakeElement(1, 2, 150f);

        RuneField bound = new RuneField(new RuneFieldData(), settings, nodes, 10, 0);
        int a = Place(bound, element, new Vector2(100, 0));
        int b = Place(bound, element, new Vector2(220, 0));

        RuneField stillBound = new RuneField(bound.GetData().Clone(), settings, nodes, 10, 0);
        Check(stillBound.GetData()._FrozenRunes.Count == 0 && stillBound.GetData()._FrozenBridges.Count == 0, "Old slot: a Bound field with empty lists stays empty");

        RuneField oldSlot = new RuneField(bound.GetData().Clone(), settings, nodes, 10, 1);
        Check(oldSlot.GetData()._FrozenRunes.Count == 2 && oldSlot.GetData()._FrozenBridges.Count == 2, "Old slot: an evolved field with empty lists gets them filled in from positions");
        Check(oldSlot.IsBridgeFrozen(a, RuneField.CoreID) && oldSlot.IsBridgeFrozen(a, b), "Old slot: the closed zone's bridges are frozen");

        RuneFieldData missingLists = bound.GetData().Clone();
        missingLists._FrozenRunes = null; //like data saved before the lists existed
        missingLists._FrozenBridges = null;
        missingLists._LockedRunes = null;
        RuneField missing = new RuneField(missingLists, settings, nodes, 10, 1);
        Check(missing.IsRuneFrozen(a) && missing.IsRuneFrozen(b), "Old slot: data with the lists missing (null) loads and gets them filled in");
    }

    void TestFrozenListsCleanUp()
    {
        //copies keep the frozen lists separate, and bad entries get cleaned out on load
        RuneFieldSettingsSO settings = MakeZoneSettings();
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();
        ElementItemSO element = MakeElement(1, 2, 150f);

        RuneFieldData data = new RuneFieldData();
        data._Runes.Add(MakeRuneEntry(0, element, new Vector2(100, 0)));
        data._Runes.Add(MakeRuneEntry(1, element, new Vector2(220, 0)));
        data._Bridges.Add(MakeBridgeEntry(RuneField.CoreID, 0));
        data._Bridges.Add(MakeBridgeEntry(0, 1));
        data._NextRuneID = 2;

        data._FrozenRunes.Add(0);
        data._FrozenRunes.Add(1);
        data._FrozenRunes.Add(5); //a rune that doesn't exist
        data._FrozenRunes.Add(0); //listed twice
        data._FrozenBridges.Add(MakeBridgeEntry(RuneField.CoreID, 0));
        data._FrozenBridges.Add(MakeBridgeEntry(0, RuneField.CoreID)); //the same bridge the other way round
        data._FrozenBridges.Add(MakeBridgeEntry(1, RuneField.CoreID)); //no real bridge behind it (logs a warning, that's expected)
        data._LockedRunes.Add(0); //not plugged into a node

        RuneFieldData copy = data.Clone();
        copy._FrozenRunes.Add(1);
        copy._FrozenBridges[0]._A = 1;
        Check(data._FrozenRunes.Count == 4 && data._FrozenBridges[0]._A == RuneField.CoreID, "Frozen lists: changing a copy's frozen lists doesn't change the original");

        RuneField field = new RuneField(data, settings, nodes, 10, 1);
        Check(field.GetData()._FrozenRunes.Count == 2 && field.IsRuneFrozen(0) && field.IsRuneFrozen(1), "Frozen lists: a missing rune and a repeat are cleaned out");
        Check(field.GetData()._FrozenBridges.Count == 1 && field.IsBridgeFrozen(0, RuneField.CoreID), "Frozen lists: a repeated frozen bridge and one with no real bridge are cleaned out");
        Check(field.GetData()._LockedRunes.Count == 0, "Frozen lists: a locked rune that isn't in a node is cleaned out");
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
        Check(bound.IsNodeBlocked(gate2) == false, "Gates: the ring 2 gate in the locked zone can take a planning rune (it would never get power)");

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

    PlacedRuneEntry MakeRuneEntry(int id, ElementItemSO element, Vector2 position)
    {
        //makes a rune entry for building saved data by hand
        PlacedRuneEntry rune = new PlacedRuneEntry();
        rune._ID = id;
        rune._Element = element;
        rune._Position = position;
        return rune;
    }

    RuneBridgeEntry MakeBridgeEntry(int idA, int idB)
    {
        //makes a bridge entry for building saved data by hand
        RuneBridgeEntry bridge = new RuneBridgeEntry();
        bridge._A = idA;
        bridge._B = idB;
        return bridge;
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
