using UnityEngine;

namespace Q3Movement
{
    public enum MovementCommandType
    {
        Move,
        Jump,
        Crouch,
        Walk
    }

    /// <summary>
    /// Single movement action requested by an external command provider.
    /// Data is interpreted by Q3PlayerController based on Type.
    /// </summary>
    public struct MovementCommand
    {
        public MovementCommandType Type;
        public object Data;

        public MovementCommand(MovementCommandType type, object data)
        {
            Type = type;
            Data = data;
        }

        public T GetData<T>()
        {
            return (T)Data;
        }

        public static MovementCommand Move(Vector3 localMove)
        {
            localMove.y = 0f;
            return new MovementCommand(MovementCommandType.Move, localMove);
        }

        public static MovementCommand Jump(bool requested = true)
        {
            return new MovementCommand(MovementCommandType.Jump, requested);
        }

        public static MovementCommand Crouch(bool held)
        {
            return new MovementCommand(MovementCommandType.Crouch, held);
        }

        public static MovementCommand Walk(bool held)
        {
            return new MovementCommand(MovementCommandType.Walk, held);
        }
    }
}
