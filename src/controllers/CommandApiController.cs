using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ValheimStreamerApi
{
    public class CommandApiController : ApiController<CommandApiController.ActionMainData>
    {
        public class ActionMainData
        {
            [JsonProperty("playerName")] public string playerName { get; set; }
            [JsonProperty("command")]    public string command    { get; set; }
            [JsonProperty("data")]       public string data       { get; set; }
        }

        private class RpcResponseData
        {
            [JsonProperty("status")] public string status { get; set; }
        }

        public CommandApiController()
        {
            http = "/api/command";
            rpc  = "ValheimStreamerApi/api/command";

            RegisterRpcAction<object>("undress", Undress);
        }

        // === Server (HTTP) ===

        protected override async Task<object> Action(ActionMainData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, data.command, new {});
            return JsonParser.Parse<RpcResponseData>(zData);
        }

        // === Client (RPC) ===

        private object Undress(object _data)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return new { status = "not a player" };

            var inventory = player.GetInventory();
            foreach (var item in inventory.GetAllItems())
            {
                if (item.m_equipped)
                    player.UnequipItem(item, true);
            }

            return new { status = "ok" };
        }
    }
}
