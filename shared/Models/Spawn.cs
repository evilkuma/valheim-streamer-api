
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Shared.Models
{
    public static class SpawnData
    {
        public static readonly string rpc = "ValheimStreamerApi/api/spawn";

        public class ActionMainData
        {
            [JsonProperty("playerName")]
            public string playerName { get; set; }
            [JsonProperty("prefabName")]
            public string prefabName { get; set; }
            [JsonProperty("amount")]
            public int amount { get; set; }
            [JsonProperty("level")]
            public int level { get; set; }
            [JsonProperty("pickup")]
            public bool pickup { get; set; }
        }

        public class ActionWoodenPrisonData
        {
            [JsonProperty("playerName")]
            public string playerName { get; set; }
        }

        public class ActionStonePrisonData
        {
            [JsonProperty("playerName")]
            public string playerName { get; set; }
        }

        public class ActionGoldenRainData
        {
            [JsonProperty("playerName")]
            public string playerName { get; set; }
        }
        
        public class ActionStarterKitData
        {
            [JsonProperty("playerName")]
            public string playerName { get; set; }
        }

        public class ChestItemData
        {
            [JsonProperty("name")]
            public string name { get; set; }
            [JsonProperty("amount")]
            public int amount { get; set; }
        }

        public class RpcActionChestData
        {
            [JsonProperty("chest")]
            public string chest { get; set; }
            [JsonProperty("items")]
            public List<ChestItemData> items { get; set; }
        }

        public class ActionInvisibleEnemyData
        {
            [JsonProperty("playerName")]
            public string playerName { get; set; }
        }

        public class RpcActionInvisibleEnemyData
        {
            [JsonProperty("name")]
            public string name { get; set; }
        }

        public class ActionSkeletonArmyData
        {
            [JsonProperty("playerName")]
            public string playerName { get; set; }
        }

        public class RpcActionSkeletonArmyData
        {
            [JsonProperty("prefabName")]
            public string prefabName { get; set; }
            [JsonProperty("amount")]
            public int amount { get; set; }
            [JsonProperty("level")]
            public int level { get; set; }
        }
        
        public class RpcRequestData
        {
            [JsonProperty("prefabName")]
            public string prefabName { get; set; }
            [JsonProperty("amount")]
            public int amount { get; set; }
            [JsonProperty("level")]
            public int level { get; set; }
            [JsonProperty("pickup")]
            public bool pickup { get; set; }
        }

        public class RpcResponseData
        {
            [JsonProperty("status")]
            public string status { get; set; }
        }
    }
}