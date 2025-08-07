using BlackMagicAPI.Network;
using UnityEngine;

namespace BlackMagicAPI.Modules.Spells;

/// <summary>
/// Abstract base class for spell behavior logic.
/// Provides the core interface for spell casting functionality and initialization.
/// </summary>
public abstract class SpellLogic : MonoBehaviour, ISpell
{
    public void PlayerSetup(GameObject ownerobj, Vector3 fwdVector, int level) { }

    /// <summary>
    /// Contains the core spell casting logic to be implemented by derived classes.
    /// </summary>
    /// <param name="playerObj">The GameObject of the player casting the spell.</param>
    /// <param name="viewDirectionVector">The direction vector of the player's view.</param>
    /// <param name="castingLevel">The power level of the spell cast.</param>
    public abstract void CastSpell(GameObject playerObj, PageController page, Vector3 spawnPos, Vector3 viewDirectionVector, int castingLevel);

    /// <summary>
    /// Virtual method for handling item-specific usage logic for spell page.
    /// </summary>
    /// <param name="itemOwner">The player using the item</param>
    /// <remarks>
    /// Note that this code executes within the SpellLogic prefab, so avoid modifying anything within the prefab itself!
    /// </remarks>
    public virtual void OnPageItemUse(GameObject itemOwner, PageController page) { }

    /// <summary>
    /// Called automatically when a spell prefab is created programmatically.
    /// Allows for custom initialization of spell prefabs.
    /// </summary>
    /// <param name="prefab">The GameObject of the created spell prefab.</param>
    public virtual void OnPrefabCreatedAutomatically(GameObject prefab) { }

    /// <summary>
    /// Castor writes data to a <see cref="DataWriter"/> for serialization to send to clients.
    /// </summary>
    /// <param name="dataWriter">The writer used to serialize data.</param>
    /// <param name="page">The page controller associated with the data.</param>
    /// <param name="playerObj">The player GameObject to serialize.</param>
    /// <param name="spawnPos">The spawn position of the player.</param>
    /// <param name="viewDirectionVector">The view direction of the player.</param>
    /// <param name="level">The current level or stage.</param>
    public virtual void WriteData(DataWriter dataWriter, PageController page, GameObject playerObj, Vector3 spawnPos, Vector3 viewDirectionVector, int level) { }

    /// <summary>
    /// Clients including Castor Synchronizes of values from WriteData received from Castor.
    /// </summary>
    /// <param name="values">An array of objects containing the data to sync from WriteData.</param>
    public virtual void SyncData(object[] values) { }
}
