
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shared;
using Shared.Models;

namespace ValheimStreamerApi.Server
{
    public class UseSpawn : HttpController<SpawnData.ActionMainData>
    {
        public UseSpawn() : base()
        {
            http = "/api/spawn";
            RegisterAction<SpawnData.ActionWoodenPrisonData>("wooden-prison", ActionWoodenPrison);
            RegisterAction<SpawnData.ActionStonePrisonData>("stone-prison", ActionStonePrison);
            RegisterAction<SpawnData.ActionGoldenRainData>("golden-rain", ActionGoldenRain);
            RegisterAction<SpawnData.ActionStarterKitData>("starter-kit", ActionStarterKit);
            RegisterAction<SpawnData.ActionInvisibleEnemyData>("invisible-enemy", ActionInvisibleEnemy);
            RegisterAction<SpawnData.ActionSkeletonArmyData>("skeleton-army", ActionSkeletonArmy);
            RegisterAction<SpawnData.ActionFollowerData>("follower", ActionFollower);
        }

        protected override async Task<object> Action(SpawnData.ActionMainData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(SpawnData.rpc, targetPeer.m_uid,
                new RpcRequestData<SpawnData.RpcRequestData>
                {
                    action = "main",
                    data = new SpawnData.RpcRequestData
                    {
                        prefabName = data.prefabName,
                        amount = data.amount,
                        level = data.level,
                        pickup = data.pickup
                    }
                }
            ).Task;
            return JsonParser.Parse<SpawnData.RpcResponseData>(zData);
        }

        private async Task<object> ActionWoodenPrison(SpawnData.ActionWoodenPrisonData data)
        {
            string playerName = data.playerName;
            
            var targetPeer = RpcManager.FindPlayerByName(playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(SpawnData.rpc, targetPeer.m_uid,
                new RpcRequestData<object>
                {
                    action = "wooden-prison",
                    data = new {}
                }
            ).Task;
            return JsonParser.Parse<SpawnData.RpcResponseData>(zData);
        }

        private async Task<object> ActionStonePrison(SpawnData.ActionStonePrisonData data)
        {
            string playerName = data.playerName;
            
            var targetPeer = RpcManager.FindPlayerByName(playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(SpawnData.rpc, targetPeer.m_uid,
                new RpcRequestData<object>
                {
                    action = "stone-prison",
                    data = new {}
                }
            ).Task;
            return JsonParser.Parse<SpawnData.RpcResponseData>(zData);
        }
        
        private async Task<object> ActionGoldenRain(SpawnData.ActionGoldenRainData data)
        {
            string playerName = data.playerName;
            
            var targetPeer = RpcManager.FindPlayerByName(playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(SpawnData.rpc, targetPeer.m_uid,
                new RpcRequestData<object>
                {
                    action = "golden-rain",
                    data = new {}
                }
            ).Task;
            return JsonParser.Parse<SpawnData.RpcResponseData>(zData);
        }

        private async Task<object> ActionStarterKit(SpawnData.ActionStarterKitData data)
        {
            string playerName = data.playerName;
            
            var targetPeer = RpcManager.FindPlayerByName(playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            string chest;
            List<SpawnData.ChestItemData> items = new List<SpawnData.ChestItemData>();

            if (!ZoneSystem.instance.GetGlobalKey("defeated_eikthyr"))
            {
                chest = "piece_chest_wood";
                items = new List<SpawnData.ChestItemData>
                {
                    new SpawnData.ChestItemData{ name = "Wood",           amount = 100 },
                    new SpawnData.ChestItemData{ name = "Stone",          amount = 50  },
                    new SpawnData.ChestItemData{ name = "Flint",          amount = 25  },
                    new SpawnData.ChestItemData{ name = "LeatherScraps",  amount = 20  },
                    new SpawnData.ChestItemData{ name = "Feathers",       amount = 15  },
                    new SpawnData.ChestItemData{ name = "Mushroom",       amount = 10  },
                    new SpawnData.ChestItemData{ name = "Raspberry",      amount = 20  },
                    new SpawnData.ChestItemData{ name = "Blueberries",    amount = 20  },
                };
            }
            else if (!ZoneSystem.instance.GetGlobalKey("defeated_gdking"))
            {
                chest = "piece_chest_wood";
                items = new List<SpawnData.ChestItemData>
                {
                    new SpawnData.ChestItemData{ name = "RoundLog",       amount = 40  },
                    new SpawnData.ChestItemData{ name = "Resin",          amount = 30  },
                    new SpawnData.ChestItemData{ name = "Coal",           amount = 20  },
                    new SpawnData.ChestItemData{ name = "CopperOre",      amount = 40  },
                    new SpawnData.ChestItemData{ name = "TinOre",         amount = 20  },
                    new SpawnData.ChestItemData{ name = "Honey",          amount = 10  },
                    new SpawnData.ChestItemData{ name = "Carrot",         amount = 15  },
                    new SpawnData.ChestItemData{ name = "CookedMeat",     amount = 10  },
                };
            }
            else if (!ZoneSystem.instance.GetGlobalKey("defeated_bonemass"))
            {
                chest = "piece_chest";
                items = new List<SpawnData.ChestItemData>
                {
                    new SpawnData.ChestItemData{ name = "ElderBark",      amount = 30  },
                    new SpawnData.ChestItemData{ name = "IronScrap",      amount = 40  },
                    new SpawnData.ChestItemData{ name = "WitheredBone",   amount = 5   },
                    new SpawnData.ChestItemData{ name = "Guck",           amount = 10  },
                    new SpawnData.ChestItemData{ name = "Turnip",         amount = 15  },
                    new SpawnData.ChestItemData{ name = "CookedDeerMeat", amount = 10  },
                };
            }
            else if (!ZoneSystem.instance.GetGlobalKey("defeated_dragon"))
            {
                chest = "piece_chest";
                items = new List<SpawnData.ChestItemData>
                {
                    new SpawnData.ChestItemData{ name = "Obsidian",       amount = 30  },
                    new SpawnData.ChestItemData{ name = "SilverOre",      amount = 40  },
                    new SpawnData.ChestItemData{ name = "WolfPelt",       amount = 15  },
                    new SpawnData.ChestItemData{ name = "WolfFang",       amount = 10  },
                    new SpawnData.ChestItemData{ name = "Onion",          amount = 20  },
                    new SpawnData.ChestItemData{ name = "FreezeGland",    amount = 5   },
                };
            }
            else
            {
                chest = "piece_chest_blackmetal";
                items = new List<SpawnData.ChestItemData>
                {
                    new SpawnData.ChestItemData{ name = "BlackMetalScrap", amount = 40 },
                    new SpawnData.ChestItemData{ name = "LinenThread",     amount = 20 },
                    new SpawnData.ChestItemData{ name = "Tar",             amount = 15 },
                    new SpawnData.ChestItemData{ name = "Resin",           amount = 20 },
                    new SpawnData.ChestItemData{ name = "Flax",            amount = 30 },
                    new SpawnData.ChestItemData{ name = "Barley",          amount = 30 },
                    new SpawnData.ChestItemData{ name = "CloudBerry",      amount = 20 },
                };
            }

            var zData = await RpcManager.SendMessageAsync(SpawnData.rpc, targetPeer.m_uid,
                new RpcRequestData<SpawnData.RpcActionChestData>
                {
                    action = "chest",
                    data = new SpawnData.RpcActionChestData{ chest = chest, items = items }
                }
            ).Task;
            return JsonParser.Parse<SpawnData.RpcResponseData>(zData);
        }

        private async Task<object> ActionInvisibleEnemy(SpawnData.ActionInvisibleEnemyData data)
        {
            string playerName = data.playerName;
            
            var targetPeer = RpcManager.FindPlayerByName(playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            string[][] mobsByTier =
            {
                new[] { "Greydwarf_Elite", "Greydwarf_Shaman"          },
                new[] { "Troll", "Greydwarf_Elite"                     },
                new[] { "Draugr_Elite", "BlobElite", "Abomination"     },
                new[] { "Fenring", "StoneGolem", "Drake"               },
                new[] { "FulingBerserker", "Deathsquito", "Lox"        },
            };

            int tier = 0;
            if (ZoneSystem.instance.GetGlobalKey("defeated_eikthyr"))  tier = 1;
            if (ZoneSystem.instance.GetGlobalKey("defeated_gdking"))   tier = 2;
            if (ZoneSystem.instance.GetGlobalKey("defeated_bonemass")) tier = 3;
            if (ZoneSystem.instance.GetGlobalKey("defeated_dragon"))   tier = 4;

            string[] pool = mobsByTier[tier];
            string prefabName = pool[UnityEngine.Random.Range(0, pool.Length)];

            var zData = await RpcManager.SendMessageAsync(SpawnData.rpc, targetPeer.m_uid,
                new RpcRequestData<SpawnData.RpcActionInvisibleEnemyData>
                {
                    action = "invisible-enemy",
                    data = new SpawnData.RpcActionInvisibleEnemyData{ name = prefabName }
                }
            ).Task;
            return JsonParser.Parse<SpawnData.RpcResponseData>(zData);
        }

        private async Task<object> ActionSkeletonArmy(SpawnData.ActionSkeletonArmyData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            // prefab, level, minAmount, maxAmount — растёт с каждым убитым боссом
            var tiers = new[]
            {
                (prefab: "Skeleton",        level: 1, min: 5, max: 6),
                (prefab: "Skeleton",        level: 2, min: 5, max: 7),
                (prefab: "Skeleton_Poison", level: 2, min: 6, max: 8),
                (prefab: "Skeleton_Poison", level: 3, min: 7, max: 9),
                (prefab: "Skeleton_Poison", level: 3, min: 8, max: 10),
            };

            int tier = 0;
            if (ZoneSystem.instance.GetGlobalKey("defeated_eikthyr"))  tier = 1;
            if (ZoneSystem.instance.GetGlobalKey("defeated_gdking"))   tier = 2;
            if (ZoneSystem.instance.GetGlobalKey("defeated_bonemass")) tier = 3;
            if (ZoneSystem.instance.GetGlobalKey("defeated_dragon"))   tier = 4;

            var t = tiers[tier];
            int amount = UnityEngine.Random.Range(t.min, t.max + 1);

            var zData = await RpcManager.SendMessageAsync(SpawnData.rpc, targetPeer.m_uid,
                new RpcRequestData<SpawnData.RpcActionSkeletonArmyData>
                {
                    action = "skeleton-army",
                    data = new SpawnData.RpcActionSkeletonArmyData
                    {
                        prefabName = t.prefab,
                        amount     = amount,
                        level      = t.level,
                    }
                }
            ).Task;
            return JsonParser.Parse<SpawnData.RpcResponseData>(zData);
        }

        private async Task<object> ActionFollower(SpawnData.ActionFollowerData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            // Draugr → Draugr_Elite с ростом прогресса — выглядят как воины-викинги
            var tiers = new[]
            {
                (prefab: "Draugr",       level: 1),
                (prefab: "Draugr",       level: 2),
                (prefab: "Draugr_Elite", level: 2),
                (prefab: "Draugr_Elite", level: 3),
                (prefab: "Draugr_Elite", level: 3),
            };

            int tier = 0;
            if (ZoneSystem.instance.GetGlobalKey("defeated_eikthyr"))  tier = 1;
            if (ZoneSystem.instance.GetGlobalKey("defeated_gdking"))   tier = 2;
            if (ZoneSystem.instance.GetGlobalKey("defeated_bonemass")) tier = 3;
            if (ZoneSystem.instance.GetGlobalKey("defeated_dragon"))   tier = 4;

            var t = tiers[tier];

            var zData = await RpcManager.SendMessageAsync(SpawnData.rpc, targetPeer.m_uid,
                new RpcRequestData<SpawnData.RpcActionFollowerData>
                {
                    action = "follower",
                    data   = new SpawnData.RpcActionFollowerData { prefabName = t.prefab, level = t.level }
                }
            ).Task;
            return JsonParser.Parse<SpawnData.RpcResponseData>(zData);
        }
    }
}
