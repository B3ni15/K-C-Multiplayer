# Kingdoms and Castles Multiplayer Mod - Bug Tracker

## Ismert hibak / Known Issues

### KRITIKUS - Server/Connection Problems

| Hiba | Status | Megjegyzes |
|------|--------|------------|
| Server nem all le amikor host kilep menube | Meg nincs elkezdve | Host menu-be megy, de server tovabb fut es fogadja a packeteket a klienstol |
| Kliens nem lesz kidobva host kilepesekor | Meg nincs elkezdve | Kliens tovabb jatszik miutan host kiment, nem kap ertesitest |
| Packetek erkeznek menu-ben | Meg nincs elkezdve | Menu-ben is fogadja es feldolgozza a packeteket, ami nem helyes |
| Rossz kapcsolat, "server disconnected" hibak | Reszben javitva | Event handler duplikacio es session cleanup javitva |
| StartGame.Start() NullReferenceException | Meg nincs elkezdve | MainMenuMode.StartGame crash-el 2x a logban, TargetInvocationException |

### Building Placement Errors (output.txt-bol) - KRITIKUS!

| Hiba | Status | Megjegyzes |
|------|--------|------------|
| **PlayerAddBuildingHook NullReferenceException** | Meg nincs elkezdve | **56 PLACEMENT START, csak 1 PLACEMENT END!** Szinte minden epulet fail-el. A hook 1-11 szamokat printeli majd crash |
| IndexOutOfRangeException WorldPlace-ben | Meg nincs elkezdve | 9 elofordulas - "Index was outside the bounds of the array" |
| Epuletek nem jelennek meg kliensnel | Vizsgalat alatt | A fenti hibak miatt 55/56 epulet NEM kerul elhelyezesre! |

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

## Kód Hibák Részletes Dokumentációja

> **FIGYELEM**: Ez a szekció a kódban található hibák részletes elemzését tartalmazza.
> A hibák **NEM** lettek javítva, csak dokumentálva vannak a pontos hellyel és javítási javaslatokkal.

### 1. KRITIKUS: Server nem áll le amikor host kilép menübe

**Fájl**: `KCServer.cs`
**Hiba helye**: Hiányzik a logika - nincs kód ami leállítaná a servert menüváltáskor
**Kapcsolódó kód**:
- `KCServer.cs:110-122` - `OnApplicationQuit()` metódus (csak alkalmazás bezáráskor hívódik)
- `Main.cs:334-346` - `TransitionToHook` (detektálja a menü változást, de nem reagál rá)

**Probléma részletesen**:
```csharp
// KCServer.cs:110-122
private void OnApplicationQuit()
{
    if (server != null && server.IsRunning)
    {
        new ShowModal { ... }.SendToAll();
        server.Stop();
    }
}
```

- A server CSAK akkor áll le, ha az alkalmazás teljesen bezár (`OnApplicationQuit`)
- Amikor a host menübe lép (pl. `MainMenuMode.State.Menu`), a `TransitionToHook` észleli a változást
- DE nincs kód ami meghívná a `server.Stop()`-ot
- Eredmény: server tovább fut és fogadja a packeteket, kliens nem kap értesítést

**Hol kell javítani**:
1. **Opció A**: `Main.cs:334-346` - TransitionToHook Prefix metódusban
   - Ellenőrizni kell: ha `newState == MainMenuMode.State.Menu` ÉS `KCServer.IsRunning`
   - Akkor hívni: `KCServer.server.Stop()` és értesíteni a klienseket

2. **Opció B**: `KCServer.cs` - új metódus hozzáadása
   - Létrehozni egy `StopServer()` metódust ami értesíti a klienseket és leállítja a servert
   - Ezt meghívni a `TransitionToHook`-ból amikor menübe lép a host

**Miért kritikus**:
- Kliens nem tudja, hogy a host kilépett
- Server erőforrásokat pazarol
- Packetek feldolgozása menüben hibákhoz vezet

---

### 2. KRITIKUS: PlayerAddBuildingHook NullReferenceException

**Fájl**: `Main.cs`
**Hiba helye**: `Main.cs:764`
**Érintett kód**:
```csharp
// Main.cs:755-764
var globalBuildingRegistry = __instance.GetType().GetField("globalBuildingRegistry", ...).GetValue(__instance) as ArrayExt<Player.BuildingRegistry>;
LogStep(); // 7
var landMassBuildingRegistry = __instance.GetType().GetField("landMassBuildingRegistry", ...).GetValue(__instance) as ArrayExt<Player.LandMassBuildingRegistry>;
LogStep(); // 8
var unbuiltBuildingsPerLandmass = __instance.GetType().GetField("unbuiltBuildingsPerLandmass", ...).GetValue(__instance) as ArrayExt<ArrayExt<Building>>;
LogStep(); // 9 (utolsó amit elér)

__instance.AddToRegistry(globalBuildingRegistry, b);
LogStep(); // 10 (sosem éri el)
__instance.AddToRegistry(landMassBuildingRegistry.data[landMass].registry, b); // <-- CRASH ITT (line 764)
```

**Probléma részletesen**:
- A README szerint: **56 PLACEMENT START, csak 1 PLACEMENT END** → 55/56 épület fail
- LogStep() 1-11-ig printeli (de legtöbbször csak 1-9-ig jut el)
- A crash valószínűleg itt: `landMassBuildingRegistry.data[landMass].registry`
- **Lehetséges okok**:
  1. `landMassBuildingRegistry` null
  2. `landMassBuildingRegistry.data` null
  3. `landMassBuildingRegistry.data[landMass]` null (IndexOutOfRange)
  4. `landMassBuildingRegistry.data[landMass].registry` null

**Hol kell javítani**: `Main.cs:755-774`

**Javítási javaslatok**:
1. **NULL check hozzáadása** minden reflection művelet után:
   ```csharp
   var landMassBuildingRegistry = __instance.GetType()...GetValue(__instance) as ArrayExt<...>;
   if (landMassBuildingRegistry == null) {
       Main.helper.Log("ERROR: landMassBuildingRegistry is null!");
       return false;
   }
   ```

2. **Array méret ellenőrzés**:
   ```csharp
   if (landMass >= landMassBuildingRegistry.data.Length) {
       Main.helper.Log($"ERROR: landMass={landMass} >= array length={landMassBuildingRegistry.data.Length}");
       return false;
   }
   ```

3. **Registry inicializálás ellenőrzés**:
   ```csharp
   if (landMassBuildingRegistry.data[landMass] == null ||
       landMassBuildingRegistry.data[landMass].registry == null) {
       // Inicializálni vagy hibát logolni
   }
   ```

**Kapcsolódó hiba**: Ez a hiba okozza a WorldPlace IndexOutOfRangeException-t is (lásd lent)

---

### 3. KRITIKUS: IndexOutOfRangeException WorldPlace-ben

**Fájl**: `Packets/Game/GameWorld/WorldPlace.cs`
**Hiba helye**: `WorldPlace.cs:167-168`
**Érintett kód**:
```csharp
// WorldPlace.cs:113
player.inst.AddBuilding(building); // <-- Meghívja PlayerAddBuildingHook-ot (ami crash-el)

// WorldPlace.cs:167-168
player.inst.LandMassNames[building.LandMass()] = player.kingdomName; // <-- CRASH ITT
Player.inst.LandMassNames[building.LandMass()] = player.kingdomName;
```

**Probléma részletesen**:
- A README szerint: **9 IndexOutOfRangeException** - "Index was outside the bounds of the array"
- **Ok-okozati lánc**:
  1. `WorldPlace.PlaceBuilding()` hívja `player.inst.AddBuilding(building)` (line 113)
  2. Ez triggereli a `PlayerAddBuildingHook.Prefix` metódust
  3. A hook crash-el NullReferenceException-nel (fenti #2 hiba)
  4. A try-catch elkapja (Main.cs:779-786), DE a building NEM kerül helyesen hozzáadásra
  5. A `WorldPlace.cs` folytatódik és megpróbálja indexelni: `LandMassNames[building.LandMass()]`
  6. **HA** a `LandMassNames` tömb nem inicializálva vagy túl kicsi → **IndexOutOfRangeException**

**Hol kell javítani**:
1. **Elsődleges**: `Main.cs:764` - Javítani a PlayerAddBuildingHook-ot (lásd #2)
2. **Másodlagos**: `WorldPlace.cs:167-168` - Védekező kód:
   ```csharp
   int landMass = building.LandMass();

   // Biztosítani hogy a LandMassNames tömb elég nagy
   while (player.inst.LandMassNames.Count <= landMass) {
       player.inst.LandMassNames.Add("");
   }
   while (Player.inst.LandMassNames.Count <= landMass) {
       Player.inst.LandMassNames.Add("");
   }

   player.inst.LandMassNames[landMass] = player.kingdomName;
   Player.inst.LandMassNames[landMass] = player.kingdomName;
   ```

**Miért kritikus**: Ez a hiba miatt **55/56 épület NEM kerül elhelyezésre** a kliensnél!

---

### Összefüggések

A három hiba összefügg:

```
1. Host menübe lép
   → Server NEM áll le (#1 hiba)
   → Server tovább fogad packeteket

2. Kliens épületet helyez
   → WorldPlace packet érkezik
   → PlaceBuilding() meghívódik
   → AddBuilding() triggereli PlayerAddBuildingHook-ot
   → Hook crash-el NullReferenceException (#2 hiba)
   → Building nem adódik hozzá helyesen

3. WorldPlace folytatódik
   → LandMassNames[landMass] indexelés
   → IndexOutOfRangeException (#3 hiba)
   → Épület NEM jelenik meg
```

**Eredmény**: 98% fail rate az épület elhelyezésben!

---

### Javítási prioritás

1. **#2 - PlayerAddBuildingHook** (LEGFONTOSABB) - Ez okozza a cascade failure-t
2. **#3 - WorldPlace IndexOutOfRange** - Védekező kód hozzáadása
3. **#1 - Server leállítás** - UX javítás, erőforrás kezelés

### Következő lépések

1. Ellenőrizni, hogy a `PlayerAddBuildingHook` null-ellenőrzései végig lefutnak, mielőtt az adatokat használjuk.
2. Biztosítani, hogy a `Server.Stop()` meghívódik, amikor a játékos visszalép a menübe (`TransitionToHook`).
3. Bővíteni a `WorldPlace` és a `LandMassNames` tömbök védelmét, hogy ne okozzon indexhiba az épület-telepítés során.
