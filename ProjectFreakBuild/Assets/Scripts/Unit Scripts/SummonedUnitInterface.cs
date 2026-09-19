using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISummonUnit
{
    void AssignSummoner(GameObject Summoner);
    void UpdateStats(CoreStats summonerStats);
}
