# Original PvZ pult assets

Plant part textures and `.reanim` animation data for Cabbage-pult, Kernel-pult (`Cornpult` internally), Melon-pult, and Umbrella Leaf were copied from the extracted original PvZ asset set already present at `PvZ assets/PvZ assets/`.

The same raw asset layout is documented by https://github.com/FregD156/PvZ_Assets. `scripts/render_original_pvz_plants.py` deterministically composes the original reanimation tracks into transparent, pivot-stable frame sequences for Unity. Runtime playback uses those generated frames at 12 FPS. Projectile textures are the original `Cabbagepult_cabbage`, `Cornpult_kernal`, `Cornpult_butter`, and `Melonpult_melon` resources.

Kernel-pult shooting is rendered as separate `ShootingKernel` and `ShootingButter` sequences. Runtime release timing follows the original projectile-track visibility boundary for each plant, so the spawned projectile replaces the projectile drawn in the last loaded-basket frame without an early pop or duplicated frame.

The rejected wiki/gameplay preview downloads are not part of this import; seed packets use the first verified idle frame from the matching original animation.

Imported 2026-10-02. Original Plants vs. Zombies artwork and audio remain property of PopCap Games / Electronic Arts.
