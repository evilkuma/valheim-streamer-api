using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace ValheimStreamerApi
{
    public class TrapApiController : ApiController
    {
        private class RpcMeteorShowerData
        {
            [JsonProperty("count")]  public int   count  { get; set; }
            [JsonProperty("radius")] public float radius { get; set; }
        }

        public TrapApiController()
        {
            http = "/api/trap";
            rpc  = "ValheimStreamerApi/api/trap";

            RegisterHttpAction<PlayerActionData>("meteor-shower",   ActionMeteorShower);
            RegisterRpcAction<RpcMeteorShowerData>("meteor-shower", MeteorShower);

            RegisterHttpAction<PlayerActionData>("thunderstorm",    ActionThunderstorm);
            RegisterRpcAction<object>("thunderstorm",               Thunderstorm);

            RegisterHttpAction<PlayerActionData>("freeze",          ActionFreeze);
            RegisterRpcAction<object>("freeze",                     Freeze);

            RegisterHttpAction<PlayerActionData>("noise-curse",       ActionNoiseCurse);
            RegisterRpcAction<object>("noise-curse",                 NoiseCurse);
        }

        // === Server (HTTP) ===

        private async Task<object> ActionMeteorShower(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "meteor-shower",
                new RpcMeteorShowerData { count = 8, radius = 8f });
            return JsonParser.Parse<object>(zData);
        }

        private async Task<object> ActionThunderstorm(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "thunderstorm", new {});
            return JsonParser.Parse<object>(zData);
        }

        private async Task<object> ActionFreeze(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "freeze", new {});
            return JsonParser.Parse<object>(zData);
        }

        private async Task<object> ActionNoiseCurse(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "noise-curse", new {});
            return JsonParser.Parse<object>(zData);
        }

        // === Client (RPC) ===

        private object NoiseCurse(object _data)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return new { status = "not a player" };

            Vector3 playerPos = player.transform.position;
            const float radius = 100f;
            var targetField = typeof(MonsterAI).GetField("m_targetCreature", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            int alerted = 0;

            foreach (var character in Character.GetAllCharacters())
            {
                if (character is Player) continue;
                if (character.m_faction == Character.Faction.Players) continue;

                BaseAI ai = character.GetComponent<BaseAI>();
                if (ai == null) continue;

                if (Vector3.Distance(character.transform.position, playerPos) > radius) continue;

                ai.Alert();

                MonsterAI monsterAI = character.GetComponent<MonsterAI>();
                if (monsterAI != null)
                    targetField?.SetValue(monsterAI, player);

                alerted++;
            }

            return new { status = "ok", alerted };
        }

        private object Thunderstorm(object _data)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return new { status = "not a player" };

            player.StartCoroutine(ThunderstormRoutine(player));
            return new { status = "ok" };
        }

        private static IEnumerator ThunderstormRoutine(Player player)
        {
            GameObject lightningPrefab = ZNetScene.instance.GetPrefab("lightningAOE");
            if (!lightningPrefab) yield break;

            EnvMan.instance.m_debugEnv = "ThunderStorm";
            yield return new WaitForSeconds(15f);

            const int   strikes      = 7;
            const float radius       = 6f;
            const float minInterval  = 2.5f;
            const float maxInterval  = 5.0f;

            for (int i = 0; i < strikes; i++)
            {
                if (player == null) break;

                float angle = Random.Range(0f, 2f * Mathf.PI);
                float r     = Random.Range(1f, radius);
                Vector3 pos = player.transform.position + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

                Object.Instantiate(lightningPrefab, pos, Quaternion.identity);

                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            }

            EnvMan.instance.m_debugEnv = "";
        }

        private object Freeze(object _data)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return new { status = "not a player" };

            player.StartCoroutine(FreezeRoutine(player));
            return new { status = "ok" };
        }

        private static IEnumerator FreezeRoutine(Player player)
        {
            EnvMan.instance.m_debugEnv = "SnowStorm";

            StatusEffect se = ObjectDB.instance.GetStatusEffect("Freezing".GetStableHashCode(true));
            if (se != null)
                player.GetSEMan().AddStatusEffect(se);

            yield return new WaitForSeconds(90f);

            EnvMan.instance.m_debugEnv = "";
            player.GetSEMan().RemoveStatusEffect("Freezing".GetStableHashCode(true));
        }

        private object MeteorShower(RpcMeteorShowerData data)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return new { status = "not a player" };

            player.StartCoroutine(MeteorShowerRoutine(player, data.count, data.radius));
            return new { status = "ok" };
        }

        private static IEnumerator MeteorShowerRoutine(Player player, int count, float radius)
        {
            GameObject meteorPrefab = ZNetScene.instance.GetPrefab("projectile_meteor");
            if (!meteorPrefab) yield break;

            for (int i = 0; i < count; i++)
            {
                if (player == null) yield break;

                float angle = Random.Range(0f, 2f * Mathf.PI);
                float r = Random.Range(0f, radius);
                Vector3 groundPos = player.transform.position + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

                Vector3 spawnPos = groundPos + Vector3.up * 35f;
                Vector3 velocity = Vector3.down * 28f;

                ZNetView.StartGhostInit();
                GameObject meteor = Object.Instantiate(meteorPrefab, spawnPos, Quaternion.LookRotation(Vector3.down));
                ZNetView.FinishGhostInit();

                Projectile proj = meteor.GetComponent<Projectile>();
                if (proj != null)
                    proj.Setup(player, velocity, 0f, new HitData(), null, null);
                else
                {
                    Rigidbody rb = meteor.GetComponent<Rigidbody>();
                    if (rb != null) { rb.isKinematic = false; rb.useGravity = true; rb.linearVelocity = velocity; }
                }

                player.StartCoroutine(MonitorMeteor(meteor, groundPos, player));

                yield return new WaitForSeconds(Random.Range(0.4f, 1.1f));
            }
        }

        private static IEnumerator MonitorMeteor(GameObject meteor, Vector3 groundPos, Player player)
        {
            float impactY  = groundPos.y + 0.5f;
            float timeout  = 6f;
            float elapsed  = 0f;

            while (meteor != null && elapsed < timeout)
            {
                if (meteor.transform.position.y <= impactY)
                    break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            Vector3 impactPos = new Vector3(groundPos.x, groundPos.y, groundPos.z);

            if (meteor != null)
                Object.Destroy(meteor);

            GameObject hitFx = ZNetScene.instance.GetPrefab("fx_goblinking_meteor_hit");
            if (hitFx) Object.Instantiate(hitFx, impactPos, Quaternion.identity);

            Collider[] cols = Physics.OverlapSphere(impactPos, 5f);
            var damaged = new HashSet<IDestructible>();
            foreach (var col in cols)
            {
                var dest = col.GetComponentInParent<IDestructible>();
                if (dest == null || !damaged.Add(dest)) continue;

                HitData hit = new HitData();
                hit.m_damage.m_fire    = 60f;
                hit.m_damage.m_blunt   = 35f;
                hit.m_damage.m_chop    = 60f;
                hit.m_damage.m_pickaxe = 50f;
                hit.m_toolTier = 3;
                hit.m_dir      = Vector3.down;
                hit.m_point    = impactPos;
                dest.Damage(hit);
            }
        }
    }
}
