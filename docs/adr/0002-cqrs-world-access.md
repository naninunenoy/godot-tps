# ADR-0002: World へのアクセスを CQRS で分離する

- Status: Accepted
- Date: 2026-05-12

## Context

ゲーム状態（World）への書き込みと読み取りを同一の経路に乗せると、
毎フレームの読み取り（60fps）にコマンドバスのオーバーヘッドが発生し、
かつ責務が曖昧になる。

## Decision

書き込みと読み取りを明確に分離する。

| 操作 | 経路 |
|---|---|
| Godot → World 書き込み | コマンド経由（VitalRouter） |
| Godot → World 読み取り | `World.GetComponent<T>()` 直接（毎フレーム） |
| 外部 → World 書き込み | コマンド経由（MCP・テストも同じ口） |
| 外部 → World 読み取り | `ISceneQuery` 経由 |

コマンドは「状態を変える意図」のみを担い、読み取りはクエリ経由とする。

## イベントバス：VitalRouter の採用

コマンドの配送には **VitalRouter** を使用する。

- **Source Generator による型安全なディスパッチ**：`[Routes]`/`[Route]` 属性だけでハンドラを登録でき、文字列ベースのディスパッチが発生しない
- **Godot Node への適用**：`CharacterBody3D` などの Godot Node クラスにも `[Routes]` を付与できるため、System と Node で同じコマンドバスを共有できる
- **async/await 対応**：`PublishAsync` が `ValueTask` を返すため、ハンドラが非同期処理を持つ場合も自然に扱える

代替として検討した C# `event` や MediatR は、Godot Node との統合が煩雑になるか、DI コンテナへの依存が重くなるため採用しなかった。

## Consequences

- 毎フレームの位置読み取りにコマンドバスのオーバーヘッドがかからない
- コマンドは「何かを変える操作」として意味が明確になる
- MCP・テスト・ゲーム内部がすべて同じ書き込み口を使うため、テスト容易性が高い
