using UnityEngine;

namespace SpiritPrototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement), typeof(SpriteRenderer))]
    public sealed class PlayerSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerWalkSet walkSet;
        private readonly PlayerWalkClock clock = new PlayerWalkClock();
        private PlayerMovement movement;
        private SpriteRenderer visual;
        private bool valid;
        public void Configure(PlayerWalkSet value) { walkSet = value; }
        private void Awake()
        {
            movement = GetComponent<PlayerMovement>(); visual = GetComponent<SpriteRenderer>();
            valid = walkSet != null && walkSet.IsValid();
            if (!valid) Debug.LogWarning("Prepara la animacion desde Tools > Espiritus > 9. Se conservaran los sprites estaticos.", this);
        }
        private void LateUpdate()
        {
            if (!valid || movement == null || visual == null) return;
            int frame = clock.Tick(movement.Facing, movement.IsMoving, Time.deltaTime, walkSet.framesPerSecond, walkSet.idleFrame);
            visual.sprite = walkSet.Frames(movement.Facing)[frame];
        }
        private void OnDisable() { clock.Reset(); }
    }
}
