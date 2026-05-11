using System.Collections;
using System.Reflection;
using UnityEngine;

namespace ValheimStreamerApi
{
    [DefaultExecutionOrder(9999)]
    public class InvisibleEnemyBehaviour : MonoBehaviour
    {
        private Character _character;
        private Renderer[] _renderers;
        private float _lastCharacterHp;
        private float _lastPlayerHp;
        private Coroutine _shimmerCoroutine;
        private bool _isFlashing;

        private static readonly Color HitColor    = new Color(1f, 0.1f, 0.1f);
        private static readonly Color AttackColor = new Color(1f, 0.6f, 0.1f);

        private static readonly FieldInfo HudsField =
            typeof(EnemyHud).GetField("m_huds", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo HudGuiField =
            typeof(EnemyHud).GetNestedType("HudData", BindingFlags.NonPublic | BindingFlags.Public)
                            ?.GetField("m_gui", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private void Start()
        {
            _character = GetComponent<Character>();
            _renderers = GetComponentsInChildren<Renderer>(true);

            _lastCharacterHp = _character.GetHealth();
            _lastPlayerHp    = Player.m_localPlayer?.GetHealth() ?? 0f;

            _character.m_name = "";

            GetComponent<MonsterAI>()?.Alert();

            SetRenderersActive(false);
            _shimmerCoroutine = StartCoroutine(ShimmerRoutine());
            StartCoroutine(MonitorRoutine());
        }

        // When not flashing: destroy HUD entry each frame so EnemyHud cannot show it.
        // When flashing: stop suppressing so EnemyHud naturally displays the HUD.
        private void LateUpdate()
        {
            if (_isFlashing) return;
            DestroyHudEntry();
        }

        private void DestroyHudEntry()
        {
            if (EnemyHud.instance == null) return;

            var huds = HudsField?.GetValue(EnemyHud.instance) as System.Collections.IDictionary;
            if (huds == null || !huds.Contains(_character)) return;

            var hudData = huds[_character];
            if (hudData != null)
            {
                var gui = HudGuiField?.GetValue(hudData) as GameObject;
                if (gui != null) Destroy(gui);
            }
            huds.Remove(_character);
        }

        private IEnumerator ShimmerRoutine()
        {
            while (true)
            {
                SetRenderersActive(true);
                yield return new WaitForSeconds(Random.Range(0.03f, 0.08f));
                SetRenderersActive(false);
                yield return new WaitForSeconds(Random.Range(0.3f, 0.8f));
            }
        }

        private IEnumerator MonitorRoutine()
        {
            while (_character != null)
            {
                float charHp = _character.GetHealth();
                if (charHp < _lastCharacterHp && !_isFlashing)
                    StartCoroutine(Flash(0.4f, HitColor));
                _lastCharacterHp = charHp;

                Player player = Player.m_localPlayer;
                if (player != null)
                {
                    float playerHp = player.GetHealth();
                    if (playerHp < _lastPlayerHp && !_isFlashing)
                        StartCoroutine(Flash(0.6f, AttackColor));
                    _lastPlayerHp = playerHp;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator Flash(float duration, Color emissionColor)
        {
            _isFlashing = true;
            if (_shimmerCoroutine != null) StopCoroutine(_shimmerCoroutine);

            SetRenderersActive(true);
            SetEmission(emissionColor * 2f);

            yield return new WaitForSeconds(duration);

            float elapsed = 0f;
            const float fadeTime = 0.25f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                SetEmission(Color.Lerp(emissionColor * 2f, Color.black, elapsed / fadeTime));
                yield return null;
            }

            SetEmission(Color.black);
            SetRenderersActive(false);
            _isFlashing = false;
            _shimmerCoroutine = StartCoroutine(ShimmerRoutine());
        }

        private void SetRenderersActive(bool active)
        {
            foreach (var r in _renderers)
                if (r != null) r.enabled = active;
        }

        private void SetEmission(Color color)
        {
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                foreach (var mat in r.materials)
                {
                    if (!mat.HasProperty("_EmissionColor")) continue;
                    mat.SetColor("_EmissionColor", color);
                    if (color != Color.black)
                        mat.EnableKeyword("_EMISSION");
                    else
                        mat.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}
