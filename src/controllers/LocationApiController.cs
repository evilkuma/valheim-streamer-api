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

        public LocationApiController()
        {
            http = "/api/location";
            rpc  = "ValheimStreamerApi/api/location";

            RegisterHttpAction<ActionTeleportToBiomeData>("teleport-to-biome", ActionTeleportToBiome);
            RegisterRpcAction<ActionTeleportToData>("teleport-to", TeleportTo);
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
    }
}
