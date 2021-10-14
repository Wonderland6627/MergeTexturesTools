using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ScriptableObject类型特点：
/// 
/// 1.必须使用CreateInstance实例化
/// 2.以.asset资源文件形式保存在硬盘
/// 3.属性必须也是在硬盘上实际存在的文件，如果只是在内存中，会显示Type missmatch，
///   虽然可以双击定位，但是和Temp文件夹一样，关闭就清理了
/// 4.更新时首先SetDirty，再SaveAsset
/// 5.和Unity中其他可序列化文件一样，它也可以使用CustomEditor来扩展在Inspector中的
///   显示效果，并且只支持一维数据结构的可视化序列化，所以可以用多个List来代替Dictionary
/// </summary>

/// <summary>
/// MTConfigInfo的使用方法：
/// 1.首次创建时，使用MTConfigInfo.CreateMTConfigInfoAsset(path)，并会调用OnCreate()方法
/// 2.修改时，调用UpdateMTConfigInfoAsset()
/// 3.加载时，调用OnLoad()来初始化
/// </summary>

[CreateAssetMenu]
public class MTConfigInfo : ScriptableObject
{
    [Header("该asset资源工程路径")]
    public string assetPath;

    [Header("该配置的Shader名称")]
    public string shaderName;

    [Header("所属场景名称")]
    public string dependSceneName;

    [Header("所属场景工程路径")]
    public string dependScenePath;

    [Header("合并配置列表")]
    public List<MergeConfig.ModelMergeConfig> mergeConfigsList;

    /// <summary>
    /// 创建时
    /// </summary>
    public void OnCreate(string shaderName)
    {
        this.shaderName = shaderName;
    }

    /// <summary>
    /// 加载时初始化
    /// </summary>
    public void OnLoad()
    {

    }

    public static MTConfigInfo CreateMTConfigInfoAsset(string shaderName)
    {
        MTConfigInfo configInfo = ScriptableObject.CreateInstance<MTConfigInfo>();
        shaderName = MTPathTools.Slash2Underline(shaderName);
        configInfo.OnCreate(shaderName);

        Scene activeScene = SceneManager.GetActiveScene();
        string sceneDirPath = Path.GetDirectoryName(activeScene.path);
        string configDirPath = string.Format("{0}/MTConfigInfos_{1}", sceneDirPath, activeScene.name);
        if (!Directory.Exists(configDirPath))
        {
            Directory.CreateDirectory(configDirPath);
        }

        configInfo.dependSceneName = activeScene.name;
        configInfo.dependScenePath = activeScene.path;

        string configInfoPathPrefix = string.Format("{0}/MT_{1}_{2}", configDirPath, activeScene.name, shaderName);
        string namePrefix = Path.GetFileName(configInfoPathPrefix);

        int index = 0;//场景_shader类型 的第几个配置文件
        DirectoryInfo directory = new DirectoryInfo(configDirPath);
        if (directory != null)
        {
            foreach (var file in directory.GetFiles())
            {
                if (file.FullName.EndsWith(".asset") && file.FullName.Contains(namePrefix))
                {
                    index++;
                }
            }
        }

        string configSavePath = string.Format("{0}_{1}.asset", configInfoPathPrefix, index);

        AssetDatabase.CreateAsset(configInfo, configSavePath);
        AssetDatabase.Refresh();

        return configInfo;
    }

    /// <summary>
    /// 更新资源 
    /// </summary>
    public void UpdateMTConfigInfoAsset()
    {
        assetPath = AssetDatabase.GetAssetPath(this);

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("更新并保存配置：" + assetPath);
    }
}