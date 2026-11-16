#!/bin/sh
# GLB character (Mixamo rig, A pose) -> T-posed FBX plus JPEG textures under Assets/Art/Models/Characters/<Name>/.
# Usage: tools/convert_character.sh <in.glb> <Name> <file-prefix>
set -e
B=${BLENDER:-$HOME/apps/blender-4.2.23-linux-x64/blender}
src=$1; name=$2; pre=$3; dir=Assets/Art/Models/Characters/$name; tmp=$(mktemp -d)
mkdir -p "$dir"
"$B" -b --python tools/glb_to_fbx.py -- "$src" "$tmp/a.fbx" mesh > "$tmp/log1.txt" 2>&1 || { tail -20 "$tmp/log1.txt"; exit 1; }
"$B" -b --python tools/tpose_fbx.py -- "$tmp/a.fbx" "$dir/$pre.fbx" > "$tmp/log2.txt" 2>&1 || { tail -20 "$tmp/log2.txt"; exit 1; }
grep -h "missing\|wrote" "$tmp/log2.txt" || true
node tools/extract_textures.mjs "$src" "$dir/$pre"
rm -rf "$tmp" "$dir/$pre.fbm"
