using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PocketPlanner.Core
{
    /// <summary>
    /// Orchestrates the 3D physics dice roll animation for all 6 dice
    /// (3 shape + 3 building). Bridges the visual layer to your existing
    /// DiceManager / DicePool logic.
    ///
    /// ── Scene Setup ─────────────────────────────────────────────────────────
    /// 1. Create 6 cube GameObjects in your scene (3 for shape, 3 for building).
    /// 2. Add DiceVisual.cs to each cube.
    /// 3. Add a flat plane/floor collider beneath them so they bounce realistically.
    /// 4. Assign the 6 DiceVisual references to this component in the Inspector.
    /// 5. Place this component on the same GameObject as DiceManager (or any manager).
    ///
    /// ── Flow ────────────────────────────────────────────────────────────────
    ///   TriggerRoll()
    ///     → DiceManager.RollAllDice()          (randomises logical faces)
    ///     → staggers LaunchRoll() on each die  (physics animation)
    ///     → waits for all 6 to settle
    ///     → fires onRollComplete callback      (your game can continue)
    /// </summary>
    public class DiceAnimationManager : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Dice Visuals")]
        [Tooltip("Assign 3 shape die GameObjects (DiceVisual components).")]
        [SerializeField] private DiceVisual[] shapeDiceVisuals = new DiceVisual[3];

        [Tooltip("Assign 3 building die GameObjects (DiceVisual components).")]
        [SerializeField] private DiceVisual[] buildingDiceVisuals = new DiceVisual[3];

        [Header("Roll Settings")]
        [Tooltip("Seconds between each die launching (stagger effect).")]
        [SerializeField] private float launchStagger = 0.12f;

        [Tooltip("Position spread: dice spawn within this radius of their anchor point.")]
        [SerializeField] private float spawnScatter = 0.4f;

        [Header("Anchor Positions")]
        [Tooltip("World-space centre of the shape dice group.")]
        [SerializeField] private Transform shapeDiceAnchor;

        [Tooltip("World-space centre of the building dice group.")]
        [SerializeField] private Transform buildingDiceAnchor;

        [Header("References")]
        [SerializeField] private DiceManager diceManager;

        // ── Events ───────────────────────────────────────────────────────────
        /// <summary>Fired when all dice have physically settled.</summary>
        public event Action OnRollComplete;

        // ── Runtime state ────────────────────────────────────────────────────
        private bool isRolling = false;
        private int settledCount = 0;
        private int totalDice = 6;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (diceManager == null)
                diceManager = GetComponent<DiceManager>();

            if (diceManager == null)
                Debug.LogError("[DiceAnimationManager] No DiceManager found. Assign it in the Inspector.");
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Call this to start a full roll sequence.
        /// Internally calls DiceManager.RollAllDice() then launches all visuals.
        /// </summary>
        public void TriggerRoll()
        {
            if (isRolling)
            {
                Debug.LogWarning("[DiceAnimationManager] Roll already in progress.");
                return;
            }

            // 1. Let the logical layer roll (randomises CurrentFace on all Dice)
            diceManager.RollAllDice();

            // 2. Kick off the visual animation
            StartCoroutine(RollSequence());
        }

        // ── Private ──────────────────────────────────────────────────────────

        private IEnumerator RollSequence()
        {
            isRolling = true;
            settledCount = 0;

            // Gather logical face results AFTER RollAllDice() has been called
            List<Dice> shapeDice    = diceManager.GetShapeDice();
            List<Dice> buildingDice = diceManager.GetBuildingDice();

            // Scatter dice to their anchor positions
            ScatterDiceToAnchor(shapeDiceVisuals,    shapeDiceAnchor);
            ScatterDiceToAnchor(buildingDiceVisuals, buildingDiceAnchor);

            // Prepare all dice with their target face BEFORE launching
            for (int i = 0; i < 3; i++)
            {
                if (i < shapeDice.Count && shapeDiceVisuals[i] != null)
                    shapeDiceVisuals[i].PrepareRoll(shapeDice[i].CurrentFace, OnDiceSettled);

                if (i < buildingDice.Count && buildingDiceVisuals[i] != null)
                    buildingDiceVisuals[i].PrepareRoll(buildingDice[i].CurrentFace, OnDiceSettled);
            }

            // Stagger-launch all 6 dice
            for (int i = 0; i < 3; i++)
            {
                if (shapeDiceVisuals[i] != null)
                    shapeDiceVisuals[i].LaunchRoll();

                yield return new WaitForSeconds(launchStagger);

                if (buildingDiceVisuals[i] != null)
                    buildingDiceVisuals[i].LaunchRoll();

                yield return new WaitForSeconds(launchStagger);
            }

            // The coroutine exits here; OnDiceSettled() handles completion
        }

        /// <summary>
        /// Fires each time a single die settles. When all 6 are done, wraps up.
        /// </summary>
        private void OnDiceSettled()
        {
            settledCount++;
            Debug.Log($"[DiceAnimationManager] {settledCount}/{totalDice} dice settled.");

            if (settledCount >= totalDice)
            {
                isRolling = false;
                Debug.Log("[DiceAnimationManager] All dice settled. Roll complete.");
                OnRollComplete?.Invoke();
            }
        }

        /// <summary>
        /// Repositions each DiceVisual near an anchor with a small random scatter
        /// so they don't all spawn on top of each other.
        /// </summary>
        private void ScatterDiceToAnchor(DiceVisual[] visuals, Transform anchor)
        {
            if (anchor == null) return;

            foreach (var visual in visuals)
            {
                if (visual == null) continue;

                Vector2 scatter2D = UnityEngine.Random.insideUnitCircle * spawnScatter;
                Vector3 offset    = new Vector3(scatter2D.x, 0.5f, scatter2D.y); // slight lift
                visual.transform.position = anchor.position + offset;
                visual.transform.rotation = UnityEngine.Random.rotation; // random start orientation
            }
        }
    }
}
