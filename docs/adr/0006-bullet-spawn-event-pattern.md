# ADR-0006: 弾生成を BulletSpawnRequested イベント経由にして System 層を Godot 非依存に保つ

- Status: Accepted
- Date: 2026-05-22

## Context

`WeaponSystem.TryFire()` が弾を発射するとき、弾の Godot Node（`PackedScene` のインスタンス化・シーンへの追加）を System 内で生成する案を検討した。
しかし `PackedScene` や `GetTree()` は Godot 依存であり、System 層に持ち込むと `tps.csharp` が Godot に依存してしまい、xUnit での単体テストができなくなる。

## Decision

`WeaponSystem.TryFire()` は弾を直接生成せず、`BulletSpawnRequested` コマンドを VitalRouter に発行する。
弾の実体化は `Player` Node が `[Route] void On(BulletSpawnRequested cmd)` で受け取り、`PackedScene.Instantiate<Bullet>()` で行う。

```
WeaponSystem.TryFire()
  → router.PublishAsync(new BulletSpawnRequested { Speed, Damage, ... })
      → Player.On(BulletSpawnRequested cmd)
          → BulletScene.Instantiate<Bullet>()
          → scene.AddChild(bullet)
```

## Consequences

- `WeaponSystem` は Godot 型を一切参照しないため、xUnit で `TryFire()` のテストが書ける
- 弾生成の責任が `Player` Node に集中し、発射位置・向きなどの Godot 座標系の処理も Node 側で完結する
- 将来「別の Actor が発射する」ケースが生じた場合、同じ `BulletSpawnRequested` を購読するハンドラを追加するだけで対応できる
- コマンドが非同期に配送されるため、発射フレームと生成フレームが1フレームずれる可能性がある（現状 `_Process` 内で `await` なしで呼んでいるため実害なし）
