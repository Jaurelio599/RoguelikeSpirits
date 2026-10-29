using UnityEngine;

namespace SpiritPrototype
{
    public enum PlayerFacing { South, North, West, East }

    [CreateAssetMenu(menuName = "Espiritus/Animacion del jugador", fileName = "PlayerWalkSet")]
    public sealed class PlayerWalkSet : ScriptableObject
    {
        public Sprite[] south = new Sprite[4], north = new Sprite[4], west = new Sprite[4], east = new Sprite[4];
        [Min(.1f)] public float framesPerSecond = 8;
        [Range(0, 3)] public int idleFrame = 1;
        public Sprite[] Frames(PlayerFacing direction)
        {
            switch (direction)
            {
                case PlayerFacing.North: return north;
                case PlayerFacing.West: return west;
                case PlayerFacing.East: return east;
                default: return south;
            }
        }
        public bool IsValid()
        {
            if (framesPerSecond <= 0 || float.IsNaN(framesPerSecond) || float.IsInfinity(framesPerSecond) || idleFrame < 0 || idleFrame > 3) return false;
            foreach (PlayerFacing direction in new[] {PlayerFacing.South, PlayerFacing.North, PlayerFacing.West, PlayerFacing.East})
            {
                var frames = Frames(direction); if (frames == null || frames.Length != 4) return false;
                foreach (var frame in frames) if (frame == null) return false;
            }
            return true;
        }
    }

    // Pure timing state: no Unity asset loads or field-initializer engine calls.
    public sealed class PlayerWalkClock
    {
        private PlayerFacing facing = PlayerFacing.South;
        private double phase;
        public int Tick(PlayerFacing next, bool moving, float deltaTime, float fps, int idle)
        {
            if (!moving) { facing = next; phase = 0; return idle; }
            if (next != facing) { facing = next; phase = 0; return 0; }
            phase = (phase + System.Math.Max(0, deltaTime) * fps) % 4;
            return (int)phase;
        }
        public void Reset() { phase = 0; }
    }
}
