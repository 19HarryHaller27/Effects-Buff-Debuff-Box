using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Buffbox;

public sealed class BuffboxHudRenderer : IRenderer
{
    private const float IconSize = 42f;
    private const float IconGap = 6f;
    private const float PadX = 4f;
    private const float HandleH = 8f;
    private const float OscillationPeriodMs = 4500f;
    private const int AlphaMin = 96;
    private const int AlphaMax = 255;

    private readonly ICoreClientAPI capi;
    private float posX = 24f;
    private float posY = 96f;
    private int lastMouseX;
    private int lastMouseY;
    private bool dragging;
    private bool lastRight;
    private LoadedTexture fillTexture;
    private DummySlot? slotBoots;
    private DummySlot? slotSword;
    private bool stacksResolved;

    public BuffboxHudRenderer(ICoreClientAPI capi)
    {
        this.capi = capi;
        fillTexture = new LoadedTexture(capi)
        {
            Width = 1,
            Height = 1
        };
    }

    public double RenderOrder => 1.12;

    public int RenderRange => 0;

    public void OnRenderFrame(float _dt, EnumRenderStage stage)
    {
        if (stage != EnumRenderStage.Ortho)
        {
            return;
        }
        IClientPlayer? player = capi.World.Player;
        if (player?.Entity is null)
        {
            return;
        }
        EnsureStacksResolved();
        string? json = player.Entity.WatchedAttributes.GetString(BuffKeys.Payload, null);
        if (string.IsNullOrEmpty(json) || json == "{}")
        {
            return;
        }
        var map = BuffPayloadCodec.DeserializeOrEmpty(json);
        if (map.Count == 0)
        {
            return;
        }
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var rows = new List<(string key, BuffTypeEntry ent, ItemSlot slot)>();
        if (slotBoots is not null && map.TryGetValue(BuffTypes.Slow, out BuffTypeEntry? slowEnt))
        {
            if (slowEnt is not null && slowEnt.EndMs > now)
            {
                rows.Add((BuffTypes.Slow, slowEnt, slotBoots));
            }
        }
        if (slotSword is not null && map.TryGetValue(BuffTypes.AttackSpeed, out BuffTypeEntry? atkEnt))
        {
            if (atkEnt is not null && atkEnt.EndMs > now)
            {
                rows.Add((BuffTypes.AttackSpeed, atkEnt, slotSword));
            }
        }
        if (rows.Count == 0)
        {
            return;
        }

        IRenderAPI rapi = capi.Render;
        EnsureFillTexture(rapi);
        int mx = capi.Input.MouseX;
        int my = capi.Input.MouseY;
        bool right = capi.Input.MouseButton.Right;
        float contentW = rows.Count * IconSize + (rows.Count - 1) * IconGap;
        float panelW = PadX * 2 + contentW;
        bool onHandle = mx >= posX && my >= posY && mx < posX + panelW && my < posY + HandleH;
        if (right)
        {
            if (onHandle && !lastRight)
            {
                dragging = true;
                lastMouseX = mx;
                lastMouseY = my;
            }
            if (dragging)
            {
                posX += mx - lastMouseX;
                posY += my - lastMouseY;
                lastMouseX = mx;
                lastMouseY = my;
            }
        }
        else
        {
            dragging = false;
        }
        lastRight = right;

        const float z = 200f;
        // Invisible grab strip: same rect as onHandle (no GL draw).

        float iconY = posY + HandleH + PadX;
        float x0 = posX + PadX;
        for (int i = 0; i < rows.Count; i++)
        {
            (_, BuffTypeEntry ent, ItemSlot slot) = rows[i];
            float ix = x0 + i * (IconSize + IconGap);
            float phase = (float)((now - ent.StartMs) * (Math.PI * 2.0 / OscillationPeriodMs));
            float wave = 0.5f + 0.5f * (float)Math.Sin(phase);
            int a = (int)(AlphaMin + (AlphaMax - AlphaMin) * wave);
            int argbBack = ColorUtil.ColorFromRgba(40, 28, 22, (int)(a * 0.55f));
            Vec4f rgbaBack = ColorUtil.ToRGBAVec4f(argbBack);
            rapi.RenderTexture(fillTexture.TextureId, ix, iconY, IconSize, IconSize, z, rgbaBack);
            int tint = ColorUtil.ColorFromRgba(255, 255, 255, a);
            capi.Render.RenderItemstackToGui(slot, ix + 3, iconY + 3, 100 + z, IconSize - 6, tint, shading: true, rotate: false, showStackSize: false);
        }
    }

    private void EnsureStacksResolved()
    {
        if (stacksResolved)
        {
            return;
        }
        stacksResolved = true;
        CollectibleObject? boots = capi.World.GetItem(new AssetLocation("game:clothes-foot-soldier-boots"));
        if (boots is not null)
        {
            slotBoots = new DummySlot(new ItemStack(boots));
        }
        // Item code is clutter-fishing/brokensword (slash), not clutter-fishing-brokensword.
        CollectibleObject? sword = capi.World.GetItem(new AssetLocation("game:clutter-fishing/brokensword"));
        if (sword is not null)
        {
            slotSword = new DummySlot(new ItemStack(sword));
        }
    }

    private void EnsureFillTexture(IRenderAPI rapi)
    {
        if (fillTexture.TextureId > 0)
        {
            return;
        }
        int[] white = { ColorUtil.ColorFromRgba(255, 255, 255, 255) };
        rapi.LoadOrUpdateTextureFromRgba(white, linearMag: false, clampMode: 0, ref fillTexture);
    }

    public void Dispose()
    {
        fillTexture.Dispose();
    }
}
