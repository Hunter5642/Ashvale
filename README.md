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

## Playing it

Press **Enter** on the title screen to start. The volcano throws a few rocks clear
of its mouth first, and then the knight is free to move.

Collect **5 coins** to win. There is only ever one coin on screen: grab it and the
next one turns up somewhere else, always a good run away and usually high enough
that you have to jump for it. Rocks fall from the sky the whole time, and a single
hit ends the run.

Win or lose, press **Enter** to play again.

## Controls

|Key|Action|
|---|---|
|**Left arrow** / **A**|Run left|
|**Right arrow** / **D**|Run right|
|**Space**|Jump|
|**Enter**|Start, or play again|
|**Esc**|Exit|

## Assets

All artwork and fonts used are listed in [ASSETS.md](ASSETS.md), along with their
sources and licenses. Everything is CC0, CC-BY 4.0, or the Open Font License.

---

Created for CIS 580 - Foundations of Game Programming at
[Kansas State University](https://ksu.edu).
