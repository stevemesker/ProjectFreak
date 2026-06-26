using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

public class Player : MonoBehaviour
{
    //Core
    public static Player player;
    public PlayerData pData;

    //State Data/Pointers
    [FoldoutGroup("Current State Data")]
    public GameObject camTarget;
    [FoldoutGroup("Current State Data")]
    [Tooltip("Points to the hand bone so weapon swapping knows where to instantiate the weapon to. IMPORTANT: hand bone must be the lowest level child as swapping checks for children and will delete it when swapping. Can easily break parent chains")]
    public GameObject handPointer;

    //event Variable//
    [FoldoutGroup("Events")]
    [SerializeField] private PickupEventChannelSO pickupChannel;

    //Combat Variables
    [FoldoutGroup("Combat")][Header("Weapons")][SerializeField] 
    bool isCharging;
    [FoldoutGroup("Combat")][SerializeField] 
    float chargeAmount;
    [FoldoutGroup("Combat")][Tooltip("Current selection number"), SerializeField]
    private int weaponSelection;

    //Private/Unserialized Variables
    private ITriggerable weaponTrigger;
    private Coroutine chargeTime;
    private Coroutine cycleTimer;
    private float chargeTimeInitiated;

    #region Initialize
    private void Start()
    {
        if (Player.player != null) { Destroy(gameObject); return; }
        Player.player = this;
        UpdateEquippedWeaponSlotSize();
    }

    private void OnEnable()
    {
        if (pickupChannel != null)
            pickupChannel.OnPickup += HandlePickup;
    }

    private void OnDisable()
    {
        if (pickupChannel != null)
            pickupChannel.OnPickup -= HandlePickup;
    }
    #endregion

    #region Item Pickup
    private void HandlePickup(ItemSO item, int amount, GameObject origin)
    {
        print("Picking up " + item.name);

        switch(item)
        {
            case WeaponItem weapon:
                if (pickUpWeapon(item as WeaponItem)) origin.GetComponent<ItemDrop>().removeItemInventory(amount);
                else Debug.LogWarning("Warning! Cannot fit weapon " + item.name);
                break;
            default:
                if (pickUpItem(item, amount)) origin.GetComponent<ItemDrop>().removeItemInventory(amount);
                else Debug.LogWarning("Warning! Cannot fit item " + item.name);
                break;
        }
    }

    private bool pickUpWeapon(WeaponItem wpn)
    {
        if (pData.pInventory.checkEquippedWeaponFits(wpn) == true)
        {
            pData.pInventory.addEquipmentInventory(wpn);
            updateCurrentWeapon();
            return true;
        }
        else if (pData.pInventory.checkInventoryFits(wpn, 1))
        {
            pData.pInventory.addBackpackInventory(wpn, 1);
            return true;
        }

        return false;
    }
    
    private bool pickUpItem(ItemSO item, int amount)
    {
        if (pData.pInventory.checkInventoryFits(item, amount))
        {
            pData.pInventory.addBackpackInventory(item, 1);
            return true;
        }
        return false;
    }
    #endregion

    #region Equipment
    public void setActiveWeapon (int index)
    {
        //function that hanles switching weapon selection
        int wpn = index;
        if (index < 0)
        {
            wpn = pData.pInventory._EquipmentSize - Mathf.Abs(index % pData.pInventory._EquipmentSize);
        }
        weaponSelection = wpn % pData.pInventory._EquipmentSize;
        updateCurrentWeapon();
    }

    void updateCurrentWeapon()
    {
        if (handPointer == null) { Debug.LogError("Error! Hand bone has not been selected to allow weapon swapping"); return; }

        if (pData.pInventory._EquippedWeapons[weaponSelection] == null || pData.pInventory._EquippedWeapons[weaponSelection].weaponPrefab == null)
        {
            //empty selection or no weapon prefab, hold nothing
            if (handPointer.transform.childCount != 0)
            {
                Destroy(handPointer.transform.GetChild(0).gameObject);
            }
            return;
        }

        if (handPointer.transform.childCount != 0) Destroy(handPointer.transform.GetChild(0).gameObject);
        //Spawn Current Weapon
        GameObject wpn = Instantiate(pData.pInventory._EquippedWeapons[weaponSelection].weaponPrefab, handPointer.transform.position, handPointer.transform.transform.rotation, handPointer.transform);
        wpn.name = pData.pInventory._EquippedWeapons[weaponSelection].ItemName;

        wpn.GetComponent<ITriggerable>().SetUpWeapon(pData.pInventory._EquippedWeapons[weaponSelection], gameObject);

    }
    public void UpdateEquippedWeaponSlotSize()
    {
        //function ensures there are a correct number of equipped weapon slots available
        if (pData.pInventory._EquippedWeapons.Count < pData.pInventory._EquipmentSize)
        {
            for (int i = pData.pInventory._EquippedWeapons.Count; i < pData.pInventory._EquipmentSize; i++)
            {
                pData.pInventory._EquippedWeapons.Add(null);
            }
            return;
        }
        if (pData.pInventory._EquippedWeapons.Count > pData.pInventory._EquipmentSize)
        {
            //removes them if the size shrank. WARNING! Currently don't have a way to put the equipment back into an inventory cuz I dunno where it needs to go yet or if that's even a problem I'll run into
            for (int i = pData.pInventory._EquippedWeapons.Count; i > pData.pInventory._EquipmentSize; i--)
            {
                pData.pInventory._EquippedWeapons.RemoveAt(pData.pInventory._EquippedWeapons.Count - 1);
            }
            return;
        }
    }

    public int getActiveWeaponIndex()
    {
        return weaponSelection;
    }
    #endregion

    #region Use Weapon
    public void UseCurrentWeapon()
    {
        print("using weapon");
        if (handPointer.transform.childCount == 0) { print("Need to add unarmed strike"); return; }

        if (pData.pInventory._EquippedWeapons[weaponSelection].isChargedShot == true)
        {
            print("Charging has begun...");
            
            chargeTimeInitiated = Time.time;
            chargeTime = StartCoroutine(ChargeTimer(pData.pInventory._EquippedWeapons[weaponSelection].chargeMaxAmount));
            return;
        }
        fireWeapon(1);
        /*
        
        if (handPointer.transform.childCount == 0) { print("Need to add unarmed strike"); return; }

        //startcharging
        if (pData.pInventory._EquippedWeapons[weaponSelection].isChargedShot == true)
        {
            print("Charging has begun...");
            isCharging = true;
            return;
        }
        fireWeapon(1f);
        */
    }
    public void releaseCurrentWeapon()
    {
        if (isCharging)
        {
            float timeRemaining = Time.time - chargeTimeInitiated;
            if (timeRemaining > pData.pInventory._EquippedWeapons[weaponSelection].chargeMaxAmount) timeRemaining = pData.pInventory._EquippedWeapons[weaponSelection].chargeMaxAmount;
            StopCoroutine(chargeTime);
            chargeTime = null;
            //fireWeapon(timeRemaining);
            isCharging = false;
            return;
        }
        if (cycleTimer != null)
        {
            StopCoroutine(cycleTimer);
            cycleTimer = null;
        }
    }

    void fireWeapon(float multiplier)
    {
        print("Bang! X " + multiplier);
        //handPointer.transform.GetChild(0).GetComponent<ITriggerable>().TriggerAttack(CalculateDamage(multiplier), getElementalDamage());

        if (pData.pInventory._EquippedWeapons[weaponSelection].isAutomatic)
        {
            cycleTimer = StartCoroutine (CycleTimer(pData.pInventory._EquippedWeapons[weaponSelection].weaponFireRate, multiplier));
        }
    }

    IEnumerator ChargeTimer(float amount)
    {
        isCharging = true;
        yield return new WaitForSeconds(amount);
        if (pData.pInventory._EquippedWeapons[weaponSelection].isAutomatic) 
        { 
            fireWeapon(amount);
        }
    }

    IEnumerator CycleTimer(float cycleTime, float bonus)
    {
        yield return new WaitForSeconds(cycleTime);
        fireWeapon(bonus);
    }

    int CalculateDamage(float multiplier)
    {

        return 0;
    }
    List<ElementType.Element> getElementalDamage()
    {
        List<ElementType.Element> eleOut = new List<ElementType.Element>();
        eleOut.Add(pData.pInventory._EquippedWeapons[weaponSelection].element);

        //add other bonuses here
        //------------------------

        return eleOut;
    }
    #endregion

    //[Button("Test equipment Index")]
    public ItemSO testInventoryIndex(int index)
    {
        if (pData.pInventory._BackpackInventory.Count < index + 1) return null;
        ItemSO keyAtIndex = pData.pInventory._BackpackInventory.Keys.ElementAt(index);
        return keyAtIndex;
    }
}
