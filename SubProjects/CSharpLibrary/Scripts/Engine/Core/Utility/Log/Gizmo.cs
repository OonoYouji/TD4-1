using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct GizmoBatchLineData
{
    public Vector3 startPosition;
    public Vector3 endPosition;
    public Vector4 color;
}

public static class GizmoBatch
{
    private static List<GizmoBatchLineData> _lineBuffer = new List<GizmoBatchLineData>();

    public static void DrawLine(Vector3 start, Vector3 end)
    {
        DrawLine(start, end, new Vector4(1, 1, 1, 1));
    }

    public static void DrawLine(Vector3 start, Vector3 end, Vector4 color)
    {
        _lineBuffer.Add(new GizmoBatchLineData { startPosition = start, endPosition = end, color = color });
    }

    public static void DrawRay(Vector3 pos, Vector3 dir)
    {
        DrawRay(pos, dir, new Vector4(1, 1, 1, 1));
    }

    public static void DrawRay(Vector3 pos, Vector3 dir, Vector4 color)
    {
        _lineBuffer.Add(new GizmoBatchLineData { startPosition = pos, endPosition = pos + dir, color = color });
    }

    /// <summary>
    /// フレームの終わりにC#からC++へ描画データを一括送信する。
    /// ECSGroup.UpdateEntities などから呼び出されることを想定。
    /// </summary>
    public static void SubmitBatch()
    {
        if (_lineBuffer.Count == 0) return;

        try
        {
            GizmoBatchLineData[] batch = _lineBuffer.ToArray();
            Internal_SubmitLineBatch(batch, batch.Length);
        }
        finally
        {
            _lineBuffer.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void Internal_SubmitLineBatch(GizmoBatchLineData[] batch, int count);
}
