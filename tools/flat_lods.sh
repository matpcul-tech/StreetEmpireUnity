#!/bin/bash
# Flatten walls rerun of the Eastside kit buildings (Sovereign-draft claude/asset-flatten-walls 9ad9173 at :4175).
# full: se-building with Flatten walls Light and Strong; Strong is kept where it pressed more (Light left dents).
# near: se-building 26k cut of the kept file (same flatten level); far, low: Blender decimate of the kept file,
# then a no-cut se-building pass. A second no-cut pass where a level comes back NOT READY. Same as build_lods.sh.
n=$1; B=~/apps/blender-4.2.23-linux-x64/blender; R=/workspace/StreetEmpireUnity/Assets/Art/Models/Buildings; O=flat
sa() { FIXNORMALS=1 FLAT=$5 timeout 900 node sa-flat.mjs "$1" "$2" se-building "$3" "$4" > "$2/$(basename $1 .glb).log" 2>&1; }
ready() { python3 -c "import json;print(json.load(open('$1'))['banner'])" 2>/dev/null || echo MISSING; }
moved() { rg -o "moved more than [0-9.]+" $1 2>/dev/null | rg -o "[0-9.]+$" | head -1; }
mkdir -p $O/light $O/strong $O/full $O/near $O/far $O/low $O/raw $O/pass2
if [ "$n" = "store-pawn" ]; then
  cp /workspace/sa-grayblock/flatten-light/store-pawn-se-building-light.glb $O/full/$n.glb
  cp /workspace/sa-grayblock/flatten-light/store-pawn-se-building-light.verdict.json $O/full/$n.verdict.json
  cp /workspace/sa-grayblock/flatten-light/store-pawn-se-building-light.report.txt $O/full/$n.report.txt; lvl=light
else
  sa $R/$n.glb $O/light "" 1024 light & sa $R/$n.glb $O/strong "" 1024 strong & wait
  ml=$(moved $O/light/$n.report.txt); ms=$(moved $O/strong/$n.report.txt); lvl=light
  if [ "$(ready $O/strong/$n.verdict.json)" = "READY" ] && python3 -c "import sys;sys.exit(0 if float('${ms:-0}')>1.5*float('${ml:-0}')+0.01 else 1)"; then lvl=strong; fi
  [ "$(ready $O/$lvl/$n.verdict.json)" = "READY" ] || lvl=light
  cp $O/$lvl/$n.glb $O/$lvl/$n.verdict.json $O/$lvl/$n.report.txt $O/full/
fi
echo "$n full $lvl $(ready $O/full/$n.verdict.json) light_moved=$ml strong_moved=$ms"
if [ "$n" = "house-03-duplex" ]; then cp $O/full/$n.glb $O/full/$n.verdict.json $O/full/$n.report.txt $O/near/; else sa $O/full/$n.glb $O/near 26000 1024 $lvl; fi
$B -b -P /workspace/StreetEmpireUnity/tools/decimate_far.py -- $O/full/$n.glb $O/raw/far-$n.glb 0.22 >/dev/null 2>&1
$B -b -P /workspace/StreetEmpireUnity/tools/decimate_far.py -- $O/full/$n.glb $O/raw/low-$n.glb 0.06 >/dev/null 2>&1
mkdir -p $O/rawfar $O/rawlow; cp $O/raw/far-$n.glb $O/rawfar/$n.glb; cp $O/raw/low-$n.glb $O/rawlow/$n.glb
sa $O/rawfar/$n.glb $O/far "" 512 $lvl & sa $O/rawlow/$n.glb $O/low "" 512 $lvl & wait
for L in near far low; do
  if [ "$(ready $O/$L/$n.verdict.json)" != "READY" ]; then mkdir -p $O/pass2/$L; sa $O/$L/$n.glb $O/pass2/$L "" 512 $lvl; cp $O/pass2/$L/$n.* $O/$L/ 2>/dev/null; echo "$n $L second pass $(ready $O/$L/$n.verdict.json)"; fi
done
echo "RESULT $n $lvl full=$(ready $O/full/$n.verdict.json) near=$(ready $O/near/$n.verdict.json) far=$(ready $O/far/$n.verdict.json) low=$(ready $O/low/$n.verdict.json)"
