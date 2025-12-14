using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Enums
{
    public enum Packets
    {
        ClientConnected = 25,
        PlayerList = 26,
        ChatSystemMessage = 27,
        ChatMessage = 28,
        ServerSettings = 29,
        PlayerReady = 30,
        PlayerBanner = 31,
        KingdomName = 32,
        StartGame = 33,
        WorldSeed = 34,
        Building = 50,
        BuildingOnPlacement = 51,
        World = 70,
        WorldPlace = 71,
        FellTree = 72,
        ShakeTree = 73,
        GrowTree = 74,
        UpdateConstruction = 75,
        SetSpeed = 76,
        CompleteBuild = 77,
        WorldPlaceBatch = 78,
        ChangeWeather = 79,
        ShowModal = 80,
        ServerHandshake = 81,
        SpawnSiegeDragon = 82,
        SpawnMamaDragon = 83,
        SpawnBabyDragon = 84,
        SaveTransferPacket = 85,
        UpdateState = 86,
        BuildingStatePacket = 87,
        AddVillager = 88,
        SetupInitialWorkers = 89,
        VillagerTeleportTo = 90,
        PlaceKeepRandomly = 91
    }
}
