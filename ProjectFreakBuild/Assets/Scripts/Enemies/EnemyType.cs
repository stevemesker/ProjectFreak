//Every enum value has an explicit number (= 3). Unity saves enums on assets as numbers, so a value added in the middle
//of the list (like Elite) gets the next unused number and saved assets don't shift. Same rule as DamageType.
namespace EnemyType
{
    //how important an enemy is. Popcorn through Elite get spread around by spawners, MiniBoss and Boss get placed in their own arenas
    public enum Rank
    {
        Popcorn = 0,
        Basic = 1,
        Lieutenant = 2,
        Elite = 5, //added Nov 2026. Listed here so it shows in order in the dropdown, but saved as 5 so MiniBoss and Boss keep their numbers
        MiniBoss = 3,
        Boss = 4
    }

    //how big an enemy is. Changes how far knockback pushes it (see SizeClassRulesSO)
    public enum SizeClass { Small, Medium, Large, Huge }
}
