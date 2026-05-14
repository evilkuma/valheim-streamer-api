using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ValheimStreamerApi
{
    public class FollowerBehaviour : MonoBehaviour
    {
        private Humanoid  _humanoid;
        private Container _container;
        private MonsterAI _ai;
        private Character _selfCharacter;
        private Character _lastFollowTarget;
        private Vector3   _stayPosition;

        private const float MaxChaseDistance = 15f;
        private const float ThreatScanRange  = 20f;

        private static readonly FieldInfo ContainerInventoryField =
            typeof(Container).GetField("m_inventory",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static readonly FieldInfo[] EquipSlotFields = GetEquipSlotFields();

        private static readonly MethodInfo SetupEquipmentMethod =
            typeof(Humanoid).GetMethod("SetupEquipment",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        // Вызов SetTarget идёт через Harmony-патч — он проверяет IsValidTarget перед установкой.
        private static readonly MethodInfo SetTargetMethod =
            typeof(MonsterAI).GetMethod("SetTarget",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(Character) }, null);

        // SetTarget не умеет очищать цель (срабатывает только при m_targetCreature == null),
        // поэтому сброс делаем прямым доступом к полю.
        private static readonly FieldInfo TargetCreatureField =
            typeof(MonsterAI).GetField("m_targetCreature",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly HashSet<ItemDrop.ItemData.ItemType> EquippableTypes =
            new HashSet<ItemDrop.ItemData.ItemType>
            {
                ItemDrop.ItemData.ItemType.Helmet,
                ItemDrop.ItemData.ItemType.Chest,
                ItemDrop.ItemData.ItemType.Legs,
                ItemDrop.ItemData.ItemType.Shoulder,
                ItemDrop.ItemData.ItemType.OneHandedWeapon,
                ItemDrop.ItemData.ItemType.TwoHandedWeapon,
                ItemDrop.ItemData.ItemType.Bow,
                ItemDrop.ItemData.ItemType.Shield,
            };

        private static FieldInfo[] GetEquipSlotFields()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var names = new[] { "m_helmetItem", "m_chestItem", "m_legItem", "m_shoulderItem", "m_rightItem", "m_leftItem", "m_utilityItem" };
            var result = new List<FieldInfo>();
            foreach (var name in names)
            {
                var f = typeof(Humanoid).GetField(name, flags);
                if (f != null) result.Add(f);
            }
            return result.ToArray();
        }

        private void Awake()
        {
            _ai            = GetComponent<MonsterAI>();
            _selfCharacter = GetComponent<Character>();
            _stayPosition  = transform.position;

            if (_ai != null)
            {
                _ai.m_attackPlayerObjects               = false;
                _ai.m_enableHuntPlayer                  = false;
                _ai.m_fleeIfHurtWhenTargetCantBeReached = true;

                foreach (string itemName in new[]{
                    "CookedMeat", "CookedDeerMeat", "CookedWolfMeat", "CookedLoxMeat",
                    "FishCooked", "SerpentMeatCooked", "NeckTailGrilled"
                })
                {
                    _ai.m_consumeItems.Add(ZNetScene.instance.GetPrefab(itemName).GetComponent<ItemDrop>());
                }
            }

            if (_selfCharacter != null)
            {
                _selfCharacter.m_name      = "Ульф";
                _selfCharacter.m_faction   = Character.Faction.Players;
                _selfCharacter.SetTamed(true);
                _selfCharacter.SetMaxHealth(600f);
                _selfCharacter.m_health    = 600f;
            }

            var vis = GetComponent<VisEquipment>();
            if (vis != null)
            {
                vis.SetModel(0);                                        // мужская модель
                vis.SetBeardItem("Beard3");                             // длинная борода с косами
                vis.SetHairItem("Hair1");                               // короткие волосы
                vis.SetHairColor(new Vector3(0.85f, 0.82f, 0.75f));    // седые волосы
                vis.SetSkinColor(new Vector3(0.90f, 0.68f, 0.46f));    // загорелая обветренная кожа
            }

            var drop = GetComponent<CharacterDrop>();
            if (drop != null) drop.m_drops.Clear();

            var tameable = GetComponent<Tameable>();
            if (tameable != null) tameable.m_commandable = true;

            _humanoid  = GetComponent<Humanoid>();
            _container = GetComponent<Container>();

            if (_container != null && _humanoid != null)
            {
                _container.m_name            = "Ульф";
                _container.m_width           = 4;
                _container.m_height          = 2;
                _container.m_checkGuardStone = false;
                StartCoroutine(ShareInventoryCoroutine());
            }

            StartCoroutine(AiControlCoroutine());
            StartCoroutine(HealCoroutine());
        }

        // ──────────────────────────────────────────────────────────────
        // AI control
        // ──────────────────────────────────────────────────────────────

        // Вызывается Harmony-патчем перед каждым SetTarget.
        // false = заблокировать установку цели.
        public bool IsValidTarget(Character target)
        {
            if (target == null) return true;

            var followGo     = _ai?.GetFollowTarget();
            var followTarget = (followGo != null) ? followGo.GetComponent<Character>() : null;

            if (followTarget == null) return true; // stay mode: MonsterAI решает сам

            bool validThreat = IsTargeting(target, followTarget) || IsTargeting(target, _selfCharacter);
            bool inRange     = Vector3.Distance(target.transform.position, followTarget.transform.position) <= MaxChaseDistance;
            return validThreat && inRange;
        }

        private IEnumerator AiControlCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                if (this == null || _ai == null) yield break;

                var followGo     = _ai.GetFollowTarget();
                var followTarget = (followGo != null) ? followGo.GetComponent<Character>() : null;

                if (_lastFollowTarget != null && followTarget == null)
                    _stayPosition = transform.position;

                _lastFollowTarget = followTarget;

                if (followTarget != null)
                    ScanThreats(followTarget);
                else
                    UpdateStayMode();
            }
        }

        private void ScanThreats(Character followTarget)
        {
            var current = _ai.GetTargetCreature();

            // Проверяем, не стала ли текущая цель невалидной (убежала далеко или перестала атаковать)
            if (current != null)
            {
                bool validThreat = IsTargeting(current, followTarget) || IsTargeting(current, _selfCharacter);
                bool inRange     = Vector3.Distance(current.transform.position, followTarget.transform.position) <= MaxChaseDistance;
                if (!validThreat || !inRange)
                {
                    ClearTarget();
                    current = null;
                }
            }

            if (current != null) return; // уже атакуем валидную цель

            // Ищем новую угрозу
            Character threat   = null;
            float     bestDist = float.MaxValue;

            foreach (var c in Character.GetAllCharacters())
            {
                if (c == null || c == _selfCharacter || c.IsPlayer()) continue;

                bool threatsPlayer = IsTargeting(c, followTarget);
                bool threatsSelf   = IsTargeting(c, _selfCharacter);
                if (!threatsPlayer && !threatsSelf) continue;

                float d = Vector3.Distance(c.transform.position, followTarget.transform.position);
                if (d < ThreatScanRange && d < bestDist)
                {
                    bestDist = d;
                    threat   = c;
                }
            }

            if (threat != null)
                SetTargetMethod?.Invoke(_ai, new object[] { threat });
        }

        private void UpdateStayMode()
        {
            var current = _ai.GetTargetCreature();
            if (current == null) return;

            if (Vector3.Distance(current.transform.position, _stayPosition) > MaxChaseDistance)
                ClearTarget();
        }

        private static bool IsTargeting(Character attacker, Character target)
        {
            return attacker.GetComponent<MonsterAI>()?.GetTargetCreature() == target;
        }

        private void ClearTarget()
        {
            TargetCreatureField?.SetValue(_ai, null);
        }

        // ──────────────────────────────────────────────────────────────
        // Health
        // ──────────────────────────────────────────────────────────────

        private IEnumerator HealCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                if (this == null || _selfCharacter == null) yield break;
                if (_selfCharacter.IsDead()) continue;

                float max     = _selfCharacter.GetMaxHealth();
                float current = _selfCharacter.GetHealth();

                if (current < max)
                    _selfCharacter.Heal(2.5f, true);

                if (current < max * 0.8f)
                    TryConsumeFood();
            }
        }

        private void TryConsumeFood()
        {
            var inventory = _humanoid?.GetInventory();
            if (inventory == null) return;

            foreach (var item in inventory.GetAllItems())
            {
                if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable) continue;
                if (item.m_shared.m_food <= 0f) continue;

                inventory.RemoveItem(item, 1);
                _selfCharacter.Heal(_selfCharacter.GetMaxHealth() * 0.2f, true);
                return;
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Death
        // ──────────────────────────────────────────────────────────────

        public void OnFollowerDeath()
        {
            var inventory = _humanoid?.GetInventory();
            if (inventory == null || inventory.GetAllItems().Count == 0) return;

            var prefab = ZNetScene.instance.GetPrefab("Player_tombstone");
            if (prefab == null) return;

            var go = UnityEngine.Object.Instantiate(prefab, transform.position, Quaternion.identity);

            go.GetComponent<TombStone>()?.Setup(_selfCharacter?.m_name ?? "Ульф", 0L);

            var tombInventory = go.GetComponent<Container>()?.GetInventory();
            if (tombInventory == null) return;

            var items = new List<ItemDrop.ItemData>(inventory.GetAllItems());
            foreach (var item in items)
            {
                tombInventory.AddItem(item);
                inventory.RemoveItem(item);
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Inventory
        // ──────────────────────────────────────────────────────────────

        private IEnumerator ShareInventoryCoroutine()
        {
            yield return null;

            if (ContainerInventoryField == null)
            {
                Debug.LogError("[ValheimStreamerApi] Container.m_inventory field not found — inventory sharing skipped");
                yield break;
            }

            ContainerInventoryField.SetValue(_container, _humanoid.GetInventory());
            StartCoroutine(AutoEquipCoroutine());
        }

        private IEnumerator AutoEquipCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                if (this == null || _humanoid == null) yield break;
                AutoEquip();
            }
        }

        private void AutoEquip()
        {
            var inventory = _humanoid.GetInventory();
            bool needsVisualUpdate = false;

            foreach (var field in EquipSlotFields)
            {
                var item = field.GetValue(_humanoid) as ItemDrop.ItemData;
                if (item != null && !inventory.ContainsItem(item))
                {
                    item.m_equipped = false;
                    field.SetValue(_humanoid, null);
                    needsVisualUpdate = true;
                }
            }

            if (needsVisualUpdate)
                SetupEquipmentMethod?.Invoke(_humanoid, null);

            foreach (var item in inventory.GetAllItems())
            {
                if (!EquippableTypes.Contains(item.m_shared.m_itemType)) continue;
                if (_humanoid.IsItemEquiped(item)) continue;
                _humanoid.EquipItem(item, triggerEquipEffects: false);
            }
        }
    }
}
