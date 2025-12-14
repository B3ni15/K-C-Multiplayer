# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Kingdoms and Castles Multiplayer Mod** that adds multiplayer functionality to the game using:
- **Riptide Networking** library for low-level networking
- **Steam P2P** transport for NAT traversal
- **Harmony** for non-invasive game modification via patches/hooks

## Architecture

### Core Components

| File | Purpose |
|------|---------|
| `Main.cs` | Entry point, Harmony patches, all game event hooks |
| `KCClient.cs` | Client-side networking wrapper around Riptide.Client |
| `KCServer.cs` | Server-side networking, client management |
| `KCPlayer.cs` | Player data container (id, steamId, inst, kingdomName) |

### Networking Layer

```
Riptide.Client/Server
    └── SteamClient/SteamServer (Steam P2P transport)
        └── KCClient/KCServer wrappers
            └── PacketHandler (serialization/routing)
```

- Port: 7777, Max clients: 25
- Team ID formula: `clientId * 10 + 2`

### Packet System

Located in `/Packets/`:
- Base class: `Packet.cs` with `Send()`, `SendToAll()`, `HandlePacketClient()`, `HandlePacketServer()`
- `PacketHandler.cs` uses reflection for automatic serialization based on property names (alphabetical order)
- Packet IDs defined in `Enums/Packets.cs`

Key packet ranges:
- 25-34: Lobby (chat, player list, settings)
- 70-79: World/building updates
- 85: Save transfer (chunked)
- 87-90: Building state, villagers

### State Synchronization

- **Buildings**: Observer pattern in `StateManagement/BuildingState/` - monitors field changes every 100ms, sends updates every 300ms
- **Villagers**: Event-based sync via Harmony hooks on `VillagerSystem.AddVillager`, `Villager.TeleportTo`
- **Save/Load**: Custom `MultiplayerSaveContainer` extends `LoadSaveContainer`, stores per-player data

### Harmony Hooks Pattern

All hooks check call stack to prevent infinite loops:
```csharp
if (new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
    return; // Skip if called by network handler
```

### Key Dictionaries

```csharp
Main.kCPlayers        // Dictionary<steamId, KCPlayer>
Main.clientSteamIds   // Dictionary<clientId, steamId>
```

## Common Issues & Patterns

### Player Resolution
```csharp
Main.GetPlayerByClientID(clientId)  // clientId -> KCPlayer
Main.GetPlayerByTeamID(teamId)      // teamId -> Player.inst
Main.GetPlayerByBuilding(building)  // building -> owner Player
```

### Building Ownership
Buildings are associated with players via `LandmassOwner.teamId`. Use `building.TeamID()` to determine owner.

### Save Directory
Multiplayer saves go to: `Application.persistentDataPath + "/Saves/Multiplayer"`

## Directory Structure

```
/Attributes          - Custom packet attributes
/Enums              - Packet types, menu states
/LoadSaveOverrides  - MultiplayerSaveContainer
/Packets            - All network packets
/Riptide            - Networking library
/RiptideSteamTransport - Steam P2P adapter, LobbyManager
/StateManagement    - Observer pattern for sync
/ServerLobby        - Lobby UI
/ServerBrowser      - Server discovery
/UI                 - Custom UI elements
```

## Known Architecture Limitations

1. Static `Client`/`Server` instances can cause issues on reconnect
2. Call stack checking for loop prevention is fragile
3. No conflict resolution - last-write-wins
4. Villager sync is event-based only, no continuous state updates
