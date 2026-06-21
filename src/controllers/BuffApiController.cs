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

            RegisterHttpAction<PlayerActionData>("berserker",       ActionBerserker);
            RegisterRpcAction<object>("berserker",                Berserker);

            RegisterHttpAction<PlayerActionData>("weakness-curse", ActionWeaknessCurse);
            RegisterRpcAction<object>("weakness-curse",            WeaknessCurse);

            RegisterHttpAction<PlayerActionData>("invisibility",   ActionInvisibility);
            RegisterRpcAction<object>("invisibility",              Invisibility);
        }

        // === Server (HTTP) ===

        private async Task<object> ActionBerserker(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "berserker", new {});
            return JsonParser.Parse<object>(zData);
        }

        private async Task<object> ActionWeaknessCurse(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "weakness-curse", new {});
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
        
        private object WeaknessCurse(object _data)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return new { status = "not a player" };

            var se    = ScriptableObject.CreateInstance<WeaknessCurseSE>();
            se.name   = "ValheimStreamerApi_WeaknessCurse";
            se.m_name = "Проклятие слабости";
            se.m_ttl  = 45f;

            var poison = ObjectDB.instance.GetStatusEffect("Poison".GetStableHashCode(true));
            if (poison != null) se.m_icon = poison.m_icon;

            player.GetSEMan().AddStatusEffect(se);
            return new { status = "ok" };
        }

        private async Task<object> ActionInvisibility(PlayerActionData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "invisibility", new {});
            return JsonParser.Parse<object>(zData);
        }

        private object Invisibility(object _data)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return new { status = "not a player" };

            var se    = ScriptableObject.CreateInstance<InvisibilitySE>();
            se.name   = "ValheimStreamerApi_Invisibility";
            se.m_name = "Невидимость";
            se.m_ttl  = 30f;

            var smoked = ObjectDB.instance.GetStatusEffect("Smoked".GetStableHashCode(true));
            if (smoked != null) se.m_icon = smoked.m_icon;

            player.GetSEMan().AddStatusEffect(se);
            return new { status = "ok" };
        }
    }

    public class InvisibilitySE : StatusEffect
    {
        public override void ModifyNoise(float baseNoise, ref float noise)   => noise   = 0f;
        public override void ModifyStealth(float baseStealth, ref float stealth) => stealth = 0f;

        public override string GetIconText() => $"{Mathf.CeilToInt(m_ttl - m_time)}s";
    }

    public class WeaknessCurseSE : StatusEffect
    {
        public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
        {
            hitData.m_damage.Modify(0.25f);
        }

        public override string GetIconText()
        {
            return $"{Mathf.CeilToInt(m_ttl - m_time)}s";
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
