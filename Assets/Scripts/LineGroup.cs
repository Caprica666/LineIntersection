using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class LineEvent
{
    private Vector3 mPoint;
    public LineSegment mLine;

    public Vector3 Start
    {
        get { return mLine.Start; }
        set
        {
            mLine = new LineSegment(value, mLine.End);
        }
    }

    public Vector3 End
    {
        get { return mLine.End; }
        set
        {
            mLine = new LineSegment(mLine.Start, value);
        }
    }

    public LineSegment Line
    {
        get { return mLine; }
    }

    public Vector3 Point
    {
        get { return mPoint; }
        set
        {
            mPoint = value;
        }
    }

    public int FindIntersection(LineSegment line2, ref Vector3 intersection)
    {
        if (Start.x == End.x)
        {
            if (line2.Start.x == line2.End.x)
            {
                return -1;
            }
            intersection.x = Start.x;
            intersection.y = line2.CalcY(intersection.x);
            if ((intersection.y >= Start.y) && (intersection.y <= End.y))
            {
                return 1;
            }
            return -1;
        }
        return mLine.FindIntersection(line2, ref intersection);
    }

    public override string ToString()
    {
        return Point.ToString();
    }

    public LineEvent(Vector3 point, LineSegment l)
    {
        mPoint = point;
        mLine = l;
    }
}

public class LineCompare : Comparer<LineSegment>
{
    public float CurrentX = float.MaxValue;

    public LineCompare() {  }

    public override int Compare(LineSegment s1, LineSegment s2)
    {
        float y1 = s1.CalcY(CurrentX);
        float y2 = s2.CalcY(CurrentX);
        float t = y1 - y2;

        if (Math.Abs(t) > LineSegment.EPSILON)
        {
            return (t > 0) ? 1 : -1;
        }
        Vector3 v1 = s1.End;
        Vector3 v2 = s2.End;
        Vector3 sweep = new Vector3(0, -1, 0);
        float a1, a2;

        if (Vector3.Equals(v1, v2))
        {
            return 0;
        }
        v1 -= s1.Start;
        v2 -= s2.Start;
        v1.Normalize();
        v2.Normalize();
        a1 = Vector3.Dot(sweep, v1);
        a2 = Vector3.Dot(sweep, v2);
        t = a2 - a1;
        if (Math.Abs(t) > LineSegment.EPSILON)
        {
            return (t > 0) ? 1 : -1;
        }
        return 0;
    }
}

public class EventCompare : IComparer<LineEvent>
{
    int IComparer<LineEvent>.Compare(LineEvent p1, LineEvent p2)
    {
        VecCompare vcompare = new VecCompare();

        int order = vcompare.Compare(p1.Point, p2.Point);
        if (order != 0)
        {
            return order;
        }
        float t = p1.Start.y - p2.Start.y;

        if (Math.Abs(t) < LineSegment.EPSILON)
        {
            return 0;
        }
        return (p1.Start.y > p2.Start.y) ? -1 : 1;
    }
}

public class VecCompare : Comparer<Vector3>
{
    public override int Compare(Vector3 v1, Vector3 v2)
    {
        float epsilon = 1e-6f;
        float t = v1.x - v2.x;

        if (Math.Abs(t) > epsilon)
        {
            return (t > 0) ? 1 : -1;
        }
        t = v1.y - v2.y;
        if (Math.Abs(t) > epsilon)
        {
            return (t > 0) ? 1 : -1;
        }
        return 0;
    }
}

public class LineGroup
{
    private RBTree<LineEvent> mEventQ;
    private RBTree<LineSegment> mActiveLines;
    private List<Vector3> mIntersections;
    private LineMesh mLineMesh = null;
    private PointMesh mPointMesh = null;
    private EventCompare mCompareEvents = new EventCompare();
    private LineCompare mCompareLines = new LineCompare();

    public LineGroup(LineMesh lmesh = null, PointMesh pmesh = null)
    {
        mLineMesh = lmesh;
        mPointMesh = pmesh;
        mEventQ = new RBTree<LineEvent>(mCompareEvents);
        mActiveLines = new RBTree<LineSegment>(mCompareLines);
    }

    public RBTree<LineEvent> Events
    {
        get { return mEventQ; }
    }

    public RBTree<LineSegment> ActiveLines
    {
        get { return mActiveLines; }
    }

    public void Clear()
    {
        mEventQ = new RBTree<LineEvent>(mCompareEvents);
        mActiveLines = new RBTree<LineSegment>(mCompareLines);
        if (mLineMesh != null)
        {
            mLineMesh.Clear();
        }
        if (mPointMesh != null)
        {
            mPointMesh.Clear();
        }
    }

    public void AddLines(List<LineSegment> lines)
    {
        try
        {
            foreach (LineSegment line in lines)
            {
                LineEvent p1 = new LineEvent(line.Start, line);
                LineEvent p2 = new LineEvent(line.End, line);

                line.VertexIndex = -1;
                mEventQ.Add(p1);
                mEventQ.Add(p2);
                if (mLineMesh != null)
                {
                    Color c = new Color(Random.value, Random.value, Random.value, 1);
                    line.VertexIndex = mLineMesh.Add(line, c);
                }
            }
        }
        catch (ArgumentException ex)
        {
            Debug.LogWarning(ex.Message);
        }
    }

    public IEnumerator ShowIntersections()
    {
        mIntersections = new List<Vector3>();
        LineEnumerator iter = new LineEnumerator(this);

        while (Process(iter))
        {
            Display();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
        }
        Display();
    }

    public void Display()
    {
        if (mLineMesh != null)
        {
            mLineMesh.Display();
        }
        if (mPointMesh != null)
        {
            mPointMesh.Display();
        }
    }

    public void FindIntersections(List<Vector3> intersections)
    {
        mIntersections = intersections;
        LineEnumerator iter = new LineEnumerator(this);

        while (Process(iter))
            ;
    }

    public void RemoveActive(LineSegment s, bool updateMesh = false)
    {
        if (updateMesh)
        {
            mActiveLines.Remove(s);
            if (mLineMesh != null)
            {
                int vindex = s.VertexIndex;
                mLineMesh.Update(vindex, Color.black);
            }
        }
        else
        {
            mActiveLines.Remove(s);
        }
    }

    private void MarkIntersection(Vector3 isect)
    {
        mIntersections.Add(isect);
        if (mPointMesh != null)
        {
            mPointMesh.Add(isect);
        }
    }

    public void AddActive(LineSegment l, Color c)
    {
        mActiveLines.Add(l);
        if (mLineMesh != null)
        {
            int vindex = l.VertexIndex;
            mLineMesh.Update(vindex, c);
        }
    }

    public void AddActive(LineSegment l)
    {
        mActiveLines.Add(l);
    }

    public void AddIsectEvent(LineSegment s1, LineSegment s2, Vector3 p)
    {
        LineEvent e1 = new LineEvent(p, s1);
        LineEvent e2 = new LineEvent(p, s2);

        if (mCompareLines.CurrentX >= p.x)
        {
            return;
        }
        mEventQ.Add(e1);
        mEventQ.Add(e2);
    }

    public void RemoveIsectEvent(LineSegment s1, LineSegment s2, Vector3 p)
    {
        LineEvent e1 = new LineEvent(p, s1);
        LineEvent e2 = new LineEvent(p, s2);

        if (mCompareLines.CurrentX < p.x)
        {
            return;
        }
        mEventQ.Remove(e1);
        mEventQ.Remove(e2);
    }

    public List<LineSegment> CollectLines(LineEvent e)
    {
        List<LineSegment> collected = new List<LineSegment>();
        LineEvent nextEvent = e;
        VecCompare vcompare = new VecCompare();
        int order = 0;

        while (order == 0)
        {
            collected.Add(nextEvent.Line);
            mEventQ.Remove(nextEvent);
            nextEvent = mEventQ.Min;
            if (nextEvent != null)
            {
                order = vcompare.Compare(nextEvent.Point, e.Point);
            }
            else
            {
                break;
            }
        }
        return collected;
    }

    public bool Process(LineEnumerator lineiter)
    {
        if (mEventQ.Count == 0)
        {
            return false;
        }
        LineEvent e = mEventQ.Min;
        Vector3 isect = new Vector3();
        LineSegment bottomNeighbor;
        LineSegment topNeighbor;
        float curX = e.Point.x;
        List<LineSegment> collected = CollectLines(e);

        if (collected.Count > 1)
        {
            MarkIntersection(e.Point);
        }
        for (int i = 0; i < collected.Count; ++i)
        {
            LineSegment l = collected[i];
            if (l.End == e.Point)
            {
                RemoveActive(l);
                collected.RemoveAt(i);
            }
        }
        if (collected.Count == 0)
        {
            mCompareLines.CurrentX = curX;
            bottomNeighbor = lineiter.FindBottomNeighbor(e.Line);
            topNeighbor = lineiter.FindTopNeighbor(e.Line);
            if ((bottomNeighbor != null) &&
                (topNeighbor != null) &&
                (bottomNeighbor.FindIntersection(topNeighbor, ref isect) > 0))
            {
                AddIsectEvent(bottomNeighbor, topNeighbor, isect);
            }
        }
        else
        {
            foreach (LineSegment l in collected)
            {
                RemoveActive(l);
            }
            mCompareLines.CurrentX = curX;
            foreach (LineSegment l in collected)
            {
                AddActive(l);
            }
            LineSegment bottom = collected[0];
            LineSegment top = collected[collected.Count - 1];
            bottomNeighbor = lineiter.FindBottomNeighbor(bottom);
            topNeighbor = lineiter.FindTopNeighbor(top);

            if ((bottomNeighbor != null) &&
                (bottomNeighbor.FindIntersection(bottom, ref isect) > 0))
            {
                AddIsectEvent(bottomNeighbor, bottom, isect);
            }
            if ((topNeighbor != null) &&
                (topNeighbor.FindIntersection(top, ref isect) > 0))
            {
                AddIsectEvent(top, topNeighbor, isect);
                if ((bottomNeighbor != null) &&
                    (bottomNeighbor.FindIntersection(topNeighbor, ref isect) > 0))
                {
                    RemoveIsectEvent(bottomNeighbor, topNeighbor, isect);
                }
            }
        }
        return true;
    }
}
