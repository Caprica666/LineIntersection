using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using UnityEngine;

public class PlaneEvent
{
    public static readonly int START = 1;
    public static readonly int END = 2;
    public static readonly int INTERSECTION = 3;

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
        if (type == START)
        {
            mPoint = segment.Start;
        }
        else if (type == END)
        {
            mPoint = segment.End;
        }
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

public class LineGroup
{
    private LineGroup mLeftChild = null;
    private LineGroup mRightChild = null;
    private PlaneEvent mEvent = null;
    private bool mCheckIntersections = false;

    public LineGroup()
    {
        mEvent = new PlaneEvent(PlaneEvent.START,
                            new LineSegment(new Vector3(0, 0, 0),
                                            new Vector3(0, 0, 0)));
    }

    public LineGroup(PlaneEvent e)
	{
        mEvent = e;
	}

    public void Clear()
    {
        mLeftChild = null;
        mRightChild = null;
        mEvent = new PlaneEvent(PlaneEvent.START,
                                new LineSegment(new Vector3(0, 0, 0),
                                                new Vector3(0, 0, 0)));
    }

    public void Add(LineSegment line)
    {
        Add(new PlaneEvent(PlaneEvent.START, line));
    }

    public void Add(PlaneEvent p)
    {
        Vector3 isect = new Vector3();
        PlaneEvent e;
        LineSegment line = p.Line;

        if (mCheckIntersections &&
            (mEvent.FindIntersection(p.Line, ref isect) == (int) LineSegment.ClipResult.INTERSECTING))
        {
            Add(new PlaneEvent(PlaneEvent.INTERSECTION, line));
        }
        if (line.Start.x > mEvent.Point.x)
        {
            if (mLeftChild == null)
            {
                e = new PlaneEvent(PlaneEvent.START, line);
                mLeftChild = new LineGroup(e);
                return;
            }
            else
            {
                mLeftChild.Add(p);
            }
        }
        else if (mRightChild == null)
        {
            e = new PlaneEvent(PlaneEvent.START, line);
            mRightChild = new LineGroup(e);
        }
        else
        {
            mRightChild.Add(p);
        }
    }

    public void FindIntersections(List<LineSegment> intersections, LineSegment line)
    {
        Vector3 isect = new Vector3();

        if (mEvent.FindIntersection(line, ref isect) == (int) LineSegment.ClipResult.INTERSECTING)
        {
            intersections.Add(mEvent.Line);
        }
        if (line.Start.x > mEvent.Point.x)
        {
            if (mLeftChild != null)
            {
                mLeftChild.FindIntersections(intersections, line);
            }
        }
        else
        {
            if (mRightChild != null)
            {
                mRightChild.FindIntersections(intersections, line);
            }
        }
    }

    public void FindIntersections(List<Vector3> intersections, float x)
    {
        if ((mEvent.Start.x > x) && (mEvent.End.x <= x))
        {
            float slope = (mEvent.End.y - mEvent.Start.y) /
                            (mEvent.End.x - mEvent.Start.x);
            float dx = x - mEvent.Start.x;
            float y = mEvent.Start.y + dx * slope;
            Vector3 v = new Vector3(x, y, 0);
            intersections.Add(v);
        }
        if (mLeftChild != null)
        {
            mLeftChild.FindIntersections(intersections, x);
        }
        if (mRightChild != null)
        {
            mRightChild.FindIntersections(intersections, x);
        }
    }
}
