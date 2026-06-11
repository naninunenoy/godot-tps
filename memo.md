# memo: gamekit 切り出しで後世に残す設計判断・トレードオフ

[plan.md](plan.md) の実施中に下した判断のうち、コードだけでは読み取れない背景・トレードオフを記録する。
正式な設計判断は最終的に ADR（docs/adr）へ昇格させる。

## Phase 1: gamekit（ECS コア）切り出し

### グローバル using で移行コストを抑えた

`World` / `Entity` 等の名前空間変更（`tps.csharp` → `gamekit`）に伴う全ファイルの using 修正を避けるため、
csproj の `<Using Include="gamekit" />`（tps.godot は `GlobalUsings.cs`）でグローバル using にした。

- 利点: 移行 churn 最小。基盤の型はプロジェクト全域で使うため違和感がない
- 欠点: ファイル単位では gamekit への依存が見えない。IDE の「usingの整理」では消えない依存になる

### 基盤テストはゲームの語彙に依存させない

`LogStoreTest` を gamekit.test へ移す際、`GameEvents.ShotFired` 等の TPS 定数を `"SampleEvent"` 等の
中立なリテラルに置き換えた。**gamekit.test が tps.* を参照した時点で「基盤が独立している」保証が壊れる**ため、
今後も gamekit.test には tps.* への参照を追加しないこと。

### SequentialIdGenerator のカウンタは prefix 間で共有（現仕様）

`Next("a")` → `a#1`、`Next("b")` → `b#2` となる。prefix ごとの連番ではない。
既存挙動をそのままテストで固定化した（`SequentialIdGeneratorTest`）。
prefix ごとの連番が欲しくなったら基盤側で仕様変更し、このテストを書き換える。

### EntityId の wire format は名前空間に依存しない

UnitGenerator の JsonConverter により素の文字列（`"player#1"`）にシリアライズされる。
名前空間移動でログ・HTTP API の互換性は壊れない。`EntityIdTest` で担保済み。

### 運用メモ: 稼働中の MCP サーバーが tps.mcp のビルドをブロックする

Claude Code セッションが godot-ext（`dotnet run --project tps.mcp`）を起動していると、
`tps.mcp/bin/Debug` の DLL がロックされ `dotnet build tps.mcp` が MSB3027 で失敗する。
コンパイル検証だけなら `dotnet build tps.mcp -o tps.mcp/bin/verify` のように別出力先を使う（検証後削除）。

## Phase 2: gamekit.contract（汎用コントラクト）切り出し

### エンドポイント定数を InputEndpoints / TpsEndpoints に分割

- `gamekit.contract.Mcp.InputEndpoints`: Port / BaseUrl / ping / actions / press_action / screenshot / state / commands
- `tps.contract.Mcp.TpsEndpoints`: camera_pitch / look_at / set_aiming

サーバーは 1 プロセス 1 ポートなので Port / BaseUrl は基盤側に置いた。
トレードオフ: パス文字列が 2 クラスに分かれるため、**パスの重複をコンパイル時に検出できない**。
ルート登録機構を作る Phase 4 で、実行時の重複検出を入れるのが望ましい。

### ライフサイクルコマンドは基盤のコントラクト

Pause / Resume / Quit はどのゲームにもある「System レベル」のコマンドなので gamekit.contract へ移した。
これらは `VitalRouter.ICommand` を実装しており、**「コマンド = VitalRouter.ICommand」という規約ごと基盤が持つ**
（= VitalRouter は基盤の公式コマンドバス。`ICommandPublisher` のような独自抽象はあえて作らない）。
この判断は Phase 6 で ADR 0012 に正式化する。

### 名前空間 `Mcp` は命名負債として温存

`gamekit.contract.Mcp` の実態は「リモート操作 HTTP API のコントラクト」であり、MCP 専用ではない
（CLI も同じ口を使う）。`Api` 等へのリネームが筋だが、Phase 2 では移行 churn 削減を優先して
既存名 `Mcp` を踏襲した。リネームするなら全フェーズ完了後に一括で行う。

### `GameCommand` 名前空間は基盤・ゲームの両方に存在する

`gamekit.contract.GameCommand`（ライフサイクル）と `tps.contract.GameCommand`（TPS コマンド）が併存する。
利用側ファイルには 2 つの using が並ぶことがあるが、既存規約との一貫性を優先した。

### 推移的参照に頼らず、直接 using するプロジェクトには明示参照を張る

SDK スタイルの ProjectReference は推移的にコンパイル参照が届くため、tps.contract 経由でも
gamekit.contract の型は使える。ただし「どのプロジェクトが基盤の何に依存しているか」を csproj から
読めるよう、**ソース上で直接 using しているプロジェクトには明示的に ProjectReference を張る**方針とした。
逆に直接 using しなくなったら明示参照は外す（Phase 3 で tps.client から gamekit.contract を外した例）。

## Phase 3: gamekit.client（汎用 HTTP クライアント）切り出し

### 拡張はコンポジションでなく継承（TpsGameApiClient : GameApiClient）

ゲーム固有 API は「同一サーバー・同一 HttpClient に対するエンドポイント追加」であり、
基底の `Http`（BaseAddress/Timeout 設定済み）と `Serialize` を protected で公開して継承拡張とした。

- 利点: 利用側（MCP ツール・CLI）はクライアント 1 個で汎用 + TPS の全 API に届く。DI も typed client 1 本
- 欠点: protected メンバーが事実上の公開 API になる。変更時は派生クラス（各ゲーム）への影響を考慮すること

### /state は GetStateAsync&lt;TState&gt;() + GetStateRawAsync() の 2 口

state ペイロードはゲーム定義（plan.md 方針 3）のため、基盤クライアントは型を知らない。

- `GetStateAsync<TState>()`: ゲーム側クライアントが具体型を指定して包む（`TpsGameApiClient.GetStateAsync()`）
- `GetStateRawAsync()`: 素の JSON 文字列。型を介さない中継用で、Phase 5 の汎用 MCP ツール
  （JSON → ToonEncoder 変換）が DTO 知識なしで動くために先行追加した

なお `ReadFromJsonAsync` は Web デフォルト（大文字小文字を区別しない）なので、
サーバー側の PascalCase JSON との互換は分割後も変わらない。

### DI の前借り情報: Phase 5 で base 型への forward 登録が要る

tps.mcp は現在 `AddHttpClient<TpsGameApiClient>()` で登録し、全ツールが TpsGameApiClient を受ける。
Phase 5 で汎用ツールを gamekit.mcp に移すと、それらは基底 `GameApiClient` を要求するため、
`services.AddTransient<GameApiClient>(sp => sp.GetRequiredService<TpsGameApiClient>())` のような
forward 登録で**同一インスタンス系列を共有**させること（別々に typed client 登録すると HttpClient が二重になる）。

### 発見: InputSimulationTools に TPS 固有の SetAiming が混在している

`InputSimulationTools`（名前は汎用）に ADS 操作の `SetAiming` ツールが入っている。
Phase 5 で汎用/TPS にツールを分けるとき、SetAiming は TPS 側（CameraControlTools 等）へ移すこと。
