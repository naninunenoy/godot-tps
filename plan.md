# カメラ操作統合計画

## 目標

- `press_action` で使えるデジタルカメラ回転アクションを追加する
- 宣言的な精密制御（`set_camera_pitch` / `look_at_position`）は残す
- `InputServer` の `Player` 直接依存を解消し、レイヤー整合性を回復する

---

## 現状の問題

| 問題 | 場所 |
|---|---|
| `InputServer` が `Player` 具象型に直接依存 | `tps.godot/InputServer.cs` |
| MCP 経由のカメラ操作が VitalRouter を素通り | `InputServer` → `Player.SetCameraPitch/FaceToward` 直接呼び出し |
| カメラ回転をデジタルアクションで操作できない | InputMap 未登録 |

---

## 変更一覧

### 1. Godot 側ラッパーコマンドを定義（`tps.godot`）

`tps.contract` に VitalRouter を持ち込まず、`tps.godot` 内で Router 用コマンドを定義する。

```csharp
// tps.godot/Commands/GodotCameraCommands.cs
using VitalRouter;
namespace tps;

public record SetCameraPitchGodotCommand(float PitchRadians) : ICommand;
public record LookAtPositionGodotCommand(float X, float Y, float Z) : ICommand;
```

### 2. `InputServer` をリファクタリング

`Player?` フィールドを削除し、`Router` を受け取るよう `Initialize` を変更する。  
カメラ操作エンドポイントはラッパーコマンドを publish する。

```csharp
// 変更前
public void Initialize(ISceneQuery sceneQuery, IScene scene, Player player)

// 変更後
public void Initialize(ISceneQuery sceneQuery, IScene scene, Router router)
```

```csharp
// POST /camera_pitch
await _router.PublishAsync(new SetCameraPitchGodotCommand(cmd.PitchDegrees * Mathf.Pi / 180f));

// POST /look_at
await _router.PublishAsync(new LookAtPositionGodotCommand(cmd.X, cmd.Y, cmd.Z));
```

### 3. `Player` に `[Route]` ハンドラを追加

```csharp
[Route]
public void On(SetCameraPitchGodotCommand cmd) => SetCameraPitch(cmd.PitchRadians);

[Route]
public void On(LookAtPositionGodotCommand cmd) => FaceToward(cmd.X, cmd.Y, cmd.Z);
```

既存の `SetCameraPitchCommand` / `LookAtPositionCommand` ハンドラは削除する  
（MCP → InputServer → Router → GodotCommand → Player に経路が統一されるため）。

### 4. `Main.cs` の `Initialize` 呼び出しを更新

```csharp
// 変更前
GetNode<InputServer>("/root/InputServer").Initialize(this, _currentScene, GetNode<Player>("Player"));

// 変更後
GetNode<InputServer>("/root/InputServer").Initialize(this, _currentScene, _router);
```

### 5. カメラ回転アクションを InputMap に登録

Godot エディタのプロジェクト設定から以下の 4 アクションを追加する：

| アクション名 | 用途 |
|---|---|
| `camera_rotate_left` | カメラを左（Yaw -）に回転 |
| `camera_rotate_right` | カメラを右（Yaw +）に回転 |
| `camera_look_up` | カメラを上（Pitch -）に回転 |
| `camera_look_down` | カメラを下（Pitch +）に回転 |

デフォルトキーバインドは任意（MCP 用途では不要）。

### 6. `Player._Process` でアクションをポーリング

```csharp
// 既存の移動処理の後に追加
const float RotSpeed = 2.0f; // rad/s、PlayerSettings に移してもよい

var rotX = Input.IsActionPressed("camera_rotate_left")  ? -RotSpeed * (float)delta
         : Input.IsActionPressed("camera_rotate_right") ?  RotSpeed * (float)delta : 0f;
var rotY = Input.IsActionPressed("camera_look_up")   ? -RotSpeed * (float)delta
         : Input.IsActionPressed("camera_look_down") ?  RotSpeed * (float)delta : 0f;

if (rotX != 0f || rotY != 0f)
    _controller.CalcCameraAim(rotX, rotY);
```

### 7. `InGameScene.AvailableCommands` を更新

`SetCameraPitchGodotCommand` / `LookAtPositionGodotCommand` は `tps.godot` 内の型なので  
`InGameScene`（`tps.csharp`）には追加できない。  
→ `InGameScene.AvailableCommands` の記載は contract 型のままとし、  
　Godot 側コマンドは「内部転送用」として /commands エンドポイントには露出しない。

---

## 変更後のデータフロー

```
MCP (set_camera_pitch)
  → POST /camera_pitch
  → InputServer.PublishAsync(SetCameraPitchGodotCommand)
  → VitalRouter
  → Player.On(SetCameraPitchGodotCommand)
  → Player.SetCameraPitch()           ← 統一された経路

MCP (press_action: camera_rotate_left, 500ms)
  → POST /press_action
  → Input.ActionPress("camera_rotate_left")
  → Player._Process がポーリング
  → _controller.CalcCameraAim()       ← 既存経路と同じ
```

---

## 残るトレードオフ

- `SetCameraPitchGodotCommand` と `tps.contract` の `SetCameraPitchCommand` の2種類の型が並存する（変換は InputServer が担う）
- デジタル回転は固定速度なので「N 度だけ回転」には `set_camera_pitch` や `look_at_position` を使う必要がある
- カメラ回転アクションは Godot エディタ操作が必要（コードだけでは完結しない）
