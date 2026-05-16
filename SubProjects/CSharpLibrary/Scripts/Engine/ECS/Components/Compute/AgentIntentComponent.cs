using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

/// <summary>
/// AIの「意図」を格納するコンポーネント
/// C#側で計算され、C++側で行動に変換される
/// </summary>
public class AgentIntentComponent : Component {
    /// <summary>
    /// C++とC#でデータを一括同期するための構造体
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchData {
        public uint compId;
        public Vector3 desiredMoveDirection;
        public byte isAttacking; // Use byte for bool interop
        public uint targetEntityId;
    }

    public Vector3 desiredMoveDirection = Vector3.zero;
    public bool isAttacking = false;
    public uint targetEntityId = 0; // 0 is considered an invalid ID
}
