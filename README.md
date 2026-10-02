# Ashvale

A small medieval game built with MonoGame, grown out of the Ashvale title screen.

A knight runs along the foot of a smoldering volcano, jumping to snatch coins out
of the air while the volcano rains rocks down on him.

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

Collect **5 coins** to win. There is only ever one coin on screen: grab it and the
next one turns up somewhere else, always a good run away and usually high enough
that you have to jump for it. Rocks fall from the sky the whole time, and a single
hit ends the run.

## Sound

Music plays on a loop the whole time, and sound effects play when:

- a button is clicked
- the volcano erupts at the start of a run
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
sources and licenses. Everything is CC0, CC-BY 4.0, or the Open Font License.

The music and sound effects were made by AI.

---

Created for CIS 580 - Foundations of Game Programming at
[Kansas State University](https://ksu.edu).
