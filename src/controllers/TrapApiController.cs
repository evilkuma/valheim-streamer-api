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

            RegisterHttpAction<PlayerActionData>("meteor-shower", ActionMeteorShower);
            RegisterRpcAction<RpcMeteorShowerData>("meteor-shower", MeteorShower);
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

        // === Client (RPC) ===

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
