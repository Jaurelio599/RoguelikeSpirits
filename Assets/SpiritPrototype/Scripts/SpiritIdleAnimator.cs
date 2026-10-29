using UnityEngine;
namespace SpiritPrototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpiritIdleAnimator : MonoBehaviour
    {
        [SerializeField] private SpiritDefinition definition;
        private SpriteRenderer visual;
        public void Configure(SpiritDefinition value) { definition = value; }
        private void Awake() { visual = GetComponent<SpriteRenderer>(); }
        private void LateUpdate()
        {
            if (definition == null || visual == null) return;
            var sprite = definition.FrameAt(Time.unscaledTime);
            if (sprite != null) visual.sprite = sprite;
        }
    }
}
