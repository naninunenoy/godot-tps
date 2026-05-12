# 実装計画

ADR-0001〜0005 で決定したアーキテクチャへの移行計画。
現状のゲームが動き続けることを保ちながら段階的に移行する。

## Phase 1: Foundation（tps.csharp）

ECS の土台となる型を整備する。

- [ ] `UnitGenerator` パッケージ追加（tps.csharp / tps.csharp.test）
- [ ] `EntityId` 定義（UnitGenerator、基底型 string、JsonConverter + MessagePackFormatter）
- [ ] `IIdGenerator` インターフェース + `SequentialIdGenerator`（`bullet#1` 形式）
- [ ] `IComponent` マーカーインターフェース
- [ ] Component 型の定義
  - `TransformComponent`（Position, Velocity）
  - `HealthComponent`（Hp, MaxHp）← 既存 `Health` クラスを置き換え
  - `WeaponComponent`（Ammo, MagazineSize, ReloadTimer, FireCooldown）← 既存 `WeaponState` を置き換え
- [ ] `World` クラス（EntityId → Component の辞書、GetComponent / SetComponent / GetEntitiesWithComponent）
- [ ] `Entity` クラス（EntityId + World のファサード。Get / Set / Has / Snapshot）
- [ ] `ISceneQuery` インターフェース（FrameCount, ObjectCount, Snapshot）
- [ ] `IScene` インターフェース（AvailableCommands）

## Phase 2: System 化（tps.csharp）

ロジックを System に移行し、既存クラスを廃止する。

- [ ] `HealthSystem`（ダメージ処理、死亡検知 → `TargetDestroyedCommand` 発行）
- [ ] `WeaponSystem`（射撃、リロード、弾薬管理）← 既存 `WeaponState` のロジックを移行
- [ ] `MovementSystem`（速度計算）← 既存 `PlayerController` のロジックを移行
- [ ] `KillSystem`（`TargetDestroyedCommand` を受け kill カウント更新）← 既存 `KillCounter` を置き換え
- [ ] 各 System の xUnit テスト追加

## Phase 3: DI ルート化（tps.godot）

`Main.cs` を DI ルートにし、`GameRouter.Default` 静的シングルトンを廃止する。

- [ ] `GameRouter.Default` 削除、`Router` を `Main.cs` で生成してコンストラクタ注入
- [ ] `Main.cs` をリファクタ（DI 配線、`ISceneQuery` 実装）
- [ ] `Player.cs` をリファクタ（`Initialize()` で依存注入、self-new 廃止）
- [ ] `Target.cs` をリファクタ（`EntityId` 付与、`World` 経由でコンポーネント管理）
- [ ] `InGameScene` 実装（`IScene.AvailableCommands` を返す）

## Phase 4: 構造化ログストア

テスト・デバッグ基盤を整備する。

- [ ] `GameLogEntry` レコード定義（Level, EventType, Properties, FrameCount）
- [ ] `GameEvents` 定数クラス（イベント種別を文字列リテラルでなく定数で管理）
- [ ] `ILogStore` インターフェース（Entries, Errors, HasEvent）
- [ ] `InMemoryLogStore` 実装
- [ ] カスタム `ILoggerProvider` として `Microsoft.Extensions.Logging` と統合
- [ ] Main.cs で LogStore を DI に組み込む
- [ ] テストに共通アサート追加（`logStore.Errors.IsEmpty`）

## Phase 5: MCP / 状態公開

外部エージェント・テスト向けのインターフェースを整備する。

- [ ] `ToonEncoder` パッケージ追加（tps.mcp）
- [ ] `ISceneQuery.Snapshot` の MCP レスポンスを ToonEncoder でエンコード
- [ ] デバッグログ出力を JSONL 形式に変更
- [ ] `IScene.AvailableCommands` を MCP から取得できるエンドポイント追加

## 移行方針

- 各 Phase は独立してコミット可能な単位にする
- Phase 3 完了まで既存ゲームの動作を維持する
- 各 Phase の完了時に `dotnet build` + `dotnet test` がパスすること
