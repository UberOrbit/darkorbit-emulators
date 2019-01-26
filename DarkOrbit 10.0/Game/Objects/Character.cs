﻿using Ow.Game.Clans;
using Ow.Game.Movements;
using Ow.Game.Objects.Collectables;
using Ow.Game.Objects.Players;
using Ow.Game.Objects.Players.Managers;
using Ow.Managers;
using Ow.Net.netty.commands;
using Ow.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Ow.Game.Spacemap;

namespace Ow.Game.Objects
{
    class Character : Attackable
    {
        public ConcurrentDictionary<int, Character> InRangeCharacters = new ConcurrentDictionary<int, Character>();
        public ConcurrentDictionary<int, VisualModifierCommand> VisualModifiers = new ConcurrentDictionary<int, VisualModifierCommand>();

        public string Name { get; set; }
        public override int FactionId { get; set; }
        public override Position Position { get; set; }
        public override Spacemap Spacemap { get; set; }
        public Ship Ship { get; set; }
        public Clan Clan { get; set; }
        public bool Destroyed = false;
        public bool Collecting = false;

        public override int CurrentHitPoints { get; set; }
        public override int MaxHitPoints { get; set; }
        public override int CurrentNanoHull { get; set; }
        public override int MaxNanoHull { get; set; }
        public override int CurrentShieldPoints { get; set; }
        public override int MaxShieldPoints { get; set; }
        public override double ShieldAbsorption { get; set; }
        public override double ShieldPenetration { get; set; }

        public virtual int Speed { get; set; }
        public virtual int Damage { get; set; }
        public virtual int RocketDamage { get; set; }

        public bool Moving { get; set; }
        public Position OldPosition { get; set; }
        public Position Destination { get; set; }
        public Position Direction { get; set; }
        public DateTime MovementStartTime { get; set; }
        public int MovementTime { get; set; }

        public Attackable Selected { get; set; }
        public Character SelectedCharacter => Selected as Character;

        public Character MainAttacker { get; set; }
        public ConcurrentDictionary<int, Attacker> Attackers = new ConcurrentDictionary<int, Attacker>();

        protected Character(int id, string name, int factionId, Ship ship, Position position, Spacemap spacemap, Clan clan = null) : base(id)
        {
            Name = name;
            FactionId = factionId;
            Ship = ship;
            Position = position;
            Spacemap = spacemap;
            Clan = clan;

            Moving = false;
            OldPosition = new Position(0, 0);
            Destination = position;
            Direction = new Position(0, 0);
            MovementStartTime = new DateTime();
            MovementTime = 0;

            if (clan == null)
            {
                Clan = GameManager.GetClan(0);
            }
        }

        public override void Tick()
        {
            if (!Destroyed)
            {
                if (this is Player)
                {
                    ((Player)this).Tick();
                }
                else if (this is Pet)
                {
                    ((Pet)this).Tick();
                }
                else if (this is Spaceball)
                {
                    ((Spaceball)this).Tick();
                }
            }
        }

        public void SetPosition(Position targetPosition)
        {
            Destination = targetPosition;
            Position = targetPosition;
            OldPosition = targetPosition;
            Direction = targetPosition;
            Moving = false;

            Movement.Move(this, Movement.ActualPosition(this));
        }

        public override void Destroy(Character destroyer, DestructionType destructionType)
        {
            if (this is Spaceball) return;

            var thisPlayer = this as Player;

            if (MainAttacker != null && MainAttacker is Player)
            {
                destroyer = MainAttacker;
                destructionType = DestructionType.PLAYER;
            }

            if (destructionType == DestructionType.PLAYER)
            {
                var destroyerPlayer = destroyer as Player;

                destroyerPlayer.Selected = null;
                destroyerPlayer.DisableAttack(destroyerPlayer.SettingsManager.SelectedLaser);

                if (this is Pet && (this as Pet).Owner != destroyer)
                {
                    int experience = destroyerPlayer.Ship.GetExperienceBoost(Ship.Rewards.Experience);
                    int honor = destroyerPlayer.GetHonorBoost(destroyerPlayer.Ship.GetHonorBoost(Ship.Rewards.Honor));
                    int uridium = Ship.Rewards.Uridium;

                    destroyerPlayer.Experience += experience;
                    destroyerPlayer.Honor += honor;
                    destroyerPlayer.Uridium += uridium;

                    destroyerPlayer.SendPacket("0|LM|ST|EP|" + experience + "|" + destroyerPlayer.Experience + "|" + destroyerPlayer.Level);
                    destroyerPlayer.SendPacket("0|LM|ST|HON|" + honor + "|" + destroyerPlayer.Honor);
                    destroyerPlayer.SendPacket("0|LM|ST|URI|" + uridium + "|" + destroyerPlayer.Uridium);

                    new CargoBox(AssetTypeModule.BOXTYPE_FROM_SHIP, Position, Spacemap, false, false, destroyerPlayer);
                    QueryManager.SavePlayer.Information(destroyerPlayer);
                }
            }

            Destroyed = true;

            var destroyCommand = ShipDestroyedCommand.write(Id, 0);
            SendCommandToInRangePlayers(destroyCommand);

            if (this is Player)
            {
                thisPlayer.SkillManager.DisableAllSkills();
                thisPlayer.Pet.Deactivate(true);
                thisPlayer.CurrentHitPoints = 0;
                thisPlayer.SendCommand(destroyCommand);
                thisPlayer.DisableAttack(thisPlayer.SettingsManager.SelectedLaser);
                thisPlayer.CurrentInRangePortalId = -1;
                thisPlayer.InRangeAssets.Clear();
                thisPlayer.KillScreen(destroyer, destructionType);

                if (thisPlayer.Spacemap.Id == EventManager.JackpotBattle.Spacemap.Id && EventManager.JackpotBattle.Players.ContainsKey(thisPlayer.Id))
                {
                    EventManager.JackpotBattle.Players.TryRemove(thisPlayer.Id, out thisPlayer);
                    GameManager.SendPacketToMap(EventManager.JackpotBattle.Spacemap.Id, "0|LM|ST|SLE|" + EventManager.JackpotBattle.Players.Count);
                }
            }

            Selected = null;
            InRangeCharacters.Clear();
            VisualModifiers.Clear();
            Spacemap.RemoveCharacter(this);

            if (this is Pet)
                (this as Pet).Deactivate(true, true);
        }

        public void SendPacketToInRangePlayers(string Packet)
        {
            foreach (var otherPlayers in InRangeCharacters.Values)
                if (otherPlayers is Player)
                    (otherPlayers as Player).SendPacket(Packet);
        }

        public void SendCommandToInRangePlayers(byte[] Command)
        {
            foreach (var otherPlayers in InRangeCharacters.Values)
                if (otherPlayers is Player)
                    (otherPlayers as Player).SendCommand(Command);
        }

        public event EventHandler<CharacterArgs> InRangeCharacterRemoved;
        public event EventHandler<CharacterArgs> InRangeCharacterAdded;

        public bool AddInRangeCharacter(Character character)
        {
            if (IsInRangeCharacter(character) || character.Destroyed) return false;
            if (character == this) return false;

            var success = InRangeCharacters.TryAdd(character.Id, character);

            if (success)
            {
                InRangeCharacterAdded?.Invoke(this, new CharacterArgs(character));

                short relationType = character.Clan != null && Clan != null ? Clan.GetRelation(character.Clan) : (short)0;
                bool sameClan = character.Clan != null && Clan != null ? Clan == character.Clan : false;

                if (character is Player)
                {
                    var player = this as Player;
                    var otherPlayer = character as Player;
                    player.SendCommand(otherPlayer.GetShipCreateCommand(player.RankId == 21 ? true : false, relationType, sameClan, (EventManager.JackpotBattle.Active && player.Spacemap == EventManager.JackpotBattle.Spacemap && otherPlayer.Spacemap == EventManager.JackpotBattle.Spacemap)));
                    player.SendPacket($"0|n|INV|{otherPlayer.Id}|{Convert.ToInt32(otherPlayer.Invisible)}");

                    if (!EventManager.JackpotBattle.Active && player.Spacemap != EventManager.JackpotBattle.Spacemap && otherPlayer.Spacemap != EventManager.JackpotBattle.Spacemap)
                        player.SendPacket($"0|n|t|{otherPlayer.Id}|366|title_1");

                    player.CheckAbilities(otherPlayer);
                    player.SendPacket(otherPlayer.GetDronesPacket());
                    player.SendCommand(DroneFormationChangeCommand.write(otherPlayer.Id, DroneManager.GetSelectedFormationId(otherPlayer.SettingsManager.SelectedFormation)));
                }
                else if (character is Pet)
                {
                    var pet = character as Pet;
                    var player = this as Player;
                    player.SendCommand(PetActivationCommand.write(pet.Owner.Id, pet.Id, 22, 3, pet.Owner.Name + "'s P.E.T.", (short)pet.Owner.FactionId, pet.Owner.GetClanId(), 15, pet.Owner.GetClanTag(), new ClanRelationModule(relationType), pet.Position.X, pet.Position.Y, pet.Speed, false, true, new class_11d(class_11d.DEFAULT)));
                    player.SendPacket($"0|n|INV|{pet.Id}|{Convert.ToInt32(pet.Invisible)}");
                }
                else if (character is Spaceball)
                {
                    var spaceball = character as Spaceball;
                    var player = this as Player;
                    player.SendCommand(spaceball.GetShipCreateCommand());
                }
            }

            return success;
        }

        public void CheckAbilities(Player otherPlayer)
        {
            var player = this as Player;

            var sentinel = otherPlayer.SkillManager.Sentinel;
            var diminisher = otherPlayer.SkillManager.Diminisher;
            var spectrum = otherPlayer.SkillManager.Spectrum;
            var venom = otherPlayer.SkillManager.Venom;
            player.SendPacket($"0|SD|{(sentinel.Active ? "A" : "D")}|R|4|{otherPlayer.Id}");
            player.SendPacket($"0|SD|{(diminisher.Active ? "A" : "D")}|R|2|{otherPlayer.Id}");
            player.SendPacket($"0|SD|{(spectrum.Active ? "A" : "D")}|R|3|{otherPlayer.Id}");
            player.SendPacket($"0|SD|{(venom.Active ? "A" : "D")}|R|5|{otherPlayer.Id}");
        }

        public void Heal(int amount, int healerId = 0, HealType healType = HealType.HEALTH)
        {
            if (amount < 0)
                return;

            switch (healType)
            {
                case HealType.HEALTH:
                    if (CurrentHitPoints + amount > MaxHitPoints)
                        amount = MaxHitPoints - CurrentHitPoints;
                    CurrentHitPoints += amount;
                    break;
                case HealType.SHIELD:
                    if (CurrentShieldPoints + amount > MaxShieldPoints)
                        amount = MaxShieldPoints - CurrentShieldPoints;
                    CurrentShieldPoints += amount;
                    break;
            }

            if (this is Player player)
            {
                var healPacket = "0|A|HL|" + healerId + "|" + Id + "|" + (healType == HealType.HEALTH ? "HPT" : "SHD") + "|" + CurrentHitPoints + "|" + amount;

                if (!Invisible)
                {
                    foreach (var otherPlayers in InRangeCharacters.Values)
                        if (otherPlayers.Selected == this)
                            if (otherPlayers is Player)
                                (otherPlayers as Player).SendPacket(healPacket);
                }

                player.SendPacket(healPacket);
            }

            UpdateStatus();

        }

        public void UpdateStatus()
        {
            if (CurrentHitPoints > MaxHitPoints) CurrentHitPoints = MaxHitPoints;
            if (CurrentHitPoints < 0) CurrentHitPoints = 0;
            if (CurrentShieldPoints > MaxShieldPoints) CurrentShieldPoints = MaxShieldPoints;
            if (CurrentShieldPoints < 0) CurrentShieldPoints = 0;


            if (this is Player player)
            {
                var gameSession = GameManager.GetGameSession(Id);
                if (gameSession == null) return;

                player.SendCommand(AttributeHitpointUpdateCommand.write(CurrentHitPoints, MaxHitPoints, CurrentNanoHull, MaxNanoHull));
                player.SendCommand(AttributeShieldUpdateCommand.write(player.CurrentShieldPoints, player.MaxShieldPoints));
                player.SendCommand(SetSpeedCommand.write(player.Speed, player.Speed));
            }

            if (this is Pet pet)
            {
                var gameSession = pet.Owner.GetGameSession();
                if (gameSession == null) return;

                //gameSession.Player.SendCommand(PetHitpointsUpdateCommand.write(pet.CurrentHitPoints, pet.MaxHitPoints, false));

                gameSession.Player.SendCommand(PetShieldUpdateCommand.write(pet.CurrentShieldPoints, pet.MaxShieldPoints));
            }

            foreach (var character in InRangeCharacters.Values)
                if (character is Player && character.Selected != null && character.Selected.Id == Id)
                    (character as Player).SendCommand(ShipSelectionCommand.write(Id, Ship.Id, CurrentShieldPoints, MaxShieldPoints, CurrentHitPoints, MaxHitPoints, CurrentNanoHull, MaxNanoHull, false));
        }

        public bool RemoveInRangeCharacter(Character character)
        {
            if (!IsInRangeCharacter(character)) return false;

            var success = InRangeCharacters.TryRemove(character.Id, out character);
            if (success)
            {
                InRangeCharacterRemoved?.Invoke(this, new CharacterArgs(character));

                if (this is Player)
                {
                    var player = this as Player;
                    if (SelectedCharacter == character)
                    {
                        Selected = null;
                        player.DisableAttack(player.SettingsManager.SelectedLaser);
                    }
                    var shipRemoveCommand = ShipRemoveCommand.write(character.Id);
                    player.SendCommand(shipRemoveCommand);
                }
            }
            return success;
        }

        public void AddVisualModifier(VisualModifierCommand visualModifier)
        {
            if (this is Player)
            {
                var player = this as Player;

                switch (visualModifier.modifier)
                {
                    case VisualModifierCommand.INVINCIBILITY:
                        player.invincibilityEffect = true;
                        player.invincibilityEffectTime = DateTime.Now;
                        break;
                    case VisualModifierCommand.MIRRORED_CONTROLS:
                        player.mirroredControlEffect = true;
                        player.mirroredControlEffectTime = DateTime.Now;
                        break;
                    case VisualModifierCommand.WIZARD_ATTACK:
                        player.wizardEffect = true;
                        player.wizardEffectTime = DateTime.Now;
                        break;
                }
            }

            VisualModifiers.TryAdd(visualModifier.modifier, visualModifier);
            SendCommandToInRangePlayers(visualModifier.writeCommand());

            if (this is Player)
                (this as Player).SendCommand(visualModifier.writeCommand());
        }

        public void RemoveVisualModifier(int attributeId)
        {
            var visualModifier = VisualModifiers.FirstOrDefault(x => x.Value.modifier == attributeId).Value;

            if (visualModifier != null)
            {
                SendCommandToInRangePlayers(new VisualModifierCommand(visualModifier.userId, visualModifier.modifier, visualModifier.attribute, Ship.LootId, visualModifier.count, false).writeCommand());

                if (this is Player)
                    (this as Player).SendCommand(new VisualModifierCommand(visualModifier.userId, visualModifier.modifier, visualModifier.attribute, Ship.LootId, visualModifier.count, false).writeCommand());

                VisualModifiers.TryRemove(visualModifier.modifier, out visualModifier);
            }
        }

        public bool IsInRangeCharacter(Character character)
        {
            return InRangeCharacters.ContainsKey(character.Id);
        }
    }
}
