using BlackMagicAPI.Network;
using System.Collections;
using UnityEngine;

namespace BlackMagicAPI.Modules.Spells;

/// <summary>
/// Abstract base class for spell behavior logic.
/// Provides the core interface for spell casting functionality and initialization.
/// </summary>
public abstract class SpellLogic : MonoBehaviour, ISpell
{
    internal static Dictionary<string, List<SpellLogic>> Instances = [];

    [SerializeField]
    [Tooltip("Spelldata Fullname, (DO NOT SET)")]
    internal string? SpellDataTypeName;

    [SerializeField]
    [Tooltip("Keep item on death, (DO NOT SET)")]
    internal bool KeepOnDeath;

    /// <summary>
    /// Gets whether this instance is a prefab template or an active spell instance.
    /// </summary>
    public bool IsPrefab { get; internal set; } = true;

    /// <summary>
    /// Initializes the spell instance and sets up type references asynchronously.
    /// </summary>
    protected virtual void Awake()
    {
        StartCoroutine(CoAwake());
    }

    /// <summary>
    /// Cleans up the spell instance by removing it from tracking dictionaries when destroyed.
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (SpellDataTypeName != null && Instances.TryGetValue(SpellDataTypeName, out var list))
        {
            list.Remove(this);

            if (list.Count <= 0)
            {
                Instances.Remove(SpellDataTypeName);
            }
        }
    }

    private IEnumerator CoAwake()
    {
        while (SpellDataTypeName == null)
        {
            yield return null;
        }

        float wait = 0f;
        while (IsPrefab)
        {
            wait += Time.deltaTime;
            if (wait > 5f)
            {
                yield break;
            }

            yield return null;
        }

        if (!Instances.ContainsKey(SpellDataTypeName))
        {
            Instances[SpellDataTypeName] = [];
        }
        Instances[SpellDataTypeName].Add(this);
    }

    /// <inheritdoc/>
    public void PlayerSetup(GameObject ownerobj, Vector3 fwdVector, int level) { }

    /// <summary>
    /// Contains the core spell casting logic to be implemented by derived classes.
    /// </summary>
    /// <param name="playerObj">The GameObject of the player casting the spell.</param>
    /// <param name="page">The PageController containing spell information.</param>
    /// <param name="spawnPos">The position where the spell should be spawned.</param>
    /// <param name="viewDirectionVector">The direction vector of the player's view.</param>
    /// <param name="castingLevel">The power level of the spell cast.</param>
    public abstract void CastSpell(GameObject playerObj, PageController page, Vector3 spawnPos, Vector3 viewDirectionVector, int castingLevel);

    /// <summary>
    /// Virtual method for handling item-specific usage logic for spell page.
    /// </summary>
    /// <param name="itemOwner">The player using the item</param>
    /// <param name="page">The PageController containing spell information</param>
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
    /// Castor writes data to a <see cref="
    /// "/> for serialization to send to clients.
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

    /// <summary>
    /// Cleans up and destroys the spell GameObject.
    /// </summary>
    public void DisposeSpell()
    {
        Destroy(gameObject);
    }
}
