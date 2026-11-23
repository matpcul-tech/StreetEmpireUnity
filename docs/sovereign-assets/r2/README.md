Street Empire wave A and B buildings, r2 remesh. Prerelease for review, not game ready.

Batch 1 is the 7 buildings that passed review as whole remeshes. Later buildings use the split method and are added here as they pass. job-car-wash and warehouse-cold-storage stay as delivered and are not repeated here.

Whole remesh method: Meshy remesh (free) from each original High Detail model, never decimated. Each file is scaled to the as delivered height, grounded with its origin at ground center, same facing, same footprint (within 2.4 cm). Material names, count and order match the game files, including windows_lit with its glow, by giving each remesh triangle the material of the nearest as delivered triangle. Textures are 1024 (color and normal JPEG, metal roughness JPEG). job-loading-lot came back from Meshy with its UVs packed into a thin strip, so it got new UV charts and its color, roughness and normal were baked from the as delivered file onto the same remesh surface.

Split method (per Matthew's Nov 23 instructions): the as delivered file is split by material. Only the wall and roof surface comes from the Meshy remesh. windows_lit, windows_dark, door and the window frame geometry around them (within the frame ring of each pane) are the as delivered triangles, unmoved and not re triangulated, joined back onto the remeshed walls. Where a remesh triangle was torn, folded or crossed a window it was cut and the as delivered triangles under it fill the gap, so the seams overlap slightly instead of opening. Remesh triangles facing into the wall had their winding fixed. Color, roughness and normal are baked from the as delivered file into one new 1024 atlas per building. Joined parts are not welded, so the topology check counts open edges along the seams; the ray test from four sides and above finds no see through areas.

| file | triangles (as delivered) | MB | method |
|---|---|---|---|
| civic-church | 66,297 (96,044) | 3.81 | whole remesh |
| tower-copper-mansard | 57,522 (83,279) | 4.64 | whole remesh |
| crew-redkings-club | 25,658 (214,254) | 1.98 | whole remesh |
| civic-clinic | 23,182 (80,754) | 1.82 | whole remesh |
| dock-ice-house | 25,846 (249,464) | 1.88 | whole remesh |
| job-pool-hall | 23,923 (88,972) | 1.89 | whole remesh |
| job-loading-lot | 20,695 (101,064) | 1.92 | whole remesh |
| dock-pier-warehouse | 45,437 (84,618) | 3.72 | split |
| job-chop-shop | 79,452 (93,874) | 5.66 | split |
| warehouse-sawtooth | 65,178 (92,761) | 4.46 | split |
| civic-school | 65,713 (86,872) | 5.14 | split |
| civic-police-station | 88,034 (184,272) | 6.38 | split |

Flags:
- copper mansard is just under the 60k band and over the 4 MB target (its vertex data alone is about 3.5 MB). Wall textures are a little softer up close than the 4K originals.
- dock-pier-warehouse: split method, 45,437 tris, over the 20k to 40k box band only because of as delivered fill at the cut seams. Clapboard relief slightly softer.
- job-chop-shop: split method, 79,452 tris, over the 20k to 40k box band because the as delivered roll up doors and wall fill under the cut seams are kept (the lighter fill left black slits on the doors). One faint dark line on the back wall; walls a little softer.
- warehouse-sawtooth: split method, 65,178 tris, over the 50k cap because the as delivered glass, door and frames alone are about 30k. Brick slightly softer, faint creases under the parapet.
- civic-school: split method, 65,713 tris, over the 20k to 40k box band because the as delivered frames and frame ring alone are about 46k. Brick courses noticeably softer up close.
- civic-police-station: split method, 88,034 tris, over the 20k to 40k box band because the as delivered frames, sills and wall fill under the cut seams are kept. Faint thin crack lines on some wall faces up close, brick softer.

MANIFEST.txt lists bytes, triangles and SHA-256 for every file. r2-views-front-back-side.jpg has front 3/4, back 3/4 and side views with triangle counts. as-delivered-vs-r2.jpg compares day, night and close views.

The as delivered release meshy-buildings-wave-ab-2026-11-22 stays up.
