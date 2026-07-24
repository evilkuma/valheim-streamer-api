using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimStreamerApi.Patches
{
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    internal static class ZNetScenePatch
    {
        private static GameObject _prefabParent;

        private static GameObject PrefabParent
        {
            get
            {
                if (_prefabParent == null)
                {
                    _prefabParent = new GameObject("Custom_Prefabs");
                    UnityEngine.Object.DontDestroyOnLoad(_prefabParent);
                    _prefabParent.SetActive(false);
                }
                return _prefabParent;
            }
        }

        [HarmonyPostfix]
        private static void Postfix(ZNetScene __instance)
        {
            PatchFollower(__instance);
            PatchDragonCompanion(__instance);
            PatchModerVisit(__instance);
        }

        private static void PatchFollower(ZNetScene __instance)
        {
            var sourcePrefab = __instance.GetPrefab("Player");
            if (sourcePrefab == null)
            {
                Log.LogError($"[ValheimStreamerApi] Источник Player не найден — Follower не зарегистрирован.");
                return;
            }
            
            var clone = UnityEngine.Object.Instantiate(sourcePrefab, PrefabParent.transform);
            clone.name = "StreamerApi.Follower";

            var oldPlayer   = clone.GetComponent<Player>();
            var newHumanoid = clone.AddComponent<Humanoid>();

            if (oldPlayer != null) CopyFields(oldPlayer, newHumanoid);

            var keepSet = new HashSet<Component> { newHumanoid };
            foreach (var comp in clone.GetComponents<Component>())
            {
                if (keepSet.Contains(comp)) continue;

                switch (comp.GetType().Name)
                {
                    case "Transform":
                    case "Rigidbody":
                    case "CapsuleCollider":
                    case "Animator":
                    case "VisEquipment":
                    case "ZNetView":
                    case "ZSyncTransform":
                    case "ZSyncAnimation":
                    case "FootStep":
                        continue;
                }

                UnityEngine.Object.DestroyImmediate(comp);
            }

            clone.AddComponent<MonsterAI>();
            clone.AddComponent<FollowerTameable>();

            // Размер задаётся на prefab-объекте до спауна, чтобы Container.Awake() создал
            // Inventory нужного размера (иначе он создаётся с дефолтом 3×2 = 6 слотов).
            var container = clone.AddComponent<Container>();
            container.m_name            = "Ульф";
            container.m_width           = 8;
            container.m_height          = 4;
            container.m_checkGuardStone = false;

            clone.AddComponent<CharacterDrop>();
            clone.AddComponent<FollowerBehaviour>();

            var zview = clone.GetComponent<ZNetView>();
            if (zview != null) AccessTools.Field(typeof(ZNetView), "m_persistent")?.SetValue(zview, true);

            var namedPrefabs = AccessTools.Field(typeof(ZNetScene), "m_namedPrefabs")
                ?.GetValue(__instance) as Dictionary<int, GameObject>;
            if (namedPrefabs == null)
            {
                Log.LogError(
                    "[ValheimStreamerApi] Не удалось получить m_namedPrefabs — регистрация прервана.");
                return;
            }
            
            namedPrefabs[clone.name.GetStableHashCode(true)] = clone;
            __instance.m_prefabs.Add(clone);

            Log.LogInfo($"[ValheimStreamerApi] Follower зарегистрирован на основе Player.");
        }

        private static void PatchDragonCompanion(ZNetScene __instance)
        {
            var sourcePrefab = __instance.GetPrefab("Hatchling");
            if (sourcePrefab == null)
            {
                Log.LogError($"[ValheimStreamerApi] Источник Hatchling не найден — DragonCompanion не зарегистрирован.");
                return;
            }

            var clone = UnityEngine.Object.Instantiate(sourcePrefab, PrefabParent.transform);
            clone.name = "StreamerApi.DragonCompanion";

            clone.AddComponent<Tameable>();
            clone.AddComponent<DragonCompanionBehaviour>();

            var namedPrefabs = AccessTools.Field(typeof(ZNetScene), "m_namedPrefabs")
                ?.GetValue(__instance) as Dictionary<int, GameObject>;
            if (namedPrefabs == null)
            {
                Log.LogError(
                    "[ValheimStreamerApi] Не удалось получить m_namedPrefabs — регистрация прервана.");
                return;
            }

            var zviewDragon = clone.GetComponent<ZNetView>();
            if (zviewDragon != null) AccessTools.Field(typeof(ZNetView), "m_persistent")?.SetValue(zviewDragon, true);

            namedPrefabs[clone.name.GetStableHashCode(true)] = clone;
            __instance.m_prefabs.Add(clone);

            Log.LogInfo($"[ValheimStreamerApi] DragonCompanion зарегистрирован на основе Hatchling.");
        }

        private static void PatchModerVisit(ZNetScene __instance)
        {
            var sourcePrefab = __instance.GetPrefab("Dragon");
            if (sourcePrefab == null)
            {
                Log.LogError("[ValheimStreamerApi] Dragon не найден — ModerVisit не зарегистрирован.");
                return;
            }

            var clone = UnityEngine.Object.Instantiate(sourcePrefab, PrefabParent.transform);
            clone.name = "StreamerApi.ModerVisit";

            var drop = clone.GetComponent<CharacterDrop>();
            if (drop != null) drop.m_drops.Clear();

            clone.AddComponent<ModerVisitBehaviour>();

            var namedPrefabs = AccessTools.Field(typeof(ZNetScene), "m_namedPrefabs")
                ?.GetValue(__instance) as Dictionary<int, GameObject>;
            if (namedPrefabs == null)
            {
                Log.LogError("[ValheimStreamerApi] m_namedPrefabs не найден — ModerVisit не зарегистрирован.");
                return;
            }

            var zview = clone.GetComponent<ZNetView>();
            if (zview != null) AccessTools.Field(typeof(ZNetView), "m_persistent")?.SetValue(zview, true);

            namedPrefabs[clone.name.GetStableHashCode(true)] = clone;
            __instance.m_prefabs.Add(clone);

            Log.LogInfo("[ValheimStreamerApi] ModerVisit зарегистрирован на основе Dragon.");
        }

        private static int CopyFields(Component source, Component destination)
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var sourceType = source.GetType();
            int count      = 0;

            var currentType = destination.GetType();
            while (currentType != null && currentType != typeof(MonoBehaviour))
            {
                foreach (var field in currentType.GetFields(flags))
                {
                    if (field.IsLiteral || field.IsInitOnly) continue;

                    var sourceField = sourceType.GetField(field.Name, flags);
                    if (sourceField == null || sourceField.FieldType != field.FieldType) continue;

                    field.SetValue(destination, sourceField.GetValue(source));
                    count++;
                }
                currentType = currentType.BaseType;
            }

            return count;
        }
    }

    [HarmonyPatch(typeof(Character), "OnDeath")]
    internal static class FollowerDeathPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Character __instance)
        {
            var follower = __instance.GetComponent<FollowerBehaviour>();
            if (follower == null) return;
            follower.OnFollowerDeath();
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "SetTarget")]
    internal static class FollowerSetTargetPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(MonsterAI __instance, Character attacker)
        {
            var follower = __instance.GetComponent<FollowerBehaviour>();
            if (follower == null) return true;
            return follower.IsValidTarget(attacker);
        }
    }

    [HarmonyPatch(typeof(Tameable), nameof(Tameable.GetHoverText))]
    internal static class FollowerHoverTextPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Tameable __instance, ref string __result)
        {
            if (__instance.GetComponent<FollowerBehaviour>() == null) return;
            __result = __result
                .Replace("Переименовать", "Открыть инвентарь")
                .Replace("Rename", "Open inventory");
        }
    }

}
