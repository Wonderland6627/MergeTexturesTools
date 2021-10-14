using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MTPreview
{
    private GUIStyle style;

    private MergeConfig mergeConfig;
    private MTMainWindow mainWindow;

    private Texture previewTex;

    private Rect previewRect;
    private Texture previewBG;

    public MTPreview(MTMainWindow window, MergeConfig config)
    {
        mainWindow = window;
        mergeConfig = config;

        previewRect = new Rect(0, 0, MTConst.InspectorWidth, MTConst.InspectorWidth);
        if (previewBG == null)
        {
            previewBG = Resources.Load<Texture>("PreviewBG");
        }
    }

    public void OnDraw()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.window);
        }

        Rect areaRect = new Rect(mainWindow.position.width - MTConst.InspectorWidth, mainWindow.position.height - MTConst.InspectorWidth, MTConst.InspectorWidth, MTConst.InspectorWidth);
        GUILayout.BeginArea(areaRect, style);
        {
            EditorGUI.DrawPreviewTexture(previewRect, previewBG);

            UpdatePreviewTexture();
        }
        GUILayout.EndArea();
    }

    public void UpdatePreviewTexture()
    {
        int edgeWidth = mergeConfig.textureWidth;
        if (edgeWidth <= 0)
        {
            return;
        }

        if (mergeConfig.modelMergeConfigsList == null || mergeConfig.modelMergeConfigsList.Count <= 0)
        {
            return;
        }

        var configsList = mergeConfig.modelMergeConfigsList;

        for (int i = 0; i < configsList.Count; i++)
        {
            if (!configsList[i].IsValid())
            {
                continue;
            }

            DrawPreviewTexture(edgeWidth, configsList[i]);
        }
    }

    private void DrawPreviewTexture(int edgeWidth, MergeConfig.ModelMergeConfig mConfig)
    {
        Rect rect = new Rect();
        var config = ScaleWithSide(edgeWidth, mConfig, out rect);
        Texture texture = config.mainTexture;

        EditorGUI.DrawPreviewTexture(rect, texture);
    }

    /// <summary>
    /// 等比适配至预览窗口的尺寸
    /// </summary>
    private MergeConfig.ModelMergeConfig ScaleWithSide(int width, MergeConfig.ModelMergeConfig modelMergeConfig, out Rect newRect)
    {
        MergeConfig.ModelMergeConfig config = new MergeConfig.ModelMergeConfig();

        Vector2 origin = modelMergeConfig.origin;
        Texture texture = modelMergeConfig.mainTexture;

        //根据预览框边长和新贴图边长进行缩放
        float scale = previewRect.width / width;
        Vector2 newOrigin = new Vector2(origin.x * scale, origin.y * scale);
        Vector2 newSize = new Vector2(texture.width * scale, texture.height * scale);
        newRect = new Rect(newOrigin, newSize);

        config.origin = newOrigin;
        config.mainTexture = texture;

        return config;
    }
}
