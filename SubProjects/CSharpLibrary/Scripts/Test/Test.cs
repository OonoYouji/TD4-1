using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum TestEnum {
    None,
    First,
    Second,
    Third,
    Final
}

public class Test : MonoScript {

    [SerializeField] float testValue = 0.0f;
    [SerializeField] float gizmoThickness = 4.0f;

    // BUG-001: Enum Support
    [SerializeField] TestEnum testEnum = TestEnum.None;

    // BUG-003: List Support
    [SerializeField] List<int> testIntList = new List<int>();
    [SerializeField] List<Vector3> testVectorList = new List<Vector3>();

    public override void Initialize() {
        Debug.Log("Test script initialized.");
    }

    public override void Update() {
        // --- Gizmo表示のテスト (太さをSerializeFieldから取得) ---
        Vector3 center = transform.position;
        
        // 1. 赤い太い円 (半径5.0、地面から少し浮かす)
        GizmoBatch.DrawWireCircle(center + Vector3.up * 0.1f, 5.0f, new Vector4(1, 0, 0, 1), 32, gizmoThickness);
        
        // 2. 青い太い線 (真上に3.0)
        GizmoBatch.DrawLine(center, center + Vector3.up * 3.0f, new Vector4(0, 0, 1, 1), gizmoThickness);
        
        // 3. 黄色い太い円 (横向き)
        GizmoBatch.DrawWireCircle(center + Vector3.up * 1.5f, 3.0f, Vector3.forward, new Vector4(1, 1, 0, 1), 24, gizmoThickness);

        // BUG-002: Window Size access
        if (Input.TriggerKey(KeyCode.Space)) {
            Vector2 size = Window.Size;
            Debug.Log("========================================");
            Debug.Log("WINDOW SIZE CHECK");
            Debug.Log("Width: " + size.x);
            Debug.Log("Height: " + size.y);
            Debug.Log("Aspect Ratio: " + (size.x / size.y));
            Debug.Log("========================================");
        }

        if (Input.TriggerKey(KeyCode.Return)) {
            Debug.Log("Current Enum: " + testEnum);
            Debug.Log("Int List Count: " + testIntList.Count);
            foreach (var i in testIntList) Debug.Log(" - " + i);
        }
    }
}

