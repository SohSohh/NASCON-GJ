# Hollow Light: Cutscene Plan (intro → escape → outro)

Style rule: **the past is paper** (2D ink-scroll shadow puppets, Kung Fu Panda backstory style). **The present is the game** (3D, painterly/comic post, same look as gameplay). Cuts are short and quick: 1.5–4 s per shot, no dead air, and captions stand in for VO until it's recorded.

Names in captions are placeholders: **REN** (the bearer, the player) and **YUNA** (their sister). Rename them in `PrologueDirector` (`GirlName`, `BearerName`).

---

## C01: The Elder's Tale (2D puppets) · ~60 s · BUILT
`Scenes/Cutscenes/C01_Prologue` · `Cutscenes/Scroll/ScrollDirector`

| # | Shot | Line (VO) | Transition out |
|---|------|-----------|----------------|
| 1 | Lantern glows in the dark, the town paints itself in, camera pulls wide | "There was once a town that lived in the light." | swish cut |
| 2 | Market close → street (puppets selling, waving, kids running) | "They worked, they laughed, they quarreled and made up." | brush sweep |
| 3 | Night courtyard, the elder by the fire, kids listening, shadows on the wall | "And in that town lived an old elder, who told the same story every night." | paper drop |
| 4 | Gold-leaf landscape: hills, the tomb on a hill, moon, xiangyun clouds | "Of a tomb beyond the hills, older than anyone's memory." / "And of what lived inside it." | ink drop |
| 5 | The wall: the kids' shadows swell into the Void | "A spirit made of shadow. The Void." | ink drop |
| 6 | The Void alone, backlit by a pale moon, camera rises to the hood | "It had no face, no voice, and no name." / "And because it had nothing, it hungered. For a face. A voice. A self." | brush sweep |
| 7 | Its arm reaches for the kids' shadows | "'One day,' he would whisper, 'it will come looking for what it lacks.'" | hard cut |
| 8 | Kids laugh, adults walk past; seasons pass (leaves → snow), kids fade away | "The children laughed. The adults shook their heads." / "In time, even the children stopped listening." | page flip + ink → C01b |

### C01 look (done, and next steps)
| Layer | What | Status |
|---|---|---|
| Paper grade (`NASCON/PaperGrade`, quad on the camera) | rice-paper fibres + mottling (multiply), burned edges, drifting warm light leaks, paper grain (add) | done |
| Post (`VP_Scroll`) | stronger warm bloom, lifted warm blacks, teal-shadow / gold-light split tone, bokeh DoF focused on the puppet plane (focus follows the camera), vignette, grain, slight CA | done |
| Light shafts (`NASCON/InkRays`) | sun rays over the town, firelight up the courtyard wall, moonlight on the legend, a cold shaft on the Void | done |
| Glowing dust motes | drifting in the light, in front of and behind the puppets | done |
| Next | puppets cast a soft warm halo when lit from behind (backlight rim on InkLayer); per-panel colour grade (night panels cooler); paper "crease" and torn-edge frame on panel cuts; a real scanned paper texture (CC0, e.g. ambientCG Paper) to replace the generated one | todo |

## C01b: The Present (3D) · ~60 s · BUILT
`Scenes/Cutscenes/C01b_PrologueTown` · `Cutscenes/PrologueDirector`

| # | Shot | Line | Notes |
|---|------|------|-------|
| 1 | Crane down over the busy town (birds, murmur, music) | caption "Years later." | 4 s |
| 2 | Dolly past neighbours chatting | — | 3 s |
| 3 | **Rooftop**, wide two-shot from behind; casual sibling talk | YUNA: "You ever think about the old tomb? Past the hills." | |
| 4 | Over Ren's shoulder on Yuna | YUNA: "Grandpa swears something's still in there." | |
| 5 | Over Yuna's shoulder on Ren | REN: "He also swears the well talks back." | |
| 6 | Back on Yuna; birds stop, wind rises, music cuts | YUNA: "...Hey. Why'd it go so quiet?" | |
| 7 | Low angle: both look up | — | 1.6 s |
| 8 | From behind them: **the Void rises** over the town, thunderbolt impact frames ×2, arms spread | — | Thunderbolts-style |
| 9 | Street: the darkness shockwave rolls at the lens, people turn to statues | — | 2.6 s |
| 10 | Rooftop: **Yuna turns to shadow beside Ren**; Ren's torch keeps them | — | heartbeat + bell |
| 11 | Shadows burned into the walls | "Not dead. Not taken. Emptied." | |
| 12 | Wasteland: torch passes the lens, Ren walks alone | "One soul was left untouched. And they remembered the old man's story." | |
| 13 | Tomb in the storm, lightning | "Many had walked into that tomb over the ages." | |
| 14 | Reverse from the doorway | "None had ever walked out." | |
| 15 | Two steps inside → **hard cut to black, silence 0.9 s** | — | no fade |
| 16 | **Memory fall**: memories flash past like a chewed tape (puppet tale, town, rooftop, Yuna, tomb), faster and faster, rising past us as we fall; *the.bread.society* track, slightly distorted, tape wow; white flash | — | `MemoryFall` + `NASCON/MemoryGlitch`, ~5.5 s |
| 17 | Impact in black → L01 (wake up at the bottom) | — | |

## In-game beats (gameplay camera, no letterbox)
| Where | Beat | Status |
|---|---|---|
| L01 start | Wake up, look up the shaft, the torch on the floor | built (`WakeUpSequence`) |
| L01 | Guiding light first appears ("You fell a long way.") | built |
| L01 Hall of Echoes | Pan across townsfolk shadows on the walls; **Yuna's shadow among them** (one frame of recognition, heartbeat) | TODO: add Yuna's silhouette as a `ShadowFigure` frame |
| L03/L04 | Short flashback stingers (1.5 s, puppet style, `MemoryGlitch` transition) when you find a tomb mural | TODO, optional |
| L05 Crystal | Taking the crystal: all light dies, 2 s of black, the crystal lights from within | TODO |

## L06: The Void Escape (playable chase with scripted moments) · ~60–90 s
The design doc calls for a chase after the final puzzle that ends in a hallway with a bright light. The player keeps control the whole time.

| # | Beat | How |
|---|------|-----|
| 1 | Crystal taken → the tomb **inhales**: dust falls, every flame leans toward the dark, 1 s silence | camera shake + `WindDraftZone` reversed, low boom |
| 2 | Behind you the wall **floods black**: the Void's hand (the 3D vine giant from C01b) pushes through | `VoidApparition` spawned at the back of the corridor, its arm extended; `ComicLook.SetVoid` radius grows behind the player |
| 3 | **RUN.** Corridor collapses behind (rigidbody slabs), shadows peel off the walls and race along them beside you | reuse `ShadowBody` along a spline; floor drops in sections |
| 4 | Flame gutters in a wind tunnel: hold **cover** at the right time (teaches RUSH) or die | `WindDraftZone extinguishUncovered` |
| 5 | Jump a broken bridge; the Void's fingers close on empty air behind you (slow-mo 0.4 s) | `Time.timeScale` dip + impact frame |
| 6 | Final hallway: **white light at the end**, sound drops out to heartbeat and breath, the Void's whispers turn into Yuna's voice saying Ren's name | camera FOV push, audio snapshot |
| 7 | Step into the light → white → C02 | `ScreenFader` white |

Fail state: the Void touches you → black, whisper, restart at the chase's start checkpoint.

## C02: Ending, "The Cycle" (3D, then paper) · ~70 s
| # | Shot | Line | Notes |
|---|------|------|-------|
| 1 | White. Ren steps out of the tomb at dawn; the light is coming **from Ren's torch**, which burns white | — | 3D, tomb set from C01b |
| 2 | A burst of energy erupts from the tomb, a ring of light racing over the hills to the town | — | reverse of the C01b shockwave (`ComicLook.SetVoid` shrinking, inverted colours) |
| 3 | The town: shadows **peel off the walls** and step back into their bodies; colour floods back; birds | — | `Villager.Shadowify` reversed + imprints fade |
| 4 | Rooftop: **Yuna** breathes, turns, smiles at someone off-camera | YUNA: "Ren?" | payoff of the rooftop talk |
| 5 | Back at the tomb mouth: Ren looks at their hand. The fingers are **ink**. | — | close-up, `NASCON/ShadowGlob` creeping up the arm |
| 6 | Ren's torch goes out. Their face is gone: no face, no voice, no name | — | |
| 7 | Ren turns and walks back into the tomb | REN (VO, distorted): "It had forgotten all of them long ago." | |
| 8 | **Ink transition back to paper**: a new puppet scroll, a new elder telling kids the story, and on the wall the Void's shadow now wears **Ren's silhouette** | ELDER: "There was once a town that lived in the light..." | bookends C01; reuse the Scroll panels |
| 9 | Title: HOLLOW LIGHT → credits | — | |

---

## Assets required

### Characters (Meshy: **confirm credits before generating**; about 955 left)
| Asset | Use | Est. credits | Status |
|---|---|---|---|
| Yuna (sister), teen girl, simple tunic/scarf | C01b rooftop, C02 | ~20–35 (image→3D + texture) | **NEEDED** (placeholder: `PF_Villager_Woman`) |
| Elder (3D) | optional cameo in C02 | ~20–35 | optional (exists as a villager variant) |
| Ren, shadow-corrupted variant | C02 | 0 (shader on the main character) | shader TODO |
Animation is procedural (`ProceduralHumanoid`, `Villager`, `CutsceneActor`); no Meshy animation credits.

### Environments (procedural, code)
| Asset | Use | Status |
|---|---|---|
| Town street, houses, lamp posts | C01b, C02 | built |
| Rooftop terrace | C01b, C02 | built |
| Wasteland path, tomb façade, tomb interior | C01b, C02 | built |
| Collapsing chase corridor + broken bridge + light hallway | L06 | TODO (`LevelBuilder.BuildEscape`) |
| Tomb mouth at dawn | C02 | reuse the tomb set + dawn atmosphere |

### 2D scroll art (procedural, `InkArtGenerator`)
| Asset | Use | Status |
|---|---|---|
| Landscapes, village, pines, grass, calligraphy, seal, xiangyun | C01, C02 epilogue | built |
| Puppets: man, woman, child, elder, vendor, bearer | C01 | built |
| **Puppet Ren** (bearer variant with Ren's silhouette), **Void-Ren** wall shadow | C02 epilogue | TODO |

### VFX / shaders
| Asset | Use | Status |
|---|---|---|
| Ink transitions (`NASCON/InkTransition`) | all 2D cuts | built |
| Thunderbolt impact frames, darkness shockwave (`ComicPost`) | C01b, L06, C02 | built |
| `NASCON/MemoryGlitch` + `MemoryFall` | C01b fall, flashback stingers | built |
| Light burst (inverse shockwave, white) | C02 | TODO |
| Ink creeping up a body (`ShadowGlob` dissolve) | C02 | TODO |

### Audio
| Asset | Use | Status |
|---|---|---|
| CC0 pack (birds, fire, wind, rain/thunder, swishes, gong, bell, paper, steps, heartbeat, boom) | everywhere | in `Audio/CC0` |
| the.bread.society track | memory fall | in `Audio/Music/MUS_MemoryFall.mp3` |
| **VO: narrator** (11 lines, C01 + C01b) | C01, C01b | **to record** (lines in the tables above) |
| **VO: Yuna** (3 lines + "Ren?") | C01b, C02 | **to record** |
| **VO: Ren** (2 lines) | C01b, C02 | **to record** |
| Chase music stinger, light-burst swell | L06, C02 | needed (or reuse `MUS_Presence` with a riser) |
| Whisper → Yuna's voice morph | L06 hallway | needed (record Yuna saying "Ren...") |
