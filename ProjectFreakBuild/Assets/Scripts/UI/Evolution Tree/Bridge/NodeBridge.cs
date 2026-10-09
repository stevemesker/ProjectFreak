using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//the line between two connected pieces on the 2D rune field (the dungeon map uses it for its lines too). It's a view: it draws the bridge and runs the tear timer,
//and when the timer fills it asks the rune field to remove the bridge
public class NodeBridge : MonoBehaviour
{
    [Header("<=====Tearing Settings=====>")]
    [Tooltip("How much pull has to build up before the bridge snaps")]
    [SerializeField] private float PullTearRequiredTime = 10;
    [Tooltip("Cap on how hard a single pull can count")]
    [SerializeField] private float MaxPullStrength = 200;
    [Tooltip("How often the pull builds up, in seconds")]
    [SerializeField] private float pullTickTimeLength = .1f;
    [Tooltip("Bigger = pulling further counts for less. Pull strength is (pixels past reach / this)")]
    [SerializeField] private float pullStrengthModifierDampening = 150;

    [Header("<=====Connections=====>")]
    [Tooltip("The art that gets stretched and rotated between the two ends")]
    [SerializeField] GameObject artPointer;
    [Tooltip("Object at one end (read only)")]
    public GameObject connectionOne;
    [Tooltip("Object at the other end (read only)")]
    public GameObject connectionTwo;

    [Header("<-----Runtime Data----->")]
    [SerializeField] private bool tearing;
    [SerializeField] private float PullTearCurrentTime;
    [SerializeField] private float currentPullStrength;

    //local variables
    RuneFieldManager _runeField;
    int _endA; //rune ID (or RuneField.CoreID) at each end
    int _endB;
    Coroutine _pullTimer;

    private void OnDisable()
    {
        //stops the tear timer so it doesn't keep running on a bridge that's going away
        if (_pullTimer != null) StopCoroutine(_pullTimer);
        _pullTimer = null;
        tearing = false;
    }

    #region Initialize
    public void Setup(RuneFieldManager runeField, int idA, int idB, GameObject objectA, GameObject objectB)
    {
        //function the rune field calls right after spawning this bridge
        _runeField = runeField;
        _endA = idA;
        _endB = idB;
        BuildConnection(objectA, objectB);
    }

    public void BuildConnection(GameObject X, GameObject Y)
    {
        //sets the two objects the bridge is drawn between
        //the dungeon map calls this directly: it only needs the line drawn, with no rune field and no tearing
        connectionOne = X;
        connectionTwo = Y;
    }

    public bool Touches(int id)
    {
        //is this rune (or the core) one of the bridge's ends
        return _endA == id || _endB == id;
    }

    public int GetOtherEnd(int id)
    {
        //returns the ID at the opposite end from the one given
        return _endA == id ? _endB : _endA;
    }
    #endregion

    #region Drawing
    public void UpdatePosition(float length)
    {
        //move to position of first connected node
        transform.position = connectionOne.GetComponent<RectTransform>().transform.position;
        RectTransform rect = artPointer.GetComponent<RectTransform>();

        //find current angle rotation
        Vector2 direction = connectionTwo.transform.position - connectionOne.transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        //set angle and length of bridge
        rect.rotation = Quaternion.Euler(0, 0, angle-90f);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, length);
    }
    #endregion

    #region Tearing
    public void StartTearing(float pullDistance)
    {
        //function called every drag frame while the bridge is pulled past its reach. Pulling harder makes it tear faster
        currentPullStrength = pullDistance / pullStrengthModifierDampening;
        if (currentPullStrength > MaxPullStrength) currentPullStrength = MaxPullStrength;
        if (tearing) return; //timer already running, it just picks up the new strength

        tearing = true;
        _pullTimer = StartCoroutine(TearTimer());
    }

    public void StopTearing()
    {
        //function called when the pull stops (the rune moved back in reach or was let go)
        currentPullStrength = 0;
        PullTearCurrentTime = 0;
        tearing = false;
        if (_pullTimer != null) StopCoroutine(_pullTimer);
        _pullTimer = null;
    }

    IEnumerator TearTimer()
    {
        //builds up pull every tick until the bridge snaps
        while (PullTearCurrentTime < PullTearRequiredTime)
        {
            yield return new WaitForSeconds(pullTickTimeLength);
            PullTearCurrentTime += pullTickTimeLength * currentPullStrength;
        }

        _pullTimer = null;
        tearing = false;
        if (_runeField != null) _runeField.TearBridge(_endA, _endB); //the field removes the bridge, then redraws (which destroys this object)
    }
    #endregion
}
