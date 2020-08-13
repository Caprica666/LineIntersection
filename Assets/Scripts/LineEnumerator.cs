using System;
using System.Collections.Generic;
using UnityEngine;

public class LineEnumerator : RBTree<PlaneEvent>.Enumerator
{
    private Vector3 mCurrentPoint;
    private PlaneEvent mFirst = null;
    private PlaneEvent mLeftNeighbor = null;
    private PlaneEvent mRightNeighbor = null;
    private List<PlaneEvent> mCollected = new List<PlaneEvent>();
    public LineEnumerator(LineGroup lines)
    : base(lines.Events)
    {
        mCurrentPoint = new Vector3(float.MinValue, float.MinValue, float.MinValue);
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

    public List<PlaneEvent> Collected
    {
        get { return mCollected; }
    }

    public override void Reset()
    {
        mCollected.Clear();
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
        mCollected.Clear();
        return (FindRightNeighbor(mCurrentPoint) != null);
    }

    public PlaneEvent FindRightNeighbor(Vector3 P)
    {
        VecCompare vcomparer = new VecCompare();
        int order = 0, prevorder = -1;

        stack.Clear();
        stack.Push(Root);
        mLeftNeighbor = null;

        current = Root;
        while (stack.Count > 0)
        {
            order = vcomparer.Compare(Current.Point, P);
            // P > current node, move right or stop
            if (order <= 0)
            {
                stack.Pop();
                mLeftNeighbor = Current;
                if (current.Right != null)
                {
                    // move right
                    current = current.Right;
                    stack.Push(current);
                    prevorder = order;
                }
                else
                {
                    prevorder = 0;
                    current = stack.Peek();
                }
            }
            // P < current node, move left or move next
            else
            {
                if ((prevorder == 0) ||
                    (current.Left == null))
                {
                    if (MoveNext())
                    {
                        return Current;
                    }
                    return null;
                }
                else
                {
                    current = current.Left;
                    stack.Push(current);
                    prevorder = order;
                }
            }
        }
        return null;
     }

    public PlaneEvent FindLeftNeighbor(Vector3 P)
    {
        VecCompare vcomparer = new VecCompare();
        int order = 0, prevorder = -1;

        stack.Clear();
        stack.Push(Root);
        mLeftNeighbor = null;

        current = Root;
        while (stack.Count > 0)
        {
            order = vcomparer.Compare(Current.Point, P);
            // P > current node, move right or next
            if (order < 0)
            {
                if ((prevorder == 0) ||
                    (current.Right == null))
                {
                    if (MovePrev())
                    {
                        return Current;
                    }
                    return null;
                }
                else
                {
                    current = current.Right;
                    stack.Push(current);
                    prevorder = order;
                }
            }
            // P <= current node, move left or move next
            else
            {
                stack.Pop();
                if (current.Left != null)
                {
                    // move left
                    current = current.Left;
                    stack.Push(current);
                    prevorder = order;
                }
                else
                {
                    prevorder = 0;
                    if (stack.Count > 0)
                    {
                        current = stack.Peek();
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
        return null;
    }

    public virtual bool MovePrev()
    {
        if (stack.Count == 0)
        {
            current = null;
            return false;
        }
        current = stack.Pop();
        RBTree<PlaneEvent>.Node node = current.Left;
        while (node != null)
        {
            stack.Push(node.Right);
            node = node.Right;
        }
        return true;
    }

    public List<LineSegment> AddFirst(PlaneEvent p)
    {
        List<LineSegment> lines = new List<LineSegment>();

        lines.Add(p.Line);
        mCollected.Clear();
        mCollected.Add(p);
        mCurrentPoint = p.Point;
        Debug.Log(String.Format("Current = {0}, Left neighbor = {1}", p,
                                (mLeftNeighbor != null) ? mLeftNeighbor.ToString() :
                                                          "none"));
        return lines;
    }

    public List<LineSegment> CollectAtPoint()
    {
        VecCompare vcomparer = new VecCompare();
        List<LineSegment> lines = AddFirst(Current);

        mRightNeighbor = null;
        while (MoveNext())
        {
            int order = vcomparer.Compare(mCurrentPoint, Current.Point);

            mRightNeighbor = Current;
            if (order != 0)
            {
                break;
            }
            if (!lines.Contains(Current.Line))
            {
                lines.Add(Current.Line);
            }
            mCollected.Add(Current);
            Debug.Log("Collected " + Current);
        }
        Debug.Log("Right neighbor = " +
                  ((mRightNeighbor != null) ? mRightNeighbor.ToString() : "none"));
        return lines;
    }

    public override string ToString()
    {
        String s = "";
        foreach (RBTree<PlaneEvent>.Node n in stack)
        {
            s += n.Item.ToString() + '\n';
        }
        return s;
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
