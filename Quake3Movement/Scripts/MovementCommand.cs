using UnityEngine;

namespace Q3Movement
{
    public readonly struct JumpCommand
    {
        public bool Requested { get; }
        public bool RepeatWhileHeld { get; }

        public JumpCommand(bool requested, bool repeatWhileHeld)
        {
            Requested = requested;
            RepeatWhileHeld = repeatWhileHeld;
        }
    }

    /// <summary>
    /// Complete movement request submitted for one controller update.
    /// </summary>
    public readonly struct MovementCommandSet
    {
        public Vector2 Move { get; }
        public JumpCommand Jump { get; }
        public bool Crouch { get; }
        public bool Walk { get; }

        public MovementCommandSet(
            Vector2 move,
            JumpCommand jump,
            bool crouch,
            bool walk
        )
        {
            Move = move;
            Jump = jump;
            Crouch = crouch;
            Walk = walk;
        }
    }
}
