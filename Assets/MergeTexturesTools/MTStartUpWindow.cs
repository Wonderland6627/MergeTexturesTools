using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[InitializeOnLoad]
public class MTStartUpWindow : EditorWindow
{
    private static Texture iconTex;

    private static GUIStyle imageStyle;
    private int width = 500;
    private int height = 450;

    private bool isReady = false;

    static MTStartUpWindow()
    {
        OpenStartUpPage();
    }

    [MenuItem("Wonderland6627/OpenStartUpPage", priority = ((int)MTLevel.Zero))]
    public static void OpenStartUpPage()
    {
        GetWindow<MTStartUpWindow>(true);
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("MergeTextureTools StartUp");
        maxSize = new Vector2(width, height);
        minSize = maxSize;
    }

    private void OnGUI()
    {
        InitInterface();

        GUI.Box(new Rect(0, 0, width, width / 2.4f), "", imageStyle);
    }

    private void InitInterface()
    {
        if (!isReady)
        {
            imageStyle = new GUIStyle();
            imageStyle.normal.background = Resources.Load<Texture2D>("StartUpBG");
            imageStyle.normal.textColor = Color.white;

            isReady = true;
        }
    }
}
