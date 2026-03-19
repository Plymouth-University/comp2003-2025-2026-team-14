using UnityEngine;

namespace PocketPlanner.Core
{
    /// <summary>
    /// Attach this to a flat plane GameObject to act as the floor that dice
    /// bounce off during the physics roll animation.
    ///
    /// ── Quick Setup ─────────────────────────────────────────────────────────
    /// 1. Create: GameObject > 3D Object > Plane (or a thin Cube).
    /// 2. Add this script.
    /// 3. Call SetupFloor() or let Awake() handle it automatically.
    /// 4. Optionally create a PhysicsMaterial in the Project window and assign
    ///    it for realistic bouncing (see CreateBouncyPhysicsMaterial()).
    ///
    /// ── Recommended Physics Material Values ─────────────────────────────────
    ///   Dynamic Friction : 0.4
    ///   Static Friction  : 0.4
    ///   Bounciness       : 0.3   ← gives a satisfying but not crazy bounce
    ///   Friction Combine : Average
    ///   Bounce Combine   : Average
    ///
    /// ── Recommended Rigidbody Values on each DiceVisual ────────────────────
    ///   Mass             : 0.1
    ///   Drag             : 0.3
    ///   Angular Drag     : 0.5
    ///   Collision Detection: Continuous  (prevents tunnelling through floor)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DiceFloor : MonoBehaviour
    {
        [Header("Optional: auto-create a physics material at runtime")]
        [SerializeField] private bool autoCreatePhysicsMaterial = true;
        [SerializeField] private float bounciness = 0.3f;
        [SerializeField] private float friction    = 0.4f;

        private void Awake()
        {
            SetupFloor();
        }

        /// <summary>
        /// Ensures the floor has a collider and optionally a physics material.
        /// </summary>
        public void SetupFloor()
        {
            var col = GetComponent<Collider>();

            if (autoCreatePhysicsMaterial)
                col.material = CreateBouncyPhysicsMaterial();
        }

        /// <summary>
        /// Creates a PhysicsMaterial at runtime with realistic dice-bouncing values.
        /// You can also create one manually in the Project window (preferred for tuning).
        /// </summary>
        private PhysicsMaterial CreateBouncyPhysicsMaterial()
        {
            var mat = new PhysicsMaterial("DiceFloorMat")
            {
                dynamicFriction  = friction,
                staticFriction   = friction,
                bounciness       = bounciness,
                frictionCombine  = PhysicsMaterialCombine.Average,
                bounceCombine    = PhysicsMaterialCombine.Average
            };
            return mat;
        }
    }
}
