import math
import wave
from pathlib import Path

import numpy as np


RATE = 44100
DURATION = 64.0
OUT = Path(__file__).resolve().parents[2] / "Assets" / "_Project" / "Audio" / "BGM"


def frequency(midi):
    return 440.0 * (2.0 ** ((midi - 69) / 12.0))


def circular_age(time, start):
    return np.mod(time - start, DURATION)


def pad_note(time, start, duration, midi, phase=0.0):
    age = circular_age(time, start)
    active = age < duration
    attack = np.clip(age / 1.8, 0.0, 1.0)
    release = np.clip((duration - age) / 2.2, 0.0, 1.0)
    envelope = np.sin(np.minimum(attack, 1.0) * math.pi * 0.5)
    envelope *= np.sin(np.minimum(release, 1.0) * math.pi * 0.5)
    envelope *= active
    f = frequency(midi)
    drift = 0.0025 * np.sin(2.0 * math.pi * 0.07 * age + phase)
    angle = 2.0 * math.pi * f * age * (1.0 + drift) + phase
    return envelope * (
        0.42 * np.sin(angle)
        + 0.36 * np.sin(angle * 2.0 + 0.4)
        + 0.22 * np.sin(angle * 3.0 + 1.1)
    )


def bell_note(time, start, midi):
    age = circular_age(time, start)
    active = age < 3.6
    envelope = np.exp(-age * 1.7) * np.clip(age / 0.045, 0.0, 1.0) * active
    f = frequency(midi)
    return envelope * (
        0.78 * np.sin(2.0 * math.pi * f * age)
        + 0.16 * np.sin(2.0 * math.pi * f * 2.01 * age + 0.3)
        + 0.06 * np.sin(2.0 * math.pi * f * 3.98 * age + 0.8)
    )


def render():
    count = int(RATE * DURATION)
    time = np.arange(count, dtype=np.float64) / RATE
    left = np.zeros(count, dtype=np.float64)
    right = np.zeros(count, dtype=np.float64)

    # Eight slow overlapping chords. The final A-minor voicing wraps into
    # the opening A-minor voicing, keeping the exported clip loop-safe.
    chords = [
        (45, 52, 57), (41, 48, 57), (43, 50, 59), (40, 47, 55),
        (45, 52, 60), (41, 48, 53), (43, 50, 57), (45, 52, 57),
    ]
    for chord_index, chord in enumerate(chords):
        start = chord_index * 8.0
        for note_index, midi in enumerate(chord):
            signal = pad_note(time, start, 10.0, midi, note_index * 0.8 + chord_index * 0.17)
            pan = (-0.24, 0.18, 0.0)[note_index]
            # The sustained chord is only a barely audible bed. The sparse
            # glass notes below are intentionally the main musical voice.
            layer_gain = (0.0005, 0.0015, 0.002)[note_index]
            left += signal * (0.5 - pan * 0.5) * layer_gain
            right += signal * (0.5 + pan * 0.5) * layer_gain

    # Sparse glass-ball motif: enough identity to feel alchemical, but with
    # long gaps so it does not compete with game effects.
    melody = [69, 72, 76, 74, 72, 69, 67, 69, 76, 74, 72, 69]
    starts = [3, 7, 13, 19, 23, 29, 35, 39, 45, 49, 55, 61]
    for index, (start, midi) in enumerate(zip(starts, melody)):
        signal = bell_note(time, float(start), midi)
        pan = -0.28 if index % 2 == 0 else 0.28
        left += signal * (0.5 - pan * 0.5) * 0.07
        right += signal * (0.5 + pan * 0.5) * 0.07

    # Quiet breathing motion with loop-length-aligned oscillators.
    breath = 0.92 + 0.08 * np.sin(2.0 * math.pi * time / 16.0)
    left *= breath
    right *= np.roll(breath, int(RATE * 0.7))

    # Remove the recurring low pad bloom (the audible "boom") while keeping
    # the glass notes and the airy upper harmonics. FFT filtering is circular,
    # so it also preserves the seamless loop boundary.
    frequencies = np.fft.rfftfreq(count, 1.0 / RATE)
    highpass = np.clip((frequencies - 130.0) / 100.0, 0.0, 1.0)
    highpass = highpass * highpass * (3.0 - 2.0 * highpass)
    left = np.fft.irfft(np.fft.rfft(left) * highpass, n=count)
    right = np.fft.irfft(np.fft.rfft(right) * highpass, n=count)

    peak = max(float(np.max(np.abs(left))), float(np.max(np.abs(right))), 1e-8)
    target_peak = 10.0 ** (-12.0 / 20.0)
    # Only protect against clipping/loud peaks; never boost a deliberately
    # quiet ambient mix back up to the target level.
    scale = min(1.0, target_peak / peak)
    stereo = np.column_stack((left * scale, right * scale))
    pcm = np.clip(stereo * 32767.0, -32768, 32767).astype("<i2")

    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / "BGM_QuietAlchemyLab_Loop.wav"
    with wave.open(str(path), "wb") as output:
        output.setnchannels(2)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(pcm.tobytes())
    print(path)


if __name__ == "__main__":
    render()
