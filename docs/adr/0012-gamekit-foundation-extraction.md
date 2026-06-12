# ADR-0012: 汎用ゲーム基盤 gamekit を切り出す

- Status: Accepted
- Date: 2026-06-11

## Context

ECS ライクなコア（ADR-0001）、CQRS の読み書き口（ADR-0002）、構造化ログストア（ADR-0005）、
リモート操作 HTTP API（ADR-0010）、CLI / MCP の通信層（ADR-0011）は、いずれも TPS という
ジャンルに依存しない概念だが、すべて `tps.*` プロジェクトに実装されており、汎用部分と
TPS 固有実装の境界がコード上に存在しなかった。

これらを「どのゲームでも使える基盤」として分離し、**基盤の上に TPS ゲームが乗っている**
という立て付けに変える。オセロ・横スクロール・格ゲー・ノベルゲー等を実際に作る予定はないが、
作るとしても基盤側の変更が不要である程度の汎用性を目指す。

## Decision

汎用部分を **`gamekit.*`** プロジェクト群として切り出し、`tps.*` はその利用者とする。

| プロジェクト | 内容 |
|---|---|
| `gamekit` | ECS コア（`World` / `Entity` / `EntityId` / `IComponent` / `IIdGenerator`）、シーン抽象（`IScene` / `ISceneQuery`）、構造化ログストア（`ILogStore` / `GameLogEntry` / `InMemoryLogStore`） |
| `gamekit.contract` | 汎用エンドポイント定数（`InputEndpoints`）と DTO、ライフサイクルコマンド（Pause / Resume / Quit） |
| `gamekit.client` | `GameApiClient` 基底（汎用 API + `GetStateAsync<TState>` / `GetStateRawAsync`） |
| `gamekit.godot` | `GameHttpServer`（HTTP プラミング + ルートテーブル）、組み込みルート（`GameApiRoutes`）、ロギングプロバイダ、Vector 変換 |
| `gamekit.mcp` | 汎用 MCP ツール（入力シミュレーション・状態取得） |
| `gamekit.test` | 基盤の単体テスト |

### 主要な設計判断

1. **具象 Component は基盤に置かない。** 基盤は `IComponent` マーカーのみを持ち、
   `TransformComponent` 含む全 Component はゲーム定義とする。共通に見える Component は
   「2 つ目のゲームが必要としたら基盤へ昇格」のルールで運用する。
2. **VitalRouter を基盤の公式コマンドバスとする。** 「コマンド = `VitalRouter.ICommand` 実装」
   という規約ごと基盤が持つ。`ICommandPublisher` のような独自抽象はあえて作らない。
3. **/state のペイロードはゲーム定義。** 基盤はエンドポイントと枠だけ提供する。
   サーバー側は `Func<object?>` の stateProvider 注入（null = 未初期化 = 503）、
   クライアント側は `GetStateAsync<TState>()`（型はゲームが指定）と
   `GetStateRawAsync()`（素の JSON 中継。MCP / CLI の ToonEncoder 変換用）。
4. **Godot の Node はゲーム側、基盤はプレーンクラス。** Godot はシーンにアタッチする
   スクリプトをゲームアセンブリに要求するため、autoload の `InputServer`(Node) は
   `tps.godot` に残し、HTTP 機能は `SceneTree` を注入された `GameHttpServer` に委譲する
   （継承でなくコンポジション）。フレーム待ち・タイマーは `tree.ToSignal(...)` で行う。
5. **CLI はプロジェクト分割しない。** ConsoleAppFramework v5 はソースジェネレータ前提で
   コマンドクラスの別アセンブリ化と相性が悪いため、`tps.cli` 内で汎用 `GameCommands` と
   TPS 固有 `TpsCommands` のクラス分割に留める。MCP はライブラリ（`gamekit.mcp`）+
   exe（`tps.mcp`）合成で分割し、DI は `GameApiClient` → `TpsGameApiClient` の
   forward 登録で同一クライアントを共有する。
6. **エンドポイント定数は `InputEndpoints`（汎用）と `TpsEndpoints`（TPS）に分割。**
   パス重複をコンパイル時に検出できなくなる代償は、`GameHttpServer` のルート登録時の
   実行時検出（起動時に必ず露見する）で補う。

### 不変条件

- `gamekit.*` は `tps.*` を参照しない（逆方向のみ）
- `gamekit.test` は tps の語彙（`GameEvents` 等）に依存しない
- 基盤の Godot 依存は `gamekit.godot` のみが持つ

## Consequences

| メリット | デメリット |
|---|---|
| 汎用 / ゲーム固有の境界がプロジェクト境界として強制される | プロジェクト数が 7 → 13 に増加 |
| 基盤が単体でテスト可能（`gamekit.test` 23 件） | `Godot.NET.Sdk` のバージョンが `tps.godot` / `gamekit.godot` の 2 箇所に重複し、更新時に同期が必要 |
| 新しいゲームは gamekit 参照 + Component / System / ルート登録だけで始められる | 名前空間 `gamekit.contract.Mcp` は実態（HTTP API）と名前がずれた命名負債（リネームは将来一括で） |
| MCP / CLI の汎用部分がゲームの DTO を知らずに動く（raw JSON 中継） | state 出力から null フィールドが消える挙動変更あり（サーバー側 `WhenWritingNull` がそのまま反映） |

切り出し時の経緯・トレードオフの詳細は [memo.md](../../memo.md)、移行計画は [plan.md](../../plan.md) を参照。
