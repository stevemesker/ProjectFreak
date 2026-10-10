using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//what the inventory list and the shade slot buttons need from a rune field view
//both the 2D view (RuneFieldManager) and the 3D view (RuneField3DObject) have it, so the same UI works with either
public interface IRuneFieldView
{
    event System.Action OnDraftChanged; //fires whenever the draft changes or a field loads

    int GetPendingUse(ElementItemSO element); //how many of this rune the draft uses on top of the saved field
    int GetAvailable(ElementItemSO element); //how many of this rune can still be placed
    bool CanPlaceFromInventory(ElementItemSO element, Vector2 screenPosition); //could a rune from the inventory go here
    void PlaceRuneFromInventory(ElementItemSO element, Vector2 screenPosition); //drop a rune from the inventory here
    Color GetInvalidColor(); //tint for a spot a rune can't go
    void SelectSlot(int index); //open another shade slot (asks about unsaved changes first)
}
