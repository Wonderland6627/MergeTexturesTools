using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum TextureSuffix : int
{
    JPG,
    TGA,
    PNG,
}

/// <summary>
/// 工具内合并配置信息，包括工具面板的设置信息
/// </summary>
[Serializable]
public class MergeConfig
{
    public int textureWidth;//合并后的贴图边长
    public List<ModelMergeConfig> modelMergeConfigsList;//需要合并的网格和贴图
    public string newTextureName;//合并后贴图的名字
    public TextureSuffix newTextureSuffix = TextureSuffix.TGA;

    /// <summary>
    /// 合并对象信息
    /// </summary>
    [Serializable]
    public class ModelMergeConfig
    {
        //public GameObject gameObject;
        public Mesh mesh;
        public Material material;
        public Texture mainTexture;
        public Vector2 origin;

        /// <summary>
        /// 有效配置
        /// </summary>
        public bool IsValid()
        {
            return /*mesh != null &&*/ mainTexture != null;
        }

        public string toString()
        {
            return string.Format("Mesh:{0}\n" +
                                 "Material:{1}\n" +
                                 "Texture:{2}\n" +
                                 "Origin:{3}", mesh.name, material.name, mainTexture.name, origin.ToString());
        }
    }

    public MergeConfig()
    {
        modelMergeConfigsList = new List<ModelMergeConfig>();
    }
}

public class MTMainWindow : EditorWindow
{
    private static MTMainWindow mainWindow;

    public MTInspector inspector;
    public MTScene scene;
    public MTPreview preview;

    public int dragDelaySens = 10;//拖拽辅助灵敏度
    public bool enableDragDelay = false;//是否启用拖拽辅助
    public bool showPreviewPowerPoint = false;//是否显示预览图次幂点
    public bool showPreviewTexInfo = true;//是否显示预览图信息
    public bool showUVLines = false;//是否显示uv

    public bool autoMatchWidth = true;//自动匹配背景边长
    public bool enableForceOverlay = false;//重名文件强制覆盖

    public int FocusID
    {
        get
        {
            return scene.FocusRectID;
        }

        set
        {
            scene.FocusRectID = value;
        }
    }

    public MergeConfig mergeConfig;//总的合并配置 所有控件操作的都是这个字段

    [MenuItem("Wonderland6627/MergeTextureMainWindow")]
    public static void ShowWindow()
    {
        mainWindow = GetWindow<MTMainWindow>("MergeTextureMainWindow");
        mainWindow.minSize = new Vector2(MTConst.SceneMinWidth + MTConst.InspectorWidth, MTConst.MinWindowHeight);
        mainWindow.Show();
    }

    private void OnEnable()
    {
        MTTools.MakeSureDirExists();

        mergeConfig = new MergeConfig();

        inspector = new MTInspector(this, mergeConfig);
        scene = new MTScene(this, mergeConfig);
        preview = new MTPreview(this, mergeConfig);

        inspector.OnEnable();

        LoadLocalSettings();
    }

    private void OnDisable()
    {
        inspector.OnDisable();
    }

    private void OnGUI()
    {
        inspector.OnDraw();

        BeginWindows();
        {
            scene.OnDraw();
        }
        EndWindows();

        preview.OnDraw();
    }

    public void LoadLocalSettings()
    {
        enableForceOverlay = EditorPrefs.GetBool(MTConst.ForceOverlayKey, false);
    }

    public void RemoveRectAt(int index)
    {
        mergeConfig.modelMergeConfigsList.RemoveAt(index);
        scene.RemoveRectAt(index);
    }

    private void OnDestroy()
    {
        MTCursorTools.Reset2NormalCursor();
    }
}
