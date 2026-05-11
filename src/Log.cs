using BepInEx.Logging;

namespace ValheimStreamerApi
{
    public static class Log
    {
        private static ManualLogSource _logger;

        public static void Initialize(ManualLogSource logger)
        {
            _logger = logger;
        }

        public static void LogInfo(string message)
        {
            _logger.LogInfo(message);
        }

        public static void LogError(string message)
        {
            _logger.LogError(message);
        }
    }
}
