using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MTConfigInfo))]
public class MTConfigInfoEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        MTConfigInfo configInfo = target as MTConfigInfo;

        GUILayout.Space(20);

        if (GUILayout.Button("更新并保存配置"))
        {
            configInfo.UpdateMTConfigInfoAsset();
        }
    }
}
