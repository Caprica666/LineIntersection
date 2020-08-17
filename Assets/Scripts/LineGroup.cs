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
    public const int VERTICAL = 4;

    private int mType;
    private LineSegment mSegment;
    private Vector3 mPoint;

    public PlaneEvent(int type, Vector3 point, LineSegment segment)
    {
        mType = type;
        mPoint = point;
        mSegment = segment;
        foreach (PlaneEvent p in segment.Users)
        {
            if (p.Point == point)
            {
                return;
            }
        }
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
        foreach (PlaneEvent p in segment.Users)
        {
            if (p.Point == mPoint)
            {
                return;
            }
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
            case VERTICAL: s = "HORZ "; break;
        }
        s += Point.ToString();
        return s;
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
            v1 = p1.Start;
            break;

            case PlaneEvent.VERTICAL:
            v1 = (p1.Start.y < p1.End.y) ? p1.Start : p1.End;
            break;

            case PlaneEvent.END:
            v1 = p1.End;
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
            v2 = p2.Start;
            break;

            case PlaneEvent.VERTICAL:
            v2 = (p2.Start.y < p2.End.y) ? p2.Start : p2.End;
            break;

            case PlaneEvent.END:
            v2 = p2.End;
            break;

            case PlaneEvent.INTERSECTION:
            v2 = (p2.Point.x < CurrentX) ? p2.Start : p2.End;
            break;

            default:
            return p1.Type - p2.Type;
        }
        float epsilon = 1e-6f;
        float t = v1.y - v2.y;
        if (Math.Abs(t) > epsilon)
        {
            return (t > 0) ? 1 : -1;
        }
        return 0;
    }
};

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
        Add(p, new Color(Random.value, Random.value, Random.value, 1));
    }

    public void Add(PlaneEvent p, Color c)
    {
        try
        {
            mEventQ.Add(p);
            Debug.Log("Adding " + p.ToString());
            if (mLineMesh != null)
            {
                int vindex = p.Line.VertexIndex;
                if (vindex >= 0)
                {
                    mLineMesh.Update(vindex, c);
                }
                else
                {
                    p.Line.VertexIndex = mLineMesh.Add(p.Line, c);
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
                    p1.Line.VertexIndex = mLineMesh.Add(line, c);
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

    public void RemoveUsers(PlaneEvent e)
    {
        for (int i = 0; i < e.Line.Users.Count; ++i)
        {
            PlaneEvent p = e.Line.Users[i];
            if (p.Point.x < e.Point.x)
            {
                mEventQ.Remove(p);
                e.Line.Users.RemoveAt(i);
                --i;
            }
            if (mLineMesh != null)
            {
                int vindex = p.Line.VertexIndex;
                mLineMesh.Update(vindex, Color.black);
            }
        }
    }

    public void Remove(PlaneEvent e, bool updateMesh = false)
    {
        if (updateMesh)
        {
            Debug.Log("Deleting " + e);
            foreach (PlaneEvent p in e.Line.Users)
            {
                mEventQ.Remove(p);
            }
            e.Line.Users.Clear();
            if (mLineMesh != null)
            {
                int vindex = e.Line.VertexIndex;
                mLineMesh.Update(vindex, Color.black);
            }
        }
        else
        {
            Debug.Log("Removing " + e);
            mEventQ.Remove(e);
        }
    }

    public bool Process(LineEnumerator iter)
    {
        Vector3 currentPoint = iter.CurrentPoint;
        List<PlaneEvent> collected = iter.CollectAtPoint();
        VecCompare vc = new VecCompare();

        if (collected.Count > 1)
        {
            mIntersections.Add(currentPoint);
            if (mPointMesh != null)
            {
                mPointMesh.Add(currentPoint);
            }
            Debug.Log(String.Format("Intersection at {0}", currentPoint));
        }

        for (int i = 0; i < collected.Count; ++i)
        {
            PlaneEvent e = collected[i];

            if (e.Line.End == currentPoint)
            {
                Remove(e, true);
                collected.Remove(e);
            }
            else
            {
                Remove(e, false);
            }
        }
        mCompareEvents.CurrentX = currentPoint.x;
        Add(collected);

        Vector3 isect = new Vector3();
        PlaneEvent leftNeighbor = null;
        PlaneEvent rightNeighbor = null;

        if (collected.Count > 0)
        {
            PlaneEvent p = collected[0];
            LineSegment l = p.Line;

            leftNeighbor = iter.FindLeftNeighbor(p);
            if ((leftNeighbor != null) &&
                (leftNeighbor.FindIntersection(l, ref isect) > 0) &&
                (vc.Compare(isect, currentPoint) != 0))
            {
                PlaneEvent p1 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                             leftNeighbor.Line);
                PlaneEvent p2 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                            l);
                RemoveUsers(leftNeighbor);
                RemoveUsers(p);
                Add(p1);
                Add(p2);
            }
            p = collected[collected.Count - 1];
            l = p.Line;
            rightNeighbor = iter.FindRightNeighbor(p);
            if ((rightNeighbor != null) &&
                (rightNeighbor.FindIntersection(l, ref isect) > 0) &&
                (vc.Compare(isect, currentPoint) != 0))
            {
                PlaneEvent p1 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                             rightNeighbor.Line);
                PlaneEvent p2 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                            l);
                RemoveUsers(rightNeighbor);
                RemoveUsers(p);
                Add(p1);
                Add(p2);
            }
        }
        else 
        {
            leftNeighbor = iter.FindLeftNeighbor(currentPoint);
            rightNeighbor = iter.FindRightNeighbor(leftNeighbor);
            if ((leftNeighbor != null) && (rightNeighbor != null) &&
                (leftNeighbor.FindIntersection(rightNeighbor.Line, ref isect) > 0) &&
                (vc.Compare(isect, currentPoint) != 0))
            {
                PlaneEvent p1 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                              leftNeighbor.Line);
                PlaneEvent p2 = new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                               rightNeighbor.Line);
                RemoveUsers(rightNeighbor);
                RemoveUsers(leftNeighbor);
                Add(p1);
                Add(p2);
            }
        }
        if (mLineMesh != null)
        {
            if (leftNeighbor != null)
            {
                mLineMesh.Update(leftNeighbor.Line.VertexIndex, Color.green);
            }
            if (rightNeighbor != null)
            {
                mLineMesh.Update(rightNeighbor.Line.VertexIndex, Color.red);
            }
        }
        if (rightNeighbor == null)
        {
            return false;
        }
        iter.CurrentPoint = rightNeighbor.Point;
        return true;
    }
}
