# TPS 要素整理

## 要素と属性

### Player（1人）

| 属性 | 型 | 備考 |
|---|---|---|
| カメラ向き（視点） | 方向 | まず視点が動く |
| プレイヤー向き（体） | 方向 | 視点に追従 |
| 位置 | Vector3 | |
| 移動速度 | float | |

| 行動 | 備考 |
|---|---|
| 移動する | 入力方向 × カメラ向きで移動方向を決定 |
| 向きを変える | カメラ（視点）→ 体の順で向きが変わる |
| 銃を構える | ADS（エイムダウンサイト）状態の切り替え |
| 引き金を引く | 銃に発射を依頼する |

---

### 銃（Player 1人につき 1つ）

| 属性 | 型 | 備考 |
|---|---|---|
| 弾数（現在） | int | |
| マガジンサイズ | int | |
| 発射間隔 | float | 連射速度 |
| リロード時間 | float | |
| 速度（玉に渡す） | float | 玉の初速 |
| 威力（玉に渡す） | int | 玉のダメージ量 |

| 行動 | 備考 |
|---|---|
| CanFire チェック | 弾数 > 0 かつ 発射間隔クールダウン済み |
| 玉を生成する | 発射方向・速度・威力を玉に渡す |
| 弾数を消費する | 発射ごとに -1 |
| リロードする | 一定時間後に弾数をマガジンサイズに戻す |

---

### 玉（複数同時存在）

| 属性 | 型 | 備考 |
|---|---|---|
| 速度 | float | 銃から受け取る |
| 威力 | int | 銃から受け取る |
| 射程（最大飛距離） | float | この距離に達したら消える |
| 飛行方向 | Vector3 | 生成時に確定、以後変化しない |

| 行動 | 備考 |
|---|---|
| 直線飛行する | 毎フレーム 飛行方向 × 速度 で移動 |
| 的に当たる | 当たり判定で検知、威力を渡して消える |
| 射程切れで消える | 最大飛距離に達したら消える |

---

### 的（複数、固定）

| 属性 | 型 | 備考 |
|---|---|---|
| HP（現在） | int | |
| 最大 HP | int | |

| 行動 | 備考 |
|---|---|
| ダメージを受ける | 玉の威力分 HP を減らす |
| 破壊される | HP = 0 で消えたまま（リスポーンなし） |

---

## 要素間の関係

```
Player ──持つ──▶ 銃（1つ）
銃     ──生成──▶ 玉（複数）
玉     ──命中──▶ 的
```

| 関係 | 渡すもの |
|---|---|
| Player → 銃 | 「発射せよ」の指示、発射方向 |
| 銃 → 玉 | 速度、威力、飛行方向 |
| 玉 → 的 | 威力（ダメージ量） |

---

## イベントフロー

### メインフロー：引き金を引いてから的が壊れるまで

```
[1] Player: 引き金を引く（入力）
      │
      ▼
[2] Player: ADS 中か確認
      ├─ NO（構えていない）──▶ 何も起きない（終了）
      └─ YES
           │
           ▼
[3] 銃: CanFire チェック
      ├─ NO（弾切れ or クールダウン中）──▶ 何も起きない（終了）
      └─ YES
           │
           ▼
[4] 銃: 発射方向を決定（カメラ正面）、弾数 -1、クールダウン開始
      │
      ▼
[5] 玉: 生成（速度・威力・飛行方向を受け取る）
      │
      ▼
[6] 玉: 毎フレーム直線飛行（重力なし）
      ├─ 最大飛距離に達した ──▶ 玉が消える（終了）
      └─ 的に当たった（玉が検知）
           │
           ▼
[7] 的: ダメージを受ける（HP -= 玉の威力）、玉が消える
      ├─ HP > 0 ──▶ 的は生きたまま（終了）
      └─ HP <= 0
           │
           ▼
[8] 的: 破壊される（消えたまま）
```

---

### サブフロー：リロード

```
[A] 銃: 弾数 = 0 を検知（発射後 or Player がリロード入力）
      │
      ▼
[B] 銃: リロード開始（タイマースタート）
      │
      ▼
[C] タイマー満了
      │
      ▼
[D] 銃: 弾数をマガジンサイズに戻す
```

---

### フローで確定した責務

| ステップ | 誰が持つ責務 |
|---|---|
| 入力を受け取る | Player |
| 発射可否の判断 | 銃 |
| 発射方向の決定 | Player（カメラ正面を銃に渡す。ADS 有無で変化しない） |
| 玉の生成 | 銃 |
| 飛行・消滅 | 玉 |
| 当たり判定の検知 | 玉（自分が誰かに当たったかを知る） |
| ダメージ計算 | 的（玉から威力を受け取り自分のHPを減らす） |
| 破壊状態への遷移 | 的 |

---

## Component 設計

### Entity の種類

| Entity | Component の組み合わせ |
|---|---|
| Player | TransformComponent, CameraComponent, MovementComponent, AdsComponent, WeaponComponent |
| Target | TransformComponent, HealthComponent |
| Bullet | Entity にしない（Godot Node として完結） |

---

### TransformComponent（Player が持つ）

```
TransformComponent(
    Position : Vector3,   // 位置
    Forward  : Vector3    // プレイヤーの体の向き
)
```

---

### CameraComponent（Player が持つ）

```
CameraComponent(
    Forward : Vector3   // カメラ（視点）の向き。発射方向の基準
)
```

体の向きとカメラ向きは別 Component。カメラが先に動き、体が追従する。

---

### MovementComponent（Player が持つ）

```
MovementComponent(
    Speed : float   // 移動速度
)
```

---

### AdsComponent（Player が持つ）

```
AdsComponent(
    IsAiming : bool   // 銃を構えているか。false のとき発射不可
)
```

---

### WeaponComponent（Player が持つ）

```
WeaponComponent(
    CurrentAmmo    : int,   // 現在の弾数
    MagazineSize   : int,   // マガジンサイズ（リロード後に戻る値）
    FireInterval   : float, // 発射間隔（連射速度）
    FireCooldown   : float, // 発射後の残りクールダウン時間（0 で発射可）
    ReloadDuration : float, // リロードにかかる時間
    ReloadTimer    : float, // リロード残り時間（0 で完了）
    BulletSpeed    : float, // 玉に渡す初速
    BulletDamage   : int    // 玉に渡す威力
)

CanFire  = CurrentAmmo > 0 && FireCooldown <= 0 && ReloadTimer <= 0
IsReloading = ReloadTimer > 0
NeedsReload = CurrentAmmo == 0 && !IsReloading
```

---

### HealthComponent（Target が持つ）

```
HealthComponent(
    Hp    : int,   // 現在の HP
    MaxHp : int    // 最大 HP
)

IsAlive = Hp > 0
```

---

### Bullet が持つ値（Component ではなく生成時の引数）

Bullet は Entity でないため Component を持たない。
Godot Node 生成時に以下を引数で渡す。

```
BulletSpeed    : float   // WeaponComponent.BulletSpeed から
BulletDamage   : int     // WeaponComponent.BulletDamage から
Direction      : Vector3 // CameraComponent.Forward から
MaxDistance    : float   // 定数（銃や玉の種類で変えるなら WeaponComponent に追加）
```

---

## System 設計

System は Component を読み書きするロジック。Godot 非依存。

---

### MovementSystem

**担当**：Player の位置・向きの更新

| | 内容 |
|---|---|
| 読む Component | TransformComponent, CameraComponent, MovementComponent |
| 書く Component | TransformComponent |

**メソッド**

```
Move(entityId, inputDir: Vector2, delta: float)
  → 入力方向 × CameraComponent.Forward から移動ベクトルを計算
  → TransformComponent.Position, Forward を更新
```

---

### WeaponSystem

**担当**：ADS 状態・銃の状態管理・発射判定

| | 内容 |
|---|---|
| 読む Component | AdsComponent, WeaponComponent, CameraComponent |
| 書く Component | AdsComponent, WeaponComponent |
| 発行するイベント | BulletSpawnRequested（玉の生成を Godot 側に依頼） |

**メソッド**

```
Update(entityId, delta: float)
  → WeaponComponent.FireCooldown, ReloadTimer を減算
  → ReloadTimer が 0 になったら CurrentAmmo = MagazineSize

StartAim(entityId)
  → AdsComponent.IsAiming = true

StopAim(entityId)
  → AdsComponent.IsAiming = false

TryFire(entityId)
  → AdsComponent.IsAiming でなければ何もしない
  → WeaponComponent.CanFire でなければ何もしない
  → CurrentAmmo -= 1, FireCooldown = FireInterval
  → BulletSpawnRequested(Direction=CameraForward, Speed=BulletSpeed, Damage=BulletDamage) を発行

TryReload(entityId)
  → WeaponComponent.NeedsReload でなければ何もしない
  → ReloadTimer = ReloadDuration, CurrentAmmo = 0
```

---

### HealthSystem

**担当**：Target の HP 管理と破壊判定

| | 内容 |
|---|---|
| 読む Component | HealthComponent |
| 書く Component | HealthComponent |
| 発行するイベント | TargetDestroyed |

**メソッド**

```
TakeDamage(entityId, damage: int)
  → HealthComponent.Hp -= damage（0 未満にはしない）
  → Hp == 0 なら TargetDestroyed を発行
```

---

### System とイベントフローの対応

```
[1] 引き金入力          → WeaponSystem.TryFire()
[2] ADS チェック        → WeaponSystem 内部で AdsComponent を確認
[3] CanFire チェック    → WeaponSystem 内部で WeaponComponent を確認
[4] 弾数消費・CD開始    → WeaponSystem が WeaponComponent を更新
[5] 玉の生成依頼        → WeaponSystem が BulletSpawnRequested を発行
[6] 玉の飛行・消滅      → Godot Node（System 対象外）
[7] ダメージ適用        → HealthSystem.TakeDamage()
[8] 破壊               → HealthSystem が TargetDestroyed を発行
```

---

## イベントバス（VitalRouter）

System は Godot Node を直接参照できないため、System → Node の通知は Router 経由になる。

### イベント一覧

| 発行者 | イベント | 受信者 | 用途 |
|---|---|---|---|
| WeaponSystem | BulletSpawnRequested | Player Node | 玉の生成を依頼 |
| WeaponSystem | ShotFiredCommand | HUD | 残弾表示の更新 |
| Bullet Node | TargetHitCommand | Target Node | 命中通知（TargetName + Damage） |
| HealthSystem | TargetDestroyedCommand | Target Node | 破壊通知（消える） |
| Player Node | AimUpdatedCommand | HUD | 照準が的に当たっているかを通知（Raycast 結果） |

### 直接呼び出し（Router を使わない）

| 呼び出し元 | 呼び出し先 | 理由 |
|---|---|---|
| Target Node | HealthSystem.TakeDamage() | Initialize 時に注入済み |

### 通信フロー

```
WeaponSystem
  ├─ BulletSpawnRequested ──▶ Player Node ──▶ Bullet を生成
  └─ ShotFiredCommand ──────▶ HUD

Bullet Node
  └─ TargetHitCommand ──────▶ Target Node
                                  └─ healthSystem.TakeDamage()  ← 直接呼び出し
                                       └─ TargetDestroyedCommand ──▶ Target Node（消える）

Player Node（ADS 中、毎フレーム）
  └─ Raycast 実行 → AimUpdatedCommand { IsOnTarget } ──▶ HUD（照準インジケーター表示）
```

---

## World 状態（MCP 公開用）

### Player

| 状態 | Component | 型 |
|---|---|---|
| ID | Entity.Id | EntityId |
| 位置 | TransformComponent.Position | Vector3 |
| 体の向き | TransformComponent.Forward | Vector3 |
| カメラの向き | CameraComponent.Forward | Vector3 |
| ADS 中か | AdsComponent.IsAiming | bool |
| 現在の弾数 | WeaponComponent.CurrentAmmo | int |
| マガジンサイズ | WeaponComponent.MagazineSize | int |
| リロード中か | WeaponComponent.IsReloading | bool |
| 発射可能か | WeaponComponent.CanFire | bool |

### Target（的ごとに）

| 状態 | Component | 型 |
|---|---|---|
| ID | Entity.Id | EntityId |
| 位置 | TransformComponent.Position | Vector3 |
| 現在 HP | HealthComponent.Hp | int |
| 最大 HP | HealthComponent.MaxHp | int |

- Target Entity は `TransformComponent` を持つ。位置は初期化時に Godot Node の位置をセットし、以後変化しない
- `IsAlive` は `Hp > 0` で算出できるため出力しない

---

## MCP 外部操作

プレイヤー操作と同じ System メソッドを呼ぶ。経路だけ違って出口は同じ。

### 操作一覧

| 操作 | 内部で呼ぶもの |
|---|---|
| 移動 | `MovementSystem.Move(inputDir)` |
| 向き変え | `CameraOrientCommand` → CameraComponent.Forward を更新 |
| 構える（ADS） | `WeaponSystem.StartAim()` |
| 玉を発射 | `WeaponSystem.TryFire()`（ADS 中でないと不可） |
| リロード | `WeaponSystem.TryReload()` |
| 構え解除 | `WeaponSystem.StopAim()` |
| ゲーム終了 | `QuitRequestedCommand` を発行 |
| World 状態取得 | `ISceneQuery` 経由で Player・Target の状態を返す |

### 発射の手順

MCP から発射する場合は以下の順で呼ぶ。

```
1. CameraOrientCommand（向きをセット）
2. WeaponSystem.StartAim()
3. WeaponSystem.TryFire()
4. WeaponSystem.StopAim()（必要なら）
```

---

## 現状との差分（修正点）

### Component

| Component | 状態 | 内容 |
|---|---|---|
| TransformComponent | 要修正 | 既存は `(Position, Velocity)`。plan では `(Position, Forward)` に変える必要あり。Velocity は MovementSystem 内部で扱う |
| CameraComponent | 未実装 | 新規追加が必要 |
| MovementComponent | 未実装 | Speed が `PlayerSettings` に入っている。Component 化が必要 |
| AdsComponent | 未実装 | ADS の概念そのものがない。新規追加が必要 |
| WeaponComponent | 要修正 | `BulletSpeed`・`BulletDamage` が未実装。フィールド名 `Ammo` → `CurrentAmmo` |
| HealthComponent | 一致 | 変更不要 |

### System

| System | 状態 | 内容 |
|---|---|---|
| WeaponSystem.TryFire | 要修正 | ADS チェックなし。`BulletSpawnRequested` を発行していない |
| WeaponSystem.StartAim / StopAim | 未実装 | 新規追加が必要 |
| MovementSystem | 要修正 | `CameraComponent` を World から読む設計に変更。現状は呼び出し元（Player Node）がカメラ情報を渡している |

### Godot Node

| Node | 状態 | 内容 |
|---|---|---|
| Player.cs の Raycast | 要修正 | 当たり判定でなく HUD フィードバック専用に変更。ADS 中に毎フレーム実行し `AimUpdatedCommand` を発行する |
| Player.cs の Bullet 生成 | 要修正 | 現状は Player が直接生成。plan では `BulletSpawnRequested` を受けて生成する |
| Player.cs の WeaponDamage | 要修正 | `[Export]` で Player が持っている。`WeaponComponent.BulletDamage` に移す |
| Bullet.cs | 要修正 | `Damage`（威力）フィールドがない。衝突時に `TargetHitCommand` を発行する処理がない。当たり判定は Bullet 自身が行う（Raycast は HUD 用途のみ） |
| Target.cs の TransformComponent | 要修正 | 初期化時に Position が `Vector3.Zero` のまま。Godot Node の実座標をセットする必要あり |
| Target.cs のリスポーン | 劣後 | `_respawnTimer` が実装済みだが plan では劣後。今は削除しない |

---

## 確定事項

- 発射方向：常にカメラ正面。ADS 有無で変化しない
- 当たり判定の検知：玉が自分の命中を検知し、的にダメージを通知する
- 的の破壊後：消えたまま。リスポーンは劣後機能
- ADS：構えていないと発射できない。構え中に速度・精度は変化しない
- 玉：重力なし。まっすぐ飛ぶ
