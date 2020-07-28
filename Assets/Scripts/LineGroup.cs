using System;
using System.Collections.Generic;;
using System.Linq;
using UnityEngine;

public class PlaneEvent
{
    public static readonly int START = 1;
    public static readonly int END = 2;
    public static readonly int INTERSECTION = 3;
    public static readonly int HORIZONTAL = 4;

    private int mType;
    private LineSegment mSegment;
    private Vector3 mPoint;

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
        if (type == START)
        {
            mPoint = mSegment.Start;
        }
        else
        {
            mPoint = mSegment.End;
        }
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
}

public class LineCompare : IComparer<PlaneEvent>
{
    private float X;

    public LineCompare(float x)
    {
        X = x;
    }

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
    private LineGroup mLeftChild = null;
    private LineGroup mRightChild = null;
    private PlaneEvent mEvent = null;
    private SortedSet<PlaneEvent> mEventQ;
    private SortedList<Vector3, Vector3> mVertices;
    private List<PlaneEvent> mStartEvents = new List<PlaneEvent>();
    private List<PlaneEvent> mEndEvents = new List<PlaneEvent>();
    private List<PlaneEvent> mIsectEvents = new List<PlaneEvent>();
    private List<PlaneEvent> mCandidates = new List<PlaneEvent>();
    private List<Vector3> mIntersections;

    public LineGroup()
    {
        mEventQ = new SortedSet<PlaneEvent>();
        mVertices = new SortedList<Vector3, Vector3>(new VecCompare());
    }

    public void Clear()
    {
        mEventQ = new SortedSet<PlaneEvent>();
        mVertices = new SortedList<Vector3, Vector3>(new VecCompare());
    }

    public void Add(LineSegment line)
    {
        Add(new PlaneEvent(PlaneEvent.START, line));
    }

    public void Add(PlaneEvent p)
    {
        try
        {
            mEventQ.Add(p);
        }
        catch (ArgumentException ex)
        {
            Debug.LogWarning(String.Format("${0}, ${1] already added", p.Point.x, p.Point.y));
        }
    }

    public void AddLines(List<LineSegment> lines)
    {
        foreach (LineSegment line in lines)
        {
            PlaneEvent p = new PlaneEvent(PlaneEvent.START, line);
            mVertices.Add(p.Start, p.Start);
            mVertices.Add(p.End, p.End);
            Add(p);
            p = new PlaneEvent(PlaneEvent.END, line);
            Add(p);
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
                    mIsectEvents.Add(p);
                }
            }
            else if (prev != null)
            {
                Process(prev);
                mStartEvents.Clear();
                mCandidates.Clear();
                mEndEvents.Clear();
                prev = p;
            }
        }
        if (prev != null)
        {
            Process(prev);
        }
        return mIntersections;
    }

    public void Process(PlaneEvent p)
    {
        int vindex = mVertices.IndexOfKey(p.Point);

        if ((mCandidates.Count + mEndEvents.Count) > 1)
        {
            mEventQ.Add(new PlaneEvent(PlaneEvent.INTERSECTION,
                            new LineSegment(p.Point, p.End)));
            mIntersections.Add(p.Point);
        }
        foreach (PlaneEvent e0 in mEndEvents)
        {
            mEventQ.Remove(e0);
        }
        foreach (PlaneEvent e0 in mIsectEvents)
        {
            mEventQ.Remove(e0);
        }
        SortedSet<PlaneEvent> left = FindLeftNeighbor(p, vindex);
        SortedSet<PlaneEvent> right = FindRightNeighbor(p, vindex);
        Vector3 isect = new Vector3();

        if (mCandidates.Count > 0)
        {
            foreach (PlaneEvent e1 in mCandidates)
            {
                mEventQ.Remove(e1);
            }
            PlaneEvent e = mCandidates[0];
            foreach (PlaneEvent e2 in left)
            {
                if (e2.FindIntersection(e.Line, ref isect) > 0)
                {
                    mEventQ.Add(new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                            new LineSegment(isect, e2.End)));
                    mEventQ.Add(new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                               new LineSegment(isect, e.End)));
                    mIntersections.Add(p.Point);
                }
            }
            e = mCandidates[mCandidates.Count - 1];
            foreach (PlaneEvent e2 in right)
            {
                if (e2.FindIntersection(e.Line, ref isect) > 0)
                {
                    mEventQ.Add(new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                               new LineSegment(isect, e2.End)));
                    mEventQ.Add(new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                               new LineSegment(isect, e.End)));
                    mIntersections.Add(p.Point);
                }
            }
        }
        else
        {
            foreach (PlaneEvent e2 in left)
            {
                foreach (PlaneEvent e3 in right)
                {
                    if (e2.FindIntersection(e3.Line, ref isect) > 0)
                    {
                        mEventQ.Add(new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                                   new LineSegment(isect, e2.End)));
                        mEventQ.Add(new PlaneEvent(PlaneEvent.INTERSECTION, isect,
                                                   new LineSegment(isect, e3.End)));
                        mIntersections.Add(p.Point);
                    }
                }
            }
        }
    }

    public SortedSet<PlaneEvent> FindLeftNeighbor(PlaneEvent p, int vindex)
    {
        if (vindex == 0)
        {
            return new SortedSet<PlaneEvent>();
        }
        Vector3 v1 = mVertices.Values[vindex - 1];
        PlaneEvent p1 = new PlaneEvent(PlaneEvent.START,
                                       new LineSegment(v1, v1));
        PlaneEvent p2 = new PlaneEvent(PlaneEvent.INTERSECTION,
                                       new LineSegment(v1, v1));
        return mEventQ.GetViewBetween(p1, p2);
    }

    public SortedSet<PlaneEvent> FindRightNeighbor(PlaneEvent p, int vindex)
    {
        if (vindex >= (mEventQ.Count - 1))
        {
            return new SortedSet<PlaneEvent>();
        }
        Vector3 v1 = mVertices.Values[vindex + 1];
        PlaneEvent p1 = new PlaneEvent(PlaneEvent.INTERSECTION,
                                       new LineSegment(v1, v1));
        PlaneEvent p2 = new PlaneEvent(PlaneEvent.END,
                                       new LineSegment(v1, v1));
        return mEventQ.GetViewBetween(p1, p2);
    }

}
