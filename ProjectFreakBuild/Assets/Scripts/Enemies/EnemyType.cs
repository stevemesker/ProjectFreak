namespace EnemyType
{
    //how important an enemy is. Popcorn through Lieutenant get spread around by spawners, MiniBoss and Boss get placed in their own arenas
    public enum Rank { Popcorn, Basic, Lieutenant, MiniBoss, Boss }

    //how big an enemy is. Changes how far knockback pushes it (see SizeClassRulesSO)
    public enum SizeClass { Small, Medium, Large, Huge }
}
