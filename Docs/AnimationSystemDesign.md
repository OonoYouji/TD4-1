# SkinMesh Animation System 拡張設計書 (改訂版 v2)

## 1. 概要と目的
本設計は、高性能なキャラクター制御を実現するため、既存の `SkinMeshRenderer` を拡張し、アニメーションのブレンド、レイヤー制御、およびボーンマスク機能を導入します。特に、ランタイムのパフォーマンスを最大化するため、**文字列ルックアップの完全な排除**と**固定長バッファによるメモリレイアウトの最適化**を基本原則とします。

---

## 2. コアとなる概念とデータ構造の最適化

### 2.1. AnimationClip (アセット) と ID 参照
アニメーションデータは `AnimationClip` として独立させ、ランタイムではボーン名による検索を一切行いません。
*   **Hash-based Access**: ボーン名はロード時に `uint32_t` (CRC32等) のハッシュ値に変換されます。
*   **Joint Index Cache**: `Animator` の初期化時に、モデルの `Skeleton` 内のジョイントインデックスとアニメーションカーブのインデックスを対応付けたキャッシュテーブルを構築します。これにより、Updateループ内での検索コストを O(1) に抑えます。
*   **Animation Events**: `AnimationClip` 内に、特定の正規化時間（0.0〜1.0）で発火するイベント定義（文字列IDまたはハッシュID）を保持します。

### 2.2. Animator コンポーネント (固定長管理)
動的なメモリ確保によるキャッシュミスや、C# 連携時の GC 負荷を抑えるため、レイヤーとステートの管理を固定長バッファで行います。
*   **Fixed Capacity**: 1つの `Animator` が保持できる最大レイヤー数、および1レイヤーあたりの最大同時ブレンドステート数をコンパイル定数で制限します。
*   **In-place Storage**: `std::vector` 等の動的コンテナを避け、コンポーネント構造体内にメンバ変数として直接配列を保持します。

### 2.3. AnimationLayer と AnimationState
`Animator` 内部でアニメーションを階層的に管理します。
*   **AnimationLayer**: 独立した再生タイムラインとウェイト（重み）、および **Bone Mask（ボーンマスク）** を持ちます。
*   **AnimationState**: レイヤー内で再生される1つの `AnimationClip` とその現在の再生時間を保持します。トランジション（ブレンド遷移）中は、1つのレイヤー内に複数の State が存在し、それぞれのウェイトが時間経過で変化します。

### 2.4. BoneMask (ボーンマスク)
レイヤーが影響を与えるボーンの範囲を定義します。ハッシュ化されたジョイントIDまたはインデックスで参照されます。

---

## 3. ブレンディングアルゴリズム (Updateロジック)

`AnimatorUpdateSystem` (新設) の毎フレームの処理フローは以下のようになります。

1. **タイムラインの進行 (インデックスベース)**
   ハッシュ化されたIDを用いてキャッシュされたインデックスを取得し、ボーンのトランスフォームを直接抽出します。

2. **ボーンごとのローカルトランスフォーム計算 (サンプリング & ブレンド)**
   すべてのジョイントに対して以下の計算を行います。スタックメモリ上で固定長配列として展開し、SIMDフレンドリーな処理を心掛けます。

   ```cpp
   // 各レイヤーを順番に評価 (ベースレイヤー -> 上位レイヤー)
   for (int i = 0; i < MAX_ANIMATION_LAYERS; ++i) {
       AnimationLayer& layer = layers[i];
       float layerBoneWeight = layer.GetBoneMaskWeight(jointIndex);
       if (layerBoneWeight == 0.0f) continue;

       // レイヤー内の State (トランジション中の複数Stateも含む) の結果をブレンド
       // ... 補間計算 (Lerp/Slerp) ...
   }
   ```

3. **クォータニオンの正規化 (Normalization)**
   複数のレイヤーやステートをブレンドした結果、計算誤差によりクォータニオンの長さが 1.0 から乖離する可能性があります。**最終的なボーン姿勢を確定させる前に、必ず Quaternion::Normalize を実行**し、モデルの歪みを防止します。

4. **スケルトン空間・ワールド空間行列の計算**
   既存の `SkinMeshUpdateSystem` と同様に、親から子へローカル行列を乗算し、最終的な `matSkeletonSpace` を確定させます。

---

## 4. アニメーションイベントとルートモーション

### 4.1. アニメーションイベント通知
*   **検出**: `AnimationState` が保持する「前フレームの再生時間」と「現在の再生時間」の範囲内にイベントが含まれるかを毎フレームチェックします。
*   **通知**: イベントを検出した場合、グローバルな `FrameEventQueue` にイベントを Push します。
*   **購読**: C# 側のロジックや、サウンド・エフェクトシステムは `FrameEventQueue` を介してこれを受け取ります。

### 4.2. ルートモーション (Root Motion)
*   **定義**: モデルのルートボーン（通常は "Root" や "Hips"）の移動・回転を、アニメーション再生によって抽出します。
*   **抽出**: 前フレームからのルートボーンのトランスフォームの差分（Delta）を計算します。
*   **適用**: 抽出された Delta は、直接 Transform を書き換えるのではなく、`AgentIntentComponent` 等の「移動意図」として出力し、物理システム（PhysicsSystem）が最終的な移動量を決定します。

---

## 5. パフォーマンス設計指針

*   **Zero String Allocation**: ランタイムの `Update` および `Play` / `CrossFade` 呼び出しにおいて、`std::string` の生成や比較を禁止します。
*   **Cache Locality**: `Animator` 内部のデータ構造を連続したメモリ領域に配置し、CPU キャッシュヒット率を向上させます。

---

## 6. C# API インターフェース (案)

開発効率を維持するため、マジックナンバー（ハッシュ値）を直接扱うのではなく、型安全なユーティリティを提供します。

```csharp
public class Animator : Component {
    // 開発用ヘルパー: 文字列から内部的にハッシュを取得して実行
    public void Play(string clipName) => Play(StringHash.Get(clipName));
    
    // パフォーマンス用: ハッシュ値を直接指定
    public void Play(uint clipId);
    public void CrossFade(uint clipId, float transitionDuration);
}

// リソースインポーターによって自動生成される定数クラスの例
public static class AnimationIDs {
    public const uint Idle = 0x1A2B3C4D;
    public const uint Run = 0x5E6F7G8H;
    public const uint Attack = 0x9I0J1K2L;
}
```
