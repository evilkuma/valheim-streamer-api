using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace ValheimStreamerApi
{
    public class SpawnApiController : ApiController<SpawnApiController.ActionMainData>
    {
        public class ActionMainData
        {
            [JsonProperty("playerName")] public string playerName { get; set; }
            [JsonProperty("prefabName")] public string prefabName { get; set; }
            [JsonProperty("amount")]     public int    amount     { get; set; }
            [JsonProperty("level")]      public int    level      { get; set; }
            [JsonProperty("pickup")]     public bool   pickup     { get; set; }
        }

        private class ChestItemData
        {
            [JsonProperty("name")]   public string name   { get; set; }
            [JsonProperty("amount")] public int    amount { get; set; }
        }

        private class RpcActionData
        {
            [JsonProperty("prefabName")] public string prefabName { get; set; }
            [JsonProperty("amount")]     public int    amount     { get; set; }
            [JsonProperty("level")]      public int    level      { get; set; }
            [JsonProperty("pickup")]     public bool   pickup     { get; set; }
        }

        private class RpcResponseData
        {
            [JsonProperty("status")] public string status { get; set; }
        }

        private class RpcActionChestData
        {
            [JsonProperty("chest")] public string          chest { get; set; }
            [JsonProperty("items")] public List<ChestItemData> items { get; set; }
        }

        private class RpcActionInvisibleEnemyData
        {
            [JsonProperty("name")] public string name { get; set; }
        }

        private class RpcActionSkeletonArmyData
        {
            [JsonProperty("prefabName")] public string prefabName { get; set; }
            [JsonProperty("amount")]     public int    amount     { get; set; }
            [JsonProperty("level")]      public int    level      { get; set; }
        }

        private class RpcActionFollowerData
        {
            [JsonProperty("prefabName")] public string prefabName { get; set; }
            [JsonProperty("level")]      public int    level      { get; set; }
        }

        public SpawnApiController()
        {
            http = "/api/spawn";
            rpc  = "ValheimStreamerApi/api/spawn";

            RegisterHttpAction<PlayerActionData>("wooden-prison",    ActionWoodenPrison);
            RegisterHttpAction<PlayerActionData>("stone-prison",     ActionStonePrison);
            RegisterHttpAction<PlayerActionData>("golden-rain",      ActionGoldenRain);
            RegisterHttpAction<PlayerActionData>("starter-kit",      ActionStarterKit);
            RegisterHttpAction<PlayerActionData>("invisible-enemy",  ActionInvisibleEnemy);
            RegisterHttpAction<PlayerActionData>("skeleton-army",    ActionSkeletonArmy);
            RegisterHttpAction<PlayerActionData>("follower",         ActionFollower);

            RegisterRpcAction<RpcActionData>("main",                     Spawn);
            RegisterRpcAction<object>("wooden-prison",                   WoodenPrison);
            RegisterRpcAction<object>("stone-prison",                    StonePrison);
            RegisterRpcAction<object>("golden-rain",                     GoldenRain);
            RegisterRpcAction<RpcActionChestData>("chest",               Chest);
            RegisterRpcAction<RpcActionInvisibleEnemyData>("invisible-enemy", InvisibleEnemy);
            RegisterRpcAction<RpcActionSkeletonArmyData>("skeleton-army", SkeletonArmy);
            RegisterRpcAction<RpcActionFollowerData>("follower",         Follower);
        }

        // === Server (HTTP) ===

        protected override async Task<object> Action(ActionMainData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "main",
                new RpcActionData
                {
                    prefabName = data.prefabName,
                    amount     = data.amount,
                    level      = data.level,
                    pickup     = data.pickup
                }
            );
            return JsonParser.Parse<RpcResponseData>(zData);
        }

        private async Task<object> ActionWoodenPrison(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "wooden-prison", new {});
            return JsonParser.Parse<RpcResponseData>(zData);
        }

        private async Task<object> ActionStonePrison(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "stone-prison", new {});
            return JsonParser.Parse<RpcResponseData>(zData);
        }

        private async Task<object> ActionGoldenRain(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "golden-rain", new {});
            return JsonParser.Parse<RpcResponseData>(zData);
        }

        private async Task<object> ActionStarterKit(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            string chest;
            List<ChestItemData> items;

            if (!ZoneSystem.instance.GetGlobalKey("defeated_eikthyr"))
            {
                chest = "piece_chest_wood";
                items = new List<ChestItemData>
                {
                    new ChestItemData{ name = "Wood",          amount = 100 },
                    new ChestItemData{ name = "Stone",         amount = 50  },
                    new ChestItemData{ name = "Flint",         amount = 25  },
                    new ChestItemData{ name = "LeatherScraps", amount = 20  },
                    new ChestItemData{ name = "Feathers",      amount = 15  },
                    new ChestItemData{ name = "Mushroom",      amount = 10  },
                    new ChestItemData{ name = "Raspberry",     amount = 20  },
                    new ChestItemData{ name = "Blueberries",   amount = 20  },
                };
            }
            else if (!ZoneSystem.instance.GetGlobalKey("defeated_gdking"))
            {
                chest = "piece_chest_wood";
                items = new List<ChestItemData>
                {
                    new ChestItemData{ name = "RoundLog",   amount = 40 },
                    new ChestItemData{ name = "Resin",      amount = 30 },
                    new ChestItemData{ name = "Coal",       amount = 20 },
                    new ChestItemData{ name = "CopperOre",  amount = 40 },
                    new ChestItemData{ name = "TinOre",     amount = 20 },
                    new ChestItemData{ name = "Honey",      amount = 10 },
                    new ChestItemData{ name = "Carrot",     amount = 15 },
                    new ChestItemData{ name = "CookedMeat", amount = 10 },
                };
            }
            else if (!ZoneSystem.instance.GetGlobalKey("defeated_bonemass"))
            {
                chest = "piece_chest";
                items = new List<ChestItemData>
                {
                    new ChestItemData{ name = "ElderBark",      amount = 30 },
                    new ChestItemData{ name = "IronScrap",      amount = 40 },
                    new ChestItemData{ name = "WitheredBone",   amount = 5  },
                    new ChestItemData{ name = "Guck",           amount = 10 },
                    new ChestItemData{ name = "Turnip",         amount = 15 },
                    new ChestItemData{ name = "CookedDeerMeat", amount = 10 },
                };
            }
            else if (!ZoneSystem.instance.GetGlobalKey("defeated_dragon"))
            {
                chest = "piece_chest";
                items = new List<ChestItemData>
                {
                    new ChestItemData{ name = "Obsidian",   amount = 30 },
                    new ChestItemData{ name = "SilverOre",  amount = 40 },
                    new ChestItemData{ name = "WolfPelt",   amount = 15 },
                    new ChestItemData{ name = "WolfFang",   amount = 10 },
                    new ChestItemData{ name = "Onion",      amount = 20 },
                    new ChestItemData{ name = "FreezeGland", amount = 5 },
                };
            }
            else
            {
                chest = "piece_chest_blackmetal";
                items = new List<ChestItemData>
                {
                    new ChestItemData{ name = "BlackMetalScrap", amount = 40 },
                    new ChestItemData{ name = "LinenThread",     amount = 20 },
                    new ChestItemData{ name = "Tar",             amount = 15 },
                    new ChestItemData{ name = "Resin",           amount = 20 },
                    new ChestItemData{ name = "Flax",            amount = 30 },
                    new ChestItemData{ name = "Barley",          amount = 30 },
                    new ChestItemData{ name = "CloudBerry",      amount = 20 },
                };
            }

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "chest",
                new RpcActionChestData{ chest = chest, items = items }
            );
            return JsonParser.Parse<RpcResponseData>(zData);
        }

        private async Task<object> ActionInvisibleEnemy(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            string[][] mobsByTier =
            {
                new[] { "Greydwarf_Elite", "Greydwarf_Shaman"      },
                new[] { "Troll", "Greydwarf_Elite"                 },
                new[] { "Draugr_Elite", "BlobElite", "Abomination" },
                new[] { "Fenring", "StoneGolem", "Drake"           },
                new[] { "FulingBerserker", "Deathsquito", "Lox"   },
            };

            int tier = 0;
            if (ZoneSystem.instance.GetGlobalKey("defeated_eikthyr"))  tier = 1;
            if (ZoneSystem.instance.GetGlobalKey("defeated_gdking"))   tier = 2;
            if (ZoneSystem.instance.GetGlobalKey("defeated_bonemass")) tier = 3;
            if (ZoneSystem.instance.GetGlobalKey("defeated_dragon"))   tier = 4;

            string[] pool = mobsByTier[tier];
            string prefabName = pool[Random.Range(0, pool.Length)];

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "invisible-enemy",
                new RpcActionInvisibleEnemyData{ name = prefabName }
            );
            return JsonParser.Parse<RpcResponseData>(zData);
        }

        private async Task<object> ActionSkeletonArmy(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

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
            int amount = Random.Range(t.min, t.max + 1);

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "skeleton-army",
                new RpcActionSkeletonArmyData { prefabName = t.prefab, amount = amount, level = t.level }
            );
            return JsonParser.Parse<RpcResponseData>(zData);
        }

        private async Task<object> ActionFollower(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

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

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "follower",
                new RpcActionFollowerData { prefabName = "StreamerApi.Follower", level = t.level }
            );
            return JsonParser.Parse<RpcResponseData>(zData);
        }

        // === Client (RPC) ===

        private object Spawn(RpcActionData data)
        {
            Log.LogInfo($"Получили запрос на спавн {data.prefabName}");
            var status = SpawnPrefab(data.prefabName, data.amount, data.level, data.pickup);
            return new { status };
        }

        private object WoodenPrison(object _data)
        {
            string prefabName = "Beech1";
            int count = 10;
            float radius = 2.5f;

            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab) return new { status = $"Prefab not found: {prefabName}" };

            Player player = Player.m_localPlayer;
            Vector3 center = player.transform.position;

            for (int i = 0; i < count; i++)
            {
                float angle = 2 * Mathf.PI * i / count;
                Vector3 position = new Vector3(
                    center.x + radius * Mathf.Cos(angle),
                    center.y,
                    center.z + radius * Mathf.Sin(angle)
                );
                Quaternion rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                GameObject.Instantiate(prefab, position, rotation);
            }

            return new { status = "ok" };
        }

        private object StonePrison(object _data)
        {
            const string wallPrefabName = "stone_wall_2x1";
            const int wallCount = 10;
            const int layers = 3;
            const float wallWidth = 2f;
            const float wallHeight = 1f;

            float radius = wallCount * wallWidth / (2 * Mathf.PI);

            GameObject prefab = ZNetScene.instance.GetPrefab(wallPrefabName);
            if (!prefab) return new { status = $"Prefab not found: {wallPrefabName}" };

            Player player = Player.m_localPlayer;
            Vector3 center = player.transform.position;

            for (int layer = 0; layer < layers; layer++)
            {
                float y = center.y + layer * wallHeight;

                for (int i = 0; i < wallCount; i++)
                {
                    float angle = 2 * Mathf.PI * i / wallCount;

                    Vector3 position = new Vector3(
                        center.x + radius * Mathf.Cos(angle),
                        y,
                        center.z + radius * Mathf.Sin(angle)
                    );

                    Quaternion rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg + 90f, 0);

                    GameObject go = GameObject.Instantiate(prefab, position, rotation);
                    go.GetComponent<Piece>()?.SetCreator(player.GetPlayerID());
                }
            }

            return new { status = "ok" };
        }

        private object GoldenRain(object _data)
        {
            Player.m_localPlayer.StartCoroutine(CoinRainRoutine());
            return new { status = "ok" };
        }

        private object Chest(RpcActionChestData data)
        {
            Player player = Player.m_localPlayer;

            GameObject prefab = ZNetScene.instance.GetPrefab(data.chest);
            if (!prefab) return new { status = $"Prefab not found: {data.chest}" };

            Vector3 position = player.transform.position + player.transform.forward * 2f;
            GameObject go = GameObject.Instantiate(prefab, position, Quaternion.identity);

            go.GetComponent<Piece>()?.SetCreator(player.GetPlayerID());

            Container container = go.GetComponent<Container>();
            if (container != null)
            {
                Inventory inventory = container.GetInventory();
                foreach (var item in data.items)
                    inventory.AddItem(item.name, item.amount, 1, 0, 0L, "");
            }

            return new { status = "ok" };
        }

        private object InvisibleEnemy(RpcActionInvisibleEnemyData data)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(data.name);
            if (!prefab) return new { status = $"Prefab not found: {data.name}" };

            Player player = Player.m_localPlayer;
            Vector3 position = player.transform.position + player.transform.forward * 5f;

            GameObject go = GameObject.Instantiate(prefab, position, Quaternion.identity);
            go.GetComponent<Character>()?.SetLevel(2);
            go.AddComponent<InvisibleEnemyBehaviour>();

            return new { status = "ok" };
        }

        private object SkeletonArmy(RpcActionSkeletonArmyData data)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(data.prefabName);
            if (!prefab) return new { status = $"Prefab not found: {data.prefabName}" };

            Player player = Player.m_localPlayer;
            Vector3 center = player.transform.position;
            const float radius = 4f;

            for (int i = 0; i < data.amount; i++)
            {
                float angle = 2 * Mathf.PI * i / data.amount;
                Vector3 position = new Vector3(
                    center.x + radius * Mathf.Cos(angle),
                    center.y,
                    center.z + radius * Mathf.Sin(angle)
                );
                GameObject go = GameObject.Instantiate(prefab, position, Quaternion.identity);
                go.GetComponent<Character>()?.SetLevel(data.level);
            }

            return new { status = "ok" };
        }

        private object Follower(RpcActionFollowerData data)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(data.prefabName);
            if (!prefab) return new { status = $"Prefab not found: {data.prefabName}" };

            Player player = Player.m_localPlayer;
            Vector3 position = player.transform.position + player.transform.forward * 3f;

            GameObject go = GameObject.Instantiate(prefab, position, Quaternion.identity);

            return new { status = "ok" };
        }

        private static IEnumerator CoinRainRoutine()
        {
            const string prefabName = "Coins";
            const int iterations = 20;
            const int stackSize = 25;
            const float interval = 0.15f;
            const float spawnHeight = 13f;
            const float spreadRadius = 2.5f;

            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (prefab == null) yield break;

            for (int i = 0; i < iterations; i++)
            {
                Player player = Player.m_localPlayer;
                if (player == null) yield break;

                Vector2 spread = Random.insideUnitCircle * spreadRadius;
                Vector3 position = new Vector3(
                    player.transform.position.x + spread.x,
                    player.transform.position.y + spawnHeight,
                    player.transform.position.z + spread.y
                );

                GameObject go = GameObject.Instantiate(prefab, position, Quaternion.identity);

                ItemDrop itemDrop = go.GetComponent<ItemDrop>();
                if (itemDrop != null)
                    itemDrop.m_itemData.m_stack = stackSize;

                Rigidbody rb = go.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector2 impulse = Random.insideUnitCircle * 2f;
                    rb.linearVelocity = new Vector3(impulse.x, 0f, impulse.y);
                }

                yield return new WaitForSeconds(interval);
            }
        }

        private static string SpawnPrefab(string prefabName, int amount, int level, bool pickup)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab) return "Not find prefab";

            if (prefab.GetComponent<Character>())
                return SpawnCharacter(prefab, amount, level);

            if (prefab.GetComponent<ItemDrop>())
                return SpawnItem(prefab, amount, level, pickup);

            SpawnItem(false, prefab);
            return "ok";
        }

        private static string SpawnCharacter(GameObject prefab, int amount, int level)
        {
            Player player = Player.m_localPlayer;
            Vector3 position = player.transform.position + player.transform.forward * 2f + Vector3.up;

            for (int i = 0; i < amount; i++)
            {
                GameObject spawnedChar = GameObject.Instantiate(prefab, position, Quaternion.identity);
                spawnedChar.GetComponent<Character>()?.SetLevel(level);
            }

            return "ok";
        }

        private static string SpawnItem(GameObject prefab, int amount, int level, bool pickup)
        {
            ItemDrop itemPrefab = prefab.GetComponent<ItemDrop>();

            if (itemPrefab.m_itemData.IsEquipable())
            {
                itemPrefab.m_itemData.m_quality = level;
                itemPrefab.m_itemData.m_durability = itemPrefab.m_itemData.GetMaxDurability();

                for (int i = 0; i < amount; i++)
                    SpawnItem(pickup, prefab);
            }
            else
            {
                int maxStack = itemPrefab.m_itemData.m_shared.m_maxStackSize;
                if (maxStack < 1) maxStack = 1;

                itemPrefab.m_itemData.m_stack = maxStack;
                int stacksCount = amount / maxStack;
                int lastStack = amount % maxStack;

                for (int i = 0; i < stacksCount; i++)
                    SpawnItem(pickup, prefab);

                if (lastStack != 0)
                {
                    itemPrefab.m_itemData.m_stack = lastStack;
                    SpawnItem(pickup, prefab);
                }
            }

            itemPrefab.m_itemData.m_stack = 1;
            itemPrefab.m_itemData.m_quality = 1;

            return "ok";
        }

        private static GameObject SpawnItem(bool pickup, GameObject prefab)
        {
            Player player = Player.m_localPlayer;

            if (pickup)
            {
                player.PickupPrefab(prefab);
                return null;
            }

            return GameObject.Instantiate(
                prefab,
                player.transform.position + player.transform.forward * 2f + Vector3.up,
                Quaternion.identity
            );
        }
    }
}
