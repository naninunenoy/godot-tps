# ADR-0001: tps.csharp に ECS ライクなアーキテクチャを採用する

- Status: Accepted
- Date: 2026-05-12

## Context

Godot の Node ベースの実装はゲームロジックと描画・物理が混在しやすく、単体テストが困難だった。
ロジックを Godot 非依存の純粋 C# に寄せ、テスト可能にする必要があった。

## Decision

`tps.csharp` に ECS ライクな構造を導入する。

- **Component**：純粋データ（`record` で定義。`HealthComponent`、`WeaponComponent`、`TransformComponent` 等）
- **System**：Component を処理するロジック。Godot に依存しない
- **World**：`EntityId → Component` のデータ置き場
- **EntityId**：`UnitGenerator` で生成した強型（基底型 `string`）

`tps.godot` の Node は World を読み書きする薄いシェルとし、ロジックを持たない（Humble Object パターン）。

## Consequences

- `tps.csharp` の System・Component は xUnit で単体テスト可能になる
- Godot Node の責務が「視覚・物理の反映」に限定され、テストしやすくなる
- 純粋 ECS ではなく Godot Node が実体を持つハイブリッドのため、Node ↔ World の同期コードが必要になる
