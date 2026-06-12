# ADR-0014: リモート操作 API の JSON・エラー応答・ロギング方針

- Status: Accepted
- Date: 2026-06-12

## Context

gamekit 切り出し（ADR-0012）後のコードレビューで、旧 `InputServer`（ゲーム 1 個のデバッグ機能）
から移植したコードに残っていた欠陥が、基盤（gamekit.godot）に昇格していた：

- ボディ読み取りループに切断検出がなく、クライアントが Content-Length 分を送らず切断すると
  毎フレーム回る無限ループが残留する
- `async void` 境界から例外が漏れるとプロセスごと落ちる
- 不正なリクエスト行・Content-Length・JSON ボディに 500 を返す（本来は 400）
- リクエスト JSON の解釈が大文字小文字依存で、PascalCase クライアントとの暗黙の契約だった
- ログが `GD.Print` 直書きで、構造化ログ（debug.jsonl）に残らない

**コードの移動 PR であっても、移動先が基盤なら品質基準は基盤側で再評価する**という教訓を踏まえ、
リモート操作 API の方針を定めた。

## Decision

### JSON

- **レスポンスは PascalCase + null 省略。** `HttpResult.Json` の `JsonSerializerOptions` が
  一元管理する（変えるならここ 1 箇所）。Web 既定（camelCase）にしない理由は、既存クライアントと
  ToonEncoder 出力（MCP / CLI の見た目）を壊さないため
- **リクエスト解釈は大文字小文字を区別しない。** `GameHttpServer.MapPostJson<TReq>` が
  `PropertyNameCaseInsensitive = true` で一元管理し、クライアント実装の命名差に寛容にする。
  POST ハンドラは生ボディでなく `MapPostJson` 経由で型付きリクエストを受けること

### エラー応答

| 状況 | ステータス |
|---|---|
| リクエスト構文不正（リクエスト行・Content-Length・JSON ボディ） | 400（`HttpBadRequestException` / `MapPostJson`） |
| ゲーム未初期化（シーン・Router が未注入） | 503 |
| ハンドラ内の例外（サーバー側のバグ） | 500 |

### 堅牢性

- `async void` 境界（接続ハンドラ・タイマー）は全経路を try/catch で包む。
  例外が漏れるとプロセスごと落ちるため
- 切断検出（`StreamPeerTcp.GetStatus()`）はヘッダ・ボディ両方の読み取りループに入れる

### ロギング

- HTTP サーバーのログは `GD.Print` 直書きでなく ILogger（既定は `AppLogger`）経由とし、
  構造化ログ（debug.jsonl）にも残す
- **クライアント起因（不正リクエスト・切断）は Warning、サーバー起因（ハンドラ例外）は Error。**
  テスト規約「エラーレベル以上のログが出ていないことをアサートする」を、外部からの雑な入力で
  壊さないための区別

## Consequences

- 不正入力・切断・例外に対するサーバーの応答が予測可能になり、デバッグサーバーの hang が消えた
- JSON とエラー応答の方針が各 1 箇所（`HttpResult.Json` / `MapPostJson` / `HttpBadRequestException`）に
  集約され、将来のゲームも同じ振る舞いを継承する
- 400/503/500 の区別はクライアント側のリトライ・診断の手がかりになる（現状のクライアントは
  `EnsureSuccessStatusCode` で一律例外だが、ステータスで原因を読み分けられる）
