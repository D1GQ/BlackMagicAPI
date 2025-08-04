using BlackMagicAPI.Modules.Spells;
using UnityEngine;

namespace BlackMagicAPI.Modules;

internal class MySpellData : SpellData
{
    public override string Name => "check";

    public override float Cooldown => 10;

    public override Color GlowColor => Color.white;

    public override bool DebugForceSpawn => true;
}
