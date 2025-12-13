using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                        // Restart villager AI to ensure they continue working
                        for (int i = 0; i < Villager.villagers.Count; i++)
                        {
                            Villager v = Villager.villagers.data[i];
                            if (v != null && v.brain != null)
                            {
                                v.brain.Restart();
                            }
                        }
                        Main.helper.Log($"AI systems restarted for speed change to {speed}");
                    }
                    catch (Exception e)
                    {
                        Main.helper.Log("Error restarting AI on speed change: " + e.Message);
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
            // Server doesn't need to handle this packet
        }
    }
}
