using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets
{
    public class ShowModal : Packet
    {
        public override ushort packetId => (int)Enums.Packets.ShowModal;

        public string title { get; set; }
        public string message { get; set; }

        public override void HandlePacketClient()
        {
            Main.helper.Log("Opening Modal");
            Main.helper.Log("Title: " + title);
            Main.helper.Log("Message: " + message);

            ModalManager.ShowModal(title, message);
        }

        public override void HandlePacketServer()
        {
            //throw new NotImplementedException();
        }
    }
}
