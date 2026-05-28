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

    // BUG-001: Enum Support
    [SerializeField] TestEnum testEnum = TestEnum.None;

    // BUG-003: List Support
    [SerializeField] List<int> testIntList = new List<int>();
    [SerializeField] List<Vector3> testVectorList = new List<Vector3>();

    public override void Initialize() {
        Debug.Log("Test script initialized.");
    }

    public override void Update() {
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

