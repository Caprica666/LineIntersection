using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class LineMesh
{
    public int LineWidth = 10;
    private List<Vector3> mVertices;
    private List<Color> mColors;
    List<int> mIndices;
    private Mesh mMesh;

    public LineMesh(Mesh mesh)
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
        mMesh.SetIndices(mIndices, MeshTopology.Lines, 0);
        mMesh.RecalculateBounds();
    }

    public void Clear()
    {
        mMesh.Clear();
        mVertices.Clear();
        mIndices.Clear();
        mColors.Clear();
    }

    public int Add(LineSegment line, Color c)
    {
        int index = mIndices.Count;
        mVertices.Add(line.Start);
        mVertices.Add(line.End);
        line.VertexIndex = mIndices.Count;
        mIndices.Add(mIndices.Count);
        mIndices.Add(mIndices.Count);
        mColors.Add(c);
        mColors.Add(c);
        return index;
    }

    public void Update(int index, Color c)
    {
        mColors[index] = c;
        mColors[index + 1] = c;
    }

    public void Update(int index, LineSegment l)
    {
        mVertices[index] = l.Start;
        mVertices[index + 1] = l.End;
    }

    public int Add(LineSegment l)
    {
        return Add(l, new Color(Random.value, Random.value, Random.value, 1));
    }

}
