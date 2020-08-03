using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlaneEvent : IComparable
{
    public const int START = 1;
    public const int END = 2;
    public const int INTERSECTION = 3;
    public const int HORIZONTAL = 4;

    private int mType;
    private LineSegment mSegment;
    private Vector3 mPoint;
    private PlaneEvent mNext = null;
    private PlaneEvent mPrev = null;
    private int mVertexIndex = -1;

    public PlaneEvent(int type, Vector3 point, LineSegment segment)
    {
        mType = type;
        mPoint = point;
        mSegment = segment;
    }

    public PlaneEvent(int type, Vector3 point, LineSegment segment, PlaneEvent prev = null)
    {
        mType = type;
        mPoint = point;
        mSegment = segment;

        if (prev != null)
        {
            mPrev = prev;
            prev.Next = this;
        }
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

    public PlaneEvent Next
    {
        get { return mNext; }
        set { mNext = value; }
    }

    public PlaneEvent Prev
    {
        get { return mPrev; }
        set { mPrev = value; }
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

    public bool Remove(List<PlaneEvent> events, bool removeLinks = false)
    {
        if (events.Count == 0)
        {
            return false;
        }
        foreach (PlaneEvent e in events)
        {
            Remove(e, removeLinks);
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

    public void Remove(PlaneEvent e, bool removeLinks = false)
    {
        mEventQ.Remove(e);
        if (removeLinks)
        {
            if (mLineMesh != null)
            {
                int vindex = e.Line.VertexIndex;
                mLineMesh.Update(vindex, Color.white);
            }
            if (e.Prev != null)
            {
                Remove(e.Prev, removeLinks);
            }
        }
    }

    private void Insert(PlaneEvent oldOne, PlaneEvent newOne)
    {
        newOne.Next = oldOne.Next;
        newOne.Prev = oldOne;
        if (oldOne.Next != null)
        {
            oldOne.Next.Prev = newOne;
        }
        oldOne.Next = newOne;
    }

    public bool Process(LineEnumerator iter)
    {
        iter.CollectAtPoint(iter.CurrentPoint);
        List<PlaneEvent> intersections = iter.Intersections;
        PlaneEvent first = iter.First;
        PlaneEvent leftNeighbor = iter.LeftNeighbor;
        PlaneEvent rightNeighbor = iter.RightNeighbor;
        bool needsreset = false;
        List<PlaneEvent> removeus = new List<PlaneEvent>(intersections);

        Debug.Log(String.Format("Collected {0} events at {1}, Right neighbor = {2}",
                removeus.Count + intersections.Count,
                first.Point,
                (rightNeighbor != null) ? rightNeighbor.Point : Vector3.zero));

        needsreset = Remove(iter.Ends, true);
        mCompareEvents.CurrentX = first.Point.x;
        if ((intersections.Count + iter.Ends.Count) > 1)
        {
            mIntersections.Add(first.Point);
            if (mPointMesh != null)
            {
                mPointMesh.Add(first.Point);
            }
        }
        Vector3 isect = new Vector3();

        if (intersections.Count > 0)
        {
            PlaneEvent e = intersections[0];
            if ((leftNeighbor != null) &&
                (leftNeighbor.FindIntersection(e.Line, ref isect) > 0))
            {
                PlaneEvent p = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                             leftNeighbor.Line);
                Insert(leftNeighbor, p);
                p.VertexIndex = leftNeighbor.VertexIndex;
                intersections.Add(p);
                e.Point = isect;
                e.Type = PlaneEvent.INTERSECTION;
            }
            e = intersections[intersections.Count - 1];
            if ((rightNeighbor != null) &&
                (rightNeighbor.FindIntersection(e.Line, ref isect) > 0))
            {
                PlaneEvent p = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                              rightNeighbor.Line);
                Insert(rightNeighbor, p);
                p.VertexIndex = rightNeighbor.VertexIndex;
                intersections.Add(p);
                e.Point = isect;
                e.Type = PlaneEvent.INTERSECTION;
            }
        }
        else if ((leftNeighbor != null) && (rightNeighbor != null))
        {
            if (leftNeighbor.FindIntersection(rightNeighbor.Line, ref isect) > 0)
            {
                PlaneEvent p1 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                              leftNeighbor.Line);
                PlaneEvent p2 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                               rightNeighbor.Line);
                Insert(leftNeighbor, p1);
                Insert(rightNeighbor, p2);
                p1.VertexIndex = leftNeighbor.VertexIndex;
                p2.VertexIndex = rightNeighbor.VertexIndex;
                intersections.Add(p1);
                intersections.Add(p2);
                needsreset = true;
            }
        }
        needsreset |= Remove(removeus);
        Add(intersections);
        if (needsreset)
        {
            iter.Reset();
            return iter.MoveNextPoint();
        }
        return true;
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
        mCurrentPoint = new Vector3(float.MinValue, float.MinValue, 0);
        mLeftNeighbor = null;
        mRightNeighbor = null;
        mFirst = null;
        MoveNextPoint();
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
    }

    public bool MoveNextPoint()
    {
        mRightNeighbor = null;
        return MoveAfterPoint(mCurrentPoint);
    }

    public bool MoveAfterPoint(Vector3 point)
    {
        VecCompare vcomparer = new VecCompare();

        if (stack.Count == 0)
        {
            return false;
        }
        while (stack.Count > 0)
        {
            current = stack.Pop();
            int order = vcomparer.Compare(point, Current.Point);

            if (order == 0)
            {
                current = RightNode(current);
                if (current == null)
                {
                    return false;
                }
                stack.Push(current);
                break;
            }
            else if (order < 0)
            {
                if (current.Left == null)
                {
                    break;
                }
                stack.Push(current.Left);
            }
            else
            {
                if (current.Right == null)
                {
                    break;
                }
                stack.Push(current.Right);
            }
        }
        while (stack.Count > 0)
        {
            current = stack.Pop();
            int order = vcomparer.Compare(point, Current.Point);

            if (order < 0)
            {
                mFirst = Current;
                mCurrentPoint = Current.Point;
                mLeftNeighbor = LeftPoint(current);
                Debug.Log(String.Format("Current point = {0}, Left neighbor = {1}",
                                        mCurrentPoint,
                                        (mLeftNeighbor != null) ? mLeftNeighbor.Point :
                                                                  Vector3.zero));
                if (Current.End == mCurrentPoint)
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

    public bool CollectAtPoint(Vector3 point)
    {
        VecCompare vcomparer = new VecCompare();
        RBTree<PlaneEvent>.Node prev = current;

        while (MoveNext())
        {
            int order = vcomparer.Compare(point, Current.Point);

            if (order != 0)
            {
                break;
            }
            if (prev.Item.End == point)
            {
                mEndEvents.Add(prev.Item);
            }
            else
            {
                mCandidates.Add(prev.Item);
            }
            prev = current;
        }
        mRightNeighbor = RightPoint(prev);
        return (mEndEvents.Count + mCandidates.Count) > 0;
    }

    private RBTree<PlaneEvent>.Node LeftNode(RBTree<PlaneEvent>.Node node)
    {
        RBTree<PlaneEvent>.Node left = node;

        if (node.Left != null)
        {
            return node.Left;
        }
        Stack<RBTree<PlaneEvent>.Node> s = new Stack<RBTree<PlaneEvent>.Node>(stack);
        VecCompare vcomparer = new VecCompare();

        while (s.Count > 0)
        {
            left = s.Pop();
            int order = vcomparer.Compare(node.Item.Point, left.Item.Point);

            if (order > 0)
            {
                return left;
            }
        }
        return null;
    }

    private RBTree<PlaneEvent>.Node RightNode(RBTree<PlaneEvent>.Node node)
    {
        RBTree<PlaneEvent>.Node right = node;

        if (node.Right != null)
        {
            return node.Right;
        }
        Stack<RBTree<PlaneEvent>.Node> s = new Stack<RBTree<PlaneEvent>.Node>(stack);
        VecCompare vcomparer = new VecCompare();

        while (s.Count > 0)
        {
            right = s.Pop();
            int order = vcomparer.Compare(node.Item.Point, right.Item.Point);

            if (order < 0)
            {
                return right;
            }
        }
        return null;
    }

    private PlaneEvent LeftPoint(RBTree<PlaneEvent>.Node node)
    {
        RBTree<PlaneEvent>.Node left = LeftNode(node);

        if (left != null)
        {
            return left.Item;
        }
        return null;
    }

    private PlaneEvent RightPoint(RBTree<PlaneEvent>.Node node)
    {
        RBTree<PlaneEvent>.Node right = RightNode(node);

        if (right != null)
        {
            return right.Item;
        }
        return null;
    }

};
