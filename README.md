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

## README.md Dokumentációs Hibák

> **FIGYELEM**: Ez a szekció a README.md fájlban található dokumentációs hibákat listázza.
> Ezek a hibák **NEM** lettek javítva, csak dokumentálva vannak.

### Nyelvtani Hibák - Hiányzó Ékezetek

| Hiba helye | Jelenlegi szöveg | Helyes forma | Javítás módja |
|------------|------------------|--------------|---------------|
| **Line 3** | "Ismert hibak" | "Ismert hibák" | 'a' → 'á' |
| **Line 7** | "Megjegyzes" | "Megjegyzés" | 'e' (első) → 'e', 'e' (második) → 'é' |
| **Line 41** | "Definiciok" | "Definíciók" | 'i' (második) → 'í', 'o' → 'ó' |
| **Line 48** | "Analisis" | "Analízis" | 'i' (második) → 'í' |
| **Line 59** | "Idovonal" | "Idővonal" | 'o' → 'ő' |
| **Line 88** | "duplikacio" | "duplikáció" | 'a' → 'á', 'o' → 'ó' |

**Hol kell javítani**: README.md fájl, a fenti sorokon
**Miért fontos**: A magyar helyesírási szabályok betartása, professzionális megjelenés

### Strukturális Hiányosságok

| Probléma | Leírás | Javasolt megoldás |
|----------|--------|-------------------|
| **Hiányzó bevezető** | A dokumentum azonnal a bug listával kezdődik | Adj hozzá egy "## Áttekintés" szekciót a fájl elejére, amely elmagyarázza mi ez a dokumentum és mire való |
| **Hiányzó kontribúciós útmutató** | Nincs információ arról, hogyan lehet új bugokat jelenteni | Adj hozzá egy "## Hogyan jelentsek új bugot?" szekciót utasításokkal |
| **Verzió információ hiánya** | A "Recent Changes" szekcióban nincs verzió szám | Fontold meg verzió számok hozzáadását (pl. v0.1.0) minden változtatáshoz |

**Hol kell javítani**: README.md struktúra és szervezés
**Miért fontos**: Könnyebb navigáció, jobb felhasználói élmény

### Konzisztencia Problémák

| Probléma | Példa | Megjegyzés |
|----------|-------|------------|
| **Magyar-angol keverés** | "output.txt-bol", "NullReferenceException" | Technikai kifejezések angolul vannak, ami elfogadható, de a magyar toldalékok (-bol, -ben) inkonzisztensek |
| **Táblázat formázás** | Egyes táblázatok szélesebbek mint mások | Egységesítsd a táblázat oszlopszélességeket |

**Hol kell javítani**: README.md szövegformázás
**Miért fontos**: Egységes megjelenés, könnyebb olvashatóság

---

### Javítási Prioritások

1. **MAGAS**: Ékezetek javítása (line 3, 7, 41, 48, 59, 88)
2. **KÖZEPES**: Bevezető szekció hozzáadása
3. **ALACSONY**: Verzió információk és kontribúciós útmutató hozzáadása
