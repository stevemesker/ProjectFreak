using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class NodeBridge : MonoBehaviour
{   
    [Header("<=====Connections=====>")]
    [SerializeField] GameObject artPointer;
    public GameObject connectionOne;
    public GameObject connectionTwo;

    [Header("<=====Tearing Variables=====>")]
    [SerializeField] private bool tearing;
    [SerializeField] private float PullTearRequiredTime = 10;
    [SerializeField] private float PullTearCurrentTime;
    [SerializeField] private Coroutine currentPullTimer;
    [SerializeField] private float MaxPullStrength = 200;
    [SerializeField] private float currentPullStrength;
    [SerializeField] private float pullTickTimeLength = .1f; //how much time it takes to end one tick
    [SerializeField] private float pullStrengthModifierDampening = 150;
    public void BuildConnection(GameObject X, GameObject Y)
    {
        connectionOne = X;
        connectionTwo = Y;
    }

    public void updatePosition(float length)
    {
        //transform.position = connectionOne.transform.position;
        transform.position = connectionOne.GetComponent<RectTransform>().transform.position;
        RectTransform rect = artPointer.GetComponent<RectTransform>();

        Vector2 direction = connectionTwo.transform.position - connectionOne.transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rect.rotation = Quaternion.Euler(0, 0, angle-90f);
        //rect.sizeDelta = new Vector2(rect.sizeDelta.x, length+(connectionOne.GetComponent<RectTransform>().rect.width/2) + (connectionTwo.GetComponent<RectTransform>().rect.width / 2));
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, length);

    }

    #region BreakConnection

    public void StartTearing(float distance, GameObject origin)
    {
        currentPullStrength = distance / pullStrengthModifierDampening;
        if (currentPullStrength > MaxPullStrength) currentPullStrength = MaxPullStrength;
        if (tearing == true) return;
        tearing = true;

        currentPullTimer = StartCoroutine(TearTickTimer(origin));
    }

    public void StopTearing()
    {
        //print("stopping");
        currentPullStrength = 0;
        PullTearCurrentTime = 0;
        if (currentPullTimer != null)
        {
            StopCoroutine(currentPullTimer);
            currentPullTimer = null;
            tearing = false;
        }
    }

    public void SeverConnection(GameObject origin)
    {
        print("Severing connections between " + connectionOne.name + " and " + connectionTwo);

        connectionOne.GetComponent<IBridgeable>().disconnectNodes(connectionTwo);
        connectionTwo.GetComponent<IBridgeable>().disconnectNodes(connectionOne);
        origin.GetComponent<IDragHandler>().OnDrag(null);

        //power handling
        GameObject CoreHolder = connectionOne.GetComponent<IConnectable>().GetCoreNode();
        if (CoreHolder == null) Destroy(gameObject); //it was never connected to the core and has no power anyway

        bool tempX = connectionOne.GetComponent<IConnectable>().SearchCore(connectionOne);
        bool tempY = connectionTwo.GetComponent<IConnectable>().SearchCore(connectionOne);

        print(connectionOne.name + " " + tempX + " | " + connectionTwo.name + " " + tempY);

        //both sides are still connected
        if (tempX == true && tempY == true) Destroy(gameObject);

        //shut down nodes
        if (tempX == false) connectionOne.GetComponent<IConnectable>().DisconnectNodeTree();
        if (tempY == false) connectionTwo.GetComponent<IConnectable>().DisconnectNodeTree();

        //run core power
        CoreHolder.GetComponent<IConnectable>().ConsumePower();
        //end power handling

        Destroy(gameObject);

        //temp, can probably just delete these once everything is running properly unless I want to object pool bridges but I doubt it
        //artPointer.SetActive(false);
        //currentPullStrength = 0;
        //PullTearCurrentTime = 0;
        //StopCoroutine(currentPullTimer);
        //currentPullTimer = null;
        //tearing = false;
    }

    IEnumerator TearTickTimer(GameObject origin)
    {
        //function that counts down for tearing ticks
        yield return new WaitForSeconds(pullTickTimeLength);

        PullTearCurrentTime += pullTickTimeLength * currentPullStrength;

        if (PullTearCurrentTime < PullTearRequiredTime) currentPullTimer = StartCoroutine(TearTickTimer(origin));
        else SeverConnection(origin);
    }

    #endregion
}
