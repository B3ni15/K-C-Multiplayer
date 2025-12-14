# Kingdoms and Castles Multiplayer Mod - Bug Tracker

## Ismert hibak / Known Issues

### Host-Client Sync Problems

| Hiba | Status | Megjegyzes |
|------|--------|------------|
| Rossz kapcsolat, "server disconnected" hibak | Reszben javitva | Event handler duplikacio es session cleanup javitva |
| Jatek ujrainditasa szukseges lobby/save valtas utan | Reszben javitva | Session cleanup hozzaadva LobbyManager-ben |
| Utak/epuletek nem toltenek be helyesen vagy atfednek | Vizsgalat alatt | UpdateMaterialSelection() es UpdateRotation() hozzaadva |
| Eroforrasok nem mentenek/toltenek helyesen | Meg nincs elkezdve | |
| NPC-k veletlenszeruen megallnak es nem mozognak load utan | Vizsgalat alatt | TeleportTo problema javitva, BakePathing hozzaadva |
| Orientaciok (rotaciok) nem szinkronizalodnak | Reszben javitva | Rotation es localPosition kozvetlenul alkalmazva WorldPlace-ben |
| Host torol valamit -> kliens nem latja | Meg nincs elkezdve | BuildingDestroy packet szukseges |
| Host nem latja a kliens epuleteit helyesen (rossz textúrak) | Javitva | UpdateMaterialSelection() hozzaadva WorldPlace.cs-ben |

### Status Definiciok

- **Javitva**: A hiba javitva lett es tesztelve
- **Reszben javitva**: Javitas megkezdve, de meg nem teljes
- **Vizsgalat alatt**: Debug logging hozzaadva, vizsgaljuk a problemat
- **Meg nincs elkezdve**: A hiba ismert, de meg nem kezdtuk el javitani

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
