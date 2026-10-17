# HOLLOW LIGHT (NASCON-GJ) — Team Workflow

**Hollow Light** is a first-person, low-poly stylized horror puzzle game. The player carries a **timed fire** through a tomb, using light and the shadows it casts to block **shadow entities** that live on walls. A **guiding light** teaches the mechanics. The game ends in a chase and a twist: the player becomes the next **Void**.

- Full design: [DOCUMENTATION/Game Desgin [NASCON].docx](DOCUMENTATION/)
- Art direction: `DOCUMENTATION/CHARACTER SHEETS` and `DOCUMENTATION/ENVIRONMENT REFERENCES`. Warm orange torchlight, purple-black darkness, flat low-poly shapes, minimal textures.

**Unity 6000.6.5f1 · URP · New Input System.** Use exactly this editor version.

---

## 1. First-time setup (every dev)

```bash
git lfs install
git clone <repo>
# Smart merge for scenes/prefabs (the path depends on where your Unity editor is installed):
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'D:/UNITY EDITORS/6000.6.5f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
```

Open the project in Unity Hub, then use **NASCON > Scenes > Sandbox Dev X** to open your own sandbox.

---

## 2. Project structure

```
NASCON-GJ/
├── DOCUMENTATION/          GDD + art references (read-only reference)
├── MODULES/                Inbox for loose / WIP scripts. Not compiled by Unity. See MODULES/README.md
├── WORKFLOW.md             This file
└── Assets/
    ├── _Project/           ALL our content lives here
    │   ├── Art/            Animations, Materials, Models/{Characters,Environment,Props}, Shaders, Textures, VFX, UI, Fonts
    │   ├── Audio/          Music, SFX, Ambience, Voice, Mixers
    │   ├── Input/          InputSystem_Actions.inputactions
    │   ├── Prefabs/        Player, Items, Shadows, GuidingLight, Environment, Puzzles, Lighting, UI, Systems
    │   ├── Scenes/         Core, Cutscenes, Levels, Sandbox
    │   ├── ScriptableObjects/  Items, Dialogue, Entities, Settings
    │   ├── Scripts/        (see below)
    │   ├── Settings/Volumes    Post-processing profiles
    │   └── Timeline/       Cutscene timelines
    ├── Settings/           URP pipeline assets (template, leave here)
    └── (third-party packages go in Assets/ThirdParty/<Name>)
```

### Scripts — one folder per system, namespace `NasconGJ.<Folder>`

| Folder | Covers (from GDD) |
|---|---|
| `Core/` | GameManager, SceneLoader, SceneNames, GameEvents, GameTags. **Shared — announce before editing** |
| `Player/` | FPS controller, camera, footsteps, hands |
| `Fire/` | Timed fire, transfer (hold), cover with hands (hold, timed), `IFlammable`, `ILightSource`, `LightSourceRegistry` |
| `Items/` | Carryable flammable items, 2-slot inventory swap, drop as standalone light |
| `Interaction/` | `IInteractable`, raycast interactor, HUD prompt |
| `Shadows/` | Shadow projection on surfaces, light-blocker shadows that block enemies |
| `Entities/` | Shadow enemies, RUSH, seeker spawned on death |
| `GuidingLight/` | Guiding light behaviour + Undertale-style typewriter dialogue |
| `Puzzles/` | Narrow Path, Multi Story, Dummy Shadows, Crystal |
| `Death/` | Darkness state, delayed death, sound-only phase |
| `Audio/` | Audio manager, ambience, 3D sound cues |
| `UI/` | Main menu, HUD, pause, fade |
| `Cutscenes/` | Code-driven cutscenes. `Cutscenes/Scroll/` is the 2D story-scroll prologue (`ScrollDirector`, `InkPuppet`, `InkPanel`, `InkTransitionOverlay`, `ScrollLook`). `CutsceneUI` (letterbox, captions, title, hold-to-skip). The old 3D `PrologueDirector`, `CutsceneActor`, `Villager` are no longer used by C01 |
| `Utilities/` | Generic helpers |
| `Modules/` | **Auto-generated** mirror of `MODULES/`. Don't edit by hand |
| `Editor/` | Editor tools (`NASCON/` menu). `Editor/Generation/` = procedural asset + scene generator |

### Shared contracts (already in place)

Build against these so systems written by different devs plug together:

- `GameEvents`: global event bus (`FireTransferred`, `FireExtinguished`, `FireCovered`, `PlayerDied`, `CheckpointReached`, `DialogueStarted/Finished`, `PuzzleSolved`). Subscribe in `OnEnable`, unsubscribe in `OnDisable`.
- `IFlammable`: anything that can hold the player's fire.
- `ILightSource` + `LightSourceRegistry`: every gameplay light registers itself; shadow AI/puzzles call `LightSourceRegistry.IsPointLit(pos)`.
- `IInteractable`: anything the player can press Interact on.
- `SceneLoader.Load(SceneNames.X)` / `SceneLoader.LoadNext()`: never hardcode scene strings.
- `GameManager` auto-spawns, so **you can press Play in any scene** without going through Boot.

### Tags & layers (constants in `GameTags` / `GameLayers`)

Tags: `Player, Interactable, Flammable, LightSource, GuidingLight, ShadowEnemy, Checkpoint`
Layers: `6 Player, 7 Interactable, 8 ShadowSurface, 9 ShadowEnemy, 10 LightBlocker, 11 Ground`

### Input (`Assets/_Project/Input/InputSystem_Actions`)

| Action | KB/M | Gamepad |
|---|---|---|
| Move / Look / Jump / Sprint / Crouch | WASD / Mouse / Space / Shift / C | sticks / A / L3 / B |
| Interact (pick up / talk / advance dialogue) | E | Y |
| TransferFire (hold) | F | RB |
| CoverFire (hold) | Right Mouse | LT |
| Drop | G | D-pad down |
| Previous / Next item (2-slot swap) | 1 / 2 | D-pad |

---

## 3. Scenes

| Build # | Scene | Content |
|---|---|---|
| 0 | `Core/Boot` | Bootstrapper → loads MainMenu |
| 1 | `Core/MainMenu` | Menu |
| 2 | `Cutscenes/C01_Prologue` | 2D ink-scroll puppets, ~60 s: the elder's tale of the Void (the past is paper) |
| 2b | `Cutscenes/C01b_PrologueTown` | 3D, ~57 s: busy town → rooftop talk with the sister → the Void → shockwave → the one left → the tomb → hard black → memory fall (`MemoryFall`, `NASCON/MemoryGlitch`, `MUS_MemoryFall`) → L01. Built by `LevelBuilder.PrologueTown` (`BuildPrologueTownOnly()`) |
| 3 | `Levels/L01_TombEntrance` | Wake up in narrow space, meet guiding light, learn fire |
| 4 | `Levels/L02_NarrowPath` | Puzzle: drop a light item to hold back the shadow, reach exit |
| 5 | `Levels/L03_MultiStory` | Puzzle: free the shadow upward, drop down to escape |
| 6 | `Levels/L04_DummyShadows` | Puzzle: find the real enemy among prop shadows, block it |
| 7 | `Levels/L05_Crystal` | Puzzle: put out every torch and your own fire, take the crystal |
| 8 | `Levels/L06_Escape` | Chase to the bright light |
| 9 | `Cutscenes/C02_Ending` | Energy restores the town; player becomes the Void |
| — | `Sandbox/Sandbox_DevA/B/C` | Personal test scenes, not in build |

Each gameplay scene has the same hierarchy: `SYSTEMS / ENVIRONMENT (Kit, Props_LightBlockers, Art) / LIGHTING (Braziers_Torches, Volumes) / GAMEPLAY (PlayerSpawn, GuidingLight, Items, ShadowEnemies, Puzzle, Checkpoints, PF_Player) / UI (EventSystem, PF_HUD) / AUDIO`. Keep it that way.

`L01` is the playable vertical slice. `L02`–`L06` are 12×12 greybox rooms with the player, braziers, an exit and a note on the wall describing the puzzle to build. The main menu's Play button goes straight to L01 until the prologue cutscene exists (`MainMenuController.firstScene`).

Levels are separate scenes on purpose, so **each dev can own a different level at the same time** with zero scene conflicts.

---

Full cutscene plan (C01 → C02 ending, L06 escape, assets needed): `DOCUMENTATION/CUTSCENE_PLAN.md`.

## 3b. What's already playable (vertical slice)

Open `L01_TombEntrance` (or your sandbox) and press Play. **MainMenu → "Enter the Tomb"** also works.

**Core loop:** explore by torchlight → fire burns down → pass the flame to something new → avoid shadows → reach the exit → next level. If your fire dies, it's darkness and an unseen seeker. If you die, the level reloads.

| System | Script | What it does |
|---|---|---|
| Movement | `Player/FirstPersonController` | Walk / sprint / crouch (with ceiling check) / jump, mouse + gamepad look, head bob, `Footstep` event + optional clips |
| Input | `Player/PlayerInputReader` | The only place that reads the Input System. Add new actions here |
| Interaction | `Interaction/PlayerInteractor` | Camera raycast → `IInteractable` (E) and the flammable you're looking at |
| Timed fire | `Fire/FlammableItem` | Burn timer, flicker, low-fuel dimming, covered dimming, registers as an `ILightSource`. Braziers = `infiniteBurn`. `IntensityScale` dims items lying on the ground (set by `CarryableItem`) |
| Burning away | `Fire/BurnDown` | Torch head / stick / candle wax visibly shortens from the top and blackens as fuel drops; an ember cap rides on top and the flame follows it down; a burnt stub is left when it dies. Items are built as handle + `Fuel` child + `CharCap` |
| Item readability | `Items/CarryableItem` | Warm rim light on items lying in the world so they read on dark floors (off while held) |
| Transfer / cover | `Fire/FireCarrier` | Hold **F** to pass the flame (to a looked-at item or your other slot). Hold **RMB** to cover it: limited time, then it burns you and is forced open. Moving while covered is slower |
| Inventory | `Items/PlayerInventory`, `Items/CarryableItem` | 2 slots, **1/2** swap (the holstered item keeps burning), **G** drops the item as a standalone light |
| Death | `Death/PlayerDeath` | No player fire left → `FireExtinguished` → a seeker spawns 14–26 m away and walks toward you (attach a looping clip to `SeekerAudio`) → death → reload. `Kill(reason)` for anything else |
| Shadows | `Entities/ShadowEnemy`, `ShadowSurfaceSkin`, `ShadowLunge` | Crawls across any wall/floor/pillar within `leashRadius` of where it starts (wall crawl, corner wrap, floor flatten). Lurk → Stalk → Windup → Lunge; refuses directly lit points and **recoils** (contorted pose, dissolving smoke) when light reaches it. The sprite is draped over the geometry every frame, so it bends round corners like a real shadow. The kill is a lunge off the wall into the camera, maw open. `blockedByCastShadows` = the tentative GDD rule |
| Shadow body | `Entities/ShadowBody`, `Art/Shaders/ShadowGlob` | The living mass of every shadow: ~18 wobbling tar blobs on a rough skeleton (pool, robe, chest, head with horns, two arms), burning eyes, tar droplets. Blobs drift and swell on their own so the shape never settles; the front arm reaches for you when it stalks; it balls up and burns away in light. On walls it lies against the surface; on floors it rises up and stands. The flat sprite underneath is now just its faint stain |
| Shadow decor | `Entities/ShadowFigure` | Non-hostile: frozen townsfolk silhouettes (3 poses) and the giant Void relief (`PF_ShadowVoidRelief`) |
| Procedural animation | `Player/ProceduralHumanoid` | **No animation clips.** Builds the pose every frame in humanoid muscle space: breathing, idle weight shift, **planted-foot walking** (each foot locks to the ground, then steps in an arc via leg IK; cadence from real ground speed, so no sliding), counter-swinging arm, crouch, look pitch, then analytic two-bone IK for the hands (`TwoBoneIK`). Works on any humanoid rig. Add new moves here as signals, not as clips |
| First-person body | `Player/FirstPersonBody` | The main character is the player's body, driven by `ProceduralHumanoid`: right hand IK'd to the held item (glued into the palm), left hand rises to shield the flame, neck hidden, legs walk with your speed, body sinks when crouching, reach auto-fitted from arm length. Look down to see yourself |
| Torch-bearer pose | `Player/TorchBearerPose` | Menu/cutscene version: procedural idle, torch raised by IK, head turns to a look target |
| L01 onboarding | `Tutorial/IntroDirector`, `TutorialZone`, `InterestGlint` | Scripts the first level beat by beat so new players can't get lost or die: invisible gates stay closed until the lesson is done (take the torch / take the burning stick), tutorial flames have a fuel floor until the hall, the item you need pulses with a pale glint, and the guiding light is held at its stop until you're ready (`GuidingLight.Hold`) |
| Objective + hints | `UI/ObjectiveUI`, `UI/HintUI`, `Tutorial/TutorialHint` | Top-left objective line ("Light the stick") with a chime. The hint card (key cap + short sentence) fades in after a chime, stays until you actually do it, and sits above the dialogue band. `TutorialHint` is the generic trigger-volume version for sandboxes. HUD warns once when a flame drops below 30% |
| Wind draft | `Fire/WindDraftZone` | Uncovered flames gutter and burn down fast inside; covering protects them (teaches cover before RUSH). `extinguishUncovered` makes it lethal |
| Intro | `Player/WakeUpSequence` | L01 start: lying dazed on the floor looking up the shaft, then getting up |
| Guiding light | `GuidingLight/GuidingLight` + `DialogueData` | Route of stops, 1–2 short lines each with the speaker name "THE GUIDING LIGHT", auto-advancing unless `Hold` is set. Shrinks when the camera is close. Also a light source that shadows avoid |
| Music | `Audio/MusicDirector` | Uses the team's tracks from `DOCUMENTATION/Music And Audio` (copied to `Audio/Music/`): `MUS_MainAmbience` always, `MUS_Presence` fades in with threat (a hunting shadow within 12 m, or darkness). The menu plays `MUS_MainAmbience` |
| Audio | `Audio/FlameAudio`, `Audio/RandomOneShots` | Fire crackle follows each flame (muffled when covered). Drips in the dark. Footsteps, heartbeat, torch crackle/ignite and wind are **real CC0 recordings** from `Audio/CC0/` (sources in `CREDITS.md`, loaded via `AudioGenerator.Real(file, synthFallback)`). Shadow whispers, lunge, drips, drone and UI are still synthesised into `Audio/Generated` |
| UI | `UI/HUDController`, `DialogueUI`, `HintUI`, `ScreenFader`, `PauseMenu` | Prompt, fuel bar, cover meter, transfer ring, messages, typewriter dialogue (pale-cyan band), key hints, fades, Esc pause |
| Prologue | `Cutscenes/Scroll/ScrollDirector` (+ `InkPuppet`, `InkPanel`, `InkTransitionOverlay`) | ~4 min story scroll in the style of the Kung Fu Panda backstory sequences: ink-wash panels (gold sky, teal karst, Tsushima village, calligraphy + red seal) with jointed shadow puppets, cut together with ink-drop / brush-sweep transitions. Beats follow the narration script (captions via `CutsceneUI`; swap in recorded VO later): lantern → town life → elder by the fire → legend landscape → children's shadows become the Void → the Void in the dark → it reaches → nobody believes → seasons pass → lamps die → it passes over → the town emptied, shadows burned into walls → the bearer → the hills → the tomb (lightning reveals those who came before) → the fall → L01. Hold Space/Enter/Esc to skip. Everything is generated: `InkArtGenerator` paints the art, `LevelBuilder.Prologue` lays out the 6 panels |
| Main menu | `UI/MainMenuController`, `MenuButtonFX`, `MenuCameraDrift` | Title and menu only (no tagline). **Begin** plays the prologue. Staged intro, animated items (crystal marker, ember underline, sound), Settings (sensitivity, volume, brush strokes, fullscreen) and Credits panels, Esc to go back, drifting camera over the torch-bearer and the Void |
| Settings | `Core/GameSettings` | PlayerPrefs-backed; applied on boot |
| Level flow | `Puzzles/LevelExit` | Trigger → fade → `SceneLoader.LoadNext()`; optional `requiredPuzzleId` gate |

**L01 layout** (the tutorial is the level): *The Fall* (wake up facing your dropped torch, pick it up) → *The Crawl* (ceiling at shoulder height, **C** to crouch) → *The Narrows* (tight corridor, moonlit crack over a stick: **F** to pass the flame before yours dies) → *The Draft* (wind from a fissure: **RMB** to shield it) → *The Hall of Echoes* (vast, dark, 16 m pillars, light shafts, frozen townsfolk on the walls, the Void relief, two living shadows; light braziers to push them back) → exit.

**Prefabs to drag in:** `PF_Player`, `PF_HUD` (+ an EventSystem), `PF_Torch / PF_Stick / PF_Candle`, `PF_Brazier_Lit / _Unlit`, `PF_GuidingLight`, `PF_ShadowEnemy` (blue axis pointing out of the surface, feet where you want them), `PF_ShadowTownsfolk`, `PF_ShadowVoidRelief`, `PF_LightShaft`, `PF_DustMotes`, and every piece in `Prefabs/Environment/Kit` (now incl. rocks, rock wall, collapsed slab, chain).

## 3c. Procedural content generator (`NASCON > Generate`)

All environment art is **generated by code** in `Scripts/Editor/Generation/`. There are no external models yet.

| Step | Makes |
|---|---|
| 1. Textures + Materials | Stone grit, soft particle, HUD ring and shadow silhouette textures; ink-scroll art for the prologue (`InkArtGenerator` → `Art/Textures/Generated/Ink`: landscapes, village, puppets, the Void, calligraphy); URP materials; `VP_Tomb` / `VP_Scroll` post-processing |
| 2. Environment Kit | 4 m modular tomb kit: wall, doorway, floor, ceiling, pillar (+broken), stairs, blocks, rubble, altar, obelisk; town pieces for the prologue: 3 timber houses with warm windows, lamp post |
| 3. Gameplay + UI Prefabs | Torch / stick / candle (burnable), braziers, guiding light, shadow creature, 4 placeholder villagers, player, HUD, main-menu + cutscene canvases, dialogue assets |
| 4. Rebuild Scenes | **Overwrites** MainMenu, C01_Prologue, L01–L06 and the sandboxes. To iterate on the cutscene alone, call `LevelBuilder.BuildPrologueOnly()` |

- Steps 1–3 update assets **in place**: GUIDs stay the same, so scenes and prefabs that reference them don't break. Tweak a generator and re-run it freely.
- **Once someone hand-edits a level, stop running step 4**, or remove that level from `LevelBuilder.BuildAll()`.
- Want a new kit piece? Add a `Make(...)` line in `EnvironmentKitGenerator` using `MeshBuilder.Box/Prism`.
- Lighting values: torch intensity 9 / range 10, brazier 10 / 10, guiding light 5 / 8. Ambient `#09070D`. Keep new lights in that range.

| (step 1 also) | Shadow sprite sheets (`ShadowSheetGenerator`: 12 creature frames + 3 townsfolk), procedural audio (`AudioGenerator`), TMP fonts Cinzel/EB Garamond (`FontLibrary`), UI sprites |
| 5. Character Prefabs | Every Meshy FBX in `Art/Models/Characters/<Name>/` → `M_<Name>` (stylized material using its base-colour texture) + `PF_<Name>` scaled to 1.6 m |

## 3d. Art style: shaders & rendering (`Art/Shaders/`)

The painted look of the concept boards comes from three custom shaders. **Use them for every new asset.** Never use URP/Lit.

| Shader | Use for | Key knobs |
|---|---|---|
| `NASCON/StylizedLit` | All opaque geometry: environment, props, characters | `Light Bands` (posterised torchlight), `Shadow Tint` (purple-maroon instead of black), `Midtone Tint` (red-orange falloff), `Grit` (world-space brush texture, so no UVs needed), `Spec` (hard wet highlight, used on floors), `Rim` |
| `NASCON/StylizedFlame` | All fire / wisp particles | Cream `Core`, teal `Rim`, `Cutoff` + noise for hard painted edges. Particle Color over Lifetime tints the body |
| `NASCON/ShadowCreature` | Shadow sprites (enemies, townsfolk, Void relief) | Sheet channels R body / G glow (halo, eyes, maw, burning rim) / B inner streaks; `_Glow`, `_Dissolve`, smoke `_Wobble` |
| `NASCON/LightShaft` | God rays through ceiling cracks (`PF_LightShaft`) | `_Color`, `_Intensity`, edge softness, end fades |
| `NASCON/InkLayer` | Prologue scroll layers + puppets | Mask texture R cover / G tone / B accent; `_LightColor`/`_DarkColor`/`_AccentColor`, `_Atmos`, `_Reveal` brush wipe, `_Silhouette`, `_Lamp` (accent glows at night) |
| `NASCON/InkPaper`, `InkFire`, `InkGlow`, `InkTransition` | Prologue paper backdrop, painted flame, additive light bloom, full-screen ink-drop / brush-sweep cuts | see shader headers |
| `NASCON/PainterlyPost` | Full-screen pass (`PainterlyPass` feature on `Assets/Settings/PC_Renderer`), runs before post-processing | `Brush Radius` (Kuwahara brush strokes), palette ramp + strength, paper grain. Edit `M_PainterlyPost` to tune the whole game's look |

### Main character (Meshy)

`Prefabs/Characters/PF_MainCharacter`: generated from the turnaround in `DOCUMENTATION/CHARACTER SHEETS/Main Character.png` (front/side/back → meshy-6 multi-view, T-pose → remeshed to ~31k tris → auto-rigged). It has a Humanoid avatar (24 bones, no fingers) and `M_MainCharacter` (StylizedLit). **It is animated procedurally** by `ProceduralHumanoid` (see 3b), not by clips. Please don't add Meshy/Mixamo animation clips; extend the procedural signals instead.

Global palette lives in `MaterialLibrary` (`ShadowTint`, `MidtoneTint`, material colours) and `VP_Tomb` (bloom, vignette, grain). New Meshy characters: run step 5 and they get the same treatment automatically.

**Townsfolk (Meshy):** `Art/Models/Characters/Villager_Man|Woman|Elder` (meshy-6-lite, textured, rigged; 60 credits). Animated by `ProceduralHumanoid`, like the main character.

**First-person body:** `M_MainCharacter_FP` sets `_CamClip` on `NASCON/StylizedLit` to cut a column around the eye (head, neck, torso), so you never see inside your own head; arms and feet stay.

**Asset policy:** environment and props are procedural (this generator). **Meshy is only for advanced character assets**, such as rigged first-person arms, townsfolk, the Void and RUSH. Put Meshy exports in `Art/Models/Characters/<Name>/`.

## 4. Rules that prevent merge hell

1. **One owner per scene at a time.** Claim it in the team chat ("I'm in L03") before editing. Nobody else saves that scene.
2. **Build in your Sandbox, ship as prefabs.** Make features prefabs, test them in your sandbox, and only drop the prefab into the real level.
3. **Prefabs over scene objects.** Editing a prefab doesn't touch the scene file.
4. **Never edit someone else's prefab or ScriptableObject** without asking. Prefab Variants are fine.
5. **`Core/` and `ProjectSettings/` are shared.** Announce changes, keep them small, push quickly.
6. **Always commit `.meta` files** with their asset. Never commit `Library/ Temp/ Logs/ UserSettings/`.
7. **Pull before you start, pull before you push.** Small commits, often.
8. Branches: `main` must always open and compile. Work on `feat/<name>-<thing>` and merge to main when it plays.

## 5. Conventions

- Names: `PascalCase` classes and files, `camelCase` private fields with `[SerializeField]`, no public fields.
- One MonoBehaviour per file, and the file name matches the class name.
- Asset prefixes: `PF_` prefab, `M_` material, `T_` texture, `SM_` static mesh, `SK_` skinned mesh, `A_` anim, `AC_` animator, `SFX_`, `MUS_`, `SO_` scriptable object, `VFX_`.
- Meshy models: export as **GLB/FBX**, drop into `Art/Models/<Category>/<AssetName>/` (model + its textures together), then make a `PF_` prefab.
- Lighting: tomb ambient is near-black purple `#08060B`, and torches are warm point lights `#FF9940`. Fire is the main light the player has. Don't brighten the ambient to make something easier to see.

## 6. MODULES workflow (short version)

Got a script that isn't ready to go into the real folders yet? Put it in `MODULES/<ModuleName>/`. The integrator runs **NASCON > Modules > Import All Modules**, which copies everything into `Assets/_Project/Scripts/Modules/` so it compiles. Once it works, promote it into the proper `Scripts/<System>/` folder. Full details are in [MODULES/README.md](MODULES/README.md).

## 7. `NASCON` editor menu

- **Scenes >** quick-open any scene
- **Modules > Import All Modules / Import Single Module… / Clear Imported Modules / Open MODULES Folder**
