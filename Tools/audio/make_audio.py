"""
WICKFALL audio, synthesized from nothing (prompt §11; no samples, no licences: CC0 by construction).

Every SoundId gets a clip (named after it, "_1", "_2" for variations), plus the flame's crackle loop
and the heartbeat (AudioReactive), and every MusicId a seamless loop. Mono WAV, 16-bit; effects at
44.1 kHz, music at 22.05 kHz. Unity imports them (Vorbis for music).

    python Tools/audio/make_audio.py [out_dir]      # default: Assets/Audio
"""
from __future__ import annotations

import math
import os
import sys
import wave

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
SR = 44100
MSR = 22050
rng = np.random.default_rng(7)


# ---- building blocks -------------------------------------------------------------------------------------

def t_axis(seconds, sr=SR):
    return np.arange(int(seconds * sr)) / sr


def env(n, attack=0.005, release=0.1, sr=SR, curve=3.0):
    """Attack then exponential-ish decay to zero over the whole length."""
    e = np.ones(n)
    a = max(1, int(attack * sr))
    e[:a] = np.linspace(0, 1, a)
    tail = np.linspace(0, 1, n - a)
    e[a:] = (1 - tail) ** curve
    r = max(1, int(release * sr))
    if r < n:
        e[-r:] *= np.linspace(1, 0, r)
    return e


def sine(freq, seconds, sr=SR, phase=0.0):
    t = t_axis(seconds, sr)
    if np.ndim(freq):
        ph = 2 * np.pi * np.cumsum(freq) / sr
        return np.sin(ph + phase)
    return np.sin(2 * np.pi * freq * t + phase)


def tri(freq, seconds, sr=SR):
    t = t_axis(seconds, sr)
    return 2 * np.abs(2 * ((t * freq) % 1) - 1) - 1


def noise(seconds, sr=SR):
    return rng.uniform(-1, 1, int(seconds * sr))


def lowpass(x, cutoff, sr=SR):
    """One-pole low-pass (cutoff may be an array)."""
    cutoff = np.broadcast_to(np.asarray(cutoff, dtype=float), x.shape)
    a = np.exp(-2 * np.pi * cutoff / sr)
    y = np.empty_like(x)
    prev = 0.0
    for i in range(len(x)):
        prev = (1 - a[i]) * x[i] + a[i] * prev
        y[i] = prev
    return y


def highpass(x, cutoff, sr=SR):
    return x - lowpass(x, cutoff, sr)


def bandpass(x, lo, hi, sr=SR):
    return lowpass(highpass(x, lo, sr), hi, sr)


def sweep(f0, f1, seconds, sr=SR, curve=1.0):
    n = int(seconds * sr)
    return f0 + (f1 - f0) * np.linspace(0, 1, n) ** curve


def bell(freq, seconds, partials=((1, 1.0), (2.76, 0.5), (5.4, 0.25), (8.93, 0.12)), sr=SR, decay=3.0):
    out = np.zeros(int(seconds * sr))
    for ratio, amp in partials:
        out += amp * sine(freq * ratio, seconds, sr) * env(len(out), 0.002, 0.05, sr, decay * (1 + ratio * 0.3))
    return out


def pluck(freq, seconds, sr=SR, brightness=0.6):
    """Karplus-Strong string."""
    n = int(seconds * sr)
    period = max(2, int(sr / freq))
    buf = rng.uniform(-1, 1, period)
    out = np.empty(n)
    for i in range(n):
        out[i] = buf[i % period]
        buf[i % period] = brightness * 0.5 * (buf[i % period] + buf[(i + 1) % period]) + (1 - brightness) * buf[i % period] * 0.996
    return out


def reverb(x, sr=SR, mix=0.3, size=1.0):
    """A small Schroeder reverb: four combs and two all-passes."""
    out = np.zeros(len(x) + int(sr * 1.5 * size))
    xs = np.concatenate([x, np.zeros(len(out) - len(x))])
    for d, g in ((0.0297, 0.77), (0.0371, 0.75), (0.0411, 0.73), (0.0437, 0.71)):
        delay = int(d * size * sr)
        y = np.copy(xs)
        for i in range(delay, len(y)):
            y[i] += g * y[i - delay]
        out += y * 0.25
    for d, g in ((0.005, 0.7), (0.0017, 0.7)):
        delay = max(1, int(d * sr))
        y = np.copy(out)
        z = np.zeros_like(y)
        for i in range(len(y)):
            prev = z[i - delay] if i >= delay else 0.0
            xin = y[i - delay] if i >= delay else 0.0
            z[i] = -g * y[i] + xin + g * prev
        out = z
    wet = out / (np.max(np.abs(out)) + 1e-9)
    dry = np.concatenate([x, np.zeros(len(out) - len(x))])
    return dry * (1 - mix) + wet * mix * np.max(np.abs(x))


def fade(x, ms_in=2, ms_out=20, sr=SR):
    x = np.copy(x)
    a, b = int(ms_in * sr / 1000), int(ms_out * sr / 1000)
    if a > 0:
        x[:a] *= np.linspace(0, 1, a)
    if b > 0:
        x[-b:] *= np.linspace(1, 0, b)
    return x


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def at(x, seconds, total, sr=SR):
    out = np.zeros(int(total * sr))
    i = int(seconds * sr)
    out[i:i + len(x)] += x[:max(0, len(out) - i)]
    return out


def norm(x, peak=0.85):
    m = np.max(np.abs(x)) + 1e-9
    return x / m * peak


def write(path, x, sr=SR):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    data = np.clip(x, -1, 1)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes((data * 32767).astype(np.int16).tobytes())


# ---- sound effects -----------------------------------------------------------------------------------------

def whoosh(seconds, f0, f1, amp=1.0):
    n = noise(seconds)
    f = sweep(f0, f1, seconds)
    return amp * bandpass(n, f * 0.5, f) * env(len(n), 0.02, 0.05, curve=1.5)


def thud(freq=70, seconds=0.25, click=0.3):
    f = sweep(freq * 1.8, freq, seconds, curve=0.3)
    body = sine(f, seconds) * env(int(seconds * SR), 0.002, 0.05, curve=4)
    tick = lowpass(noise(0.02), 3000) * click
    return mix(body, tick)


def sizzle(seconds, amp=0.4):
    n = highpass(noise(seconds), 2500)
    crack = (rng.random(len(n)) < 0.003) * rng.uniform(-1, 1, len(n))
    return amp * (n * 0.3 + crack) * env(len(n), 0.01, 0.05, curve=1.2)


def tone(freq, seconds, kind="sine", attack=0.005, curve=3):
    w = sine(freq, seconds) if kind == "sine" else tri(freq, seconds)
    return w * env(len(w), attack, 0.02, curve=curve)


def crackle_loop(seconds=3.0):
    n = int(seconds * SR)
    base = lowpass(noise(seconds), 400) * 0.25
    pops = np.zeros(n)
    for _ in range(int(seconds * 22)):
        i = rng.integers(0, n - 2000)
        L = rng.integers(200, 1500)
        pops[i:i + L] += rng.uniform(0.3, 1.0) * highpass(rng.uniform(-1, 1, L), 1500) * env(L, 0.0005, 0.002, curve=6)
    x = base + pops
    # seamless: crossfade the tail into the head
    k = int(0.2 * SR)
    x[:k] = x[:k] * np.linspace(0, 1, k) + x[-k:] * np.linspace(1, 0, k)
    return norm(x[:-k], 0.6)


def heartbeat():
    lub = thud(55, 0.18, 0.05)
    dub = thud(48, 0.16, 0.03) * 0.7
    return norm(mix(lub, at(dub, 0.22, 0.45)), 0.9)


def sfx():
    S = {}
    S["Footstep"] = [norm(lowpass(noise(0.07), 600 + 150 * i) * env(int(0.07 * SR), 0.002, 0.02, curve=4), 0.6) for i in range(3)]
    S["AttackSwing"] = [norm(whoosh(0.18, 3000 - 400 * i, 600)) for i in range(2)]
    S["ArtCast"] = [norm(mix(tone(sweep(300, 900, 0.5), 0.5, "tri", 0.05, 2) * 0.5, sizzle(0.5, 0.3), reverb(bell(880, 0.4) * 0.3)[:int(0.9 * SR)]))]
    S["Dodge"] = [norm(whoosh(0.15, 2500, 900, 0.8))]
    S["PlayerHurt"] = [norm(mix(thud(90, 0.2), sizzle(0.3, 0.5)))]
    S["Extinguish"] = [norm(mix(sizzle(0.8, 0.7), tone(sweep(400, 120, 0.8), 0.8, "sine", 0.01, 2) * 0.4))]
    S["Relight"] = [norm(mix(whoosh(0.4, 400, 2000), crackle_loop(0.8)[:int(0.8 * SR)] * 0.8))]
    S["FlameSwitch"] = [norm(mix(whoosh(0.3, 500, 2500), bell(660, 0.5) * 0.3))]
    S["Flare"] = [norm(mix(whoosh(0.5, 300, 4000), sizzle(0.5, 0.3)))]
    S["TrimOn"] = [norm(tone(sweep(700, 350, 0.15), 0.15, "sine", 0.005, 2), 0.5)]
    S["TrimOff"] = [norm(tone(sweep(350, 700, 0.15), 0.15, "sine", 0.005, 2), 0.5)]
    S["WaxPickup"] = [norm(tone(1200 * (1 + 0.12 * i), 0.12, "sine", 0.002, 4), 0.6) for i in range(3)]
    S["ShardPickup"] = [norm(bell(1760, 0.3, decay=4), 0.6)]
    S["Interact"] = [norm(mix(lowpass(noise(0.03), 2500), tone(900, 0.06)), 0.5)]
    S["HitFlesh"] = [norm(mix(thud(110, 0.12), lowpass(noise(0.1), 1200) * env(int(0.1 * SR), 0.001, 0.02, curve=5)))]
    S["HitWax"] = [norm(mix(thud(130, 0.12), lowpass(noise(0.12), 900) * env(int(0.12 * SR), 0.001, 0.02, curve=4) * 0.6)) for _ in range(2)]
    S["HitMetal"] = [norm(bell(420, 0.35, ((1, 1), (2.41, 0.6), (3.87, 0.5), (5.21, 0.4)), decay=5))]
    S["HitBlocked"] = [norm(mix(bell(300, 0.18, ((1, 1), (1.47, 0.5)), decay=7), thud(200, 0.08)), 0.6)]
    S["HitGlass"] = [norm(bell(2200, 0.35, ((1, 1), (2.3, 0.5), (3.9, 0.3)), decay=5), 0.6)]
    S["EnemyDeath"] = [norm(mix(tone(sweep(300, 60, 0.4), 0.4, "tri", 0.005, 2) * 0.6, lowpass(noise(0.4), 800) * env(int(0.4 * SR), 0.01, 0.05, curve=2)))]
    S["BoltFire"] = [norm(mix(tone(sweep(1800, 600, 0.2), 0.2, "tri", 0.002, 3), highpass(noise(0.2), 3000) * env(int(0.2 * SR)) * 0.3))]
    S["PoisonBurst"] = [norm(mix(*[at(tone(rng.uniform(200, 500), 0.08, "sine", 0.002, 3), rng.uniform(0, 0.3), 0.45) for _ in range(9)]))]
    S["RadiantPulse"] = [norm(reverb(mix(bell(523, 0.6) * 0.5, bell(659, 0.6) * 0.4, bell(784, 0.6) * 0.3))[:int(1.1 * SR)])]
    S["CrimsonStrike"] = [norm(mix(thud(60, 0.35, 0.6), whoosh(0.2, 1500, 300)))]
    S["EnemyAlert"] = [norm(mix(tone(330, 0.12, "tri"), at(tone(440, 0.16, "tri"), 0.1, 0.3)), 0.6)]
    S["DriplingSquelch"] = [norm(mix(tone(sweep(400, 150, 0.2), 0.2, "sine", 0.003, 2), lowpass(noise(0.2), 900) * 0.3 * env(int(0.2 * SR))))]
    S["WaxflyBuzz"] = [norm(lowpass(tri(180, 0.4) * (0.6 + 0.4 * sine(28, 0.4)), 1500) * env(int(0.4 * SR), 0.05, 0.1, curve=1), 0.4)]
    S["HuskDrop"] = [norm(mix(highpass(noise(0.05), 2000) * 0.8, at(thud(80, 0.3), 0.05, 0.4)))]
    S["LurkerSkitter"] = [norm(mix(*[at(highpass(noise(0.015), 2500), rng.uniform(0, 0.28), 0.3) for _ in range(10)]), 0.5)]
    S["BloomSnap"] = [norm(mix(highpass(noise(0.03), 1500), at(tone(sweep(200, 90, 0.2), 0.2), 0.05, 0.3)))]
    S["SlagbornStep"] = [norm(mix(thud(50, 0.25, 0.4), bell(160, 0.2, ((1, 1), (2.7, 0.4)), decay=8) * 0.3))]
    S["SlagbornSmash"] = [norm(mix(thud(40, 0.7, 0.8), bell(140, 0.6, ((1, 1), (2.41, 0.6), (3.9, 0.4)), decay=4) * 0.5, lowpass(noise(0.7), 300) * env(int(0.7 * SR), 0.01, 0.1, curve=2)))]
    S["BellowsWindup"] = [norm(bandpass(noise(0.6), 300, 1200) * np.linspace(0.1, 1, int(0.6 * SR)), 0.5)]
    S["BellowsPuff"] = [norm(lowpass(noise(0.3), 700) * env(int(0.3 * SR), 0.003, 0.05, curve=3))]
    S["ReflectionGlitch"] = [norm(np.round(mix(tone(sweep(800, 1600, 0.2), 0.2, "tri", 0.001, 1), highpass(noise(0.2), 4000) * 0.3) * 4) / 4, 0.4)]
    S["UnlitWhisper"] = [norm(bandpass(noise(0.9), 500, 1600) * (0.5 + 0.5 * sine(5, 0.9)) * env(int(0.9 * SR), 0.2, 0.2, curve=1), 0.35)]
    S["GeyserWarn"] = [norm(mix(*[at(tone(rng.uniform(150, 400), 0.06, "sine", 0.002, 3), x, 1.2) for x in np.sort(rng.uniform(0, 1.15, 26))]) * np.linspace(0.3, 1, int(1.2 * SR)))]
    S["GeyserErupt"] = [norm(mix(whoosh(0.6, 300, 3000), thud(70, 0.3), sizzle(0.6, 0.4)))]
    S["PressWarn"] = [norm(mix(bandpass(noise(0.9), 1500, 5000) * 0.3 * np.linspace(0.2, 1, int(0.9 * SR)), tone(sweep(90, 140, 0.9), 0.9, "tri", 0.05, 0.5) * 0.4))]
    S["PressSlam"] = [norm(mix(thud(45, 0.5, 0.9), bell(180, 0.4, ((1, 1), (2.3, 0.5)), decay=6) * 0.5))]
    S["SteamHiss"] = [norm(highpass(noise(1.0), 2500) * env(int(1.0 * SR), 0.05, 0.2, curve=1), 0.5)]
    S["ConveyorLoop"] = [norm(mix(*[at(bell(260 + 40 * (k % 2), 0.08, ((1, 1), (2.6, 0.4)), decay=9), k * 0.15, 1.2) for k in range(8)], lowpass(noise(1.2), 300) * 0.3), 0.4)]
    S["LaserCharge"] = [norm(tone(sweep(300, 1600, 0.8, curve=2), 0.8, "tri", 0.05, 0.3) * np.linspace(0.2, 1, int(0.8 * SR)), 0.5)]
    S["LaserFire"] = [norm(mix(tri(110, 1.3) * 0.5, tri(221, 1.3) * 0.3, highpass(noise(1.3), 3000) * 0.15) * env(int(1.3 * SR), 0.01, 0.2, curve=0.6), 0.6)]
    S["GlassCrack"] = [norm(mix(highpass(noise(0.05), 3000), bell(3100, 0.15, decay=8) * 0.4), 0.6)]
    S["GlassBreak"] = [norm(mix(*[at(bell(rng.uniform(1800, 4200), 0.3, decay=6) * rng.uniform(0.3, 1), rng.uniform(0, 0.35), 0.7) for _ in range(10)], highpass(noise(0.3), 2000) * 0.5))]
    S["BrazierLight"] = [norm(mix(whoosh(0.5, 300, 1500), crackle_loop(1.0)[:int(0.9 * SR)] * 0.7))]
    S["BrazierRest"] = [norm(reverb(mix(bell(392, 1.4, decay=1.5) * 0.4, bell(494, 1.4, decay=1.5) * 0.3, bell(587, 1.4, decay=1.5) * 0.3))[:int(2 * SR)])]
    S["WaxPoolDrink"] = [norm(mix(*[at(tone(sweep(250, 160, 0.12), 0.12, "sine", 0.005, 2), k * 0.18, 0.6) for k in range(3)]))]
    S["GateOpen"] = [norm(mix(bandpass(noise(1.0), 80, 500) * np.linspace(1, 0.3, int(1.0 * SR)), at(thud(60, 0.3, 0.6), 0.9, 1.3)))]
    S["BellRing"] = [norm(reverb(bell(98, 3.0, ((1, 1), (2.0, 0.6), (2.76, 0.5), (4.1, 0.3), (5.4, 0.2)), decay=1.2), mix=0.35, size=1.4)[:int(3.5 * SR)])]
    S["RelicGet"] = [norm(reverb(mix(*[at(bell(f, 0.8, decay=2.5) * 0.5, 0.12 * i, 1.4) for i, f in enumerate((523, 659, 784, 1046))]))[:int(1.6 * SR)])]
    S["BeamHum"] = [norm(mix(sine(220, 0.5) * 0.5, sine(331, 0.5) * 0.3) * env(int(0.5 * SR), 0.05, 0.1, curve=0.8), 0.4)]
    S["PanelLit"] = [norm(reverb(bell(880, 0.6, decay=2.5))[:int(1.0 * SR)], 0.6)]
    S["RoseComplete"] = [norm(reverb(mix(*[at(bell(f, 1.6, decay=1.5) * 0.4, 0.15 * i, 2.2) for i, f in enumerate((440, 554, 659, 880, 1108))]), mix=0.4)[:int(2.6 * SR)])]
    S["KingRise"] = [norm(mix(lowpass(noise(2.0), 150) * np.linspace(0, 1, int(2 * SR)), tone(sweep(40, 55, 2.0), 2.0, "tri", 0.5, 0.5) * 0.6))]
    S["KingSlam"] = [norm(mix(thud(35, 0.9, 1.0), lowpass(noise(0.9), 250) * env(int(0.9 * SR), 0.005, 0.2, curve=2)))]
    S["KingVoid"] = [norm(whoosh(0.7, 200, 1800)[::-1] * 0.9)]
    S["KingPhase"] = [norm(mix(tone(55, 1.5, "tri", 0.2, 1) * 0.5, tone(58.3, 1.5, "tri", 0.2, 1) * 0.5, lowpass(noise(1.5), 400) * env(int(1.5 * SR), 0.1, 0.3, curve=1)))]
    S["KingFall"] = [norm(mix(tone(sweep(80, 30, 2.5), 2.5, "tri", 0.05, 1.2) * 0.6, lowpass(noise(2.5), 200) * env(int(2.5 * SR), 0.05, 0.5, curve=1.5)))]
    S["UiMove"] = [norm(tone(1400, 0.04, "sine", 0.001, 6), 0.4)]
    S["UiConfirm"] = [norm(mix(tone(880, 0.07), at(tone(1320, 0.09), 0.06, 0.16)), 0.5)]
    S["UiBack"] = [norm(mix(tone(990, 0.07), at(tone(660, 0.09), 0.06, 0.16)), 0.5)]
    S["UiOpen"] = [norm(tone(sweep(400, 800, 0.2), 0.2, "sine", 0.03, 1.5), 0.4)]
    S["UiError"] = [norm(lowpass(tri(140, 0.15), 1200) * env(int(0.15 * SR), 0.002, 0.03, curve=1), 0.5)]
    S["Purchase"] = [norm(mix(bell(1318, 0.3, decay=4), at(bell(1760, 0.3, decay=4), 0.08, 0.4)), 0.6)]
    S["ScoreTally"] = [norm(tone(1600, 0.03, "sine", 0.001, 6), 0.35)]
    S["RankStamp"] = [norm(mix(thud(80, 0.3, 0.6), reverb(bell(660, 0.5, decay=2.5))[:int(0.6 * SR)] * 0.6))]
    return S


# ---- music: generative loops per region ----------------------------------------------------------------------

NOTE = {"C": 0, "C#": 1, "D": 2, "D#": 3, "E": 4, "F": 5, "F#": 6, "G": 7, "G#": 8, "A": 9, "A#": 10, "B": 11}
MODES = {"minor": [0, 2, 3, 5, 7, 8, 10], "dorian": [0, 2, 3, 5, 7, 9, 10], "phrygian": [0, 1, 3, 5, 7, 8, 10],
         "major": [0, 2, 4, 5, 7, 9, 11], "pent": [0, 3, 5, 7, 10]}


def freq_of(root, octave, degree, mode):
    steps = MODES[mode]
    o, d = divmod(degree, len(steps))
    semis = NOTE[root] + steps[d] + 12 * (octave + o)
    return 440.0 * 2 ** ((semis - 57) / 12)


def drone(root, seconds, octave=1, bright=300):
    f = freq_of(root, octave, 0, "minor")
    x = mix(tri(f, seconds, MSR) * 0.5, tri(f * 1.5, seconds, MSR) * 0.25, sine(f * 2, seconds, MSR) * 0.2)
    lfo = 0.75 + 0.25 * sine(1 / 9, seconds, MSR)
    return lowpass(x * lfo, bright, MSR)


def track(root, mode, seconds, bpm, voice, density, seed, extras=()):
    """A loop of `seconds`: rendered 1 s longer, and that overflow crossfaded into the start so the
    end flows into the beginning without a click."""
    r = np.random.default_rng(seed)
    over = 1.0
    length = seconds + over
    total = int(length * MSR)
    out = drone(root, length, 1, 260 if voice != "bell" else 220) * 0.5
    beat = 60.0 / bpm
    t = 0.0
    phrase = [r.integers(0, 7) for _ in range(8)]
    k = 0
    while t < length - 1.5:
        if r.random() < density:
            deg = phrase[k % len(phrase)] + (7 if r.random() < 0.25 else 0)
            f = freq_of(root, 3, deg, mode)
            dur = beat * r.choice([1, 2, 2, 3])
            if voice == "pluck":
                n = pluck(f, dur + 1.0, MSR, 0.7) * 0.35
            elif voice == "bell":
                n = bell(f, dur + 1.5, sr=MSR, decay=2.2) * 0.25
            elif voice == "reed":
                n = lowpass(tri(f, dur + 0.3, MSR) * env(int((dur + 0.3) * MSR), 0.08, 0.2, MSR, 1.2), 1200, MSR) * 0.3
            else:
                n = sine(f, dur + 1.0, MSR) * env(int((dur + 1.0) * MSR), 0.05, 0.3, MSR, 2) * 0.3
            i = int(t * MSR)
            out[i:i + len(n)] += n[:max(0, total - i)]
        t += beat
        k += 1
        if k % 8 == 0 and r.random() < 0.5:
            phrase[r.integers(0, 8)] = r.integers(0, 7)
    for extra in extras:
        out += extra(length, r)[:total]
    out = reverb(out, MSR, 0.35, 1.3)[:total]
    out = highpass(out, 20, MSR)                       # no DC offset
    loop = int(seconds * MSR)
    xf = total - loop
    ramp = np.linspace(0, 1, xf)
    result = out[:loop].copy()
    result[:xf] = out[:xf] * ramp + out[loop:loop + xf] * (1 - ramp)
    return norm(result, 0.7)


def hits(freq, every, amp):
    def f(seconds, r):
        out = np.zeros(int(seconds * MSR))
        t = 0.0
        while t < seconds - 0.5:
            n = bell(freq * r.choice([1, 1.5]), 0.4, ((1, 1), (2.41, 0.5), (3.9, 0.3)), sr=MSR, decay=6) * amp
            i = int(t * MSR)
            out[i:i + len(n)] += n[:max(0, len(out) - i)]
            t += every * r.choice([1, 1, 2])
        return out
    return f


def drums(bpm, amp):
    def f(seconds, r):
        out = np.zeros(int(seconds * MSR))
        beat = 60.0 / bpm
        t, k = 0.0, 0
        while t < seconds - 0.5:
            n = sine(sweep(110, 45, 0.3, MSR), 0.3, MSR) * env(int(0.3 * MSR), 0.002, 0.05, MSR, 4) * (amp if k % 4 == 0 else amp * 0.6)
            i = int(t * MSR)
            out[i:i + len(n)] += n[:max(0, len(out) - i)]
            t += beat
            k += 1
        return out
    return f


def wind(amp):
    def f(seconds, r):
        n = lowpass(np.random.default_rng(3).uniform(-1, 1, int(seconds * MSR)), 500, MSR)
        return n * amp * (0.6 + 0.4 * sine(1 / 7, seconds, MSR))
    return f


MUSIC = {
    "Title":          dict(root="D", mode="minor", seconds=48, bpm=60, voice="bell", density=0.45, seed=1),
    "TallowFields":   dict(root="A", mode="pent", seconds=48, bpm=66, voice="pluck", density=0.55, seed=2),
    "Wickwood":       dict(root="E", mode="phrygian", seconds=48, bpm=56, voice="reed", density=0.4, seed=3, extras=(wind(0.15),)),
    "SootFoundries":  dict(root="C", mode="minor", seconds=40, bpm=84, voice="pluck", density=0.4, seed=4, extras=(hits(220, 60 / 84 * 2, 0.25),)),
    "GlassCathedral": dict(root="F#", mode="dorian", seconds=48, bpm=54, voice="bell", density=0.4, seed=5),
    "DeepHollow":     dict(root="B", mode="phrygian", seconds=48, bpm=40, voice="sine", density=0.18, seed=6),
    "HearthChamber":  dict(root="B", mode="minor", seconds=48, bpm=50, voice="reed", density=0.35, seed=7, extras=(wind(0.2),)),
    "Boss":           dict(root="D", mode="phrygian", seconds=32, bpm=112, voice="pluck", density=0.8, seed=8, extras=(drums(112, 0.5),)),
    "Ending":         dict(root="D", mode="major", seconds=40, bpm=58, voice="bell", density=0.5, seed=9),
    "Brazier":        dict(root="G", mode="major", seconds=24, bpm=60, voice="pluck", density=0.4, seed=10),
}


def main(out_root):
    sfx_dir = os.path.join(out_root, "SFX")
    music_dir = os.path.join(out_root, "Music")
    for name, variants in sfx().items():
        for i, x in enumerate(variants):
            suffix = f"_{i + 1}" if len(variants) > 1 else ""
            write(os.path.join(sfx_dir, f"{name}{suffix}.wav"), fade(x))
    write(os.path.join(sfx_dir, "FlameCrackleLoop.wav"), crackle_loop(3.0))
    write(os.path.join(sfx_dir, "Heartbeat.wav"), fade(heartbeat(), 1, 30))
    for name, spec in MUSIC.items():
        print("music", name)
        write(os.path.join(music_dir, f"{name}.wav"), track(**spec), MSR)
    print("done:", out_root)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Assets", "Audio"))
