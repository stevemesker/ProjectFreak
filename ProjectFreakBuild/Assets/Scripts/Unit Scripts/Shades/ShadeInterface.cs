using UnityEngine;

public interface IShadeForm
{
    //gives the form its slot data and who summoned it, then spawns its art
    void Setup(ShadeSO slot, GameObject summoner);

    //shows the shade. Instant for now, hook for the magic circle / pop-out effect later
    void Appear();

    //puts the shade away. Destroys it for now, hook for the sink-into-the-player effect later
    void Dismiss();
}
