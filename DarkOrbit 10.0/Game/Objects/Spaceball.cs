﻿using Ow.Game.Movements;
using Ow.Game.Objects.Collectables;
using Ow.Managers;
using Ow.Net.netty.commands;
using Ow.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ow.Game.Objects
{
    class Spaceball : Character
    {
        private int SelectedFactionId = 0;

        public int Mmo = 0;
        public int Eic = 0;
        public int Vru = 0;

        public static Position CurrentPosition = new Position(21000, 13200);
        private int MMODamage = 0;
        public Position MMOPosition = new Position(7000, 13500);
        private int EICDamage = 0;
        public Position EICPosition = new Position(28000, 1200);
        private int VRUDamage = 0;
        public Position VRUPosition = new Position(28000, 25000);

        public DateTime LastDamagedTime = new DateTime();

        public Spaceball(int id, int typeId) : base(id, GameManager.GetShip(typeId).Name, 0, GameManager.GetShip(typeId), CurrentPosition, GameManager.GetSpacemap(16), null)
        {
            Speed = 100;
        }

        public new void Tick()
        {
            if (EventManager.Spaceball.Active)
            {
                CheckDamage();
                CheckSpeed();
                if ((Position.DistanceTo(MMOPosition) <= 2000) || (Position.DistanceTo(EICPosition) <= 2000) || (Position.DistanceTo(VRUPosition) <= 2000))
                    SendReward();
            }
        }

        public void CheckDamage()
        {
            if (LastDamagedTime.AddSeconds(10) < DateTime.Now && Position != CurrentPosition)
            {
                GameManager.SendPacketToAll("0|n|sss|1|0");
                GameManager.SendPacketToAll("0|n|sss|2|0");
                GameManager.SendPacketToAll("0|n|sss|3|0");
                ReInitialization();
                Movement.Move(this, CurrentPosition);
            }
        }

        public void CheckSpeed()
        {
            switch (SelectedFactionId)
            {
                case 1:
                    if (MMODamage < 500000)
                    {
                        GameManager.SendPacketToAll("0|n|sss|1|1");
                    }
                    else if (MMODamage < 1000000 && MMODamage > 500000)
                    {
                        GameManager.SendPacketToAll("0|n|sss|1|2");
                        Speed = 150;
                    }
                    else if (MMODamage < 10000000 && MMODamage > 1000000)
                    {
                        GameManager.SendPacketToAll("0|n|sss|1|3");
                        Speed = 200;
                    }
                    break;
                case 2:
                    if (EICDamage < 500000)
                    {
                        GameManager.SendPacketToAll("0|n|sss|2|1");
                    }
                    else if (EICDamage < 1000000 && EICDamage > 500000)
                    {
                        GameManager.SendPacketToAll("0|n|sss|2|2");
                        Speed = 150;
                    }
                    else if (EICDamage < 10000000 && EICDamage > 1000000)
                    {
                        GameManager.SendPacketToAll("0|n|sss|2|3");
                        Speed = 200;
                    }
                    break;
                case 3:
                    if (VRUDamage < 500000)
                    {
                        GameManager.SendPacketToAll("0|n|sss|3|1");
                    }
                    else if (VRUDamage < 1000000 && VRUDamage > 500000)
                    {
                        GameManager.SendPacketToAll("0|n|sss|3|2");
                        Speed = 150;
                    }
                    else if (VRUDamage < 10000000 && VRUDamage > 1000000)
                    {
                        GameManager.SendPacketToAll("0|n|sss|3|3");
                        Speed = 200;
                    }
                    break;
            }
        }

        public void SendReward()
        {
            switch (SelectedFactionId)
            {
                case 1:
                    Mmo++;
                    GameManager.SendPacketToAll($"0|A|STM|msg_spaceball_company_scored|%COMPANY%|MMO");
                    break;
                case 2:
                    Eic++;
                    GameManager.SendPacketToAll($"0|A|STM|msg_spaceball_company_scored|%COMPANY%|EIC");
                    break;
                case 3:
                    Vru++;
                    GameManager.SendPacketToAll($"0|A|STM|msg_spaceball_company_scored|%COMPANY%|VRU");
                    break;
            }

            GameManager.SendPacketToAll($"0|n|ssi|{Mmo}|{Eic}|{Vru}");

            for (int i = 0; i <= 20; i++)
                new CargoBox(AssetTypeModule.BOXTYPE_FROM_SHIP, Position.Random(Spacemap, Position.X - 1000, Position.X + 500, Position.Y - 1000, Position.Y + 500), Spacemap, false, true);

            Respawn();
        }

        public void Respawn()
        {
            Spacemap.RemoveCharacter(this);
            SetPosition(CurrentPosition);
            ReInitialization();
            Spacemap.AddCharacter(this);
        }

        public void ReInitialization()
        {
            Speed = 100;
            MMODamage = 0;
            EICDamage = 0;
            VRUDamage = 0;
            SelectedFactionId = 0;
        }

        public void AddDamage(Player player, int damage)
        {
            switch (player.FactionId)
            {
                case 1:
                    MMODamage += damage;
                    break;
                case 2:
                    EICDamage += damage;
                    break;
                case 3:
                    VRUDamage += damage;
                    break;
            }
            LastDamagedTime = DateTime.Now;
            Move();
        }

        public void Move()
        {
            if (MMODamage > EICDamage && MMODamage > VRUDamage)
            {
                SelectedFactionId = 1;
                Movement.Move(this, MMOPosition);
            }
            else if (EICDamage > MMODamage && EICDamage > VRUDamage)
            {
                SelectedFactionId = 2;
                Movement.Move(this, EICPosition);
            }
            else if (VRUDamage > MMODamage && VRUDamage > EICDamage)
            {
                SelectedFactionId = 3;
                Movement.Move(this, VRUPosition);
            }
            else
            {
                SelectedFactionId = 0;
                Movement.Move(this, CurrentPosition);
            }
        }

        public byte[] GetShipCreateCommand()
        {
            return ShipCreateCommand.write(
                Id,
                Convert.ToString(Ship.Id),
                3,
                "",
                Ship.Name,
                Position.X,
                Position.Y,
                FactionId,
                0,
                0,
                true,
                new ClanRelationModule(ClanRelationModule.AT_WAR),
                0,
                false,
                true,
                false,
                ClanRelationModule.AT_WAR,
                ClanRelationModule.AT_WAR,
                new List<VisualModifierCommand>(),
                new class_11d(class_11d.DEFAULT));
        }
    }
}
