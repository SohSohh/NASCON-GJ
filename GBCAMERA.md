# GBCamera integration

Hollow Light uses URP, a first-person camera, fire-powered survival, shadow enemies and a talking guiding light. Pickups implement `IInteractable`; braziers also accept fire through `FlammableItem`.

The imported RogueNoodle GBCamera package predates URP. Its original presentation prefab uses a second MainCamera and a quad on layer 8, which this game uses for shadow surfaces. The integration instead uses URP's built-in Full Screen Pass with a port of the package's red/green palette lookup. The palette comes directly from the imported `GBRenderMaterial`.

- Both PC and Mobile renderer assets contain an enabled **GBCamera** feature, after post-processing. Existing cameras, scene transitions and overlay HUDs continue to work.
- Tune `Assets/_Project/Art/Materials/M_GBCamera.mat`: **Vertical Resolution** defaults to 288, preserving the screen aspect ratio; 144 gives a coarser original-Game-Boy look. **Palette Strength** defaults to 1. **Palette Exposure** defaults to 1. Choose another imported palette texture to change colours.
- Disable the GBCamera renderer feature to return to the existing painterly look. Setting palette strength to zero keeps pixelation but removes palette mapping.
- `NASCON > Rendering > Set Up GBCamera` repairs/reinstalls the feature without duplicating it or resetting material tuning.
- This is a visual pixelation pass; it does not reduce the resolution or cost of rendering the world.
- Lighting refinement: **Preserve Light Falloff** (0.8) retains shading beneath the palette, keeping unlit space dark; **Nearby Detail Lift** (0.08) opens lit midtones without lifting black. Tomb bloom uses intensity 0.45, scatter 0.55 and threshold 1.05; contrast is 10 and exposure +0.45 EV. The scene generator preserves these values. Gameplay light radii are unchanged.

`PlayerInteractor` discovers interaction and fire-transfer targets at startup and once per second, including spawned objects. `InteractableHighlight` adds a pulsing, depth-tested surface/edge glow using per-renderer material properties. Aimed-at targets brighten; held items and unavailable dialogue targets lose their highlight. Existing tutorial glints remain independent. StylizedLit interactable silhouettes use a separate HDR outline color (default bright red) and stencil mask so GBCamera palette mapping does not alter the outline. The current StylizedLit and StylizedFlame shaders both support the surface/edge effect. New custom shaders need the `_InteractionHighlight` property and matching shader logic.

Validation: scripts and affected shaders compiled in Unity 6000.6.5f1; Game view visually checked in Sandbox Dev A; runtime transition to Tomb Entrance verified all 13 targets received highlights. Inventory pickup/drop verified highlight removal/restoration. No standalone build or device performance test was run.
