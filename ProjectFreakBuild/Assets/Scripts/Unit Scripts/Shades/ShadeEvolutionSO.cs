using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SO_ShadeEvo_New", menuName = "ScriptableObjects/Shades/EvolutionStats", order = 0)]
public class ShadeEvolutionSO : ScriptableObject
{
    public ShadeStats _coreStats;

    [Header("Art")]
    [Tooltip("Art prefab spawned while the shade is tethered. Should have a ShadeArtRig on its root")]
    public GameObject _TetheredArt;

    [Tooltip("Art prefab spawned while the shade is released. Can be the same prefab as Tethered Art (use the rig's hide lists to turn parts off per form)")]
    [FormerlySerializedAs("_characterArt")] //this field used to be called _characterArt, this keeps the art already assigned on existing assets
    public GameObject _ReleasedArt;

    [Header("Tether")]
    [Tooltip("How far from the player the tethered shade sits, in meters. Bigger shades need more room")]
    [Min(0f)] public float _TetherDistance = 2f;

    [Tooltip("How wide the shade's body is where the tail connects, in meters. Not used yet, for the real tail later")]
    [Min(0f)] public float _TetherWidth = 0.5f;
}
