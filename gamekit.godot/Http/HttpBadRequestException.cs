using System;

namespace gamekit.godot;

/// <summary>リクエストの構文不正を 500 でなく 400 として応答するための内部例外。</summary>
internal sealed class HttpBadRequestException(string message) : Exception(message);
