using UnityEngine;

namespace BlackMagicAPI.Modules.Soups;

/// <summary>
/// Abstract base class for soup effect behavior logic.
/// Provides the core interface for soup effect functionality and application.
/// </summary>
public abstract class SoupEffect : MonoBehaviour
{
    internal int Id { get; set; }

    /// <summary>
    /// Applies the soup effect to the specified player.
    /// </summary>
    /// <param name="player">The PlayerMovement component of the player to apply the effect to.</param>
    /// <remarks>
    /// This must be implemented by derived classes to define the specific effect behavior.
    /// </remarks>
    public abstract void ApplyEffect(PlayerMovement player);

    /// <summary>
    /// Called automatically when a soup effect prefab is created programmatically.
    /// Allows for custom initialization of soup effect prefabs.
    /// </summary>
    /// <param name="prefab">The GameObject of the created soup effect prefab.</param>
    /// <remarks>
    /// Override this method to perform custom initialization when the prefab is created.
    /// </remarks>
    public virtual void OnPrefabCreatedAutomatically(GameObject prefab) { }

    /// <summary>
    /// Cleans up and destroys the soup effect GameObject.
    /// </summary>
    /// <remarks>
    /// This method handles proper cleanup of the soup effect instance.
    /// </remarks>
    public void DisposeEffect()
    {
        Destroy(gameObject);
    }
}