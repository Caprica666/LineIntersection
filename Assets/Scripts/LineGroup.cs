using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlaneEvent
{
    public const int START = 1;
    public const int END = 2;
    public const int INTERSECTION = 3;
    public const int HORIZONTAL = 4;

    private int mType;
    private LineSegment mSegment;
    private Vector3 mPoint;
    private int mVertexIndex = -1;

    public PlaneEvent(int type, Vector3 point, LineSegment segment)
    {
        mType = type;
        mPoint = point;
        mSegment = segment;
        segment.Users.Add(this);
    }

    public PlaneEvent(int type, LineSegment segment)
    {
        mType = type;
        mSegment = segment;

        if (type == END)
        {
            mPoint = segment.End;
        }
        else
        {
            mPoint = segment.Start;
        }
        segment.Users.Add(this);
    }

    public override string ToString()
    {
        String s = null;

        switch (mType)
        {
            case START: s = "START "; break;
            case END:  s = "END "; break;
            case INTERSECTION: s = "ISECT "; break;
            case HORIZONTAL: s = "HORZ "; break;
        }
        s += Point.ToString();
        return s;
    }

    public int VertexIndex
    {
        get { return mVertexIndex; }
        set { mVertexIndex = value; }
    }

    public int Type
    {
        get { return mType; }
        set
        {
            mType = value;
        }
    }

    public Vector3 Point
    {
        get { return mPoint; }
        set
        {
            mPoint = value;
        }
    }

    public Vector3 Start
    {
        get { return mSegment.Start; }
        set
        {
            mSegment = new LineSegment(value, mSegment.End);
        }
    }

    public Vector3 End
    {
        get { return mSegment.End; }
        set
        {
            mSegment = new LineSegment(mSegment.Start, value);
        }
    }

    public LineSegment Line
    {
        get { return mSegment; }
    }


    public bool Overlaps(float x)
    {
        if ((mSegment.Start.x < x) || (x < mSegment.End.x))
        {
            return false;
        }
        return true;
    }

    public int FindIntersection(LineSegment line2, ref Vector3 intersection)
    {
        return mSegment.FindIntersection(line2, ref intersection);
    }
}

public class EventCompare : IComparer<PlaneEvent>
{
    public float CurrentX;

    public EventCompare(float x = 0)
    {
        CurrentX = x;
    }

    int IComparer<PlaneEvent>.Compare(PlaneEvent p1, PlaneEvent p2)
    {
        VecCompare vcompare = new VecCompare();
        int order = vcompare.Compare(p1.Point, p2.Point);
        Vector3 v1, v2;
        
        if (order != 0)
        {
            return order;
        }
        switch (p1.Type)
        {
            case PlaneEvent.START:
            v1 = p1.End;
            break;

            case PlaneEvent.END:
            case PlaneEvent.HORIZONTAL:
            v1 = p1.Start;
            break;

            case PlaneEvent.INTERSECTION:
            v1 = (p1.Point.x < CurrentX) ? p1.Start : p1.End;
            break;

            default:
            return p1.Type - p2.Type;
        }
        switch (p2.Type)
        {
            case PlaneEvent.START:
            v2 = p2.End;
            break;

            case PlaneEvent.END:
            case PlaneEvent.HORIZONTAL:
            v2 = p2.Start;
            break;

            case PlaneEvent.INTERSECTION:
            v2 = (p2.Point.x < CurrentX) ? p2.Start : p2.End;
            break;

            default:
            return p1.Type - p2.Type;
        }
        order = (int) (v1.y - v2.y);
        return order;
    }
};

public class VecCompare : Comparer<Vector3>
{
    public override int Compare(Vector3 v1, Vector3 v2)
    {
        float t = v1.x - v2.x;

        if (Math.Abs(t) > 2e-7)
        {
            return (t > 0) ? 1 : -1;
        }
        t = v1.y - v2.y;
        if (Math.Abs(t) > 2e-7)
        {
            return (t > 0) ? 1 : -1;
        }
        return 0;
    }
}

public class LineGroup
{
    private RBTree<PlaneEvent> mEventQ;
    private SortedList<Vector3, Vector3> mVertices;
    private List<Vector3> mIntersections;
    private LineMesh mLineMesh = null;
    private PointMesh mPointMesh = null;
    private EventCompare mCompareEvents = new EventCompare();

    public LineGroup(LineMesh lmesh = null, PointMesh pmesh = null)
    {
        mLineMesh = lmesh;
        mPointMesh = pmesh;
        mEventQ = new RBTree<PlaneEvent>(mCompareEvents);
        mVertices = new SortedList<Vector3, Vector3>(new VecCompare());
    }

    public RBTree<PlaneEvent> Events
    {
        get { return mEventQ; }
    }

    public void Clear()
    {
        mEventQ = new RBTree<PlaneEvent>(mCompareEvents);
        mVertices = new SortedList<Vector3, Vector3>(new VecCompare());
        if (mLineMesh != null)
        {
            mLineMesh.Clear();
        }
        if (mPointMesh != null)
        {
            mPointMesh.Clear();
        }
    }

    public void Add(PlaneEvent p)
    {
        try
        {
            mEventQ.Add(p);
            Debug.Log("Adding " + p.ToString());
            if (mLineMesh != null)
            {
                int vindex = p.VertexIndex;
                if (vindex >= 0)
                {
                    mLineMesh.Update(vindex, p.Line);
                }
                else
                {
                    p.VertexIndex = mLineMesh.Add(p.Line);
                }
            }
        }
        catch (ArgumentException ex)
        {
            Debug.LogWarning(String.Format("${0}, ${1} already added ${2}",
                                            p.Point.x, p.Point.y, ex.Message));
        }
    }

    public void AddLines(List<LineSegment> lines)
    {
        try
        {
            foreach (LineSegment line in lines)
            {
                PlaneEvent p1 = new PlaneEvent(PlaneEvent.START, line);
                PlaneEvent p2 = new PlaneEvent(PlaneEvent.END, line.End, line);
                mVertices.Add(p1.Start, p1.Start);
                mVertices.Add(p1.End, p1.End);
                mEventQ.Add(p1);
                mEventQ.Add(p2);
                Debug.Log("Initial " + p1.ToString());
                Debug.Log("Initial " + p2.ToString());
                if (mLineMesh != null)
                {
                    Color c = new Color(Random.value * 0.8f,
                                        Random.value * 0.8f, 
                                        Random.value * 0.8f, 1);
                    p1.VertexIndex = mLineMesh.Add(line, c);
                    p2.VertexIndex = p1.VertexIndex;
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

    public bool Remove(List<PlaneEvent> events, bool updateMesh = false)
    {
        if (events.Count == 0)
        {
            return false;
        }
        foreach (PlaneEvent e in events)
        {
            Remove(e, updateMesh);
        }
        return true;
    }

    public void Remove(LineSegment segment)
    {
        foreach (PlaneEvent p in segment.Users)
        {
            Remove(p, true);
        }
    }

    public bool Add(List<PlaneEvent> events)
    {
        if (events.Count == 0)
        {
            return false;
        }
        foreach (PlaneEvent e in events)
        {
            Add(e);
        }
        return true;
    }

    public void Remove(PlaneEvent e, bool updateMesh = false)
    {
        mEventQ.Remove(e);
        if (updateMesh)
        {
            Debug.Log("Deleting " + e);
            if (mLineMesh != null)
            {
                int vindex = e.Line.VertexIndex;
                mLineMesh.Update(vindex, Color.red);
            }
            if ((e.Type == PlaneEvent.INTERSECTION) &&
                (mPointMesh != null))
            {
                mPointMesh.Add(e.Point);
            }
        }
        else
        {
            Debug.Log("Removing " + e);
        }
    }

    public bool Process(LineEnumerator iter)
    {
        List<LineSegment> lines = iter.CollectAtPoint();
        List<PlaneEvent> collected = iter.Collected;
        VecCompare vc = new VecCompare();

        if (lines.Count > 1)
        {
            mIntersections.Add(iter.CurrentPoint);
            if (mPointMesh != null)
            {
                mPointMesh.Add(iter.CurrentPoint);
            }
            Debug.Log(String.Format("Intersection at {0}", iter.CurrentPoint));
        }
        Vector3 isect = new Vector3();
        for (int i = 0; i < lines.Count; ++i)
        {
            LineSegment l = lines[i];
            if (l.End == iter.CurrentPoint)
            {
                lines.Remove(l);
                --i;
            }
        }
        for (int i = 0; i < collected.Count; ++i)
        {
            PlaneEvent e = collected[i];

            if (e.Line.End == iter.CurrentPoint)
            {
                Remove(e.Line);
                collected.Remove(e);
            }
            else
            {
                Remove(e, false);
            }
        }
        mCompareEvents.CurrentX = iter.CurrentPoint.x;
        Add(collected);
        iter.Reset();

        PlaneEvent leftNeighbor = iter.FindLeftNeighbor(iter.CurrentPoint);
        PlaneEvent rightNeighbor = iter.FindRightNeighbor(iter.CurrentPoint);
        if (lines.Count > 0)
        {
            LineSegment l = lines[0];
            if ((leftNeighbor != null) &&
                (leftNeighbor.FindIntersection(l, ref isect) > 0) &&
                (vc.Compare(isect, iter.CurrentPoint) != 0))
            {
                PlaneEvent p1 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                             leftNeighbor.Line);
                PlaneEvent p2 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                            l);
                p1.VertexIndex = leftNeighbor.VertexIndex;

                Add(p1);
                Add(p2);
            }
            l = lines[lines.Count - 1];
            if ((rightNeighbor != null) &&
                (rightNeighbor.FindIntersection(l, ref isect) > 0) &&
                (vc.Compare(isect, iter.CurrentPoint) != 0))
            {
                PlaneEvent p1 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                             rightNeighbor.Line);
                PlaneEvent p2 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                            l);
                Add(p1);
                Add(p2);
            }
        }
        else if ((leftNeighbor != null) && (rightNeighbor != null))
        {
            if ((leftNeighbor.FindIntersection(rightNeighbor.Line, ref isect) > 0) &&
                (vc.Compare(isect, iter.CurrentPoint) != 0))
            {
                PlaneEvent p1 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                              leftNeighbor.Line);
                PlaneEvent p2 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                               rightNeighbor.Line);
                p1.VertexIndex = leftNeighbor.VertexIndex;
                p2.VertexIndex = rightNeighbor.VertexIndex;
                Add(p1);
                Add(p2);
            }
        }
        iter.Reset();
        return iter.MoveNextPoint();
    }
}
