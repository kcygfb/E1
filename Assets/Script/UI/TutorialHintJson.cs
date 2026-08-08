using System;

/// <summary>
/// JSON 中可选的教学提示配置。description 为空时不会显示教学框。
/// </summary>
[Serializable]
public class TutorialHintJson
{
    public string description;
    public string targetId;
    public string placement = "Above";
    public float offsetX;
    public float offsetY = 48f;
}
