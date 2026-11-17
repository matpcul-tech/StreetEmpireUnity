# Stage 3 near re-cuts (Nov 17, 2026)

The three near models Sovereign Asset left above 26k triangles.

| Building | before | Sovereign Asset re-cut (26k target) | Blender decimate to 26k, then SA no-cut check | used |
|---|---|---|---|---|
| apt-02-brick | 49,010 | NOT READY, stays 49,010 ("going lower would visibly change the shape") | READY, 26,008 | decimate (26,008) |
| shop-auto-garage | 35,170 | NOT READY, 35,160 (same reason) | READY, 25,995 | decimate (25,995) |
| apt-01-stucco | 38,671 | NOT READY, 38,667 (same reason) | READY, 25,995, but the recessed wall behind the fire escape tore open (also at 55%, 31,899) | kept 38,671 |

Sovereign Asset passes the torn apt-01 file as READY (open edges are fine for games), so it was rejected by eye:
recut-sheet.jpg, left the old near, right the new one. The decimated files carry no textures; the game binds the
extracted near textures, and the decimate keeps the UVs.
