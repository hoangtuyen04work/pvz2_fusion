# Hybrid plant sprites

These production sprites were generated for this project from the user's Sunflower Queen visual reference.

- `PeaTorch`: Peashooter + Torchwood
- `TorchSun`: Torchwood + Sunflower
- `SunPea`: Sunflower + Peashooter
- `SunflowerQueen`: final three-plant evolution

Each `Frames` folder contains four idle frames followed by four action frames. `Source` keeps the original 4x2 generated sheet so the animation can be adjusted later without losing the source image.

## Gameplay inheritance

- Any hybrid containing Peashooter fires pea projectiles.
- Any hybrid containing Torchwood warms nearby plants and converts passing normal peas into the existing `FirePea` projectile.
- `PeaTorch` fires one `FirePea`; `SunPea` fires one normal `PeaBullet` and produces sun.
- `TorchSun` produces sun and provides Torchwood ignition without gaining a pea attack.
- `SunflowerQueen` fires three `FirePea` projectiles per volley at zombies across all lawn rows and produces sun.
- All pea-shooting plants use the original `PeaBullet` artwork; Torchwood ignition replaces a normal projectile with the original animated `FirePea` prefab.
- `SunflowerQueen` is the terminal evolution and rejects every further fusion attempt.
