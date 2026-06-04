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

    public static void DrawWireCircle(Vector3 center, float radius, Vector4 color, int segments = 24)
    {
        DrawWireCircle(center, radius, Vector3.up, color, segments);
    }

    public static void DrawWireCircle(Vector3 center, float radius, Vector3 up = default, Vector4 color = default, int segments = 24)
    {
        if (up.x == 0 && up.y == 0 && up.z == 0) up = Vector3.up;
        if (color.x == 0 && color.y == 0 && color.z == 0 && color.w == 0) color = new Vector4(1, 1, 1, 1);

        Vector3 forward = (Math.Abs(up.y) > 0.99f) ? Vector3.forward : Vector3.up;
        Vector3 right = Vector3.Cross(up, forward).Normalized();
        forward = Vector3.Cross(right, up).Normalized();

        Vector3 prevPoint = center + right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2.0f;
            Vector3 point = center + (right * (float)Math.Cos(angle) + forward * (float)Math.Sin(angle)) * radius;
            DrawLine(prevPoint, point, color);
            prevPoint = point;
        }
    }

    public static void DrawWireArc(Vector3 center, float radius, Vector4 color, float angle, int segments = 12)
    {
        DrawWireArc(center, radius, Vector3.up, Vector3.forward, angle, color, segments);
    }

    public static void DrawWireArc(Vector3 center, float radius, Vector3 up, Vector3 forward, float angle, Vector4 color, int segments = 12)
    {
        Vector3 right = Vector3.Cross(up, forward).Normalized();
        Vector3 prevPoint = center + (forward * (float)Math.Cos(-angle * 0.5f * Mathf.Deg2Rad) + right * (float)Math.Sin(-angle * 0.5f * Mathf.Deg2Rad)) * radius;

        float startAngle = -angle * 0.5f;
        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = (startAngle + (i / (float)segments) * angle) * Mathf.Deg2Rad;
            Vector3 point = center + (forward * (float)Math.Cos(currentAngle) + right * (float)Math.Sin(currentAngle)) * radius;
            DrawLine(prevPoint, point, color);
            prevPoint = point;
        }
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
