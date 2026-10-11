from pathlib import Path
from PIL import Image
import xml.etree.ElementTree as ET

SOURCE = Path('PvZ assets/PvZ assets/reanim')
DEST = Path('Assets/Resources/Sprites/Imported/OriginalPvZ/Pults/Rendered')
PLANTS = ('Cabbagepult', 'Cornpult', 'Melonpult', 'Umbrellaleaf')
# Render into a roomy working canvas first. Several pult attack poses swing the
# basket well outside the idle bounds, so cropping directly in the final tile
# clips authentic animation frames.
CANVAS = 320
ORIGIN = (128, 96)
OUTPUT_SIZE = 256


def value(node, name, default=None):
    child = node.find(name)
    return default if child is None else child.text


def inherited_frames(track):
    pose = {'x': 0.0, 'y': 0.0, 'sx': 1.0, 'sy': 1.0,
            'kx': 0.0, 'ky': 0.0, 'a': 1.0, 'f': -1, 'i': None}
    result = []
    for node in track.findall('t'):
        pose = pose.copy()
        for key in ('x', 'y', 'sx', 'sy', 'kx', 'ky', 'a'):
            raw = value(node, key)
            if raw is not None:
                pose[key] = float(raw)
        raw = value(node, 'f')
        if raw is not None:
            pose['f'] = int(raw)
        raw = value(node, 'i')
        if raw is not None:
            pose['i'] = raw
        result.append(pose)
    return result


def clip_range(frames):
    start = next((i for i, p in enumerate(frames) if p['f'] == 0), 0)
    end = next((i for i in range(start + 1, len(frames)) if frames[i]['f'] == -1), len(frames))
    return start, max(start + 1, end)


def image_name(resource):
    if not resource:
        return None
    return resource.replace('IMAGE_REANIM_', '').lower()


def find_texture(resource):
    wanted = image_name(resource)
    if not wanted:
        return None
    normalized = wanted.replace('_', '')
    for path in SOURCE.glob('*.png'):
        if path.stem.lower().replace('_', '') == normalized:
            return path
    return None


def render_part(source, pose):
    image = source.convert('RGBA')
    width = max(1, round(image.width * abs(pose['sx'])))
    height = max(1, round(image.height * abs(pose['sy'])))
    image = image.resize((width, height), Image.Resampling.LANCZOS)
    if pose['sx'] < 0:
        image = image.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    if pose['sy'] < 0:
        image = image.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
    image = image.rotate(-pose['kx'], Image.Resampling.BICUBIC, expand=True)
    if pose['a'] < 1:
        alpha = image.getchannel('A').point(lambda x: round(x * max(0, pose['a'])))
        image.putalpha(alpha)
    return image


for plant in PLANTS:
    text = (SOURCE / f'{plant}.reanim').read_text(encoding='utf-8-sig')
    root = ET.fromstring('<root>' + text + '</root>')
    all_tracks = []
    clips = {}
    for node in root.findall('track'):
        name = value(node, 'name', '')
        frames = inherited_frames(node)
        has_image = any(p['i'] for p in frames)
        if name.startswith('anim_') and not has_image:
            clips[name] = (frames, *clip_range(frames))
        else:
            texture = next((find_texture(p['i']) for p in frames if p['i']), None)
            if texture:
                all_tracks.append((name, frames, texture))

    rendered_paths = []
    for clip, (root_frames, start, end) in clips.items():
        if clip not in ('anim_idle', 'anim_shooting', 'anim_full_idle', 'anim_block'):
            continue
        output_name = clip.replace('anim_', '').title().replace('_', '')
        variants = [(output_name, set())]
        if plant == 'Cornpult' and clip == 'anim_shooting':
            variants = [('ShootingKernel', {'Cornpult_butter'}),
                        ('ShootingButter', {'Cornpult_kernal'})]
        for variant_name, hidden_tracks in variants:
            output = DEST / plant / variant_name
            output.mkdir(parents=True, exist_ok=True)
            for frame_index in range(start, end):
                canvas = Image.new('RGBA', (CANVAS, CANVAS), (0, 0, 0, 0))
                for track_name, frames, texture in all_tracks:
                    if track_name in hidden_tracks:
                        continue
                    pose = frames[min(frame_index, len(frames) - 1)]
                    if pose['f'] == -1 or not pose['i']:
                        continue
                    part = render_part(Image.open(texture), pose)
                    # Reanim coordinates address the source image's upper-left
                    # registration point (Unity sprites themselves use a centre pivot).
                    x = ORIGIN[0] + pose['x']
                    y = ORIGIN[1] + pose['y']
                    canvas.alpha_composite(part, (round(x), round(y)))
                path = output / f'{plant}_{clip}_{frame_index-start:03d}.png'
                canvas.save(path)
                rendered_paths.append(path)

    # Use one crop for every clip so the plant fills its tile while remaining
    # locked to the same baseline and scale during animation.
    union = None
    idle_union = None
    crop_sources = rendered_paths
    for path in crop_sources:
        bbox = Image.open(path).getchannel('A').getbbox()
        if bbox:
            union = bbox if union is None else (
                min(union[0], bbox[0]), min(union[1], bbox[1]),
                max(union[2], bbox[2]), max(union[3], bbox[3]))
            if path.parent.name == 'Idle':
                idle_union = bbox if idle_union is None else (
                    min(idle_union[0], bbox[0]), min(idle_union[1], bbox[1]),
                    max(idle_union[2], bbox[2]), max(idle_union[3], bbox[3]))
    if union:
        pad = 5
        union = (max(0, union[0]-pad), max(0, union[1]-pad),
                 min(CANVAS, union[2]+pad), min(CANVAS, union[3]+pad))
        scale_bounds = idle_union or union
        # Size the character from its idle pose, while reserving transparent
        # room for the full attack swing. This keeps it readable in-game and
        # prevents apparent shrinking when it fires.
        scale = min(164/(scale_bounds[2]-scale_bounds[0]),
                    164/(scale_bounds[3]-scale_bounds[1]),
                    236/(union[2]-union[0]), 236/(union[3]-union[1]))
        for path in rendered_paths:
            cropped = Image.open(path).crop(union)
            size = (max(1, round(cropped.width*scale)), max(1, round(cropped.height*scale)))
            cropped = cropped.resize(size, Image.Resampling.LANCZOS)
            framed = Image.new('RGBA', (OUTPUT_SIZE, OUTPUT_SIZE), (0,0,0,0))
            framed.alpha_composite(cropped, ((OUTPUT_SIZE-size[0])//2, OUTPUT_SIZE-size[1]-10))
            framed.save(path)
