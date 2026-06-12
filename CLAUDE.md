# tps

Godot 4.6 (C#) で作る TPS (Third-Person Shooter) ゲーム。
汎用ゲーム基盤 **gamekit** と、その上に乗る **tps** ゲーム実装の 2 層で構成する（ADR-0012）。

## 技術スタック

- Godot 4.6 / C# (.NET 8・.NET 10) / Jolt Physics / Direct3D 12 (Windows) / Metal (macOS)
  - net8.0: `gamekit` / `gamekit.contract` / `gamekit.client` / `gamekit.godot` / `tps.godot` / `tps.csharp` / `tps.contract` / `tps.client`
  - net10.0: `gamekit.mcp` / `gamekit.test` / `tps.cli` / `tps.mcp` / `tps.csharp.test`
- プロジェクトディレクトリ: `tps.godot/`

## プロジェクト構成

### gamekit（汎用基盤・ゲームジャンル非依存）

| プロジェクト | 役割 |
|---|---|
| `gamekit/` | ECS コア（`World` / `Entity` / `EntityId`）・シーン抽象（`IScene` / `ISceneQuery`）・構造化ログストア |
| `gamekit.contract/` | 汎用エンドポイント（`InputEndpoints`）・DTO・ライフサイクルコマンド（Pause / Resume / Quit） |
| `gamekit.client/` | HTTP 通信の基底 `GameApiClient`（state は `GetStateAsync<TState>` / `GetStateRawAsync` の 2 口） |
| `gamekit.godot/` | Godot アダプタ。`GameHttpServer`・組み込みルート（`GameApiRoutes`）・ロギングプロバイダ・Vector 変換 |
| `gamekit.mcp/` | 汎用 MCP ツール（ping・入力シミュレーション・状態取得・スクリーンショット） |
| `gamekit.test/` | 基盤の単体テスト（xUnit + Shouldly） |

### tps（このゲーム）

| プロジェクト | 役割 |
|---|---|
| `tps.godot/` | Godot プロジェクト本体。Node・シーン・物理など Godot 依存コード。HTTP サーバーの組み立てと TPS ルート登録 |
| `tps.csharp/` | TPS のゲームロジック。Component / System / Scene（Godot 非依存） |
| `tps.csharp.test/` | `tps.csharp` の単体テスト（xUnit + Shouldly） |
| `tps.contract/` | TPS コマンド・状態 DTO・TPS エンドポイント（`TpsEndpoints`） |
| `tps.client/` | `TpsGameApiClient`（`GameApiClient` を TPS 固有 API で拡張） |
| `tps.mcp/` | MCP サーバー。gamekit.mcp の汎用ツール + TPS 固有ツール（カメラ操作等）を合成 |
| `tps.cli/` | CLI ツール。汎用 `GameCommands` + TPS 固有 `TpsCommands` |

## アーキテクチャ

### レイヤー構成

```
外部エージェント / テスト
        ↓ MCP / CLI コマンド
   tps.mcp・tps.cli（gamekit.mcp の汎用ツールを合成）
        ↓ HTTP（tps.client / gamekit.client）
   tps.godot (薄いシェル)  ← Godot 依存処理のみ。gamekit.godot の部品で HTTP サーバーを組む
        ↓ interface
   tps.csharp (ロジック)   ← Godot 非依存、テスト可能
        ↓
   gamekit (基盤)          ← ECS / シーン抽象 / ログストア。ゲームジャンル非依存
```

### gamekit と tps の境界（ADR-0012）

- 参照は常に `tps.*` → `gamekit.*` の一方向。**gamekit は tps を参照しない**
- **具象 Component は基盤に置かない**。`IComponent` だけが基盤で、Component はすべてゲーム定義
  （共通に見えるものは「2 つ目のゲームが必要としたら昇格」ルール）
- **VitalRouter が基盤の公式コマンドバス**。コマンドは `VitalRouter.ICommand` 実装
- **Godot の Node はゲーム側**（シーンにアタッチするスクリプトはゲームアセンブリ必須）。
  基盤はプレーンクラスを提供し、Node がコンポジションで使う
- `gamekit.test` は tps の語彙（`GameEvents` 等）に依存させない
- ProjectReference はソース上で直接 using しているプロジェクトに明示的に張る（推移的参照に頼らない）。直接 using しなくなったら外す

### tps.godot の責務

- インテグレーション・アプリ起動・物理/衝突判定・プラットフォーム依存処理
- **DI ルート**：`Main.cs` が全依存を生成・配線する
- **Game API の組み立て**：`InputServer`(autoload) が gamekit.godot の `GameHttpServer` を構成し、
  TPS ルート（camera_pitch / look_at / set_aiming）と state ビルダーを登録する。
  TPS ルートは受信したコマンド（ICommand 兼リクエスト DTO）を Router へ publish するだけ（ADR-0013）。
  state の組み立ては tps.csharp の `GameStateResponseBuilder`（Godot 非依存・テスト可能）

### tps.csharp の責務（ECS ライク）

- **Component**：純粋データ（`HealthComponent`、`WeaponComponent`、`TransformComponent` 等）
- **System**：Component を処理するロジック（Godot 非依存、xUnit でテスト可能）
- ECS の置き場（`World` / `Entity` / `EntityId`）は gamekit にあり、tps.csharp はその上に Component / System / Scene を定義する

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
- System レベルのライフサイクルコマンド（Pause / Resume / Quit）は `gamekit.contract`、ゲーム固有コマンドは `tps.contract` に置く
- 実行可能なコマンドは現在のシーンが管理し、`IScene.AvailableCommands` 経由で公開する
- MCP はコマンドを叩く一形態。ゲーム内部も同じ口を使う

### EntityId

- `UnitGenerator` で生成した強型 `EntityId`（基底型は `string`、定義は gamekit）
- ID 採番ロジックは `IIdGenerator` で抽象化し差し替え可能

## ロギング・状態公開

| 用途 | フォーマット |
|---|---|
| xUnit テスト（インプロセス） | オブジェクトそのまま |
| MCP → LLM エージェント | ToonEncoder（トークン節約） |
| デバッグ確認・ファイル | JSONL（人間が読める） |

- ログは文字列でなく**構造化ログストア**（型付きイベントレコード）で管理する
  - 仕組み（`ILogStore` / `GameLogEntry` / `InMemoryLogStore`）は gamekit、イベント名定数（`GameEvents`）は tps.csharp
- テストでは「コマンド送信後に期待するログイベントが記録されているか」をアサートする
- エラーレベル以上のログが出ていないことも共通アサートとして入れる

## 開発ルール

- コード変更後に `dotnet build` が自動実行される。ビルドエラーが報告されたら必ず修正してから完了とすること。
- 機能追加が完了したら自動でコミットする。
- `.cs` の一括置換に PowerShell の `Get-Content` / `Set-Content` を使わない（BOM なし UTF-8 を CP932 と誤認し日本語コメントが文字化けする）。`[System.IO.File]::ReadAllText` / `WriteAllText` を使う。
- 稼働中の godot-ext MCP サーバーが `tps.mcp/bin` の DLL をロックするため、tps.mcp のビルド検証は `dotnet build tps.mcp -o tps.mcp/bin/verify` のように別出力先で行う（検証後に削除）。

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
- `tps.mcp` のコード変更は MCP 接続時のビルドが使われるため、次回の MCP 再接続（セッション再起動）から有効になる（ADR-0011 参照）

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
- HTTP 通信層は `gamekit.client/GameApiClient.cs`（汎用）+ `tps.client/TpsGameApiClient.cs`（TPS 拡張）。MCP・CLI 両方が参照する
