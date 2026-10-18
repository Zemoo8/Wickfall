#!/bin/sh
# Re-encodes a gameplay video smaller (same size and look) when it is over 29 MB, so it can be shared.
# Usage: sh Tools/shrink_video.sh Builds/Videos/<clip>.mp4
f="$1"
size=$(wc -c < "$f")
[ "$size" -le 29000000 ] && { echo "$f: $size bytes, kept"; exit 0; }
tmp="${f%.mp4}_small.mp4"
ffmpeg -loglevel error -y -i "$f" -c:v libx264 -preset slow -crf 28 -pix_fmt yuv420p -c:a aac -b:a 96k -movflags +faststart "$tmp" && mv -f "$tmp" "$f"
echo "$f: $(wc -c < "$f") bytes"
