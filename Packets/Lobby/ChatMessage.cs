using KCM.Packets.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Lobby
{
    public class ChatMessage : Packet
    {
        public override ushort packetId => (int)Enums.Packets.ChatMessage;

        public string PlayerName { get; set; }
        public string Message { get; set; }

        public override void HandlePacketServer()
        {
            //Main.helper.Log("Received chat packet: " + Message);

            //SendToAll(KCClient.client.Id);
            //LobbyHandler.AddChatMessage(clientId, PlayerName, Message);
        }

        public override void HandlePacketClient()
        {
            Main.helper.Log("Received chat packet: " + Message);

            LobbyHandler.AddChatMessage(clientId, PlayerName, Message);
        }
    }
}
