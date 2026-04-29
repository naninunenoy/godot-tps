# tps

Godot 4.6 (C#) で作る TPS (Third-Person Shooter) ゲーム。

## 技術スタック

- Godot 4.6 / C# (.NET 8) / Jolt Physics / Direct3D 12
- プロジェクトディレクトリ: `tps.godot/`

## 開発ルール

- コード変更後に `dotnet build` が自動実行される。ビルドエラーが報告されたら必ず修正してから完了とすること。
- 機能追加が完了したら自動でコミットする。
- 動作確認は機能追加のたびには行わない。まとめて確認する際は「確認項目」と「期待する挙動」を提示したうえで人間に依頼する。

## godot-mcp の使用ルール

- `.tscn` / `.tres` などの Godot リソースファイルの情報取得・編集には godot-mcp ツールを使う（直接ファイル編集しない）
- 動作確認が必要な場合は godot-mcp (`run_project` / `get_debug_output` など) を使う
- 動作確認は明示的に「動作確認して」と指示された場合のみ行う（自動では実行しない）
- `run_project` 後はユーザーの指示を待たず即座に `get_debug_output` でログを確認する

## ロギング方針

- 実装時にあらかじめ `_logger.LogDebug(...)` で動作確認に役立つログを入れておく
- godot-mcp 経由で起動したプロセスのみ `get_debug_output` で取得できる（エディタから手動起動したプロセスは取得不可）
