﻿using Ow.Game.Movements;
using Ow.Managers;
using Ow.Net.netty;
using Ow.Net.netty.commands;
using Ow.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ow.Game.Objects.Collectables
{
    class BonusBox : Collectable
    {
        public BonusBox(int collectableId, Position position, Spacemap spacemap, bool respawnable, Player toPlayer = null) : base(collectableId, position, spacemap, respawnable, toPlayer) { }

        public override void Reward(Player player)
        {
            var luck = Randoms.random.Next(0,2);

            if (luck == 1)
            {
                var uridium = Randoms.random.Next(0, 150);
                player.Data.uridium += uridium;
                player.SendPacket("0|LM|ST|URI|" + uridium + "|" + player.Data.uridium);
            }
            else
            {
                var credits = Randoms.random.Next(0, 1000);
                player.Data.credits += credits;
                player.SendPacket("0|LM|ST|CRE|" + credits + "|" + player.Data.credits);
            }

            QueryManager.SavePlayer.Information(player);
        }
    }
}
