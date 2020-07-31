using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.Collections;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class LineIntersection : MonoBehaviour
{
    public int NumLines = 10;
    public float SCALE = 100;
    public float ExecutionTime;
    public bool New;
    public bool PlaneSweep;

    private LineGroup mLines;
    private LineMesh mLinesToRender;
    private PointMesh mIntersections;
    private LineRenderer mCurLine = null;

    public void Awake()
    {
        GameObject intersections = GameObject.Find("Intersections");
        MeshFilter mf = intersections.GetComponent<MeshFilter>() as MeshFilter;
        mIntersections = new PointMesh(mf.mesh);
        mIntersections.PointSize = 0.2f;
        mCurLine = gameObject.AddComponent<LineRenderer>() as LineRenderer;
        mCurLine.useWorldSpace = false;
        mCurLine.widthMultiplier = 10;
        mCurLine.material = new Material(Shader.Find("Unlit/VertexColorUnlit"));
        mLinesToRender = new LineMesh(gameObject.GetComponent<MeshFilter>().mesh);
        mLines = new LineGroup(mLinesToRender, mIntersections);
    }

    private void Update()
    {
        if (New)
        {
            New = false;
            NewLines(NumLines);
        }
        else if (PlaneSweep)
        {
            PlaneSweep = false;
            StartCoroutine(FindIntersections());
        }
    }

    public LineMesh LinesToRender
    {
        get { return mLinesToRender; }
        set { mLinesToRender = value; } 
    }

    public void Clear(bool clearmesh = false)
    {
        mLines.Clear();
        if (clearmesh && (mLinesToRender != null))
        {
            mLinesToRender.Clear();
        }
    }

    public IEnumerator FindIntersections()
    {
//        Stopwatch stopWatch = new Stopwatch();
        List<Vector3> intersections;

//        stopWatch.Start();
        StartCoroutine(mLines.ShowIntersections());
//        stopWatch.Stop();
//        ExecutionTime = (float) stopWatch.Elapsed.TotalSeconds;
        Debug.Log(string.Format("Execution Time = {0}", ExecutionTime));
//        mIntersections.MakeMesh(intersections);
        yield return new WaitForEndOfFrame();
    }

    public void NewLines(int n)
    {
        float size = SCALE;
        List<LineSegment> lines = new List<LineSegment>(n);
        mLines.Clear();
        mIntersections.Clear();
        for (int i = 0; i < n; i++)
        {
            float x = Random.value - 0.5f;
            float y = Random.value - 0.5f;
            Vector3 v1 = new Vector3(size * x, size * y, 0);
            x = Random.value - 0.5f;
            y = Random.value - 0.5f;
            Vector3 v2 = new Vector3(size * x, size * y, 0);

            lines.Add(new LineSegment(v1, v2));
        }
        mLines.AddLines(lines);
        mLinesToRender.Display();
    }


}


