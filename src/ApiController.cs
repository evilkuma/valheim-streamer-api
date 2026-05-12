using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ValheimStreamerApi
{
    public abstract class ApiController
    {
        protected string http = "";
        protected string rpc = "";

        private readonly Dictionary<string, Func<HttpListenerRequest, Task<object>>> _httpActions = new();
        private readonly Dictionary<string, Func<string, object>> _rpcActions = new();

        public async Task<object> HandleHttp(object sender, EventArgs args)
        {
            if (args is not HttpEventArgs dataArgs)
                return new { error = "no data" };

            string action = dataArgs.Action == null ? "main" : dataArgs.Action.ToLower();

            if (_httpActions.TryGetValue(action, out var handler))
                return await handler(dataArgs.Request);

            return new { error = "undefined action" };
        }

        public void HandleRpc(ZPackage pkg)
        {
            var (action, json) = RpcManager.Unpack(pkg);
            object result = new { status = "not action" };

            if (action != null && _rpcActions.TryGetValue(action, out var handler))
                result = handler(json);

            RpcManager.SendMessage(rpc, RpcManager.GetServerId(), result);
        }

        protected void RegisterHttpAction<TData>(string action, Func<TData, Task<object>> handler)
        {
            _httpActions[action.ToLower()] = async request =>
            {
                var data = JsonParser.Parse<TData>(request);
                return await handler(data);
            };
        }

        protected void RegisterRpcAction<TData>(string action, Func<TData, object> handler)
        {
            _rpcActions[action.ToLower()] = json => handler(RpcManager.UnpackData<TData>(json));
        }

        public static EventManager Init()
        {
            var events = new EventManager();
            var controllerType = typeof(ApiController);

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => !t.IsAbstract && controllerType.IsAssignableFrom(t)))
            {
                var controller = (ApiController)Activator.CreateInstance(type);

                if (!string.IsNullOrEmpty(controller.http))
                {
                    events.Add(controller.http, controller.HandleHttp);
                    Log.LogInfo($"HTTP registered: {controller.http} ({type.Name})");
                }

                if (!string.IsNullOrEmpty(controller.rpc))
                {
                    var handler = new RPCHandler(controller.rpc);
                    ZRoutedRpc.instance.Register(controller.rpc, new Action<long, ZPackage>(handler.Handle));
                    RpcManager.AddListener(controller.rpc, controller.HandleRpc);
                    Log.LogInfo($"RPC registered: {controller.rpc} ({type.Name})");
                }
            }

            return events;
        }
    }

    public class PlayerActionData
    {
        [JsonProperty("playerName")] public string playerName { get; set; }
    }

    public abstract class ApiController<THttpData> : ApiController
    {
        protected ApiController()
        {
            RegisterHttpAction<THttpData>("main", Action);
        }

        protected virtual Task<object> Action(THttpData data)
        {
            return Task.FromResult<object>(new { error = "undefined action" });
        }
    }
}
