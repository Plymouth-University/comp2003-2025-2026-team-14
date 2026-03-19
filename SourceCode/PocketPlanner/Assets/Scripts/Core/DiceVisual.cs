using System;
using System.Collections;
using UnityEngine;

namespace PocketPlanner.Core
{
    /// <summary>
    /// Attached to each 3D die GameObject.
    /// Handles the physics-based tumble animation and snapping to the correct
    /// face texture once the die has settled.
    ///
    /// Setup requirements per die GameObject:
    ///   - Rigidbody          (is NOT kinematic at start; we toggle it)
    ///   - BoxCollider        (standard cube collider)
    ///   - MeshRenderer       (uses a DiceFaceMaterial with a 3x2 texture atlas)
    ///   - This script
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(MeshRenderer))]
    public class DiceVisual : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Physics Settings")]
        [Tooltip("How hard the die is thrown upward on roll.")]
        [SerializeField] private float throwForce = 5f;

        [Tooltip("Random torque magnitude applied on roll.")]
        [SerializeField] private float torqueMagnitude = 12f;

        [Tooltip("Seconds of near-zero velocity before we consider the die settled.")]
        [SerializeField] private float settleThreshold = 0.08f;

        [Tooltip("How long we wait for the die to settle before forcing a result.")]
        [SerializeField] private float maxSettleWait = 3.5f;

        [Header("Face Rendering")]
        [Tooltip("One material per face (0-5). Assign in order: +Y, -Y, +X, -X, +Z, -Z.")]
        [SerializeField] private Material[] faceMaterials = new Material[6];

        // ── Runtime state ────────────────────────────────────────────────────
        private Rigidbody rb;
        private MeshRenderer meshRenderer;

        private int targetFaceIndex = 0;   // set by DiceAnimationManager before roll
        private bool isRolling = false;
        private Action onSettled;           // callback → DiceAnimationManager

        // World-up axis mapped to Unity cube face indices
        // Face order MUST match faceMaterials array order.
        // For a standard Unity cube:
        //   Face 0 → +Y (top)
        //   Face 1 → -Y (bottom)
        //   Face 2 → +X (right)
        //   Face 3 → -X (left)
        //   Face 4 → +Z (front)
        //   Face 5 → -Z (back)
        private static readonly Vector3[] FaceNormals =
        {
            Vector3.up,
            Vector3.down,
            Vector3.right,
            Vector3.left,
            Vector3.forward,
            Vector3.back
        };

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            meshRenderer = GetComponent<MeshRenderer>();

            // Start kinematic so dice don't fall before the game starts
            rb.isKinematic = true;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Called by DiceAnimationManager before launching the roll.
        /// Stores which face should be face-up when the die settles.
        /// </summary>
        public void PrepareRoll(int faceIndex, Action onSettledCallback)
        {
            targetFaceIndex = Mathf.Clamp(faceIndex, 0, 5);
            onSettled = onSettledCallback;
        }

        /// <summary>
        /// Physically launches the die into the air with random spin.
        /// Call PrepareRoll() before this.
        /// </summary>
        public void LaunchRoll()
        {
            if (isRolling) return;

            // Reset physics state
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Random upward throw + slight horizontal scatter
            Vector3 throwDir = new Vector3(
                UnityEngine.Random.Range(-0.3f, 0.3f),
                1f,
                UnityEngine.Random.Range(-0.3f, 0.3f)
            ).normalized;

            rb.AddForce(throwDir * throwForce, ForceMode.Impulse);

            // Random tumble torque
            rb.AddTorque(UnityEngine.Random.insideUnitSphere * torqueMagnitude, ForceMode.Impulse);

            isRolling = true;
            StartCoroutine(WaitForSettle());
        }

        /// <summary>
        /// Instantly snaps the die to show a specific face without animation.
        /// Used when restoring game state.
        /// </summary>
        public void ShowFaceImmediate(int faceIndex)
        {
            targetFaceIndex = Mathf.Clamp(faceIndex, 0, 5);
            rb.isKinematic = true;
            SnapToTargetFace();
            ApplyFaceMaterial();
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Polls velocity until the die is nearly still, then snaps orientation
        /// and fires the settled callback.
        /// </summary>
        private IEnumerator WaitForSettle()
        {
            float elapsed = 0f;

            // Brief delay so the die actually starts moving before we check velocity
            yield return new WaitForSeconds(0.4f);

            while (elapsed < maxSettleWait)
            {
                elapsed += Time.deltaTime;

                bool almostStopped = rb.linearVelocity.magnitude < settleThreshold
                                  && rb.angularVelocity.magnitude < settleThreshold;

                if (almostStopped)
                    break;

                yield return null;
            }

            // Die has settled (or timed out) — freeze it and show the right face
            rb.isKinematic = true;
            SnapToTargetFace();
            ApplyFaceMaterial();

            isRolling = false;
            onSettled?.Invoke();
        }

        /// <summary>
        /// Rotates the die so that the target face normal points straight up.
        /// This makes it look like the correct face landed on top.
        /// </summary>
        private void SnapToTargetFace()
        {
            // The face that should end up on top
            Vector3 desiredFaceNormal = FaceNormals[targetFaceIndex];

            // We want desiredFaceNormal (in local space) to point toward world-up.
            // Quaternion.FromToRotation gives us the rotation that takes
            // desiredFaceNormal → Vector3.up.
            Quaternion snapRotation = Quaternion.FromToRotation(desiredFaceNormal, Vector3.up);

            // Apply a clean 90-degree-aligned version to avoid any float drift
            transform.rotation = RoundToNearest90(snapRotation);
        }

        /// <summary>
        /// Snaps a quaternion to the nearest 90-degree-aligned rotation,
        /// keeping the die perfectly axis-aligned after settling.
        /// </summary>
        private Quaternion RoundToNearest90(Quaternion q)
        {
            Vector3 euler = q.eulerAngles;
            euler.x = Mathf.Round(euler.x / 90f) * 90f;
            euler.y = Mathf.Round(euler.y / 90f) * 90f;
            euler.z = Mathf.Round(euler.z / 90f) * 90f;
            return Quaternion.Euler(euler);
        }

        /// <summary>
        /// Swaps the MeshRenderer's material to the one representing the top face.
        /// If you use a single shared material with a texture atlas, swap UV offsets here instead.
        /// </summary>
        private void ApplyFaceMaterial()
        {
            if (faceMaterials == null || faceMaterials.Length != 6) return;
            if (faceMaterials[targetFaceIndex] == null) return;

            // For a simple cube with one sub-mesh per face, you'd set all materials.
            // This sets a single shared material; see DiceFaceAtlas approach below
            // if you're using UV-offset icon mapping.
            meshRenderer.material = faceMaterials[targetFaceIndex];
        }
    }
}
