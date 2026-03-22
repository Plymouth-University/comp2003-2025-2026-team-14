using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PocketPlanner.Core;
using System.Collections.Generic;
using System.Collections;

namespace PocketPlanner.UI
{
    public class DiceUIManager : MonoBehaviour
    {
        [Header("Dice Manager Reference")]
        [SerializeField] private DiceManager diceManager;

        [Header("Shape Manager Reference")]
        [SerializeField] private ShapeManager shapeManager;

        [Header("Shape Dice Face Sprites")]
        [SerializeField] private Sprite singleShapeFaceSprite = null;
        [SerializeField] private Sprite TShapeFaceSprite = null;
        [SerializeField] private Sprite LShapeFaceSprite = null;
        [SerializeField] private Sprite squareShapeFaceSprite = null;
        [SerializeField] private Sprite lineShapeFaceSprite = null;
        [SerializeField] private Sprite ZShapeFaceSprite = null;

        [Header("Building Dice Face Sprites")]
        [SerializeField] private Sprite industrialFaceSprite = null;
        [SerializeField] private Sprite residentialFaceSprite = null;
        [SerializeField] private Sprite commercialFaceSprite = null;
        [SerializeField] private Sprite schoolFaceSprite = null;
        [SerializeField] private Sprite parkFaceSprite = null;
        [SerializeField] private Sprite waterFaceSprite = null;

        [Header("Shape Dice UI Elements")]
        [SerializeField] private List<Button> shapeDiceButtons = new List<Button>(3);
        [SerializeField] private List<TextMeshProUGUI> shapeDiceTexts = new List<TextMeshProUGUI>(3);
        [SerializeField] private List<Image> shapeDiceBackgrounds = new List<Image>(3);

        [Header("Building Dice UI Elements")]
        [SerializeField] private List<Button> buildingDiceButtons = new List<Button>(3);
        [SerializeField] private List<TextMeshProUGUI> buildingDiceTexts = new List<TextMeshProUGUI>(3);
        [SerializeField] private List<Image> buildingDiceBackgrounds = new List<Image>(3);

        [Header("Colors")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color selectedColor = Color.green;
        [SerializeField] private Color doubleColor = Color.yellow;
        [SerializeField] private Color invalidColor = Color.red;

        [Header("Water Die UI")]
        [SerializeField] private GameObject waterDiePanel;
        [SerializeField] private Button industrialButton;
        [SerializeField] private Button residentialButton;
        [SerializeField] private Button commercialButton;
        [SerializeField] private Button schoolButton;
        [SerializeField] private Button parkButton;

        [Header("Wildcard UI")]
        [SerializeField] private Button shapeWildcardButton;
        [SerializeField] private Button buildingWildcardButton;
        [SerializeField] private WildcardSelectionPanel shapeWildcardPanel;
        [SerializeField] private WildcardSelectionPanel buildingWildcardPanel;
        [SerializeField] private TextMeshProUGUI wildcardCountText;
        [SerializeField] private TextMeshProUGUI wildcardCostText;

        // ── Roll Animation Settings ───────────────────────────────────────────
        [Header("Roll Animation")]
        [Tooltip("How many random frames flash before the die lands on its result.")]
        [SerializeField] private int rollFlickerCount = 10;

        [Tooltip("Interval between flicker frames at the START of the roll (fast).")]
        [SerializeField] private float flickerIntervalFast = 0.05f;

        [Tooltip("Interval between flicker frames at the END of the roll (slow / settling).")]
        [SerializeField] private float flickerIntervalSlow = 0.18f;

        [Tooltip("Delay between each die starting its animation (stagger feel).")]
        [SerializeField] private float dieStaggerDelay = 0.08f;

        // ── Runtime ───────────────────────────────────────────────────────────
        private bool waterDieClickedThisFrame = false;
        private int wildcardTargetShapeDieIndex = 0;
        private int wildcardTargetBuildingDieIndex = 0;
        private bool isAnimating = false;

        // ─────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────────

        void Start()
        {
            Debug.Log($"DiceUIManager.Start() on {gameObject.name}");

            if (diceManager == null)
            {
                diceManager = FindAnyObjectByType<DiceManager>();
                if (diceManager == null)
                    Debug.LogWarning("DiceUIManager: DiceManager not found on Start. Will try again later.");
            }

            if (shapeManager == null)
            {
                shapeManager = FindAnyObjectByType<ShapeManager>();
                if (shapeManager == null)
                    Debug.LogWarning("DiceUIManager: ShapeManager not found.");
            }

            // Dice button listeners
            for (int i = 0; i < shapeDiceButtons.Count; i++)
            {
                int index = i;
                shapeDiceButtons[i].onClick.AddListener(() => OnShapeDieClicked(index));
            }
            for (int i = 0; i < buildingDiceButtons.Count; i++)
            {
                int index = i;
                buildingDiceButtons[i].onClick.AddListener(() => OnBuildingDieClicked(index));
            }

            // Water die buttons
            if (industrialButton != null)
                industrialButton.onClick.AddListener(() => OnWaterDieBuildingTypeClicked(BuildingType.Industrial));
            if (residentialButton != null)
                residentialButton.onClick.AddListener(() => OnWaterDieBuildingTypeClicked(BuildingType.Residential));
            if (commercialButton != null)
                commercialButton.onClick.AddListener(() => OnWaterDieBuildingTypeClicked(BuildingType.Commercial));
            if (schoolButton != null)
                schoolButton.onClick.AddListener(() => OnWaterDieBuildingTypeClicked(BuildingType.School));
            if (parkButton != null)
                parkButton.onClick.AddListener(() => OnWaterDieBuildingTypeClicked(BuildingType.Park));

            // Wildcard buttons
            if (shapeWildcardButton != null)
                shapeWildcardButton.onClick.AddListener(OnShapeWildcardButtonClicked);
            if (buildingWildcardButton != null)
                buildingWildcardButton.onClick.AddListener(OnBuildingWildcardButtonClicked);

            if (shapeWildcardPanel != null)
                shapeWildcardPanel.onSelectionMade.AddListener(OnShapeWildcardSelected);
            if (buildingWildcardPanel != null)
                buildingWildcardPanel.onSelectionMade.AddListener(OnBuildingWildcardSelected);

            if (waterDiePanel != null)
                waterDiePanel.SetActive(false);
            if (shapeWildcardPanel != null)    shapeWildcardPanel.Hide();
            if (buildingWildcardPanel != null)  buildingWildcardPanel.Hide();

            UpdateWildcardUI();
            StartCoroutine(EnsureGameManagerReady());
            UpdateDiceUI();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Sprite helpers
        // ─────────────────────────────────────────────────────────────────────

        private Sprite GetShapeSprite(ShapeType shapeType)
        {
            return shapeType switch
            {
                ShapeType.SingleShape => singleShapeFaceSprite,
                ShapeType.TShape      => TShapeFaceSprite,
                ShapeType.LShape      => LShapeFaceSprite,
                ShapeType.SquareShape => squareShapeFaceSprite,
                ShapeType.LineShape   => lineShapeFaceSprite,
                ShapeType.ZShape      => ZShapeFaceSprite,
                _ => null
            };
        }

        private Sprite GetBuildingSprite(BuildingType buildingType)
        {
            return buildingType switch
            {
                BuildingType.Industrial  => industrialFaceSprite,
                BuildingType.Residential => residentialFaceSprite,
                BuildingType.Commercial  => commercialFaceSprite,
                BuildingType.School      => schoolFaceSprite,
                BuildingType.Park        => parkFaceSprite,
                BuildingType.Water       => waterFaceSprite,
                _ => null
            };
        }

        // Returns a random shape sprite — used during the flicker frames
        private Sprite GetRandomShapeSprite()
        {
            Sprite[] all =
            {
                singleShapeFaceSprite, TShapeFaceSprite,    LShapeFaceSprite,
                squareShapeFaceSprite, lineShapeFaceSprite, ZShapeFaceSprite
            };
            return all[Random.Range(0, all.Length)];
        }

        // Returns a random building sprite — used during the flicker frames
        private Sprite GetRandomBuildingSprite()
        {
            Sprite[] all =
            {
                industrialFaceSprite,  residentialFaceSprite, commercialFaceSprite,
                schoolFaceSprite,      parkFaceSprite,        waterFaceSprite
            };
            return all[Random.Range(0, all.Length)];
        }

        // ─────────────────────────────────────────────────────────────────────
        // Roll animation
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Triggers the flicker animation across all 6 dice.
        /// diceManager.RollAllDice() must already have been called before this
        /// so the final face values are decided; this is purely visual.
        /// </summary>
        public void PlayRollAnimation()
        {
            if (isAnimating) return;

            HideWaterDiePanel();
            waterDieClickedThisFrame = false;
            if (shapeWildcardPanel != null)    shapeWildcardPanel.Hide();
            if (buildingWildcardPanel != null)  buildingWildcardPanel.Hide();

            SetDiceButtonsInteractable(false);
            StartCoroutine(RollAnimationSequence());
        }

        private IEnumerator RollAnimationSequence()
        {
            isAnimating = true;

            if (diceManager == null)
            {
                isAnimating = false;
                yield break;
            }

            var shapeDice    = diceManager.GetShapeDice();
            var buildingDice = diceManager.GetBuildingDice();
            int totalDice    = shapeDice.Count + buildingDice.Count;
            int doneCount    = 0;

            // Stagger-launch shape dice
            for (int i = 0; i < shapeDice.Count && i < shapeDiceBackgrounds.Count; i++)
            {
                int     ci           = i;
                Sprite  finalSprite  = GetShapeSprite(shapeDice[ci].GetShapeType());
                string  finalLabel   = shapeDice[ci].GetFaceName();

                StartCoroutine(AnimateSingleDie(
                    shapeDiceBackgrounds[ci],
                    shapeDiceTexts[ci],
                    isShape: true,
                    finalSprite: finalSprite,
                    finalLabel: finalLabel,
                    onComplete: () => doneCount++
                ));

                yield return new WaitForSeconds(dieStaggerDelay);
            }

            // Stagger-launch building dice
            for (int i = 0; i < buildingDice.Count && i < buildingDiceBackgrounds.Count; i++)
            {
                int     ci           = i;
                Sprite  finalSprite  = GetBuildingSprite(buildingDice[ci].GetBuildingType());
                string  finalLabel   = buildingDice[ci].GetFaceName();

                StartCoroutine(AnimateSingleDie(
                    buildingDiceBackgrounds[ci],
                    buildingDiceTexts[ci],
                    isShape: false,
                    finalSprite: finalSprite,
                    finalLabel: finalLabel,
                    onComplete: () => doneCount++
                ));

                yield return new WaitForSeconds(dieStaggerDelay);
            }

            // Wait for all dice to finish their individual animations
            yield return new WaitUntil(() => doneCount >= totalDice);

            // Final update — applies selection colours, double highlights, etc.
            UpdateDiceUI();
            UpdateWildcardUI();
            SetDiceButtonsInteractable(true);
            isAnimating = false;

            Debug.Log("DiceUIManager: Roll animation complete.");
        }

        /// <summary>
        /// Animates one die: flickers through random sprites (fast→slow easing),
        /// then snaps to the real result with a small pop/bounce.
        /// </summary>
        private IEnumerator AnimateSingleDie(
            Image             dieImage,
            TextMeshProUGUI   dieText,
            bool              isShape,
            Sprite            finalSprite,
            string            finalLabel,
            System.Action     onComplete)
        {
            if (dieImage == null) { onComplete?.Invoke(); yield break; }

            // Opening pop
            yield return StartCoroutine(PunchScale(dieImage.transform, 1.15f, 0.06f));

            // Flicker phase — eases from fast to slow
            for (int frame = 0; frame < rollFlickerCount; frame++)
            {
                dieImage.sprite = isShape ? GetRandomShapeSprite() : GetRandomBuildingSprite();

                // Clear the label text while flickering so it doesn't distract
                if (dieText != null) dieText.text = string.Empty;

                float t        = (float)frame / Mathf.Max(rollFlickerCount - 1, 1);
                float interval = Mathf.Lerp(flickerIntervalFast, flickerIntervalSlow, t);
                yield return new WaitForSeconds(interval);
            }

            // Snap to the real result
            dieImage.sprite = finalSprite;
            if (dieText != null) dieText.text = finalLabel;

            // Landing bounce
            yield return StartCoroutine(PunchScale(dieImage.transform, 1.1f, 0.08f));
            dieImage.transform.localScale = Vector3.one;

            onComplete?.Invoke();
        }

        /// <summary>
        /// Scales a transform to peakScale and back over `duration` seconds.
        /// Gives a satisfying pop when a die lands.
        /// </summary>
        private IEnumerator PunchScale(Transform t, float peakScale, float duration)
        {
            float half    = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.Lerp(1f, peakScale, elapsed / half);
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.Lerp(peakScale, 1f, elapsed / half);
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            t.localScale = Vector3.one;
        }

        private void SetDiceButtonsInteractable(bool interactable)
        {
            foreach (var btn in shapeDiceButtons)
                if (btn != null) btn.interactable = interactable;
            foreach (var btn in buildingDiceButtons)
                if (btn != null) btn.interactable = interactable;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public methods (same signatures as before — no GameManager changes needed)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates all dice UI instantly (no animation). Used after selection changes,
        /// wildcard use, etc.
        /// </summary>
        public void UpdateDiceUI()
        {
            if (diceManager == null) return;

            var shapeDice               = diceManager.GetShapeDice();
            var buildingDice            = diceManager.GetBuildingDice();
            var shapeOriginalDoubles    = diceManager.GetOriginalDoubleFaces(DiceType.Shape);
            var buildingOriginalDoubles = diceManager.GetOriginalDoubleFaces(DiceType.Building);

            for (int i = 0; i < shapeDice.Count && i < shapeDiceTexts.Count; i++)
            {
                shapeDiceTexts[i].text        = shapeDice[i].GetFaceName();
                shapeDiceBackgrounds[i].sprite = GetShapeSprite(shapeDice[i].GetShapeType());
                bool isSelected = shapeDice[i].Selected;
                bool isDouble   = shapeOriginalDoubles.Contains(shapeDice[i].CurrentFace);
                shapeDiceBackgrounds[i].color = isSelected ? selectedColor : (isDouble ? doubleColor : defaultColor);
            }

            for (int i = 0; i < buildingDice.Count && i < buildingDiceTexts.Count; i++)
            {
                buildingDiceTexts[i].text        = buildingDice[i].GetFaceName();
                buildingDiceBackgrounds[i].sprite = GetBuildingSprite(buildingDice[i].GetBuildingType());
                bool isSelected = buildingDice[i].Selected;
                bool isDouble   = buildingOriginalDoubles.Contains(buildingDice[i].CurrentFace);
                buildingDiceBackgrounds[i].color = isSelected ? selectedColor : (isDouble ? doubleColor : defaultColor);
            }

            UpdateWaterDieUI();
        }

        /// <summary>
        /// Called by GameManager after RollAllDice(). Now plays the flicker animation
        /// instead of instantly updating — no changes needed in GameManager.
        /// </summary>
        public void OnDiceRolled()
        {
            HideWaterDiePanel();
            waterDieClickedThisFrame = false;
            if (shapeWildcardPanel != null)    shapeWildcardPanel.Hide();
            if (buildingWildcardPanel != null)  buildingWildcardPanel.Hide();

            PlayRollAnimation();
        }

        public void OnSelectionCleared()
        {
            HideWaterDiePanel();
            waterDieClickedThisFrame = false;
            if (shapeWildcardPanel != null)    shapeWildcardPanel.Hide();
            if (buildingWildcardPanel != null)  buildingWildcardPanel.Hide();
            UpdateDiceUI();
            UpdateWildcardUI();
        }

        /// <summary>Kept for compatibility — UpdateDiceUI now handles double colours.</summary>
        public void HighlightDoubleFaces() => UpdateDiceUI();

        // ─────────────────────────────────────────────────────────────────────
        // Click handlers
        // ─────────────────────────────────────────────────────────────────────

        private void OnShapeDieClicked(int index)
        {
            if (diceManager == null || isAnimating) return;
            diceManager.SelectShapeDie(index);
            UpdateDiceUI();
            if (shapeManager != null) shapeManager.UpdateActiveShapeFromSelectedDice();
        }

        private void OnBuildingDieClicked(int index)
        {
            if (diceManager == null || isAnimating) return;
            diceManager.SelectBuildingDie(index);
            if (diceManager.IsSelectedBuildingWater()) waterDieClickedThisFrame = true;
            UpdateDiceUI();
            if (shapeManager != null) shapeManager.UpdateActiveShapeFromSelectedDice();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Water die UI
        // ─────────────────────────────────────────────────────────────────────

        public void ShowWaterDiePanel()
        {
            if (waterDiePanel != null) waterDiePanel.SetActive(true);
            Debug.Log("Water die panel shown.");
        }

        public void HideWaterDiePanel()
        {
            if (waterDiePanel != null) waterDiePanel.SetActive(false);
            Debug.Log("Water die panel hidden.");
        }

        private void OnWaterDieBuildingTypeClicked(BuildingType buildingType)
        {
            if (diceManager == null) return;
            diceManager.SetWaterDieChosenBuildingType(buildingType);
            Debug.Log($"Water die building type chosen: {buildingType}");
            if (shapeManager != null) shapeManager.UpdateActiveShapeFromSelectedDice();
            HideWaterDiePanel();
            waterDieClickedThisFrame = false;
        }

        private void UpdateWaterDieUI()
        {
            if (diceManager == null) return;
            bool isWaterSelected = diceManager.IsSelectedBuildingWater();
            if (isWaterSelected)
            {
                if (!diceManager.IsWaterDieChosenBuildingTypeSet() || waterDieClickedThisFrame)
                    ShowWaterDiePanel();
                else
                    HideWaterDiePanel();
            }
            else
            {
                HideWaterDiePanel();
            }
            waterDieClickedThisFrame = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Wildcard methods
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator EnsureGameManagerReady()
        {
            while (GameManager.Instance == null)
                yield return null;
            UpdateWildcardUI();
            Debug.Log("EnsureGameManagerReady: GameManager.Instance now available.");
        }

        private void OnShapeWildcardButtonClicked()
        {
            Debug.Log($"OnShapeWildcardButtonClicked. GameManager={(GameManager.Instance != null ? "set" : "null")}");
            if (GameManager.Instance == null) { UpdateWildcardUI(); return; }
            if (!GameManager.Instance.CanUseWildcard()) return;

            wildcardTargetShapeDieIndex = GetTargetShapeDieIndex();
            if (shapeWildcardPanel != null) shapeWildcardPanel.Show();
            else Debug.LogError("Shape wildcard panel not assigned.");
        }

        private void OnBuildingWildcardButtonClicked()
        {
            Debug.Log($"OnBuildingWildcardButtonClicked. GameManager={(GameManager.Instance != null ? "set" : "null")}");
            if (GameManager.Instance == null) { UpdateWildcardUI(); return; }
            if (!GameManager.Instance.CanUseWildcard()) return;

            wildcardTargetBuildingDieIndex = GetTargetBuildingDieIndex();
            if (buildingWildcardPanel != null) buildingWildcardPanel.Show();
            else Debug.LogError("Building wildcard panel not assigned.");
        }

        private void OnShapeWildcardSelected(int faceIndex)
        {
            Debug.Log($"Shape wildcard selected: face {faceIndex}, die {wildcardTargetShapeDieIndex}");
            if (diceManager == null || GameManager.Instance == null) return;
            diceManager.ApplyWildcardOverride(DiceType.Shape, wildcardTargetShapeDieIndex, faceIndex);
            if (!GameManager.Instance.UseWildcard()) return;
            UpdateDiceUI();
            UpdateWildcardUI();
            if (shapeManager != null) shapeManager.UpdateActiveShapeFromSelectedDice();
        }

        private void OnBuildingWildcardSelected(int faceIndex)
        {
            Debug.Log($"Building wildcard selected: face {faceIndex}, die {wildcardTargetBuildingDieIndex}");
            if (diceManager == null || GameManager.Instance == null) return;
            diceManager.ApplyWildcardOverride(DiceType.Building, wildcardTargetBuildingDieIndex, faceIndex);
            if (!GameManager.Instance.UseWildcard()) return;
            UpdateDiceUI();
            UpdateWildcardUI();
            if (shapeManager != null) shapeManager.UpdateActiveShapeFromSelectedDice();
        }

        private int GetTargetShapeDieIndex()
        {
            if (diceManager == null) return 0;
            var dice = diceManager.GetShapeDice();
            for (int i = 0; i < dice.Count; i++)
                if (dice[i].Selected) return i;
            return 0;
        }

        private int GetTargetBuildingDieIndex()
        {
            if (diceManager == null) return 0;
            var dice = diceManager.GetBuildingDice();
            for (int i = 0; i < dice.Count; i++)
                if (dice[i].Selected) return i;
            return 0;
        }

        private void UpdateWildcardUI()
        {
            Debug.Log($"UpdateWildcardUI. GameManager={(GameManager.Instance != null ? "set" : "null")}");
            if (GameManager.Instance == null)
            {
                if (wildcardCountText != null)      wildcardCountText.text      = "-/-";
                if (wildcardCostText != null)       wildcardCostText.text       = "Cost: -";
                if (shapeWildcardButton != null)    shapeWildcardButton.interactable    = false;
                if (buildingWildcardButton != null) buildingWildcardButton.interactable = false;
                return;
            }

            bool canUse    = GameManager.Instance.CanUseWildcard();
            int  remaining = GameManager.MAX_WILDCARDS - GameManager.Instance.WildcardsUsed;
            int  nextCost  = GameManager.Instance.GetNextWildcardCost();

            if (wildcardCountText != null)      wildcardCountText.text      = $"{remaining}/{GameManager.MAX_WILDCARDS}";
            if (wildcardCostText != null)       wildcardCostText.text       = $"Cost: {nextCost}";
            if (shapeWildcardButton != null)    shapeWildcardButton.interactable    = canUse;
            if (buildingWildcardButton != null) buildingWildcardButton.interactable = canUse;

            Debug.Log($"UpdateWildcardUI: canUse={canUse}, shape={shapeWildcardButton?.interactable}, building={buildingWildcardButton?.interactable}");
        }
    }
}
