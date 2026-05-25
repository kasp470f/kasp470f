import argparse
import datetime
import os
import sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import requests

GITHUB_GRAPHQL = "https://api.github.com/graphql"

# color palettes for variants
PALETTES = {
    'dark': {
        'background': (7, 8, 15),
        'green': (82, 225, 163),
        'glow': (40, 120, 80),
        'text': (220, 255, 240),
        'shadow': (180, 200, 200, 150),
    },
    'light': {
        'background': (245, 246, 250),
        'green': (10, 130, 60),
        'glow': (160, 230, 200),
        'text': (60, 60, 60, 180),
        'shadow': (0, 0, 0, 100),
    }
}

SQUARE = 10
PADDING = 4
# Maximum banner height in pixels. Set to None to disable the clamp.
MAX_BANNER_HEIGHT = 120

import random
import math

class Particle:
    def __init__(self, w, h):
        self.w = w
        self.h = h
        self.reset()

    def reset(self):
        self.x = random.random() * self.w
        self.y = random.random() * self.h
        self.size = random.random() * 4 + 2
        self.depth = random.random() * 0.5 + 0.5
        self.brightness = random.random() * 0.3 + 0.2
        self.glowStrength = random.random() * 20 + 5
        self.glowAlpha = random.random() * 0.8 + 0.2

    def update(self, windX, windY):
        self.x += windX * self.depth
        self.y += windY * self.depth
        if self.x > self.w + 20:
            self.x = -20
        if self.x < -20:
            self.x = self.w + 20
        if self.y > self.h + 20:
            self.y = -20
        if self.y < -20:
            self.y = self.h + 20

def make_particles(count, w, h):
    return [Particle(w, h) for _ in range(count)]

def update_wind(wind):
    # wind is dict with angle and strength
    wind['angle'] += (random.random() - 0.5) * 0.004
    wind['x'] = math.cos(wind['angle']) * wind['strength']
    wind['y'] = math.sin(wind['angle']) * wind['strength']


def fetch_contributions(username: str, year: int, token: str | None):
    from_iso = f"{year}-01-01T00:00:00Z"
    to_iso = f"{year}-12-31T23:59:59Z"
    query = """
    query($login: String!, $from: DateTime!, $to: DateTime!) {
      user(login: $login) {
        contributionsCollection(from: $from, to: $to) {
          contributionCalendar {
            weeks {
              contributionDays {
                date
                contributionCount
              }
            }
          }
        }
      }
    }
    """
    variables = {"login": username, "from": from_iso, "to": to_iso}
    headers = {"Accept": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    resp = requests.post(GITHUB_GRAPHQL, json={"query": query, "variables": variables}, headers=headers)
    resp.raise_for_status()
    data = resp.json()
    if "errors" in data:
        raise RuntimeError(f"GitHub API error: {data['errors']}")
    weeks = data["data"]["user"]["contributionsCollection"]["contributionCalendar"]["weeks"]
    return weeks


def build_grid(weeks):
    n_weeks = len(weeks)
    grid = [[0] * n_weeks for _ in range(7)]
    for wi, week in enumerate(weeks):
        for di, day in enumerate(week["contributionDays"]):
            grid[di][wi] = day["contributionCount"]
    return grid


def draw_grid_image(grid, square=12, padding=4, header="", font=None, header_size=48, header_offset=0, glow=True, particles=None, wind=None, palette=None):
    rows = 7
    cols = len(grid[0])
    grid_w = cols * square + (cols - 1) * padding
    grid_h = rows * square + (rows - 1) * padding
    # reserve space for header; make it proportional but not excessive
    text_h = max(36, int(header_size * 1.2))
    margin = 20
    img_w = grid_w + margin * 2
    # reduce bottom void by using two margins total instead of three
    img_h = grid_h + text_h + margin * 2

    # clamp to MAX_BANNER_HEIGHT if set — shrink the grid area (particle area)
    if MAX_BANNER_HEIGHT is not None and img_h > MAX_BANNER_HEIGHT:
        img_h = MAX_BANNER_HEIGHT
        # recompute grid_h to fit inside the clamped img_h
        grid_h = img_h - text_h - margin * 2
        # ensure grid_h is at least enough for the rows of squares
        min_grid_h = rows * square + (rows - 1) * padding
        if grid_h < min_grid_h:
            grid_h = min_grid_h
            img_h = grid_h + text_h + margin * 2

    # resolve palette
    if palette is None:
        palette = PALETTES.get('dark')
    bg = palette.get('background')
    green = palette.get('green')
    glow_color = palette.get('glow')
    text_col = palette.get('text', (220, 255, 240))
    shadow_col = palette.get('shadow', (180, 200, 200, 150))

    def _with_alpha(col, a=255):
        return col if len(col) == 4 else (col[0], col[1], col[2], a)

    base = Image.new("RGBA", (img_w, img_h), _with_alpha(bg, 255))

    glow_layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow_layer)

    solid_layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    solid_draw = ImageDraw.Draw(solid_layer)

    start_x = margin
    start_y = margin + text_h + margin

    max_count = max((grid[r][c] for r in range(rows) for c in range(cols)), default=0)

    # draw particle background if provided
    if particles is not None:
        for p in particles:
            gx = int(p.x)
            gy = int(p.y)
            r = int(p.size * 1.5)
            # glow color uses palette glow; scale alpha by particle glowAlpha
            gc = glow_color if len(glow_color) == 3 else glow_color[:3]
            glow_col = (int(gc[0]), int(gc[1]), int(gc[2]), int(160 * p.glowAlpha))
            glow_draw.ellipse([gx - r, gy - r, gx + r, gy + r], fill=glow_col)
            # particle solid color derived from palette green, weighted by brightness
            solid_rgb = tuple(min(255, max(0, int(green[i] * (0.6 + 0.4 * p.brightness) + (255 * (1 - p.brightness) * 0.15)))) for i in range(3))
            solid_alpha = int(200 * (0.35 + p.brightness * 0.65))
            solid_draw.rounded_rectangle([gx, gy, gx + int(p.size), gy + int(p.size)], radius=2, fill=(solid_rgb[0], solid_rgb[1], solid_rgb[2], solid_alpha))

    # NOTE: contribution grid removed — only render particle background and centered text
    # (grid data still used to determine image size)

    if glow:
        blurred = glow_layer.filter(ImageFilter.GaussianBlur(radius=max(2, square / 1.2)))
        base = Image.alpha_composite(base, blurred)
    base = Image.alpha_composite(base, solid_layer)

    draw = ImageDraw.Draw(base)
    # render header (centered, larger)
    if header:
        # load local Press Start 2P from repo fonts/ (guaranteed present)
        script_dir = os.path.dirname(__file__)
        press_path = os.path.join(script_dir, "fonts", "PressStart2P-Regular.ttf")
        try:
            header_font = ImageFont.truetype(press_path, header_size)
        except Exception:
            header_font = ImageFont.load_default()
        # try to center using anchor='mm' at the midpoint of the reserved header area
        cx = img_w // 2
        # move center slightly down for better visual centering (smaller nudge)
        cy = margin + text_h // 2 + int(header_size * 0.20) + int(header_offset)
        try:
            draw.text((cx + 2, cy + 2), header, font=header_font, fill=_with_alpha(shadow_col), anchor="mm")
            draw.text((cx, cy), header, font=header_font, fill=_with_alpha(text_col), anchor="mm")
        except Exception:
            # fallback: measure bbox and center manually
            try:
                bbox = draw.textbbox((0, 0), header, font=header_font)
                hw = bbox[2] - bbox[0]
                hh = bbox[3] - bbox[1]
            except Exception:
                try:
                    hw, hh = header_font.getsize(header)
                except Exception:
                    hw = len(header) * 10
                    hh = header_size
            htx = (img_w - hw) // 2
            hty = margin + max(0, (text_h - hh) // 2)
            draw.text((htx + 2, hty + 2), header, font=header_font, fill=_with_alpha(shadow_col))
            draw.text((htx, hty), header, font=header_font, fill=_with_alpha(text_col))

    # If a max height is set, crop the bottom so the image is anchored at the top
    if MAX_BANNER_HEIGHT is not None and base.size[1] > MAX_BANNER_HEIGHT:
        base = base.crop((0, 0, img_w, MAX_BANNER_HEIGHT))

    final = base.convert("P", palette=Image.ADAPTIVE)
    return final


def generate_frames(grid, square=12, padding=4, header="", header_size=48, header_offset=0, font=None, palette=None):
    cols = len(grid[0])
    frames = []

    # compute image size (same logic as draw_grid_image)
    rows = 7
    grid_w = cols * square + (cols - 1) * padding
    grid_h = rows * square + (rows - 1) * padding
    # note: mirror sizing logic from draw_grid_image
    text_h = max(36, int(header_size * 1.2))
    margin = 20
    img_w = grid_w + margin * 2
    img_h = grid_h + text_h + margin * 2
    if MAX_BANNER_HEIGHT is not None and img_h > MAX_BANNER_HEIGHT:
        img_h = MAX_BANNER_HEIGHT
        grid_h = img_h - text_h - margin * 2
        min_grid_h = rows * square + (rows - 1) * padding
        if grid_h < min_grid_h:
            grid_h = min_grid_h
            img_h = grid_h + text_h + margin * 2

    # particle background setup
    particle_count = min(300, max(60, img_w // 2))
    particles = make_particles(particle_count, img_w, img_h)
    wind = { 'angle': random.random() * math.pi * 2, 'strength': random.random() * 0.8 + 0.2 }
    update_wind(wind)

    # number of frames for the reveal animation (use weeks count as baseline)
    frames_count = max(30, cols)
    for _ in range(frames_count):
        # update wind and particles for this frame
        update_wind(wind)
        for p in particles:
            p.update(wind['x'], wind['y'])

        img = draw_grid_image(grid, square=square, padding=padding, header=header, header_size=header_size, header_offset=header_offset, font=font, particles=particles, wind=wind, palette=palette)
        frames.append(img)

    # final hold frames
    last = frames[-1]
    for _ in range(8):
        # advance particles slightly to keep subtle motion
        update_wind(wind)
        for p in particles:
            p.update(wind['x'], wind['y'])
        frames.append(draw_grid_image(grid, square=square, padding=padding, header=header, header_size=header_size, header_offset=header_offset, font=font, particles=particles, wind=wind, palette=palette))

    return frames


def save_gif(frames, out_path, duration=120):
    if not frames:
        raise RuntimeError("No frames to save")
    frames[0].save(out_path, save_all=True, append_images=frames[1:], duration=duration, loop=0, optimize=True)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--username", required=True)
    p.add_argument("--year", type=int, default=datetime.date.today().year)
    p.add_argument("--token", default=None, help="GitHub token (optional). If not set, reads GITHUB_TOKEN env var.)")
    p.add_argument("--header", default="", help="Header text to render at the top (default empty)")
    p.add_argument("--header-size", type=int, default=48, help="Header font size in px")
    p.add_argument("--header-offset", type=int, default=24, help="Vertical offset in pixels to nudge header down (can be negative)")
    args = p.parse_args()
    # use header size as provided (default 48)

    token = args.token or os.getenv("GITHUB_TOKEN")
    try:
        weeks = fetch_contributions(args.username, args.year, token)
    except Exception as e:
        print("Failed to fetch contributions:", e)
        sys.exit(1)

    grid = build_grid(weeks)
    font = ImageFont.load_default()

    header_text = args.header

    # generate dark and light variants
    frames_dark = generate_frames(grid, square=SQUARE, padding=PADDING, header=header_text, header_size=args.header_size, header_offset=args.header_offset, font=font, palette=PALETTES.get('dark'))
    frames_light = generate_frames(grid, square=SQUARE, padding=PADDING, header=header_text, header_size=args.header_size, header_offset=args.header_offset, font=font, palette=PALETTES.get('light'))

    # output directory (public/) relative to repo
    out_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', 'public'))
    if not os.path.exists(out_dir):
        os.makedirs(out_dir, exist_ok=True)

    out_dark = os.path.join(out_dir, 'banner_dark.gif')
    out_light = os.path.join(out_dir, 'banner_light.gif')

    save_gif(frames_dark, out_dark)
    print(f"Saved banner to {out_dark}")
    save_gif(frames_light, out_light)
    print(f"Saved banner to {out_light}")


if __name__ == "__main__":
    main()
