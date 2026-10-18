# Hollow Light

*There was once a town that lived in the light.*

**Hollow Light** is a first-person horror game made for the **NASCON 2026 game jam**. You carry a dying flame
into an ancient tomb to bring the light back to your town. The dark inside has waited a long time for someone
like you.

▶ **Trailer:** [`Marketing/Trailer/HollowLight_Trailer.mp4`](Marketing/Trailer/) (3:00)

---

## The story

An old elder tells the same story every night: a tomb beyond the hills, and the **Void** inside it. It is a
spirit made of shadow, with no face, no voice and no name, and it hungers for all three. The children stop
listening. Years later the town falls silent in a single moment: its people are not dead, not taken, but
emptied. One soul is left untouched. Ren takes up a torch and walks into the tomb that no one has ever walked
out of.

## What you do

- **Keep the flame alive.** Your torch burns down. Light braziers, relight from other flames, and swap to
  sticks and candles to keep going. Draughts gutter the flame, so shield it.
- **Read the walls.** Shadow murals tell what happened to everyone who came before.
- **Never trust the dark.** Shadows live on the walls. They watch, follow and close in, and they drain your
  light faster the nearer they get. Light holds them back; darkness lets them feed.
- **Take the crystal, then run.** The thing in the tomb comes for you across the walls, the ceiling and the
  floor. Reach the light at the end.
- **Find out what the light costs.**

## Controls

| Action | Key |
|---|---|
| Move | W A S D |
| Look | Mouse |
| Interact · pick up · light (hold) | F |
| Shield the flame | Right mouse |
| Sprint | Left Shift |
| Crouch | C |
| Jump | Space |
| Drop item | G |

## Running the project

- **Unity 6000.6.5f1** (exactly this version) with **URP** and the **New Input System**.
- Clone with Git LFS (`git lfs install` first), then open the folder in Unity Hub.
- Play from `Assets/_Project/Scenes/Core/Boot.unity` (or `MainMenu`).
- Story order: Main Menu → C01 *The Elder's Tale* → C01b *The Present* → L01 Tomb Entrance → L02 Narrow
  Path → M01 Maze → L03 Multi-Story → M02 Maze → L05 Crystal + the escape → C02 *The Cycle*.
- Levels are joined by connector passages that stream the next level in, so you walk from one to the next
  without a loading screen.

Most content is generated or rebuilt from editor menus under **NASCON** (levels, the L01 horror layer, level
passages, the chase, the ending cutscene, UI and gameplay prefabs). See [`WORKFLOW.md`](WORKFLOW.md) for team
setup, folder layout and conventions, and [`GBCAMERA.md`](GBCAMERA.md) for the pixel/palette look.

## Repository map

```
Assets/_Project/        all game content (Art, Audio, Prefabs, Scenes, Scripts, Settings)
DOCUMENTATION/          game design doc, cutscene plan, character and environment references
Marketing/Trailer/      the 3:00 story trailer and the script that edits it from in-game captures
MODULES/                inbox for loose / WIP scripts (not compiled)
Tools/                  art and audio helper scripts (VO cutting, sound design)
```

## Credits and licences

- Fonts: **Cinzel** and **EB Garamond**, SIL Open Font License (see `Assets/_Project/Art/Fonts/OFL-*.txt`).
- GBCamera palette effect adapted from RogueNoodle's GBCamera package.
- Environments, props, shaders, animation and most sound design are made procedurally in code.
- Made for NASCON 2026.
