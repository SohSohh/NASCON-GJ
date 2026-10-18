# Hollow Light: story trailer

`HollowLight_Trailer.mp4`: 3:00 exactly, 1920×1080, 50 fps, AAC 320 kbps, with burned-in subtitles. Cut
entirely from real in-game footage, with the game's own voice-over, music and sound design.

## Structure

| Time | Beat |
|---|---|
| 0:00 | **Cold open**: sharp cuts (eyes in the dark, a brazier catching fire, the shadow circle, the chase, the Void) |
| 0:08 | The tomb hall from behind the main menu, slow push-in |
| 0:14 | **The legend** (C01 scroll): the town in the light, the elder's tale, the tomb, the Void |
| 0:41 | *Years later.* The town, the rooftop, "Why'd it go so quiet?" |
| 0:55 | The Void over the sunset, the emptied town, Ren takes the torch, the tomb |
| 1:17 | **ONE SOUL. ONE LIGHT.** |
| 1:19 | **Inside the tomb**: the torch, braziers lit, the gallery, the shadow circle |
| 1:41 | **AND THEY ARE HUNGRY** |
| 1:44 | **The crystal**: the dark tears out of the walls, **RUN**, the chase, the jump scare |
| 2:08 | **The light**: Ren carries the crystal out, dawn, the town restored, Ren turning dark |
| 2:32 | Title: **HOLLOW LIGHT** |
| 2:39 | Cliffhanger: the storyteller and the shadow on the wall |
| 2:49 | Stinger: "Ren?", a heartbeat, eyes in the dark, cut |

## Rebuilding

The edit is fully scripted in `build_trailer.py` (ffmpeg + Python). The shot list, title cards, subtitles and the
audio mix are data at the top of the file.

```
py -3.12 build_trailer.py <frames_dir> [out.mp4]
```

`<frames_dir>` holds one folder per capture (`C01`, `C01b`, `C02`, `MenuBG`, `L01wake`, `L01hall`, `L01gallery`,
`L01circle`, `L01light0`, `L01light1`, `L05chase`, `L05death`). Each is a numbered JPG sequence (`f00000.jpg` …)
at 50 fps, captured from the Unity editor by stepping play mode one frame at a time and rendering the camera to
1280×720. Gameplay shots were driven by scripted camera paths. The frames are not checked in (several GB);
recapture them to re-edit.
