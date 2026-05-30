# ADR-0010: 入力模擬 MCP を C# 別プロセスで実装する

- Status: Accepted
- Date: 2026-05-30

## Context

TPS ゲームの動作確認を Claude から行えるよう、キー入力・マウスクリック・ゲームパッド入力を模擬する MCP ツールが必要になった。

既存の godot-mcp（Coding-Solo/godot-mcp）には入力模擬機能がない。追加を検討するにあたり、godot-mcp の実装を調査した。

## 調査結果（Context 補足）

godot-mcp の Godot との通信方式は以下のとおり：

```
MCP サーバー（Node.js/TS）
  → execFile("godot --headless --script godot_operations.gd <操作名> <JSON>")
  → stdout をキャプチャして結果返却
```

ヘッドレス Godot プロセスを都度 spawn する方式であり、**実行中ゲームとのリアルタイム通信は行っていない**。

入力模擬は実行中ゲームに対してリアルタイムで `Input.ParseInputEvent()` を呼ぶ必要があるため、godot-mcp の現アーキテクチャとは根本的に相容れない。

## Decision

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

## Consequences

| メリット | デメリット |
|---|---|
| godot-mcp に手を加えない | Godot 側に HTTP サーバーの AutoLoad 実装が必要 |
| 慣れた C# で書ける | MCP サーバーの起動設定を Claude に別途登録する手間 |
| 責務が明確に分離される | ポート競合などの運用上の考慮が必要 |

## 2サーバー構成の全体像

現在、MCP サーバーは以下の2本立てで運用している。

| サーバー名 | 実装 | 役割 |
|---|---|---|
| `godot` | `@coding-solo/godot-mcp`（npx） | エディタ・ヘッドレス操作。シーン作成・UID 管理・プロジェクト起動など |
| `godot-ext` | `tps.mcp/`（dotnet run） | 実行中ゲームとのリアルタイム通信。入力模擬・状態取得など |

`godot-ext` は godot-mcp で対応できない機能を**補完的・一時的**に実装する位置づけであり、将来 godot-mcp 本体に同等機能が取り込まれた時点で削除対象となる。新機能を `godot-ext` に追加する際は「godot-mcp に取り込む余地がないか」を先に確認する。
