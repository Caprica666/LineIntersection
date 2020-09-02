using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class LineEvent
{
    public const int START = 1;
    public const int END = 2;
    public const int INTERSECTION = 3;
    public const int VERTICAL = 4;

    private Vector3 mPoint;
    public int mType;
    public LineSegment mLine;

    public Vector3 Start
    {
        get { return mLine.Start; }
        set
        {
            mLine = new LineSegment(value, mLine.End);
        }
    }

    public int Type
    {
        get { return mType; }
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
        if (mType == LineEvent.VERTICAL)
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
        string s = null;

        switch (mType)
        {
            case START:
            s = " START ";
            break;
            case END:
            s = " END ";
            break;
            case INTERSECTION:
            s = " ISECT ";
            break;
            case VERTICAL:
            s = " VERT ";
            break;
        }
        s += Point;
        return s;
    }

    public LineEvent(int type, Vector3 point, LineSegment l)
    {
        mPoint = point;
        mType = type;
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

        if (p1.Type == LineEvent.VERTICAL)
        {
            if (p2.Type != LineEvent.VERTICAL)
            {
                return 1;
            }
        }
        else if (p2.Type == LineEvent.VERTICAL)
        {
            return -1;
        }
        int order = vcompare.Compare(p1.Point, p2.Point);

        if (order == 0)
        {
            order = vcompare.Compare(p1.Start, p2.Start);
        }
        return order;
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
                LineEvent p1 = new LineEvent(LineEvent.START, line.Start, line);
                LineEvent p2 = new LineEvent(LineEvent.END, line.End, line);

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

    public bool Process(LineEnumerator iter)
    {
        if (mEventQ.Count == 0)
        {
            return false;
        }
        LineEvent e = mEventQ.Min;
        Vector3 isect = new Vector3();
        LineEvent p1 = null;
        LineEvent p2 = null;
        VecCompare vc = new VecCompare();
        LineSegment l = e.Line;
        LineSegment leftNeighbor;
        LineSegment rightNeighbor;
        float curX = e.Point.x;

        mCompareLines.CurrentX = curX;
        if (e.Type == LineEvent.END)
        {
            leftNeighbor = iter.FindLeftNeighbor(l);
            RemoveActive(e, true);
            if (leftNeighbor != null)
            {
                rightNeighbor = iter.FindRightNeighbor(leftNeighbor);
                if ((rightNeighbor != null) &&
                    (leftNeighbor.FindIntersection(rightNeighbor, ref isect) > 0) &&
                    (isect.x > curX))
                {
                    p1 = new LineEvent(LineEvent.INTERSECTION, isect, leftNeighbor);
                    p2 = new LineEvent(LineEvent.INTERSECTION, isect, rightNeighbor);
                    mActiveLines.Remove(leftNeighbor);
                    mActiveLines.Remove(rightNeighbor);
                    AddActive(leftNeighbor, Color.green);
                    AddActive(rightNeighbor, Color.red);
                    mEventQ.Add(p1);
                    mEventQ.Add(p2);
                }
            }
            mEventQ.Remove(e);
            return true;
        }
        if (e.Type == LineEvent.START)
        {
            AddActive(l);
        }
        else if (e.Type == LineEvent.INTERSECTION)
        {
            MarkIntersection(e.Point);
            mActiveLines.Remove(l);
            AddActive(l);
        }
        leftNeighbor = iter.FindLeftNeighbor(l);
        rightNeighbor = iter.FindRightNeighbor(l);
        if ((leftNeighbor != null) &&
            (leftNeighbor.FindIntersection(l, ref isect) > 0) &&
            (isect.x > curX))
        {
            p1 = new LineEvent(LineEvent.INTERSECTION, isect, leftNeighbor);
            p2 = new LineEvent(LineEvent.INTERSECTION, isect, l);
            mActiveLines.Remove(l);
            mActiveLines.Remove(leftNeighbor);
            AddActive(leftNeighbor, Color.green);
            AddActive(l);
            mEventQ.Add(p1);
            mEventQ.Add(p2);
        }
        if ((rightNeighbor != null) &&
            (rightNeighbor.FindIntersection(l, ref isect) > 0) &&
            (isect.x > curX))
        {
            p1 = new LineEvent(LineEvent.INTERSECTION, isect, rightNeighbor);
            p2 = new LineEvent(LineEvent.INTERSECTION, isect, l);
            mActiveLines.Remove(l);
            mActiveLines.Remove(rightNeighbor);
            mCompareLines.CurrentX = curX;
            AddActive(rightNeighbor, Color.red);
            AddActive(l);
            mEventQ.Add(p1);
            mEventQ.Add(p2);
        }
        mEventQ.Remove(e);
        return true;
    }
}
