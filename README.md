# Kingdoms and Castles Multiplayer Mod - Bug Tracker

## Ismert hibak / Known Issues

### KRITIKUS - Server/Connection Problems

| Hiba | Status | Megjegyzes |
|------|--------|------------|
| Server nem all le amikor host kilep menube | Meg nincs elkezdve | Host menu-be megy, de server tovabb fut es fogadja a packeteket a klienstol |
| Kliens nem lesz kidobva host kilepesekor | Meg nincs elkezdve | Kliens tovabb jatszik miutan host kiment, nem kap ertesitest |
| Packetek erkeznek menu-ben | Meg nincs elkezdve | Menu-ben is fogadja es feldolgozza a packeteket, ami nem helyes |
| Rossz kapcsolat, "server disconnected" hibak | Reszben javitva | Event handler duplikacio es session cleanup javitva |

### Building Placement Errors (output.txt-bol)

| Hiba | Status | Megjegyzes |
|------|--------|------------|
| PlayerAddBuildingHook NullReferenceException | Meg nincs elkezdve | Tobbszor elofordul: "Error in add building hook" + NullRef a Prefix-ben. ~50+ elofordulas a logban |
| IndexOutOfRangeException WorldPlace-ben | Meg nincs elkezdve | Nehany epulet elhelyezesnel: "Index was outside the bounds of the array" |
| Epuletek nem jelennek meg kliensnel | Vizsgalat alatt | A fenti hibak miatt sok epulet nem kerul elhelyezesre |

### Host-Client Sync Problems

| Hiba | Status | Megjegyzes |
|------|--------|------------|
| Jatek ujrainditasa szukseges lobby/save valtas utan | Reszben javitva | Session cleanup hozzaadva LobbyManager-ben |
| Utak/epuletek nem toltenek be helyesen vagy atfednek | Vizsgalat alatt | UpdateMaterialSelection() es UpdateRotation() hozzaadva |
| Eroforrasok nem mentenek/toltenek helyesen | Meg nincs elkezdve | |
| NPC-k veletlenszeruen megallnak es nem mozognak load utan | Vizsgalat alatt | TeleportTo problema javitva, BakePathing hozzaadva |
| Orientaciok (rotaciok) nem szinkronizalodnak | Reszben javitva | Rotation es localPosition kozvetlenul alkalmazva WorldPlace-ben |
| Host torol valamit -> kliens nem latja | Meg nincs elkezdve | BuildingDestroy packet szukseges |
| Host nem latja a kliens epuleteit helyesen (rossz texturak) | Javitva | UpdateMaterialSelection() hozzaadva WorldPlace.cs-ben |

### Gameplay Bugs

| Hiba | Status | Megjegyzes |
|------|--------|------------|
| Tobb Keep ugyanarra a szigetre | Meg nincs elkezdve | Engedi hogy masik jatekos szigetere Keep-et rakjunk, igy az eredeti jatekos elveszti a Keep-jet es nem tudja mozgatni az embereit |

### Status Definiciok

- **Javitva**: A hiba javitva lett es tesztelve
- **Reszben javitva**: Javitas megkezdve, de meg nem teljes
- **Vizsgalat alatt**: Debug logging hozzaadva, vizsgaljuk a problemat
- **Meg nincs elkezdve**: A hiba ismert, de meg nem kezdtuk el javitani

## Log Analisis (2024-12-14 15:39-15:56)

### Idovonal
- 15:39: Session start
- 15:40:03: Exception MainMenuMode.StartGame - NullReferenceException
- 15:42:14: Client disconnect events (ketszer)
- 15:44:11: Ujabb Exception MainMenuMode.StartGame
- 15:45-15:50: **Sok "Error in add building hook"** - NullReferenceException es IndexOutOfRangeException
- 15:52:00: Utolso BuildingStatePacket erkezik client 2-tol, utana csak ServerSettings
- 15:53:55: **Host kilep menube** ("Menu set to: Menu")
- 15:53:55-15:56:25: **Server TOVABB FUT** es fogadja a packeteket a klienstol!
- 15:56:27: Server vegre leall

### Fo problemak a logbol
1. **PlayerAddBuildingHook.Prefix** crash-el sokszor - valami null benne
2. **Server nem all le menu-nel** - 2.5 percig meg fut miutan host kiment
3. **Kliens nem kap ertesitest** - tovabb kuldi a packeteket

## Recent Changes

### 2024-12-14
- Advanced sync logging hozzaadva `[SYNC]` prefix-szel
- Building placement reszletes logging (minden property)
- Packet send/receive logging
- Building state update logging

### Korabbi javitasok
- KCServer.cs: Event handler duplikacio javitas
- LobbyManager.cs: Session cleanup (clientSteamIds, loadingSave)
- WorldPlace.cs: Building guid duplikacio check, rotation/localPosition fix
- AddVillagerPacket.cs: Villager position sync + duplikacio check
- Main.cs: BakePathing() hozzaadva PlayerAddBuildingHook-ban
