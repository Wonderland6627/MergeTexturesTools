using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Octree
{
    public OctNode treeRoot;
    public int depth;

    public static Vector3 MinSize = Vector3.one * 10;

    public void CreateTree(Bounds rootBounds, List<MeshRenderer> meshRenderers = null)
    {
        treeRoot = new OctNode();
        treeRoot.nodeBounds = rootBounds;
        treeRoot.Divide();
        treeRoot.nodeType = NodeType.Root;

        if (meshRenderers != null)
        {
            IEnumerator<MeshRenderer> enumerator = meshRenderers.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var mr = enumerator.Current;
                treeRoot.Append(mr);
            }
            enumerator.Dispose();
        }
    }

    public void RefreshTree(Bounds rootBounds, List<MeshRenderer> meshRenderers)
    {

    }
}

/// <summary>
/// 俯视象限逆时针顺序
/// </summary>
public enum NodeType : int
{
    Root = -1,

    Quadrant1Top = 0,
    Quadrant2Top,
    Quadrant3Top,
    Quadrant4Top,

    Quadrant1Bottom,
    Quadrant2Bottom,
    Quadrant3Bottom,
    Quadrant4Bottom = 7,
}

[System.Serializable]
public class OctNode
{
    public int guid;
    public int depth;
    public int index;
    public NodeType nodeType;
    public Bounds nodeBounds;

    public Dictionary<int, OctNode> octNodeMap;
    public List<OctNode> octNodesList;
    public List<MeshRenderer> objRendersList;

    public int Count
    {
        get
        {
            return objRendersList.Count;
        }
    }

    public OctNode()
    {
        guid = GetHashCode();
        octNodeMap = new Dictionary<int, OctNode>();
        objRendersList = new List<MeshRenderer>();
    }

    public bool Append(MeshRenderer mr)
    {
        Divide();

        if (nodeBounds.Contains(mr.bounds.max) && nodeBounds.Contains(mr.bounds.min))
        {
            OctNode subNode = GetContainedNode(mr.bounds);
            if (subNode != null)
            {
                Divide();
                subNode.Append(mr);
            }
            else
            {
                objRendersList.Add(mr);

                Debug.Log(guid + " add " + mr.gameObject.name + " " + nodeType.ToString());
                Debug.Log(guid + " count " + objRendersList.Count);

                octNodesList = octNodeMap.Values.ToList();
            }
        }

        return true;
    }

    public void Divide()
    {
        if (octNodeMap.Count > 0)
        {
            return;
        }

        if (nodeBounds.size.x < Octree.MinSize.x || nodeBounds.size.y < Octree.MinSize.y || nodeBounds.size.z < Octree.MinSize.z)
        {
            return;
        }

        DivideOctNode();
    }

    public void DivideOctNode()
    {
        octNodeMap.Clear();

        Vector3 subSize = nodeBounds.size * 0.5f;

        for (int i = 0; i < 8; i++)
        {
            Vector3 subCenter = nodeBounds.center * 0.5f + GetNodeCenter((NodeType)i) * 0.5f;
            Bounds subBounds = new Bounds(subCenter, subSize);

            OctNode subNode = new OctNode();
            subNode.nodeBounds = subBounds;
            subNode.index = i;
            subNode.nodeType = (NodeType)i;

            this.octNodeMap[i] = subNode;
        }

        octNodesList = octNodeMap.Values.ToList();
    }

    /// <summary>
    /// 是否存在能够装下bounds大小的Node
    /// </summary>
    /// <param name="bounds"></param>
    /// <returns></returns>
    private OctNode GetContainedNode(Bounds bounds)
    {
        for (int i = 0; i < 8; i++)
        {
            OctNode subNode = null;

            if (octNodeMap.TryGetValue(i, out subNode))
            {
                if ((subNode.nodeBounds.Contains(bounds.max) && subNode.nodeBounds.Contains(bounds.min)))
                {
                    return subNode;
                }
            }
        }

        return null;
    }

    private Vector3 GetNodeCenter(NodeType nodeType)
    {
        switch (nodeType)
        {
            case NodeType.Quadrant1Top:
                return nodeBounds.max;
            case NodeType.Quadrant2Top:
                return new Vector3(nodeBounds.min.x, nodeBounds.max.y, nodeBounds.max.z);
            case NodeType.Quadrant3Top:
                return new Vector3(nodeBounds.min.x, nodeBounds.max.y, nodeBounds.min.z);
            case NodeType.Quadrant4Top:
                return new Vector3(nodeBounds.max.x, nodeBounds.max.y, nodeBounds.min.z);

            case NodeType.Quadrant1Bottom:
                return new Vector3(nodeBounds.max.x, nodeBounds.min.y, nodeBounds.max.z);
            case NodeType.Quadrant2Bottom:
                return new Vector3(nodeBounds.min.x, nodeBounds.min.y, nodeBounds.max.z);
            case NodeType.Quadrant3Bottom:
                return nodeBounds.min;
            case NodeType.Quadrant4Bottom:
                return new Vector3(nodeBounds.max.x, nodeBounds.min.y, nodeBounds.min.z);
        }

        return Vector3.zero;
    }

    public void ClearNode()
    {
        
    }

    public void Execute(Action<OctNode> callBack)
    {
        for (int i = 0; i < 8; i++)
        {
            if (octNodeMap.ContainsKey(i))
            {
                if (callBack != null)
                {
                    callBack.Invoke(octNodeMap[i]);
                }
            }
        }
    }
}
