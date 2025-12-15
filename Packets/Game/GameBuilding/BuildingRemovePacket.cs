using System;
using System.Reflection;
using UnityEngine;

namespace KCM.Packets.Game.GameBuilding
{
    public class BuildingRemovePacket : Packet
    {
        public override ushort packetId => (ushort)KCM.Enums.Packets.BuildingRemove;

        // Flag to prevent infinite loop when removing buildings from packet
        public static bool isProcessingPacket = false;

        public Guid guid { get; set; }

        public override void HandlePacketClient()
        {
            if (clientId == KCClient.client.Id) return;

            Main.helper.Log($"Received building remove packet for guid {guid} from {player.name}");

            // Try to find the building in the player who owns it
            Building building = player.inst.GetBuilding(guid);

            if (building == null)
            {
                // Try to find it in any player's buildings
                foreach (var kcp in Main.kCPlayers.Values)
                {
                    building = kcp.inst.GetBuilding(guid);
                    if (building != null) break;
                }
            }

            if (building == null)
            {
                Main.helper.Log($"Building with guid {guid} not found on client, may already be removed.");
                return;
            }

            try
            {
                Main.helper.Log($"Removing building {building.UniqueName} at {building.transform.position}");

                // Set flag to prevent sending packet back
                isProcessingPacket = true;

                // Set Player.inst to the correct player for this building
                // This ensures the removal modifies the correct player's job lists
                Player originalPlayer = Player.inst;
                Player correctPlayer = Main.GetPlayerByBuilding(building);
                if (correctPlayer != null)
                {
                    Player.inst = correctPlayer;
                }

                // Use reflection to call the Remove method from the game assembly
                MethodInfo removeMethod = typeof(Building).GetMethod("Remove", BindingFlags.Public | BindingFlags.Instance);
                if (removeMethod != null)
                {
                    removeMethod.Invoke(building, null);
                }
                else
                {
                    // Fallback: destroy the building GameObject directly
                    Main.helper.Log("Remove method not found, using Destroy fallback");
                    building.destroyedWhileInPlay = true;
                    UnityEngine.Object.Destroy(building.gameObject);
                }

                // Restore original Player.inst
                Player.inst = originalPlayer;
                isProcessingPacket = false;
            }
            catch (Exception e)
            {
                isProcessingPacket = false;
                Main.helper.Log($"Error removing building: {e.Message}");
                Main.helper.Log(e.StackTrace);
            }
        }

        public override void HandlePacketServer()
        {
            // Forward the remove packet to all other clients
            SendToAll(clientId);
        }
    }
}


