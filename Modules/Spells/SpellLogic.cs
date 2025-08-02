using UnityEngine;

namespace BlackMagicAPI.Modules.Spells;

/// <summary>
/// Abstract base class for spell behavior logic.
/// Provides the core interface for spell casting functionality and initialization.
/// </summary>
public abstract class SpellLogic : MonoBehaviour, ISpell
{
    /// <summary>
    /// Initializes and casts the spell when used by a player.
    /// </summary>
    /// <param name="ownerobj">The GameObject of the player casting the spell.</param>
    /// <param name="fwdVector">The forward direction vector of the player's view.</param>
    /// <param name="level">The power level at which the spell is being cast.</param>

    public void PlayerSetup(GameObject ownerobj, Vector3 fwdVector, int level)
    {
        CastSpell(ownerobj, fwdVector, level);
    }

    /// <summary>
    /// Contains the core spell casting logic to be implemented by derived classes.
    /// </summary>
    /// <param name="playerObj">The GameObject of the player casting the spell.</param>
    /// <param name="viewDirectionVector">The direction vector of the player's view.</param>
    /// <param name="castingLevel">The power level of the spell cast.</param>
    public abstract void CastSpell(GameObject playerObj, Vector3 viewDirectionVector, int castingLevel);

    /// <summary>
    /// Called automatically when a spell prefab is created programmatically.
    /// Allows for custom initialization of spell prefabs.
    /// </summary>
    /// <param name="prefab">The GameObject of the created spell prefab.</param>
    public virtual void OnPrefabCreatedAutomatically(GameObject prefab) { }
}
