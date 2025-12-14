using KCM.Attributes;
using Riptide;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KCM
{
    public class KCPlayer
    {
        [Transmittable]
        public ushort id;
        [Transmittable]
        public string name;

        public string steamId;


        [Transmittable]
        public string kingdomName;
        [Transmittable]
        public int banner = 0;
        [Transmittable]
        public bool ready = false;

        public Player inst;
        public GameObject gameObject;


        public KCPlayer(string name, ushort id, string steamId)
        {
            if (id != KCClient.client.Id)
            {
                gameObject = new GameObject($"Client Player ({id} {name})");

                inst = gameObject.AddComponent<Player>();
                var irrigation = gameObject.AddComponent<IrrigationManager>();
                var lmo = gameObject.AddComponent<LandmassOwner>();

                inst.irrigation = irrigation;

                inst.PlayerLandmassOwner = lmo;
                inst.PlayerLandmassOwner.teamId = id * 10 + 2;

                inst.hazardPayWarmup = new Timer(5f);
                inst.hazardPayWarmup.Enabled = false;

                bool[] flagsArr = new bool[38];
                for (int i = 0; i < flagsArr.Length; i++)
                    flagsArr[i] = true;

                var field = typeof(Player).GetField("defaultEnabledFlags", BindingFlags.NonPublic | BindingFlags.Instance);
                field.SetValue(inst, flagsArr);



                Player oldPlayer = Player.inst;
                Player.inst = inst;

                inst.Reset();

                Player.inst = oldPlayer;
            }
            else
            {

                gameObject = Player.inst.gameObject;
                inst = Player.inst;
            }

            this.name = name;
            this.id = id;
            this.steamId = steamId;
            this.kingdomName = " ";
        }

        public KCPlayer(ushort id, Player player)
        {
            gameObject = player.gameObject;
            inst = player;

            this.id = id;
        }
    }
}
