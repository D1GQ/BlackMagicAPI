using BlackMagicAPI.Modules.Spells;
using System.Reflection;

namespace BlackMagicAPI.Managers;

enum CompatibilityResult
{
    Successful,
    NotAssignable,
    NoProperty,
    OldVersion,
    Error
}

internal class CompatibilityManager
{
    // BE SURE TO UPDATE BlackMagicAPI.csproj
    internal static Version COMPATIBILITY_VERSION { get; } = typeof(CompatibilityManager).Assembly.GetName().Version;

    internal static Version GetReferencedVersion(Type type) => type?.Assembly?.GetReferencedAssemblies()?.FirstOrDefault(assembly => assembly.Name == "BlackMagicAPI")?.Version ?? new Version("0.0.0.0");

    private static bool CheckCompatibility(Version version)
    {
        if (version == COMPATIBILITY_VERSION)
        {
            return true;
        }

        return false;
    }

    private static bool CheckForProperty(Type type)
    {
        var property = type.GetProperty("CompatibilityVersion", BindingFlags.Public | BindingFlags.Instance);

        if (property == null)
        {
            return false;
        }

        return true;
    }

    internal static CompatibilityResult CheckSpellCompatibility(Type spellDataType)
    {
        if (!typeof(SpellData).IsAssignableFrom(spellDataType))
            return CompatibilityResult.NotAssignable;

        if (!CheckForProperty(spellDataType))
        {
            return CompatibilityResult.NoProperty;
        }

        try
        {
            if (Activator.CreateInstance(spellDataType) is SpellData data)
            {
                if (CheckCompatibility(data.CompatibilityVersion))
                {
                    return CompatibilityResult.Successful;
                }
            }
            return CompatibilityResult.OldVersion;
        }
        catch
        {
            return CompatibilityResult.Error;
        }
    }

    internal static CompatibilityResult CheckItemCompatibility(Type itemDataType)
    {
        if (!typeof(ItemData).IsAssignableFrom(itemDataType))
            return CompatibilityResult.NotAssignable;

        if (!CheckForProperty(itemDataType))
        {
            return CompatibilityResult.NoProperty;
        }

        try
        {
            if (Activator.CreateInstance(itemDataType) is ItemData data)
            {
                if (CheckCompatibility(data.CompatibilityVersion))
                {
                    return CompatibilityResult.Successful;
                }
            }
            return CompatibilityResult.OldVersion;
        }
        catch
        {
            return CompatibilityResult.Error;
        }
    }
}
