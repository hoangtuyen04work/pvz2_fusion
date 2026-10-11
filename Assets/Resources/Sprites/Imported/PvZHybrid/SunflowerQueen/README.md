# Sunflower Queen source assets

Plant names:

- Chinese: `向日葵女王`
- English: `Sunflower Queen`
- Internal animation: `SunKing`

## Source

- Release repository: https://github.com/nextisme/Plants.vs.Zombies-hybrid
- Release tag: `PvZ-hybrid-2.6.1`
- Archive: `v.2.6.1.zip`
- Archive SHA-256: `9B5B6D43994986218F138E5D9CFD824EBEB594F44CBDF6733CC63D0933BDDC08`
- Extracted from the embedded `main.pak` in `pvzHE-Launcher.exe` without
  executing the installer or launcher.
- Imported on: 2026-09-18

The `SunKing.reanim.compiled` file is retained only as upstream animation
layout/reference data. Unity does not play that PopCap reanimation format
directly.

## Included content

- `FireRing/`: 12 transparent 500x500 animation frames.
- `Parts/`: crown, fire sunflower head/petals, three small flame frames,
  plant stem/leaves, and the red cloak pieces referenced by `SunKing`.
- `Source/`: upstream compiled reanimation data used to identify the exact
  component set.

The original upstream filenames are preserved so the IDs embedded in the
reanimation data remain traceable.

## Licensing warning

The release repository does not provide an artwork license. These files are
fan-mod assets derived from Plants vs. Zombies and are not covered by this
project's MIT source-code license. Plants vs. Zombies and its original artwork
belong to EA/PopCap; PvZ Hybrid additions belong to their respective creator(s).

Treat this pack as provenance-tracked material for personal study and
non-commercial fan work only. Do not redistribute it as independently licensed
art or use it commercially without permission from the relevant rights holders.
