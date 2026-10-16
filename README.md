# Ashvale

A small medieval game built with MonoGame, grown out of the Ashvale title screen.

A knight runs along the foot of a pair of smoldering volcanoes, jumping to snatch
coins out of the air while the volcanoes rain rocks down on him.

## Running it

```
dotnet run
```

Requires the .NET 9 SDK. On the first build, run `dotnet tool restore` to install
the MonoGame content builder.

## Menus

The title screen has three buttons:

- **Start Game** begins a run.
- **Settings** opens a menu with two more screens: **Controls**, which lists every key,
and **How to Play**, which explains the objective and how to win or lose.
- **Quit** closes the game.

When a run ends, **Play Again** starts a new one and **Main Menu** goes back to the
title screen.

## Playing it

When a run starts, the volcano throws a few rocks clear of its mouth first, and then
the knight is free to move.

Collect **5 coins** to win. There is only ever one coin out at a time: grab it and the
next one turns up somewhere else, always a good run away and usually high enough
that you have to jump for it. Rocks fall from the sky the whole time, and a single
hit ends the run.

The run starts with 3 rocks falling. Every coin you grab angers the volcano: it rumbles,
throws up a shower of embers, and another rock joins the fall, up to 7 by the last coin.

## The world

Ashvale is three screens wide, and the camera scrolls to follow the knight. The map
across the top of the screen shows the whole world, the part you can see, where the
knight is (white) and where the coin is (gold). When the coin is off screen, a gold
arrow at the edge points the way.

The castle and volcano painting scrolls at half the speed of the road, so it looks far
away. Every other copy of the painting is mirrored, so the copies meet without a seam.

## Visual effects

|Effect|Where it is|How it works|
|---|---|---|
|Scrolling, zoom and screen shake|`Camera.cs`|Every layer is drawn with `SpriteBatch.Begin(transformMatrix: camera.GetTransform(...))`. The camera zooms in on the volcano at the start of a run and on the knight when a run ends, and shakes the screen for eruptions, landing rocks and hits|
|Parallax layers|`ParallaxBackground.cs`|The background batch uses a transform that scrolls at 0.5 times the world's speed|
|Volcano embers|`Particles/EmberParticleSystem.cs`|Sparks drawn with `BlendState.Additive` that fade from yellow to orange to red|
|Falling ash|`Particles/AshParticleSystem.cs`|Spinning, semi-transparent flakes, each tinted its own grey|
|Rock debris and dust|`Particles/DebrisParticleSystem.cs`, `Particles/DustParticleSystem.cs`|Rock chips tinted from a set of rock colors that tumble and settle on the road, plus translucent clouds of dust|
|Explosion and sparkles|`Particles/ExplosionParticleSystem.cs`, `Particles/SparkleParticleSystem.cs`|Additive bursts when the knight is hit, when you grab a coin, and as fireworks when you win|

The particle engine (`Particle.cs`, `ParticleSystem.cs`, `RandomHelper.cs`) comes from the
CIS 580 particle system tutorial. It now draws through the camera, which it gets from
`Game.Services`.

## Sound

Music plays on a loop the whole time, and sound effects play when:

- a button is clicked
- a volcano erupts at the start of a run
- the knight jumps
- a coin is picked up
- a rock hits the knight (the music drops so the hit stands out)
- the last coin is collected and a fanfare plays

Press **M** at any time to mute or unmute everything.

## Controls

|Key|Action|
|---|---|
|**Left arrow** / **A**|Run left|
|**Right arrow** / **D**|Run right|
|**Space**|Jump|
|**Left mouse button**|Press a menu button|
|**M**|Mute or unmute the sound|
|**Esc**|Exit|

## Assets

All artwork and fonts used are listed in [ASSETS.md](ASSETS.md), along with their
sources and licenses. Everything is CC0, public domain, CC-BY 4.0, the Open Font
License, or the Microsoft Permissive License.

The music and sound effects were made by AI.

---

Created for CIS 580 - Foundations of Game Programming at
[Kansas State University](https://ksu.edu).
