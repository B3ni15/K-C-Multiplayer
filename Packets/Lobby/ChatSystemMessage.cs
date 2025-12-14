using KCM.Packets.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Lobby
{
    public class ChatSystemMessage : Packet
    {
        public override ushort packetId => (int)Enums.Packets.ChatSystemMessage;

        public string Message { get; set; }

        public override void HandlePacketServer()
        {
            //LobbyHandler.AddSystemMessage(Message);
        }

        public override void HandlePacketClient()
        {
            LobbyHandler.AddSystemMessage(Message);
        }
    }
}
