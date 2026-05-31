using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace ValheimStreamerApi
{
    // Biome IDs: Meadows, Swamp, Mountain, BlackForest, Plains, AshLands, DeepNorth, Ocean, Mistlands
    public class LocationApiController : ApiController
    {
        private class ActionTeleportToBiomeData
        {
            [JsonProperty("playerName")] public string playerName { get; set; }
            [JsonProperty("biome")]      public string biome      { get; set; }
        }

        private class ActionTeleportToData
        {
            [JsonProperty("x")] public float x { get; set; }
            [JsonProperty("y")] public float y { get; set; }
            [JsonProperty("z")] public float z { get; set; }
        }

        private class RpcResponseData
        {
            [JsonProperty("status")] public string status { get; set; }
        }

        private class RpcDangerPointData
        {
            [JsonProperty("x")]            public float x            { get; set; }
            [JsonProperty("y")]            public float y            { get; set; }
            [JsonProperty("z")]            public float z            { get; set; }
            [JsonProperty("spawnFulings")] public bool  spawnFulings { get; set; }
        }

        public LocationApiController()
        {
            http = "/api/location";
            rpc  = "ValheimStreamerApi/api/location";

            RegisterHttpAction<ActionTeleportToBiomeData>("teleport-to-biome", ActionTeleportToBiome);
            RegisterRpcAction<ActionTeleportToData>("teleport-to", TeleportTo);
            RegisterHttpAction<PlayerActionData>("biome-danger-point", ActionBiomeDangerPoint);
            RegisterRpcAction<RpcDangerPointData>("biome-danger-point", DangerPoint);
            RegisterHttpAction<PlayerActionData>("to-home", ActionToHome);
            RegisterRpcAction<object>("to-home", ToHome);
            RegisterHttpAction<PlayerActionData>("random-biome", ActionRandomBiome);
        }

        // === Server (HTTP) ===

        private async Task<object> ActionTeleportToBiome(ActionTeleportToBiomeData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            Vector3 playerPosition = targetPeer.m_refPos;
            Vector3 targetPosition = new Vector3();
            float distance = float.MaxValue;

            foreach (var entry in ZoneSystem.instance.m_locationInstances)
            {
                var location = entry.Value.m_location;
                var position = entry.Value.m_position;
                string biome = location.m_biome.ToString();

                if (!biome.Contains(data.biome)) continue;

                float numX = position.x - playerPosition.x;
                float numZ = position.z - playerPosition.z;
                float dist = numX * numX + numZ * numZ;

                if (dist > distance) continue;

                distance = dist;
                targetPosition = position;
            }

            if (distance == float.MaxValue) return new { error = "no finded location" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "teleport-to",
                new ActionTeleportToData { x = targetPosition.x, y = targetPosition.y, z = targetPosition.z }
            );
            return JsonParser.Parse<RpcResponseData>(zData);
        }

        private async Task<object> ActionToHome(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };
            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "to-home", new {});
            return JsonParser.Parse<RpcResponseData>(zData);
        }

        private async Task<object> ActionRandomBiome(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var byBiome = new Dictionary<string, List<Vector3>>();
            foreach (var entry in ZoneSystem.instance.m_locationInstances)
            {
                string biome = entry.Value.m_location.m_biome.ToString();
                if (!byBiome.ContainsKey(biome)) byBiome[biome] = new List<Vector3>();
                byBiome[biome].Add(entry.Value.m_position);
            }

            if (byBiome.Count == 0) return new { error = "no locations found" };

            var biomes = new List<string>(byBiome.Keys);
            string selectedBiome = biomes[Random.Range(0, biomes.Count)];
            List<Vector3> locations = byBiome[selectedBiome];
            Vector3 target = locations[Random.Range(0, locations.Count)];

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "teleport-to",
                new ActionTeleportToData { x = target.x, y = target.y, z = target.z }
            );
            return JsonParser.Parse<RpcResponseData>(zData);
        }

        private async Task<object> ActionBiomeDangerPoint(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            Vector3 playerPos = targetPeer.m_refPos;
            Heightmap.Biome biome = WorldGenerator.instance.GetBiome(playerPos.x, playerPos.z);

            Vector3? target = FindDangerPoint(playerPos, biome);
            if (target == null) return new { error = "no danger point found" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "biome-danger-point",
                new RpcDangerPointData
                {
                    x = target.Value.x, y = target.Value.y, z = target.Value.z,
                    spawnFulings = biome == Heightmap.Biome.BlackForest
                }
            );
            return JsonParser.Parse<RpcResponseData>(zData);
        }

        // === Client (RPC) ===

        private object TeleportTo(ActionTeleportToData data)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return new { status = "not a player" };

            player.TeleportTo(
                new Vector3(data.x, data.y, data.z),
                player.transform.rotation,
                false
            );

            return new { status = "ok" };
        }

        private object ToHome(object _data)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return new { status = "not a player" };

            var profile = Game.instance.GetPlayerProfile();
            if (!profile.HaveCustomSpawnPoint()) return new { status = "no spawn point set" };
            var spawnPoint = profile.GetCustomSpawnPoint();

            player.TeleportTo(spawnPoint, player.transform.rotation, false);
            return new { status = "ok" };
        }

        private object DangerPoint(RpcDangerPointData data)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return new { status = "not a player" };

            player.TeleportTo(new Vector3(data.x, data.y, data.z), player.transform.rotation, false);

            if (data.spawnFulings)
                player.StartCoroutine(SpawnFulingsAfterTeleport());

            return new { status = "ok" };
        }

        private static Vector3? FindDangerPoint(Vector3 playerPos, Heightmap.Biome biome)
        {
            switch (biome)
            {
                case Heightmap.Biome.Meadows:
                    return FindNearestBiomePoint(playerPos, Heightmap.Biome.BlackForest);

                case Heightmap.Biome.BlackForest:
                    return FindNearestLocation(playerPos, "BlackForest", "Crypt")
                        ?? FindNearestLocation(playerPos, "BlackForest");

                case Heightmap.Biome.Swamp:
                    return FindNearestLocation(playerPos, "Swamp", "Draugr")
                        ?? FindNearestLocation(playerPos, "Swamp", "SunkenCrypt")
                        ?? FindNearestLocation(playerPos, "Swamp");

                case Heightmap.Biome.Mountain:
                    return FindNearestLocation(playerPos, "Mountain", "DrakeNest")
                        ?? FindNearestLocation(playerPos, "Mountain");

                case Heightmap.Biome.Plains:
                    return FindNearestLocation(playerPos, "Plains", "Goblin")
                        ?? FindNearestLocation(playerPos, "Plains");

                case Heightmap.Biome.Mistlands:
                    return FindNearestLocation(playerPos, "Mistlands");

                case Heightmap.Biome.AshLands:
                    return FindNearestLocation(playerPos, "AshLands")
                        ?? FindNearestBiomePoint(playerPos, Heightmap.Biome.AshLands);

                case Heightmap.Biome.Ocean:
                    return FindOceanPoint(playerPos);

                case Heightmap.Biome.DeepNorth:
                    return FindNearestLocation(playerPos, "DeepNorth")
                        ?? FindNearestBiomePoint(playerPos, Heightmap.Biome.DeepNorth);

                default:
                    return null;
            }
        }

        private static Vector3? FindNearestLocation(Vector3 playerPos, string biome, string nameFilter = null)
        {
            float bestDist = float.MaxValue;
            Vector3? result = null;

            foreach (var entry in ZoneSystem.instance.m_locationInstances)
            {
                var location = entry.Value.m_location;
                var position = entry.Value.m_position;

                if (!location.m_biome.ToString().Contains(biome)) continue;
                if (nameFilter != null && !location.m_prefabName.ToLower().Contains(nameFilter.ToLower())) continue;

                float dx = position.x - playerPos.x;
                float dz = position.z - playerPos.z;
                float dist = dx * dx + dz * dz;

                if (dist >= bestDist) continue;
                bestDist = dist;
                result = position;
            }

            return result;
        }

        private static Vector3? FindNearestBiomePoint(Vector3 origin, Heightmap.Biome targetBiome, float maxRadius = 800f)
        {
            const float step = 50f;
            const int angularSteps = 8;

            for (float r = step; r <= maxRadius; r += step)
            {
                for (int i = 0; i < angularSteps; i++)
                {
                    float angle = 2f * Mathf.PI * i / angularSteps;
                    float x = origin.x + r * Mathf.Cos(angle);
                    float z = origin.z + r * Mathf.Sin(angle);

                    if (WorldGenerator.instance.GetBiome(x, z) == targetBiome)
                        return new Vector3(x, origin.y, z);
                }
            }

            return null;
        }

        private static Vector3? FindOceanPoint(Vector3 playerPos)
        {
            const int angularSteps = 8;
            const float probeStep = 20f;
            const float maxDist = 400f;
            const float extraOffset = 50f;

            for (int i = 0; i < angularSteps; i++)
            {
                float angle = 2f * Mathf.PI * i / angularSteps;
                float dx = Mathf.Cos(angle);
                float dz = Mathf.Sin(angle);

                for (float d = probeStep; d <= maxDist; d += probeStep)
                {
                    float x = playerPos.x + dx * d;
                    float z = playerPos.z + dz * d;

                    if (WorldGenerator.instance.GetBiome(x, z) == Heightmap.Biome.Ocean)
                    {
                        return new Vector3(
                            playerPos.x + dx * (d + extraOffset),
                            ZoneSystem.instance.m_waterLevel,
                            playerPos.z + dz * (d + extraOffset)
                        );
                    }
                }
            }

            return null;
        }

        private static IEnumerator SpawnFulingsAfterTeleport()
        {
            yield return new WaitForSeconds(3f);
            Player player = Player.m_localPlayer;
            if (player == null) yield break;

            GameObject prefab = ZNetScene.instance.GetPrefab("Fuling");
            if (!prefab) yield break;

            for (int i = 0; i < 2; i++)
            {
                float angle = Mathf.PI * i;
                Vector3 pos = player.transform.position + new Vector3(Mathf.Cos(angle) * 4f, 0f, Mathf.Sin(angle) * 4f);
                GameObject go = GameObject.Instantiate(prefab, pos, Quaternion.identity);
                go.GetComponent<Character>()?.SetLevel(2);
                go.GetComponent<BaseAI>()?.Alert();
                MonsterAI monsterAI = go.GetComponent<MonsterAI>();
                if (monsterAI != null)
                    typeof(MonsterAI)
                        .GetField("m_targetCreature", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        ?.SetValue(monsterAI, player);
            }
        }
    }
}
