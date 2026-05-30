# ADR-0004: EntityId を UnitGenerator の強型で定義し採番ロジックを抽象化する

- Status: Accepted
- Date: 2026-05-12

## Context

ゲームオブジェクトの識別子を `string` や `int` の生の型で扱うと、
異なる種別の ID を誤って混在させるバグが起きやすい。
また、UUID は 36 文字と長く、ログや MCP レスポンスで冗長になる。

## Decision

- `UnitGenerator` の `[UnitOf(typeof(string))]` で `EntityId` を強型として定義する
- 採番ロジックは `IIdGenerator` インターフェースで抽象化する
- 当面は `SequentialIdGenerator`（`bullet#1`、`player#1` 等）を使用する
- 将来 NanoID 等に切り替える場合は `IIdGenerator` の実装差し替えのみで済む

## Consequences

- `string` との暗黙的な混用がコンパイルエラーになる
- ID 形式が変わっても `EntityId` 型を使う側のコードは影響を受けない
- `JsonConverter` / `MessagePackFormatter` を UnitGenerator で自動生成するため、シリアライズの一貫性が保たれる
