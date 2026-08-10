using UnityEngine;

/// <summary>
/// 可选：启动即全屏，且任意屏幕分辨率下 UI 都不飘。
///
/// 原理：固定 1920x1080 渲染分辨率（与 CanvasScaler 参考分辨率一致，
/// 缩放因子恒为 1，UI 布局完全按设计尺寸），再用 FullScreenWindow
/// 交给显卡拉伸到任意屏幕全屏。宽高比同为 16:9 时完全等比，无变形。
///
/// 使用方式：
/// - 想要"启动即全屏"：保留本文件，直接打包即可（RuntimeInitializeOnLoadMethod
///   自安装，无需挂到任何场景物体）。
/// - 想切回窗口模式：按 Alt+Enter（Player Settings 已开启 allowFullscreenSwitch）。
/// - 不想要此行为：直接删除本文件。
/// </summary>
public static class ScreenPresentation
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyStartupFullscreen()
    {
        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
    }
}
