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
    public bool Test;

    private LineGroup mLines;
    private LineMesh mLinesToRender;
    private PointMesh mIntersections;
    private List<LineSegment> mSaved = null;

    public void Awake()
    {
        GameObject intersections = GameObject.Find("Intersections");
        MeshFilter mf = intersections.GetComponent<MeshFilter>() as MeshFilter;
        mIntersections = new PointMesh(mf.mesh);
        mIntersections.PointSize = 0.2f;
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
            Clear();
            mLines.AddLines(mSaved);
            mLinesToRender.Recolor();
            mLinesToRender.Display();
            StartCoroutine(FindIntersections());
        }
        else if (Test)
        {
            Test = false;
            Clear();
            mSaved = new List<LineSegment>();
//            mSaved.Add(new LineSegment(new Vector3(1.0f, 3.5f, 0), new Vector3(2.6f, -1.6f, 0)));
//            mSaved.Add(new LineSegment(new Vector3(-0.4f, -1.5f, 0), new Vector3(-4.1f, -4.7f, 0)));
//            mSaved.Add(new LineSegment(new Vector3(0.6f, -4.5f, 0), new Vector3(4.9f, -0.8f, 0)));
//            mSaved.Add(new LineSegment(new Vector3(-3.9f, -2.1f, 0), new Vector3(3.4f, 2.6f, 0)));
//            mSaved.Add(new LineSegment(new Vector3(-3.3f, 3.8f, 0), new Vector3(4.7f, 4.1f, 0)));
//            mSaved.Add(new LineSegment(new Vector3(-3.0f, 0.5f, 0), new Vector3(-0.9f, -3.4f, 0)));
//            mSaved.Add(new LineSegment(new Vector3(-3.6f, -2.1f, 0), new Vector3(-0.2f, 1.9f, 0)));
//            mSaved.Add(new LineSegment(new Vector3(-1.4f, -0.6f, 0), new Vector3(2.5f, -4.0f, 0)));
//            mSaved.Add(new LineSegment(new Vector3(-2.9f, 1.5f, 0), new Vector3(-1.9f, 2.9f, 0)));
//            mSaved.Add(new LineSegment(new Vector3(-3.1f, -2.7f, 0), new Vector3(3.4f, -2.7f, 0)));
//            mSaved.Add(new LineSegment(new Vector3(-2.6f, -4.9f, 0), new Vector3(0.6f, 0.4f, 0)));
//            mSaved.Add(new LineSegment(new Vector3(-3.7f, 2.1f, 0), new Vector3(0.6f, -1.9f, 0)));
//            mSaved.Add(new LineSegment(new Vector3(-2.5f, 3.8f, 0), new Vector3(-2.5f, 3.8f, 0)));
//            mSaved.Add(new LineSegment(new Vector3(-0.4f, 3.4f, 0), new Vector3(2.0f, -2.4f, 0)));
//            mSaved.Add(new LineSegment(new Vector3(1.9f, -1.8f, 0), new Vector3(3.7f, 2.3f, 0)));
//            mSaved.Add(new LineSegment(new Vector3(-2.6f, -0.1f, 0), new Vector3(-2.6f, -0.1f, 0)));
            mSaved.Add(new LineSegment(new Vector3(-4.1f, 4.4f, 0), new Vector3(4.0f, 3.0f, 0)));
            mSaved.Add(new LineSegment(new Vector3(-2.8f, 2.8f, 0), new Vector3(-0.8f, 4.0f, 0)));
            mSaved.Add(new LineSegment(new Vector3(-3.4f, 2.5f, 0), new Vector3(1.5f, 0.3f, 0)));
            mSaved.Add(new LineSegment(new Vector3(-2.3f, 3.0f, 0), new Vector3(-1.7f, -1.7f, 0)));
            mLines.AddLines(mSaved);
            mLinesToRender.Recolor();
            mLinesToRender.Display();
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
        mIntersections.Clear();
        if (clearmesh && (mLinesToRender != null))
        {
            mLinesToRender.Clear();
        }
    }

    public IEnumerator FindIntersections()
    {
//        Stopwatch stopWatch = new Stopwatch();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

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
        mSaved = new List<LineSegment>(n);
        Clear();
        for (int i = 0; i < n; i++)
        {
            float x = Random.value - 0.5f;
            float y = Random.value - 0.5f;
            Vector3 v1 = new Vector3(size * x, size * y, 0);
            x = Random.value - 0.5f;
            y = Random.value - 0.5f;
            Vector3 v2 = new Vector3(size * x, size * y, 0);
            mSaved.Add(new LineSegment(v1, v2));
        }
        mLines.AddLines(mSaved);
        mLinesToRender.Display();
    }


}


