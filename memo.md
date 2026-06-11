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

## Phase 4: gamekit.godot（Godot アダプタ）切り出し

### Godot.NET.Sdk のライブラリプロジェクトは問題なく成立する

plan.md でリスク扱いだったが、`Godot.NET.Sdk/4.6.2` を使った非ゲームのクラスライブラリは
普通にビルドできた（Node 派生クラスを含まなければ Godot のソースジェネレータも無害）。
代替案（GodotSharp NuGet 直接参照・フォルダ分離）は不要だった。
**SDK バージョンが tps.godot と gamekit.godot の 2 つの csproj に重複している**ので、
Godot アップデート時は両方を揃えること。

### Node はゲーム側、基盤はプレーンクラス（コンポジション）

Godot はシーンにアタッチするスクリプトをゲームアセンブリに要求するため、autoload の
`InputServer`(Node) は tps.godot に残し、HTTP 機能は `GameHttpServer`（プレーンクラス）に委譲した。

- フレーム待ち・タイマーは `SceneTree` を ctor 注入して `tree.ToSignal(tree, ProcessFrame)` で行う
  （Node でなくても GodotObject 経由で await できる）
- 駆動は InputServer の `_Process` → `Poll()`。基盤クラスは自走しない
- スクリーンショットは `tree.Root`（ルート Window = Viewport）から取得。
  元実装の autoload Node の `GetViewport()` と同じものを指す

### ルート重複は登録時に実行時検出（Phase 2 の宿題回収）

パス定数が InputEndpoints / TpsEndpoints に分かれ重複をコンパイル時に検出できないため、
`GameHttpServer.MapGet/MapPost` が同一 (method, path) の再登録で `InvalidOperationException` を投げる。
起動時（_Ready の登録時点）に必ず露見するので、実行時例外でも検出タイミングとしては十分早い。

### /state の規約: stateProvider が null を返したら 503

基盤の組み込みルートはゲーム注入の `Func<object?>` を呼ぶだけ。「未初期化なら null を返す」が
ゲーム側との取り決め。`HttpResult.Json<T>` は宣言型が object でもランタイム型で
シリアライズされる（System.Text.Json の仕様に依存）。

### 事故記録: PowerShell 5.1 の一括置換で日本語コメントが文字化け

`Get-Content`（BOM なし UTF-8 を CP932 と誤認）→ `Set-Content -Encoding utf8` の一括置換で、
tps.godot の日本語コメントが mojibake 化した。`git checkout` で復元し、
`[System.IO.File]::ReadAllText` / `WriteAllText`（UTF-8 既定・BOM なし）でやり直した。
**今後 .cs の一括置換は .NET File API を使うこと。** なお Phase 1〜2 で置換したファイルは
全て ASCII のみだったため実害なし（ただし BOM が付いた）。

### 動作確認

ユニットテストに加え、run_project で実機起動し ping / state / commands / set_aiming の
疎通を確認した（HTTP プラミング移植のリグレッションは unit test で担保できないため）。

## Phase 5: gamekit.mcp 切り出し + CLI 内部分割

### get_game_state / state の出力から null フィールドが消えた（意図的な挙動変更）

汎用化のため、MCP の `get_game_state` と CLI の `state` を「型経由の再シリアライズ」から
「`GetStateRawAsync()` の素の JSON 中継」に変更した。従来は DTO に一度デシリアライズしてから
再シリアライズしていたため `Health: null` のような null フィールドが復活していたが、
サーバー側は `WhenWritingNull` で null を省略しているので、中継後は出力されない。

- 利点: トークン節約（ToonEncoder の目的に合致）。基盤ツールがゲームの DTO を知らなくて済む
- 欠点: 「コンポーネントを持っていない」ことが明示されなくなった。LLM が困るようなら
  サーバー側の JsonOptions を変える（基盤の HttpResult.Json が一元管理している）

### SetAiming ツールは CameraControlTools へ移動（Phase 3 の宿題回収）

MCP ツール名はメソッド名由来（`set_aiming`）なので、クラス間移動では外部互換は壊れない。
ツールのクラス分けは「どの口（汎用/ゲーム固有）か」で決め、名前の互換はメソッド名で守る。

### DI forward 登録を実装（Phase 3 の宿題回収）

`AddHttpClient<TpsGameApiClient>()` + `AddTransient<GameApiClient>(sp => sp.GetRequiredService<TpsGameApiClient>())`。
gamekit.mcp の汎用ツールは基底型で受け、実体は TPS クライアントが流れる。

### CLI は予定どおりプロジェクト分割せず 2 クラス分割

ConsoleAppFramework v5 はソースジェネレータ前提で、コマンドクラスの別アセンブリ化と相性が悪い
（plan.md 方針 5）。`GameCommands`（汎用）と `TpsCommands`（TPS）を同一 exe 内で分け、
`app.Add<T>()` を 2 回呼んでルート名前空間にマージした。
**2 クラス間でメソッド名（=コマンド名）が衝突しないよう注意**（衝突時は CAF が実行時に失敗する）。

### 注意: 稼働中の godot-ext MCP サーバーは旧ビルドのまま

MCP サーバー（dotnet run）はセッション接続時に起動されるため、このフェーズの変更は
**次回の MCP 接続（セッション再起動）から有効**になる。今セッションでは新コードの MCP 経路を
直接検証できないため、同一実装経路（GetStateRawAsync → ToonEncoder）を CLI の `state` で検証した。
