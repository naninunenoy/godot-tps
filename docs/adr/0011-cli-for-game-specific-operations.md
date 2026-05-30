# ADR-0011: ゲーム固有操作を CLI で実装する

- Status: Accepted
- Date: 2026-05-30

## Context

`tps.mcp`（godot-ext）は C# の exe プロセスとして動作するため、コードを変更するたびに Claude Code の MCP 接続を切断・再接続する必要がある。

開発中はゲーム固有の操作（照準、カメラ制御、アクション入力など）を頻繁に追加・変更するため、このサイクルが開発の摩擦になっていた。

### C# MCP のホットリロード制約

`dotnet watch run` を使えば変更検知→再ビルド→再起動を自動化できるが、プロセス再起動 = MCP 接続断 であるため Claude Code の再接続は依然として必要になる。メソッド実装の変更のみ Hot Reload で吸収できるが、MCP ツールの追加・シグネチャ変更といった頻繁な変更には適用できない。

## Decision

ゲーム固有の操作を **CLI ツール（`tps.cli`）** として実装する。

あわせて MCP・CLI 共通の HTTP 通信層を **`tps.client`（`GameApiClient`）** として別プロジェクトに切り出し、重複を排除する。

### 役割分担

| 手段 | 対象 | 理由 |
|---|---|---|
| MCP (`godot-ext`) | 安定・汎用な操作（`ping`, `get_game_state`, `take_screenshot`, `get_available_commands` など） | 変更頻度が低い。スキーマが Claude Code に自動注入される |
| CLI (`tps.cli`) | ゲーム固有の操作（`press_action`, `set_aiming`, `set_camera_pitch`, `look_at` など） | 変更頻度が高い。呼び出しのたびに新プロセスを起動するため、ビルドだけで即反映される |

### アーキテクチャ

```
Claude Code
  ├─ MCP (godot-ext)  ─┐
  └─ Bash tool          ├─ tps.client (GameApiClient) ─→ tps.godot (HTTP)
       ↓ dotnet run      │
     tps.cli  ──────────┘
```

- `tps.client` は `tps.contract` の DTO・エンドポイント定義を使い HTTP 呼び出しをカプセル化
- MCP・CLI どちらも `GameApiClient` 経由でゲームと通信する

## 理由

- **再接続なしで反映できる**: CLI は呼び出しのたびに新プロセスを起動するため、`dotnet build` だけで次の呼び出しから新しいコードが使われる
- **通信層の重複を避けられる**: `tps.client` に集約することで MCP・CLI で同じ HTTP ロジックを二重管理しない
- **Claude Code から自然に呼べる**: Bash ツールで `dotnet run --project tps.cli/ -- <command>` を実行するだけでよい
- **使い方の発見が容易**: `help` / `help <command>` サブコマンドで AI・人間ともにその場で仕様を確認できる

## Consequences

| メリット | デメリット |
|---|---|
| 変更→ビルド→即使用。MCP 再接続サイクルが不要 | MCP と異なりツールスキーマが自動注入されないため、AI は `help` で自ら調べる必要がある |
| `tps.client` で通信層を一元管理できる | CLI はゲームが起動済みでないと使用不可（MCP も同様） |
| MCP には安定した汎用ツールだけを残せる | `dotnet run` の初回起動に数秒かかる（ビルド済みバイナリなら不要） |
