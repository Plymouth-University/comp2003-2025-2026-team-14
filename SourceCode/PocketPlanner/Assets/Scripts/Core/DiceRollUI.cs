using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PocketPlanner.Core
{
    /// <summary>
    /// Lightweight UI overlay that:
    ///   • Shows a "Rolling…" indicator while dice are in the air
    ///   • Hides it once all dice have settled
    ///   • Hooks into DiceAnimationManager to trigger rolls automatically
    ///     at the start of each turn (call TriggerNewTurnRoll() from your GameManager)
    ///
    /// ── Scene Setup ─────────────────────────────────────────────────────────
    /// 1. Create a Canvas > Panel with a TextMeshPro label ("Rolling…").
    /// 2. Assign rollingPanel and rollingLabel in the Inspector.
    /// 3. Assign diceAnimationManager.
    /// 4. Call TriggerNewTurnRoll() from your turn-start logic.
    /// </summary>
    public class DiceRollUI : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private DiceAnimationManager diceAnimationManager;

        [Header("UI Elements")]
        [Tooltip("The panel/overlay shown while dice are rolling.")]
        [SerializeField] private GameObject rollingPanel;

        [Tooltip("Label inside the panel, e.g. 'Rolling…'")]
        [SerializeField] private TMP_Text rollingLabel;

        [Tooltip("Optional: panel shown after rolling to display results.")]
        [SerializeField] private GameObject resultsPanel;

        [Header("Animation")]
        [Tooltip("Dots cycle speed for 'Rolling…' label (seconds per dot).")]
        [SerializeField] private float dotCycleSpeed = 0.4f;

        // ── Runtime state ────────────────────────────────────────────────────
        private Coroutine dotCoroutine;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (diceAnimationManager == null)
                diceAnimationManager = FindFirstObjectByType<DiceAnimationManager>();

            if (diceAnimationManager == null)
            {
                Debug.LogError("[DiceRollUI] No DiceAnimationManager found in scene.");
                return;
            }

            diceAnimationManager.OnRollComplete += HandleRollComplete;
        }

        private void OnDestroy()
        {
            if (diceAnimationManager != null)
                diceAnimationManager.OnRollComplete -= HandleRollComplete;
        }

        private void Start()
        {
            // Hide both panels at startup
            SetRollingPanelVisible(false);
            SetResultsPanelVisible(false);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Call this at the start of a new turn to show the rolling UI and
        /// trigger the physics dice roll.
        /// </summary>
        public void TriggerNewTurnRoll()
        {
            SetResultsPanelVisible(false);
            SetRollingPanelVisible(true);

            if (dotCoroutine != null)
                StopCoroutine(dotCoroutine);
            dotCoroutine = StartCoroutine(AnimateDots());

            diceAnimationManager.TriggerRoll();
        }

        // ── Private ──────────────────────────────────────────────────────────

        private void HandleRollComplete()
        {
            SetRollingPanelVisible(false);
            SetResultsPanelVisible(true);

            if (dotCoroutine != null)
            {
                StopCoroutine(dotCoroutine);
                dotCoroutine = null;
            }
        }

        private void SetRollingPanelVisible(bool visible)
        {
            if (rollingPanel != null)
                rollingPanel.SetActive(visible);
        }

        private void SetResultsPanelVisible(bool visible)
        {
            if (resultsPanel != null)
                resultsPanel.SetActive(visible);
        }

        /// <summary>
        /// Cycles the rolling label: "Rolling." → "Rolling.." → "Rolling..."
        /// </summary>
        private IEnumerator AnimateDots()
        {
            if (rollingLabel == null) yield break;

            int dots = 1;
            while (true)
            {
                rollingLabel.text = "Rolling" + new string('.', dots);
                dots = (dots % 3) + 1;
                yield return new WaitForSeconds(dotCycleSpeed);
            }
        }
    }
}
