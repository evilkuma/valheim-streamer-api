using UnityEngine;

namespace ValheimStreamerApi
{
    public class FollowerBehaviour : MonoBehaviour
    {
        private void Awake()
        {
            var ai = GetComponent<MonsterAI>();
            if (ai != null)
            {
                ai.m_attackPlayerObjects               = false;
                ai.m_enableHuntPlayer                  = false;
                ai.m_fleeIfHurtWhenTargetCantBeReached = true;

                foreach (string itemName in new[]{
                    "CookedMeat",
                    "CookedDeerMeat",
                    "CookedWolfMeat",
                    "CookedLoxMeat",
                    "FishCooked",
                    "SerpentMeatCooked",
                    "NeckTailGrilled"
                })
                {
                    ai.m_consumeItems.Add(ZNetScene.instance.GetPrefab(itemName).GetComponent<ItemDrop>());
                }
            }

            var character = GetComponent<Character>();
            if (character != null)
            {
                character.m_name = "Ульф";
                character.m_faction = Character.Faction.Players;
                character.SetTamed(true);
            }

            var drop = GetComponent<CharacterDrop>();
            if (drop != null)
            {
                drop.m_drops.Clear();
            }

            var tameable = GetComponent<Tameable>();
            if (tameable != null)
            {
                tameable.m_commandable = true;
            }
        }
    }
}
