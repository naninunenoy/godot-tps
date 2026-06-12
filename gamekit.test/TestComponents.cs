namespace gamekit.test;

/// <summary>テスト用の数値を持つコンポーネント。</summary>
public record CounterComponent(int Value) : IComponent;

/// <summary>テスト用のタグ名を持つコンポーネント。</summary>
public record TagComponent(string Tag) : IComponent;
