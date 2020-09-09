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
        float epsilon = 1e-6f;
        float y1 = s1.CalcY(CurrentX);
        float y2 = s2.CalcY(CurrentX);
        float t = y1 - y2;

        if (Math.Abs(t) > epsilon)
        {
            return (t > 0) ? 1 : -1;
        }
        t = s1.End.y - s2.End.y;
        if (Math.Abs(t) > epsilon)
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

        if (p1.Start.x == p1.End.x)
        {
            if (p2.Start.x != p2.End.x)
            {
                return 1;
            }
        }
        else if (p2.Start.x == p2.End.x)
        {
            return -1;
        }
        return vcompare.Compare(p1.Point, p2.Point);
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
    private SortedList<Vector3, Vector3> mVertices;
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

    public List<Vector3> FindIntersections()
    {
        mIntersections = new List<Vector3>();
        LineEnumerator iter = new LineEnumerator(this);

        while (Process(iter))
            ;
        return mIntersections;
    }

    public void RemoveActive(LineEvent e, bool updateMesh = false)
    {
        if (updateMesh)
        {
            mActiveLines.Remove(e.Line);
            if (mLineMesh != null)
            {
                int vindex = e.Line.VertexIndex;
                mLineMesh.Update(vindex, Color.black);
            }
        }
        else
        {
            mActiveLines.Remove(e.Line);
        }
    }

    private void MarkIntersection(Vector3 isect)
    {
        mIntersections.Add(isect);
        if (mPointMesh != null)
        {
            mPointMesh.Add(isect);
        }
        Debug.Log(String.Format("Intersection at {0}", isect));
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

    public void AddIsectEvent(LineSegment s1, LineSegment s2, Vector3 p,float curx)
    {
        LineEvent p1 = new LineEvent(p, s1);
        LineEvent p2 = new LineEvent(p, s2);
        LineEvent p3;
        LineCompare comparer = new LineCompare();

        comparer.CurrentX = curx;
        if (comparer.Compare(s1, s2) < 0)
        {
            p2.mLine = s1;
        }
        if (mEventQ.TryGetValue(p2, out p3))
        {
            if (comparer.Compare(p3.Line, p2.Line) < 0)
            {
                p3.mLine = p2.Line;
            }
        }
        else
        {
            mEventQ.Add(p2);
        }
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
        List<LineSegment> addus = lineiter.CollectAt(e.Line, e.Point);

        mCompareLines.CurrentX = curX;
        if (e.Start == e.Point)
        {
            AddActive(e.Line);
            addus.Add(e.Line);
        }
        if (addus.Count > 1)
        {
            MarkIntersection(e.Point);
        }
        for (int i = 0; i < addus.Count; ++i)
        {
            LineSegment ls = e.Line;
            if (ls.End == e.Point)
            {
                RemoveActive(e, true);
                addus.Remove(ls);
                --i;
            }
        }
        if (addus.Count == 0)
        {
            bottomNeighbor = lineiter.FindBottomNeighbor(e.Line);
            topNeighbor = lineiter.FindTopNeighbor(e.Line);
            if ((bottomNeighbor != null) &&
                (topNeighbor != null) &&
                (bottomNeighbor.FindIntersection(topNeighbor, ref isect) > 0) &&
                (isect.x > curX))
            {
                AddIsectEvent(bottomNeighbor, topNeighbor, isect, curX);
                mActiveLines.Remove(bottomNeighbor);
                mActiveLines.Remove(topNeighbor);
                AddActive(bottomNeighbor);
                AddActive(topNeighbor);
            }
        }
        else
        {
            foreach (LineSegment ls in addus)
            {
                mActiveLines.Remove(ls);
            }
            foreach (LineSegment ls in addus)
            {
                AddActive(ls);
            }
            LineSegment top = addus[0];
            LineSegment bottom = addus[addus.Count - 1];
            bottomNeighbor = lineiter.FindBottomNeighbor(bottom);
            topNeighbor = lineiter.FindTopNeighbor(top);
            if ((bottomNeighbor != null) &&
                (bottomNeighbor.FindIntersection(bottom, ref isect) > 0) &&
                (isect.x > curX))
            {
                AddIsectEvent(bottomNeighbor, bottom, isect, curX);
                mActiveLines.Remove(bottomNeighbor);
                AddActive(bottomNeighbor);
            }
            if ((topNeighbor != null) &&
                (topNeighbor.FindIntersection(top, ref isect) > 0) &&
                (isect.x > curX))
            {
                AddIsectEvent(top, topNeighbor, isect, curX);
                mActiveLines.Remove(topNeighbor);
                AddActive(topNeighbor);
            }
        }
        mEventQ.Remove(e);
        return true;
    }
}
