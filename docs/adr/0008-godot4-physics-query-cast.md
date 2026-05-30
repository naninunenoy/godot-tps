# ADR-0008: Godot 4 物理クエリ結果の C# サブクラス取得に InstanceFromId を使う

- Status: Accepted
- Date: 2026-05-30

## Context

`PhysicsDirectSpaceState3D.IntersectRay` などの物理クエリは結果を `Dictionary` で返す。
衝突オブジェクトを特定の C# サブクラス（例: `Target : StaticBody3D`）として扱うために、
`result["collider"].AsGodotObject() is Target` というパターンを最初に採用した。

しかし実行時に `is Target` が常に `false` を返し、ヒット判定が機能しないことが判明した。

## Decision

物理クエリ結果から C# サブクラスのインスタンスを取得する場合は `GodotObject.InstanceFromId` を使う。

```csharp
// NG: AsGodotObject() はネイティブ基底クラス型を返すため is SubClass が常に false
var collider = result["collider"].AsGodotObject();
if (collider is Target target) { ... }

// OK: InstanceFromId は管理オブジェクトプールから既存の C# インスタンスを引き当てる
var colliderId = result["collider_id"].AsUInt64();
if (GodotObject.InstanceFromId(colliderId) is Target target) { ... }
```

## 理由

Godot 4 の物理エンジンは衝突情報をネイティブポインタとして保持しており、
`AsGodotObject()` はそのポインタから新しいマネージドラッパーを生成する。
この際 Godot はネイティブクラス名（`StaticBody3D`）でラップするため、
C# スクリプトで定義したサブクラスの型情報が失われる。

`GodotObject.InstanceFromId(id)` はインスタンス ID を使ってエンジン内の管理オブジェクトプールを検索し、
すでに生成済みの C# インスタンス（`Target`）を返すため、正しい派生型が得られる。

グループ判定（`node.IsInGroup("targets")`）も動作するが、
文字列リテラルに依存しており型安全性が失われるため採用しない。

## Consequences

- 物理クエリ結果に対して C# サブクラスへの型チェックを行う箇所はすべてこのパターンに統一する
- `IntersectRay`, `IntersectShape`, `IntersectPoint` など物理クエリ全般に同様の制約が存在する
- `collider_id` キーが存在しない場合（空の Dictionary）は手前の `result.Count > 0` ガードで防ぐ
