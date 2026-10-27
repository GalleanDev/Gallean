<p align="center">
  <a href="https://gallean.org"><img src="https://gallean.org/logo.webp" alt="Gallean" width="160" /></a>
</p>

<p align="center">
  A cross-platform open source Disciples 2 remake.<br />
  <a href="https://gallean.org">Website</a> ·
  <a href="https://gallean.org/blog/remaking-disciples-2-gallean">Remaking Disciples 2</a> ·
  <a href="https://gallean.org/roadmap">Roadmap</a> ·
  <a href="https://github.com/GalleanDev/Gallean/releases/latest">Download</a>
</p>

Gallean is a full remake of Disciples II: Rise of the Elves: every campaign and scenario, the original rules, hotseat multiplayer, and visuals rebuilt for 4K with dynamic lighting.

<p align="center">
  <a href="https://gallean.org/media/brand/gallean-battle.webp"><img src="https://gallean.org/media/brand/gallean-battle-1920px.webp" width="32%" /></a>
  <a href="https://gallean.org/media/brand/gallean-city.webp"><img src="https://gallean.org/media/brand/gallean-city-1920px.webp" width="32%" /></a>
  <a href="https://gallean.org/media/brand/gallean-buildings.webp"><img src="https://gallean.org/media/brand/gallean-buildings-1920px.webp" width="32%" /></a>
</p>

## System requirements

- **Operating system:** Windows 10 or newer (x64), Linux (x64), macOS 14 or newer (arm64, x64)
- **Processor:** Any 64-bit dual-core
- **Memory:** 4 GB RAM
- **Graphics:** 1 GB, DirectX 11 or OpenGL 3.0 (Integrated graphics work)

## Building

You will need the .NET SDK 10. See [`global.json`](global.json).

```bash
dotnet build
```

Run the client for your platform:

```bash
dotnet run --project src/Gallean.Client.Windows
dotnet run --project src/Gallean.Client.Linux
dotnet run --project src/Gallean.Client.MacOS
```

## Tests

```bash
dotnet test
```

Screenshot tests are explicit and need the game assets:

```bash
dotnet test --filter TestCategory=Visual
```

> [!INFO]
> On Apple Silicon Macs, install the OpenMP runtime needed by the asset pipeline tests (`brew install libomp`).

## Publishing

Each client publishes as a trimmed Native AOT build on its own platform, so you need the [Native AOT prerequisites](https://learn.microsoft.com/dotnet/core/deploying/native-aot/#prerequisites) installed:

```bash
dotnet publish
```

The release will be in `dist/` at the root, for example `dist/Gallean-0.8.0-linux-x64.zip`.

> [!INFO]
> On macOS, use the [Microsoft .NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) and install Xcode or its Command Line Tools (`xcode-select --install`).

## Assets

The game needs a `.galleanpack` with its assets. [Packer](tools/Packer) can build one from your own copy of the original game and can enhance it with the 4K asset pipeline.

---

Gallean is an independent fan project, unaffiliated with Strategy First. Disciples is a trademark of its respective owners.
