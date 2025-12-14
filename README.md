# Kingdoms and Castles Multiplayer Mod - Bug Tracker

## Ismert hibak / Known Issues

### KRITIKUS - Server/Connection Problems

| Hiba | Status | Megjegyzes |
|------|--------|------------|
| Rossz kapcsolat, "server disconnected" hibak | Reszben javitva | Event handler duplikacio es session cleanup javitva |
| StartGame.Start() NullReferenceException | Meg nincs elkezdve | MainMenuMode.StartGame crash-el 2x a logban, TargetInvocationException |

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

### Hibak szamokban
- **56 BUILDING PLACEMENT START** - epulet elhelyezesi kiserlet
- **1 BUILDING PLACEMENT END** - sikeres elhelyezes
- **55 FAIL (98%)** - majdnem minden epulet elbukik!
- **55 "Error in add building hook"** - NullReferenceException
- **9 IndexOutOfRangeException** - tomb tulindexeles
- **2 StartGame.Start() crash** - TargetInvocationException
- **2 Client disconnect event** - kapcsolat megszakadas

### Idovonal
- 15:39: Session start
- 15:40:03: Exception MainMenuMode.StartGame - NullReferenceException (1.)
- 15:42:14: Client disconnect events (ketszer)
- 15:44:11: Ujabb Exception MainMenuMode.StartGame (2.)
- 15:45-15:50: **55x "Error in add building hook"** - szinte minden epulet fail
- 15:52:00: Utolso BuildingStatePacket erkezik client 2-tol, utana csak ServerSettings
- 15:53:55: **Host kilep menube** ("Menu set to: Menu")
- 15:53:55-15:56:25: **Server TOVABB FUT** es fogadja a packeteket a klienstol!
- 15:56:27: Server vegre leall

### Fo problemak a logbol
1. **PlayerAddBuildingHook.Prefix** crash-el 55x - valami null benne (1-11 szamokat printel elotte)
2. **Server nem all le menu-nel** - 2.5 percig meg fut miutan host kiment
3. **Kliens nem kap ertesitest** - tovabb kuldi a packeteket
4. **StartGame exception** - 2x crash jatek inditaskor

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

---

## Javítások / Fixed Issues (2024-12-14)

### KRITIKUS hibák javítva:

1. **Server leállítás menü váltáskor** - `Main.cs:342-356`
   - Server most leáll és értesíti a klienseket amikor host menübe lép
   - Kliensek kapnak "Host disconnected" modal-t

2. **PlayerAddBuildingHook NullReferenceException** - `Main.cs:762-806`
   - Teljes null-ellenőrzés hozzáadva reflection mezőkhöz
   - Array bounds ellenőrzés landMass indexhez
   - Registry inicializálás ellenőrzés

3. **IndexOutOfRangeException WorldPlace-ben** - `WorldPlace.cs:167-183`
   - LandMassNames tömb automatikus bővítése szükség esetén
   - Védekező kód hogy megelőzze az index hibákat
