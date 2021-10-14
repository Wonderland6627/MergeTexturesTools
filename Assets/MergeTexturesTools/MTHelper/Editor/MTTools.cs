using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
//using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Text;

//b*[^:b#/]+.*$
public class MTTools
{
    public static string MergeTexturesToolsPathPrefix()
    {
        return Path.Combine(Application.dataPath , "MergeTexturesTools");
    }

    //贴图输出路径
    public static string TexOutputPath(string texName, TextureSuffix texSuffix)
    {
        return string.Format("{0}/Output/{1}.{2}", MergeTexturesToolsPathPrefix(), texName, texSuffix.ToString().ToLower());
    }

    public static void MakeSureDirExists()
    {
        bool created = false;

        string texOutDirPath = string.Format("{0}/Output", MergeTexturesToolsPathPrefix());
        if (!Directory.Exists(texOutDirPath))
        {
            Directory.CreateDirectory(texOutDirPath);
            created = true;
        }

        /*string configDirPath = Path.Combine(Application.dataPath, "Editor/MergeTexturesTools/Configs");
        if (!Directory.Exists(configDirPath))
        {
            Directory.CreateDirectory(configDirPath);
            created = true;
        }*/

        if (created)
        {
            Debug.Log("创建输出文件夹");
            AssetDatabase.Refresh();
        }
    }

    //public static void SaveMergeConfigInfoJson(MergeConfig mergeConfig)
    //{
    //    MTSystemTools.DisplayProgressBar(title: "检查合并配置");

    //    if (mergeConfig.modelMergeConfigsList == null)
    //    {
    //        Debug.Log("无需要合并的贴图配置信息");
    //        MTSystemTools.DisplayProgressBar(false);

    //        return;
    //    }

    //    if (mergeConfig.newTextureName == null)
    //    {
    //        Debug.Log("请输入新贴图名称");
    //        MTSystemTools.DisplayProgressBar(false);

    //        return;
    //    }

    //    string configName = mergeConfig.newTextureName;
    //    string configPath = ConfigJsonPath(configName);
    //    MTMergeConfigSerializationInfo serializationInfo = new MTMergeConfigSerializationInfo();

    //    serializationInfo.textureWidth = mergeConfig.textureWidth;
    //    serializationInfo.textureName = mergeConfig.newTextureName;
    //    serializationInfo.textureSuffix = (int)mergeConfig.newTextureSuffix;
    //    if (mergeConfig.modelMergeConfigsList.Count != 0)
    //    {
    //        var modelInfosList = new List<MTMergeConfigSerializationInfo.ModelMergeConfigSerializationInfo>();
    //        for (int i = 0; i < mergeConfig.modelMergeConfigsList.Count; i++)
    //        {
    //            var config = mergeConfig.modelMergeConfigsList[i];
    //            var info = new MTMergeConfigSerializationInfo.ModelMergeConfigSerializationInfo();

    //            /*if (config.gameObject != null)
    //            {
    //                string goAssetPath = AssetDatabase.GetAssetPath(config.gameObject);
    //                if (!string.IsNullOrEmpty(goAssetPath))
    //                {
    //                    info.gameObjectPath = goAssetPath;
    //                }
    //                else//是场景内物体
    //                {
    //                    info.gameObjectPath = "^" + config.gameObject.name;
    //                }
    //            }*/
    //            if (config.mesh != null)
    //            {
    //                info.meshPath = AssetDatabase.GetAssetPath(config.mesh);
    //            }
    //            if (config.mainTexture != null)
    //            {
    //                info.texturePath = AssetDatabase.GetAssetPath(config.mainTexture);
    //            }
    //            info.originX = (int)config.origin.x;
    //            info.originY = (int)config.origin.y;

    //            modelInfosList.Add(info);
    //        }

    //        serializationInfo.modelMergeConfigSerializationInfosList = modelInfosList;
    //    }

    //    string json = JsonConvert.SerializeObject(serializationInfo);
    //    File.WriteAllText(configPath, json, Encoding.UTF8);
    //    Debug.Log("保存配置：" + configPath);

    //    MTSystemTools.DisplayProgressBar(false);
    //    AssetDatabase.Refresh();
    //}

    //public static MTMergeConfigSerializationInfo LoadMergeConfigInfoJson(string jsonName)
    //{
    //    MTMergeConfigSerializationInfo serializationInfo = new MTMergeConfigSerializationInfo();



    //    return serializationInfo;
    //}
}

/// <summary>
/// 用于读写配置存档
/// </summary>
//public class MTMergeConfigSerializationInfo
//{
//    public string appointBGTexturePath;

//    public int textureWidth;
//    public string textureName;
//    public int textureSuffix;
//    public List<ModelMergeConfigSerializationInfo> modelMergeConfigSerializationInfosList;

//    public class ModelMergeConfigSerializationInfo
//    {
//        public string gameObjectPath;
//        public string meshPath;
//        public string texturePath;
//        public int originX;
//        public int originY;

//        public ModelMergeConfigSerializationInfo()
//        {
//            gameObjectPath = "";
//            meshPath = "";
//            texturePath = "";
//            originX = 0;
//            originY = 0;
//        }
//    }

//    public MTMergeConfigSerializationInfo()
//    {
//        appointBGTexturePath = "";
//        textureWidth = 0;
//        textureName = "";
//        textureSuffix = 0;
//        modelMergeConfigSerializationInfosList = new List<ModelMergeConfigSerializationInfo>();
//    }
//}

public class MTMergeTools
{
    public static void StartMerge(MergeConfig mergeConfig)
    {
        MTSystemTools.DisplayProgressBar(title: "检查合并配置");
        if (mergeConfig.textureWidth <= 0)
        {
            Debug.Log("新贴图边长不正确");
            MTSystemTools.DisplayProgressBar(false);

            return;
        }

        if (mergeConfig.modelMergeConfigsList == null || mergeConfig.modelMergeConfigsList.Count <= 0)
        {
            Debug.Log("无需要合并的贴图配置信息");
            MTSystemTools.DisplayProgressBar(false);

            return;
        }

        if (mergeConfig.newTextureName == null)
        {
            Debug.Log("请输入新贴图名称");
            MTSystemTools.DisplayProgressBar(false);

            return;
        }

        string outPath = MTTools.TexOutputPath(mergeConfig.newTextureName, mergeConfig.newTextureSuffix);
        string assetFullPath = Path.Combine(Directory.GetCurrentDirectory(), outPath);//要输出的绝对路径
        if (File.Exists(assetFullPath))
        {
            Debug.Log("这个名称的贴图已存在");

            if (!EditorPrefs.GetBool(MTConst.ForceOverlayKey))
            {
                MTSystemTools.DisplayProgressBar(false);

                return;
            }

            Debug.Log("强制覆盖已存在的贴图");
        }

        MergeTextures(mergeConfig);
        CreateMeshes(mergeConfig);

        MTSystemTools.DisplayProgressBar(false);
    }

    public static void MergeTextures(MergeConfig mergeConfig)
    {
        int edgeWidth = mergeConfig.textureWidth;
        var configsList = mergeConfig.modelMergeConfigsList;

        MTSystemTools.DisplayProgressBar(title: "生成透明背景图");
        Texture2D newTexture2D = new Texture2D(edgeWidth, edgeWidth, TextureFormat.ARGB32, true);
        Color[] colors = newTexture2D.GetPixels();
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.clear;
        }
        newTexture2D.SetPixels(colors);//设置背景图透明

        for (int i = 0; i < configsList.Count; i++)
        {
            MTSystemTools.DisplayProgressBar(title: "合并贴图");

            Vector2 origin = configsList[i].origin;
            Texture tex = configsList[i].mainTexture;

            bool preState = MTMergeTools.GetTextureReadEnable(tex);
            if (!preState)
            {
                MTMergeTools.SetTextureReadEnable(tex, true);
            }

            Texture2D tex2D = tex as Texture2D;
            newTexture2D.SetPixels((int)origin.x, ConvertUnityRect2TextureOriginY(origin, edgeWidth, tex2D.height), tex2D.width, tex2D.height, tex2D.GetPixels());
            MTSystemTools.DisplayProgressBar(title: "合并贴图", info: "填充配置贴图", progress: i / (float)configsList.Count);

            if (!preState)
            {
                MTMergeTools.SetTextureReadEnable(tex, preState);//开启可读写会使Texture所需内存量增加一倍 因此默认情况下禁用此属性
            }
        }

        string outPath = MTTools.TexOutputPath(mergeConfig.newTextureName, mergeConfig.newTextureSuffix);
        byte[] bs = newTexture2D.EncodeToPNG();
        File.WriteAllBytes(outPath, bs);
        AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
        MTSystemTools.DisplayProgressBar(title: "导出合并后的贴图");
        AssetDatabase.Refresh();

        Debug.Log("创建贴图:" + outPath);
    }

    public static void CreateMeshes(MergeConfig mergeConfig)
    {
        int edgeWidth = mergeConfig.textureWidth;
        var configsList = mergeConfig.modelMergeConfigsList;

        for (int i = 0; i < configsList.Count; i++)
        {
            MTSystemTools.DisplayProgressBar(title: "创建网格");

            Texture tex = configsList[i].mainTexture;
            Vector2 origin = configsList[i].origin;
            Vector2 size = new Vector2(tex.width, tex.height);
            Rect rect = new Rect(origin, size);

            Mesh mesh = configsList[i].mesh;
            Vector2[] uvs = mesh.uv;
            for (int j = 0; j < uvs.Length; j++)
            {
                uvs[j] = ConvertRectUV(edgeWidth, rect, uvs[j]);
            }

            Mesh newMesh = Mesh.Instantiate(mesh);
            newMesh.uv = uvs;//替换uv

            MTSystemTools.DisplayProgressBar(title: "创建网格", info: "生成新uv信息的网格", progress: i / (float)configsList.Count);

            string outPath = string.Format("{0}/{1}_{2}_Merge.asset", GetAssetDirectory(mesh), GetAssetName(mesh), tex.name);
            AssetDatabase.CreateAsset(newMesh, outPath);
            AssetDatabase.Refresh();

            Debug.Log("创建Mesh:" + outPath);
        }
    }

    /// <summary>
    /// 获取资源文件名称
    /// </summary>
    private static string GetAssetName<T>(T asset) where T : UnityEngine.Object
    {
        string name = "";
        string assetPath = AssetDatabase.GetAssetPath(asset);
        if (!string.IsNullOrEmpty(assetPath))
        {
            name = Path.GetFileNameWithoutExtension(assetPath) + "_" + asset.name;
        }
        else
        {
            name = DateTime.Now.ToString("hhmmss");
        }

        return name;
    }

    /// <summary>
    /// 获取资源所在的文件夹路径 并转化成以Assets开头的格式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="asset"></param>
    /// <returns></returns>
    private static string GetAssetDirectory<T>(T asset) where T : UnityEngine.Object
    {
        string dirPath = "";
        string assetPath = AssetDatabase.GetAssetPath(asset);
        if (!string.IsNullOrEmpty(assetPath) && !assetPath.Contains("Library"))//排除Unity自带的网格
        {
            dirPath = Path.GetDirectoryName(assetPath);
        }
        else
        {
            dirPath = "Assets";
        }

        return dirPath;
    }

    /// <summary>
    /// 将uv修改为指定贴图区域
    /// </summary>
    /// <param name="uvInRect">小图uv</param>
    /// <returns>新图uv</returns>
    private static Vector2 ConvertRectUV(int newTexWidth, Rect configRect, Vector2 uvInRect)
    {
        Vector2 newUV = new Vector2();
        newUV.x = (float)((configRect.position.x + uvInRect.x * configRect.width) / newTexWidth);//将小图的点坐标计算成大图点坐标
        newUV.y = (float)((ConvertUnityRect2TextureOriginY(configRect.position, newTexWidth, (int)configRect.height) + uvInRect.y * configRect.height) / newTexWidth);

        return newUV;
    }

    /// <summary>
    /// UnityRect的原点坐标y轴转化至Texture2D的原点坐标y轴
    /// UnityRect的原点在左上角，Texture2D的原点在左下角 Mesh的同Tex2D
    /// </summary>
    private static int ConvertUnityRect2TextureOriginY(Vector2 origin, int newTexWidth, int texHeight)
    {
        return newTexWidth - (int)origin.y - texHeight;
    }

    public static bool GetTextureReadEnable(Texture tex)
    {
        if (tex == null)
        {
            Debug.Log("贴图文件不存在");

            return false;
        }

        string texDataPath = AssetDatabase.GetAssetPath(tex);
        TextureImporter textureImporter = AssetImporter.GetAtPath(texDataPath) as TextureImporter;
        if (textureImporter == null)
        {
            Debug.Log(texDataPath + "不存在");

            return false;
        }

        return textureImporter.isReadable;
    }

    public static void SetTextureReadEnable(Texture tex, bool enable)
    {
        if (tex == null)
        {
            Debug.Log("贴图文件不存在");

            return;
        }

        string texDataPath = AssetDatabase.GetAssetPath(tex);
        TextureImporter textureImporter = AssetImporter.GetAtPath(texDataPath) as TextureImporter;
        if (textureImporter == null)
        {
            Debug.Log(texDataPath + "不存在");

            return;
        }

        textureImporter.isReadable = enable;
        AssetDatabase.ImportAsset(texDataPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
    }
}

public class MTPathTools
{
    public static string Underline2Slash(string str)
    {
        str = str.Replace("_", "/");

        return str;
    }

    public static string Slash2Underline(string str)
    {
        str = str.Replace("/", "_");
        str = str.Replace(Path.DirectorySeparatorChar, '_');

        return str;
    }
}

public class MTSystemTools
{
    public static void DisplayProgressBar(bool display = true, string title = "", string info = "", float progress = 0)
    {
        if (!display)
        {
            EditorUtility.ClearProgressBar();

            return;
        }

        EditorUtility.DisplayProgressBar(title, info, progress);
    }
}

public class MTCursorTools
{
    //光标资源加载函数  
    //fileName为加载路径下的.cur文件  
    [DllImport("User32.DLL")]
    public static extern IntPtr LoadCursorFromFile(string fileName);

    //设置系统指针函数（用hcur替换id定义的光标）  
    //hcur用于表示指针或句柄的特定类型，可以用LoadCursorFromFile函数加载一个路径下的.cur指针文件  
    //id是系统光标标识符，例：  
    //* OCR_APPSTARTING：标准箭头和小的沙漏；  
    //* OCR_NORAAC：标准箭头；  
    //* OCR_CROSS：交叉十字线光标；  
    //* OCR_HAND：手的形状（WindowsNT5.0和以后版本）；  
    //* OCR_HELP：箭头和向东标记；  
    //* OCR_IBEAM：I形梁；  
    //* OCR_NO：斜的圆；  
    //* OCR_SIZEALL：四个方位的箭头分别指向北、南、东、西；  
    //* OCR_SIZENESEW：双箭头分别指向东北和西南；  
    //* OCR_SIZENS：双箭头，分别指向北和南；  
    //* OCR_SIZENWSE：双箭头分别指向西北和东南；  
    //* OCR_SIZEWE：双箭头分别指向西和东；  
    [DllImport("User32.DLL")]
    public static extern bool SetSystemCursor(IntPtr hcur, uint id);
    public const uint OCR_NORMAL = 32512;

    //查询或设置的系统级参数函数  
    //* uiAction该参数指定要查询或设置的系统级参数，SPI_SETCURSORS：重置系统光标  
    //* fWinIni该参数指定在更新用户配置文件之后广播SPI_SENDWININICHANGE消息  

    [DllImport("User32.DLL")]
    public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);
    public const uint SPI_SETCURSORS = 87;
    public const uint SPIF_SENDWININICHANGE = 2;

    /// <summary>
    /// 把准备好的.cur指针资源放在StreamingAssets/Cursors下
    /// </summary>
    private static string CursorPath(string fileName)
    {
        return string.Format("{0}/Resources/Cursors/{1}.cur", MTTools.MergeTexturesToolsPathPrefix(), fileName);
        //return Path.Combine(Application.streamingAssetsPath + "/Cursors", fileName) + ".cur";
    }

    /// <summary>
    /// 恢复正常鼠标指针
    /// </summary>
    public static void Reset2NormalCursor()
    {
        SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_SENDWININICHANGE);
    }

    /// <summary>
    /// 设置拖拽移动样式的指针
    /// </summary>
    public static void SetDragCursor()
    {
        IntPtr hcur_drag = LoadCursorFromFile(CursorPath("aero_move"));
        SetSystemCursor(hcur_drag, OCR_NORMAL);
    }

    /// <summary>
    /// 设置修改尺寸样式的指针
    /// </summary>
    public static void SetResizeCursor()
    {
        IntPtr hcur_drag = LoadCursorFromFile(CursorPath("aero_ns"));
        SetSystemCursor(hcur_drag, OCR_NORMAL);
    }
}