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
   サーバー側はゲームが注入する `Func<ISceneQuery?>`（null = 未初期化 = 503）と
   `Func<ISceneQuery, object>` の stateBuilder（tps では tps.csharp の
   `GameStateResponseBuilder`。Godot 非依存の純粋マッピングとして単体テストする）、
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

## 実装メモ

切り出し実施中に下した、コードだけでは読み取れない判断。

- **グローバル using で移行コストを抑えた。** 基盤型（`World` / `Entity` 等）の名前空間変更に伴う
  全ファイルの using 修正を避けるため、csproj の `<Using Include="gamekit" />`
  （tps.godot は `GlobalUsings.cs`）を使う。ファイル単位では gamekit への依存が見えなくなる
  トレードオフは、基盤型をプロジェクト全域で使う前提で許容した。
- **ProjectReference はソース上で直接 using しているプロジェクトに明示的に張る**（推移的参照に
  頼らない）。直接 using しなくなったら外す。依存関係を csproj から読めるようにするため。
- **クライアントの拡張は継承**（`TpsGameApiClient : GameApiClient`）。同一サーバーへの
  エンドポイント追加なので、基底の `Http`（BaseAddress 設定済み）と `PostJsonAsync` を
  protected で公開する。protected メンバーは事実上の公開 API になるため変更時は派生側に注意。
- **`GameCommand` 名前空間は基盤・ゲームの両方に存在する**（`gamekit.contract.GameCommand` =
  ライフサイクル、`tps.contract.GameCommand` = TPS コマンド）。既存規約との一貫性を優先した。
- **`SequentialIdGenerator` のカウンタは prefix 間で共有**（`Next("a")`→`a#1`、`Next("b")`→`b#2`）。
  この仕様は gamekit.test で固定化しており、変えるならテストごと変える。
- **EntityId の wire format は素の文字列**（UnitGenerator の JsonConverter）。名前空間移動の
  影響を受けないことを gamekit.test で担保している。

## 移行履歴

| Phase | コミット | 内容 |
|---|---|---|
| 計画 | `fc5ed50` | 移行計画の策定 |
| 1 | `46275fc` | ECS コア・ログストアを gamekit へ。gamekit.test 新設 |
| 2 | `d3c7b34` | 汎用エンドポイント・DTO・ライフサイクルコマンドを gamekit.contract へ |
| 3 | `4b0fb12` | 汎用 HTTP クライアントを gamekit.client へ。TpsGameApiClient 新設 |
| 4 | `e4257cb` | GameHttpServer / GameApiRoutes / ロギングを gamekit.godot へ |
| 5 | `7924d0b` | 汎用 MCP ツールを gamekit.mcp へ。CLI をコマンドクラス分割 |
| 6 | `91e17bb` | ドキュメント更新・本 ADR 作成 |

切り出し後のコードレビューで決めた API 方針は ADR-0013 / ADR-0014 を参照。

## 今後の課題

- 名前空間 `gamekit.contract.Mcp` → `Api` 等への一括リネーム（命名負債）
- 旧実装（`Health` / `KillCounter` / `WeaponState` と R3 依存）の整理（`PlayerController` は使用中）
- HTTP ポート (9876) の設定可能化
- `Godot.NET.Sdk` バージョンの一元管理（Directory.Build.props 等）
- HTTP から任意の ICommand を投入する汎用エンドポイント（POST /command 等。ADR-0013 参照）
