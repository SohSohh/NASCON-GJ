#!/usr/bin/env bash
# Sound design for the prologues: layered, pitched, filtered and reverbed from CC0 recordings + ffmpeg synthesis.
set -e
SP="C:/Users/RAFAYA~1/AppData/Local/Temp/claude/d--NASCON-2026-NASCON-GJ/440606cf-3f8e-4189-8c20-31ab7a7d91a5/scratchpad"
D="$SP/dl2"
CC="/d/NASCON 2026/NASCON-GJ/Assets/_Project/Audio/CC0"
MU="/d/NASCON 2026/NASCON-GJ/Assets/_Project/Audio/Music"
OUT="/d/NASCON 2026/NASCON-GJ/Assets/_Project/Audio/Designed"
mkdir -p "$OUT"
F="ffmpeg -hide_banner -loglevel error -y"
ENC="-ac 2 -ar 44100 -c:a libvorbis -q:a 6"
VERB="aecho=0.8:0.75:70|160|310:0.35|0.25|0.15"          # a stone-room tail
BIG="aecho=0.85:0.8:120|290|520|900:0.4|0.3|0.2|0.12"     # a huge space

# ---------- thunder: real thunder + a slowed boom underneath + a sub drop
$F -i "$D/sfx100/sfx100v2_thunder_01.ogg" -i "$CC/SFX_DeepBoom.wav" \
  -filter_complex "[0]volume=1.6[t];[1]asetrate=44100*0.72,aresample=44100,volume=1.2[b];[t][b]amix=2:duration=longest:normalize=0,highpass=f=28,$VERB,alimiter=limit=0.95,afade=t=out:st=4.2:d=1.5" \
  $ENC "$OUT/SFX_ThunderStrike.ogg"

# the Void's arrival: the sky tears open (thunder + the long storm crack + sub sweep + rising air)
$F -i "$D/sfx100/sfx100v2_thunder_01.ogg" -i "$CC/AMB_RainThunder.ogg" -i "$CC/SFX_DeepBoom.wav" \
   -f lavfi -i "aevalsrc=0.9*sin(2*PI*(55-10*t)*t)*exp(-t/2.5):s=44100:d=6" \
   -f lavfi -i "anoisesrc=c=pink:a=0.5:d=2.5,afade=t=in:d=2.3,highpass=f=300,lowpass=f=3000" \
  -filter_complex "[0]volume=1.8[a];[1]atrim=13.6:23,asetpts=PTS-STARTPTS,volume=1.4[r];[2]asetrate=44100*0.55,aresample=44100,volume=1.5,adelay=150|150[b];[3]volume=0.9[s];[4]volume=0.6[n];[n][a]concat=n=2:v=0:a=1[pre];[pre][r][b][s]amix=4:duration=longest:normalize=0,highpass=f=24,$BIG,alimiter=limit=0.95,afade=t=out:st=8:d=2.5" \
  -t 11 $ENC "$OUT/SFX_ThunderVoid.ogg"

# ---------- the tomb floor giving way: quake rumble + cave-in + stones skittering
$F -i "$D/shake.flac" -i "$D/cave_in_-_starninjas_0.ogg" -i "$D/sfx100/sfx100v2_stones_01.ogg" -i "$D/sfx100/sfx100v2_stones_02.ogg" -i "$D/sfx100/sfx100v2_stones_03.ogg" \
  -filter_complex "[0]volume=1.4[q];[1]asetrate=44100*0.7,aresample=44100,volume=1.3,adelay=600|600[c];[2]adelay=900|900[s1];[3]adelay=1400|1400,asetrate=44100*0.8,aresample=44100[s2];[4]adelay=1900|1900[s3];[q][c][s1][s2][s3]amix=5:duration=longest:normalize=0,lowpass=f=7000,$VERB,alimiter=limit=0.95,afade=t=out:st=4.5:d=1.2" \
  -t 6 $ENC "$OUT/SFX_Collapse.ogg"

# ---------- the floor cracks open under them: a body-heavy slam with debris
$F -i "$D/sfx100/sfx100v2_hit_01.ogg" -i "$CC/SFX_DeepBoom.wav" -i "$D/sfx100/sfx100v2_stones_02.ogg" -i "$CC/SFX_FireCrackleShort.wav" \
  -filter_complex "[0]asetrate=44100*0.5,aresample=44100,volume=2[h];[1]asetrate=44100*0.8,aresample=44100,volume=1.3[b];[2]adelay=80|80,volume=1.2[s];[3]atrim=0:1.2,asetrate=44100*0.6,aresample=44100,volume=1.5[c];[h][b][s][c]amix=4:duration=longest:normalize=0,acompressor=threshold=0.2:ratio=6:attack=2:release=200,$VERB,alimiter=limit=0.97,afade=t=out:st=2.4:d=1" \
  -t 3.5 $ENC "$OUT/SFX_ImpactHeavy.ogg"

# ---------- falling into the pit: a long dropping whoosh with rising wind
$F -i "$CC/SFX_Swish_Heavy.wav" -f lavfi -i "anoisesrc=c=pink:a=0.7:d=3,highpass=f=200,lowpass=f=2500,afade=t=in:d=2,afade=t=out:st=2.4:d=0.6" \
  -filter_complex "[0]asetrate=44100*0.55,aresample=44100,volume=1.4[w];[w][1]amix=2:duration=longest:normalize=0,$BIG,alimiter=limit=0.95" \
  -t 3.5 $ENC "$OUT/SFX_FallWhoosh.ogg"

# ---------- the memories end in a scream that echoes away...
$F -i "$D/female_scream_1.ogg" \
  -af "asetrate=44100*0.9,aresample=44100,volume=1.4,acrusher=bits=10:mix=0.25,$BIG,lowpass=f=7500,alimiter=limit=0.95,apad=pad_dur=2.5,afade=t=out:st=2.5:d=1.8" \
  $ENC "$OUT/SFX_MemoryScream.ogg"

# ---------- tape hiss + static riding under the memory track
$F -i "$D/ScatterNoise1.mp3" -af "atrim=0:8,highpass=f=800,lowpass=f=6000,volume=0.5,afade=t=in:d=0.2" $ENC "$OUT/SFX_TapeStatic.ogg"

# ---------- distant town murmur: a crowd pushed far away (no words, just life)
$F -i "$D/crowd.ogg" -af "aloop=loop=3:size=2e9,lowpass=f=650,highpass=f=120,$BIG,volume=0.55,atrim=0:40" $ENC "$SP/murmur_tmp.ogg"

# ---------- town ambience bed: birds + soft air + the far murmur + kids far off + a carpenter somewhere
$F -i "$CC/AMB_BirdsWind.ogg" -i "$SP/murmur_tmp.ogg" -i "$CC/SFX_ChildrenLaugh.ogg" -i "$D/sfx100/sfx100v2_wood_hit_01.ogg" -i "$D/sfx100/sfx100v2_wood_hit_02.ogg" \
   -f lavfi -i "anoisesrc=c=pink:a=0.12:d=40,lowpass=f=900" \
  -filter_complex "[0]aloop=loop=3:size=2e9,atrim=0:40,volume=1.0[birds];[1]volume=0.8[m];[2]lowpass=f=2200,$BIG,volume=0.18,adelay=9000|9000[k];[3]asetrate=44100*0.85,aresample=44100,lowpass=f=3000,volume=0.25,aecho=0.6:0.5:90:0.3,adelay=4000|4000[w1];[4]lowpass=f=3000,volume=0.22,aecho=0.6:0.5:90:0.3,adelay=4600|4600[w2];[3]asetrate=44100*0.85,aresample=44100,lowpass=f=3000,volume=0.22,adelay=21000|21000[w3];[5]volume=1[air];\
[birds][m][k][w1][w2][air]amix=6:duration=first:normalize=0,alimiter=limit=0.9,afade=t=in:d=1.5,afade=t=out:st=38:d=2" \
  -t 40 $ENC "$OUT/AMB_Town.ogg" 2>/dev/null || \
$F -i "$CC/AMB_BirdsWind.ogg" -i "$SP/murmur_tmp.ogg" -filter_complex "[0]aloop=loop=3:size=2e9,atrim=0:40[b];[b][1]amix=2:duration=first:normalize=0" -t 40 $ENC "$OUT/AMB_Town.ogg"

# ---------- the Void: wind + a low breathing drone + the horror choir far below
$F -i "$CC/AMB_Wind.ogg" -i "$D/ambient_horror_0.ogg" \
   -f lavfi -i "aevalsrc=0.35*sin(2*PI*41*t)*(0.6+0.4*sin(2*PI*0.18*t))+0.2*sin(2*PI*61.7*t)*(0.5+0.5*sin(2*PI*0.11*t+1)):s=44100:d=30" \
  -filter_complex "[0]aloop=loop=4:size=2e9,atrim=0:30,volume=0.9[w];[1]aloop=loop=2:size=2e9,atrim=0:30,lowpass=f=2500,volume=0.5[h];[2]lowpass=f=200[d];[w][h][d]amix=3:duration=first:normalize=0,alimiter=limit=0.9,afade=t=in:d=2,afade=t=out:st=28:d=2" \
  -t 30 $ENC "$OUT/AMB_VoidWind.ogg"

# ---------- inside the tomb: dripping dark with a low wind
$F -i "$D/dungeon_ambient_1_0.ogg" -i "$D/atmosbasement.mp3_.flac" \
  -filter_complex "[0]volume=1.1[a];[1]aloop=loop=4:size=2e9,atrim=0:60,volume=0.7[b];[a][b]amix=2:duration=first:normalize=0,alimiter=limit=0.9" \
  -t 60 $ENC "$OUT/AMB_TombInside.ogg"

# ---------- music: the scroll theme (koto/shamisen/shakuhachi), the town theme, a soft drone pad under the wasteland
$F -i "$D/orientalsomber_0.ogg" -af "atrim=0:150,lowpass=f=11000,aecho=0.7:0.6:60|140:0.2|0.12,volume=0.9,afade=t=in:d=2,afade=t=out:st=144:d=6" $ENC "$OUT/MUS_ScrollTheme.ogg"
$F -i "$D/Shangririver.ogg" -af "lowpass=f=10000,aecho=0.7:0.6:60|140:0.2|0.12,volume=0.85,afade=t=in:d=1.5,afade=t=out:st=50:d=4" $ENC "$OUT/MUS_TownTheme.ogg"
$F -f lavfi -i "aevalsrc=0.16*sin(2*PI*110*t)*(0.7+0.3*sin(2*PI*0.07*t))+0.12*sin(2*PI*164.8*t)*(0.6+0.4*sin(2*PI*0.05*t+2))+0.1*sin(2*PI*220*t+sin(2*PI*0.3*t))*(0.5+0.5*sin(2*PI*0.09*t))+0.08*sin(2*PI*329.6*t)*(0.5+0.5*sin(2*PI*0.04*t+1)):s=44100:d=40" \
  -af "lowpass=f=1200,$BIG,alimiter=limit=0.8,afade=t=in:d=3,afade=t=out:st=36:d=4" $ENC "$OUT/MUS_LonePad.ogg"

# ---------- waking up in L01: the ringing in your ears after the fall, a slow heartbeat
$F -f lavfi -i "aevalsrc=0.12*sin(2*PI*6200*t)*exp(-t/2.2)+0.05*sin(2*PI*3100*t)*exp(-t/1.5):s=44100:d=6" -i "$CC/SFX_Heartbeat.wav" \
  -filter_complex "[1]volume=0.9,lowpass=f=900[h];[0][h]amix=2:duration=longest:normalize=0,alimiter=limit=0.9,afade=t=out:st=5:d=1.5" -t 6.5 $ENC "$OUT/SFX_WakeRinging.ogg"

rm -f "$SP/murmur_tmp.ogg"
ls -la "$OUT"
