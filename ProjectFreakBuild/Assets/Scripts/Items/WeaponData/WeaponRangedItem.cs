using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

////////////////////////////////////////////////
///
/// Data for a ranged weapon. WeaponAttackRanged reads it and does the firing.
/// See Ranged Weapon System in the GDD.
///
////////////////////////////////////////////////
[CreateAssetMenu(fileName = "SO_Weapon_Ranged_Name_0", menuName = "Combat/Ranged Weapon", order = 0)]
public class WeaponRangedItem : WeaponItem
{
    [Title("Firing")]
    [Tooltip("Holding the trigger keeps firing cycles. Off = one cycle per trigger pull, and pressing again mid cycle does nothing")]
    public bool _IsAutomatic = true;

    [Tooltip("Seconds of spin up after pressing the trigger before the first cycle (like a chain gun). Lost when the trigger is let go, kept through dashes. 0 = none")]
    [Min(0f)] public float _WarmUpTime = 0f;

    [Tooltip("Fires instant rays instead of projectiles. Each projectile in the cycle's pattern becomes one ray. Rays can tilt up and down to hit enemies on ledges or stairs, and can pierce")]
    public bool _IsHitScan;

    //HIT SCAN
    [FoldoutGroup("Hit Scan Settings"), ShowIf(nameof(_IsHitScan))]
    [Tooltip("How far the ray goes, in meters. 0 = infinite")]
    [Min(0f)] public float _FireDistance = 0f;

    [FoldoutGroup("Hit Scan Settings"), ShowIf(nameof(_IsHitScan))]
    [Tooltip("How many enemies one ray can hit before it stops. Destructible objects don't count")]
    [Min(1)] public int _HitPierce = 1;

    [FoldoutGroup("Hit Scan Settings"), ShowIf(nameof(_IsHitScan))]
    [Tooltip("How far up or down a shot can tilt to find a target (ledges, stairs), in degrees")]
    [Range(0f, 89f)] public float _VerticalAimAngle = 45f;

    [FoldoutGroup("Hit Scan Settings"), ShowIf(nameof(_IsHitScan))]
    [Tooltip("How close to the aim line (side to side) an enemy's body has to be to count as a target for tilting, in meters. Keep it small so it only catches enemies on the line")]
    [Min(0.01f)] public float _AimWidth = 0.5f; //todo: starting value still an open question in the GDD

    [FoldoutGroup("Hit Scan Settings"), ShowIf(nameof(_IsHitScan))]
    [Tooltip("Thickness of the ray, in meters (it's a sphere pushed along the line). A little thickness makes shots easier to land")]
    [Min(0.01f)] public float _RayRadius = 0.1f;

    [FoldoutGroup("Hit Scan Settings"), ShowIf(nameof(_IsHitScan))]
    [Tooltip("Line effect drawn from the muzzle to where the ray ends. Needs a LineRenderer and a HitScanTracerObject on it")]
    public GameObject _TracerEffect;

    [FoldoutGroup("Hit Scan Settings"), ShowIf(nameof(_IsHitScan))]
    [Tooltip("Effect spawned at each hit, facing out of the surface")]
    public GameObject _ImpactEffect;

    [FoldoutGroup("Hit Scan Settings"), ShowIf(nameof(_IsHitScan))]
    [Tooltip("Seconds before an impact effect is removed, in case it doesn't clean itself up")]
    [Min(0.1f)] public float _ImpactLifeTime = 2f;

    //CHARGE
    [Title("Charge")]
    [Tooltip("The weapon charges its shots while the trigger is held. Dashing cancels a charge")]
    public bool _UsesCharge;

    [FoldoutGroup("Charge Settings"), ShowIf(nameof(_UsesCharge))]
    [Tooltip("Seconds to reach max charge")]
    [InfoBox("Charge time + burst is longer than the attack time (1 / attack speed), so the weapon really fires slower than its attack speed says. Players read attack speed, so try to keep it the real rate.", InfoMessageType.Warning, nameof(IsChargeLongerThanAttackTime))]
    [Min(0.01f)] public float _ChargeTime = 1f;

    [FoldoutGroup("Charge Settings"), ShowIf(nameof(_UsesCharge))]
    [Tooltip("Damage multiplier at full charge. Firing early gives charge % × this, so a ×2 weapon at half charge does normal damage")]
    [Min(0f)] public float _MaxChargeMultiplier = 2f;

    [FoldoutGroup("Charge Settings"), ShowIf(nameof(_UsesCharge))]
    [Tooltip("If on, letting go before full charge doesn't fire. Automatic weapons always fire on their own at full charge")]
    public bool _RequiresFullCharge;

    [FoldoutGroup("Charge Settings"), ShowIf(nameof(_UsesCharge))]
    [Tooltip("The wielder moves slower while charging (player or shade)")]
    public bool _SlowWhileCharging;

    [FoldoutGroup("Charge Settings"), ShowIf(nameof(ShowChargeMoveMultiplier))]
    [Tooltip("Move speed while charging. 0.5 = half speed, 1 = normal")]
    [Range(0f, 1f)] public float _ChargeMoveMultiplier = 0.5f;

    //CYCLES
    [Title("Cycle", "What one cycle fires")]
    [InfoBox("$" + nameof(GetCycleWarning), InfoMessageType.Warning, nameof(HasCycleWarning))] //the $ tells Odin to call the function for the message text
    [HideLabel, InlineProperty] //draws the entry's fields right here instead of inside a collapsed box
    public FireCycleEntry _Cycle = new FireCycleEntry();

    [Title("Finisher", "Every Nth cycle fires this instead")]
    [Tooltip("Every Nth cycle fires the finisher. 0 = no finisher. Automatic weapons reset the count when the trigger is let go, others keep it between pulls")]
    [Min(0)] public int _FinisherEvery = 0;

    [ShowIf(nameof(HasFinisher))]
    [InfoBox("$" + nameof(GetFinisherWarning), InfoMessageType.Warning, nameof(HasFinisherWarning))]
    [HideLabel, InlineProperty]
    public FireCycleEntry _FinisherCycle = new FireCycleEntry();

    //local variables
    [System.NonSerialized] bool _warningsLogged; //NonSerialized: never saved to the asset, so it's safe to change at runtime. Stops the setup warnings repeating every equip

    #region Tools
    public bool HasFinisher()
    {
        //function for checking if this weapon uses a finisher
        return _FinisherEvery > 0;
    }

    public void LogSetupWarnings(Object context)
    {
        //logs the same problems the inspector warns about, once per play session, so they show up even if nobody looked at the asset
        if (_warningsLogged) return;
        _warningsLogged = true;

        if (HasCycleWarning()) Debug.LogWarning($"Warning! {name} cycle: {GetCycleWarning()}", context);
        if (HasFinisherWarning()) Debug.LogWarning($"Warning! {name} finisher: {GetFinisherWarning()}", context);
    }

    string GetEntryWarning(FireCycleEntry entry)
    {
        //function that collects every setup problem in one cycle entry into one message. Empty = all good
        if (entry == null) return "The cycle is empty.";

        string warning = "";
        if (entry._Projectile == null && _IsHitScan == false) warning += "No projectile assigned, nothing will fire. "; //hit scan doesn't use a projectile
        if (entry._BurstCount > 1 && entry.GetBurstTime() >= GetAttackTime()) warning += $"Burst is longer than the attack time ({GetAttackTime():0.##}s), so attack speed no longer matters. Set it up as automatic instead. ";
        if (entry.IsStacked()) warning += "Projectile count is more than 1 but firing origin and spread are both 0, so only one projectile fires (they'd stack on top of each other). ";
        return warning;
    }

    string GetCycleWarning() { return GetEntryWarning(_Cycle); }
    bool HasCycleWarning() { return GetCycleWarning() != ""; }

    string GetFinisherWarning() { return GetEntryWarning(_FinisherCycle); }
    bool HasFinisherWarning() { return HasFinisher() && GetFinisherWarning() != ""; }

    bool ShowChargeMoveMultiplier() { return _UsesCharge && _SlowWhileCharging; }

    bool IsChargeLongerThanAttackTime()
    {
        //used by the inspector warning on charge time
        if (_UsesCharge == false || _Cycle == null) return false;
        return _ChargeTime + _Cycle.GetBurstTime() > GetAttackTime();
    }
    #endregion
}

[System.Serializable]
public class FireCycleEntry
{
    //what one cycle of a ranged weapon fires. The normal cycle and the finisher both use this, so a finisher can change anything

    [Tooltip("Projectile prefab spawned for each shot. Needs a ProjectileObject on it. Hit scan weapons ignore this and the two projectile fields below")]
    public GameObject _Projectile;

    [Tooltip("How fast the projectile flies, in meters per second")]
    [Min(0f)] public float _ProjectileSpeed = 15f; //temp: moves to the projectile itself once projectiles get their own behaviors

    [Tooltip("Seconds before a projectile that hasn't hit anything removes itself")]
    [Min(0.1f)] public float _ProjectileLifeTime = 5f; //temp: same as speed

    [Tooltip("Projectiles (or rays) fired at the same time. More than 1 needs a firing origin or spread")]
    [Min(1)] public int _ProjectileCount = 1;

    [Tooltip("Times the pattern fires per cycle. 1 = no burst, 3 = a 3 round burst")]
    [Min(1)] public int _BurstCount = 1;

    [ShowIf(nameof(IsBurst))]
    [Tooltip("Burst shots per second")]
    [Min(0.01f)] public float _BurstSpeed = 10f;

    [Tooltip("Width of the line the projectiles start along, centered on the muzzle, in meters. 0 = all start at the muzzle")]
    [Min(0f)] public float _FiringOrigin = 0f;

    [ShowIf(nameof(HasOriginWidth))]
    [Tooltip("Start points are random within the width instead of evenly spaced")]
    public bool _RandomOrigin;

    [Tooltip("Total arc the projectiles fan out over, in degrees, centered on the wielder's forward. 360 = full circle. Negative points them inward so they cross")]
    [Range(-360f, 360f)] public float _Spread = 0f;

    [Tooltip("± random degrees added to each projectile's angle, so the pattern isn't identical every shot")]
    [Min(0f)] public float _SpreadRandomness = 0f;

    [Tooltip("× the weapon's damage. Lets a finisher hit harder")]
    [Min(0f)] public float _DamageMultiplier = 1f;

    #region Tools
    public float GetBurstTime()
    {
        //function for how long the burst takes, in seconds. The first shot is instant, then one wait between each shot
        if (_BurstCount <= 1) return 0f;
        return (_BurstCount - 1) / Mathf.Max(_BurstSpeed, 0.01f);
    }

    public bool IsStacked()
    {
        //true when several projectiles would spawn on top of each other flying the same way
        return _ProjectileCount > 1 && _FiringOrigin <= 0f && Mathf.Approximately(_Spread, 0f);
    }

    public int GetSafeProjectileCount()
    {
        //function for how many projectiles to really spawn. Stacked setups only fire 1
        if (IsStacked()) return 1;
        return Mathf.Max(1, _ProjectileCount);
    }

    bool IsBurst() { return _BurstCount > 1; }
    bool HasOriginWidth() { return _FiringOrigin > 0f; }
    #endregion
}
