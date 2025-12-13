# KCM (Kingdoms and Castles Multiplayer) – javított verzió

Ez a repo egy *Kingdoms and Castles* multiplayer mod forrását tartalmazza, pár stabilitási/szinkron hibára célzott javításokkal.

## Mi volt a gond?

A mellékelt log (`output.txt`) alapján több tipikus hiba okozta a szerver indításkori/ lobby-beli szétesést:

- `NullReferenceException` a lobby player UI frissítésében (`PlayerEntryScript.SetValues`)
- duplikált SteamID miatti `ArgumentException: same key already added` a handshake során
- csomagkezelés közben `KeyNotFoundException` / `NullReferenceException` (hiányzó `clientId -> steamId` map, race/állapot problémák)
- Save/load utani desync: a kliens oldali "Client Player" Reset nem futott mentés betöltése közben (loading flag miatt), így remote player állapot (registry/field/worker) beragadhatott; ez okozhatott "nem látni az aratást/elszállítást", hiányzó épület/resource state, és UI anomáliákat.
- Save/load utani "kezdo keep" kerese: betöltéskor a keep beallitasa túl szigorúan teamId-hez volt kötve, ezért előfordult hogy a kliensnél nem lett beállítva a már létező keep, és újra kérte a játék a kezdő lerakást.
- Többszöri load után romló sync: a statikus StateObserver/observer GameObject-ok nem lettek kitakarítva új load előtt, így régi világ/objektum referenciák maradhattak bent és rossz state frissítéseket küldhettek.

## Mit javít ez a verzió?

- Lobby UI frissítés stabilizálása (null/állapot ellenőrzések, helyes inicializálási sorrend)
- Handshake alatt a player-regisztráció ütközésmentessé tétele + `clientSteamIds` beállítása
- Packet oldali player lookup biztonságossá tétele (ne dobjon kivételt hiányzó map esetén)
- `PlayerReady` packet: ha nincs player, ne crasheljen
- Szerver oldalon a csatlakozáskor a játékos regisztráció/map frissítése
- Kilépés/clear esetén `clientSteamIds` takarítása, hogy ne maradjanak “árva” bejegyzések
- Épületek `Player.inst` referenciáinak patch-elése már nem csak a base `Building` osztályban fut, hanem az összes `Building`-ből származó típusban (pl. farmok speciális logikája)
- `FieldSystem` `Player.inst` referenciáinak patch-elése (farm/termés állapotkezelés több helyen erre támaszkodik)
- Mentés betöltéskor a `ProcessBuilding` útvonal kiegészítése `World.inst.PlaceFromLoad(...)` + `UnpackStage2(...)` hívásokkal (különösen fontos a “világba helyezés” mellékhatásai miatt, pl. farm/field regisztráció)
- Save transfer kliens oldalon robusztusabb inicializálás/reset (ne ragadjon be a statikus állapot több betöltés után, plusz bounds/null ellenőrzések)
- Fix: save/load közben is lefut a remote "Client Player" Reset (nem csak új világ generálásnál), hogy a player alrendszerek mindig tiszta alapból induljanak.
- Fix: keep detektálás betöltéskor (ne teamId egyezésen múljon), így nem kéri a játék a kezdő keep lerakását, ha már létezik.
- Fix: új load előtt StateObserver takarítás (save transfer kezdetén, host oldali `LoadAtPath` elején, lobby elhagyásakor), hogy ne maradjanak beragadt observer objektumok.
- Kompatibilitási fix: `World.inst.liverySets` lista esetén `.Count` használata `.Length` helyett (különben `Compilation failed` lehet egyes verziókon)
- Hálózati stabilitás: `BuildingStatePacket` most `Unreliable` módban megy (state jellegű csomagoknál jobb, ha a legfrissebb állapot érkezik meg és nem torlódik fel a megbízható sor)
- Mentés-szinkron stabilitás: szerver oldalon a save chunkok már nem egy nagy for-ciklusban mennek ki, hanem ütemezve (csökkenti a “The gap between received sequence IDs…” / “Poor connection” diszkonnekteket)
- Kapcsolat tuning: kliens és szerver oldalon emelt `MaxSendAttempts`, és tiltott minőség-alapú auto-disconnect (különösen save transfer közben volt agresszív)
- Fix: a `sendMode` csak a Riptide üzenetküldési mód kiválasztására szolgál, nem kerül szériázásra; az enum csomagmezők szériázása/deszériázása robusztusabb lett (különben csatlakozáskor packet-parszolás szétesett)

Érintett fájlok (főbb pontok):

- `ServerLobby/PlayerEntryScript.cs`
- `Packets/Network/ServerHandshake.cs`
- `Packets/Network/ClientConnected.cs`
- `Packets/Packet.cs`
- `Packets/Lobby/PlayerReady.cs`
- `Packets/Lobby/PlayerList.cs`
- `Packets/Lobby/SaveTransferPacket.cs`
- `KCServer.cs`
- `Packets/Handlers/LobbyHandler.cs`
- `RiptideSteamTransport/LobbyManager.cs`
- `Packets/Handlers/PacketHandler.cs`
- `Packets/State/BuildingStatePacket.cs`
- `StateManagement/Observers/StateObserver.cs`
- `Main.cs`

## Telepítés / használat

Fontos: a hostnak és **minden kliensnek ugyanaz a verzió** kell, különben továbbra is lehetnek sync problémák.

Megjegyzés: a mod menüben a piros `Restart to load` üzenet azt jelenti, hogy a mod engedélyezése/letöltése közben változott valami, és **teljes játék-újraindítás** kell, hogy betöltődjön.

1. Tedd a mod mappáját a játék `mods` könyvtárába (vagy használd Workshopból, de ott egy frissítés felülírhatja a javításokat).
2. Indítsd újra teljesen a játékot.
3. Hostolj/ csatlakozz, majd ellenőrizd, hogy a lobby és a szerver indítás stabil marad.

Workshop módosításokhoz ajánlott: másold ki a Workshop mappából egy **külön névvel** a `...\\KingdomsAndCastles_Data\\mods\\` alá, és a mod menüben kapcsold ki a Workshop verziót, hogy Steam frissítés ne írja felül.

## Hibaelhárítás

Ha továbbra is hibát látsz:

- Küldd el a `output.txt` releváns részét (a hiba előtti/utáni stack trace-t), vagy írd le a pontos üzenetet.
- Írd meg, hogy: hostoltál-e, hány kliens csatlakozott, és mindenkin ugyanaz a mod-verzió van-e.
- Teszthez kapcsold ki a többi modot (különösen azokat, amik Harmony patch-elnek). A logban egy `Profiler` mod (`Profiler.ProfilerMod`) is hibázott, ez meg tudja zavarni a betöltést.
- Farm/termés desync esetén írd meg: host vagy kliens oldalon nem látszik-e a termés, új világban történik-e vagy save betöltés után, és hány perc játék után jön elő.

## Repo higiénia

- A `.gitignore` kizárja a logokat (`output*.txt`) és tipikus IDE/build artifactokat, hogy ne kerüljenek fel GitHubra.
