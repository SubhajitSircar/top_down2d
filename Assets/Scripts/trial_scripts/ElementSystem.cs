using System.Collections.Generic;
using UnityEngine;

// 🔮 Clean, type-safe representation of your 5 elements
public enum ElementType
{
    Default, // Fallback lime circle
    Water,
    Fire,
    Earth,
    Lightning,
    Wind
}

// ⚔️ The 5 relationship states from your matrix image
public enum ReactionType
{
    NonReact,
    Strong,
    Weak,
    Reactive,
    Amplify
}

public static class ElementSystem
{
    // A nested dictionary to act as our underlying Rock-Paper-Scissors lookup table
    // Format: Matrix[AttackingElement][DefendingElement] = ReactionResult
    private static readonly Dictionary<ElementType, Dictionary<ElementType, ReactionType>> InteractionMatrix;

    // Static constructor: Runs automatically once in memory to bake your matrix table
    static ElementSystem()
    {
        InteractionMatrix = new Dictionary<ElementType, Dictionary<ElementType, ReactionType>>();

        // Initialize sub-dictionaries for every element
        foreach (ElementType attacker in System.Enum.GetValues(typeof(ElementType)))
        {
            InteractionMatrix[attacker] = new Dictionary<ElementType, ReactionType>();
        }

        // =========================================================================
        // 📊 IMPLEMENTING YOUR EXACT MATRIX SHEET (Attacker vs Defender)
        // =========================================================================

        // --- WATER ATTACKS ---
        SetReaction(ElementType.Water, ElementType.Water, ReactionType.NonReact);
        SetReaction(ElementType.Water, ElementType.Fire, ReactionType.Strong);
        SetReaction(ElementType.Water, ElementType.Earth, ReactionType.Reactive);
        SetReaction(ElementType.Water, ElementType.Lightning, ReactionType.Amplify);
        SetReaction(ElementType.Water, ElementType.Wind, ReactionType.Weak);

        // --- FIRE ATTACKS ---
        SetReaction(ElementType.Fire, ElementType.Water, ReactionType.Weak);
        SetReaction(ElementType.Fire, ElementType.Fire, ReactionType.NonReact);
        SetReaction(ElementType.Fire, ElementType.Earth, ReactionType.Strong);
        SetReaction(ElementType.Fire, ElementType.Lightning, ReactionType.Reactive);
        SetReaction(ElementType.Fire, ElementType.Wind, ReactionType.Amplify);

        // --- EARTH ATTACKS ---
        SetReaction(ElementType.Earth, ElementType.Water, ReactionType.Amplify);
        SetReaction(ElementType.Earth, ElementType.Fire, ReactionType.Weak);
        SetReaction(ElementType.Earth, ElementType.Earth, ReactionType.NonReact);
        SetReaction(ElementType.Earth, ElementType.Lightning, ReactionType.Strong);
        SetReaction(ElementType.Earth, ElementType.Wind, ReactionType.Reactive);

        // --- LIGHTNING ATTACKS (Labeled as 'Light' in row, 'Lightning' in col) ---
        SetReaction(ElementType.Lightning, ElementType.Water, ReactionType.Reactive);
        SetReaction(ElementType.Lightning, ElementType.Fire, ReactionType.Amplify);
        SetReaction(ElementType.Lightning, ElementType.Earth, ReactionType.Weak);
        SetReaction(ElementType.Lightning, ElementType.Lightning, ReactionType.NonReact);
        SetReaction(ElementType.Lightning, ElementType.Wind, ReactionType.Strong);

        // --- WIND ATTACKS ---
        SetReaction(ElementType.Wind, ElementType.Water, ReactionType.Strong);
        SetReaction(ElementType.Wind, ElementType.Fire, ReactionType.Reactive);
        SetReaction(ElementType.Wind, ElementType.Earth, ReactionType.Amplify);
        SetReaction(ElementType.Wind, ElementType.Lightning, ReactionType.Weak);
        SetReaction(ElementType.Wind, ElementType.Wind, ReactionType.NonReact);
    }

    private static void SetReaction(ElementType attacker, ElementType defender, ReactionType result)
    {
        InteractionMatrix[attacker][defender] = result;
    }

    // 🌐 THE PUBLIC ACCESS POINT: Call this from any script to get a reaction instantly!
    public static ReactionType GetEffectiveness(ElementType attacker, ElementType defender)
    {
        // Default fallbacks if things aren't registered yet
        if (attacker == ElementType.Default || defender == ElementType.Default)
            return ReactionType.NonReact;

        if (InteractionMatrix.ContainsKey(attacker) && InteractionMatrix[attacker].ContainsKey(defender))
        {
            return InteractionMatrix[attacker][defender];
        }

        return ReactionType.NonReact;
    }

    // 🎛️ UTILITY HOOK: Converts your string inputs ("Fire", "Water") safely into the Enum type
    public static ElementType ConvertStringToEnum(string elementName)
    {
        if (System.Enum.TryParse(elementName, true, out ElementType result))
        {
            return result;
        }
        return ElementType.Default;
    }
}