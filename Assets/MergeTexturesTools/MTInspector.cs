using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEditor.SceneManagement;

public class MTInspector
{
    private GUIStyle style;

    private MergeConfig mergeConfig;
    private MTMainWindow mainWindow;

    public int subCount;

    private Vector2 configScroll;
    private bool showSettings = false;
    private bool showConfigs = true;

    private Dictionary<string, List<MeshRenderer>> goShaderTypesTable;//shadername, MRsList
    private int shaderTypeIdx;

    private MTConfigInfo MTConfigInfoAsset;//配置文件asset
    /// <summary>
    /// 材质球内贴图属性表
    /// Mat1 的 shader(Standard) - Albedo : Tex1
    ///                          - Metallic : Tex2
    ///                          - ...
    /// </summary>
    private Dictionary<Material, Dictionary<string, Texture>> MaterialTexturePropertiesTable;

    private MTMonoOctree monoOctree;
    private Bounds treeBounds;
    private bool cubeDivide;

    public MTInspector(MTMainWindow window, MergeConfig config)
    {
        mainWindow = window;
        mergeConfig = config;
    }

    public void OnEnable()
    {
        InitMTInspector();
        FilterShadersType();
    }

    public void OnDisable()
    {
        ClearOctree();
    }

    public void OnDraw()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box);
        }

        Rect areaRect = new Rect(mainWindow.position.width - MTConst.InspectorWidth, 0, MTConst.InspectorWidth, mainWindow.position.height - MTConst.InspectorWidth - MTConst.PreviewTopDragBarHeight);
        GUILayout.BeginArea(areaRect, style);
        {
            GUILayout.Label("MTInspector");

            DrawSettings();
            DrawDivideStrategy();
            DrawOctreeConfig();
            DrawMergeConfigs();

            if (mergeConfig.modelMergeConfigsList.Count < subCount)
            {
                mergeConfig.modelMergeConfigsList.Add(new MergeConfig.ModelMergeConfig());
            }

            configScroll = GUILayout.BeginScrollView(configScroll, GUILayout.MaxWidth(MTConst.InspectorWidth));
            {
                for (int i = 0; i < subCount; i++)
                {
                    GUILayout.Space(MTConst.Space);

                    GUILayout.BeginVertical(style);
                    {
                        int index = i;

                        GUILayout.Label(string.Format("Sub {0}      {1}", index, new string('=', 20)), GUILayout.MaxWidth(MTConst.InspectorWidth));

                        #region 物体填充
                        /*GUILayout.BeginHorizontal();
                        {
                            mergeConfig.modelMergeConfigsList[index].gameObject = (GameObject)EditorGUILayout.ObjectField(new GUIContent("物体:", GetAssetFieldTips(mergeConfig.modelMergeConfigsList[index].gameObject)), mergeConfig.modelMergeConfigsList[index].gameObject, typeof(GameObject));

                            if (mergeConfig.modelMergeConfigsList[index].gameObject != null)
                            {
                                if (GUILayout.Button("自动填充", EditorStyles.miniButton))
                                {
                                    mergeConfig.modelMergeConfigsList[index] = FieldModelMergeConfig(mergeConfig.modelMergeConfigsList[index].gameObject);
                                }
                            }
                        }
                        GUILayout.EndHorizontal();
                        ShowAssetPathLabel(mergeConfig.modelMergeConfigsList[index].gameObject);*/
                        #endregion

                        mergeConfig.modelMergeConfigsList[index].mesh = (Mesh)EditorGUILayout.ObjectField(new GUIContent("网格:", GetAssetFieldTips(mergeConfig.modelMergeConfigsList[index].mesh)), mergeConfig.modelMergeConfigsList[index].mesh, typeof(Mesh));
                        ShowAssetPathLabel(mergeConfig.modelMergeConfigsList[index].mesh);

                        mergeConfig.modelMergeConfigsList[index].material = (Material)EditorGUILayout.ObjectField(new GUIContent("材质球:", GetAssetFieldTips(mergeConfig.modelMergeConfigsList[index].material)), mergeConfig.modelMergeConfigsList[index].material, typeof(Material));
                        ShowAssetPathLabel(mergeConfig.modelMergeConfigsList[index].material);

                        mergeConfig.modelMergeConfigsList[index].mainTexture = (Texture)EditorGUILayout.ObjectField(new GUIContent("贴图:", GetAssetFieldTips(mergeConfig.modelMergeConfigsList[index].mainTexture)), mergeConfig.modelMergeConfigsList[index].mainTexture, typeof(Texture));
                        ShowAssetPathLabel(mergeConfig.modelMergeConfigsList[index].mainTexture);

                        GUILayout.BeginHorizontal();
                        {
                            GUI.enabled = false;
                            GUILayout.Label(new GUIContent("原点:", "贴图的原点坐标"));
                            mergeConfig.modelMergeConfigsList[index].origin = EditorGUILayout.Vector2Field("", mergeConfig.modelMergeConfigsList[index].origin);
                            GUI.enabled = true;
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("选中", EditorStyles.miniButtonLeft))
                            {
                                mainWindow.scene.FocusRectID = index;
                            }

                            if (GUILayout.Button("移除", EditorStyles.miniButtonRight))
                            {
                                mainWindow.RemoveRectAt(index);

                                subCount--;
                                subCount = Mathf.Clamp(subCount, 0, int.MaxValue);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(MTConst.Space);

            if (subCount > 0)
            {
                GUILayout.BeginHorizontal();
                {
                    if (MTConfigInfoAsset == null)//如果没选中说明不是修改 是新建
                    {
                        if (GUILayout.Button("新建配置"))
                        {
                            CreateMTConfigInfoAsset();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("保存修改"))
                        {
                            UpdateMTConfigInfoAsset();
                        }
                    }

                    if (GUILayout.Button("合并贴图"))
                    {
                        MTMergeTools.StartMerge(mergeConfig);
                    }
                }
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.EndArea();

        RemoveSubConfig();
    }

    /// <summary>
    /// 绘制合并配置
    /// </summary>
    private void DrawMergeConfigs()
    {
        showConfigs = EditorGUILayout.Foldout(showConfigs, "合并配置");
        if (!showConfigs)
        {
            return;
        }

        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("一键加入选中物体", EditorStyles.miniButtonMid))
                {
                    var gos = Selection.gameObjects;
                    List<MeshRenderer> mrs = new List<MeshRenderer>();
                    for (int i = 0; i < gos.Length; i++)
                    {
                        MeshRenderer mr = gos[i].GetComponent<MeshRenderer>();
                        if (mr)
                        {
                            mrs.Add(mr);
                        }
                    }

                    AddGosIntoMergeConfigsList(mrs.ToArray());
                }

                if (GUILayout.Button("清空配置列表", EditorStyles.miniButtonRight))
                {
                    ClearMergeConfigsList();
                }
            }
            GUILayout.EndHorizontal();

            mainWindow.autoMatchWidth = GUILayout.Toggle(mainWindow.autoMatchWidth, "自动拉伸边长(2^n)");
            GUILayout.BeginHorizontal();
            {
                if (mainWindow.autoMatchWidth)//自动适配时禁用边长输入
                {
                    GUI.enabled = false;
                }
                mergeConfig.textureWidth = EditorGUILayout.IntField("新贴图边长", mergeConfig.textureWidth);
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                subCount = EditorGUILayout.IntField("需要合并的贴图数量", subCount);
                if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(25)))
                {
                    subCount++;
                }
                subCount = Mathf.Clamp(subCount, 0, int.MaxValue);
            }
            GUILayout.EndHorizontal();

            if (subCount > 0)
            {
                mergeConfig.newTextureName = EditorGUILayout.TextField("新贴图名称:", mergeConfig.newTextureName);
                mergeConfig.newTextureSuffix = (TextureSuffix)EditorGUILayout.EnumPopup("新贴图格式:", mergeConfig.newTextureSuffix);
                mainWindow.enableForceOverlay = GUILayout.Toggle(mainWindow.enableForceOverlay, "重名文件强制覆盖");
                EditorPrefs.SetBool(MTConst.ForceOverlayKey, mainWindow.enableForceOverlay);

                if (GUILayout.Button("大图在下 重新排列", EditorStyles.miniButton))
                {
                    SortMergeConfigs();
                }

                if (GUILayout.Button("自动排布", EditorStyles.miniButton))
                {
                    AutoLayout();
                }
            }
        }
    }

    /// <summary>
    /// 绘制分类策略
    /// </summary>
    private void DrawDivideStrategy()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("修改配置文件:");
            MTConfigInfoAsset = (MTConfigInfo)EditorGUILayout.ObjectField(MTConfigInfoAsset, typeof(MTConfigInfo));

            if (MTConfigInfoAsset)
            {
                if (GUILayout.Button("加载"))
                {
                    LoadConfig();
                }
            }

            if (GUILayout.Button("清除"))
            {
                MTConfigInfoAsset = null;
            }
        }
        GUILayout.EndHorizontal();

        if (MTConfigInfoAsset != null)
        {
            GUI.enabled = false;
        }
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("场景内Shader筛选:");
            shaderTypeIdx = EditorGUILayout.Popup(shaderTypeIdx, goShaderTypesTable.Keys.ToArray());
            if (GUILayout.Button("选中"))//根据shader类型选中gos
            {
                ClearMergeConfigsList();

                List<MeshRenderer> mrsList = GetCurrentShaderMeshRenderers();
                AddGosIntoMergeConfigsList(mrsList.ToArray());

                Selection.objects = mrsList.ToArray();
            }
        }
        GUILayout.EndHorizontal();
        GUI.enabled = true;
    }

    private List<MeshRenderer> GetCurrentShaderMeshRenderers()
    {
        string key = goShaderTypesTable.Keys.ToArray()[shaderTypeIdx];
        var mrsList = goShaderTypesTable[key];

        return mrsList;
    }

    private void DrawOctreeConfig()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("划分空间最小尺寸:");
            Octree.MinSize = EditorGUILayout.Vector3Field("", Octree.MinSize);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
            cubeDivide = GUILayout.Toggle(cubeDivide, "正方体划分");
            if (GUILayout.Button("根据所选Shader生成八叉树", EditorStyles.miniButtonMid))
            {
                GenerateOctree();
            }

            if (GUILayout.Button("销毁八叉树", EditorStyles.miniButtonMid))
            {
                ClearOctree();
            }
        }
        GUILayout.EndHorizontal();

        if (monoOctree == null)
        {
            return;
        }

        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("八叉树对象:");
            GUI.enabled = false;
            monoOctree = EditorGUILayout.ObjectField(monoOctree, typeof(MTMonoOctree)) as MTMonoOctree;
            GUI.enabled = true;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("八叉树包围盒:");
            GUI.enabled = false;
            treeBounds = EditorGUILayout.BoundsField(treeBounds);
            GUI.enabled = true;
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 生成八叉树
    /// 生成规则：
    /// 1.根据所选Shader类型，筛选场景内符合条件的物体
    /// 2.创建Bounds将所有物体包裹，并取其最长边，将Bounds修改为一个正方体（目的是为了与空间划分最小尺寸做比较）
    /// 3.划分Bounds，直到达到规定最小空间
    /// </summary>
    private void GenerateOctree()
    {
        monoOctree = GameObject.FindObjectOfType<MTMonoOctree>();
        if (monoOctree == null)
        {
            GameObject go = new GameObject("MTMonoOctree");
            go.transform.position = Vector3.zero;
            monoOctree = go.AddComponent<MTMonoOctree>();
        }

        var mrsList = GetCurrentShaderMeshRenderers();
        treeBounds = GenerateSceneBounds(mrsList);
        monoOctree.CreateOctree(treeBounds, mrsList);
        monoOctree.DrawRendererBounds(mrsList, Color.blue);
        monoOctree.showTree = true;
    }

    /// <summary>
    /// 根据场景创建包围盒
    /// </summary>
    private Bounds GenerateSceneBounds(List<MeshRenderer> mrsList)
    {
        Bounds bounds = new Bounds();

        if (mrsList != null || mrsList.Count > 0)
        {
            bounds = mrsList[0].bounds;
            for (int i = 0; i < mrsList.Count; i++)
            {
                bounds.Encapsulate(mrsList[i].bounds);
            }
        }

        if (cubeDivide)
        {
            float maxWidth = Mathf.Max(new float[] { bounds.size.x, bounds.size.y, bounds.size.z });
            bounds = new Bounds(bounds.center, maxWidth * Vector3.one);
        }

        return bounds;
    }

    private void ClearOctree()
    {
        if (monoOctree)
        {
            monoOctree.showTree = false;
            GameObject.DestroyImmediate(monoOctree.gameObject);
            monoOctree = null;
        }
    }

    /// <summary>
    /// 绘制面板设置
    /// </summary>
    private void DrawSettings()
    {
        showSettings = EditorGUILayout.Foldout(showSettings, "面板设置");
        if (!showSettings)
        {
            return;
        }

        GUILayout.BeginVertical();
        {
            mainWindow.enableDragDelay = GUILayout.Toggle(mainWindow.enableDragDelay, "是否启用拖拽辅助");
            if (mainWindow.enableDragDelay)
            {
                mainWindow.dragDelaySens = EditorGUILayout.IntField("拖拽辅助灵敏度", mainWindow.dragDelaySens);
                mainWindow.dragDelaySens = Mathf.Clamp(mainWindow.dragDelaySens, 0, 50);
            }

            mainWindow.showPreviewPowerPoint = GUILayout.Toggle(mainWindow.showPreviewPowerPoint, "是否显示预览图次幂点");
            mainWindow.showPreviewTexInfo = GUILayout.Toggle(mainWindow.showPreviewTexInfo, "是否显示预览图信息");
            mainWindow.showUVLines = GUILayout.Toggle(mainWindow.showUVLines, "是否显示UV");
        }
        GUILayout.EndVertical();
    }

    private void InitMTInspector()
    {
        treeBounds = new Bounds();
    }

    /// <summary>
    /// 过滤场景中的shader类型 刷新shader/GosList字典
    /// </summary>
    private void FilterShadersType()
    {
        goShaderTypesTable = new Dictionary<string, List<MeshRenderer>>();
        var renderers = GameObject.FindObjectsOfType<MeshRenderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer mr = renderers[i];
            if (mr)
            {
                Material mat = mr.sharedMaterial;
                if (mat)
                {
                    Shader shader = mat.shader;
                    if (shader)
                    {
                        string shaderName = shader.name;
                        if (!goShaderTypesTable.ContainsKey(shaderName))
                        {
                            goShaderTypesTable[shaderName] = new List<MeshRenderer>();
                        }

                        goShaderTypesTable[shaderName].Add(mr);
                    }
                }
            }
        }
    }

    private void AddGosIntoMergeConfigsList(MeshRenderer[] gos)
    {
        if (gos != null && gos.Length > 0)
        {
            for (int i = 0; i < gos.Length; i++)
            {
                var config = FieldModelMergeConfig(gos[i]);

                if (!mergeConfig.modelMergeConfigsList.Contains(config, new IModelMergeConfigComparer()))//同样的配置只能存在一个
                {
                    mergeConfig.modelMergeConfigsList.Add(config);
                    subCount++;
                }
            }
        }
    }

    /// <summary>
    /// 加载配置信息 加入列表 初始化材质贴图表
    /// </summary>
    private void LoadConfig()
    {
        if (MTConfigInfoAsset == null)
        {
            return;
        }

        ClearMergeConfigsList();

        mergeConfig.modelMergeConfigsList = MTConfigInfoAsset.mergeConfigsList;
        subCount = mergeConfig.modelMergeConfigsList.Count;

        InitMaterialTexturePropTable();
    }

    private void InitMaterialTexturePropTable()
    {
        if (MTConfigInfoAsset == null)
        {
            return;
        }

        MaterialTexturePropertiesTable = new Dictionary<Material, Dictionary<string, Texture>>();
        var list = MTConfigInfoAsset.mergeConfigsList;
        for (int i = 0; i < list.Count; i++)
        {
            var config = list[i];
            if (config != null)
            {
                Material mat = config.material;
                if (mat)
                {
                    Shader shader = mat.shader;

                    if (!MaterialTexturePropertiesTable.ContainsKey(mat))
                    {
                        MaterialTexturePropertiesTable[mat] = new Dictionary<string, Texture>();
                    }

                    string[] texPropNames = mat.GetTexturePropertyNames();//获取所有的贴图属性名称
                    for (int j = 0; j < texPropNames.Length; j++)
                    {
                        Texture tex = mat.GetTexture(texPropNames[j]);
                        if (tex)
                        {
                            MaterialTexturePropertiesTable[mat].Add(texPropNames[j], tex);
                        }
                    }
                }
            }
        }
    }

    private void ClearMergeConfigsList()
    {
        subCount = 0;
        mergeConfig.modelMergeConfigsList = new List<MergeConfig.ModelMergeConfig>();
    }

    private void CreateMTConfigInfoAsset()
    {
        if (goShaderTypesTable == null || goShaderTypesTable.Count == 0)
        {
            Debug.Log("Table is null");

            return;
        }

        var shadersList = goShaderTypesTable.Keys.ToArray();
        string shaderName = shadersList[shaderTypeIdx];

        MTConfigInfo configInfo = MTConfigInfo.CreateMTConfigInfoAsset(shaderName);
        configInfo.mergeConfigsList = new List<MergeConfig.ModelMergeConfig>(mergeConfig.modelMergeConfigsList);//与场景内的操作分离
        configInfo.UpdateMTConfigInfoAsset();

        MTConfigInfoAsset = configInfo;
    }

    private void UpdateMTConfigInfoAsset()
    {
        if (MTConfigInfoAsset == null)
        {
            return;
        }

        MTConfigInfoAsset.UpdateMTConfigInfoAsset();
    }

    /// <summary>
    /// 根据传入物体获取配置
    /// </summary>
    private MergeConfig.ModelMergeConfig FieldModelMergeConfig(MeshRenderer target)
    {
        MergeConfig.ModelMergeConfig config = new MergeConfig.ModelMergeConfig();

        if (target == null)
        {
            return config;
        }

        //config.gameObject = target;

        MeshFilter filter = target.GetComponent<MeshFilter>();
        if (filter)
        {
            config.mesh = filter.sharedMesh;
        }

        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        if (renderer)
        {
            Material mat = renderer.sharedMaterial;//一定要传入的是sharedMaterial
            if (mat)
            {
                config.material = mat;

                if (mat.mainTexture)
                {
                    config.mainTexture = mat.mainTexture;
                }
            }
        }

        return config;
    }

    /// <summary>
    /// 对配置按照贴图大小进行排序，大图靠列表前，才能显示在最下面
    /// </summary>
    private void SortMergeConfigs()
    {
        mergeConfig.modelMergeConfigsList.Sort(new ITextureWidthDescendingOrder());
        mainWindow.FocusID = -1;
    }

    /// <summary>
    /// 移除多余配置对
    /// </summary>
    private void RemoveSubConfig()
    {
        while (mergeConfig.modelMergeConfigsList.Count > subCount)
        {
            int index = mergeConfig.modelMergeConfigsList.Count - 1;
            mergeConfig.modelMergeConfigsList.RemoveAt(index);
        }
    }

    /// <summary>
    /// 平面装箱 自动排布
    /// 排布规则：↖大
    /// </summary>
    private void TexuturesRectPacking()
    {
        SortMergeConfigs();//首先排序

        int count = mergeConfig.modelMergeConfigsList.Count;
        var maximumWidth = mergeConfig.modelMergeConfigsList[0].mainTexture.width;//最宽的贴图边长
        if (!mainWindow.autoMatchWidth)
        {
            mergeConfig.textureWidth = maximumWidth;
        }
    }

    /// <summary>
    /// 自动排布
    /// </summary>
    public void AutoLayout()
    {
        Texture2D layoutTex = new Texture2D(1, 1, TextureFormat.Alpha8, false);
        List<Texture2D> tex2DList = new List<Texture2D>();
        List<string> errorTexsList = new List<string>();

        for (int i = 0; i < mergeConfig.modelMergeConfigsList.Count; i++)
        {
            var config = mergeConfig.modelMergeConfigsList[i];
            if (config.mainTexture)
            {
                bool preState = MTMergeTools.GetTextureReadEnable(config.mainTexture);
                if (!preState)
                {
                    errorTexsList.Add(AssetDatabase.GetAssetPath(config.mainTexture));

                    continue;
                }

                tex2DList.Add((Texture2D)config.mainTexture);
            }
        }

        if (errorTexsList.Count > 0)
        {
            for (int i = 0; i < errorTexsList.Count; i++)
            {
                Debug.LogErrorFormat("{0} 贴图资源：{1} 的可读写性为禁用，请将其启用", i, errorTexsList[i]);
            }

            return;
        }
        
        Rect[] layoutRects = layoutTex.PackTextures(tex2DList.ToArray(), 0);
        for (int i = 0; i < mergeConfig.modelMergeConfigsList.Count; i++)
        {
            mergeConfig.modelMergeConfigsList[i].origin = layoutRects[i].position * GetMinRectAreaPo2Side();
        }
    }

    /// <summary>
    /// 获取所有贴图的面积，根据面积计算最小合并边长
    /// </summary>
    private int GetMinRectAreaPo2Side()
    {
        float area = 0;

        for (int i = 0; i < mergeConfig.modelMergeConfigsList.Count; i++)
        {
            var config = mergeConfig.modelMergeConfigsList[i];
            if (config.mainTexture)
            {
                area += config.mainTexture.width * config.mainTexture.height;
            }
        }

        if (area != 0)
        {
            area = Mathf.Sqrt(area);
            area = Mathf.NextPowerOfTwo((int)(area + 1));
        }
        //Debug.Log("Side:" + area);

        return (int)area;
    }

    private GUIStyle labelStyle;
    /// <summary>
    /// 显示asset的Data路径
    /// </summary>
    private void ShowAssetPathLabel<T>(T asset) where T : Object
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle();
            labelStyle.fontSize = 8;
            labelStyle.wordWrap = true;//文本自动换行
        }

        string label = GetAssetFieldTips<T>(asset);
        if (string.IsNullOrEmpty(label))//资源路径为空说明是场景内的
        {
            label = "场景内物体";
        }
        else
        {
            label = string.Format("Path:{0}", label);
        }

        GUILayout.Label(label, labelStyle, GUILayout.MaxWidth(MTConst.InspectorWidth));
    }

    private string GetAssetFieldTips<T>(T asset) where T : Object
    {
        string tips = "";

        if (asset == null)
        {
            tips = string.Format("请选择{0}类型的asset", typeof(T).ToString());
        }
        else
        {
            tips = AssetDatabase.GetAssetPath(asset);
        }

        return tips;
    }
}

/// <summary>
/// 根据贴图宽度按降序排列
/// </summary>
public class ITextureWidthDescendingOrder : IComparer<MergeConfig.ModelMergeConfig>
{
    public int Compare(MergeConfig.ModelMergeConfig x, MergeConfig.ModelMergeConfig y)
    {
        return CompareTex(x.mainTexture, y.mainTexture);
    }

    public int CompareTex(Texture x, Texture y)
    {
        return y.width.CompareTo(x.width);
    }
}

/// <summary>
/// 根据配置中材质和网格是否相同进行对比
/// </summary>
public class IModelMergeConfigComparer : IEqualityComparer<MergeConfig.ModelMergeConfig>
{
    public bool Equals(MergeConfig.ModelMergeConfig x, MergeConfig.ModelMergeConfig y)
    {
        bool equals = true;

        if (!x.mesh.Equals(y.mesh) && !x.material.Equals(y.material) && !x.mainTexture.Equals(y.mainTexture))
        {
            equals = false;
        }

        return equals;
    }

    public int GetHashCode(MergeConfig.ModelMergeConfig obj)
    {
        return obj.GetHashCode();
    }
}