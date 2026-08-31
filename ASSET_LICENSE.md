# Asset licensing

The current Unity migration baseline uses engine primitive meshes, code-created
materials, IMGUI, colors and procedural presentation. No third-party art,
audio, animation, font, texture or model asset is bundled.

## Current project-owned / generated artifacts

| Asset | Method | License / status |
|---|---|---|
| `Assets/_BladeBreath/Scripts/Prototype/PrototypeBootstrap.cs` presentation | Unity primitives and project code | Covered by repository code license |
| `prototype-web/*` presentation | Original code-generated shapes, particles and UI | Covered by repository code license |

Unity packages declared in `Packages/manifest.json` are dependencies distributed
under their own package licenses; they are not project-owned art assets.

Museum and historical links in design documents are research references, not
bundled production assets. Using an image, scan or 3D capture in the game
requires a new entry recording the exact source, object ID, license,
modifications and production purpose.

Generated concept art remains reference material unless a later entry marks it
production-ready and records the model/tool, date, prompt lineage, human
changes, intended distribution rights and any platform restrictions.
