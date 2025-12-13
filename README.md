# KCM (Kingdoms and Castles Multiplayer)

Ez a repó egy *Kingdoms and Castles* multiplayer mod forrása. A mod Steam lobby + Riptide alapú hálózattal próbálja a világot és a játékosok akcióit több kliens között szinkronban tartani.

Ha a `output.txt` logban `Compilation failed` szerepel, akkor a mod **nem töltődött be**, és semmi nem fog szinkronizálódni (ilyenkor tipikusan C# szintaxis / runtime-kompatibilitási hiba van a forrásban).

## Mit szinkronizál a mod? (jelenlegi állapot)

**Lobby / kapcsolat**
- Játékos csatlakozás/leválás, player lista, ready állapot.
- Szerver beállítások (név, max players, seed, world opciók).
- Chat és rendszerüzenetek.

**Világ indítás**
- World seed szétküldése és world generálás a klienseken.
- (Beállítástól függően) keep elhelyezés csomagból.

**Gameplay alap**
- Épület lerakás események (alap meta: `uniqueName`, `guid`, pozíció/rotáció).
- Épület állapot frissítések “snapshot” jelleggel (`BuildingStatePacket`): built/placed, constructionProgress, life, stb.
- Néhány globális esemény: idősebesség változtatás, időjárás váltás, fa kivágás (repo verziótól függően).
- Host oldalon periodikus *resource snapshot* korrigálás (ha drift/desync van, visszahúzza a klienst).

**Mentés betöltés (host → kliens)**
- Host oldalon a mentés byte-ok chunkolva kerülnek kiküldésre (`SaveTransferPacket`).
- Kliens oldalon érkezés után `LoadSave.Load()` + `MultiplayerSaveContainer.Unpack()` fut.
- Ha a kiválasztott mentés nem multiplayer container (vanilla mentés), a host fallback-ként átadja a normál betöltést.

## Mi nincs (még) rendesen szinkronizálva? (gyakori desync okok)

Ezek okozzák a tipikus “farm termel, de nem látszik” / “resource nem frissül” / “animáció hiányzik” jelenségeket:
- **Erőforrás-logika és szállítás**: raktárkészletek, haul/cart routing, villager “viszem/lerakom” animációk nincsenek teljes állapotban szinkronizálva.
- **Villager/job részletek**: current task, target, carried resource, pathing cache, részfeladat-állapot.
- **Field/Farm belső állapot**: growth stage, harvest queue, field regisztráció edge case-ek.
- **UI / kliens oldali state**: beragadt menük, promptok (pl. “rakd le a kezdő épületet”), lokális UI state nem hálózati adat.
- **AI brains / nem-player rendszerek**: részben vagy egyáltalán nincs “szerver az igazság” modell.

## Mit érdemes még hozzáadni? (roadmap)

Ha cél a stabil “load utáni sync” és kevesebb vizuális desync:
- **Resource szinkron**: raktárak készlete, termelés/fogyasztás tick eredménye, szállítási queue események (event-based vagy periodikus snapshot).
- **Villager szinkron**: villager state machine + carried resource + célpont; vagy determinisztikus szerver oldali szimuláció és kliens “replay”.
- **Farm/Field szinkron**: field állapot (growth/ready/harvest), aratás események explicit hálózati üzenetként.
- **Robusztus reconnect**: kilépés egy sessionből → másik lobby csatlakozás restart nélkül (minden statikus állapot, observer, transfer state, player cache teljes resetje).
- **Debug eszközök**: desync detektor (hash/snapshot összehasonlítás), több log a load/sync pontokra.

## Telepítés

- Hostnak és **minden kliensnek ugyanaz a mod verzió** kell.
- Workshop verzió frissítés felülírhatja a módosításokat. Ajánlott:
  - kimásolni a modot a játék `...\\KingdomsAndCastles_Data\\mods\\` mappájába egy külön névvel,
  - és a mod menüben kikapcsolni a Workshop verziót.
- Változtatások után **teljes játék újraindítás** javasolt.

## Hibaelhárítás

**Log helye:** a mod mappájában gyakran van `output.txt`.

Nézd ezeket a kulcssorokat:
- `Compilation failed` → a mod nem fordult le, nincs multiplayer.
- `Save Transfer started/complete` → mentés átküldés/betöltés állapota.
- `Error loading save` / `LoadError` → sérült/rossz típusú mentés, vagy verzió eltérés.

Bug reporthoz küldd el:
- a hiba környéki 50–100 sort a `output.txt`-ből,
- host/kliens szerep, játék verzió, mod verzió,
- új világban vagy mentés betöltés után jelentkezik-e.

## Fejlesztés

Repo-szabályok és szerkezet: `AGENTS.md`.

### Gyors resync

A lobby chatben írd be: `/resync` – a kliens kér egy resync-et a hosttól (resource + building + villager “teleport” snapshot).
