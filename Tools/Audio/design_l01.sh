#!/usr/bin/env bash
# L01 horror beats: screech stinger, distant pass-by, dread hit, whispers, hall dread bed.
# Built from real CC0 recordings (sfx100 pack, CC0 folder, crowd murmur) — pitched, stretched, layered,
# and convolved with noise-built stone/hall impulse responses. Synthesis only for sub layers.
set -e
SP="C:/Users/RAFAYA~1/AppData/Local/Temp/claude/d--NASCON-2026-NASCON-GJ/440606cf-3f8e-4189-8c20-31ab7a7d91a5/scratchpad"
D="$SP/dl2"
S="$D/sfx100"
CC="/d/NASCON 2026/NASCON-GJ/Assets/_Project/Audio/CC0"
OUT="/d/NASCON 2026/NASCON-GJ/Assets/_Project/Audio/Designed"
T="$SP/l01_tmp"
mkdir -p "$OUT" "$T"
[ -d "$S" ] || unzip -oq "$D/sfx_100_v2.zip" -d "$S"
F="ffmpeg -hide_banner -loglevel error -y"
ENC="-ac 2 -ar 44100 -c:a libvorbis -q:a 6"
WAV="-ar 44100 -c:a pcm_f32le"
LIM="alimiter=limit=0.82:attack=1:release=80:level=disabled"   # ~ -1.7 dBFS ceiling; vorbis overshoot lands it near -1 dBFS
# fin IN OUT PRECHAIN POSTCHAIN DRIVE_DB : peak-normalise to -1 dBFS (+drive into the limiter), limit, encode
fin(){ $F -i "$1" -af "$3" $WAV "$T/_pre.wav"
  pk=$(ffmpeg -hide_banner -i "$T/_pre.wav" -af astats=measure_perchannel=none:measure_overall=Peak_level -f null - 2>&1 | grep -o "Peak level dB: [-0-9.]*" | tail -1 | awk '{print $4}')
  g=$(awk -v p="$pk" -v d="$5" 'BEGIN{printf "%.2f", -1.0-p+d}')
  $F -i "$T/_pre.wav" -af "volume=${g}dB,$LIM,$4" $ENC "$2"; }
RS="rubberband"                                   # pitch/tempo on real recordings

# ---------- impulse responses: decorrelated noise with a dark, frequency-dependent decay
# stone room (~1.8 s): bright early part dies fast, darker part rings longer
$F -f lavfi -i "anoisesrc=c=white:seed=11:a=0.5:d=2.2" -f lavfi -i "anoisesrc=c=white:seed=23:a=0.5:d=2.2" \
  -filter_complex "[0][1]amerge=inputs=2,asplit[a][b];[a]aeval='val(0)*exp(-t/0.16)|val(1)*exp(-t/0.16)':c=stereo,highpass=f=200,lowpass=f=6500[e];[b]aeval='val(0)*exp(-t/0.45)|val(1)*exp(-t/0.45)':c=stereo,lowpass=f=1800,highpass=f=80[l];[e][l]amix=2:normalize=0,afade=t=in:d=0.004" \
  $WAV "$T/ir_stone.wav"
# big tomb hall (~5 s)
$F -f lavfi -i "anoisesrc=c=white:seed=5:a=0.5:d=5.5" -f lavfi -i "anoisesrc=c=white:seed=9:a=0.5:d=5.5" \
  -filter_complex "[0][1]amerge=inputs=2,asplit[a][b];[a]aeval='val(0)*exp(-t/0.35)|val(1)*exp(-t/0.35)':c=stereo,highpass=f=150,lowpass=f=4500[e];[b]aeval='val(0)*exp(-t/1.1)|val(1)*exp(-t/1.1)':c=stereo,lowpass=f=1200,highpass=f=50[l];[e][l]amix=2:normalize=0,afade=t=in:d=0.02" \
  $WAV "$T/ir_hall.wav"

# rev IN OUT IR DRY WET PADSEC
# (afir is 100% wet, so the dry path is mixed back in by hand; irnorm=2 = unit-energy IR)
rev(){ $F -i "$1" -i "$3" -filter_complex "[0]aformat=sample_fmts=fltp:sample_rates=44100:channel_layouts=stereo,apad=pad_dur=$6,asplit[x][d];[x][1]afir=irnorm=2,volume=$5[w];[d]volume=$4[dd];[dd][w]amix=2:normalize=0" $WAV "$2"; }

# ---------- whisper source: the crowd murmur with its pitch destroyed (random FFT phase) -> breathy, wordless voices
$F -i "$D/crowd.ogg" -af "aformat=channel_layouts=mono,afftfilt=real='hypot(re,im)*cos((random(0)*2-1)*2*3.14)':imag='hypot(re,im)*sin((random(1)*2-1)*2*3.14)':win_size=512:overlap=0.75,highpass=f=350,lowpass=f=7500,$RS=pitch=0.86" \
  $WAV "$T/whisper_src.wav"

# =====================================================================================
# 1. SFX_Screech — reversed suck-in, then a violent metal shriek slammed with a low hit + sub
# shriek core: grinding/creaking recording pitched way up + scraped metal stretched out, detuned against itself
$F -i "$S/sfx100v2_misc_28.ogg" -i "$S/sfx100v2_metal_01.ogg" -i "$S/sfx100v2_metal_05.ogg" -i "$S/sfx100v2_misc_28.ogg" \
  -filter_complex "[0]aformat=channel_layouts=mono,$RS=pitch=1.9:tempo=1.0,highpass=f=700,tremolo=f=9:d=0.25,volume=1.4[a];\
[1]aformat=channel_layouts=mono,$RS=pitch=1.45:tempo=0.3,highpass=f=500,volume=1.3,pan=stereo|c0=0.9*c0|c1=0.5*c0[b];\
[2]aformat=channel_layouts=mono,$RS=pitch=2.1:tempo=0.38,highpass=f=900,adelay=25,volume=1.1,pan=stereo|c0=0.45*c0|c1=0.95*c0[c];\
[3]aformat=channel_layouts=mono,$RS=pitch=2.62:tempo=1.0,highpass=f=1200,tremolo=f=14:d=0.3,adelay=15,volume=0.8[d];\
[a]pan=stereo|c0=c0|c1=c0[a2];[d]pan=stereo|c0=0.7*c0|c1=0.7*c0[d2];\
[a2][b][c][d2]amix=4:duration=longest:normalize=0,equalizer=f=3200:t=q:w=1.2:g=5,aformat=sample_fmts=fltp,volume=2.2,asoftclip=type=tanh,atrim=0:2.0,afade=t=out:st=0.7:d=1.3" \
  $WAV "$T/scr_core.wav"
# reversed lead-in: reverb of the shriek's first instant, played backwards -> 0.25 s inhale into the hit
$F -i "$T/scr_core.wav" -af "atrim=0:0.35" $WAV "$T/scr_head.wav"
rev "$T/scr_head.wav" "$T/scr_headrev.wav" "$T/ir_stone.wav" 0.3 1 1.6
$F -i "$T/scr_headrev.wav" -i "$S/sfx100v2_air_03.ogg" \
  -filter_complex "[0]areverse,atrim=start=1.70:end=1.95,asetpts=PTS-STARTPTS[r];[1]aformat=channel_layouts=stereo,areverse,atrim=0:0.25,asetpts=PTS-STARTPTS,highpass=f=1500,volume=0.8[a];[r][a]amix=2:normalize=0,afade=t=in:d=0.22:curve=exp" \
  $WAV "$T/scr_lead.wav"
# low hit + sub thump
$F -i "$S/sfx100v2_hit_01.ogg" -i "$S/sfx100v2_stones_02.ogg" -i "$CC/SFX_DeepBoom.wav" \
   -f lavfi -i "aevalsrc='0.9*sin(2*PI*(58*t-9*t*t))*exp(-t/0.4)':s=44100:d=1.6" \
  -filter_complex "[0]aformat=channel_layouts=stereo,$RS=pitch=0.5,volume=2.2[h];[1]aformat=channel_layouts=stereo,$RS=pitch=0.7,volume=1.1[s];[2]atrim=0:2,asetrate=44100*0.75,aresample=44100,volume=1.4,lowpass=f=400[b];[3]aformat=channel_layouts=stereo[sub];\
[h][s][b][sub]amix=4:duration=longest:normalize=0,acompressor=threshold=0.25:ratio=4:attack=1:release=150" \
  $WAV "$T/scr_low.wav"
$F -i "$T/scr_lead.wav" -i "$T/scr_core.wav" -i "$T/scr_low.wav" \
  -filter_complex "[1]adelay=250:all=1[c];[2]adelay=250:all=1[l];[0][c][l]amix=3:duration=longest:normalize=0,highpass=f=28" \
  $WAV "$T/scr_dry.wav"
rev "$T/scr_dry.wav" "$T/scr_wet.wav" "$T/ir_stone.wav" 1 0.5 1.2
fin "$T/scr_wet.wav" "$OUT/SFX_Screech.ogg" "atrim=0:2.6" "afade=t=out:st=1.9:d=0.7" 4

# =====================================================================================
# 2. SFX_PassBy — distant shadow darting across a doorway, L -> R
# moving bus: airy whoosh (two air recordings) + dry paper/cloth rustle + a smear of the breathy whisper
$F -i "$S/sfx100v2_air_02.ogg" -i "$S/sfx100v2_air_01.ogg" -i "$CC/SFX_Paper_1.ogg" -i "$CC/SFX_Cloth.ogg" -i "$T/whisper_src.wav" \
  -filter_complex "[0]aformat=channel_layouts=mono,$RS=tempo=1.3:pitch=1.1,afade=t=in:d=0.25,afade=t=out:st=0.55:d=0.3,adelay=250,volume=1.4[w1];\
[1]aformat=channel_layouts=mono,atrim=0.6:1.8,asetpts=PTS-STARTPTS,$RS=tempo=1.4,afade=t=in:d=0.3,afade=t=out:st=0.55:d=0.3,highpass=f=400,adelay=200,volume=0.9[w2];\
[2]aformat=channel_layouts=mono,$RS=tempo=1.3,highpass=f=1500,adelay=480,volume=0.9[p];\
[3]aformat=channel_layouts=mono,atrim=0:0.5,$RS=tempo=1.2:pitch=1.15,highpass=f=1200,adelay=420,volume=0.7[c];\
[4]atrim=5.0:6.0,asetpts=PTS-STARTPTS,bandpass=f=2200:w=2400,tremolo=f=11:d=0.5,afade=t=in:d=0.35,afade=t=out:st=0.5:d=0.5,adelay=330,volume=0.55[wh];\
[w1][w2][p][c][wh]amix=5:duration=longest:normalize=0,\
aeval='val(0)*cos(PI/2*clip((t-0.2)/0.8,0,1))|val(0)*sin(PI/2*clip((t-0.2)/0.8,0,1))':c=stereo" \
  $WAV "$T/pass_dry.wav"
rev "$T/pass_dry.wav" "$T/pass_wet.wav" "$T/ir_stone.wav" 1 0.55 1.2
fin "$T/pass_wet.wav" "$OUT/SFX_PassBy.ogg" "lowpass=f=7500,highpass=f=90,atrim=0:2.0,aeval='val(0)*(1-0.6*clip((t-0.55)/0.5,0,1))|val(1)*(1-0.6*clip((0.45-t)/0.3,0,1))':c=stereo" "afade=t=out:st=1.5:d=0.5" -3

# =====================================================================================
# 3. SFX_DreadHit — reversed gong swell into a deep boom, dissonant bowed drone hanging after
# swell: gong + its hall tail, reversed so it crescendos into the hit at 1.5 s; a whisper of air on top
$F -i "$CC/SFX_Gong_1.ogg" -af "aformat=channel_layouts=stereo,$RS=pitch=0.7" $WAV "$T/dh_gong.wav"
rev "$T/dh_gong.wav" "$T/dh_gongrev.wav" "$T/ir_hall.wav" 0.7 1 2
$F -i "$T/dh_gongrev.wav" -i "$S/sfx100v2_air_01.ogg" \
  -filter_complex "[0]atrim=0:1.5,asetpts=PTS-STARTPTS,areverse,highpass=f=120[g];[1]aformat=channel_layouts=stereo,atrim=0:1.5,asetpts=PTS-STARTPTS,areverse,highpass=f=2000,volume=0.5[a];[g][a]amix=2:normalize=0,afade=t=in:d=1.4:curve=exp" \
  $WAV "$T/dh_swell.wav"
# drone: a ringing tonal recording pitched low in three detuned voices (root, +1 semitone, tritone below) + stretched bowed metal
$F -i "$S/sfx100v2_misc_12.ogg" -i "$S/sfx100v2_misc_12.ogg" -i "$S/sfx100v2_misc_12.ogg" -i "$S/sfx100v2_metal_01.ogg" \
  -filter_complex "[0]aformat=channel_layouts=mono,$RS=pitch=0.5:tempo=0.75,pan=stereo|c0=0.9*c0|c1=0.6*c0[a];\
[1]aformat=channel_layouts=mono,$RS=pitch=0.53:tempo=0.72,pan=stereo|c0=0.6*c0|c1=0.9*c0[b];\
[2]aformat=channel_layouts=mono,$RS=pitch=0.3536:tempo=0.78,volume=0.9,pan=stereo|c0=c0|c1=c0[c];\
[3]aformat=channel_layouts=mono,$RS=pitch=0.5:tempo=0.2,highpass=f=150,volume=0.6,pan=stereo|c0=0.7*c0|c1=0.7*c0[d];\
[a][b][c][d]amix=4:duration=longest:normalize=0,lowpass=f=2400,afade=t=in:d=0.12,volume=0.8" \
  $WAV "$T/dh_drone.wav"
# boom: deep boom slowed + a stone hit an octave+ down + a falling sub sine
$F -i "$CC/SFX_DeepBoom.wav" -i "$S/sfx100v2_hit_01.ogg" -f lavfi -i "aevalsrc='0.9*sin(2*PI*(44*t-4*t*t))*exp(-t/1.0)':s=44100:d=3" \
  -filter_complex "[0]asetrate=44100*0.6,aresample=44100,volume=1.5[b];[1]aformat=channel_layouts=stereo,$RS=pitch=0.35,volume=1.8[h];[2]aformat=channel_layouts=stereo[s];[b][h][s]amix=3:duration=longest:normalize=0,atrim=0:3.5" \
  $WAV "$T/dh_boom.wav"
$F -i "$T/dh_swell.wav" -i "$T/dh_boom.wav" -i "$T/dh_drone.wav" \
  -filter_complex "[1]adelay=1500:all=1[b];[2]adelay=1450:all=1[d];[0][b][d]amix=3:duration=longest:normalize=0,highpass=f=25" \
  $WAV "$T/dh_dry.wav"
rev "$T/dh_dry.wav" "$T/dh_wet.wav" "$T/ir_hall.wav" 1 0.6 2
fin "$T/dh_wet.wav" "$OUT/SFX_DreadHit.ogg" "atrim=0:4.3" "afade=t=out:st=3.0:d=1.3" 3

# =====================================================================================
# 4. SFX_Whisper — close, overlapping, wordless whispers around the head
$F -i "$T/whisper_src.wav" -i "$T/whisper_src.wav" -i "$T/whisper_src.wav" \
   -f lavfi -i "anoisesrc=c=pink:seed=3:a=0.5:d=1.1" -f lavfi -i "anoisesrc=c=pink:seed=8:a=0.5:d=0.9" \
  -filter_complex "[0]atrim=2.0:3.5,asetpts=PTS-STARTPTS,tremolo=f=6:d=0.35,afade=t=in:d=0.25,afade=t=out:st=1.0:d=0.5,volume=1.1,pan=stereo|c0=0.95*c0|c1=0.35*c0[v1];\
[1]atrim=9.0:10.4,asetpts=PTS-STARTPTS,tremolo=f=7.5:d=0.4,afade=t=in:d=0.2,afade=t=out:st=0.9:d=0.5,adelay=800,volume=1.0,pan=stereo|c0=0.3*c0|c1=0.95*c0[v2];\
[2]atrim=15.0:16.3,asetpts=PTS-STARTPTS,$RS=pitch=0.92,tremolo=f=5:d=0.3,afade=t=in:d=0.3,afade=t=out:st=0.8:d=0.5,adelay=1650,volume=1.0,pan=stereo|c0=0.75*c0|c1=0.6*c0[v3];\
[3]bandpass=f=1600:w=1400,tremolo=f=3:d=0.6,afade=t=in:d=0.45:curve=qsin,afade=t=out:st=0.5:d=0.6,adelay=350,volume=0.35,pan=stereo|c0=0.8*c0|c1=0.4*c0[b1];\
[4]bandpass=f=2600:w=2000,tremolo=f=4:d=0.6,afade=t=in:d=0.3:curve=qsin,afade=t=out:st=0.4:d=0.5,adelay=2000,volume=0.3,pan=stereo|c0=0.4*c0|c1=0.8*c0[b2];\
[v1][v2][v3][b1][b2]amix=5:duration=longest:normalize=0,highpass=f=250" \
  $WAV "$T/wh_dry.wav"
rev "$T/wh_dry.wav" "$T/wh_wet.wav" "$T/ir_stone.wav" 1 0.3 0.6
fin "$T/wh_wet.wav" "$OUT/SFX_Whisper.ogg" "atrim=0:3.0" "afade=t=out:st=2.5:d=0.5" -2

# =====================================================================================
# 5. AMB_HallDread — 30 s seamless loop (render 34 s, crossfade the last 4 s into the first 4 s)
L=34
# far events in the hall: creaks, stone ticks, settling grit, one long faint scrape
$F -i "$S/sfx100v2_wood_01.ogg" -i "$S/sfx100v2_stones_01.ogg" -i "$S/sfx100v2_stones_03.ogg" -i "$S/sfx100v2_door_03.ogg" \
   -i "$S/sfx100v2_hit_03.ogg" -i "$S/sfx100v2_wood_03.ogg" -i "$S/sfx100v2_misc_28.ogg" -i "$S/sfx100v2_stones_02.ogg" \
  -filter_complex "[0]aformat=channel_layouts=stereo,$RS=pitch=0.5,volume=0.35,adelay=3000:all=1[e0];\
[1]aformat=channel_layouts=stereo,$RS=pitch=0.6,volume=0.3,pan=stereo|c0=c0|c1=0.4*c1,adelay=7600:all=1[e1];\
[2]aformat=channel_layouts=stereo,$RS=pitch=0.75,volume=0.22,pan=stereo|c0=0.4*c0|c1=c1,adelay=12900:all=1[e2];\
[3]aformat=channel_layouts=stereo,$RS=pitch=0.45:tempo=0.7,volume=0.3,adelay=16800:all=1[e3];\
[4]aformat=channel_layouts=stereo,$RS=pitch=0.5,volume=0.3,pan=stereo|c0=0.5*c0|c1=c1,adelay=22800:all=1[e4];\
[5]aformat=channel_layouts=stereo,$RS=pitch=0.4,volume=0.3,pan=stereo|c0=c0|c1=0.5*c1,adelay=26800:all=1[e5];\
[6]aformat=channel_layouts=mono,$RS=pitch=0.55:tempo=0.6,lowpass=f=1400,afade=t=in:d=0.6,afade=t=out:st=2.4:d=1,volume=0.16,pan=stereo|c0=0.45*c0|c1=0.9*c0,adelay=19300:all=1[e6];\
[7]aformat=channel_layouts=stereo,$RS=pitch=0.55,volume=0.18,adelay=9800:all=1[e7];\
[e0][e1][e2][e3][e4][e5][e6][e7]amix=8:duration=longest:normalize=0,lowpass=f=2600,apad=whole_dur=$L,atrim=0:$L" \
  $WAV "$T/hd_events.wav"
rev "$T/hd_events.wav" "$T/hd_eventsrev.wav" "$T/ir_hall.wav" 0.25 1 0
# beds: dark wind (slowed, darkened), basement air, low machine hum pitched into a drone, sub sine with slow swell
$F -i "$CC/AMB_Wind.ogg" -i "$D/atmosbasement.mp3_.flac" -i "$S/sfx100v2_loop_ambient_04.ogg" -i "$D/dungeon_ambient_1_0.ogg" \
   -i "$T/hd_eventsrev.wav" \
  -filter_complex "[0]aformat=channel_layouts=stereo,$RS=tempo=0.5:pitch=0.6,aloop=loop=4:size=2e9,atrim=0:$L,lowpass=f=700,volume=1.2[w];\
[1]aformat=channel_layouts=stereo,aloop=loop=2:size=2e9,atrim=3:$((L+3)),asetpts=PTS-STARTPTS,lowpass=f=1100,volume=0.6[bs];\
[2]aformat=channel_layouts=stereo,aloop=loop=4:size=2e9,atrim=0:$L,$RS=pitch=0.6,lowpass=f=320,volume=0.7[hum];\
[3]aformat=channel_layouts=stereo,atrim=40:$((L+40)),asetpts=PTS-STARTPTS,lowpass=f=2500,volume=0.35[dg];\
[4]atrim=0:$L[ev];\
[w][bs][hum][dg][ev]amix=5:duration=first:normalize=0,highpass=f=22,alimiter=limit=0.6:level=disabled,volume=1.3" \
  $WAV "$T/hd_full.wav"
# seamless loop: first 4 s = equal-power crossfade of (30..34 fading out) over (0..4 fading in), then 4..30
# then the sub drone (36.7/55 Hz, 10/15 s swells: exactly periodic in 30 s) goes on top un-crossfaded,
# so it doesn't double up coherently inside the crossfade
$F -i "$T/hd_full.wav" -f lavfi -i "aevalsrc='0.10*sin(2*PI*36.7*t)*(0.65+0.35*sin(2*PI*t/10))+0.06*sin(2*PI*55*t)*(0.5+0.5*sin(2*PI*t/15+1))':s=44100:d=30" \
  -filter_complex "[0]asplit=3[a][b][c];[a]atrim=0:4,asetpts=PTS-STARTPTS,afade=t=in:d=4:curve=qsin[h];[b]atrim=30:34,asetpts=PTS-STARTPTS,afade=t=out:d=4:curve=qsin[t];[c]atrim=4:30,asetpts=PTS-STARTPTS[m];[h][t]amix=2:normalize=0[x];[x][m]concat=n=2:v=0:a=1,atrim=0:30[bed];[1]aformat=channel_layouts=stereo,volume=1.3[sub];[bed][sub]amix=2:normalize=0" \
  $ENC "$OUT/AMB_HallDread.ogg"

rm -rf "$T"
ls -la "$OUT" | grep -E "Screech|PassBy|DreadHit|Whisper|HallDread"
