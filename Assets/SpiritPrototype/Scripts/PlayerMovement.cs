using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SpiritPrototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float speed = 3f;
        [SerializeField] private Sprite south;
        [SerializeField] private Sprite north;
        private Rigidbody2D body;
        private SpriteRenderer visual;
        private Vector2 direction;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            visual = GetComponent<SpriteRenderer>();
            if (south == null) south = visual.sprite;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
#if !ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Debug.LogError("Activa Input System o Input Manager en Player Settings > Active Input Handling.", this);
#endif
        }

        private void Update()
        {
            Vector2 raw = ReadDirection();
            // Continuous movement in FOUR directions, without diagonal speed gain.
            // If two perpendicular keys are held equally, horizontal takes priority.
            if (raw.sqrMagnitude < 0.04f) direction = Vector2.zero;
            else if (Mathf.Abs(raw.x) >= Mathf.Abs(raw.y))
                direction = new Vector2(Mathf.Sign(raw.x), 0f);
            else direction = new Vector2(0f, Mathf.Sign(raw.y));

            if (direction.y > 0f && north != null) visual.sprite = north;
            else if (direction != Vector2.zero && south != null) visual.sprite = south;
        }

        private void FixedUpdate()
        {
            // Dynamic physics resolves contacts with static Collider2D walls.
            body.linearVelocity = direction * speed;
        }

        private void OnDisable()
        {
            direction = Vector2.zero;
            if (body != null) body.linearVelocity = Vector2.zero;
        }

        private static Vector2 ReadDirection()
        {
            Vector2 value = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                value.x = ((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) ? 1f : 0f)
                        - ((keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) ? 1f : 0f);
                value.y = ((keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) ? 1f : 0f)
                        - ((keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) ? 1f : 0f);
            }
            if (value == Vector2.zero && Gamepad.current != null)
            {
                value = Gamepad.current.dpad.ReadValue();
                if (value.sqrMagnitude < 0.04f) value = Gamepad.current.leftStick.ReadValue();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            value.x = ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) ? 1f : 0f)
                    - ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) ? 1f : 0f);
            value.y = ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) ? 1f : 0f)
                    - ((Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) ? 1f : 0f);
#endif
            return value;
        }
    }
}
