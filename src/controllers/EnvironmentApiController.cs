using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimStreamerApi
{
    public class EnvironmentApiController : ApiController
    {
        private const string FogEnvName        = "ValheimStreamerApi_FogOfWar";
        private const string PolarNightEnvName = "ValheimStreamerApi_PolarNight";

        public EnvironmentApiController()
        {
            http = "/api/environment";
            rpc  = "ValheimStreamerApi/api/environment";

            RegisterHttpAction<PlayerActionData>("fog-of-war",   ActionFogOfWar);
            RegisterRpcAction<object>("fog-of-war",              FogOfWar);

            RegisterHttpAction<PlayerActionData>("polar-night",  ActionPolarNight);
            RegisterRpcAction<object>("polar-night",             PolarNight);

            RegisterHttpAction<PlayerActionData>("sunny-day", ActionSunnyDay);
        }

        // === Server (HTTP) ===

        private async Task<object> ActionFogOfWar(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "fog-of-war", new {});
            return JsonParser.Parse<object>(zData);
        }

        private async Task<object> ActionPolarNight(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "polar-night", new {});
            return JsonParser.Parse<object>(zData);
        }

        private async Task<object> ActionSunnyDay(PlayerActionData _data)
        {
            double dayLength  = EnvMan.instance.m_dayLengthSec;
            long   currentDay = (long)(ZNet.instance.GetTimeSeconds() / dayLength);
            ZNet.instance.SetNetTime((currentDay + 0.25) * dayLength);
            EnvMan.instance.m_debugEnv = "";
            return new { status = "ok" };
        }

        // === Client (RPC) ===

        private object FogOfWar(object _data)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return new { status = "not a player" };

            EnsureFogEnvironment();
            player.StartCoroutine(FogRoutine());
            return new { status = "ok" };
        }

        private static void EnsureFogEnvironment()
        {
            if (EnvMan.instance.m_environments.Any(e => e.m_name == FogEnvName))
                return;

            var fogColor    = new Color(0.45f, 0.48f, 0.52f, 1f);
            var fogColorSun = new Color(0.50f, 0.50f, 0.55f, 1f);

            var env = new EnvSetup
            {
                m_name               = FogEnvName,
                m_psystems           = new GameObject[0],
                m_fogDensityDay      = 0.12f,
                m_fogDensityNight    = 0.15f,
                m_fogDensityEvening  = 0.13f,
                m_fogDensityMorning  = 0.13f,
                m_fogColorDay        = fogColor,
                m_fogColorNight      = fogColor,
                m_fogColorEvening    = fogColor,
                m_fogColorMorning    = fogColor,
                m_fogColorSunDay     = fogColorSun,
                m_fogColorSunNight   = fogColorSun,
                m_fogColorSunEvening = fogColorSun,
                m_fogColorSunMorning = fogColorSun,
                m_lightIntensityDay  = 0.4f,
                m_lightIntensityNight = 0f,
                m_rainCloudAlpha     = 0f,
                m_ambColorDay        = new Color(0.35f, 0.35f, 0.40f),
                m_ambColorNight      = new Color(0.05f, 0.05f, 0.08f),
            };

            EnvMan.instance.AppendEnvironment(env);
        }

        private static IEnumerator FogRoutine()
        {
            EnvMan.instance.m_debugEnv = FogEnvName;
            yield return new WaitForSeconds(60f);
            EnvMan.instance.m_debugEnv = "";
        }

        private object PolarNight(object _data)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return new { status = "not a player" };

            EnsurePolarNightEnvironment();
            player.StartCoroutine(PolarNightRoutine());
            return new { status = "ok" };
        }

        private static void EnsurePolarNightEnvironment()
        {
            if (EnvMan.instance.m_environments.Any(e => e.m_name == PolarNightEnvName))
                return;

            var darkFog = new Color(0f, 0f, 0.02f, 1f);

            var env = new EnvSetup
            {
                m_name                = PolarNightEnvName,
                m_psystems            = new GameObject[0],
                m_lightIntensityDay   = 0f,
                m_lightIntensityNight = 0f,
                m_ambColorDay         = Color.black,
                m_ambColorNight       = Color.black,
                m_fogDensityDay       = 0.05f,
                m_fogDensityNight     = 0.05f,
                m_fogDensityEvening   = 0.05f,
                m_fogDensityMorning   = 0.05f,
                m_fogColorDay         = darkFog,
                m_fogColorNight       = darkFog,
                m_fogColorEvening     = darkFog,
                m_fogColorMorning     = darkFog,
                m_fogColorSunDay      = darkFog,
                m_fogColorSunNight    = darkFog,
                m_fogColorSunEvening  = darkFog,
                m_fogColorSunMorning  = darkFog,
                m_rainCloudAlpha      = 0.9f,
            };

            EnvMan.instance.AppendEnvironment(env);
        }

        private static IEnumerator PolarNightRoutine()
        {
            EnvMan.instance.m_debugEnv = PolarNightEnvName;
            yield return new WaitForSeconds(120f);
            EnvMan.instance.m_debugEnv = "";
        }
    }
}
