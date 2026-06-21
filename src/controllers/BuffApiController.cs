using System.Threading.Tasks;
using UnityEngine;

namespace ValheimStreamerApi
{
    public class BuffApiController : ApiController
    {
        public BuffApiController()
        {
            http = "/api/buff";
            rpc  = "ValheimStreamerApi/api/buff";

            RegisterHttpAction<PlayerActionData>("berserker", ActionBerserker);
            RegisterRpcAction<object>("berserker",            Berserker);
        }

        // === Server (HTTP) ===

        private async Task<object> ActionBerserker(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "berserker", new {});
            return JsonParser.Parse<object>(zData);
        }

        // === Client (RPC) ===

        private object Berserker(object _data)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return new { status = "not a player" };

            var se    = ScriptableObject.CreateInstance<BerserkerBuffSE>();
            se.name   = "ValheimStreamerApi_BerserkerBuff";
            se.m_name = "Берсерк";
            se.m_ttl  = 30f;

            var adrenaline = ObjectDB.instance.GetStatusEffect("AdrenalineRush".GetStableHashCode(true));
            if (adrenaline != null) se.m_icon = adrenaline.m_icon;

            player.GetSEMan().AddStatusEffect(se);
            return new { status = "ok" };
        }
    }

    public class BerserkerBuffSE : StatusEffect
    {
        public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
        {
            hitData.m_damage.Modify(2f);
        }

        public override void ModifyAttackStaminaUsage(float baseStaminaUse, ref float staminaUse)
        {
            staminaUse /= 1.5f;
        }

        public override string GetIconText()
        {
            return $"{Mathf.CeilToInt(m_ttl - m_time)}s";
        }
    }
}
