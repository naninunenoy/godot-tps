# tps

Godot 4.6 (C#) で作る TPS (Third-Person Shooter) ゲーム。

## 技術スタック

- Godot 4.6 / C# (.NET 8) / Jolt Physics / Direct3D 12 (Windows) / Metal (macOS)
- プロジェクトディレクトリ: `tps.godot/`

## プロジェクト構成

| プロジェクト | 役割 |
|---|---|
| `tps.godot/` | Godot プロジェクト本体。Node・シーン・物理など Godot 依存コード |
| `tps.csharp/` | 純粋 C# クラスライブラリ。Godot 非依存のゲームロジック |
| `tps.csharp.test/` | `tps.csharp` の単体テスト（xUnit + Shouldly） |

### アーキテクチャルール

- **Godot に依存しないロジックは最大限 `tps.csharp` に実装する**
  - ダメージ計算・AI ステート・弾薬管理・スコアなどのゲームロジックはここ
  - `Node` / `GodotObject` を継承しない純粋な C# クラスとして書く
- `tps.godot` の Node クラスは `tps.csharp` のロジッククラスを薄くラップするだけにとどめる（Humble Object パターン）
- `tps.csharp` は net8.0 を維持すること（`tps.godot` が net8.0 のため）

## 開発ルール

- コード変更後に `dotnet build` が自動実行される。ビルドエラーが報告されたら必ず修正してから完了とすること。
- 機能追加が完了したら自動でコミットする。
- 動作確認は機能追加のたびには行わない。まとめて確認する際は「確認項目」と「期待する挙動」を提示したうえで人間に依頼する。

## MCP サーバー構成

| サーバー名 | 実装 | 役割 |
|---|---|---|
| `godot` | `@coding-solo/godot-mcp` (npx) | エディタ/ヘッドレス操作。シーン作成・UID 管理・プロジェクト起動など |
| `godot-ext` | `tps.mcp/` (dotnet run) | 実行中ゲームとのリアルタイム通信。godot-mcp にない機能を補う拡張 |

`godot-ext` は godot-mcp で対応できない機能を一時的・補完的に実装する位置づけ。将来 godot-mcp 本体に取り込まれたら削除対象。

- `take_screenshot` は明示的に「スクリーンショットを撮って」と指示された場合のみ使用する（自動実行しない）
- `godot-ext` のツールは `run_project` でゲームを起動した後でないと使用できない

## godot-mcp の使用ルール

- `.tscn` / `.tres` などの Godot リソースファイルの情報取得・編集には godot-mcp ツールを使う（直接ファイル編集しない）
- 動作確認が必要な場合は godot-mcp (`run_project` / `get_debug_output` など) を使う
- 動作確認は明示的に「動作確認して」と指示された場合のみ行う（自動では実行しない）
- `run_project` 後はユーザーの指示を待たず即座に `get_debug_output` でログを確認する

## ロギング方針

- 実装時にあらかじめ `_logger.LogDebug(...)` で動作確認に役立つログを入れておく
- godot-mcp 経由で起動したプロセスのみ `get_debug_output` で取得できる（エディタから手動起動したプロセスは取得不可）
