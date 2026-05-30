# tps

Godot 4.6 (C#) で作る TPS (Third-Person Shooter) ゲーム。

## 技術スタック

- Godot 4.6 / C# (.NET 8・.NET 10) / Jolt Physics / Direct3D 12 (Windows) / Metal (macOS)
  - `tps.godot` / `tps.csharp` / `tps.contract` / `tps.client` は net8.0、`tps.cli` / `tps.mcp` / `tps.csharp.test` は net10.0
- プロジェクトディレクトリ: `tps.godot/`

## プロジェクト構成

| プロジェクト | 役割 |
|---|---|
| `tps.godot/` | Godot プロジェクト本体。Node・シーン・物理など Godot 依存コード |
| `tps.csharp/` | 純粋 C# クラスライブラリ。Godot 非依存のゲームロジック |
| `tps.csharp.test/` | `tps.csharp` の単体テスト（xUnit + Shouldly） |
| `tps.contract/` | コマンド定義など共有型 |
| `tps.client/` | ゲーム HTTP 通信層。MCP・CLI 共通の `GameApiClient` |
| `tps.mcp/` | MCP サーバー。ゲームへのコマンド投入・状態取得 |
| `tps.cli/` | CLI ツール。ゲーム固有操作をコマンドラインから実行 |

## アーキテクチャ

### レイヤー構成

```
外部エージェント / テスト
        ↓ MCP コマンド
   tps.mcp (godot-ext)
        ↓
   tps.godot (薄いシェル)  ← Godot 依存処理のみ
        ↓ interface
   tps.csharp (ロジック)   ← Godot 非依存、テスト可能
```

### tps.godot の責務

- インテグレーション・アプリ起動・物理/衝突判定・プラットフォーム依存処理
- **DI ルート**：`Main.cs` が全依存を生成・配線する
- **Game API の実装**：シーン状態の公開とコマンドの受付

### tps.csharp の責務（ECS ライク）

- **Component**：純粋データ（`HealthComponent`、`WeaponComponent`、`TransformComponent` 等）
- **System**：Component を処理するロジック（Godot 非依存、xUnit でテスト可能）
- **World**：EntityId → Component のデータ置き場

`tps.csharp` は Godot に依存しない。Godot との境界は interface 経由のみ。

### CQRS：書き込みと読み取りの分離

| 操作 | 経路 |
|---|---|
| Godot → World 書き込み | コマンド経由 |
| Godot → World 読み取り | `World.GetComponent<T>()` 直接（毎フレーム） |
| 外部 → World 書き込み | コマンド経由（MCP・テストも同じ口） |
| 外部 → World 読み取り | `ISceneQuery` 経由 |

### コマンド設計

- コマンドは**プレイヤー・システムの意図**で切る（UI の実装詳細を含めない）
- 対象の階層：System レベル / Scene レベル / Object レベル
- 実行可能なコマンドは現在のシーンが管理し、`IGameApi` 経由で公開する
- MCP はコマンドを叩く一形態。ゲーム内部も同じ口を使う

### EntityId

- `UnitGenerator` で生成した強型 `EntityId`（基底型は `string`）
- ID 採番ロジックは `IIdGenerator` で抽象化し差し替え可能

## ロギング・状態公開

| 用途 | フォーマット |
|---|---|
| xUnit テスト（インプロセス） | オブジェクトそのまま |
| MCP → LLM エージェント | ToonEncoder（トークン節約） |
| デバッグ確認・ファイル | JSONL（人間が読める） |

- ログは文字列でなく**構造化ログストア**（型付きイベントレコード）で管理する
- テストでは「コマンド送信後に期待するログイベントが記録されているか」をアサートする
- エラーレベル以上のログが出ていないことも共通アサートとして入れる

## 開発ルール

- コード変更後に `dotnet build` が自動実行される。ビルドエラーが報告されたら必ず修正してから完了とすること。
- 機能追加が完了したら自動でコミットする。

### テストコードの規約

- 各テストメソッドには `<summary>` XML ドキュメントコメントを必ず書く。
- コメントには「何を検証するか」と「期待する結果（具体的な値）」を日本語で記述する。
  - 例: `/// <summary>TakeDamage(30)でHPが100から70に減ること。</summary>`

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

## tps.cli の使い方

使用可能なコマンドとオプションは `--help` で確認すること：

```bash
# コマンド一覧
dotnet run --project tps.cli/ -- --help

# 個別コマンドの詳細
dotnet run --project tps.cli/ -- <command> --help
```

- CLI はゲームが起動済みの状態でのみ使用できる（MCP の `run_project` で起動後）
- `tps.client/GameApiClient.cs` が HTTP 通信層。MCP・CLI 両方が参照する
