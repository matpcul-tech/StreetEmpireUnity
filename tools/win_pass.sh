#!/bin/bash
# window maps (tools/window_maps.mjs) on the flattened full file, then a Sovereign Asset se-building pass (no cut)
n=$1; O=flat; i=$2
lvl=$(python3 -c "import json;print(json.load(open('$O/full/$n.verdict.json')).get('flatten','light'))" 2>/dev/null); [ "$lvl" = "none" -o -z "$lvl" ] && lvl=light
mkdir -p $O/win $O/final
(cd /workspace/gltf-tools && node window_maps.mjs /workspace/se-props/$O/full/$n.glb /workspace/se-props/$O/win/$n.glb $i)
FIXNORMALS=1 FLAT=$lvl timeout 900 node sa-flat.mjs $O/win/$n.glb $O/final se-building "" 1024 > $O/final/$n.log 2>&1
echo "FINAL $n $lvl $(python3 -c "import json;print(json.load(open('$O/final/$n.verdict.json'))['banner'])" 2>/dev/null)"
