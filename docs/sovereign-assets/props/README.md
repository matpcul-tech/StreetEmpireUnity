# Sovereign Asset results for the Eastside props

Each prop went through the Sovereign Asset app (/asset.html, Sovereign-draft, built and served locally) driven
through its own page controls: preset "game", textures capped (2048 for the hero props seen up close: hydrants,
bench, trash can, street lamp; 1024 for the rest), format kept, no zip, ground and center on, Fix normals and
Fix facing on. `<name>.report.txt` is the app's own report and `<name>.verdict.json` its banner and checks.

All 43 finished files are READY. Three Poly Haven props (hydrant, hydrant-aged, aircon) came out NOT READY on
the first pass with a triangle cut (the Normals check failed: the app fixes normals before it cuts, and the cut
flipped some triangles). A second Sovereign Asset pass on the cut file with Fix normals and no further cut made
them READY; the verdicts here are from that second pass. The covered car did the same.

Sovereign Asset has no Street Empire prop preset in /asset.html (its se-* presets are car, motorcycle,
building and character), so "game" was the closest flow. Steps that applied: load, cleanup (debris, facing,
normals), triangle cut where the source was over budget, texture resize, ground and center, export, checks.
A Street Empire prop preset (triangle budget per prop class, 2k or 1k texture cap, keep format, a normals fix
after the cut) would make this one click per prop.
