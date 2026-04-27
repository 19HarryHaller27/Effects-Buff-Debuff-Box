using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Buffbox;

public class BuffboxServerSystem : ModSystem
{
    private ICoreServerAPI? sapi;
    private long tickListenerId = -1;

    public static void SetEffect(IServerPlayer player, string effectType, int durationMs)
    {
        if (durationMs <= 0 || player.Entity is null)
        {
            return;
        }
        string t = (effectType ?? "").Trim();
        if (t.Length == 0)
        {
            return;
        }
        t = t.ToLowerInvariant();
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var a = player.Entity.WatchedAttributes;
        var map = BuffPayloadCodec.DeserializeOrEmpty(a.GetString(BuffKeys.Payload, null));
        long end = now + durationMs;
        map[t] = new BuffTypeEntry { StartMs = now, EndMs = end };
        a.SetString(BuffKeys.Payload, BuffPayloadCodec.Serialize(map));
        a.MarkPathDirty(BuffKeys.Payload);
        ApplyStatFor(player, t);
    }

    public static void ClearEffectType(IServerPlayer player, string effectType)
    {
        if (player.Entity is null)
        {
            return;
        }
        string t = (effectType ?? "").Trim().ToLowerInvariant();
        if (t.Length == 0)
        {
            return;
        }
        var a = player.Entity.WatchedAttributes;
        var map = BuffPayloadCodec.DeserializeOrEmpty(a.GetString(BuffKeys.Payload, null));
        if (!map.Remove(t))
        {
            return;
        }
        a.SetString(BuffKeys.Payload, map.Count == 0 ? "{}" : BuffPayloadCodec.Serialize(map));
        a.MarkPathDirty(BuffKeys.Payload);
        RemoveStatFor(player, t);
    }

    public static void ClearAllBuffbox(IServerPlayer player)
    {
        if (player.Entity is null)
        {
            return;
        }
        var a = player.Entity.WatchedAttributes;
        a.SetString(BuffKeys.Payload, "{}");
        a.MarkPathDirty(BuffKeys.Payload);
        RemoveStatFor(player, BuffTypes.Slow);
        RemoveStatFor(player, BuffTypes.AttackSpeed);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;
        tickListenerId = api.Event.RegisterGameTickListener(OnServerTick, 1000, 0);
        api.Event.PlayerDeath += OnPlayerDeath;

        api.ChatCommands
            .GetOrCreate("slow")
            .WithDescription("Test slow movement: -75% walk speed for 10s + boot icon. Usage: /slow")
            .RequiresPlayer()
            .RequiresPrivilege("gamemode")
            .HandleWith(OnSlowCommand);

        api.ChatCommands
            .GetOrCreate("attackspeed")
            .WithDescription("Test attack-speed buff: +25% mining & ranged weapon speed for 20s + broken sword icon. Usage: /attackspeed")
            .RequiresPlayer()
            .RequiresPrivilege("gamemode")
            .HandleWith(OnAttackSpeedCommand);
    }

    public override void Dispose()
    {
        if (sapi is not null)
        {
            sapi.Event.PlayerDeath -= OnPlayerDeath;
            if (tickListenerId != -1)
            {
                sapi.Event.UnregisterGameTickListener(tickListenerId);
                tickListenerId = -1;
            }
        }
    }

    private void OnPlayerDeath(IServerPlayer player, DamageSource _)
    {
        if (player is null)
        {
            return;
        }
        ClearAllBuffbox(player);
    }

    private void OnServerTick(float _dt)
    {
        ICoreServerAPI? a = sapi;
        if (a is null)
        {
            return;
        }
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (IServerPlayer? sp in a.Server.Players)
        {
            if (sp is null || sp.Entity is null)
            {
                continue;
            }
            PruneForPlayer(sp, now);
        }
    }

    private static void PruneForPlayer(IServerPlayer player, long nowMs)
    {
        if (player.Entity is null)
        {
            return;
        }
        var a = player.Entity.WatchedAttributes;
        string? raw = a.GetString(BuffKeys.Payload, null);
        if (string.IsNullOrEmpty(raw) || raw == "{}")
        {
            return;
        }
        var map = BuffPayloadCodec.DeserializeOrEmpty(raw);
        var expiredKeys = new List<string>();
        foreach (KeyValuePair<string, BuffTypeEntry> kv in map)
        {
            if (kv.Value.EndMs <= nowMs)
            {
                expiredKeys.Add(kv.Key);
            }
        }
        int removed = 0;
        for (int i = 0; i < expiredKeys.Count; i++)
        {
            string key = expiredKeys[i];
            if (!map.Remove(key))
            {
                continue;
            }
            RemoveStatFor(player, key);
            removed++;
        }
        if (removed == 0)
        {
            return;
        }
        a.SetString(BuffKeys.Payload, map.Count == 0 ? "{}" : BuffPayloadCodec.Serialize(map));
        a.MarkPathDirty(BuffKeys.Payload);
    }

    private static void ApplyStatFor(IServerPlayer player, string effectType)
    {
        if (player.Entity is null)
        {
            return;
        }
        Entity e = player.Entity;
        switch (effectType.ToLowerInvariant())
        {
            case BuffTypes.Slow:
                e.Stats.Set("walkspeed", BuffGameplay.StatCodeSlow, BuffGameplay.WalkSpeedModifier, persistent: false);
                break;
            case BuffTypes.AttackSpeed:
                e.Stats.Set("miningSpeedMul", BuffGameplay.StatCodeAttack, BuffGameplay.MiningSpeedBonus, persistent: false);
                e.Stats.Set("rangedWeaponsSpeed", BuffGameplay.StatCodeAttack, BuffGameplay.RangedSpeedBonus, persistent: false);
                break;
        }
    }

    private static void RemoveStatFor(IServerPlayer player, string effectType)
    {
        if (player.Entity is null)
        {
            return;
        }
        Entity e = player.Entity;
        switch (effectType.ToLowerInvariant())
        {
            case BuffTypes.Slow:
                e.Stats.Remove("walkspeed", BuffGameplay.StatCodeSlow);
                break;
            case BuffTypes.AttackSpeed:
                e.Stats.Remove("miningSpeedMul", BuffGameplay.StatCodeAttack);
                e.Stats.Remove("rangedWeaponsSpeed", BuffGameplay.StatCodeAttack);
                break;
        }
    }

    private static TextCommandResult OnSlowCommand(TextCommandCallingArgs args)
    {
        IPlayer? pl = args.Caller.Player;
        if (pl is not IServerPlayer splayer)
        {
            return TextCommandResult.Error("Not a server player", "noplayer");
        }
        if (splayer.Entity is null)
        {
            return TextCommandResult.Error("No entity", "noentity");
        }
        SetEffect(splayer, BuffTypes.Slow, 10_000);
        return TextCommandResult.Success("Slow movement 10s: walk speed ×0.25 (boot icon).");
    }

    private static TextCommandResult OnAttackSpeedCommand(TextCommandCallingArgs args)
    {
        IPlayer? pl = args.Caller.Player;
        if (pl is not IServerPlayer splayer)
        {
            return TextCommandResult.Error("Not a server player", "noplayer");
        }
        if (splayer.Entity is null)
        {
            return TextCommandResult.Error("No entity", "noentity");
        }
        SetEffect(splayer, BuffTypes.AttackSpeed, 20_000);
        return TextCommandResult.Success("Attack-speed test 20s: miningSpeedMul +25%, rangedWeaponsSpeed +25% (broken sword icon).");
    }
}
