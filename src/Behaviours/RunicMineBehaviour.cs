using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimStreamerApi
{
    public class RunicMineBehaviour : MonoBehaviour
    {
        private const float WarnRadius      = 5f;
        private const float TriggerRadius   = 1.5f;
        private const float ExplosionRadius = 4f;
        private const float ArmDuration     = 1f;
        private const float InitialDelay    = 2f;
        private const float Lifetime        = 90f;

        private bool  _triggered   = false;
        private float _closestDist = float.MaxValue;
        private Light _light;
        private float _baseIntensity;

        private void Start()
        {
            _light = GetComponentInChildren<Light>();
            if (_light != null)
            {
                _baseIntensity   = _light.intensity;
                _light.intensity = 0f;
            }

            StartCoroutine(LifetimeRoutine());
            StartCoroutine(ProximityRoutine());
            StartCoroutine(GlowRoutine());
        }

        private IEnumerator LifetimeRoutine()
        {
            yield return new WaitForSeconds(Lifetime);
            if (this != null) Destroy(gameObject);
        }

        private IEnumerator ProximityRoutine()
        {
            yield return new WaitForSeconds(InitialDelay);

            while (this != null && !_triggered)
            {
                yield return new WaitForSeconds(0.2f);

                _closestDist = float.MaxValue;
                foreach (var col in Physics.OverlapSphere(transform.position, WarnRadius))
                {
                    var ch = col.GetComponentInParent<Character>();
                    if (ch == null) continue;
                    float dist = Vector3.Distance(ch.transform.position, transform.position);
                    if (dist < _closestDist) _closestDist = dist;
                }

                if (_closestDist <= TriggerRadius)
                {
                    _triggered = true;
                    StartCoroutine(ArmingRoutine());
                    yield break;
                }
            }
        }

        private IEnumerator GlowRoutine()
        {
            while (this != null && !_triggered)
            {
                if (_light != null)
                {
                    if (_closestDist <= WarnRadius)
                    {
                        float proximity = 1f - Mathf.Clamp01(
                            (_closestDist - TriggerRadius) / (WarnRadius - TriggerRadius));
                        float pulseSpeed = Mathf.Lerp(1.5f, 5f, proximity);
                        float pulse      = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                        _light.intensity = Mathf.Lerp(_baseIntensity * 0.3f, _baseIntensity * 2.5f, proximity * pulse);
                    }
                    else
                    {
                        _light.intensity = 0f;
                    }
                }
                yield return null;
            }
        }

        private IEnumerator ArmingRoutine()
        {
            float elapsed = 0f;

            while (elapsed < ArmDuration)
            {
                float t          = elapsed / ArmDuration;
                float flashSpeed = Mathf.Lerp(8f, 30f, t) * Mathf.PI;
                float flash      = (Mathf.Sin(Time.time * flashSpeed) + 1f) * 0.5f;

                if (_light != null)
                    _light.intensity = Mathf.Lerp(_baseIntensity, _baseIntensity * 8f, flash);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Explode();
        }

        private void Explode()
        {
            gameObject.SetActive(false);

            GameObject fx = ZNetScene.instance?.GetPrefab("fx_goblinking_meteor_hit");
            if (fx != null) Object.Instantiate(fx, transform.position, Quaternion.identity);

            var damaged = new HashSet<IDestructible>();
            foreach (var col in Physics.OverlapSphere(transform.position, ExplosionRadius))
            {
                var dest = col.GetComponentInParent<IDestructible>();
                if (dest == null || !damaged.Add(dest)) continue;

                var hit = new HitData();
                hit.m_damage.m_blunt = 80f;
                hit.m_damage.m_fire  = 40f;
                hit.m_toolTier       = 3;
                hit.m_dir            = (col.transform.position - transform.position).normalized;
                hit.m_point          = col.transform.position;
                dest.Damage(hit);
            }

            Destroy(gameObject);
        }
    }
}
