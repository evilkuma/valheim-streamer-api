using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json;

namespace ValheimStreamerApi
{
    internal class RpcPacket<TData>
    {
        [JsonProperty("action")] public string action { get; set; }
        [JsonProperty("data")]   public TData  data   { get; set; }
    }

    public static class RpcManager
    {
        private static readonly Dictionary<string, TaskCompletionSource<ZPackage>> _tasks = new();
        private static readonly Dictionary<string, Action<ZPackage>> _events = new();

        public static void Initialize()
        {
            Harmony.CreateAndPatchAll(typeof(RPCRegistration));
        }

        public static void AddListener(string name, Action<ZPackage> func)
        {
            _events[name] = func;
        }

        internal static (string action, string json) Unpack(ZPackage pkg)
        {
            string json = pkg.ReadString();
            var envelope = JsonParser.Parse<RpcPacket<object>>(json);
            return (envelope?.action?.ToLower(), json);
        }

        internal static TData UnpackData<TData>(string json)
        {
            var envelope = JsonParser.Parse<RpcPacket<TData>>(json);
            return envelope.data;
        }

        public static void Emit(string name, long sender, ZPackage pkg)
        {
            string taskId = $"{name}-{sender}";
            if (_tasks.TryGetValue(taskId, out var tcs))
            {
                _tasks.Remove(taskId);
                tcs.TrySetResult(pkg);
                return;
            }

            if (_events.TryGetValue(name, out var func))
                func(pkg);
        }

        public static ZNetPeer FindPlayerByName(string playerName)
        {
            foreach (var peer in ZNet.instance.GetPeers())
            {
                if (peer.m_playerName != null &&
                    peer.m_playerName.Equals(playerName))
                    return peer;
            }
            return null;
        }

        public static long GetServerId()
        {
            foreach (var peer in ZNet.instance.GetPeers())
            {
                if (peer.m_server)
                    return peer.m_uid;
            }
            return 0L;
        }

        public static Task<ZPackage> SendMessageAsync<TData>(
            string eventName, long playerUID, string action, TData data, int timeoutMs = 10000)
        {
            return SendMessageAsync(eventName, playerUID,
                new RpcPacket<TData> { action = action, data = data }, timeoutMs);
        }

        public static async Task<ZPackage> SendMessageAsync(string eventName, long playerUID, object data, int timeoutMs = 10000)
        {
            var tcs = new TaskCompletionSource<ZPackage>();
            string taskId = $"{eventName}-{playerUID}";
            _tasks[taskId] = tcs;

            ZPackage package = new ZPackage();
            package.Write(JsonParser.Serialize(data));
            ZRoutedRpc.instance.InvokeRoutedRPC(playerUID, eventName, package);

            var timeoutTask = Task.Delay(timeoutMs);
            var completed = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completed == timeoutTask)
            {
                _tasks.Remove(taskId);
                throw new TimeoutException($"RPC {eventName} не ответил за {timeoutMs}мс");
            }

            return await tcs.Task;
        }

        public static void SendMessage(string eventName, long playerUID, ZPackage package)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(playerUID, eventName, package);
        }

        public static void SendMessage(string eventName, long playerUID, object data)
        {
            ZPackage package = new ZPackage();
            package.Write(JsonParser.Serialize(data));
            ZRoutedRpc.instance.InvokeRoutedRPC(playerUID, eventName, package);
        }
    }

    public class RPCHandler
    {
        private readonly string Name;

        public RPCHandler(string name)
        {
            Name = name;
        }

        public void Handle(long sender, ZPackage pkg)
        {
            RpcManager.Emit(Name, sender, pkg);
        }
    }

    [HarmonyPatch(typeof(Game), "Start")]
    public static class RPCRegistration
    {
        private static void Postfix()
        {
            ValheimStreamerApiPlugin.Instance?.OnGameStart();
        }
    }
}
