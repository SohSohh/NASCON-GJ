"""
Hollow Light: story trailer builder (exactly 3:00).

Edits frame captures of the game (cutscenes + gameplay, captured at 50 fps from the Unity editor) into a
1920x1080 / 50 fps trailer with title cards, a letterbox, film grain and a mixed soundtrack built from the
game's own music, voice-over and sound design.

    py -3.12 build_trailer.py <frames_dir> [out.mp4]

<frames_dir> holds one folder per capture (C01, C01b, C02, L01wake, L01hall, L01gallery, L05chase, L05death),
each a sequence f00000.jpg, f00001.jpg, ... at 50 fps.
"""
import os
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
ASSETS = os.path.join(ROOT, "Assets")
AUDIO = os.path.join(ASSETS, "_Project", "Audio")
FONT_DIR = os.path.join(ASSETS, "_Project", "Art", "Fonts")

FPS_IN = 50          # capture rate
FPS_OUT = 50
W, H = 1920, 1080
TOTAL = 180.0

FRAMES = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "frames")
OUT = sys.argv[2] if len(sys.argv) > 2 else os.path.join(HERE, "HollowLight_Trailer.mp4")
WORK = os.path.join(HERE, "_build_" + os.path.splitext(os.path.basename(OUT))[0])

# ----------------------------------------------------------------------------------------------------------
#  The cut. ("shot", folder, first frame, seconds, grade, fade in, fade out, [overlay texts])
#           ("card", seconds, [texts], fade)        texts: (text, font, size, y, colour, start, end)
# ----------------------------------------------------------------------------------------------------------
CINE = "eq=contrast=1.06:saturation=1.05"
GAME = "curves=all='0/0 0.15/0.2 0.5/0.62 1/1',eq=contrast=1.08:saturation=1.12"
DARK = "curves=all='0/0 0.12/0.22 0.45/0.65 1/1',eq=contrast=1.06:saturation=1.1"
SERIF, TITLE = "EBGaramond-Italic.ttf", "Cinzel.ttf"
GOLD, BONE, BLOOD = "0xE8C98A", "0xEDE3D0", "0xC0141E"

TIMELINE = [
    # --- cold open: sharp cuts --------------------------------------------------------------------------
    ("card", 1.5, [], 0),
    ("shot", "L05chase", 330, 0.8, DARK, 0, 0, []),
    ("card", 0.1, [], 0),
    ("shot", "L01light0", 50, 0.8, GAME, 0, 0, []),
    ("shot", "L01circle", 300, 0.7, GAME, 0, 0, []),
    ("card", 0.1, [], 0),
    ("shot", "L05chase", 680, 0.7, GAME, 0, 0, []),
    ("shot", "L01light1", 90, 0.6, GAME, 0, 0, []),
    ("shot", "L05chase", 1000, 0.8, GAME, 0, 0, []),
    ("shot", "C01b", 1100, 0.8, CINE, 0, 0, []),
    ("card", 1.5, [], 0),
    ("shot", "MenuBG", 120, 5.6, CINE, 0.8, 0.8, []),
    # --- the legend ------------------------------------------------------------------------------------
    ("shot", "C01", 0, 5.0, CINE, 1.0, 0, []),
    ("shot", "C01", 300, 4.0, CINE, 0, 0, []),
    ("shot", "C01", 650, 4.5, CINE, 0, 0, []),
    ("shot", "C01", 1100, 4.0, CINE, 0, 0, []),
    ("shot", "C01", 1450, 5.0, CINE, 0, 0, []),
    ("shot", "C01", 2050, 5.0, CINE, 0, 0.8, []),
    ("card", 2.0, [("Years later.", SERIF, 54, 0, BONE, 0.3, 1.8)], 0),
    # --- the present ------------------------------------------------------------------------------------
    ("shot", "C01b", 0, 3.0, CINE, 0.6, 0, []),
    ("shot", "C01b", 380, 5.0, CINE, 0, 0, []),
    ("shot", "C01b", 700, 3.9, CINE, 0, 0, []),
    ("shot", "C01b", 1070, 4.0, CINE, 0, 0, []),
    ("shot", "C01b", 1350, 5.0, DARK, 0, 0, []),
    ("shot", "C01b", 1800, 6.0, CINE, 0, 0, []),
    ("shot", "C01b", 2430, 3.4, DARK, 0, 0, []),
    ("shot", "C01b", 2620, 3.6, DARK, 0, 1.0, []),
    ("card", 2.2, [("ONE SOUL. ONE LIGHT.", TITLE, 64, 0, GOLD, 0.3, 2.0)], 0),
    # --- the tomb: quick cuts -----------------------------------------------------------------------------
    ("shot", "L01wake", 300, 3.0, DARK, 0.6, 0, [("KEEP THE FLAME ALIVE", TITLE, 46, 300, BONE, 0.5, 2.8)]),
    ("shot", "L01light0", 40, 3.0, GAME, 0, 0, []),
    ("shot", "L01gallery", 0, 3.0, GAME, 0, 0, []),
    ("shot", "L01light1", 30, 3.0, GAME, 0, 0, [("LIGHT HOLDS THEM BACK", TITLE, 46, 300, BONE, 0.4, 2.8)]),
    ("shot", "L05chase", 0, 3.0, GAME, 0, 0, [("THE WALLS REMEMBER", TITLE, 46, 300, BONE, 0.4, 2.8)]),
    ("shot", "L01circle", 240, 3.2, GAME, 0, 0, []),
    ("shot", "L01gallery", 600, 1.2, GAME, 0, 0, []),
    ("shot", "L01circle", 380, 1.0, GAME, 0, 0, []),
    ("card", 0.4, [], 0),
    ("shot", "L01light1", 150, 0.8, GAME, 0, 0, []),
    ("card", 2.8, [("AND THEY ARE HUNGRY", TITLE, 50, 0, BLOOD, 0.2, 2.6)], 0),
    # --- the chase -------------------------------------------------------------------------------------
    ("shot", "L05chase", 170, 3.5, GAME, 0, 0, []),
    ("shot", "L05chase", 330, 2.5, DARK, 0, 0, []),
    ("card", 1.0, [("RUN", TITLE, 150, 0, BLOOD, 0.0, 1.0)], 0),
    ("shot", "L05chase", 420, 3.0, GAME, 0, 0, []),
    ("shot", "L05chase", 650, 1.6, GAME, 0, 0, []),
    ("shot", "L05chase", 830, 1.2, GAME, 0, 0, []),
    ("shot", "L05chase", 980, 1.6, GAME, 0, 0, []),
    ("shot", "L05chase", 880, 1.0, GAME, 0, 0, []),
    ("shot", "L05chase", 1050, 1.2, GAME, 0, 0, []),
    ("shot", "L05chase", 1180, 1.4, GAME, 0, 0, []),
    ("shot", "L05death", 0, 1.2, GAME, 0, 0, []),
    ("shot", "L05chase", 1100, 0.8, GAME, 0, 0, []),
    ("shot", "L05death", 236, 1.28, CINE, 0, 0, []),
    ("card", 2.72, [], 0),
    # --- the light -------------------------------------------------------------------------------------
    ("shot", "C02", 150, 5.0, CINE, 1.0, 0, []),
    ("shot", "C02", 420, 4.0, CINE, 0, 0, []),
    ("shot", "C02", 600, 4.0, CINE, 0, 0, []),
    ("shot", "C02", 850, 3.0, CINE, 0, 0, []),
    ("shot", "C02", 1430, 3.0, CINE, 0, 0, []),
    ("shot", "C02", 1560, 4.0, CINE, 0, 0.8, []),
    ("card", 1.0, [], 0),
    ("card", 7.0, [("HOLLOW LIGHT", TITLE, 150, 0, GOLD, 0.6, 6.6)], 0),
    ("shot", "C02", 2800, 5.0, CINE, 0.8, 0, []),
    ("shot", "C02", 3100, 5.0, CINE, 0, 1.0, []),
    # --- stinger ---------------------------------------------------------------------------------------
    ("card", 7.0, [], 0),
    ("shot", "L05chase", 330, 2.2, DARK, 0.5, 0, []),
    ("shot", "L05death", 272, 0.9, CINE, 0, 0, []),
    ("card", 0.9, [], 0),
]

# subtitles: (text, start, end), drawn in the lower letterbox bar
SUBS = [
    ("There was once a town that lived in the light.", 14.6, 18.0),
    ("Of a tomb beyond the hills, older than anyone's memory.", 22.8, 27.3),
    ("And of what lived inside it.", 27.6, 30.1),
    ("A spirit made of shadow. The Void.", 31.8, 36.5),
    ("It had no face, no voice, and no name.", 36.8, 41.7),
    ("You ever think about the old tomb? Past the hills.", 47.6, 50.7),
    ("...Hey. Why'd it go so quiet?", 51.8, 54.6),
    ("Not dead. Not taken. Emptied.", 59.6, 64.6),
    ("One soul was left untouched. And they remembered the old man's story.", 64.7, 70.4),
    ("Many had walked into that tomb over the ages.", 70.5, 74.0),
    ("None had ever walked out.", 74.1, 76.9),
    ("Every age, someone comes to save them.", 147.3, 151.0),
    ("There was once again a town that lived in the light...", 159.4, 163.6),
    ("Ren?", 170.2, 171.4),
]

# ----------------------------------------------------------------------------------------------------------
#  The soundtrack: (file, start in trailer, volume, trim from, length or None, fade in, fade out)
# ----------------------------------------------------------------------------------------------------------
def A(rel):
    return os.path.join(AUDIO, rel)

SFX, VO, MUS = "Designed/", "Voice/", "Music/"
JUMPSCARE = os.path.join(ASSETS, "Pool", "JUMPSCARE-2 (1).mp3")

AUDIO_EVENTS = [
    # cold open
    (A(SFX + "AMB_VoidWind.ogg"), 0.0, 0.4, 0, 8.4, 0.5, 1.0),
    (A(SFX + "SFX_ImpactHeavy.ogg"), 1.5, 0.9, 0, None, 0, 0.8),
    (A(SFX + "SFX_FallWhoosh.ogg"), 2.3, 0.7, 0, 1.2, 0, 0.3),
    (A(SFX + "SFX_PassBy.ogg"), 3.2, 0.8, 0, None, 0, 0),
    (A(SFX + "SFX_ImpactHeavy.ogg"), 4.0, 0.8, 0, None, 0, 0.8),
    (A(SFX + "SFX_Screech.ogg"), 4.7, 0.8, 0, 1.0, 0, 0.3),
    (A(SFX + "SFX_DreadHit.ogg"), 6.1, 0.9, 0, None, 0, 1.0),
    (A(SFX + "SFX_Whisper.ogg"), 7.0, 0.7, 0, None, 0, 0.3),
    (A(MUS + "MUS_MemoryFall.mp3"), 8.4, 0.5, 0, 6.0, 1.0, 1.0),
    # legend + present
    (A(SFX + "MUS_ScrollTheme.ogg"), 13.6, 0.45, 0, 29.9, 2.0, 2.0),
    (A(SFX + "MUS_TownTheme.ogg"), 43.5, 0.4, 0, 11.1, 1.2, 0.05),
    (A(SFX + "SFX_ThunderStrike.ogg"), 55.2, 1.0, 0, None, 0, 1.5),
    (A(SFX + "MUS_LonePad.ogg"), 55.4, 0.5, 0, 24.2, 1.0, 2.5),
    (A(SFX + "AMB_VoidWind.ogg"), 55.4, 0.3, 0, 24.2, 2.0, 2.0),
    (A(SFX + "SFX_DreadHit.ogg"), 35.6, 0.8, 0, None, 0, 1.0),
    (A(SFX + "SFX_DreadHit.ogg"), 77.0, 0.9, 0, None, 0, 1.0),
    # tomb
    (A(SFX + "AMB_HallDread.ogg"), 79.6, 0.55, 0, 24.4, 1.5, 1.0),
    (A(MUS + "MUS_MainAmbience.mp3"), 79.6, 0.45, 0, 21.6, 2.0, 1.0),
    (A(SFX + "SFX_FallWhoosh.ogg"), 82.9, 0.5, 0, 1.2, 0, 0.4),
    (A(SFX + "SFX_FallWhoosh.ogg"), 89.0, 0.5, 0, 1.2, 0, 0.4),
    (A(SFX + "SFX_Whisper.ogg"), 95.0, 0.8, 0, None, 0, 0.5),
    (A(SFX + "SFX_Screech.ogg"), 97.8, 0.9, 0, 1.0, 0, 0.3),
    (A(SFX + "SFX_ImpactHeavy.ogg"), 98.4, 0.8, 0, None, 0, 0.8),
    (A(SFX + "SFX_PassBy.ogg"), 99.6, 0.8, 0, None, 0, 0),
    (A(SFX + "SFX_ImpactHeavy.ogg"), 100.4, 0.8, 0, None, 0, 0.8),
    (A(SFX + "SFX_Whisper.ogg"), 101.6, 0.8, 0, None, 0, 0.5),
    # chase
    (A(SFX + "SFX_Collapse.ogg"), 104.0, 0.9, 0, None, 0, 1.0),
    (A(SFX + "SFX_DreadHit.ogg"), 106.0, 0.9, 0, None, 0, 1.0),
    (A(SFX + "SFX_Screech.ogg"), 107.6, 1.0, 0, None, 0, 0.5),
    (A(SFX + "SFX_ImpactHeavy.ogg"), 110.0, 1.0, 0, None, 0, 1.0),
    (A(MUS + "MUS_Presence.mp3"), 108.0, 0.8, 0, 16.0, 0.3, 0.05),
    (A(SFX + "SFX_Screech.ogg"), 122.2, 0.9, 0, None, 0, 0.3),
    (JUMPSCARE, 124.0, 1.0, 0, 1.6, 0, 0.3),
    # light, title, cliffhanger
    (A(MUS + "Theme.mp3"), 128.0, 0.7, 0, 42.0, 2.0, 3.0),
    (A(SFX + "SFX_DreadHit.ogg"), 150.8, 0.9, 0, None, 0, 1.0),
    (A(SFX + "SFX_ImpactHeavy.ogg"), 152.0, 0.7, 0, None, 0, 1.5),
    (A(SFX + "SFX_Whisper.ogg"), 166.0, 0.7, 0, None, 0, 0.5),
    # stinger
    (A(SFX + "AMB_VoidWind.ogg"), 169.0, 0.25, 0, 11.0, 1.0, 0.1),
    (A(SFX + "SFX_Whisper.ogg"), 175.4, 0.9, 0, None, 0, 0.3),
    (A(SFX + "SFX_DreadHit.ogg"), 176.0, 0.8, 0, None, 0, 1.0),
    (A(SFX + "SFX_Screech.ogg"), 178.2, 1.0, 0, None, 0, 0.2),
    (A(SFX + "SFX_ImpactHeavy.ogg"), 178.2, 1.0, 0, None, 0, 1.2),
    # voice
    (A(VO + "VO_N01_TownInTheLight.ogg"), 14.6, 1.0, 0, None, 0, 0),
    (A(VO + "VO_N04_TombBeyond.ogg"), 22.8, 1.0, 0, None, 0, 0),
    (A(VO + "VO_N05_WhatLived.ogg"), 27.6, 1.0, 0, None, 0, 0),
    (A(VO + "VO_N06_SpiritOfShadow.ogg"), 31.8, 1.0, 0, None, 0, 0),
    (A(VO + "VO_N07_NoFace.ogg"), 36.8, 1.0, 0, None, 0, 0),
    (A(VO + "VO_Y01_OldTomb.ogg"), 47.6, 1.0, 0, None, 0, 0),
    (A(VO + "VO_Y03_SoQuiet.ogg"), 51.8, 1.0, 0, None, 0, 0),
    (A(VO + "VO_P01_Emptied.ogg"), 59.6, 1.0, 0, None, 0, 0),
    (A(VO + "VO_P02_OneSoul.ogg"), 64.7, 1.0, 0, None, 0, 0),
    (A(VO + "VO_P03_ManyWalked.ogg"), 70.5, 1.0, 0, None, 0, 0),
    (A(VO + "VO_P04_NoneWalkedOut.ogg"), 74.1, 1.0, 0, None, 0, 0),
    (A(VO + "VO_E_V1_EveryAge.ogg"), 147.3, 1.0, 0, None, 0, 0),
    (A(VO + "VO_E_E1_OnceAgain.ogg"), 159.4, 1.0, 0, None, 0, 0),
    (A(VO + "VO_E_Y01_Ren.ogg"), 170.2, 0.9, 0, None, 0, 0),
]
_t = 111.0
while _t < 122.0:
    AUDIO_EVENTS.append((A(SFX + "SFX_Heartbeat.ogg"), round(_t, 2), 0.9, 0, None, 0, 0))
    _t += 0.6 - (_t - 111.0) * 0.015
AUDIO_EVENTS += [(A(SFX + "SFX_Heartbeat.ogg"), t, 1.0, 0, None, 0, 0) for t in (172.6, 173.4, 174.2)]


# ----------------------------------------------------------------------------------------------------------
def run(args, cwd=None):
    r = subprocess.run(args, cwd=cwd, capture_output=True, text=True)
    if r.returncode != 0:
        print(r.stderr[-3000:])
        raise SystemExit("ffmpeg failed: " + " ".join(args[:6]))


def esc(text):
    return text.replace("\\", "\\\\").replace(":", "\\:").replace("'", "’").replace(",", "\\,")


def text_filters(texts):
    out = []
    for text, font, size, y, colour, a, b in texts:
        f = 0.5 if b - a > 1.2 else 0.15
        alpha = f"if(lt(t\\,{a})\\,0\\,if(lt(t\\,{a + f})\\,(t-{a})/{f}\\,if(lt(t\\,{b - f})\\,1\\,if(lt(t\\,{b})\\,({b}-t)/{f}\\,0))))"
        out.append(f"drawtext=fontfile={font}:text='{esc(text)}':fontsize={size}:fontcolor={colour}:"
                   f"x=(w-text_w)/2:y=(h-text_h)/2+{y}:alpha='{alpha}':shadowcolor=black@0.8:shadowx=2:shadowy=2")
    return out


def subtitle_filters():
    out = ""
    for text, a, b in SUBS:
        alpha = f"if(lt(t\\,{a + 0.15})\\,(t-{a})/0.15\\,if(gt(t\\,{b - 0.15})\\,({b}-t)/0.15\\,1))"
        out += (f"drawtext=fontfile={SERIF}:text='{esc(text)}':fontsize=40:fontcolor={BONE}:x=(w-text_w)/2:"
                f"y=h-66-text_h/2:enable='between(t\\,{a}\\,{b})':alpha='{alpha}',")
    return out


def fades(dur, fin, fout):
    f = []
    if fin > 0:
        f.append(f"fade=t=in:st=0:d={fin}")
    if fout > 0:
        f.append(f"fade=t=out:st={dur - fout:.3f}:d={fout}")
    return f


def render(i, item):
    out = os.path.join(WORK, f"seg{i:03d}.mp4")
    common = ["-c:v", "libx264", "-preset", "ultrafast", "-crf", "16", "-pix_fmt", "yuv420p", "-r", str(FPS_OUT), "-an"]
    if item[0] == "card":
        dur, texts = item[1], item[2]
        colour = item[4] if len(item) > 4 else "black"
        vf = ",".join(text_filters(texts)) or "null"
        run(["ffmpeg", "-y", "-f", "lavfi", "-i", f"color=c={colour}:s={W}x{H}:r={FPS_OUT}:d={dur}", "-vf", vf] + common + [out], cwd=WORK)
        return out, dur
    _, folder, start, dur, grade, fin, fout, texts = item
    frames = int(round(dur * FPS_IN))
    src = os.path.join(FRAMES, folder)
    available = len([n for n in os.listdir(src) if n.endswith(".jpg")])
    if start + frames > available:
        raise SystemExit(f"{folder}: needs frames {start}..{start + frames} but only {available} captured")
    vf = [f"scale={W}:{H}:flags=lanczos", grade] + text_filters(texts) + fades(dur, fin, fout)
    run(["ffmpeg", "-y", "-framerate", str(FPS_IN), "-start_number", str(start), "-i", os.path.join(src, "f%05d.jpg"),
         "-frames:v", str(int(round(dur * FPS_OUT * 2))), "-vf", ",".join(vf) + f",fps={FPS_OUT}", "-t", f"{dur}"] + common + [out], cwd=WORK)
    return out, dur


def build_audio(path):
    args = ["ffmpeg", "-y"]
    chains, labels = [], []
    for i, (f, start, vol, trim, length, fin, fout) in enumerate(AUDIO_EVENTS):
        args += ["-i", f]
        trim_f = f"atrim=start={trim}" + (f":duration={length}" if length else "")
        chain = [f"[{i}:a]aformat=sample_rates=48000:channel_layouts=stereo", trim_f, "asetpts=PTS-STARTPTS"]
        if fin:
            chain.append(f"afade=t=in:d={fin}")
        if fout and length:
            chain.append(f"afade=t=out:st={max(0.0, length - fout):.3f}:d={fout}")
        chain += [f"volume={vol}", f"adelay={int(start * 1000)}:all=1"]
        chains.append(",".join(chain) + f"[a{i}]")
        labels.append(f"[a{i}]")
    mix = "".join(labels) + f"amix=inputs={len(labels)}:normalize=0:duration=longest,alimiter=limit=0.92,apad,atrim=0:{TOTAL}[out]"
    run(args + ["-filter_complex", ";".join(chains + [mix]), "-map", "[out]", "-c:a", "pcm_s16le", path])


def main():
    total = sum(item[1] if item[0] == "card" else item[3] for item in TIMELINE)
    assert abs(total - TOTAL) < 1e-6, f"timeline is {total:.3f}s, must be {TOTAL}s"
    os.makedirs(WORK, exist_ok=True)
    for font in (TITLE, SERIF):
        shutil.copy(os.path.join(FONT_DIR, font), os.path.join(WORK, font))

    listing = []
    for i, item in enumerate(TIMELINE):
        seg, dur = render(i, item)
        listing.append(f"file '{os.path.basename(seg)}'")
        print(f"{i:02d} {item[0]:4s} {dur:5.2f}s")
    with open(os.path.join(WORK, "list.txt"), "w") as fh:
        fh.write("\n".join(listing))

    video = os.path.join(WORK, "video.mp4")
    run(["ffmpeg", "-y", "-f", "concat", "-safe", "0", "-i", "list.txt",
         "-vf", "drawbox=x=0:y=0:w=iw:h=132:color=black:t=fill,drawbox=x=0:y=ih-132:w=iw:h=132:color=black:t=fill,"
                "vignette=PI/5," + subtitle_filters() + "noise=alls=7:allf=t+u",
         "-c:v", "libx264", "-preset", "veryfast", "-crf", "20", "-pix_fmt", "yuv420p", "-r", str(FPS_OUT), "-t", str(TOTAL), video], cwd=WORK)

    audio = os.path.join(WORK, "audio.wav")
    build_audio(audio)

    run(["ffmpeg", "-y", "-i", video, "-i", audio, "-map", "0:v", "-map", "1:a", "-c:v", "copy", "-c:a", "aac", "-b:a", "320k",
         "-t", str(TOTAL), "-movflags", "+faststart", OUT])
    print("wrote", OUT)


if __name__ == "__main__":
    main()
