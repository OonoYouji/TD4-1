using System;
public class Player : MonoScript
{

    // =========================================================
    // パラメーター
    // =========================================================

    // 移動速度の最大値
    [SerializeField] public float maxMoveSpeed = 10.0f;

    // 目標方向にリニアで回転する速さ
    [SerializeField] public float rotationSpeed = 8.0f;

    // 腕の長さ
    [SerializeField] public float armLength = 2.0f;

    // 弾の発射間隔（クールタイム）
    [SerializeField] public float fireInterval = 0.5f;


    // =========================================================
    // 内部状態
    // =========================================================

    // 入力のデッドゾーン
    private const float inputDeadZone = 0.5f;
    // マウスが移動とみなすピクセル距離
    private const float mouseIsMovePixel = 10.0f;
    // 現在のY軸回転角
    private float currentYaw = 0.0f;
    // ゲームパッド入力モードかどうか
    private bool isGamepadMode = false;
    // 最後のフレームのマウス位置
    private Vector2 lastMousePos = new Vector2(640f, 360f);
    // 発射クールタイムタイマー
    private float fireCooldownTimer;
    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        currentYaw = transform.rotate.ToEuler().y;
        lastMousePos = Input.MousePosition();
        fireCooldownTimer = fireInterval;

    }

    public override void Update()
    {
        // 入力モードの更新
        UpdateInputMode();
        // 移動処理
        HandleMovement();
        // 回転処理
        HandleRotation();
        // 発射処理
        HandleFiring();
    }

    private void UpdateInputMode()
    {
        // スティック判定
        bool anyStick =
            Input.GamepadThumb(GamepadAxis.LeftThumb).Length() > 0.1f ||
            Input.GamepadThumb(GamepadAxis.RightThumb).Length() > 0.1f;

        // キー判定
        bool anyKey =
            Input.PressKey(KeyCode.W) || Input.PressKey(KeyCode.A) ||
            Input.PressKey(KeyCode.S) || Input.PressKey(KeyCode.D) ||
            Input.PressKey(KeyCode.UpArrow) || Input.PressKey(KeyCode.DownArrow) ||
            Input.PressKey(KeyCode.LeftArrow) || Input.PressKey(KeyCode.RightArrow);

        // マウス移動量判定 
        Vector2 currentMousePos = Input.MousePosition();
        bool mouseMoved = (currentMousePos - lastMousePos).Length() > mouseIsMovePixel;
        lastMousePos = currentMousePos;

        // 入力モードの更新
        if (anyStick) { isGamepadMode = true; }
        if (anyKey || mouseMoved) { isGamepadMode = false; }
    }


    private void HandleMovement()
    {

        // キー入力で向きの決定
        Vector2 input = new Vector2(0f, 0f);
        if (Input.PressKey(KeyCode.W) || Input.PressKey(KeyCode.UpArrow)) { input.y += 1f; }
        if (Input.PressKey(KeyCode.S) || Input.PressKey(KeyCode.DownArrow)) { input.y -= 1f; }
        if (Input.PressKey(KeyCode.A) || Input.PressKey(KeyCode.LeftArrow)) { input.x -= 1f; }
        if (Input.PressKey(KeyCode.D) || Input.PressKey(KeyCode.RightArrow)) { input.x += 1f; }

        // 入力の大きさと速度の計算
        float speed = maxMoveSpeed;
        float magnitude = input.Length();

        // キー入力がなければスティックを使う
        if (magnitude < 0.001f)
        {
            // スティック入力で向きの決定
            Vector2 leftStick = Input.GamepadThumb(GamepadAxis.LeftThumb);
            magnitude = leftStick.Length(); 

            // スティックでの移動を適応
            if (magnitude > 0.001f)
            {
                input = leftStick;
                speed = maxMoveSpeed * Mathf.Clamp(magnitude, 0.0f, 1.0f);
            }
        }

        // キー入力での移動
        if (magnitude > 0.001f)
        {
            // 正規化
            Vector2 dir = input.Normalized();
            Vector3 pos = transform.position;
            pos.x += dir.x * speed * Time.deltaTime;
            pos.z += dir.y * speed * Time.deltaTime;
            transform.position = pos;
        }
    }

    // =========================================================
    // 回転
    // =========================================================

    private void HandleRotation()
    {
        float targetYaw = currentYaw;
        bool hasTarget = false;

        // ゲームパッドでの回転
        if (isGamepadMode)
        {
            // 右スティックの入力で向きの決定
            Vector2 rightStick = Input.GamepadThumb(GamepadAxis.RightThumb);
            if (rightStick.Length() > inputDeadZone)
            {
                targetYaw = Mathf.Atan2(rightStick.x, rightStick.y);
                hasTarget = true;
            }
        }
        else
        {
            Vector2 mouse = Input.MousePosition();
            // 画面中心(640,360)からのオフセットベクトル
            Vector2 offset = new Vector2(mouse.x - 640f, -(mouse.y - 360f));
            if (offset.Length() > inputDeadZone)
            {
                targetYaw = Mathf.Atan2(offset.x, offset.y);
                hasTarget = true;
            }
        }

        if (hasTarget)
        {
            // Yの回転の計算
            currentYaw = LerpShortAngle(currentYaw, targetYaw, rotationSpeed * Time.deltaTime);
        }
        // 回転の適用
        transform.rotate = Quaternion.FromEuler(new Vector3(0f, currentYaw, 0f));
    }

    private float LerpShortAngle(float a, float b, float t)
    {
        float diff = b - a;
        while (diff > Mathf.PI)
        {
            diff -= 2.0f * Mathf.PI;
        }
        while (diff < -Mathf.PI)
        {
            diff += 2.0f * Mathf.PI;
        }

        return a + diff * t;
    }

    // =========================================================
    // 発射
    // =========================================================

    private void HandleFiring()
    {
        fireCooldownTimer += Time.deltaTime;

        // 右クリック または ゲームパッド LB/RB
        bool wantFire =
            Input.PressMouse(Mouse.Right) ||
            Input.PressGamepad(Gamepad.LeftShoulder) ||
            Input.PressGamepad(Gamepad.RightShoulder);

        if (wantFire && fireCooldownTimer >= fireInterval)
        {
            fireCooldownTimer = 0.0f;
        }
    }
}
