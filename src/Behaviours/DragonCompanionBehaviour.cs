using System.Collections;
using UnityEngine;

namespace ValheimStreamerApi
{
    public class DragonCompanionBehaviour : MonoBehaviour
    {
        private const float  Lifetime    = 300f;
        private const string BaseName    = "Дракон";
        private const string ZdoExpireAt = "StreamerApi.DragonExpireAt";

        private Character _character;
        private ZNetView  _nview;

        private void Awake()
        {
            _character = GetComponent<Character>();
            _nview     = GetComponent<ZNetView>();

            if (_character != null)
            {
                _character.m_faction = Character.Faction.Players;
                _character.SetTamed(true);
                _character.SetMaxHealth(1000f);
                _character.m_health = 1000f;
                _character.m_name   = BaseName;
            }

            var monsterAI = GetComponent<MonsterAI>();
            if (monsterAI != null)
            {
                monsterAI.m_attackPlayerObjects               = false;
                monsterAI.m_enableHuntPlayer                  = false;
                monsterAI.m_fleeIfHurtWhenTargetCantBeReached = true;
            }

            var drop = GetComponent<CharacterDrop>();
            if (drop != null) drop.m_drops.Clear();

            var tameable = GetComponent<Tameable>();
            if (tameable != null) tameable.m_commandable = true;
        }

        // Вызывается только при первом спавне из SpawnApiController
        public void Init(Player owner)
        {
            if (_nview != null && _nview.IsValid())
                _nview.GetZDO().Set(ZdoExpireAt, (float)(ZNet.instance.GetTimeSeconds() + Lifetime));

            GetComponent<Tameable>()?.Command(owner);
        }

        private void Start()
        {
            if (_nview == null || !_nview.IsValid()) return;

            float expireAt = _nview.GetZDO().GetFloat(ZdoExpireAt, 0f);
            if (expireAt <= 0f)
            {
                expireAt = (float)(ZNet.instance.GetTimeSeconds() + Lifetime);
                _nview.GetZDO().Set(ZdoExpireAt, expireAt);
            }

            float remaining = expireAt - (float)ZNet.instance.GetTimeSeconds();
            if (remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            StartCoroutine(LifetimeRoutine(remaining));
            StartCoroutine(NameUpdateRoutine(expireAt));
        }

        private IEnumerator LifetimeRoutine(float remaining)
        {
            yield return new WaitForSeconds(remaining);
            if (this != null) Destroy(gameObject);
        }

        private IEnumerator NameUpdateRoutine(float expireAt)
        {
            while (this != null && _character != null)
            {
                float left = Mathf.Max(0f, expireAt - (float)ZNet.instance.GetTimeSeconds());
                _character.m_name = $"{BaseName} [{Mathf.CeilToInt(left)}с]";
                yield return new WaitForSeconds(1f);
            }
        }
    }
}
