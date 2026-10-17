#!/usr/bin/env bash
# Cut the recorded dialogue (screen recordings in DOCUMENTATION/Music And Audio/DIALOGUES) into one clean clip per line.
# Times are the spoken phrases found with silencedetect; each clip gets a little head/tail room.
set -e
SRC="/d/NASCON 2026/NASCON-GJ/DOCUMENTATION/Music And Audio/DIALOGUES"
OUT="/d/NASCON 2026/NASCON-GJ/Assets/_Project/Audio/Voice"
mkdir -p "$OUT"
CLEAN="highpass=f=75,lowpass=f=12000,afftdn=nf=-28,acompressor=threshold=0.12:ratio=3:attack=8:release=180:makeup=2,loudnorm=I=-14:TP=-1.2:LRA=7"
NARR="$CLEAN,aecho=0.8:0.6:45|95:0.12|0.07"     # the storyteller: a faint warm room
cut() { # file start end name chain
  local s e
  s=$(awk "BEGIN{v=$2-0.12; if(v<0)v=0; print v}"); e=$(awk "BEGIN{print $3+0.3}")
  ffmpeg -hide_banner -loglevel error -y -i "$SRC/$1" -vn -af "atrim=$s:$e,asetpts=PTS-STARTPTS,$5,afade=t=in:d=0.04,afade=t=out:st=$(awk "BEGIN{print $e-$s-0.12}"):d=0.12" \
    -ac 2 -ar 44100 -c:a libvorbis -q:a 6 "$OUT/$4.ogg"
}
# Part 1 (puppet scroll), narrator
cut "MONOLOGUE 1.mp4" 1.61 4.42  VO_N01_TownInTheLight "$NARR"
cut "MONOLOGUE 1.mp4" 5.06 8.72  VO_N02_TheyWorked     "$NARR"
cut "MONOLOGUE 1.mp4" 9.48 14.52 VO_N03_OldElder       "$NARR"
cut "MONOLOGUE 1.mp4" 15.24 19.22 VO_N04_TombBeyond    "$NARR"
cut "MONOLOGUE 1.mp4" 19.91 21.87 VO_N05_WhatLived     "$NARR"
cut "MONOLOGUE 2.mp4" 2.81 6.95  VO_N06_SpiritOfShadow "$NARR"
cut "MONOLOGUE 2.mp4" 7.93 12.23 VO_N07_NoFace         "$NARR"
cut "MONOLOGUE 2.mp4" 13.14 21.22 VO_N08_Hungered      "$NARR"
cut "MONOLOGUE 2.mp4" 22.62 30.34 VO_N09_OneDay        "$NARR"
cut "MONOLOGUE 2.mp4" 31.11 34.33 VO_N10_ChildrenLaughed "$NARR"
cut "MONOLOGUE 2.mp4" 35.14 37.14 VO_N11_StoppedListening "$NARR"
# Part 2 (3D), rooftop
cut "SIS DIALOGUE 1.mp4" 2.12 4.73 VO_Y01_OldTomb   "$CLEAN"
cut "SIS DIALOGUE 1.mp4" 4.94 7.02 VO_Y02_Grandpa   "$CLEAN"
cut "MC DIALOGUE.mp4"    1.11 3.79 VO_R01_WellTalks "$CLEAN"
cut "SIS DIALOGUE 2.mp4" 0.83 3.17 VO_Y03_SoQuiet   "$CLEAN"
# Part 2, narrator after the Void
cut "POST MONOLOGUE.mp4" 1.28 6.01  VO_P01_Emptied      "$NARR"
cut "POST MONOLOGUE.mp4" 7.41 12.74 VO_P02_OneSoul      "$NARR"
cut "POST MONOLOGUE.mp4" 13.74 16.98 VO_P03_ManyWalked  "$NARR"
cut "POST MONOLOGUE.mp4" 17.82 20.13 VO_P04_NoneWalkedOut "$NARR"
for f in "$OUT"/*.ogg; do printf "%-32s %5.2fs\n" "$(basename "$f")" "$(ffprobe -v error -show_entries format=duration -of csv=p=0 "$f")"; done
