using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MTMonoOctree : MonoBehaviour
{
    public Octree tree;
    public bool showTree;

    public List<MeshRenderer> SelectMeshRenderersList;
    public Color mrLineColor;

    public void CreateOctree(Bounds bounds, List<MeshRenderer> mrsList = null)
    {
        tree = new Octree();
        tree.CreateTree(bounds, mrsList);
    }

    public void DrawOctree(OctNode node, Color color)
    {
        if (node != null)
        {
            Bounds bounds = node.nodeBounds;
            
            if (node.Count == 0)
            {
                Gizmos.color = color;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
            else
            {
                Gizmos.color = new Color(color.r, color.g, color.b, 0.35f);//节点中有物体的 半透明绘制
                Gizmos.DrawCube(bounds.center, bounds.size);
            }

            node.Execute((n) =>
            {
                DrawOctree(n, color);
            });
        }
    }

    public void DrawRendererBounds(List<MeshRenderer> mrsList, Color color)
    {
        SelectMeshRenderersList = mrsList;
        mrLineColor = color;
    }

    private void DrawRendererBounds()
    {
        if (SelectMeshRenderersList == null || SelectMeshRenderersList.Count == 0)
        {
            return;
        }

        Gizmos.color = mrLineColor;

        for (int i = 0; i < SelectMeshRenderersList.Count; i++)
        {
            if (SelectMeshRenderersList[i])
            {
                Bounds mrBounds = SelectMeshRenderersList[i].bounds;
                Gizmos.DrawWireCube(mrBounds.center, mrBounds.size);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (showTree)
        {
            var bounds = tree.treeRoot.nodeBounds;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            DrawOctree(tree.treeRoot, Color.yellow);
        }

        DrawRendererBounds();
    }
}
