using UnityEngine;

namespace ValheimStreamerApi
{
    [DefaultExecutionOrder(9999)]
    public class FriendlyNpcBehaviour : MonoBehaviour
    {
        private const string FactionKey    = "streamer_faction";
        private const string FollowNameKey = "streamer_follow";
        private const string NpcNameKey    = "streamer_npc_name";

        private ZNetView  _nview;
        private Character _character;
        private MonsterAI _ai;
        private string    _followName;

        private void Awake()
        {
            _nview     = GetComponent<ZNetView>();
            _character = GetComponent<Character>();
            _ai        = GetComponent<MonsterAI>();
        }

        // Вызывается сразу после Instantiate на клиенте-владельце
        public void Init(string npcName, string followPlayerName)
        {
            _followName = followPlayerName;
            _nview.GetZDO().Set(FactionKey,    (int)Character.Faction.Players);
            _nview.GetZDO().Set(FollowNameKey, followPlayerName);
            _nview.GetZDO().Set(NpcNameKey,    npcName);
            ApplyFromZdo();
        }

        private void Start()
        {
            // Все клиенты (включая владельца) читают ZDO: к этому моменту данные уже записаны
            _followName = _nview.GetZDO().GetString(FollowNameKey, "");
            ApplyFromZdo();
        }

        private void Update()
        {
            // AI гоняет только владелец; остальные получают трансформ через сеть
            if (!_nview.IsOwner() || _ai == null || string.IsNullOrEmpty(_followName)) return;
            _ai.SetFollowTarget(FindPlayer(_followName)?.gameObject);
        }

        private void ApplyFromZdo()
        {
            int factionVal = _nview.GetZDO().GetInt(FactionKey, -1);
            if (factionVal >= 0)
                _character.m_faction = (Character.Faction)factionVal;

            string name = _nview.GetZDO().GetString(NpcNameKey, "");
            if (!string.IsNullOrEmpty(name))
                _character.m_name = name;
        }

        private static Player FindPlayer(string name)
        {
            foreach (var p in Player.GetAllPlayers())
                if (p.GetPlayerName() == name)
                    return p;
            return null;
        }
    }
}
