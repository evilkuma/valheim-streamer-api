using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ValheimStreamerApi
{
    public class DebugApiController : ApiController<DebugApiController.ActionMainData>
    {
        public class ActionMainData
        {
            [JsonProperty("playerName")] public string playerName { get; set; }
            [JsonProperty("message")]    public string message    { get; set; }
        }

        private class RpcRequestMainData
        {
            [JsonProperty("message")] public string message { get; set; }
        }

        private class RpcResponseMainData
        {
            [JsonProperty("status")] public string status { get; set; }
        }

        public DebugApiController()
        {
            http = "/api/debug";
            rpc  = "ValheimStreamerApi/api/debug";

            RegisterRpcAction<RpcRequestMainData>("terminal-message", TerminalMessage);
        }

        // === Server (HTTP) ===

        protected override async Task<object> Action(ActionMainData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(
                rpc, targetPeer.m_uid, "terminal-message",
                new RpcRequestMainData { message = data.message }
            );
            return JsonParser.Parse<RpcResponseMainData>(zData);
        }

        // === Client (RPC) ===

        private object TerminalMessage(RpcRequestMainData data)
        {
            Log.LogInfo($"Получили тестовое сообщение от сервера: {data.message}");
            return new { status = "ok" };
        }
    }
}
