# C#スクリプトの新規追加手順

本プロジェクトでは、C#スクリプトを使用してゲームロジックを記述します。スクリプトの追加から反映までの手順は以下の通りです。

## 1. スクリプトファイルの作成

1. `SubProjects/CSharpLibrary/Scripts/Game` フォルダ（またはそのサブフォルダ）に新しい `.cs` ファイルを作成します。
2. クラスを作成し、`MonoScript` クラスを継承させます。

```csharp
using System;

public class MyNewScript : MonoScript
{
    // インスペクターに表示したいフィールドには [SerializeField] を付けます
    [SerializeField] public float moveSpeed = 5.0f;

    public override void Initialize()
    {
        // 初期化処理
        Debug.Log("MyNewScript Initialized!");
    }

    public override void Update()
    {
        // 毎フレームの更新処理
        // transform.position += Vector3.forward * moveSpeed * Time.deltaTime;
    }
}
```

## 2. コンパイルと反映

1. Visual Studio 等で `SubProjects/CSharpLibrary/CSharpLibrary.sln` を開き、ビルド（ビルド -> ソリューションのビルド）を実行します。
2. ビルドが成功すると、`Project/Packages/Scripts` に `CSharpLibrary.dll` が出力されます。
3. エンジン実行中の場合は、自動的に最新のDLLがロードされるか、エディター機能を通じてリロードされます。
   - ※ビルド時にDLLがロックされている場合は、エンジンを一度終了するか、ビルドスクリプトによるリネーム処理が走ります。

## 3. エンティティへのアタッチ

1. エディターの **Hierarchy** ウィンドウで、スクリプトを追加したいエンティティを選択します。
2. **Inspector** ウィンドウの「Add Component」から `Script` コンポーネントを追加します。
3. `Script` コンポーネント内のリストに、作成したスクリプトをドラッグドロップします。
4. 正しく追加されると、インスペクター上にスクリプトの名前が表示され、`[SerializeField]` を付けた変数が編集可能になります。

---
## 補足：ライフサイクルメソッド

- `Initialize()`: スクリプトが有効になった最初のフレームの更新前に呼ばれます。
- `Update()`: 毎フレーム呼ばれます。
- `OnCollisionEnter(Entity collision)`: 他のコライダーと接触した瞬間に呼ばれます。
