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
    List<Object> _madeAssets = new List<Object>(); //temporary elements and layouts, destroyed when the tests finish

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
        TestNoLayout();

        CleanUp();

        if (_failed == 0) Debug.Log($"Rune field tests: all {_passed} checks passed", this);
        else Debug.LogError($"Error! Rune field tests: {_failed} failed, {_passed} passed. See the errors above", this);
    }
    #endregion

    #region Tests
    void TestBridgeReach()
    {
        //a rune inside the core's reach bridges to it, one far away bridges to nothing
        RuneFieldLayoutSO layout = MakeLayout();
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), layout, 10);

        int near = Place(field, element, new Vector2(100, 0));
        int far = Place(field, element, new Vector2(600, 0));

        Check(field.AreConnected(near, RuneField.CoreID), "Reach: a rune 100 from the core bridges to it");
        Check(field.GetConnections(far).Count == 0, "Reach: a rune 600 away bridges to nothing");
        Check(field.IsRunePowered(near) && field.IsRunePowered(far) == false, "Reach: only the bridged rune is powered");
    }

    void TestMaxBridges()
    {
        //a rune with 2 max bridges dropped next to 4 free runes only bridges to 2 of them
        RuneFieldLayoutSO layout = MakeLayout();
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), layout, 10);

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
        RuneFieldLayoutSO layout = MakeLayout();
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), layout, 2);

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
        RuneFieldLayoutSO layout = MakeLayout();
        ElementItemSO cheap = MakeElement(1, 2, 150f);
        ElementItemSO expensive = MakeElement(5, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), layout, 3);

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
        RuneFieldLayoutSO layout = MakeLayout();
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), layout, 10);

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
        RuneFieldLayoutSO layout = MakeLayout();
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), layout, 10);

        int a = Place(field, element, new Vector2(100, 0));
        Vector2 clamped = field.ClampToBridges(a, new Vector2(400, 0));

        Check(Mathf.Abs(clamped.x - 150f) < 0.01f && Mathf.Abs(clamped.y) < 0.01f, "Clamp: dragging to 400 stops at the 150 reach");
        Check(Mathf.Abs(field.GetBridgeStretch(a, new Vector2(400, 0), RuneField.CoreID) - 250f) < 0.01f, "Clamp: the bridge is stretched 250 past its reach");
    }

    void TestNodeLockoutsAndUnlocks()
    {
        //node 0 and node 1 lock each other, node 2 needs node 0 on
        RuneFieldLayoutSO layout = MakeLayout();
        int node0 = AddNode(layout, new Vector2(100, 0));
        int node1 = AddNode(layout, new Vector2(-100, 0));
        int node2 = AddNode(layout, new Vector2(0, 100));
        layout._AbilityNodes[node0]._Lockouts.Add(node1);
        layout._AbilityNodes[node1]._Lockouts.Add(node0);
        layout._AbilityNodes[node2]._Unlocks.Add(node0);

        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), layout, 10);

        int r0 = Place(field, element, new Vector2(100, 0));
        field.TryPlugRune(r0);
        Check(field.IsNodeActive(node0), "Nodes: a powered rune turns node 0 on");

        int r1 = Place(field, element, new Vector2(-100, 0));
        field.TryPlugRune(r1);
        Check(field.IsNodeActive(node1) == false, "Nodes: node 1 stays off while node 0 locks it");

        Place(field, element, new Vector2(0, -100)); //unrelated change
        Check(field.IsNodeActive(node0) && field.IsNodeActive(node1) == false, "Nodes: an unrelated change doesn't swap which locked node wins");

        int r3 = Place(field, element, new Vector2(0, 100));
        field.TryPlugRune(r3);
        Check(field.IsNodeActive(node2), "Nodes: node 2 turns on because node 0 is on");

        field.UnplugRune(r0);
        Check(field.IsNodeActive(node0) == false, "Nodes: unplugging turns node 0 off");
        Check(field.IsNodeActive(node1), "Nodes: node 1 turns on once node 0 stops locking it");
        Check(field.IsNodeActive(node2) == false, "Nodes: node 2 turns off without node 0");
    }

    void TestMaxPowerAndRemoval()
    {
        //dropping the core to 0 power turns everything off, and removing a rune cuts off what was behind it
        RuneFieldLayoutSO layout = MakeLayout();
        int node0 = AddNode(layout, new Vector2(100, 0));
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), layout, 10);

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
        RuneFieldLayoutSO layout = MakeLayout();
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), layout, 10);
        Place(field, element, new Vector2(100, 0));

        RuneFieldData copy = field.GetData().Clone();
        RuneField copyField = new RuneField(copy, layout, 10);
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

        new RuneField(broken, layout, 10);
        Check(broken._Bridges.Count == 1 && broken._Plugs.Count == 0, "Clean up: bad bridges and plugs are removed when a field loads");
    }

    void TestNoLayout()
    {
        //the Shade Manager works out stats before the rune field UI has handed it a layout. That must not wipe the saved node data
        RuneFieldLayoutSO layout = MakeLayout();
        int node0 = AddNode(layout, new Vector2(100, 0));
        ElementItemSO element = MakeElement(1, 2, 150f);
        RuneField field = new RuneField(new RuneFieldData(), layout, 10);
        int a = Place(field, element, new Vector2(100, 0));
        field.TryPlugRune(a);

        RuneFieldData saved = field.GetData().Clone();
        RuneField noLayoutField = new RuneField(saved, null, 10);

        Check(saved._Plugs.Count == 1 && saved._ActiveNodes.Contains(node0), "No layout: saved plugs and active nodes are kept");
        Check(noLayoutField.IsRunePowered(a) && noLayoutField.GetPowerUsed() == 1, "No layout: power still works from the saved bridges");
    }
    #endregion

    #region Tools
    RuneFieldLayoutSO MakeLayout()
    {
        //makes a temporary layout with the default core settings and no field radius limit
        RuneFieldLayoutSO layout = ScriptableObject.CreateInstance<RuneFieldLayoutSO>();
        layout._CoreReach = 150f;
        layout._CoreMaxBridges = 0;
        layout._FieldRadius = 0f;
        _madeAssets.Add(layout);
        return layout;
    }

    int AddNode(RuneFieldLayoutSO layout, Vector2 position)
    {
        //adds an ability node to a temporary layout and returns its index
        AbilityNodeEntry node = new AbilityNodeEntry();
        node._Position = position;
        layout._AbilityNodes.Add(node);
        return layout._AbilityNodes.Count - 1;
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
        //destroys the temporary elements and layouts the tests made
        for (int i = 0; i < _madeAssets.Count; i++)
        {
            if (_madeAssets[i] != null) DestroyImmediate(_madeAssets[i]);
        }
        _madeAssets.Clear();
    }
    #endregion
}
