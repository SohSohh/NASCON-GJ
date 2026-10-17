# MODULES — script inbox

This folder sits **outside `Assets/`**, so Unity never compiles it. Anyone can drop WIP, experimental or miscellaneous scripts here without breaking the project for everyone else. One person (the integrator) then pulls them into the project and checks that they compile.

## For contributors

1. Copy `_TEMPLATE/` and rename it to `MODULES/<ModuleName>/`, using PascalCase and one module per feature (e.g. `FireTransfer`, `RushEntity`, `TypewriterDialogue`).
2. Put your `.cs` files in it. Subfolders are fine. Shaders, `.asset` files and other Unity files can go here too.
3. Fill in `MODULE.md`: what it does, how to set it up, what it depends on, and which `Scripts/` folder it should be promoted to.
4. Use the namespace `NasconGJ.<Area>` and build against the shared contracts in `Assets/_Project/Scripts/Core`, `Fire/` and `Interaction/` (`GameEvents`, `IFlammable`, `ILightSource`, `IInteractable`).
5. Commit and push. Your scripts don't need to compile with everything else yet; that's the point of this folder.

Rules:
- Folders starting with `_` (like `_TEMPLATE`) are **never imported**. Prefix a module with `_` to park it.
- `.md`, `.txt` and `.meta` files are not copied into Unity.
- Don't put the same class name in two modules. Everything compiles into one assembly.

## For the integrator ("compiling" the modules)

1. Pull.
2. In Unity: **NASCON > Modules > Import All Modules**, or **Import Single Module…** to import just one.
   - Each module is mirrored to `Assets/_Project/Scripts/Modules/<ModuleName>/`.
   - A module's mirrored folder is **wiped and replaced** on every import, so `MODULES/` stays the source of truth. Don't edit the mirrored copy.
3. Check the Console. Fix any compile errors **in `MODULES/`**, then import again.
4. When a module works, **promote** it: move its scripts from `Scripts/Modules/<ModuleName>/` into the correct `Scripts/<System>/` folder **inside Unity** (so the `.meta` GUIDs and prefab references are kept). Then delete `MODULES/<ModuleName>/` and set its status to *Promoted* in the commit message.
5. Commit the mirrored/promoted files together with their `.meta` files so everyone compiles the same code.

To undo everything: **NASCON > Modules > Clear Imported Modules**. This only deletes the mirrors; `MODULES/` is not touched.
