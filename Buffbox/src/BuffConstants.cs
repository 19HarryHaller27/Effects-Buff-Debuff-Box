namespace Buffbox;

public static class BuffKeys
{
    public const string Payload = "buffboxData";
}

public static class BuffTypes
{
    public const string Slow = "slow";
    public const string AttackSpeed = "attackspeed";
}

/// <summary>Gameplay modifiers; HUD uses the same type ids in <see cref="BuffKeys.Payload"/>.</summary>
public static class BuffGameplay
{
    public const string StatCodeSlow = "buffbox_slow";
    public const string StatCodeAttack = "buffbox_attackspeed";

    /// <summary>Weighted sum with base 1: 1 + (-0.75) = 0.25 → 75% slower movement.</summary>
    public const float WalkSpeedModifier = -0.75f;

    /// <summary>Weighted sum with base 1: faster block breaking / tool use.</summary>
    public const float MiningSpeedBonus = 0.25f;

    /// <summary>Weighted sum with base 1: faster bow/sling draw (rangedWeaponsSpeed).</summary>
    public const float RangedSpeedBonus = 0.25f;
}
