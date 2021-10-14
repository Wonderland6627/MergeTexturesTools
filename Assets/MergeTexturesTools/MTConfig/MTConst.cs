using UnityEngine;

public class MTConst
{
    public const float Space = 10f;//间隔
    public const float MinWindowHeight = 750f;

    public const float InspectorWidth = 300f;

    public const float SceneMinWidth = 512f;
    public const float ScrollBarEx = 1.15f;//滚动框扩展系数

    public const float PreviewTopDragBarHeight = 10f;
    public const float PreviewWindowWidth = 256f;//预览窗口边长
    public const float PreviewBGWidth = 256f;//预览背景边长

    public const string ForceOverlayKey = "ForceOverlay";
}

public enum MTLevel : int
{
    Zero = 0,
    One = 1,
}