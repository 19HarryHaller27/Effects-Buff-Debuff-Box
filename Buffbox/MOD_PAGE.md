# Buffbox

**Movable, icon-based buff and debuff strip for Vintage Story 1.22+**

---

## What it is

Buffbox adds a small **HUD strip** of timed effects next to your normal UI. Each active effect shows as an **item icon** in a square, with a gentle **pulse** so you can see time is still ticking. **Right-drag** on the top strip to move the whole bar anywhere on screen.

Out of the box it includes two **test commands** (require `gamemode` privilege):

| Command        | Effect |
|----------------|--------|
| `/slow`        | Heavy walk-speed penalty for **10 seconds** (boot icon). |
| `/attackspeed` | **+25%** mining and ranged weapon speed for **20 seconds** (broken sword icon). |

Effects are applied as **real entity stats**, stored in **synced watched attributes**, cleared on **player death**, and removed automatically when their timer expires.

---

## Vision

We wanted **readable combat and movement feedback** without a separate buff database or MMO-style spell framework. The strip should feel **native to Vintage Story**: icons come from **real item definitions**, timing lives on the **player entity**, and the UI stays **minimal**—one draggable row you can ignore until you care.

Long term, Buffbox is a **foundation**: add new effect type ids, wire server-side stat changes, and (if you want a custom icon) point the client HUD at any item code the game already knows. The goal is **clarity and moddability**, not a kitchen sink of RPG systems.

---

## For modders: how it fits together

### Layout

| Piece | Role |
|-------|------|
| `BuffConstants.cs` | `BuffKeys.Payload` (attribute path), `BuffTypes.*` string ids, `BuffGameplay` stat codes and numeric modifiers. |
| `BuffPayload.cs` | JSON encode/decode of the buff map on the entity (`Newtonsoft.Json` + `JObject`—required for Vintage Story’s in-process mod compiler). |
| `BuffboxServerSystem.cs` | Sets/clears payload, applies `Entity.Stats`, chat commands, death clear, **1s tick** to prune expired entries. |
| `BuffboxClientSystem.cs` | Registers the orthographic HUD renderer. |
| `BuffboxHudRenderer.cs` | Reads payload from **local player entity**, resolves **item stacks** for icons, draws squares and handles drag. |

### Data model

- Watched attribute: **`buffboxData`** (see `BuffKeys.Payload`).
- Value: JSON object whose **keys** are effect type ids (e.g. `slow`, `attackspeed`) and **values** are `{ "s": startUnixMs, "e": endUnixMs }` (short keys to keep packets small).

Server is authoritative: it writes the string and marks the path dirty; the client HUD **reads the same attribute** from the player entity after sync.

### Adding a new buff type (checklist)

1. **`BuffConstants.cs`**  
   - Add a new `BuffTypes.YourId` constant (lowercase string used as JSON key).  
   - Add any **stat layer codes** in `BuffGameplay` if you need unique `Entity.Stats.Set` / `Remove` ids.

2. **`BuffboxServerSystem.cs`**  
   - In `ApplyStatFor` / `RemoveStatFor`, branch on your id and call `e.Stats.Set(...)` / `Remove(...)` with `persistent: false` for temporary combat/movement buffs.  
   - Expose duration via your own API, e.g. `BuffboxServerSystem.SetEffect(player, BuffTypes.YourId, durationMs)`, or register new chat commands / story events / items that call it.

3. **`BuffboxHudRenderer.cs`**  
   - Resolve an icon: `capi.World.GetItem(new AssetLocation("game:your-domain/your-item"))`.  
   - **Important:** item paths use **`/`** between groups (e.g. `clutter-fishing/brokensword`), not a single hyphenated slug unless the game really defines it that way.  
   - Cache a `DummySlot` like boots/sword, then add a `rows.Add` block gated on `map.TryGetValue(BuffTypes.YourId, ...)`.

4. **No `System.Linq` in mod sources** compiled by the game (e.g. `.ToList()` on dictionaries). Use explicit loops or `new List<>` + `for`.

### Public hook for other systems

Other mods or patch code on the **server** can drive the same UI by calling:

```csharp
BuffboxServerSystem.SetEffect(serverPlayer, BuffTypes.Slow, 15_000);
BuffboxServerSystem.ClearEffectType(serverPlayer, BuffTypes.Slow);
BuffboxServerSystem.ClearAllBuffbox(serverPlayer);
```

Use the string ids from `BuffTypes` (or your new constants) so the HUD and stat cleanup stay aligned.

### Building vs in-folder sources

- **Development:** keep `.cs` under `src\` next to `modinfo.json`; Vintage Story compiles them in place.  
- **Deploy:** copy the whole `Buffbox` folder (or sync from your repo) into `%AppData%\VintagestoryData\Mods\Buffbox`.  
- Optional: build `Buffbox.dll` with the provided `.csproj` for a DLL-only install; do not leave stray `.cs` next to the DLL unless you intend hybrid loading.

---

## Credits

**Buffbox** — authors and version in `modinfo.json`.
