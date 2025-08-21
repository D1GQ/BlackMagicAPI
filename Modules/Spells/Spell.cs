using BlackMagicAPI.Managers;

namespace BlackMagicAPI.Modules.Spells;

/// <summary>
/// Generic base class for spell implementations that provides access to spell instances
/// and prefabs for a specific spell data type.
/// </summary>
/// <typeparam name="SD">The type of SpellData associated with this spell</typeparam>
public class Spell<SD> where SD : SpellData
{
    /// <summary>
    /// Gets the data class for the spell.
    /// </summary>
    public static SD? GetData() => (SD)SpellManager.Mapping.FirstOrDefault(map => map.data.GetType() == typeof(SD)).data;

    /// <summary>
    /// Gets the prefab PageController associated with this spell type from the SpellManager.
    /// Returns null if no matching spell mapping is found.
    /// </summary>
    public static PageController? GetPagePrefab() => SpellManager.Mapping.FirstOrDefault(map => map.data.GetType() == typeof(SD)).page;

    /// <summary>
    /// Gets the SpellLogic prefab component associated with this spell type.
    /// Returns null if no valid spell prefab is found.
    /// </summary>
    /// <returns>The base SpellLogic component from the spell prefab</returns>
    public static SpellLogic? GetLogicPrefab() => GetPagePrefab()?.spellprefab.GetComponent<SpellLogic>();

    /// <summary>
    /// Gets the typed SpellLogic prefab component associated with this spell type.
    /// Returns null if no valid spell prefab is found or if the component doesn't match the specified type.
    /// </summary>
    /// <typeparam name="L">The specific SpellLogic subtype to retrieve</typeparam>
    /// <returns>The typed SpellLogic component from the spell prefab</returns>
    public static L? GetLogicPrefab<L>() where L : SpellLogic => GetPagePrefab()?.spellprefab.GetComponent<L>();

    /// <summary>
    /// Gets all active instances of this spell type as an array of SpellLogic objects.
    /// Returns an empty array if no instances exist.
    /// </summary>
    /// <returns>Array of active spell instances</returns>
    public static SpellLogic?[] GetLogicInstances()
    {
        if (SpellLogic.Instances.TryGetValue(typeof(SD).FullName, out var instances))
        {
            return [.. instances];
        }
        return [];
    }

    /// <summary>
    /// Gets all active instances of this spell type cast to a specific SpellLogic subtype.
    /// Returns an empty array if no instances exist.
    /// </summary>
    /// <typeparam name="SL">The specific SpellLogic subtype to return</typeparam>
    /// <returns>Array of active spell instances cast to the specified type</returns>
    public static SL?[] GetLogicInstances<SL>() where SL : SpellLogic
    {
        if (SpellLogic.Instances.TryGetValue(typeof(SD).FullName, out var instances))
        {
            return instances.Select(logic => logic as SL).Where(logic => logic != null).ToArray();
        }
        return [];
    }
}