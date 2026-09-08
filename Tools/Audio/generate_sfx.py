import math
import random
import struct
import wave
from pathlib import Path

RATE = 44100
OUT = Path(__file__).resolve().parents[2] / "Assets" / "_Project" / "Audio" / "SFX"
TAU = math.tau


def env(t, duration, attack=0.004, release=0.12):
    a = min(1.0, t / max(attack, 1e-5))
    r = min(1.0, max(0.0, duration - t) / max(release, 1e-5))
    return a * r * r


def tone(t, f0, f1=None, duration=1.0, phase=0.0):
    f1 = f0 if f1 is None else f1
    k = (f1 - f0) / max(duration, 1e-5)
    return math.sin(TAU * (f0 * t + 0.5 * k * t * t) + phase)


def lowpass(values, amount):
    out, state = [], 0.0
    for value in values:
        state += (value - state) * amount
        out.append(state)
    return out


def noise_track(count, seed, smooth=0.25):
    rng = random.Random(seed)
    return lowpass([rng.uniform(-1.0, 1.0) for _ in range(count)], smooth)


def write(name, duration, generator, gain=0.82):
    count = int(RATE * duration)
    noise = noise_track(count, hash(name) & 0xFFFFFFFF)
    samples = []
    for i in range(count):
        t = i / RATE
        value = generator(t, duration, noise[i]) * gain
        samples.append(max(-1.0, min(1.0, value)))
    peak = max(abs(v) for v in samples) or 1.0
    scale = min(1.0, 0.92 / peak)
    OUT.mkdir(parents=True, exist_ok=True)
    with wave.open(str(OUT / f"{name}.wav"), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(b"".join(struct.pack("<h", int(v * scale * 32767)) for v in samples))


def click(t, d, n):
    return env(t, d, 0.001, 0.055) * (0.55 * tone(t, 540, 400, d) + 0.16 * n)


def panel_open(t, d, n):
    return env(t, d, 0.01, 0.15) * (0.38 * tone(t, 260, 520, d) + 0.22 * tone(t, 520, 780, d) + 0.07 * n)


def panel_close(t, d, n):
    return env(t, d, 0.006, 0.12) * (0.42 * tone(t, 520, 240, d) + 0.16 * tone(t, 740, 390, d) + 0.08 * n)


def card_appear(t, d, n):
    shimmer = tone(t, 720, 1050, d) * (0.65 + 0.35 * math.sin(TAU * 9 * t))
    return env(t, d, 0.025, 0.25) * (0.28 * shimmer + 0.18 * tone(t, 360, 610, d) + 0.08 * n)


def card_select(t, d, n):
    chime = sum(0.13 * tone(t, f, f * 1.03, d) for f in (440, 660, 880))
    return env(t, d, 0.003, 0.26) * (chime + 0.08 * n)


def ball_launch(t, d, n):
    return env(t, d, 0.002, 0.11) * (0.34 * tone(t, 210, 620, d) + 0.17 * tone(t, 420, 900, d) + 0.1 * n)


def ball_hit(t, d, n):
    ceramic = 0.42 * tone(t, 730, 610, d) + 0.2 * tone(t, 1180, 880, d)
    return env(t, d, 0.001, 0.065) * (ceramic + 0.13 * n)


def wall_hit(t, d, n):
    return env(t, d, 0.001, 0.09) * (0.43 * tone(t, 250, 170, d) + 0.2 * tone(t, 410, 300, d) + 0.18 * n)


def block_break(t, d, n):
    crack = n * math.exp(-24 * t)
    body = tone(t, 310, 135, d) + 0.35 * tone(t, 680, 310, d)
    return env(t, d, 0.001, 0.18) * (0.28 * body + 0.42 * crack)


def critical(t, d, n):
    return env(t, d, 0.001, 0.16) * (0.34 * tone(t, 460, 1080, d) + 0.25 * tone(t, 920, 1380, d) + 0.2 * n)


def ball_return(t, d, n):
    return env(t, d, 0.008, 0.13) * (0.32 * tone(t, 650, 310, d) + 0.17 * tone(t, 980, 500, d) + 0.06 * n)


def player_hit(t, d, n):
    return env(t, d, 0.001, 0.2) * (0.48 * tone(t, 150, 75, d) + 0.25 * n)


def gold(t, d, n):
    ring = 0.27 * tone(t, 980, 1010, d) + 0.2 * tone(t, 1480, 1510, d)
    return env(t, d, 0.002, 0.3) * (ring + 0.05 * n)


def key(t, d, n):
    notes = 0.2 * tone(t, 620, 650, d) + 0.2 * tone(t, 930, 970, d) + 0.14 * tone(t, 1240, 1280, d)
    return env(t, d, 0.004, 0.36) * (notes + 0.06 * n)


def fire(t, d, n):
    flicker = n * (0.55 + 0.45 * math.sin(TAU * 17 * t))
    return env(t, d, 0.006, 0.22) * (0.34 * flicker + 0.2 * tone(t, 170, 85, d))


def ice(t, d, n):
    glass = 0.22 * tone(t, 1450, 900, d) + 0.17 * tone(t, 2150, 1250, d)
    return env(t, d, 0.001, 0.25) * (glass + 0.26 * n * math.exp(-10 * t))


def lightning(t, d, n):
    buzz = tone(t, 820, 1380, d) * math.sin(TAU * 42 * t)
    return env(t, d, 0.001, 0.15) * (0.27 * buzz + 0.32 * n)


def clear(t, d, n):
    notes = (0.15 * tone(t, 392, 405, d) + 0.15 * tone(t, 523, 540, d) +
             0.14 * tone(t, 659, 680, d) + 0.12 * tone(t, 784, 810, d))
    pulse = 0.7 + 0.3 * math.sin(TAU * 3.2 * t)
    return env(t, d, 0.018, 0.55) * (notes * pulse + 0.035 * n)


SOUNDS = [
    ("SFX_UI_Click", 0.11, click, 0.72),
    ("SFX_UI_PanelOpen", 0.28, panel_open, 0.72),
    ("SFX_UI_PanelClose", 0.24, panel_close, 0.72),
    ("SFX_Card_Appear", 0.48, card_appear, 0.72),
    ("SFX_Card_Select", 0.44, card_select, 0.75),
    ("SFX_Ball_Launch", 0.2, ball_launch, 0.66),
    ("SFX_Ball_Hit", 0.13, ball_hit, 0.52),
    ("SFX_Ball_WallHit", 0.16, wall_hit, 0.48),
    ("SFX_Block_Break", 0.32, block_break, 0.68),
    ("SFX_Combat_Critical", 0.28, critical, 0.72),
    ("SFX_Ball_Return", 0.24, ball_return, 0.55),
    ("SFX_Player_Hit", 0.34, player_hit, 0.75),
    ("SFX_Reward_Gold", 0.4, gold, 0.68),
    ("SFX_Reward_Key", 0.56, key, 0.7),
    ("SFX_Element_Fire", 0.45, fire, 0.58),
    ("SFX_Element_Ice", 0.38, ice, 0.56),
    ("SFX_Element_Lightning", 0.32, lightning, 0.5),
    ("SFX_Stage_Clear", 0.9, clear, 0.72),
]


if __name__ == "__main__":
    for spec in SOUNDS:
        write(*spec)
    print(f"Generated {len(SOUNDS)} sound effects in {OUT}")
