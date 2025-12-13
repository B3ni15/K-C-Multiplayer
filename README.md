# KCM (Kingdoms and Castles Multiplayer) – javított verzió

Ez a repo egy *Kingdoms and Castles* multiplayer mod forrását tartalmazza, pár stabilitási/szinkron hibára célzott javításokkal.

## Mi volt a gond?

A mellékelt log (`output.txt`) alapján több tipikus hiba okozta a szerver indításkori/ lobby-beli szétesést:

- `NullReferenceException` a lobby player UI frissítésében (`PlayerEntryScript.SetValues`)
- duplikált SteamID miatti `ArgumentException: same key already added` a handshake során
- csomagkezelés közben `KeyNotFoundException` / `NullReferenceException` (hiányzó `clientId -> steamId` map, race/állapot problémák)

## Mit javít ez a verzió?

- Lobby UI frissítés stabilizálása (null/állapot ellenőrzések, helyes inicializálási sorrend)
- Handshake alatt a player-regisztráció ütközésmentessé tétele + `clientSteamIds` beállítása
- Packet oldali player lookup biztonságossá tétele (ne dobjon kivételt hiányzó map esetén)
- `PlayerReady` packet: ha nincs player, ne crasheljen
- Szerver oldalon a csatlakozáskor a játékos regisztráció/map frissítése
- Kilépés/clear esetén `clientSteamIds` takarítása, hogy ne maradjanak “árva” bejegyzések

Érintett fájlok (főbb pontok):

- `ServerLobby/PlayerEntryScript.cs`
- `Packets/Network/ServerHandshake.cs`
- `Packets/Network/ClientConnected.cs`
- `Packets/Packet.cs`
- `Packets/Lobby/PlayerReady.cs`
- `Packets/Lobby/PlayerList.cs`
- `KCServer.cs`
- `Packets/Handlers/LobbyHandler.cs`
- `RiptideSteamTransport/LobbyManager.cs`

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

## Repo higiénia

- A `.gitignore` kizárja a logokat (`output*.txt`) és tipikus IDE/build artifactokat, hogy ne kerüljenek fel GitHubra.
