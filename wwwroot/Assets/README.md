# Trigger Item Variant Creator (Gate Variant Creator)
SourceCode: [https://github.com/AchimBunke/TM_TriggerSurfaceMapping](https://github.com/AchimBunke/TM_TriggerSurfaceMapping)

This tool is a C# implementation of the [Custom Gates](https://openplanet.dev/file/112) ([Github](https://github.com/schadocalex/gbx-py/blob/a140ca0f51664fc2e935f40a74f859672ed99861/custom_gates.py)) tool.

Items have similar blender requirements as with the original tool:

- \_trigger\_ objects define the zone where the gameplay takes effect
- Materials are resolved via their Link, so by using TriggerFX, SpecialFX, Sign, SignOff, Decal links. 
  (The material name does not matter, also the effect of the link e.g. Turbo, Boost, etc. does not matter)
- export item as usual
- Drag&Drop it onto the exe file, it will create all available variants of the effect as a .zip file

## Notes

- No guarantee it will work with all items! There is a current problem with parsing items with trigger effects in GBX.NET
- Not all effects have Icons, Decals, Signs and TriggerFX. For those either a default one or a texture from another effect is used.
