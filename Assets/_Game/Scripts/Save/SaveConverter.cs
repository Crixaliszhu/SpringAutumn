using System;
using SpringAutumn.Commands;
using SpringAutumn.Config;
using SpringAutumn.Core.Engine;
using SpringAutumn.Core.Utils;
using SpringAutumn.Runtime;

namespace SpringAutumn.Save
{
    public class SaveConverter
    {
        public const int CurrentVersion = 1;

        public SaveData ToSave(WorldRuntime world, int slot = 0)
        {
            var data = new SaveData();
            data.version = CurrentVersion;
            data.info.slot = slot;
            data.info.displayName = world.Time.ToString();
            data.info.year = world.Time.Year;
            data.info.month = world.Time.Month;
            data.time.year = world.Time.Year;
            data.time.month = world.Time.Month;

            foreach (var nation in world.Nations.GetAll())
            {
                data.nations.Add(new NationSaveData
                {
                    id = nation.Id,
                    treasuryGrain = nation.TreasuryGrain,
                    treasuryMoney = nation.TreasuryMoney,
                    aiState = nation.AIState.ToString(),
                    warStatus = nation.WarStatus.ToString()
                });
            }

            foreach (var region in world.Regions.GetAll())
            {
                data.regions.Add(new RegionSaveData
                {
                    id = region.Id,
                    ownerId = region.OwnerId,
                    isFrontier = region.IsFrontier,
                    cityId = region.CityId,
                    villageIds = new System.Collections.Generic.List<string>(region.VillageIds),
                    neighborRegionIds = new System.Collections.Generic.List<string>(region.NeighborRegionIds)
                });
            }

            foreach (var settlement in world.Settlements.GetAll())
                data.settlements.Add(ToSettlementData(settlement));

            foreach (var army in world.Armies.GetAll())
            {
                data.armies.Add(new ArmySaveData
                {
                    id = army.Id,
                    nationId = army.NationId,
                    sourceSettlementId = army.SourceSettlementId,
                    currentRegionId = army.CurrentRegionId,
                    targetRegionId = army.TargetRegionId,
                    targetSettlementId = army.TargetSettlementId,
                    soldiers = army.Soldiers,
                    morale = army.Morale,
                    status = army.Status.ToString(),
                    mission = army.Mission.ToString(),
                    moveProgress = army.MoveProgress
                });
            }

            foreach (var pair in world.Diplomacy.All)
            {
                data.diplomacy.Add(new DiplomacyData
                {
                    key = pair.Key,
                    relationValue = pair.Value
                });
            }

            foreach (var command in world.Commands.Peek())
                data.commandQueue.commands.Add(ToCommandData(command));

            foreach (var evt in world.Events.Active)
            {
                data.events.Add(new EventData
                {
                    eventId = evt.EventId,
                    targetId = evt.TargetId,
                    remainingMonths = evt.RemainingMonths
                });
            }

            var randomState = world.Random.GetState();
            data.random.x = randomState.x;
            data.random.y = randomState.y;
            data.random.z = randomState.z;
            data.random.w = randomState.w;
            return data;
        }

        public WorldRuntime Restore(SaveData data, ConfigDatabase config)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.version != CurrentVersion)
                throw new InvalidOperationException("存档版本不兼容: " + data.version);

            var world = new WorldRuntime();
            world.Time = new GameTimeState(data.time.year, data.time.month);
            world.Nations = new Repository<NationState>();
            world.Regions = new Repository<RegionState>();
            world.Settlements = new Repository<SettlementState>();
            world.Armies = new Repository<ArmyState>();
            world.Diplomacy = new DiplomacyState();
            world.Commands = new CommandQueue();
            world.Events = new EventState();
            world.Random = new DeterministicRandom(data.random.x, data.random.y, data.random.z, data.random.w);

            foreach (var n in data.nations)
            {
                world.Nations.Add(new NationState(n.id)
                {
                    TreasuryGrain = n.treasuryGrain,
                    TreasuryMoney = n.treasuryMoney,
                    AIState = ParseEnum(n.aiState, NationAIState.Developing),
                    WarStatus = ParseEnum(n.warStatus, WarStatus.Peace)
                });
            }

            foreach (var r in data.regions)
            {
                world.Regions.Add(new RegionState(r.id)
                {
                    OwnerId = r.ownerId,
                    IsFrontier = r.isFrontier,
                    CityId = r.cityId,
                    VillageIds = r.villageIds ?? new System.Collections.Generic.List<string>(),
                    NeighborRegionIds = r.neighborRegionIds ?? new System.Collections.Generic.List<string>()
                });
            }

            foreach (var s in data.settlements)
                world.Settlements.Add(FromSettlementData(s));

            foreach (var a in data.armies)
            {
                world.Armies.Add(new ArmyState(a.id)
                {
                    NationId = a.nationId,
                    SourceSettlementId = a.sourceSettlementId,
                    CurrentRegionId = a.currentRegionId,
                    TargetRegionId = a.targetRegionId,
                    TargetSettlementId = a.targetSettlementId,
                    Soldiers = a.soldiers,
                    Morale = a.morale,
                    Status = ParseEnum(a.status, ArmyStatus.Idle),
                    Mission = ParseEnum(a.mission, ArmyMission.Attack),
                    MoveProgress = a.moveProgress
                });
            }

            foreach (var d in data.diplomacy)
            {
                var ids = d.key.Split('|');
                if (ids.Length == 2)
                    world.Diplomacy.SetRelation(ids[0], ids[1], d.relationValue);
            }

            foreach (var command in data.commandQueue.commands)
                world.Commands.Enqueue(FromCommandData(command, config));

            foreach (var e in data.events)
                world.Events.Active.Add(new OngoingEvent(e.eventId, e.targetId, e.remainingMonths));

            return world;
        }

        private static SettlementSaveData ToSettlementData(SettlementState s)
        {
            var data = new SettlementSaveData
            {
                id = s.Id,
                type = s.Type.ToString(),
                regionId = s.RegionId,
                ownerId = s.OwnerId,
                households = s.Households,
                population = s.Population,
                populationCap = s.PopulationCap,
                land = s.Land,
                grain = s.Grain,
                money = s.Money,
                loyalty = s.Loyalty,
                garrison = s.Garrison,
                neighborSettlementIds = new System.Collections.Generic.List<string>(s.NeighborSettlementIds)
            };

            foreach (var b in s.Buildings)
                data.buildings.Add(new BuildingData { buildingId = b.BuildingId, level = b.Level });
            foreach (var c in s.ConstructionQueue)
                data.constructionQueue.Add(new ConstructionTaskData { buildingId = c.BuildingId, remainingMonths = c.RemainingMonths });
            foreach (var r in s.RecruitQueue)
                data.recruitQueue.Add(new RecruitTaskData { count = r.Count, remainingMonths = r.RemainingMonths });
            return data;
        }

        private static SettlementState FromSettlementData(SettlementSaveData data)
        {
            var s = new SettlementState(data.id)
            {
                Type = ParseEnum(data.type, SettlementType.Village),
                RegionId = data.regionId,
                OwnerId = data.ownerId,
                Households = data.households,
                Population = data.population,
                PopulationCap = data.populationCap,
                Land = data.land,
                Grain = data.grain,
                Money = data.money,
                Loyalty = data.loyalty,
                Garrison = data.garrison,
                NeighborSettlementIds = data.neighborSettlementIds ?? new System.Collections.Generic.List<string>()
            };

            foreach (var b in data.buildings)
                s.Buildings.Add(new BuildingInstance(b.buildingId, b.level));
            foreach (var c in data.constructionQueue)
                s.ConstructionQueue.Add(new ConstructionTask(c.buildingId, c.remainingMonths));
            foreach (var r in data.recruitQueue)
                s.RecruitQueue.Add(new RecruitTask(r.count, r.remainingMonths));
            return s;
        }

        private static CommandData ToCommandData(GameCommand command)
        {
            var data = new CommandData { nationId = command.NationId };
            if (command is BuildCommand build)
            {
                data.type = "Build";
                data.settlementId = build.SettlementId;
                data.buildingId = build.BuildingId;
            }
            else if (command is RecruitCommand recruit)
            {
                data.type = "Recruit";
                data.settlementId = recruit.SettlementId;
                data.count = recruit.Count;
            }
            else if (command is MoveArmyCommand move)
            {
                data.type = "MoveArmy";
                data.sourceSettlementId = move.SourceSettlementId;
                data.targetRegionId = move.TargetRegionId;
                data.targetSettlementId = move.TargetSettlementId;
                data.soldiers = move.Soldiers;
            }
            else if (command is TransferArmyCommand transfer)
            {
                data.type = "TransferArmy";
                data.sourceSettlementId = transfer.SourceSettlementId;
                data.targetSettlementId = transfer.TargetSettlementId;
                data.soldiers = transfer.Soldiers;
            }
            else if (command is AttackCommand attack)
            {
                data.type = "Attack";
                data.armyId = attack.ArmyId;
                data.targetSettlementId = attack.TargetSettlementId;
            }
            else if (command is DiplomacyCommand diplomacy)
            {
                data.type = "Diplomacy";
                data.targetNationId = diplomacy.TargetNationId;
                data.relationDelta = diplomacy.RelationDelta;
                data.declareWar = diplomacy.DeclareWar;
            }
            return data;
        }

        private static GameCommand FromCommandData(CommandData data, ConfigDatabase config)
        {
            switch (data.type)
            {
                case "Build":
                    return new BuildCommand(data.nationId, data.settlementId, data.buildingId, config);
                case "Recruit":
                    return new RecruitCommand(data.nationId, data.settlementId, data.count, config);
                case "MoveArmy":
                    return new MoveArmyCommand(data.nationId, data.sourceSettlementId, data.targetRegionId, data.targetSettlementId, data.soldiers, config);
                case "TransferArmy":
                    return new TransferArmyCommand(data.nationId, data.sourceSettlementId, data.targetSettlementId, data.soldiers, config);
                case "Attack":
                    return new AttackCommand(data.nationId, data.armyId, data.targetSettlementId);
                case "Diplomacy":
                    return new DiplomacyCommand(data.nationId, data.targetNationId, data.relationDelta, data.declareWar);
                default:
                    return null;
            }
        }

        private static T ParseEnum<T>(string value, T fallback) where T : struct
        {
            return Enum.TryParse(value, out T parsed) ? parsed : fallback;
        }
    }
}
