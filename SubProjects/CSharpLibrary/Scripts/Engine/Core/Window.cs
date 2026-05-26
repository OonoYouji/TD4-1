using System;
using System.Runtime.CompilerServices;

public static class Window {
    public static Vector2 Size {
        get {
            InternalGetSize(out Vector2 size);
            return size;
        }
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void InternalGetSize(out Vector2 size);
}
