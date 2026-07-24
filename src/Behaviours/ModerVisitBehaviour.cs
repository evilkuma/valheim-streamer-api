using System.Collections;
using System.Reflection;
using UnityEngine;

namespace ValheimStreamerApi
{
    public class ModerVisitBehaviour : MonoBehaviour
    {
        private static readonly FieldInfo TargetCreatureField =
            typeof(MonsterAI).GetField("m_targetCreature",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public void Init(Player owner)
        {
            var drop = GetComponent<CharacterDrop>();
            if (drop != null) drop.m_drops.Clear();

            var monsterAI = GetComponent<MonsterAI>();
            if (monsterAI != null)
            {
                monsterAI.m_attackPlayerObjects = true;
                monsterAI.m_enableHuntPlayer    = true;
                TargetCreatureField?.SetValue(monsterAI, owner);
            }

            StartCoroutine(LifetimeRoutine());
        }

        private IEnumerator LifetimeRoutine()
        {
            yield return new WaitForSeconds(20f);
            Object.Destroy(gameObject);
        }
    }
}
