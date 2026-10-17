"""Procedural tomb-frieze murals for Hollow Light.

Draws Egyptian-tomb / cave-art style wall paintings about the living shadows
as RGBA PNGs (paint only, alpha = pigment coverage, worn and chipped).

Usage:  py -3.12 -I murals.py [out_dir] [preview_dir]
Needs:  pillow, numpy
"""
import math
import os
import random
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SS = 2  # supersampling factor for drawing

# Layer stacking order (bottom -> top). 'outline' is generated from skin+cloth.
ORDER = ['ink_back', 'rays', 'outline', 'skin', 'cloth', 'flame', 'core',
         'ink_front', 'ochre', 'bone', 'eyes']
INK = (30, 26, 24)
COL = {
    'ink_back': INK, 'outline': INK, 'ink_front': INK,
    'rays': (150, 94, 42), 'skin': (114, 50, 30), 'cloth': (204, 190, 160),
    'flame': (166, 102, 42), 'core': (222, 207, 174), 'ochre': (124, 60, 32),
    'bone': (206, 192, 162), 'eyes': (182, 112, 44),
}


# ----------------------------------------------------------------- geometry
def circle(c, r, n=28):
    a = np.linspace(0, 2 * math.pi, n, endpoint=False)
    return np.stack([c[0] + r * np.cos(a), c[1] + r * np.sin(a)], 1)


def ellipse(c, rx, ry, ang=0.0, n=32):
    a = np.linspace(0, 2 * math.pi, n, endpoint=False)
    x, y = rx * np.cos(a), ry * np.sin(a)
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    return np.stack([c[0] + x * ca - y * sa, c[1] + x * sa + y * ca], 1)


def strip(pts, w0, w1=None):
    """Tapered stroke along a polyline -> polygon."""
    pts = np.asarray(pts, float)
    w1 = w0 if w1 is None else w1
    ws = np.linspace(w0, w1, len(pts))[:, None]
    d = np.gradient(pts, axis=0)
    d /= np.linalg.norm(d, axis=1, keepdims=True) + 1e-9
    nrm = np.stack([-d[:, 1], d[:, 0]], 1)
    return np.vstack([pts + nrm * ws / 2, (pts - nrm * ws / 2)[::-1]])


def limb(layer, p0, p1, w0, w1):
    p0, p1 = np.asarray(p0, float), np.asarray(p1, float)
    return [(layer, strip([p0, p1], w0, w1)), (layer, circle(p0, w0 / 2, 16)),
            (layer, circle(p1, w1 / 2, 16))]


def bezier(p0, p1, p2, n=40):
    t = np.linspace(0, 1, n)[:, None]
    p0, p1, p2 = (np.asarray(p, float) for p in (p0, p1, p2))
    return (1 - t) ** 2 * p0 + 2 * (1 - t) * t * p1 + t ** 2 * p2


def tendril(start, ang, length, curl, rng, steps=50):
    """Curling polyline (world coords, y down). ang in degrees, 0 = +x."""
    p = np.array(start, float)
    a = math.radians(ang)
    ph = rng.uniform(0, 6.28)
    out = [p.copy()]
    for i in range(steps):
        t = i / steps
        a += curl * (0.3 + t * 1.6) / steps * 1.6 + 0.03 * math.sin(ph + t * 9)
        p = p + length / steps * np.array([math.cos(a), math.sin(a)])
        out.append(p.copy())
    return np.array(out)


def dirv(a):
    """Unit vector for an angle measured from straight down, + = forward (local, y up)."""
    r = math.radians(a)
    return np.array([math.sin(r), -math.cos(r)])


# ------------------------------------------------------------------- canvas
class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.layers = {k: Image.new('L', (w * SS, h * SS), 0) for k in ORDER if k != 'outline'}
        self.draw = {k: ImageDraw.Draw(v) for k, v in self.layers.items()}

    def poly(self, layer, pts):
        pts = [(float(x) * SS, float(y) * SS) for x, y in pts]
        if len(pts) >= 3:
            self.draw[layer].polygon(pts, fill=255)

    def shapes(self, shapes):
        for layer, pts in shapes:
            self.poly(layer, pts)

    def clear_rows(self, layer, y0, y1):
        self.draw[layer].rectangle([0, y0 * SS, self.w * SS, y1 * SS], fill=0)


def transform(pts, x0, y0, h, facing=1, rot=0.0):
    pts = np.asarray(pts, float).reshape(-1, 2).copy()
    pts[:, 0] *= facing
    c, s = math.cos(math.radians(rot)), math.sin(math.radians(rot))
    xr = pts[:, 0] * c - pts[:, 1] * s
    yr = pts[:, 0] * s + pts[:, 1] * c
    return np.stack([x0 + xr * h, y0 - yr * h], 1)


def place(cv, fig, x0, y0, h, facing=1, rot=0.0):
    shapes, anchors = fig
    cv.shapes([(l, transform(p, x0, y0, h, facing, rot)) for l, p in shapes])
    return {k: (transform(v, x0, y0, h, facing, rot)[0] if np.ndim(v) else v * h)
            for k, v in anchors.items()}


def ground(shapes, anchors):
    """Shift a local figure so its lowest point sits at y = 0."""
    miny = min(p[:, 1].min() for _, p in shapes)
    off = np.array([0, miny])
    shapes = [(l, p - off) for l, p in shapes]
    anchors = {k: (v - off if np.ndim(v) else v) for k, v in anchors.items()}
    return shapes, anchors


# ------------------------------------------------------------------ figures
def flame_shapes(base, rr, rays, rng_seed=0, ray_len=4.2):
    """Teardrop flame standing on base (local coords, y up) + radiating strokes."""
    S = []
    base = np.asarray(base, float)
    c = base + np.array([0, rr])
    arc = [c + rr * np.array([math.cos(a), math.sin(a)])
           for a in np.radians(np.linspace(25, -205, 18))]
    tip = c + np.array([rr * 0.15, rr * 2.6])
    tongue = c + np.array([rr * 1.05, rr * 1.4])
    S.append(('flame', np.array([tip, tongue * 0.5 + tip * 0.5 + [rr * 0.2, 0], tongue, *arc,
                                 c + [-rr * 0.9, rr * 1.2]])))
    ci = c + np.array([0, -rr * 0.15])
    arc2 = [ci + rr * 0.5 * np.array([math.cos(a), math.sin(a)])
            for a in np.radians(np.linspace(30, -210, 14))]
    S.append(('core', np.array([ci + [0, rr * 1.35], *arc2])))
    fc = c + np.array([0, rr * 0.6])
    r = random.Random(rng_seed)
    if rays:
        for i in range(rays):
            a = math.radians(-40 + 260 * i / max(rays - 1, 1) + r.uniform(-4, 4))  # skip below
            d = np.array([math.cos(a), math.sin(a)])
            r0 = rr * 2.3
            r1 = rr * (ray_len + (0.9 if i % 2 else 0) + r.uniform(-0.3, 0.3))
            S.append(('rays', strip([fc + d * r0, fc + d * r1], rr * 0.42, rr * 0.12)))
    return S, fc


def human(legs=((17, 1), (-15, -1)), arms=((68, 138), (-14, -8)), torch=None,
          flame=1.0, rays=9, seed=0):
    """Egyptian profile figure facing +x, height ~1. Returns (shapes, anchors)."""
    S, A = [], {}
    hip = np.array([0.0, 0.50])
    for ta, sa in legs[::-1]:  # back leg first
        knee = hip + 0.25 * dirv(ta)
        ank = knee + 0.25 * dirv(sa)
        S += limb('skin', hip, knee, 0.065, 0.046)
        S += limb('skin', knee, ank, 0.046, 0.032)
        sd = dirv(sa)
        fd = np.array([-sd[1], sd[0]])
        S.append(('skin', np.array([ank - 0.03 * fd, ank + 0.02 * fd, ank + 0.095 * fd + 0.022 * sd,
                                    ank - 0.03 * fd + 0.03 * sd])))
    S.append(('skin', np.array([(-0.055, 0.53), (0.06, 0.53), (0.125, 0.80), (0.0, 0.84), (-0.115, 0.80)])))
    S.append(('cloth', np.array([(-0.075, 0.56), (0.072, 0.56), (0.15, 0.36), (0.02, 0.385), (-0.085, 0.375)])))
    S.append(('ink_front', np.array([(-0.075, 0.56), (0.072, 0.56), (0.07, 0.535), (-0.074, 0.535)])))
    S.append(('skin', np.array([(-0.025, 0.79), (0.04, 0.79), (0.035, 0.875), (-0.02, 0.875)])))
    hc = np.array([0.018, 0.915])
    S.append(('skin', circle(hc, 0.06)))
    S.append(('skin', np.array([(0.065, 0.935), (0.093, 0.905), (0.066, 0.895)])))  # nose
    S.append(('skin', np.array([(0.04, 0.87), (0.075, 0.875), (0.06, 0.855)])))  # chin
    S.append(('ink_front', np.array([(0.045, 0.972), (0.0, 0.985), (-0.055, 0.972), (-0.088, 0.93),
                                     (-0.092, 0.85), (-0.075, 0.795), (-0.03, 0.80), (-0.01, 0.86),
                                     (0.02, 0.905), (0.05, 0.93)])))  # wig
    S.append(('ink_front', ellipse((0.052, 0.922), 0.014, 0.008)))  # eye
    A['head'] = hc
    shoulders = [np.array([0.095, 0.785]), np.array([-0.085, 0.785])]
    for i in (1, 0):
        ua, fa = arms[i]
        sh = shoulders[i]
        el = sh + 0.17 * dirv(ua)
        hd = el + 0.16 * dirv(fa)
        S += limb('skin', sh, el, 0.05, 0.036)
        S += limb('skin', el, hd, 0.036, 0.03)
        S.append(('skin', circle(hd, 0.024)))
        A['hand%d' % i] = hd
    if torch is not None:
        d = dirv(180 - torch)
        hd = A['hand0']
        top = hd + 0.21 * d
        S.append(('ink_front', strip([hd - 0.05 * d, top], 0.018, 0.028)))
        S.append(('ink_front', strip([top - 0.03 * d, top + 0.012 * d], 0.045, 0.045)))
        fs, fc = flame_shapes(top + 0.01 * d, 0.045 * flame, rays, seed)
        S += fs
        A['flame'] = fc
        A['flame_r'] = 0.045 * flame
        A['torch_top'] = top
    return ground(S, A)


def shade(legs=((12, 0), (-12, -2)), tilt=12, head_tilt=0, arms=((78, 92), (55, 75)),
          fingers=4, finger_len=0.10, spread=34, layer='ink_back', front_arm_layer=None,
          eyes=True):
    """Tall thin living shadow facing +x, height ~1.05."""
    S, A = [], {}
    L = layer
    hip = np.array([0.0, 0.58])
    for ta, sa in legs[::-1]:
        knee = hip + 0.29 * dirv(ta)
        ank = knee + 0.29 * dirv(sa)
        S += limb(L, hip, knee, 0.05, 0.03)
        S += limb(L, knee, ank, 0.03, 0.016)
        sd = dirv(sa)
        fd = np.array([-sd[1], sd[0]])
        S.append((L, np.array([ank - 0.015 * fd, ank + 0.012 * fd, ank + 0.075 * fd + 0.012 * sd,
                               ank - 0.02 * fd + 0.016 * sd])))
    t = math.radians(tilt)
    up = np.array([math.sin(t), math.cos(t)])
    fw = np.array([math.cos(t), -math.sin(t)])
    chest = hip + 0.27 * up
    S.append((L, np.array([hip - 0.035 * fw, hip + 0.03 * fw, chest + 0.045 * fw + 0.02 * up,
                           chest - 0.065 * fw + 0.01 * up, chest - 0.06 * fw - 0.08 * up])))
    th = math.radians(tilt + head_tilt)
    hup = np.array([math.sin(th), math.cos(th)])
    hfw = np.array([math.cos(th), -math.sin(th)])
    neck_top = chest + 0.07 * hup
    S += limb(L, chest, neck_top, 0.04, 0.03)
    hc = neck_top + 0.055 * hup + 0.012 * hfw
    S.append((L, ellipse(hc, 0.045, 0.072, -(tilt + head_tilt), 30)))
    S.append((L, np.array([hc + 0.03 * hfw - 0.03 * hup, hc + 0.07 * hfw - 0.045 * hup,
                           hc + 0.02 * hfw - 0.06 * hup])))  # jaw jut
    if eyes:
        for k in (0.0, 0.027):
            S.append(('eyes', circle(hc + (0.008 + k) * hfw + 0.012 * hup, 0.0105, 14)))
    A['head'] = hc
    A['mouth'] = hc + 0.06 * hfw - 0.04 * hup
    sh = chest + 0.005 * up
    for i in (1, 0):
        ua, fa = arms[i]
        al = (front_arm_layer or L) if i == 0 else L
        el = sh + 0.23 * dirv(ua)
        hd = el + 0.22 * dirv(fa)
        S += limb(al, sh, el, 0.032, 0.022)
        S += limb(al, el, hd, 0.022, 0.018)
        S.append((al, circle(hd, 0.019)))
        fdir = math.degrees(math.atan2(*(dirv(fa)[::-1] * [1, 1])))
        for k in range(fingers):
            a = fdir + (k - (fingers - 1) / 2) * spread / max(fingers - 1, 1)
            p0 = hd
            p1 = p0 + finger_len * 0.55 * np.array([math.cos(math.radians(a)), math.sin(math.radians(a))])
            a2 = a - 18
            p2 = p1 + finger_len * 0.5 * np.array([math.cos(math.radians(a2)), math.sin(math.radians(a2))])
            S.append((al, strip(bezier(p0, p1, p2, 10), 0.011, 0.003)))
        A['hand%d' % i] = hd
    return ground(S, A)


# ------------------------------------------------------------------- glyphs
def glyph(cv, kind, x, y, w, h, rng):
    cx, cy = x + w / 2, y + h / 2
    s = min(w, h)
    lw = max(3.0, s * 0.11)
    if kind == 'eye':
        top = bezier((x, cy), (cx, cy - h * 0.55), (x + w, cy), 16)
        bot = bezier((x + w, cy), (cx, cy + h * 0.55), (x, cy), 16)
        cv.poly('ink_front', strip(np.vstack([top, bot[1:]]), lw))
        cv.poly('ochre', circle((cx, cy), s * 0.16))
    elif kind == 'disk':
        cv.poly('ink_front', circle((cx, cy), s * 0.38))
        cv.poly('bone', circle((cx, cy), s * 0.1))
    elif kind == 'bars':
        n = rng.choice([2, 3])
        for i in range(n):
            bx = x + w * (i + 0.5) / n
            cv.poly('ink_front', strip([(bx, y + h * 0.1), (bx, y + h * 0.9)], lw))
    elif kind == 'zig':
        for row in (0.32, 0.68):
            pts = [(x + w * i / 6, y + h * row + (h * 0.12 if i % 2 else -h * 0.12)) for i in range(7)]
            cv.poly('ink_front', strip(pts, lw * 0.8))
    elif kind == 'tri':
        cv.poly('ink_front', [(cx, y + h * 0.08), (x + w * 0.92, y + h * 0.92), (x + w * 0.08, y + h * 0.92)])
        cv.poly('ochre', [(cx, y + h * 0.5), (cx + w * 0.16, y + h * 0.8), (cx - w * 0.16, y + h * 0.8)])
    elif kind == 'crescent':
        r = s * 0.42
        outer = [(cx + r * math.cos(a), cy + r * math.sin(a)) for a in np.radians(np.linspace(50, 310, 20))]
        inner = [(cx + r * 0.45 + r * 0.8 * math.cos(a), cy + r * 0.8 * math.sin(a))
                 for a in np.radians(np.linspace(300, 60, 20))]
        cv.poly('ochre', outer + inner)
    elif kind == 'torch':
        cv.poly('ink_front', strip([(cx, y + h * 0.95), (cx, y + h * 0.45)], lw, lw * 1.3))
        cv.poly('ochre', np.vstack([[(cx, y + h * 0.02)], circle((cx, y + h * 0.33), s * 0.15)[[1, 0, 27, 26, 25, 22, 19, 16, 13, 12]]]))
    elif kind == 'shade':
        cv.poly('ink_front', ellipse((cx, y + h * 0.16), s * 0.1, s * 0.15))
        cv.poly('ink_front', strip([(cx, y + h * 0.28), (cx, y + h * 0.98)], lw * 1.2, lw * 0.6))
        cv.poly('ink_front', strip([(x + w * 0.1, y + h * 0.18), (cx, y + h * 0.42), (x + w * 0.9, y + h * 0.18)], lw * 0.7))
        cv.poly('eyes', circle((cx + s * 0.03, y + h * 0.14), s * 0.04, 10))
    elif kind == 'spiral':
        a = np.linspace(0, 4.2 * math.pi, 60)
        r = s * 0.06 + a / a[-1] * s * 0.36
        cv.poly('ink_front', strip(np.stack([cx + r * np.cos(a), cy + r * np.sin(a)], 1), lw * 0.4, lw))
    elif kind == 'comb':
        cv.poly('ink_front', strip([(x, y + h * 0.3), (x + w, y + h * 0.3)], lw))
        for i in range(4):
            bx = x + w * (i + 0.5) / 4
            cv.poly('ink_front', strip([(bx, y + h * 0.3), (bx, y + h * 0.85)], lw * 0.7, lw * 0.4))
    elif kind == 'steps':
        pts = [(x, y + h * 0.95)]
        for i in range(3):
            pts += [(x + w * (0.12 * (i + 1)), y + h * (0.95 - 0.28 * i)),
                    (x + w * (0.12 * (i + 1)), y + h * (0.95 - 0.28 * (i + 1)))]
        pts += [(x + w - p[0] + x, p[1]) for p in pts[::-1]]
        cv.poly('ink_front', pts)
    elif kind == 'sun':
        cv.poly('ochre', circle((cx, cy), s * 0.22))
        for i in range(8):
            a = math.radians(i * 45)
            d = np.array([math.cos(a), math.sin(a)])
            cv.poly('ink_front', strip([(cx, cy) + d * s * 0.3, (cx, cy) + d * s * 0.47], lw * 0.7, lw * 0.3))


GLYPHS = ['eye', 'disk', 'bars', 'zig', 'tri', 'crescent', 'torch', 'shade', 'spiral', 'comb', 'steps', 'sun']


def glyph_band(cv, x0, x1, y0, y1, rng):
    cv.poly('ink_front', [(x0, y0), (x1, y0), (x1, y0 + 6), (x0, y0 + 6)])
    cv.poly('ink_front', [(x0, y1 - 6), (x1, y1 - 6), (x1, y1), (x0, y1)])
    cv.poly('ochre', [(x0, y0 + 11), (x1, y0 + 11), (x1, y0 + 14), (x0, y0 + 14)])
    cv.poly('ochre', [(x0, y1 - 14), (x1, y1 - 14), (x1, y1 - 11), (x0, y1 - 11)])
    a, b = y0 + 22, y1 - 22
    gh = b - a
    x = x0 + 14
    last = None
    while x < x1 - gh * 0.8:
        if rng.random() < 0.09:
            cv.poly('ink_front', strip([(x + 6, a - 2), (x + 6, b + 2)], 4))
            x += 18
            continue
        kind = rng.choice([g for g in GLYPHS if g != last])
        last = kind
        w = gh * (1.3 if kind in ('zig', 'comb', 'eye') else 0.9)
        if x + w > x1 - 10:
            break
        glyph(cv, kind, x, a, w, gh, rng)
        x += w + gh * 0.3


# ---------------------------------------------------------------- finishing
def fbm(w, h, scales, rng, falloff=0.55):
    acc = np.zeros((h, w), np.float32)
    amp = 1.0
    for s in scales:
        gw, gh = int(w / s) + 3, int(h / s) + 3
        g = rng.random((gh, gw)).astype(np.float32)
        im = Image.fromarray(g, 'F').resize((int(gw * s) + 1, int(gh * s) + 1), Image.BICUBIC)
        acc += np.asarray(im)[:h, :w] * amp
        amp *= falloff
    return (acc - acc.mean()) / (acc.std() + 1e-6)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def blur(m, r):
    im = Image.fromarray(np.clip(m * 255, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(r))
    return np.asarray(im, np.float32) / 255


def dilate(m, r):
    out = m.copy()
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx * dx + dy * dy <= r * r + 0.5:
                out = np.maximum(out, np.roll(np.roll(m, dy, 0), dx, 1))
    return out


def cracks(w, h, rng, n):
    im = Image.new('L', (w, h), 0)
    d = ImageDraw.Draw(im)
    r = random.Random(int(rng.integers(1 << 30)))
    for _ in range(n):
        x, y = r.uniform(0, w), r.uniform(0, h)
        a = r.uniform(0, 6.28)
        pts = [(x, y)]
        for _ in range(r.randint(25, 60)):
            a += r.uniform(-0.6, 0.6)
            x += math.cos(a) * 9
            y += math.sin(a) * 9
            pts.append((x, y))
            if r.random() < 0.04:  # branch
                b = a + r.choice([-1, 1]) * r.uniform(0.6, 1.2)
                bx, by = x, y
                bp = [(bx, by)]
                for _ in range(r.randint(5, 15)):
                    b += r.uniform(-0.5, 0.5)
                    bx += math.cos(b) * 8
                    by += math.sin(b) * 8
                    bp.append((bx, by))
                d.line(bp, fill=200, width=1)
        d.line(pts, fill=255, width=2)
    return np.asarray(im.filter(ImageFilter.GaussianBlur(0.6)), np.float32) / 255


def finish(cv, path, seed, preview_dir=None):
    w, h = cv.w, cv.h
    rng = np.random.default_rng(seed)
    M = {k: np.asarray(img.resize((w, h), Image.LANCZOS), np.float32) / 255
         for k, img in cv.layers.items()}
    edge_n = fbm(w, h, [5, 2.5, 1.2], rng)
    for k, m in M.items():
        if m.max() <= 0:
            continue
        mb = blur(m, 0.9)
        M[k] = np.clip((mb - 0.5) * 3.2 + 0.5 + edge_n * 0.22, 0, 1) * (mb > 0.03)
    M['outline'] = dilate(np.maximum(M['skin'], M['cloth']), 3)

    pig = fbm(w, h, [60, 25, 9, 3], rng)
    tint = (1.0 + 0.07 * np.clip(pig, -2.5, 2.5))[..., None]
    C = np.zeros((h, w, 3), np.float32)
    A = np.zeros((h, w), np.float32)
    for k in ORDER:
        a = M[k][..., None]
        C = C * (1 - a) + np.array(COL[k], np.float32) * tint * a
        A = A + M[k] * (1 - A)

    fade = np.clip(0.86 + 0.09 * fbm(w, h, [420, 200], rng), 0.6, 1.0)
    chips = smooth(-2.05, -1.7, fbm(w, h, [70, 34, 16, 7], rng))
    speck = smooth(-2.6, -2.2, fbm(w, h, [2.2, 1.1], rng))
    grain = np.clip(0.9 + 0.07 * fbm(w, h, [14, 5], rng), 0.7, 1.0)
    crk = 1 - 0.9 * cracks(w, h, rng, 6 if h < 900 else 8)
    U = A.copy()  # coverage before wear
    A = A * fade * chips * speck * grain * crk

    rgb = C / np.maximum(U, 1e-4)[..., None]
    rgb = np.where((U > 0.004)[..., None], rgb, np.array(INK, np.float32))
    out = np.dstack([np.clip(rgb, 0, 255), np.clip(A * 255, 0, 255)]).astype(np.uint8)
    img = Image.fromarray(out, 'RGBA')
    img.save(path, optimize=True)

    if preview_dir:
        os.makedirs(preview_dir, exist_ok=True)
        stone = np.ones((h, w, 3), np.float32) * np.array([112, 106, 98], np.float32)
        stone *= (1 + 0.06 * np.clip(fbm(w, h, [80, 20, 4], rng), -3, 3))[..., None]
        a = A[..., None]
        comp = stone * (1 - a) + np.clip(rgb, 0, 255) * a
        pv = Image.fromarray(np.clip(comp, 0, 255).astype(np.uint8)).resize((w // 2, h // 2), Image.LANCZOS)
        pv.save(os.path.join(preview_dir, os.path.basename(path).replace('.png', '_preview.png')))
    return img


# ------------------------------------------------------------------- panels
def frame(cv, rng, G, top=(14, 104), bottom=(664, 754)):
    W = cv.w
    if top:
        glyph_band(cv, 24, W - 24, top[0], top[1], rng)
    if bottom:
        glyph_band(cv, 24, W - 24, bottom[0], bottom[1], rng)
    if G:
        cv.poly('ink_front', [(24, G + 4), (W - 24, G + 4), (W - 24, G + 10), (24, G + 10)])


def mural_01():
    cv = Canvas(2048, 768)
    rng = random.Random(101)
    G = 630
    frame(cv, rng, G)
    for i, x in enumerate([190, 520, 850, 1180, 1510, 1840]):
        over = i % 3 == 1
        arms = ((100, 128), (-14, -8)) if over else ((66, 136), (-12, -6))
        fig = human(legs=((17, 1), (-15, -1)), arms=arms, torch=18 if over else 24,
                    flame=1.05, rays=11, seed=i)
        place(cv, fig, x, G, 400)
    return cv


def mural_02():
    cv = Canvas(2048, 768)
    rng = random.Random(202)
    G = 630
    frame(cv, rng, G)
    for i, x in enumerate([400, 880, 1360, 1840]):
        fig = human(legs=((17, 1), (-15, -1)), arms=((66, 136), (-12, -6)), torch=24,
                    flame=0.9, rays=7, seed=10 + i)
        place(cv, fig, x, G, 370)
        sx = x - 285
        sh = shade(legs=((10, 0), (-12, -2)), tilt=16 + (i % 2) * 6, head_tilt=8,
                   arms=((72 + i * 3, 84), (52, 70)), fingers=4, finger_len=0.11)
        place(cv, sh, sx, G, 495)
        # shadow still joined to the walker's heel along the ground
        cv.poly('ink_back', [(sx - 30, G + 2), (x - 30, G + 1), (x - 25, G + 8), (sx - 40, G + 9)])
    return cv


def mural_03():
    cv = Canvas(2048, 768)
    rng = random.Random(303)
    G = 630
    frame(cv, rng, G)
    cx = 1024
    hero = human(legs=((14, 0), (-12, 0)), arms=((168, 178), (-14, -8)), torch=0, flame=1.5,
                 rays=0, seed=3)
    an = place(cv, hero, cx - 40, G, 340)
    fx, fy = an['flame']
    R = 455
    r = random.Random(7)
    for i in range(72):
        a = math.radians(i * 5 + r.uniform(-1, 1))
        d = np.array([math.cos(a), math.sin(a)])
        r0 = 55 + (i % 2) * 15
        r1 = R - (25 if i % 3 else 8) - r.uniform(0, 30)
        cv.poly('rays', strip([(fx, fy) + d * r0, (fx, fy) + d * r1], 9 if i % 2 else 6, 2.5))
    for i in range(90):  # ring of light the shadows cannot cross
        a0 = math.radians(i * 4)
        a1 = a0 + math.radians(2.8)
        arc = [(fx + R * math.cos(a), fy + R * math.sin(a)) for a in np.linspace(a0, a1, 5)]
        cv.poly('bone', strip(arc, 7))
    cv.clear_rows('rays', 0, 112)
    cv.clear_rows('bone', 0, 112)
    cv.clear_rows('rays', G + 2, 768)
    cv.clear_rows('bone', G + 2, 768)

    def ring_x(y, side):
        dy = y - fy
        return fx + side * math.sqrt(max(R * R - dy * dy, 0))

    poses = [  # (height, tilt, head_tilt, arms, legs, reach to ring?)
        dict(H=330, tilt=38, ht=10, arms=((95, 100), (75, 85)), legs=((55, 0), (-35, -70)), x=None),
        dict(H=440, tilt=14, ht=5, arms=((90, 98), (100, 108)), legs=((14, 0), (-18, -4)), x=None),
        dict(H=360, tilt=24, ht=0, arms=((70, 82), (40, 60)), legs=((20, 2), (-20, -6)), x=70),
    ]
    for side in (-1, 1):
        for k, p in enumerate(poses):
            sh = shade(legs=p['legs'], tilt=p['tilt'], head_tilt=p['ht'], arms=p['arms'],
                       fingers=5, finger_len=0.12, spread=60)
            facing = -side
            a0 = place(Canvas(8, 8), sh, 0, G, p['H'], facing)
            if p['x'] is None:
                hx, hy = a0['hand0']
                target = ring_x(hy, side) + side * 22
                x0 = target - hx
            else:
                x0 = p['x'] if side < 0 else 2048 - p['x']
            an = place(cv, sh, x0, G, p['H'], facing)
            if p['x'] is None:  # little "stopped" ticks at the fingertips
                hx, hy = an['hand0']
                for j in (-1, 0, 1):
                    ty = hy + j * 22
                    tx = ring_x(ty, side) + side * 4
                    cv.poly('bone', strip([(tx - side * 2, ty), (tx + side * 20, ty + j * 6)], 4, 1.5))
    return cv


def mural_04():
    cv = Canvas(2048, 768)
    rng = random.Random(404)
    G = 630
    frame(cv, rng, G)
    r = np.random.default_rng(4)
    # kneeling bearer, torch thrust out toward the shadow
    kn = human(legs=((92, 2), (-4, -92)), arms=((74, 100), (28, 74)), torch=58, flame=0.6,
               rays=0, seed=4)
    ka = place(cv, kn, 860, G, 400)
    # the shadow, hunched over and wrapped around him
    sh = shade(legs=((22, 4), (-14, -2)), tilt=52, head_tilt=28, arms=((25, 50), (62, 96)),
               fingers=5, finger_len=0.13, spread=70, layer='ink_back', front_arm_layer='ink_front')
    sa = place(cv, sh, 1300, G, 610, facing=-1)
    # flame pulled thin into the shadow's mouth
    f0 = np.array(ka['flame'])
    m = np.array(sa['mouth'])
    n = 60
    t = np.linspace(0, 1, n)[:, None]
    line = f0 * (1 - t) + m * t
    d = (m - f0) / np.linalg.norm(m - f0)
    nrm = np.array([-d[1], d[0]])
    wav = line + nrm * (np.sin(t * 2.4 * 2 * math.pi) * 14 * (1 - t * 0.6))
    cv.poly('flame', strip(wav, 26, 4))
    cv.poly('core', strip(wav[:int(n * 0.8)], 9, 1.5))
    # tendrils of the shadow wrapping the kneeler (front ones cross his body)
    hip = np.array([1235.0, G - 250])
    for (ex, ey, rx, ry, w0) in ((875, G - 215, 150, 42, 30), (890, G - 70, 165, 38, 26)):
        # loop: from the shadow's body, behind the kneeler (upper arc), then across his front
        back = [(ex + rx * math.cos(a), ey - ry * math.sin(a)) for a in np.linspace(0.15, math.pi, 30)]
        lead = bezier(hip, ((hip[0] + back[0][0]) / 2, min(hip[1], back[0][1]) - 30), back[0], 16)
        cv.poly('ink_back', strip(np.vstack([lead, back[1:]]), w0, w0 * 0.6))
        front = [(ex + rx * math.cos(a), ey - ry * math.sin(a)) for a in np.linspace(math.pi, 2 * math.pi - 0.5, 24)]
        tip = np.array(front[-1])
        curl = [tip + 22 * np.array([math.cos(a), math.sin(a)]) + (0, -22) for a in np.linspace(math.pi / 2, -math.pi, 10)]
        cv.poly('ink_front', strip(np.vstack([front, curl[1:]]), w0 * 0.6, 3))
        hip = hip + (0, 110)
    # left: an earlier victim, drained, lying with a dead torch and smoke
    vic = human(legs=((6, 0), (-6, 0)), arms=((12, 6), (-10, -4)), torch=None)
    place(cv, vic, 360, G - 36, 300, facing=1, rot=90)
    cv.poly('ink_front', strip([(470, G - 4), (560, G - 26)], 7, 9))
    for k in range(3):
        pts = tendril((548 + k * 14, G - 30), -92 + k * 6, 110 + k * 25, (-1) ** k * 0.35, r)
        cv.poly('bone', strip(pts, 6, 1.5))
    # right: another shadow crawling in
    cr = shade(legs=((75, -5), (40, -60)), tilt=72, head_tilt=-40, arms=((15, 35), (-5, 20)),
               fingers=4, finger_len=0.12, spread=50)
    place(cv, cr, 1800, G, 330, facing=-1)
    return cv


def mural_05():
    W, H = 2048, 1024
    cv = Canvas(W, H)
    rng = random.Random(505)
    r = np.random.default_rng(5)
    frame(cv, rng, None, top=(14, 96), bottom=None)
    sc = np.array([1024.0, 360.0])
    R = 190
    a = np.linspace(0, 2 * math.pi, 140, endpoint=False)
    rad = R * (1 + 0.035 * np.sin(a * 7 + 1) + 0.02 * r.standard_normal(140))
    cv.poly('ink_back', np.stack([sc[0] + rad * np.cos(a), sc[1] + rad * np.sin(a)], 1))
    for i in range(16):  # tendrils
        ang = -170 + i * (340 / 15) + r.uniform(-6, 6)
        if -140 < ang < -40:
            ln = r.uniform(140, 230)
        else:
            ln = r.uniform(260, 520)
        st = sc + (R - 12) * np.array([math.cos(math.radians(ang)), math.sin(math.radians(ang))])
        pts = tendril(st, ang, ln, r.choice([-1, 1]) * r.uniform(0.8, 2.0), r)
        pts = pts[pts[:, 1] > 100] if (pts[:, 1] > 100).sum() > 3 else pts
        cv.poly('ink_back', strip(pts, r.uniform(24, 36), 2))
    for i in range(60):  # broken pale ring around the void
        a0 = math.radians(i * 6 + r.uniform(0, 1.5))
        a1 = a0 + math.radians(r.uniform(2.5, 4.5))
        arc = [sc + (R + 22) * np.array([math.cos(t), math.sin(t)]) for t in np.linspace(a0, a1, 4)]
        cv.poly('bone', strip(arc, 6))
    for k in (-1, 1):  # eyes in the void
        cv.poly('eyes', ellipse(sc + (k * 46, 10), 13, 8, 0, 18))
    # houses
    G = 728
    x = 90
    i = 0
    while x < W - 150:
        hw = r.uniform(110, 160)
        hh = r.uniform(90, 150)
        cxh = x + hw / 2
        lift = max(0.0, 1 - abs(cxh - 1024) / 330)
        rot = lift * r.uniform(-14, 14)
        dy = lift * 60
        body = np.array([(x, G), (x + hw, G), (x + hw, G - hh), (x, G - hh)])
        door = np.array([(cxh - 13, G), (cxh + 13, G), (cxh + 13, G - 46), (cxh - 13, G - 46)])
        roof = np.array([(x - 8, G - hh), (x + hw + 8, G - hh), (x + hw + 8, G - hh - 12), (x - 8, G - hh - 12)])
        wins = [np.array([(wx, G - hh + 24), (wx + 14, G - hh + 24), (wx + 14, G - hh + 40), (wx, G - hh + 40)])
                for wx in (x + 16, x + hw - 30)]
        c = np.array([cxh, G - hh / 2])
        rr = math.radians(rot)
        Rm = np.array([[math.cos(rr), -math.sin(rr)], [math.sin(rr), math.cos(rr)]])
        f = lambda p: (p - c) @ Rm.T + c - (0, dy)
        cv.poly('skin', f(body))
        cv.poly('cloth', f(roof))
        cv.poly('ink_front', f(door))
        for wv in wins:
            cv.poly('ink_front', f(wv))
        x += hw + r.uniform(14, 34)
        i += 1
    for k in range(10):  # fragments torn upward
        c = np.array([r.uniform(760, 1290), r.uniform(470, 600)])
        s = r.uniform(10, 22)
        cv.poly('skin', ellipse(c, s, s * 0.6, r.uniform(0, 180), 4))
    # figures falling up into the void
    spots = [(690, 520, 30), (830, 445, -25), (955, 560, 160), (1110, 470, 200),
             (1230, 540, -150), (1370, 470, 20), (1500, 535, -35), (560, 470, 40)]
    for k, (fx, fy, rot) in enumerate(spots):
        fig = human(legs=((30, 15), (-25, -20)), arms=((150 + k * 3, 170), (130, 160)), torch=None)
        sh, an = fig
        cpt = np.array([0.0, 0.5])
        sh = [(l, p - cpt) for l, p in sh]
        place(cv, (sh, {}), fx, fy, 105, facing=(-1) ** k, rot=rot)
    cv.poly('ink_front', [(24, G + 4), (W - 24, G + 4), (W - 24, G + 10), (24, G + 10)])
    # lower register: the tomb door
    cv.poly('ink_front', [(24, 760), (W - 24, 760), (W - 24, 768), (24, 768)])
    cv.poly('ochre', [(24, 776), (W - 24, 776), (W - 24, 780), (24, 780)])
    B = 998
    cv.poly('ink_back', [(930, 840), (1118, 840), (1128, B), (920, B)])  # dark opening
    cv.poly('skin', [(838, 840), (930, 840), (920, B), (822, B)])
    cv.poly('skin', [(1118, 840), (1210, 840), (1226, B), (1128, B)])
    cv.poly('cloth', [(812, 800), (1236, 800), (1236, 840), (812, 840)])
    cv.poly('ink_front', [(812, 812), (1236, 812), (1236, 818), (812, 818)])
    for side, xs in ((-1, (845, 922)), (1, (1126, 1203))):
        for j, kind in enumerate(['shade', 'eye', 'torch']):
            glyph(cv, kind, xs[0] + 18, 856 + j * 46, xs[1] - xs[0] - 36, 38, rng)
    cv.poly('ink_front', [(780, B), (1268, B), (1268, B + 8), (780, B + 8)])
    tiny = human(legs=((17, 1), (-15, -1)), arms=((66, 136), (-12, -6)), torch=24, flame=1.1,
                 rays=7, seed=55)
    place(cv, tiny, 975, B, 118)
    glyph_band(cv, 40, 790, 840, 940, rng)
    glyph_band(cv, 1258, W - 40, 840, 940, rng)
    return cv


PANELS = [('Mural_01_Bearers.png', mural_01), ('Mural_02_Following.png', mural_02),
          ('Mural_03_Holding.png', mural_03), ('Mural_04_Feeding.png', mural_04),
          ('Mural_05_Swallowing.png', mural_05)]


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
        here, '..', '..', 'Assets', '_Project', 'Art', 'Textures', 'Murals')
    pv = sys.argv[2] if len(sys.argv) > 2 else None
    only = sys.argv[3:] if len(sys.argv) > 3 else None
    os.makedirs(out, exist_ok=True)
    for i, (name, fn) in enumerate(PANELS):
        if only and str(i + 1) not in only:
            continue
        cv = fn()
        finish(cv, os.path.join(out, name), 1000 + i, pv)
        print('wrote', name)


if __name__ == '__main__':
    main()
