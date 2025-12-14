using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KCM.Packets.Game
{
    public class SetSpeed : Packet
    {
        public override ushort packetId => (int)Enums.Packets.SetSpeed;

        public int speed { get; set; }
        public bool isPaused { get; set; }

        public override void HandlePacketClient()
        {
            if (clientId == KCClient.client.Id) // Prevent speed softlock
                return;

            try
            {
                // Apply speed setting
                SpeedControlUI.inst.SetSpeed(speed);
                
                // Handle pause/unpause state
                if (isPaused && Time.timeScale > 0)
                {
                    // Game should be paused
                    Time.timeScale = 0f;
                    Main.helper.Log("Game paused via network sync");
                }
                else if (!isPaused && Time.timeScale == 0)
                {
                    // Game should be unpaused - restore speed
                    Time.timeScale = 1f;
                    SpeedControlUI.inst.SetSpeed(speed);
                    Main.helper.Log("Game unpaused via network sync");
                }
                
                // Force AI system update when speed changes
                if (speed > 0)
                {
                    try
                    {
                        // Force villager system refresh to ensure they continue working
                        if (VillagerSystem.inst != null)
                        {
                            // Use reflection to call any refresh methods on VillagerSystem
                            var villagerSystemType = typeof(VillagerSystem);
                            var refreshMethods = villagerSystemType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                .Where(m => m.Name.Contains("Refresh") || m.Name.Contains("Update") || m.Name.Contains("Restart"));
                            
                            foreach (var method in refreshMethods)
                            {
                                if (method.GetParameters().Length == 0)
                                {
                                    try
                                    {
                                        method.Invoke(VillagerSystem.inst, null);
                                        Main.helper.Log($"Called VillagerSystem.{method.Name} for speed change");
                                    }
                                    catch { }
                                }
                            }
                        }
                        Main.helper.Log($"AI systems refreshed for speed change to {speed}");
                    }
                    catch (Exception e)
                    {
                        Main.helper.Log("Error refreshing AI on speed change: " + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Main.helper.Log("Error handling SetSpeed packet: " + e.Message);
            }
        }

        public override void HandlePacketServer()
        {
            // Server relay is handled automatically by PacketHandler unless [NoServerRelay] is used.
        }
    }
}
