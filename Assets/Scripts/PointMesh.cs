using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PointMesh
{
    public float PointSize = 10;
    private List<Vector3> mVertices;
    private List<Color> mColors;
    List<int> mIndices;
    private Mesh mMesh;

    public PointMesh(Mesh mesh)
    {
        mMesh = mesh;
        mVertices = new List<Vector3>();
        mIndices = new List<int>();
        mColors = new List<Color>();
        mMesh.Clear();
    }

   public int VertexCount
    {
        get { return mVertices.Count;  }
    }

    public void Display()
    {
        mMesh.SetVertices(mVertices);
        mMesh.SetColors(mColors);
        mMesh.SetTriangles(mIndices, 0);
        mMesh.RecalculateBounds();
    }

    public void Clear()
    {
        mMesh.Clear();
        mVertices.Clear();
        mIndices.Clear();
        mColors.Clear();
    }

    public int Add(Vector3 v, Color c)
    {
        int index = mIndices.Count;
        mVertices.Add(new Vector3(v.x, v.y + PointSize, 0));
        mVertices.Add(new Vector3(v.x + PointSize, v.y - PointSize, 0));
        mVertices.Add(new Vector3(v.x - PointSize, v.y - PointSize, 0));
        mIndices.Add(mIndices.Count);
        mIndices.Add(mIndices.Count);
        mIndices.Add(mIndices.Count);
        mColors.Add(c);
        mColors.Add(c);
        mColors.Add(c);
        return index;
    }

    public int Add(Vector3 v)
    {
        return Add(v, new Color(Random.value, Random.value, Random.value, 1));
    }

    public void MakeMesh(List<Vector3> pointlist)
    {
        mMesh.Clear();
        mIndices.Clear();
        mVertices.Clear();
        mColors.Clear();
        foreach (Vector3 v in pointlist)
        {
            Add(v);
        }
        Display();
    }
}
