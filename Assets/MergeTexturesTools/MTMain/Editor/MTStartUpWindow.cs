using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[InitializeOnLoad]
public class MTStartUpWindow : EditorWindow
{
    private static string Version = "v1.1";

    private static Texture2D iconTex;

    private static GUIStyle imageStyle;
    private static int width = 500;
    private static int height = 450;

    private static bool isReady = false;

    private static MTStartUpWindow targetWindow = null;

    /// <summary>
    /// 修改调试的时候记得先关闭窗口，否则会报一个很奇怪的错误
    /// 如果出现这个报错，关掉窗口，随便改一下代码，保存让Unity重新编译即可
    /// </summary>
    static MTStartUpWindow()
    {
        Debug.Log("static construct");
        Release();
        OpenStartUpPage();
    }

    private static void Release()
    {
        if (targetWindow)
        {
            Debug.Log("release");
            targetWindow.Close();
            isReady = false;
        }
    }

    [MenuItem("Wonderland6627/OpenStartUpPage", priority = ((int)MTLevel.Zero))]
    public static void OpenStartUpPage()
    {
        // targetWindow = GetWindow<MTStartUpWindow>(true);
    }

    private void OnEnable()
    {
        Debug.Log("OnEnable");

        titleContent = new GUIContent("MergeTextureTools StartUp");
        maxSize = new Vector2(width, height);
        minSize = maxSize;

        iconTex = Resources.Load<Texture2D>("StartUpBG");
    }

    private void OnGUI()
    {
        InitInterface();

        GUI.Box(new Rect(0, 0, width, width / 2.4f), "", imageStyle);
        GUI.Label(new Rect(0, 0, 200, 30), "Version : " + Version);

        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("H12425343453543435");
        }
        GUILayout.EndHorizontal();

        using (MTExtends.Horizontal horizontal = new MTExtends.Horizontal())
        {
            GUILayout.Label("153dw15ad1");
        }
    }

    private void InitInterface()
    {
        if (!isReady)
        {
            Debug.Log("init interface");

            imageStyle = new GUIStyle();
            imageStyle.normal.background = iconTex;
            imageStyle.normal.textColor = Color.white;

            isReady = true;
        }
    }

    private void OnDisable()
    {
        Debug.Log("OnDisable");

        isReady = false;
    }
}
