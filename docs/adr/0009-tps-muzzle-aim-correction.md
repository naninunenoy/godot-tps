# ADR-0009: TPS マズル照準補正にカメラレイキャストを使う

- Status: Accepted
- Date: 2026-05-30

## Context

TPS（三人称視点）では、カメラとマズルの位置が異なるため、カメラの向きをそのままマズルに適用すると弾道が期待と一致しない。

本プロジェクトの構成：

| ノード | Y 座標（プレイヤー原点基準） |
|---|---|
| `CameraPivot` | +2.5 m |
| マズル（`Player.GlobalPosition + Up * 1.3f`） | +1.3 m |

`FaceToward` はカメラピボット位置を基点に仰角を計算するため、低い位置にある目標を見るときに大きな俯角がつく。
マズルはピボットより 1.2 m 低いため、同じ角度で発射した弾が目標に届く前に Y=0 を下回り、床下を通過してヒットしない。

## Decision

弾の発射方向を「カメラ起点レイキャスト → 着弾点 → マズルから着弾点へ向ける」方式で決定する。

```csharp
// 1. カメラから着弾点を求める
var camOrigin = _camera.GlobalPosition;
var camForward = -_camera.GlobalBasis.Z;
var rayQuery = PhysicsRayQueryParameters3D.Create(camOrigin, camOrigin + camForward * 200f);
rayQuery.Exclude = [GetRid()];
var rayResult = GetWorld3D().DirectSpaceState.IntersectRay(rayQuery);
var aimPoint = rayResult.Count > 0 ? rayResult["position"].AsVector3() : camOrigin + camForward * 200f;

// 2. マズルから着弾点へ向ける
var muzzlePos = GlobalPosition + Vector3.Up * 1.3f + camForward * 0.5f;
bullet.GlobalPosition = muzzlePos;
bullet.LookAt(muzzlePos + (aimPoint - muzzlePos).Normalized());
```

検討した他の案：

| 案 | 却下理由 |
|---|---|
| 弾をカメラ位置から発射 | マズルフラッシュが画面外になり演出が壊れる |
| マズル高さをカメラピボットに合わせる | キャラクターの頭上から弾が出る見た目になる |
| カメラ方向をそのまま使う（補正なし） | 目標より低い位置に着弾・ミスが発生する（今回の不具合） |

## 理由

カメラのクロスヘアが指す点（着弾点）に向けてマズルを向けることで、
プレイヤーが「画面に映っているものを撃てる」という直感に一致する。

FPS では弾をカメラ起点で発射する方法が多いが、TPS では腰位置に弾の発射口があるため、
着弾点経由での方向補正がより自然な解になる。

## Consequences

- 弾の生成時にカメラレイキャストが 1 本増える（ADS フィードバック用と合わせて最大 2 本）。現状の Target 数・弾数では問題なし
- 着弾点が遮蔽物に当たった場合も正しく補正される（壁越しに撃てない）
- 将来 `BulletSpawnRequested` にカメラ origin / aim point を含める設計変更を行う場合、この補正ロジックをシステム側に移してもよい
