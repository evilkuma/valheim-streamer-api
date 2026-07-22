using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimStreamerApi
{
    public class GeyserBehaviour : MonoBehaviour
    {
        private const float Lifetime        = 30f;
        private const float WarningDuration = 1.5f;
        private const float EruptionRadius  = 6f;
        private const float MinRestTime     = 4f;
        private const float MaxRestTime     = 9f;

        private Light _light;
        private float _baseIntensity;

        private void Start()
        {
            _light = GetComponent<Light>();
            if (_light != null) _baseIntensity = _light.intensity;

            StartCoroutine(LifetimeRoutine());
            StartCoroutine(GeyserCycleRoutine());
        }

        private IEnumerator LifetimeRoutine()
        {
            yield return new WaitForSeconds(Lifetime);
            if (this != null) Destroy(gameObject);
        }

        private IEnumerator GeyserCycleRoutine()
        {
            yield return new WaitForSeconds(Random.Range(0f, MaxRestTime));

            while (this != null)
            {
                // Фаза покоя — рандомная пауза
                if (_light != null) _light.intensity = _baseIntensity * 0.25f;
                yield return new WaitForSeconds(Random.Range(MinRestTime, MaxRestTime));

                if (this == null) yield break;

                // Фаза предупреждения
                yield return StartCoroutine(WarningRoutine());

                if (this == null) yield break;

                // Взрыв — ждём пока эффект сам не закончится
                yield return StartCoroutine(EruptRoutine());
            }
        }

        private IEnumerator WarningRoutine()
        {
            float elapsed = 0f;
            while (elapsed < WarningDuration)
            {
                float t          = elapsed / WarningDuration;
                float flashSpeed = Mathf.Lerp(3f, 14f, t) * Mathf.PI;
                float flash      = (Mathf.Sin(Time.time * flashSpeed) + 1f) * 0.5f;

                if (_light != null)
                    _light.intensity = Mathf.Lerp(_baseIntensity * 0.5f, _baseIntensity * 5f, flash);

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator EruptRoutine()
        {
            GameObject fx = ZNetScene.instance?.GetPrefab("Fader_WallOfFire_AOE");
            if (fx == null) yield break;

            ZNetView.StartGhostInit();
            var instance = Object.Instantiate(fx, transform.position, Quaternion.identity);
            ZNetView.FinishGhostInit();

            // Удаляем все Aoe-компоненты чтобы префаб не наносил урон сам
            foreach (var aoe in instance.GetComponentsInChildren<Aoe>(true))
                Object.DestroyImmediate(aoe);

            // Наш урон — ровно один раз в момент взрыва
            var damaged = new HashSet<IDestructible>();
            foreach (var col in Physics.OverlapSphere(transform.position, EruptionRadius))
            {
                var dest = col.GetComponentInParent<IDestructible>();
                if (dest == null || !damaged.Add(dest)) continue;

                var hit = new HitData();
                hit.m_damage.m_fire  = 60f;
                hit.m_damage.m_blunt = 20f;
                hit.m_toolTier       = 2;
                hit.m_dir            = (col.transform.position - transform.position).normalized;
                hit.m_point          = col.transform.position;
                dest.Damage(hit);
            }

            // Ждём пока эффект уничтожит себя сам, fallback 15с
            float timeout = 15f;
            float elapsed = 0f;
            while (instance != null && elapsed < timeout)
            {
                elapsed += 0.3f;
                yield return new WaitForSeconds(0.3f);
            }

            if (instance != null) Object.Destroy(instance);
        }
    }
}
