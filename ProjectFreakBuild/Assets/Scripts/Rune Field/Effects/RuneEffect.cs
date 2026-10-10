using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//parent class for everything a rune field can do to a shade: stat changes, granting abilities, evolving
//works like AbilityFunction: lists of these use [SerializeReference], so one list can hold different effect types and the inspector lets you pick which type to add
//ability nodes hold a list of these, and rune stat boosts are StatChangeEffects. Saving a field compiles them all into one list (see RuneField.CompileEffects)
//to make a new effect type: create a class that inherits RuneEffect, add [System.Serializable], and override GetSetupProblem if it can be set up wrong
[System.Serializable]
public class RuneEffect
{
    public virtual RuneEffect Clone()
    {
        //function that makes a copy of this effect, so a compiled list never shares objects with the node or rune it came from
        //MemberwiseClone copies every field into a new object. Asset references (AbilitySO, ShadeEvolutionSO) still point at the same asset, which is what we want
        return (RuneEffect)MemberwiseClone();
    }

    public virtual string GetSetupProblem()
    {
        //function that says what's wrong with this effect's setup, or "" if it's fine
        //effects with a problem are skipped when a field compiles, and the problem shows as a warning in the inspector
        return "";
    }

    public bool IsSetUp()
    {
        return GetSetupProblem() == "";
    }

    #region Tools
    public static List<RuneEffect> CloneList(List<RuneEffect> effects)
    {
        //function that copies a whole effect list. Empty entries are left out
        //"static" means it belongs to the class itself, so it's called as RuneEffect.CloneList(...) without needing an effect first
        List<RuneEffect> copy = new List<RuneEffect>();
        if (effects == null) return copy;

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] == null) continue;
            copy.Add(effects[i].Clone());
        }
        return copy;
    }

    public static bool HasEvolve(List<RuneEffect> effects)
    {
        //function that checks a list for an Evolve effect. A node with one is an evolution gate
        if (effects == null) return false;

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] is EvolveEffect) return true; //"is" checks what type an object really is
        }
        return false;
    }

    public static ShadeEvolutionSO GetEvolution(List<RuneEffect> effects)
    {
        //function that returns the form a list's first Evolve effect evolves into, or null if it has none (or it isn't set)
        if (effects == null) return null;

        for (int i = 0; i < effects.Count; i++)
        {
            EvolveEffect evolve = effects[i] as EvolveEffect;
            if (evolve != null && evolve._Evolution != null) return evolve._Evolution;
        }
        return null;
    }

    public static Dictionary<DamageType.StatType, int> AddUpStats(List<RuneEffect> effects)
    {
        //function that adds up every stat change in a list, per stat. Used for a field's compiled list and a slot's saved one
        Dictionary<DamageType.StatType, int> totals = new Dictionary<DamageType.StatType, int>();
        if (effects == null) return totals;

        for (int i = 0; i < effects.Count; i++)
        {
            StatChangeEffect statChange = effects[i] as StatChangeEffect; //"as" gives the effect back as a StatChangeEffect, or null if it's a different type
            if (statChange == null || statChange.IsSetUp() == false) continue;

            if (totals.ContainsKey(statChange._Stat)) totals[statChange._Stat] += statChange._Amount;
            else totals.Add(statChange._Stat, statChange._Amount);
        }
        return totals;
    }

    public static string GetListProblems(List<RuneEffect> effects, string ownerName)
    {
        //function that lists every setup problem in an effect list, one per line, for inspector warnings. "" if there are none
        string problems = "";
        if (effects == null) return problems;

        int evolveCount = 0;
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] == null)
            {
                problems += $"{ownerName} effect {i} is empty (no type picked)\n";
                continue;
            }

            string problem = effects[i].GetSetupProblem();
            if (problem != "") problems += $"{ownerName} effect {i}: {problem}\n";
            if (effects[i] is EvolveEffect) evolveCount++;
        }

        if (evolveCount > 1) problems += $"{ownerName} has {evolveCount} Evolve effects. A gate should only evolve into one form\n";
        return problems;
    }
    #endregion
}
