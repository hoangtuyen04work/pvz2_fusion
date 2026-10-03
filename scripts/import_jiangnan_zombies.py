from pathlib import Path
from PIL import Image
import shutil

SOURCE = Path('.asset_sources/NewPvZJS/images/Zombies')
DEST = Path('Assets/Resources/Sprites/Imported/JiangNan/Zombies')
ZOMBIES = (
    'PoleVaultingZombie', 'FootballZombie', 'ScreenDoorZombie',
    'BalloonZombie', 'JackinTheBoxZombie', 'DancingZombie',
    'BackupDancer', 'DolphinRiderZombie', 'SnorkelZombie',
    'Zomboni', 'Imp',
)

for zombie in ZOMBIES:
    source_dir = SOURCE / zombie
    output_dir = DEST / zombie
    output_dir.mkdir(parents=True, exist_ok=True)
    for gif_path in sorted(source_dir.glob('*.gif')):
        state_dir = output_dir / gif_path.stem
        state_dir.mkdir(parents=True, exist_ok=True)
        with Image.open(gif_path) as animation:
            for index in range(animation.n_frames):
                animation.seek(index)
                frame = animation.convert('RGBA')
                frame.save(state_dir / f'{gif_path.stem}_{index:03d}.png')

license_source = Path('.asset_sources/NewPvZJS/LICENSE-ENGLISH.md')
if license_source.exists():
    shutil.copy2(license_source, DEST / 'JIANGNAN_LICENSE.md')
