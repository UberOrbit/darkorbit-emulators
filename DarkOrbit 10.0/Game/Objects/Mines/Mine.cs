﻿using Ow.Game.Movements;
using Ow.Game.Ticks;
using Ow.Managers;
using Ow.Net.netty.commands;
using Ow.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ow.Game.Objects.Mines
{
    abstract class Mine : Tick
    {
        public const int RANGE = 150; //değiştirilebilir
        public const int ACTIVATION_TIME = 3; //değiştirilebilir

        public int TickId { get; set; }
        public int MineTypeId { get; set; }
        public string Hash { get; set; }
        public Player Player { get; set; }
        public Spacemap Spacemap { get; set; }
        public Position Position { get; set; }
        public DateTime activationTime = new DateTime();
        public bool Lance { get; set; }
        public bool Pulse { get; set; }
        public bool Active = true;

        public Mine(Player player, Spacemap spacemap, Position position, int mineTypeId, bool lance = false, bool pulse = false)
        {
            Player = player;
            Spacemap = spacemap;
            Position = position;
            MineTypeId = mineTypeId;
            Lance = lance;
            Pulse = pulse;
            Hash = Randoms.GenerateHash(16);
            Spacemap.Mines.TryAdd(Hash, this);
            GameManager.SendCommandToMap(Spacemap.Id, GetMineCreateCommand());

            var tickId = -1;
            Program.TickManager.AddTick(this, out tickId);
            TickId = tickId;

            activationTime = DateTime.Now;
        }

        public abstract void Explode();

        public void Tick()
        {
            if (Active)
            {
                if (activationTime.AddSeconds(ACTIVATION_TIME) < DateTime.Now)
                {
                    foreach (var player in Spacemap.Characters.Values)
                    {
                        if (player.Position.DistanceTo(Position) < RANGE)
                        {
                            Remove();
                            Explode();
                        }
                    }
                }
            }
        }

        public void Remove()
        {
            Active = false;
            var mine = this;
            Spacemap.Mines.TryRemove(Hash, out mine);
            GameManager.SendPacketToMap(Spacemap.Id, $"0|n|MIN|{Hash}");
            Program.TickManager.RemoveTick(this);
        }

        public byte[] GetMineCreateCommand()
        {
            return MineCreateCommand.write(Hash, Pulse, false, MineTypeId, Position.Y, Position.X);
        }
    }
}