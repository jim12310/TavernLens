# TavernLens

A Windows overlay companion for Hearthstone Battlegrounds.

Website and Windows installer: https://tavernlens.pages.dev/

## Features

- Solo and Duos game detection and separate MMR tracking.
- Minion browsing with normal and golden previews.
- Ranked Solo composition guides used in both modes, with a pinned build shelf.
- Previously revealed opponent boards and mid-match log recovery.
- Blood Gem and Tavern spell counters.
- Experimental combat estimates, including conditional Duos estimates.

Combat estimates can be unavailable or inaccurate when the game state or card effects are unsupported. Hidden boards cannot be recovered. Mid-match recovery requires the earlier game logs. Public data sources can change and are not guaranteed to reflect every patch immediately.

## Building from source

This is the source snapshot of the current Windows tracker. It is a .NET Framework Windows Forms application with Node.js helpers. It is not a self-contained build: external game-reader dependencies must be obtained separately.

1. Use 64-bit Windows with .NET Framework 4.x and its C# compiler.
2. Install a current Node.js release with npm. Place a copy of node.exe at runtime/node.exe, as the application expects this location.
3. Run `npm ci --prefix simulator` and `npm install --prefix art-runtime` from this folder.
4. Obtain the matching reader dependencies described in GAME-READER-NOTICE.txt from the official Hearthstone Deck Tracker v1.55.6 release. Place HearthMirror.dll, untapped-scry-dotnet.dll, Newtonsoft.Json.dll and System.Reflection.DispatchProxy.dll in this folder. These third-party binaries are not included in this repository.
5. Run `node src/update-data.cjs` to populate the data directory from public sources. Optional card-art caching: `node src/cache-art.cjs`.
6. Run `powershell -ExecutionPolicy Bypass -File Build.ps1` and launch TavernLens.exe.

Use the app's Setup and sources screen to configure Hearthstone logging. An installer is available on the website for people who do not want to build the source.

## Repository contents

- src/: tracker, overlay, data helpers and existing test code.
- assets/: Ember Tavern branding and menu detection reference images.
- simulator/: dependency manifest, lockfile and third-party notices.
- art-runtime/: image decoding dependency manifest and lockfile.

Personal settings, ratings, match history, sessions, downloaded card caches, compiled applications and installed dependencies are excluded.

## Attribution

Independent project, not affiliated with Blizzard, HearthSim, HSReplay or Firestone. Hearthstone card artwork, names and text belong to their respective owners. Public data adapters use HearthstoneJSON, Battlegrounds Buddy and other sources identified in the code. Combat simulation uses Firestone packages. See GAME-READER-NOTICE.txt and simulator/THIRD-PARTY-NOTICES.txt for third-party components.

No blanket license is granted here for third-party code or artwork. A project-wide license has not yet been selected.
