using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestContent : MonoBehaviour
{
    public ItemSO content;
    public TimelineRunner _TLine;

    public void openChest()
    {
        print("Boop");
    }
    public void test()
    {
        print("Test");
    }
    public void signalTest()
    {
        print("woof");
        _TLine.ContinueTimeline();
    }
}
