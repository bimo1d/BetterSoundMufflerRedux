# Better Sound Muffler Redux

Redux-native vacuum sound muffling module for Kerbal Space Program 2.

Better Sound Muffler Redux adds configurable sound muffling in vacuum.

## Features

- Vacuum muffling.
- Muffling for engines, impacts, collisions, RCS, decouplers and staging.
- Configurable muffling amount: 25%, 50%, 75% or 100%.
- Smooth sound transition between atmosphere and vacuum.
- App bar button.
- Redux settings menu integration.
- English and Russian localization.

## Installation

1. Make sure you are using `KSP2 Redux` v0.2.3.0-beta.
2. Download the latest release.
3. Copy `BetterSoundMufflerRedux` into your KSP 2 `mods` folder.
4. Remove older Better Sound Muffler Redux files before updating.

## Dependencies

- Redux
- SpaceWarp 2

## Configuration

Redux Settings:
- `Better Sound Muffler Redux`
- `Main`
- `Advanced`

Runtime config:
- `mods/BetterSoundMufflerRedux/BetterSoundMufflerRedux-config.json`

## Controls

App bar:
- Left click the app bar button: open module settings.
- Right click the app bar button: enable or disable the module.

Other behavior:
- Disable the module to use stock sound volume and default muffling.
- `100%` mutes affected audio in vacuum.
- UI sounds, music, app bar sounds, settings sounds and kerbal comms are not changed.
- Sound returns smoothly when the active vessel enters atmosphere again.

## Compatibility

- Built for KSP 2 `0.2.3.0`.
- Built for Redux and SpaceWarp 2.
- Designed for active vessel audio in flight.

## Development status

Public beta.

## Build

Run from this folder:

```powershell
.\build.ps1
```

The build script compiles the plugin and deploys it to:

```text
C:\Games\Kerbal Space Program 2\mods\BetterSoundMufflerRedux
```

If your KSP 2 install path is different, change the default `$Ksp2Root` value in `build.ps1`.

## Disclaimer and License

This mod is released under the Unlicense, which means it is in the public domain.
