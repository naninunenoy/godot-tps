# ADR-0013: リモート操作 API の書き込みはコマンドバスを経由する

- Status: Accepted
- Date: 2026-06-12

## Context

gamekit 切り出し（ADR-0012）後のコードレビューで、規約と実装の矛盾が見つかった。

- ADR-0012 / CLAUDE.md は「コマンドは `VitalRouter.ICommand` 実装」「外部からの World 書き込みは
  コマンド経由」と規定しているが、カメラ操作（camera_pitch / look_at / set_aiming）だけは
  `InputServer` が `Player` のメソッドを直接呼んでおり、コマンドバスを迂回していた
- `InGameScene.AvailableCommands` は ICommand でない素のリクエスト DTO
  （`SetCameraPitchRequest` 等）を「コマンド」として公開していた
- 一方で `Player` には `SetCameraPitchRequest` / `LookAtPositionRequest` の `[Route]` ハンドラが
  存在するのに誰も publish しておらず、デッドコードになっていた。
  元々コマンドバスを通す設計意図があったとみられる

ドキュメント側を弱めて現状を追認するか、コードを設計に合わせるかの選択になった。

## Decision

コードを設計に合わせ、**リモート操作による書き込みはすべてコマンドバス経由にする**。

1. **リクエスト DTO 兼コマンド。** HTTP リクエスト型（`SetCameraPitchRequest` /
   `LookAtPositionRequest` / `SetAimingRequest`）が `VitalRouter.ICommand` を実装する。
   HTTP ボディの形とコマンドの形は実質同じであり、型を二重定義して詰め替えるより薄い。
2. **InputServer は Router へ publish するだけ。** Player への参照を持たない。
   ハンドラは「デシリアライズ済みコマンドを publish して成功レスポンスを返す」の一形に揃う。
3. **Player のカメラ操作メソッドは private。** `[Route]` ハンドラ（コマンドバス）が唯一の入口で
   あることをアクセス修飾子で強制する。
4. **`AvailableCommands` は publish 可能な ICommand 型のみを列挙する。** 漏れていた
   `SetAimingRequest` を追加し、/commands の応答は実態（4 件）と一致した。

### 順序保証について

ハンドラは `_ = router.PublishAsync(cmd)` の fire-and-forget だが、VitalRouter の同期
`[Route]` ハンドラは publish 時点でインライン実行されるため、HTTP レスポンスを返す前に
状態が反映される。非同期ハンドラを使う場合はこの前提が崩れることに注意。

## Consequences

| メリット | デメリット |
|---|---|
| CQRS 規約「外部からの書き込みはコマンド経由」が全経路で真になった | `tps.contract.Mcp` のリクエスト型が VitalRouter に依存する（HTTP コントラクトとコマンドの結合。`Mcp` 名前空間の命名負債と合わせて将来整理） |
| MCP・CLI・ゲーム内部が文字どおり「同じ口」を使う | publish の fire-and-forget は同期ハンドラ前提（上記の順序保証参照） |
| デッドコードだった Player の [Route] ハンドラが本来の役割を取り戻した | — |

### 残課題

`/commands` には `GamePauseRequestedCommand` が列挙されるが、HTTP から任意の ICommand を
投入する汎用エンドポイントはまだ無い（カメラ系は専用ルートで受けている）。
汎用の `POST /command` を設けるかは、必要になった時点で判断する。
