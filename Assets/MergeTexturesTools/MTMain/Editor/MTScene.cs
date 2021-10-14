using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class MTScene
{
    private GUIStyle style;

    private MergeConfig mergeConfig;
    private MTMainWindow mainWindow;

    private Vector2 scrollPosition;
    private List<Rect> previewRectsList;
    private Texture backGroundTex;

    private int focusRectID = -1;
    public int FocusRectID
    {
        get
        {
            //GUI.BringWindowToFront(focusRectID);
            if (previewRectsList.Count == 0)
            {
                focusRectID = -1;
            }

            return focusRectID;
        }

        set
        {
            focusRectID = value;
        }
    }

    private static Color outLineColor = new Color(1, 0.3803922f, 0, 1);

    public MTScene(MTMainWindow window, MergeConfig config)
    {
        mainWindow = window;
        mergeConfig = config;
        backGroundTex = Resources.Load<Texture>("SceneBG");
        previewRectsList = new List<Rect>();
    }

    public void OnDraw()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.scrollView);
        }

        var rect = new Rect(0, 0, mainWindow.position.width - MTConst.InspectorWidth, mainWindow.position.height);
        //scrollVec = EditorGUILayout.BeginScrollView(new Vector2(0, 0), new GUIStyle(), GUILayout.MaxWidth(mainWindow.position.width - MTConst.InspectorWidth - MTConst.Space));
        scrollPosition = GUI.BeginScrollView(rect, scrollPosition, new Rect(0, 0, rect.width * (mergeConfig.textureWidth / rect.width) * MTConst.ScrollBarEx, rect.height * (mergeConfig.textureWidth / rect.height) * MTConst.ScrollBarEx), true, true);
        {
            scrollPosition = MoveSceneWithMouseScroll(scrollPosition, Event.current);
            {
                //GUILayout.Label("[Scene] 背景图左上角为" + GetBGPreviewOrigin());
                //GUILayout.Space(MTConst.Space);
                if (mergeConfig.textureWidth > 0)
                {
                    //左上角 (0,MTConst.Space * 2) 右下角(TextureWidth,TextureWidth+MTConst.Space * 2)
                    Rect previewRect = new Rect(Vector2.zero, Vector2.one * mergeConfig.textureWidth);
                    EditorGUI.DrawPreviewTexture(previewRect, backGroundTex);

                    DrawRuler(previewRect);
                    DrawPowerPoint(previewRect);
                }

                int subCount = mergeConfig.modelMergeConfigsList.Count;
                if (subCount == 0)
                {
                    previewRectsList = new List<Rect>();
                    GUI.EndScrollView();

                    return;
                }

                for (int i = 0; i < subCount; i++)
                {
                    int index = i;
                    if (previewRectsList.Count < subCount)
                    {
                        previewRectsList.Add(new Rect(GetBGPreviewOrigin(), Vector2.zero));
                    }

                    if (mergeConfig.modelMergeConfigsList[index].mainTexture != null)
                    {
                        Texture tex = mergeConfig.modelMergeConfigsList[index].mainTexture;
                        Vector2 origin = mergeConfig.modelMergeConfigsList[index].origin;
                        Vector2 texSize = new Vector2(tex.width, tex.height);
                        /*previewRectsList[index] = GUI.Window(index, previewRectsList[index], (id) =>
                        {
                            EditorGUI.DrawPreviewTexture(new Rect(Vector2.zero, texSize), tex);//贴图预览
                            if (mainWindow.showPreviewTexInfo)
                            {
                                GUI.Label(new Rect(0, 0, 200, 50), "Sub " + index);
                                //GUI.Label(new Rect(0, 20, 200, 50), "Origin:" + previewRectsList[index].position.ToString());
                                GUI.Label(new Rect(0, 20, 200, 50), "Size:" + texSize.ToString());
                            }

                            //GUI.DragWindow();
                        }
                        , tex.name);*///如果使用Window的方法 会不跟随滚动框的偏移
                        previewRectsList[index] = new Rect(origin, texSize);
                        EditorGUI.DrawPreviewTexture(previewRectsList[index], tex);//贴图预览
                        if (mainWindow.showPreviewTexInfo)
                        {
                            GUI.Label(new Rect(origin.x, origin.y, 200, 50), "Sub " + index);
                            //GUI.Label(new Rect(0, 20, 200, 50), "Origin:" + previewRectsList[index].position.ToString());
                            GUI.Label(new Rect(origin.x, origin.y + 20, 200, 50), "Size:" + texSize.ToString());
                        }

                        previewRectsList[index] = MoveWindow(index, previewRectsList[index], Event.current);
                        previewRectsList[index] = new Rect(ClampPreviewTexturePos(previewRectsList[index].position), texSize);
                        mergeConfig.modelMergeConfigsList[index].origin = previewRectsList[index].position;//记录原点坐标
                                                                                                           //Debug.Log(previewRectsList[index].position + " " + scrollPosition);
                    }
                }

                RemoveRedundantRectWindow();
                FocusRectWindow(Event.current);
                DrawUV();
                DrawOutLine4FocusRect();

                if (mainWindow.autoMatchWidth)
                {
                    Vector2 furthestV2 = GetFurthestPreviewTexturePos();
                    int max = (int)Mathf.Max(furthestV2.x, furthestV2.y);
                    int temp = max;
                    int powerMax = ClosestPowerOf2(temp);
                    max = max <= powerMax ? powerMax : 2 * ClosestPowerOf2(max);
                    mergeConfig.textureWidth = max;
                }
            }
            GUI.EndScrollView();
        }
        //EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 通过点击聚焦窗口
    /// </summary>
    /// <param name="current"></param>
    private void FocusRectWindow(Event current)
    {
        if (current == null || current.rawType != EventType.MouseDown)
        {
            return;
        }

        Vector2 inputPos = current.mousePosition;
        for (int i = 0; i < previewRectsList.Count; i++)
        {
            if (previewRectsList[i].Contains(inputPos))
            {
                /*if (FocusRectID == i)
                {
                    break;
                }*/

                FocusRectID = i;
            }
        }
    }

    private int delay = 0;
    /// <summary>
    /// 通过↑↓←→微调预览图位置
    /// 拖拽窗口
    /// </summary>
    private Rect MoveWindow(int id, Rect clientRect, Event current)
    {
        if (FocusRectID == -1 || FocusRectID != id || current == null)
        {
            return clientRect;
        }

        Rect windowRect = previewRectsList[FocusRectID];
        Vector2 position = windowRect.position;

        switch (current.rawType)
        {
            case EventType.KeyDown:
                switch (current.keyCode)
                {
                    case KeyCode.UpArrow:
                        position -= Vector2.up;
                        break;
                    case KeyCode.DownArrow:
                        position += Vector2.up;
                        break;
                    case KeyCode.LeftArrow:
                        position -= Vector2.right;
                        break;
                    case KeyCode.RightArrow:
                        position += Vector2.right;
                        break;
                }
                break;
            case EventType.MouseUp:
            case EventType.MouseDown:
                if (current.button == 0)
                {
                    delay = 0;
                }
                break;
            case EventType.MouseDrag:
                if (current.button == 0)//左键才能拖动图片
                {
                    if (mainWindow.enableDragDelay)
                    {
                        Vector2 pos = Vector2.zero;
                        if (IsNearPowerPoint(position, out pos))
                        {
                            delay++;
                            if (delay > 20)
                            {
                                position += current.delta;
                                break;
                            }

                            position = pos;
                        }
                        else
                        {
                            delay = 0;
                            position += current.delta;
                        }
                    }
                    else
                    {
                        position += current.delta;
                    }
                }
                break;
        }
        windowRect.position = position;
        mainWindow.Repaint();

        return windowRect;
    }

    private bool dragScene;
    /// <summary>
    /// 使用鼠标滚轮移动整体
    /// </summary>
    private Vector2 MoveSceneWithMouseScroll(Vector2 scrollRect, Event current)
    {
        if (current == null || current.button != 2)//鼠标滚轮事件
        {
            return scrollRect;
        }

        Vector2 position = scrollRect;

        switch (current.rawType)
        {
            case EventType.MouseDown:
                {
                    dragScene = true;
                    FocusRectID = -1;
                    MTCursorTools.SetDragCursor();
                }
                break;
            case EventType.MouseDrag:
                {
                    if (dragScene)
                    {
                        position -= current.delta;
                    }
                }
                break;
            case EventType.MouseUp:
                {
                    MTCursorTools.Reset2NormalCursor();
                    dragScene = false;
                }
                break;
        }

        mainWindow.Repaint();

        return position;
    }

    /// <summary>
    /// 获取到距离背景原点最远的贴图的最远点
    /// </summary>
    private Vector2 GetFurthestPreviewTexturePos()
    {
        if (mergeConfig.textureWidth < 0 || previewRectsList.Count <= 0)
        {
            return Vector2.zero;
        }

        List<int> xEdgesList = new List<int>();
        List<int> yEdgesList = new List<int>();
        for (int i = 0; i < previewRectsList.Count; i++)
        {
            Rect rect = previewRectsList[i];
            xEdgesList.Add((int)(rect.position.x + rect.width));
            yEdgesList.Add((int)(rect.position.y + rect.height));
        }

        xEdgesList.Sort(new IDescendingOrder());
        yEdgesList.Sort(new IDescendingOrder());

        return new Vector2(xEdgesList[0], yEdgesList[0]);
        return Vector2.one * mergeConfig.textureWidth;
    }

    /// <summary>
    /// 将value限制在最接近的2^x上
    /// </summary>
    private int ClosestPowerOf2(int value)
    {
        return Mathf.ClosestPowerOfTwo(value);
    }

    /// <summary>
    /// 是否是二的次幂点
    /// </summary>
    private bool IsPowerPoint(Vector2 point)
    {
        return Mathf.IsPowerOfTwo((int)point.x)
            && Mathf.IsPowerOfTwo((int)point.y);
    }

    /// <summary>
    /// 检测范围内有没有powerpoint，有的话返回true并out出去
    /// </summary>
    private bool IsNearPowerPoint(Vector2 point, out Vector2 powerPoint)
    {
        int checkRange = mainWindow.dragDelaySens;

        powerPoint = point;
        for (int x = -checkRange; x < checkRange; x++)
        {
            for (int y = -checkRange; y < checkRange; y++)
            {
                int checkX = (int)point.x + x;
                int checkY = (int)point.y + y;
                Vector2 checkPoint = new Vector2(checkX, checkY);
                if (IsPowerPoint(checkPoint))
                {
                    powerPoint = checkPoint;

                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 限制预览图坐标>=0
    /// </summary>
    private Vector2 ClampPreviewTexturePos(Vector2 currentPos)
    {
        currentPos.x = Mathf.Clamp(currentPos.x, 0, int.MaxValue);
        currentPos.y = Mathf.Clamp(currentPos.y, 0, int.MaxValue);

        return currentPos;
    }

    private void DrawRuler(Rect bgRect)
    {
        Rect rightTop = new Rect(bgRect.position.x + bgRect.size.x, bgRect.position.y, 100, 50);
        GUI.Label(rightTop, rightTop.position.ToString());

        Rect rightBottom = new Rect(bgRect.position.x + bgRect.size.x, bgRect.position.y + bgRect.size.y, 100, 50);
        GUI.Label(rightBottom, rightBottom.position.ToString());
    }

    /// <summary>
    /// 绘制2^n顶点
    /// </summary>
    /// <param name="bgRect"></param>
    private void DrawPowerPoint(Rect bgRect)
    {
        if (!mainWindow.showPreviewPowerPoint)
        {
            return;
        }

        Vector2 size = bgRect.size;
        for (int x = 0; x < size.x; x++)
        {
            if (!Mathf.IsPowerOfTwo(x))
            {
                continue;
            }

            for (int y = 0; y < size.y; y++)
            {
                if (/*y != x ||*/ !Mathf.IsPowerOfTwo(y))
                {
                    continue;
                }

                Vector2 position = new Vector2(x, y);
                EditorGUI.DrawRect(new Rect(position, Vector2.one * 2), Color.red);
            }
        }
    }

    /// <summary>
    /// 给选中的贴图绘制边框
    /// </summary>
    private void DrawOutLine4FocusRect()
    {
        if (FocusRectID == -1)
        {
            return;
        }

        Rect focusRect = previewRectsList[FocusRectID];
        Vector2 lt = focusRect.position;
        Vector2 rt = focusRect.position + Vector2.right * focusRect.width;
        Vector2 lb = lt - Vector2.down * focusRect.height;
        Vector2 rb = rt - Vector2.down * focusRect.height;

        Handles.color = outLineColor;
        Handles.DrawLine(lt, rt);
        Handles.DrawLine(lt, lb);
        Handles.DrawLine(rt, rb);
        Handles.DrawLine(lb, rb);
    }

    private void DrawUV()
    {
        if (!mainWindow.showUVLines)
        {
            return;
        }

        var list = mergeConfig.modelMergeConfigsList;
        List<Vector2> uvPointsDrawList = new List<Vector2>();

        for (int i = 0; i < list.Count; i++)
        {
            var config = list[i];
            Mesh mesh = config.mesh;
            var uvs = mesh.uv;
            int[] tris = mesh.triangles;
            Vector2[] points = new Vector2[uvs.Length];

            for (int k = 0; k < points.Length; k++)
            {
                points[k] = ConvertUV2ScenePoint(uvs[k], previewRectsList[i]);
            }

            for (int j = 0; j < tris.Length; j += 3)
            {
                Vector2 p0 = points[tris[j]];
                Vector2 p1 = points[tris[j + 1]];
                Vector2 p2 = points[tris[j + 2]];

                //uvPointsDrawList.AddRange(new Vector2[6] { p0, p1, p1, p2, p2, p0 });
                uvPointsDrawList.AddRange(new Vector2[6] { p0, p1, p2, p0, p1, p2 });
            }
        }

        DrawUVLines(uvPointsDrawList);
    }

    /// <summary>
    /// 获取uv在Scene中对应的坐标
    /// </summary>
    private Vector2 ConvertUV2ScenePoint(Vector2 uv, Rect texRect)
    {
        Vector2 point = Vector2.zero;

        point.x = texRect.position.x + texRect.width * uv.x;
        point.y = texRect.height + texRect.position.y + texRect.height * -uv.y;

        return point;
    }

    public void DrawUVLines(List<Vector2> points)
    {
        //Handles.BeginGUI();
        Handles.color = Color.white;

        for (int i = 0; i < points.Count; i += 2)
        {
            Handles.DrawLine(points[i + 0], points[i + 1]);
        }

        //Handles.EndGUI();
    }

    /// <summary>
    /// 背景图的原点
    /// </summary>
    private Vector2 GetBGPreviewOrigin()
    {
        return Vector2.zero;
        return new Vector2(0, MTConst.Space * 2);
    }

    public void RemoveRectAt(int index)
    {
        previewRectsList.RemoveAt(index);
    }

    /// <summary>
    /// 移除多余的预览窗口
    /// </summary>
    private void RemoveRedundantRectWindow()
    {
        while (previewRectsList.Count > mergeConfig.modelMergeConfigsList.Count)
        {
            int index = previewRectsList.Count - 1;
            previewRectsList.RemoveAt(index);
        }
    }
}

/// <summary>
/// 降序排列
/// </summary>
public class IDescendingOrder : IComparer<int>
{
    public int Compare(int x, int y)
    {
        return y.CompareTo(x);
    }
}