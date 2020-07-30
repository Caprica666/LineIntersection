using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlaneEvent : IComparable
{
    public static readonly int START = 1;
    public static readonly int END = 2;
    public static readonly int INTERSECTION = 3;
    public static readonly int HORIZONTAL = 4;

    private int mType;
    private LineSegment mSegment;
    private Vector3 mPoint;
    private int mVertexIndex = -1;

    public PlaneEvent(int type, Vector3 point, LineSegment segment)
    {
        mType = type;
        mPoint = point;
        mSegment = segment;
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
    }

    public PlaneEvent(int type, PlaneEvent src)
    {
        mType = type;
        mSegment = src.Line;
        if (type == END)
        {
            mPoint = mSegment.End;
        }
        else
        {
            mPoint = mSegment.Start;
        }
    }

    public int VertexIndex
    {
        get { return mVertexIndex; }
        set { mVertexIndex = value; }
    }

    public int Type
    {
        get { return mType; }
    }

    public Vector3 Start
    {
        get { return mSegment.Start; }
    }

    public Vector3 End
    {
        get { return mSegment.End; }
    }

    public LineSegment Line
    {
        get { return mSegment; }
    }

    public Vector3 Point
    {
        get { return mPoint; }
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

    public int CompareTo(object obj)
    {
        PlaneEvent p1 = this;
        PlaneEvent p2 = obj as PlaneEvent;
        float t = p1.Point.x - p2.Point.x;

        if (t < 0)
        {
            return -1;
        }
        if (t > 0)
        {
            return 1;
        }
        t = p1.Point.y - p2.Point.y;
        if (t < 0)
        {
            return 1;
        }
        if (t > 0)
        {
            return -1;
        }
        return p1.Type - p2.Type;
    }
}

public class EventCompare : IComparer<PlaneEvent>
{
    int IComparer<PlaneEvent>.Compare(PlaneEvent p1, PlaneEvent p2)
    {
        float t = p1.Point.x - p2.Point.x;

        if (t < 0)
        {
            return -1;
        }
        if (t > 0)
        {
            return 1;
        }
        t = p1.Point.y - p2.Point.y;
        if (t < 0)
        {
            return 1;
        }
        if (t > 0)
        {
            return -1;
        }
        return p1.Type - p2.Type;
    }
};

public class VecCompare : IComparer<Vector3>
{
    int IComparer<Vector3>.Compare(Vector3 v1, Vector3 v2)
    {
        float t = v1.x - v2.x;

        if (t < 0)
        {
            return -1;
        }
        if (t > 0)
        {
            return 1;
        }
        t = v1.y - v2.y;
        if (t < 0)
        {
            return 1;
        }
        if (t > 0)
        {
            return -1;
        }
        return 0;
    }
};

public class LineGroup
{
    private RBTree<PlaneEvent> mEventQ;
    private SortedList<Vector3, Vector3> mVertices;
    private List<PlaneEvent> mStartEvents = new List<PlaneEvent>();
    private List<PlaneEvent> mEndEvents = new List<PlaneEvent>();
    private List<PlaneEvent> mCandidates = new List<PlaneEvent>();
    private List<Vector3> mIntersections;
    private LineMesh mLineMesh = null;
    private PointMesh mPointMesh = null;

    public LineGroup(LineMesh lmesh = null, PointMesh pmesh = null)
    {
        mLineMesh = lmesh;
        mPointMesh = pmesh;
        mEventQ = new RBTree<PlaneEvent>();
        mVertices = new SortedList<Vector3, Vector3>(new VecCompare());
    }

    public void Clear()
    {
        mEventQ = new RBTree<PlaneEvent>();
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
                PlaneEvent p2 = new PlaneEvent(PlaneEvent.END, line);
                mVertices.Add(p1.Start, p1.Start);
                mVertices.Add(p1.End, p1.End);
                mEventQ.Add(p1);
                mEventQ.Add(p2);
                if (mLineMesh != null)
                {
                    Color c = new Color(Random.value, Random.value, Random.value, 1);
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
        Vector3 curpoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        PlaneEvent prev = null;
        IEnumerator<PlaneEvent> iter = mEventQ.GetEnumerator();

        mCandidates.Clear();
        mStartEvents.Clear();
        mEndEvents.Clear();
        while (iter.MoveNext())
        {
            PlaneEvent p = iter.Current;

            if (p.Point != curpoint)
            {
                if (Process(prev))
                {
                    Display();
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    iter = mEventQ.GetEnumerator();
                }
                mStartEvents.Clear();
                mEndEvents.Clear();
                mCandidates.Clear();
                curpoint = p.Point;
                prev = p;
            }
            if (p.Type == PlaneEvent.START)
            {
                mStartEvents.Add(p);
                mCandidates.Add(p);
            }
            else if (p.Type == PlaneEvent.END)
            {
                mEndEvents.Add(p);
            }
            else
            {
                mCandidates.Add(p);
            }
        }
        Process(prev);
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
        IEnumerator<PlaneEvent> iter = mEventQ.GetEnumerator();
        Vector3 vmax = mVertices.Values[mVertices.Count - 1];
        PlaneEvent prev = null;

        while (iter.MoveNext())
        {
            PlaneEvent p = iter.Current;

            if (p.Point == vmax)
            {
                if (p.Type == PlaneEvent.START)
                {
                    mStartEvents.Add(p);
                    mCandidates.Add(p);
                }
                else if (p.Type == PlaneEvent.END)
                {
                    mEndEvents.Add(p);
                }
                else
                {
                    mCandidates.Add(p);
                }
            }
            else
            {
                if (prev != null)
                {
                    Process(prev);
                }
                prev = p;
            }
        }
        Process(prev);
        return mIntersections;
    }

    public bool Remove(List<PlaneEvent> events)
    {
        if (events.Count == 0)
        {
            return false;
        }
        foreach (PlaneEvent e in events)
        {
            mEventQ.Remove(e);
            if (mLineMesh != null)
            {
                int vindex = e.Line.VertexIndex;
                mLineMesh.Update(vindex, Color.black);
            }
        }
        return true;
    }

    public void Remove(PlaneEvent e)
    {
        mEventQ.Remove(e);
        if (mLineMesh != null)
        {
            int vindex = e.Line.VertexIndex;
            mLineMesh.Update(vindex, Color.black);
        }
    }

    public bool Process(PlaneEvent p)
    {
        bool changed = false;
        if (p == null)
        {
            return false;
        }
        if ((mCandidates.Count + mEndEvents.Count) > 1)
        {
            mIntersections.Add(p.Point);
            if (mPointMesh != null)
            {
                mPointMesh.Add(p.Point);
            }
        }
        if (Remove(mEndEvents))
        {
            changed = true;
        }
        Vector3 isect = new Vector3();
        PlaneEvent leftNeighbor = null;
        PlaneEvent rightNeighbor = null;

        try
        {
            leftNeighbor = mEventQ.FindPredecessor(p);
        }
        catch (ArgumentOutOfRangeException) { }
        try
        {
            rightNeighbor = mEventQ.FindSuccessor(p);
        }
        catch (ArgumentOutOfRangeException) { }
        if (mCandidates.Count > 0)
        {
            PlaneEvent e = mCandidates[0];

            if ((leftNeighbor != null) &&
                (leftNeighbor.FindIntersection(e.Line, ref isect) > 0))
            {
                Remove(leftNeighbor);
                p = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                   new LineSegment(isect, leftNeighbor.End));
                p.VertexIndex = leftNeighbor.VertexIndex;
                Add(p);
                p = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                   new LineSegment(isect, e.End));
                p.VertexIndex = e.VertexIndex;
                Add(p);
                mIntersections.Add(p.Point);
            }
            e = mCandidates[mCandidates.Count - 1];
            if ((rightNeighbor != null) &&
                (rightNeighbor.FindIntersection(e.Line, ref isect) > 0))
            {
                Remove(rightNeighbor);
                p = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                   new LineSegment(isect, rightNeighbor.End));
                p.VertexIndex = rightNeighbor.VertexIndex;
                Add(p);
                p = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                   new LineSegment(isect, e.End));
                p.VertexIndex = e.VertexIndex;
                Add(p);
                mIntersections.Add(p.Point);
            }
            Remove(mCandidates);
            changed = true;
        }
        else if ((leftNeighbor != null) && (rightNeighbor != null))
        {
            if (leftNeighbor.FindIntersection(rightNeighbor.Line, ref isect) > 0)
            {
                p = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                   new LineSegment(isect, leftNeighbor.End));
                p.VertexIndex = leftNeighbor.VertexIndex;
                Add(p);
                p = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                   new LineSegment(isect, rightNeighbor.End));
                p.VertexIndex = rightNeighbor.VertexIndex;
                Add(p);
                mIntersections.Add(p.Point);
                changed = true;
            }
        }
        if (mCandidates.Count > 1)
        {
            return true;
        }
        return changed;
    }
}
