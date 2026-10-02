"""
Synthesizes every sound in Ashvale from scratch and writes them into Content/ as WAV files.

Made by AI. Only the Python standard library is needed. Run it from the repository root:

    python3 Tools/generate_audio.py

Every random choice is seeded, so running it again produces the same files.
"""

import math
import os
import random
import struct
import wave

SAMPLE_RATE = 44100
CONTENT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Content")


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def midi_to_freq(note):
    return 440.0 * 2 ** ((note - 69) / 12)


def silence(seconds):
    return [0.0] * int(seconds * SAMPLE_RATE)


def mix_into(target, source, start, gain=1.0):
    """Adds source into target starting at sample index start, growing target if needed"""
    end = start + len(source)
    if end > len(target):
        target.extend([0.0] * (end - len(target)))
    for i, s in enumerate(source):
        target[start + i] += s * gain


def low_pass(samples, amount):
    """One-pole low-pass filter. Smaller amounts cut more of the highs"""
    out = []
    y = 0.0
    for x in samples:
        y += amount * (x - y)
        out.append(y)
    return out


def normalize(samples, peak):
    loudest = max(abs(s) for s in samples) or 1.0
    return [s * peak / loudest for s in samples]


def fade_out(samples, seconds):
    n = min(len(samples), int(seconds * SAMPLE_RATE))
    for i in range(n):
        samples[len(samples) - n + i] *= 1 - i / n
    return samples


def write_wav(name, samples):
    path = os.path.join(CONTENT_DIR, name)
    with wave.open(path, "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(SAMPLE_RATE)
        f.writeframes(b"".join(
            struct.pack("<h", int(max(-1.0, min(1.0, s)) * 32767)) for s in samples))
    print(f"wrote {os.path.relpath(path)} ({len(samples) / SAMPLE_RATE:.2f}s)")


def sweep(start_freq, end_freq, seconds, wave_fn):
    """A tone that glides exponentially from one pitch to another"""
    n = int(seconds * SAMPLE_RATE)
    out = []
    phase = 0.0
    for i in range(n):
        freq = start_freq * (end_freq / start_freq) ** (i / n)
        phase += freq / SAMPLE_RATE
        out.append(wave_fn(phase))
    return out


def sine(phase):
    return math.sin(2 * math.pi * phase)


def triangle(phase):
    return 4 * abs(phase % 1 - 0.5) - 1


def envelope(samples, attack, decay):
    """A quick linear attack followed by an exponential decay with the given time constant"""
    a = max(1, int(attack * SAMPLE_RATE))
    for i in range(len(samples)):
        level = i / a if i < a else math.exp(-(i - a) / (decay * SAMPLE_RATE))
        samples[i] *= level
    return samples


def noise(seconds, rng):
    return [rng.uniform(-1, 1) for _ in range(int(seconds * SAMPLE_RATE))]


def pluck(freq, seconds, rng, brightness=0.5, decay=0.996):
    """
    A Karplus-Strong plucked string: a burst of noise fed around a delay line one
    wavelength long, averaged each pass so it mellows and dies away like a lute string
    """
    period = max(2, int(SAMPLE_RATE / freq))
    buffer = low_pass([rng.uniform(-1, 1) for _ in range(period)], brightness)
    out = []
    for i in range(int(seconds * SAMPLE_RATE)):
        j = i % period
        current = buffer[j]
        buffer[j] = decay * 0.5 * (current + buffer[(j + 1) % period])
        out.append(current)
    return out


def brass(freq, seconds):
    """A bright, buzzy horn tone built from a sawtooth's harmonics, with a little vibrato"""
    out = []
    phase = 0.0
    for i in range(int(seconds * SAMPLE_RATE)):
        t = i / SAMPLE_RATE
        vibrato = 1 + 0.004 * math.sin(2 * math.pi * 5.5 * t) * min(1, t * 3)
        phase += freq * vibrato / SAMPLE_RATE
        out.append(sum(math.sin(2 * math.pi * n * phase) / n for n in range(1, 9)))
    return out


# ---------------------------------------------------------------------------
# Sound effects
# ---------------------------------------------------------------------------

def make_coin():
    # the classic two-note pickup chime, B5 then E6
    tone = lambda phase: sine(phase) + 0.3 * sine(3 * phase)
    first = envelope(sweep(987.77, 987.77, 0.07, tone), 0.002, 0.2)
    second = envelope(sweep(1318.51, 1318.51, 0.4, tone), 0.002, 0.12)
    out = first + second
    return normalize(fade_out(out, 0.05), 0.7)


def make_jump():
    rng = random.Random(2)
    rise = envelope(sweep(180, 560, 0.18, triangle), 0.005, 0.08)
    puff = envelope(low_pass(noise(0.18, rng), 0.3), 0.002, 0.03)
    out = [r + 0.25 * p for r, p in zip(rise, puff)]
    return normalize(fade_out(out, 0.03), 0.55)


def make_hit():
    rng = random.Random(3)
    crack = envelope(noise(0.04, rng), 0.0005, 0.01)
    crunch = envelope(low_pass(noise(0.6, rng), 0.15), 0.001, 0.12)
    thump = envelope(sweep(110, 40, 0.6, sine), 0.001, 0.15)
    out = []
    mix_into(out, crunch, 0, 0.8)
    mix_into(out, thump, 0, 1.0)
    mix_into(out, crack, 0, 0.35)
    return normalize(fade_out(out, 0.1), 0.9)


def make_eruption():
    rng = random.Random(4)
    rumble = envelope(low_pass(low_pass(noise(0.8, rng), 0.04), 0.1), 0.03, 0.25)
    thump = envelope(sweep(65, 32, 0.8, sine), 0.005, 0.2)
    out = []
    mix_into(out, rumble, 0, 1.0)
    mix_into(out, thump, 0, 0.6)
    return normalize(fade_out(out, 0.15), 0.8)


def make_win():
    # a quick D major arpeggio climbing into a held chord
    out = []
    step = 0.12
    for i, note in enumerate([62, 66, 69]):
        mix_into(out, envelope(brass(midi_to_freq(note), 0.3), 0.01, 0.12), int(i * step * SAMPLE_RATE))
    chord_start = int(3 * step * SAMPLE_RATE)
    for note in [62, 66, 69, 74]:
        tone = envelope(brass(midi_to_freq(note), 1.3), 0.02, 0.6)
        mix_into(out, tone, chord_start, 0.5)
    return normalize(fade_out(out, 0.3), 0.8)


def make_click():
    # pressing a button: a wooden knock with a bright rising blip on top
    rng = random.Random(6)
    knock = envelope(low_pass(noise(0.05, rng), 0.35), 0.0005, 0.008)
    blip = envelope(sweep(660, 990, 0.12, lambda p: sine(p) + 0.25 * sine(2 * p)), 0.002, 0.04)
    out = []
    mix_into(out, knock, 0, 0.6)
    mix_into(out, blip, 0, 1.0)
    return normalize(fade_out(out, 0.02), 0.6)


# ---------------------------------------------------------------------------
# Music
# ---------------------------------------------------------------------------

TEMPO = 108
EIGHTH = 60 / TEMPO / 2

# D dorian. Each bar is eight eighth notes of (midi note, length in eighths); None is a rest
D4, E4, F4, G4, A4, B4, C5, D5, E5 = 62, 64, 65, 67, 69, 71, 72, 74, 76
C4 = 60
MELODY = [
    # first phrase, low and winding
    [(D4, 2), (F4, 1), (G4, 1), (A4, 2), (A4, 1), (G4, 1)],
    [(F4, 2), (E4, 1), (F4, 1), (D4, 4)],
    [(C4, 2), (E4, 1), (F4, 1), (G4, 2), (F4, 1), (E4, 1)],
    [(F4, 2), (E4, 1), (C4, 1), (D4, 4)],
    [(A4, 2), (C5, 1), (B4, 1), (A4, 2), (G4, 1), (F4, 1)],
    [(G4, 2), (A4, 1), (G4, 1), (F4, 2), (E4, 2)],
    [(D4, 2), (F4, 1), (E4, 1), (C4, 2), (E4, 1), (G4, 1)],
    [(F4, 2), (E4, 2), (D4, 4)],
    # second phrase, climbing higher
    [(D5, 2), (C5, 1), (A4, 1), (C5, 2), (D5, 2)],
    [(E5, 2), (D5, 1), (C5, 1), (A4, 4)],
    [(G4, 2), (A4, 1), (C5, 1), (D5, 2), (C5, 1), (A4, 1)],
    [(G4, 2), (F4, 1), (G4, 1), (A4, 4)],
    [(D5, 2), (C5, 1), (A4, 1), (G4, 2), (A4, 1), (C5, 1)],
    [(D5, 2), (E5, 1), (D5, 1), (C5, 2), (A4, 2)],
    [(G4, 2), (F4, 1), (E4, 1), (F4, 2), (G4, 1), (E4, 1)],
    [(D4, 6), (None, 2)],
]

# the root of the chord under each bar, played low on beats one and three
BASS = [38, 38, 36, 38, 41, 36, 38, 38, 38, 45, 36, 41, 38, 45, 36, 38]


def drum(rng, low):
    """A frame drum: a pitched thump with a slap of noise on top"""
    body = envelope(sweep(90 if low else 160, 50 if low else 110, 0.3, sine), 0.001, 0.08 if low else 0.04)
    slap = envelope(low_pass(noise(0.3, rng), 0.25), 0.001, 0.015)
    return [b + 0.4 * s for b, s in zip(body, slap)]


def shaker(rng):
    return envelope([s for s in low_pass(noise(0.08, rng), 0.9)], 0.01, 0.015)


def make_music():
    rng = random.Random(5)
    bar_length = int(8 * EIGHTH * SAMPLE_RATE)
    loop_length = bar_length * len(MELODY)
    out = [0.0] * loop_length

    for bar, notes in enumerate(MELODY):
        bar_start = bar * bar_length

        # melody on a plucked lute, with a softer octave below for the second phrase
        position = 0
        for note, length in notes:
            if note is not None:
                start = bar_start + int(position * EIGHTH * SAMPLE_RATE)
                ring = length * EIGHTH + 0.6
                mix_into(out, pluck(midi_to_freq(note), ring, rng, 0.5), start, 0.55)
                if bar >= 8:
                    mix_into(out, pluck(midi_to_freq(note - 12), ring, rng, 0.35), start, 0.25)
            position += length

        # bass plucks on beats one and three
        for beat in (0, 4):
            start = bar_start + int(beat * EIGHTH * SAMPLE_RATE)
            mix_into(out, pluck(midi_to_freq(BASS[bar]), 4 * EIGHTH + 0.3, rng, 0.3, 0.998), start, 0.5)

        # low drum on one and three, a lighter one before three and before the next bar
        for eighth, low in ((0, True), (3, False), (4, True), (7, False)):
            start = bar_start + int(eighth * EIGHTH * SAMPLE_RATE)
            mix_into(out, drum(rng, low), start, 0.45 if low else 0.25)

        # a quiet shaker on every offbeat keeps things moving
        for eighth in (1, 3, 5, 7):
            start = bar_start + int(eighth * EIGHTH * SAMPLE_RATE)
            mix_into(out, shaker(rng), start, 0.06)

    # a quiet open-fifth drone on D underneath everything
    drone_phase = 0.0
    for i in range(loop_length):
        drone_phase += midi_to_freq(38) / SAMPLE_RATE
        swell = 0.8 + 0.2 * math.sin(2 * math.pi * i / loop_length * 4)
        out[i] += 0.07 * swell * (sine(drone_phase) + 0.6 * sine(1.5 * drone_phase) + 0.3 * sine(2 * drone_phase))

    # anything still ringing past the end wraps around to the start, so the loop is seamless
    for i in range(loop_length, len(out)):
        out[i - loop_length] += out[i]
    out = out[:loop_length]

    return normalize(out, 0.8)


if __name__ == "__main__":
    write_wav("coin.wav", make_coin())
    write_wav("jump.wav", make_jump())
    write_wav("hit.wav", make_hit())
    write_wav("eruption.wav", make_eruption())
    write_wav("win.wav", make_win())
    write_wav("click.wav", make_click())
    write_wav("music.wav", make_music())
