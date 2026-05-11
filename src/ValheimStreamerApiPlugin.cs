using BepInEx;
using BepInEx.Configuration;

namespace ValheimStreamerApi
{
    [BepInPlugin("ru.evilkuma.valheimstreamerapi", "Valheim Streamer API", "1.0.0")]
    public class ValheimStreamerApiPlugin : BaseUnityPlugin
    {
        public static ValheimStreamerApiPlugin Instance;

        private ConfigEntry<int> _port;
        private HttpServer _httpServer;

        private void Awake()
        {
            Instance = this;
            Log.Initialize(base.Logger);
            _port = Config.Bind("Server", "Port", 8080, "HTTP порт. Требует перезапуска.");
            RpcManager.Initialize();
        }

        // Вызывается из Game.Start Postfix — ZNet.m_isServer уже доступен
        internal void OnGameStart()
        {
            var httpEvents = ApiController.Init();

            if (ZNet.instance != null && ZNet.instance.IsServer())
            {
                _httpServer = new HttpServer(_port.Value, httpEvents);
                _httpServer.Start();
                Log.LogInfo($"HTTP Server: http://localhost:{_port.Value}");
            }

            Log.LogInfo("=== Valheim Streamer API загружен ===");
        }

        private void OnDestroy()
        {
            _httpServer?.Dispose();
        }
    }
}
