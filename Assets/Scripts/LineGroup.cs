using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Transactions;
using UnityEditor.UIElements;
using UnityEngine;
using Random = UnityEngine.Random;

public class LineEvent
{
    public const int START = 1;
    public const int END = 2;
    public const int INTERSECTION = 3;
    public const int VERTICAL = 4;

    private List<Segment> mSegment = new List<Segment>();
    private Vector3 mPoint;

    public struct Segment
    {
        public int Type;
        public LineSegment Line;

        public Segment(int type, LineSegment line)
        {
            Type = type;
            Line = line;
        }

        public Vector3 Start
        {
            get { return Line.Start; }
            set
            {
                Line = new LineSegment(value, Line.End);
            }
        }

        public Vector3 End
        {
            get { return Line.End; }
            set
            {
                Line = new LineSegment(Line.Start, value);
            }
        }

        public int FindIntersection(LineSegment line2, ref Vector3 intersection)
        {
            return Line.FindIntersection(line2, ref intersection);
        }

        public override string ToString()
        {
            string s = null;

            switch (Type)
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
            s += Line.Start + " -> " + Line.End;
            return s;
        }
    }

    public LineEvent(Vector3 point)
    {
        mPoint = point;
    }

    public LineSegment First(float x)
    {
        if (mSegment.Count > 0)
        {
            return ((mPoint.x < x) ? mSegment[0] : mSegment[mSegment.Count - 1]).Line; 
        }
        return null;
    }

    public LineSegment Last(float x)
    {
        if (mSegment.Count > 0)
        {
            return ((mPoint.x >= x) ? mSegment[0] : mSegment[mSegment.Count - 1]).Line; 
        }
        return null;
    }

    public List<LineSegment> Lines
    {
        get
        {
            List<LineSegment> lines = new List<LineSegment>();

            lines.Capacity = mSegment.Count;
            foreach (Segment s in mSegment)
            {
                lines.Add(s.Line);
            }
            return lines;
        }
    }

    public List<Segment> Segments
    {
        get { return mSegment; }
    }

    public bool AddSegment(Segment s, LineMesh mesh = null)
    {
        int found = mSegment.IndexOf(s);

        if (found >= 0)
        {
            return false;
        }
        mSegment.Add(s);
        Sort();
        if (mesh != null)
        {
            int vindex = s.Line.VertexIndex;
            if (vindex < 0)
            {
                Color c = new Color(Random.value, Random.value, Random.value, 1);
                s.Line.VertexIndex = mesh.Add(s.Line, c);
            }
        }
        foreach (LineEvent p in s.Line.Users)
        {
            if (p.Point == mPoint)
            {
                return true;
            }
        }
        s.Line.AddUser(this);
        return true;
    }

    public bool MoveSegments(LineEvent src)
    {
        bool added = false;
        foreach (Segment s in src.mSegment)
        {
            added |= AddSegment(s);
        }
        return added;
    }

    public bool RemoveSegment(LineSegment l)
    {
        for (int i = 0; i < mSegment.Count; ++i)
        {
            Segment s = mSegment[i];
            if (s.Line == l)
            {
                mSegment.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public override string ToString()
    {
        string s = Point.ToString();

        foreach (Segment seg in mSegment)
        {
            s += seg;
        }
        return s;
    }

    public Vector3 Point
    {
        get { return mPoint; }
        set
        {
            mPoint = value;
        }
    }

    public void Sort()
    {
        mSegment.Sort(new SegCompare());
    }
}

public class SegCompare : Comparer<LineEvent.Segment>
{
    public bool Reverse = false;

    public SegCompare(bool rev = false)
    {
        Reverse = rev;
    }

    public override int Compare(LineEvent.Segment s1, LineEvent.Segment s2)
    {
        Vector3 v1;
        Vector3 v2;

        switch (s1.Type)
        {
            case LineEvent.START:
            case LineEvent.VERTICAL:
            case LineEvent.INTERSECTION:
            v1 = s1.Start;
            break;

            case LineEvent.END:
            v1 = s1.End;
            break;

            default:
            return s1.Type - s2.Type;
        }
        switch (s2.Type)
        {
            case LineEvent.START:
            case LineEvent.VERTICAL:
            case LineEvent.INTERSECTION:
            v2 = s2.Start;
            break;

            case LineEvent.END:
            v2 = s2.End;
            break;

            default:
            return s1.Type - s2.Type;
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

public class EventCompare : IComparer<LineEvent>
{
    int IComparer<LineEvent>.Compare(LineEvent p1, LineEvent p2)
    {
        VecCompare vcompare = new VecCompare();

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
    private SortedList<Vector3, Vector3> mVertices;
    private List<Vector3> mIntersections;
    private LineMesh mLineMesh = null;
    private PointMesh mPointMesh = null;
    private EventCompare mCompareEvents = new EventCompare();

    public LineGroup(LineMesh lmesh = null, PointMesh pmesh = null)
    {
        mLineMesh = lmesh;
        mPointMesh = pmesh;
        mEventQ = new RBTree<LineEvent>(mCompareEvents);
        mVertices = new SortedList<Vector3, Vector3>(new VecCompare());
    }

    public RBTree<LineEvent> Events
    {
        get { return mEventQ; }
    }

    public void Clear()
    {
        mEventQ = new RBTree<LineEvent>(mCompareEvents);
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

    public LineEvent Add(LineEvent linein)
    {
        try
        {
            LineEvent lineout = null;
            if (mEventQ.TryGetValue(linein, out lineout))
            {
                lineout.MoveSegments(linein);
                return lineout;
            }
            else
            {
                mEventQ.Add(linein);
                Debug.Log("Adding " + linein.Point.ToString());
            }
        }
        catch (ArgumentException ex)
        {
            Debug.LogWarning(String.Format("${0}, ${1} already added ${2}",
                                            linein.Point.x, linein.Point.y, ex.Message));
        }
        return linein;
    }

    public void AddLines(List<LineSegment> lines)
    {
        try
        {
            foreach (LineSegment line in lines)
            {
                LineEvent p1 = new LineEvent(line.Start);
                LineEvent p2 = new LineEvent(line.End);

                line.VertexIndex = -1;
                p1.AddSegment(new LineEvent.Segment(LineEvent.START, line), mLineMesh);
                p2.AddSegment(new LineEvent.Segment(LineEvent.END, line), mLineMesh);
                Add(p1);
                Add(p2);
                Debug.Log("Initial " + line.ToString());
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

    public void Remove(LineEvent e, bool updateMesh = false)
    {
        if (updateMesh)
        {
            Debug.Log("Deleting " + e);
            foreach (LineSegment l in e.Lines)
            {
                foreach (LineEvent p in l.Users)
                {
                    mEventQ.Remove(p);
                }
                l.Users.Clear();
                if (mLineMesh != null)
                {
                    int vindex = l.VertexIndex;
                    mLineMesh.Update(vindex, Color.black);
                }
            }
        }
        else
        {
            Debug.Log("Removing " + e);
            mEventQ.Remove(e);
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

    public bool Process(LineEnumerator iter)
    {
        Vector3 currentPoint = iter.CurrentPoint;
        LineEvent found = iter.CollectAtPoint();
        if (found == null)
        {
            return false;
        }
        List<LineEvent.Segment> collected = found.Segments;

        if (collected.Count > 1)
        {
            MarkIntersection(currentPoint);
        }
        for (int i = 0; i < collected.Count; ++i)
        {
            LineEvent.Segment s = collected[i];

            if (s.End == currentPoint)
            {
                Remove(found, true);
                collected.Remove(s);
            }
        }

        Vector3 isect = new Vector3();
        LineEvent left = iter.FindLeftNeighbor(currentPoint);
        LineEvent right = iter.FindRightNeighbor(currentPoint);
        LineSegment leftNeighbor = null;
        LineSegment rightNeighbor = null;
        LineEvent p1 = null;
        VecCompare vc = new VecCompare();

        if (left != null)
        {
            leftNeighbor = left.Last(currentPoint.x);
        }
        if (right != null)
        {
            rightNeighbor = right.First(currentPoint.x);
        }
        if (collected.Count > 0)
        {
            found.Sort();
            LineSegment l = found.First(currentPoint.x);

            if ((leftNeighbor != null) &&
                (leftNeighbor.FindIntersection(l, ref isect) > 0) &&
                (vc.Compare(isect, currentPoint) != 0))
            {
                p1 = new LineEvent(isect);
                p1.AddSegment(
                        new LineEvent.Segment(LineEvent.INTERSECTION, leftNeighbor),
                        mLineMesh);
                p1.AddSegment(
                        new LineEvent.Segment(LineEvent.INTERSECTION, l),
                        mLineMesh);
                Add(p1);
            }
            l = found.Last(currentPoint.x);
            if (rightNeighbor != null)
            {
                if ((rightNeighbor.FindIntersection(l, ref isect) > 0) &&
                    (vc.Compare(isect, currentPoint) != 0))
                {
                    if (p1 == null)
                    {
                        p1 = new LineEvent(isect);
                    }
                    p1.AddSegment(
                        new LineEvent.Segment(LineEvent.INTERSECTION, rightNeighbor),
                            mLineMesh);
                    p1.AddSegment(new LineEvent.Segment(LineEvent.INTERSECTION, l),
                            mLineMesh);
                    Add(p1);
                }
                /*
                 iter.CurrentPoint = right.Point;
                 if (s.End.x < right.Point.x)
                 {
                     iter.CurrentPoint = s.End;
                 }
              */
            }
        }
        else 
        {
            if (rightNeighbor != null)
            {
                if ((leftNeighbor != null) &&
                    (leftNeighbor.FindIntersection(rightNeighbor, ref isect) > 0) &&
                    (vc.Compare(isect, currentPoint) != 0))
                {
                    p1 = new LineEvent(isect);
                    p1.AddSegment(
                            new LineEvent.Segment(LineEvent.INTERSECTION, leftNeighbor),
                            mLineMesh);
                    p1.AddSegment(
                            new LineEvent.Segment(LineEvent.INTERSECTION, rightNeighbor),
                            mLineMesh);
                    Add(p1);
                }
            }
        }
        if (right != null)
        {
            iter.CurrentPoint = right.Point;
            if ((p1 != null) &&
                (p1.Point.x < right.Point.x))
            {
                if (p1.Point.x > currentPoint.x)
                {
                    iter.CurrentPoint = p1.Point;
                }
                else
                {
                    MarkIntersection(p1.Point);
                }
            }
            return true;
        }
        return false;
    }
}
