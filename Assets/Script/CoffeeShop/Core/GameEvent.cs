using System;
using System.Collections.Generic;

/// <summary>
/// 通用事件总线。用 string channel + object payload，不引用任何业务类型。
/// 模块之间只通过事件通信，不在代码里互相引用。
/// </summary>
public static class GameEvent
{
    private static readonly Dictionary<string, List<Action<object>>> _channels = new(StringComparer.Ordinal);

    public static void Emit(string channel, object payload = null)
    {
        if (string.IsNullOrEmpty(channel)) return;
        if (!_channels.TryGetValue(channel, out var handlers)) return;

        for (int i = handlers.Count - 1; i >= 0; i--)
        {
            try { handlers[i]?.Invoke(payload); }
            catch (Exception e) { UnityEngine.Debug.LogError($"[GameEvent] Handler error on '{channel}': {e}"); }
        }
    }

    public static void On(string channel, Action<object> handler)
    {
        if (string.IsNullOrEmpty(channel) || handler == null) return;
        if (!_channels.TryGetValue(channel, out var handlers))
        {
            handlers = new List<Action<object>>();
            _channels[channel] = handlers;
        }
        handlers.Add(handler);
    }

    public static void Off(string channel, Action<object> handler)
    {
        if (string.IsNullOrEmpty(channel) || handler == null) return;
        if (!_channels.TryGetValue(channel, out var handlers)) return;
        handlers.Remove(handler);
    }

    public static void ClearChannel(string channel)
    {
        if (!string.IsNullOrEmpty(channel))
            _channels.Remove(channel);
    }
}
