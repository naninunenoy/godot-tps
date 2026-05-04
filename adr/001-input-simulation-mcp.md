# ADR 001: 入力模擬 MCP を C# 別プロセスで実装する

## ステータス

採用

## 背景

TPS ゲームの動作確認を Claude から行えるよう、キー入力・マウスクリック・ゲームパッド入力を模擬する MCP ツールが必要になった。

既存の godot-mcp（Coding-Solo/godot-mcp）には入力模擬機能がない。追加を検討するにあたり、godot-mcp の実装を調査した。

## 調査結果

godot-mcp の Godot との通信方式は以下のとおり：

```
MCP サーバー（Node.js/TS）
  → execFile("godot --headless --script godot_operations.gd <操作名> <JSON>")
  → stdout をキャプチャして結果返却
```

ヘッドレス Godot プロセスを都度 spawn する方式であり、**実行中ゲームとのリアルタイム通信は行っていない**。

入力模擬は実行中ゲームに対してリアルタイムで `Input.ParseInputEvent()` を呼ぶ必要があるため、godot-mcp の現アーキテクチャとは根本的に相容れない。

## 決定

入力模擬専用の MCP サーバーを **C# 別プロセス**として新規実装する。

構成：

```
Claude ↔ 入力模擬 MCP サーバー（C# コンソールアプリ）
                 ↓ HTTP
         Godot AutoLoad（実行中ゲーム内の HTTP 受付サーバー）
                 ↓
         Input.ParseInputEvent()
```

- MCP サーバー: `ModelContextProtocol` NuGet パッケージを使用した C# コンソールアプリ
- Godot 側: AutoLoad として HTTP サーバーを立て、実行中に入力イベントを受け付ける
- Claude の MCP 設定に godot-mcp とは別のサーバーとして登録する

## 理由

- **godot-mcp への追加が困難**: アーキテクチャが異なり、既存コードへの影響が大きい
- **C# を採用**: このプロジェクトはすでに C#（`tps.csharp`, `tps.godot`）を使っており、言語を統一できる
- **別プロセスで問題ない**: Claude は複数の MCP サーバーを同時に使用できる

## トレードオフ

| メリット | デメリット |
|---|---|
| godot-mcp に手を加えない | Godot 側に HTTP サーバーの AutoLoad 実装が必要 |
| 慣れた C# で書ける | MCP サーバーの起動設定を Claude に別途登録する手間 |
| 責務が明確に分離される | ポート競合などの運用上の考慮が必要 |
