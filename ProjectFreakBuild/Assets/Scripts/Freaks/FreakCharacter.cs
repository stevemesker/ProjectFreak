using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class FreakCharacter : MonoBehaviour
{
    //Base Data Variables
    [Header("Base Data")]

    [Tooltip("Base stats for the indivicual freak loaded in from the game manager")]
    public FreakData freakData;
    [Tooltip("Current held inventory of the freak")] 
    public FreakInventory _Inventory;

    [Header("Current State Data")]

    [Tooltip("Current selection number. Will be provate when I have a ui"),SerializeField]
    private int weaponSelection;
    [Tooltip("Points to the hand bone so weapon swapping knows where to instantiate the weapon to. IMPORTANT: hand bone must be the lowest level child as swapping checks for children and will delete it when swapping. Can easily break parent chains")]
    public GameObject handPointer;
    
    private ITriggerable weaponTrigger;
    //event Variable//
    [Header("Events")]
    [SerializeField] private PickupEventChannelSO pickupChannel;

    //local variables
    private PlayerInput pInput;
    [SerializeField]GameObject wpn;
    [SerializeField] float chargeAmount;
    [SerializeField] bool isCharging = false;

    private void Update()
    {
        if(isCharging == true && chargeAmount < _Inventory._FreakEquippedWeapons[weaponSelection].chargeMaxAmount)
        {
            chargeAmount += Time.deltaTime;
            if (chargeAmount >= _Inventory._FreakEquippedWeapons[weaponSelection].chargeMaxAmount)
            {
                print("Max Charge reached");
                chargeAmount = _Inventory._FreakEquippedWeapons[weaponSelection].chargeMaxAmount;
                if (_Inventory._FreakEquippedWeapons[weaponSelection].ChargeAutoAttack)
                {
                    fireWeapon(chargeAmount);
                    chargeAmount = 0;
                }
                return;
            }
        }
    }

    #region Initializing
    private void Awake()
    {
        _Inventory._FreakBackpackInventory = new Dictionary<ItemSO, int>();
        UpdateEquippedWeaponSlotSize();
        updateCurrentWeapon();
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
        int slotsAvailable = _Inventory._FreakInventorySize - _Inventory._FreakBackpackInventory.Count;
        ItemDrop drop = new ItemDrop();

        if (origin != null) drop = origin.GetComponent<ItemDrop>();

        if (item is WeaponItem && slotsAvailable > 0)
        {
            if (drop != null) drop.removeItemInventory(amount);
        }
    }
    #endregion

    #region Weapon Selection
    public void UpdateEquippedWeaponSlotSize()
    {
        //function ensures there are a correct number of equipped weapon slots available
        if (_Inventory._FreakEquippedWeapons.Count < _Inventory._FreakEquipmentSize)
        {
            for (int i = _Inventory._FreakEquippedWeapons.Count; i < _Inventory._FreakEquipmentSize; i++)
            {
                _Inventory._FreakEquippedWeapons.Add(null);
            }
            return;
        }
        if (_Inventory._FreakEquippedWeapons.Count > _Inventory._FreakEquipmentSize)
        {
            //removes them if the size shrank. WARNING! Currently don't have a way to put the equipment back into an inventory cuz I dunno where it needs to go yet or if that's even a problem I'll run into
            for (int i = _Inventory._FreakEquippedWeapons.Count; i > _Inventory._FreakEquipmentSize; i--)
            {
                _Inventory._FreakEquippedWeapons.RemoveAt(_Inventory._FreakEquippedWeapons.Count-1);
            }
            return;
        }
    }

    public void EquippedWeaponScrollSelection(int direction)
    {
        weaponSelection += direction;
        if (weaponSelection < 0) { weaponSelection += _Inventory._FreakEquipmentSize; updateCurrentWeapon(); return; }
        if (weaponSelection >= _Inventory._FreakEquipmentSize) weaponSelection -= _Inventory._FreakEquipmentSize;

        //update current equipped weapon art here
        updateCurrentWeapon();
    }

    void updateCurrentWeapon()
    {
        if (handPointer == null) { Debug.LogError("Error! Hand bone has not been selected to allow weapon swapping"); return; }
        if (_Inventory._FreakEquippedWeapons[weaponSelection] == null || _Inventory._FreakEquippedWeapons[weaponSelection].weaponPrefab == null)
        {
            //empty selection or no weapon prefab, hold nothing
            if (handPointer.transform.childCount != 0)
            {
                Destroy(handPointer.transform.GetChild(0).gameObject);
                wpn = null;
            }
            return;
        }
        if (handPointer.transform.childCount != 0) Destroy(handPointer.transform.GetChild(0).gameObject);

        //Spawn Current Weapon
        wpn = Instantiate(_Inventory._FreakEquippedWeapons[weaponSelection].weaponPrefab, handPointer.transform.position, handPointer.transform.transform.rotation, handPointer.transform);
        wpn.name = _Inventory._FreakEquippedWeapons[weaponSelection].ItemName;
        
        wpn.GetComponent<ITriggerable>().SetUpWeapon(_Inventory._FreakEquippedWeapons[weaponSelection], gameObject);
        
    }
    #endregion

    #region UseWeapon
    public void UseCurrentWeapon()
    {
        //print("using weapon");
        if (wpn == null) { print("Need to add unarmed strike"); return; }

        //startcharging
        if (_Inventory._FreakEquippedWeapons[weaponSelection].isChargedShot == true)
        {
            print("Charging has begun...");
            isCharging = true;
            return;
        }
        fireWeapon(1f);
    }
    public void releaseCurrentWeapon()
    {
        if (_Inventory._FreakEquippedWeapons[weaponSelection].isChargedShot == false)
        {
            wpn.GetComponent<ITriggerable>().ReleaseAttack();
            return;
        }
        if (_Inventory._FreakEquippedWeapons[weaponSelection].ChargeAutoAttack == false) fireWeapon(chargeAmount);
        isCharging = false;
        chargeAmount = 0;
    }

    void fireWeapon(float multiplier)
    {
        //print("Firing weapon");
        wpn.GetComponent<ITriggerable>().TriggerAttack(CalculateDamage(multiplier), getElementalDamage());
    }

    int CalculateDamage(float multiplier)
    {

        return 0;
    }
    List<ElementType.Element> getElementalDamage()
    {
        List<ElementType.Element> eleOut = new List<ElementType.Element>();
        eleOut.Add(_Inventory._FreakEquippedWeapons[weaponSelection].element);

        //add other bonuses here
        //------------------------

        return eleOut;
    }
    #endregion
}
