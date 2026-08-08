using System;

namespace KiKs.UI
{
    /// <summary>KiKs.UI → Assembly-CSharp 的事件桥。避免跨 asmdef 引用。</summary>
    public static class DialogueBridge
    {
        public static Action OnAdvance;
    }
}
