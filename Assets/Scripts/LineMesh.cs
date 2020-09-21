using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class LineMesh
{
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
        Color c = mColors[index];

        c = new Color(c.r, c.g, c.b, 1);
        mVertices[index] = l.Start;
        mVertices[index + 1] = l.End;
        mColors[index] = c;
        mColors[index + 1] = c;       
    }

    public void Recolor()
    {
        for (int i = 0; i <= mColors.Count - 3; i += 3)
        {
            Color c = new Color(Random.value * 0.8f,
                                Random.value * 0.8f,
                                Random.value * 0.8f, 1);
            mColors[i] = c;
            mColors[i + 1] = c;
            mColors[i + 2] = c;
        }
    }

    public int Add(LineSegment l)
    {
        return Add(l, new Color(Random.value * 0.8f,
                                Random.value * 0.8f,
                                Random.value * 0.8f, 1));
    }

}
