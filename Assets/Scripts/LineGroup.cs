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
    private PlaneEvent mLink = null;
    private int mVertexIndex = -1;

    public PlaneEvent(int type, Vector3 point, LineSegment segment)
    {
        mType = type;
        mPoint = point;
        mSegment = segment;
    }

    public PlaneEvent(int type, Vector3 point, LineSegment segment, PlaneEvent link = null)
    {
        mType = type;
        mPoint = point;
        mSegment = segment;
        mLink = link;
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

    public PlaneEvent Link
    {
        get { return mLink; }
        set { mLink = value; }
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

public class VecCompare : Comparer<Vector3>
{
    public override int Compare(Vector3 v1, Vector3 v2)
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

    public RBTree<PlaneEvent> Events
    {
        get { return mEventQ; }
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
            if ((p.Type == PlaneEvent.INTERSECTION) &&
                (mPointMesh != null))
            {
                mPointMesh.Add(p.Point);
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
                PlaneEvent p2 = new PlaneEvent(PlaneEvent.END, line.End, line, p1);
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
        LineEnumerator iter = new LineEnumerator(this);

        while (iter.MoveNextPoint())
        {
            Process(iter);
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

        while (iter.MoveNextPoint())
        {
            Process(iter);
            iter.Reset();
        }
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
            if (e.Link != null)
            {
                PlaneEvent l = e.Link;
                mEventQ.Remove(l);
                if (mLineMesh != null)
                {
                    int vindex = l.Line.VertexIndex;
                    mLineMesh.Update(vindex, Color.black);
                }
            }

        }
        return true;
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

    public void Remove(PlaneEvent e)
    {
        mEventQ.Remove(e);
        if (mLineMesh != null)
        {
            int vindex = e.Line.VertexIndex;
            mLineMesh.Update(vindex, Color.black);
        }
    }

    public void Process(LineEnumerator iter)
    {
        List<PlaneEvent> intersections = iter.Intersections;
        PlaneEvent first = iter.First;
        PlaneEvent leftNeighbor = iter.LeftNeighbor;
        PlaneEvent rightNeighbor = iter.RightNeighbor;
        bool needsreset = false;

        if ((intersections.Count + iter.Ends.Count) > 1)
        {
            mIntersections.Add(first.Point);
            if (mPointMesh != null)
            {
                mPointMesh.Add(first.Point);
            }
        }
        if (Remove(iter.Ends))
        {
            needsreset = true;
        }
        Vector3 isect = new Vector3();

        if (intersections.Count > 0)
        {
            Remove(intersections);
            PlaneEvent e = intersections[0];
            if ((leftNeighbor != null) &&
                (leftNeighbor.FindIntersection(e.Line, ref isect) > 0))
            {
                PlaneEvent p = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                             leftNeighbor.Line);
                if (leftNeighbor.Link != null)
                {
                    leftNeighbor.Link.Link = p;
                }
                Remove(leftNeighbor);
                p.VertexIndex = leftNeighbor.VertexIndex;
                intersections.Add(p);
                e.Point = isect;
                e.Type = PlaneEvent.INTERSECTION;
                mIntersections.Add(isect);
            }
            e = intersections[intersections.Count - 1];
            if ((rightNeighbor != null) &&
                (rightNeighbor.FindIntersection(e.Line, ref isect) > 0))
            {
                PlaneEvent p = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                              rightNeighbor.Line);
                if (rightNeighbor.Link != null)
                {
                    rightNeighbor.Link.Link = p;
                }
                Remove(rightNeighbor);
                p.VertexIndex = rightNeighbor.VertexIndex;
                intersections.Add(p);
                e.Point = isect;
                e.Type = PlaneEvent.INTERSECTION;
                mIntersections.Add(isect);
            }
            Add(intersections);
            needsreset = true;
        }
        else if ((leftNeighbor != null) && (rightNeighbor != null))
        {
            if (leftNeighbor.FindIntersection(rightNeighbor.Line, ref isect) > 0)
            {
                PlaneEvent p1 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                              leftNeighbor.Line);
                PlaneEvent p2 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                               rightNeighbor.Line);
                if (leftNeighbor.Link != null)
                {
                    leftNeighbor.Link.Link = p1;
                }
                if (rightNeighbor.Link != null)
                {
                    rightNeighbor.Link.Link = p2;
                }
                p1.VertexIndex = leftNeighbor.VertexIndex;
                p2.VertexIndex = rightNeighbor.VertexIndex;
                Remove(leftNeighbor);
                Remove(rightNeighbor);
                needsreset = true;
                Add(p1);
                Add(p2);
                mIntersections.Add(isect);
            }
        }
        if (needsreset)
        {
            iter.Reset();
        }
    }
}

public class LineEnumerator : RBTree<PlaneEvent>.Enumerator
{
    private Vector3 mCurrentPoint;
    private PlaneEvent mFirst = null;
    private PlaneEvent mLeftNeighbor = null;
    private PlaneEvent mRightNeighbor = null;
    private List<PlaneEvent> mStartEvents = new List<PlaneEvent>();
    private List<PlaneEvent> mEndEvents = new List<PlaneEvent>();
    private List<PlaneEvent> mCandidates = new List<PlaneEvent>();
    public LineEnumerator(LineGroup lines)
    : base(lines.Events)
    {
        mFirst = stack.Peek().Item;
        mCurrentPoint = mFirst.Point;
    }

    public PlaneEvent First
    {
        get { return mFirst; }
    }

    public PlaneEvent LeftNeighbor
    {
        get { return mLeftNeighbor; }
    }

    public PlaneEvent RightNeighbor
    {
        get { return mRightNeighbor; }
    }

    public Vector3 CurrentPoint
    {
        get { return mCurrentPoint; }
    }

    public List<PlaneEvent> Ends
    {
        get { return mEndEvents; }
    }

    public List<PlaneEvent> Intersections
    {
        get { return mCandidates; }
    }

    public override void Reset()
    {
        mEndEvents.Clear();
        mCandidates.Clear();
        mLeftNeighbor = null;
        mRightNeighbor = null;
        mFirst = null;
        stack.Clear();
        Intialize();
        version = tree.Version;
        MoveToPoint(mCurrentPoint);
    }

    public bool MoveNextPoint()
    {
        return MoveAfterPoint(mCurrentPoint);
    }

    public bool MoveToPoint(Vector3 point)
    {
        VecCompare vcomparer = new VecCompare();
        while (MoveNext())
        {
            int order = vcomparer.Compare(point, Current.Point);
            if (order == 0)
            {
                mCurrentPoint = Current.Point;
                mFirst = Current;
                if (Current.End == point)
                {
                    mEndEvents.Add(Current);
                }
                else
                {
                    mCandidates.Add(Current);
                }
                return true;
            }
        }
        return false;
    }

    public bool MoveAfterPoint(Vector3 point)
    {
        VecCompare vcomparer = new VecCompare();
        RBTree<PlaneEvent>.Node prev = null;

        while (MoveNext())
        {
            int order = vcomparer.Compare(point, Current.Point);
            if (order > 0)
            {
                continue;
            }
            prev = current;
            if (order < 0)
            {
                mLeftNeighbor = LeftPoint(prev);
                mRightNeighbor = Current;
                mCurrentPoint = Current.Point;
                return true;
            }
            if (Current.End == mCurrentPoint)
            {
                mEndEvents.Add(Current);
            }
            else
            {
                mCandidates.Add(Current);
            }
        }
        if (prev != null)
        {
            mCurrentPoint = prev.Item.Point;
            mLeftNeighbor = LeftPoint(prev);
            mRightNeighbor = null;
            return true;
        }
        return false;
    }

    private PlaneEvent LeftPoint(RBTree<PlaneEvent>.Node node)
    {
        RBTree<PlaneEvent>.Node left = node;

        if (node.Left != null)
        {
            return node.Left.Item;
        }
        Stack<RBTree<PlaneEvent>.Node> s = new Stack<RBTree<PlaneEvent>.Node>(stack);
        VecCompare vcomparer = new VecCompare();

        try
        {
            while ((left = s.Pop()) != null)
            {
                int order = vcomparer.Compare(node.Item.Point, left.Item.Point);

                if (order < 0)
                {
                    return left.Item;
                }
            }
        }
        catch (InvalidOperationException) { }
        return null;
    }

};
