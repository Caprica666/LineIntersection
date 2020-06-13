using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.XR.WSA.Input;



public class LineSegment
{
    public enum ClipResult
    {
        TOUCHING = 0,
        INTERSECTING = 1,
        NONINTERSECTING = -1
    }

    public LineSegment(Vector3 start, Vector3 end)
    {
        mStart = start;
        mEnd = end;
        if (mEnd.x > mStart.x)
        {
            mStart = end;
            mEnd = start;
        }
        else
        {
            mStart = start;
            mEnd = end;
        }
    }

    public Vector3 Start
    {
        get { return mStart; }
    }

    public Vector3 End
    {
        get { return mEnd; }
    }

    protected Vector3 mStart;
    protected Vector3 mEnd;
    public int VertexIndex = 0;

    /*
     * Find the intersection point between two line segments
     * @returns COINCIDENT lines are coincident
     *         NONINTERSECTING if lines segments do not intersect
     *         INTERSECTING if line segments intersect
     */
    public int FindIntersection(LineSegment line2, ref Vector3 intersection)
    {
        Vector3 A = End - Start; // s10
        Vector3 B = line2.End - line2.Start; // s32
        Vector3 C = Start - line2.Start; // s02
        float f = A.x * B.y - A.y * B.x;  // denom
        float e = A.x * C.y - A.y * C.x;  // snumer
        float d = B.x * C.y - B.y * C.x;  // tnumer
        bool denomPositive = (f > 0);

        if (f == 0)
        {
            return (int) ClipResult.TOUCHING;
        }
        if ((e < 0) == denomPositive)
        {
            return (int) ClipResult.NONINTERSECTING;
        }
        if ((d < 0) == denomPositive)
        {
            return (int) ClipResult.NONINTERSECTING;
        }
        // check to see if they are coincident
        if ((d == 0) || (d == f))
        {
            return (int) ClipResult.TOUCHING;
        }
        if (((e > f) == denomPositive) || ((d > f) == denomPositive))
        {
            return (int) ClipResult.NONINTERSECTING;
        }
        d /= f;
        intersection = Start + d * A;
        return (int) ClipResult.INTERSECTING;
    }
}