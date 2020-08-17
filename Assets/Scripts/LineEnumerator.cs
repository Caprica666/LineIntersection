using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LineEnumerator : RBTree<PlaneEvent>.Enumerator
{
    private Vector3 mCurrentPoint;

    public LineEnumerator(LineGroup lines)
    : base(lines.Events)
    {
        RBTree<PlaneEvent>.Node node = stack.Peek();
        mCurrentPoint = node.Item.Point;
    }

    public Vector3 CurrentPoint
    {
        get { return mCurrentPoint; }
        set { mCurrentPoint = value; }
    }

    public override void Reset()
    {
        stack.Clear();
        Intialize();
        version = tree.Version;
    }

    public bool MoveNextPoint()
    {
        return (FindRightNeighbor(mCurrentPoint) != null);
    }

    public PlaneEvent FindRightNeighbor(Vector3 P)
    {
        VecCompare vcomparer = new VecCompare();
        int order;

        Reset();
        stack.Clear();
        stack.Push(Root);
        current = Root;
        while (stack.Count > 0)
        {
            current = stack.Peek();
            order = vcomparer.Compare(Current.Point, P);
            // P > current node, move right or stop
            if (order < 0)
            {
                if (current.Right != null)
                {
                    current = current.Right;
                    stack.Push(current);
                }
                else
                {
                    break;
                }
            }
            // P < current node, move left or move next
            else if (order > 0)
            {
                if (current.Left != null)
                {
                    current = current.Left;
                    stack.Push(current);
                }
                else
                {
                    return Current;
                }
            }
            else
            {
                break;
            }
        }
        while (MoveNext())
        {
            current = stack.Peek();
            order = vcomparer.Compare(Current.Point, P);
            if (order > 0)
            {
                return Current;
            }
        }
        return null;
    }

    public PlaneEvent FindRightNeighbor(PlaneEvent p)
    {
        VecCompare vcomparer = new VecCompare();
        int order;

        if (p == null)
        {
            return null;
        }
        Reset();
        stack.Clear();
        stack.Push(Root);
        current = Root;
        while (stack.Count > 0)
        {
            current = stack.Peek();
            order = vcomparer.Compare(Current.Point, p.Point);
            // P > current node, move right or stop
            if (order < 0)
            {
                if (current.Right != null)
                {
                    current = current.Right;
                    stack.Push(current);
                }
                else
                {
                    break;
                }
            }
            // P < current node, move left or move next
            else if (order > 0)
            {
                if (current.Left != null)
                {
                    current = current.Left;
                    stack.Push(current);
                }
                else
                {
                    return Current;
                }
            }
            else
            {
                break;
            }
        }
        while (MoveNext())
        {
            current = stack.Peek();
            order = vcomparer.Compare(Current.Point, p.Point);
            if ((order > 0) && (Current.Line != p.Line))
            {
                return Current;
            }
        }
        return null;
    }

    public PlaneEvent FindLeftNeighbor(Vector3 P)
    {
        VecCompare vcomparer = new VecCompare();
        int order;

        Reset();
        stack.Clear();
        stack.Push(Root);
        current = Root;
        while (stack.Count > 0)
        {
            current = stack.Peek();
            order = vcomparer.Compare(Current.Point, P);
            // P > current node, move right or stop
            if (order < 0)
            {
                if (current.Right != null)
                {
                    current = current.Right;
                    stack.Push(current);
                }
                else
                {
                    break;
                }
            }
            // P < current node, move left or move next
            else if (order > 0)
            {
                if (current.Left != null)
                {
                    current = current.Left;
                    stack.Push(current);
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
        while (MovePrev())
        {
            current = stack.Peek();
            order = vcomparer.Compare(Current.Point, P);
            if (order < 0)
            {
                return Current;
            }
        }
        return null;
    }

    public PlaneEvent FindLeftNeighbor(PlaneEvent p)
    {
        VecCompare vcomparer = new VecCompare();
        int order;

        if (p == null)
        {
            return null;
        }
        Reset();
        stack.Clear();
        stack.Push(Root);
        current = Root;
        while (stack.Count > 0)
        {
            current = stack.Peek();
            order = vcomparer.Compare(Current.Point, p.Point);
            // P > current node, move left or stop
            if (order < 0)
            {
                if (current.Right != null)
                {
                    current = current.Right;
                    stack.Push(current);
                }
                else
                {
                    break;
                }
            }
            // P < current node, move left or move next
            else if (order > 0)
            {
                if (current.Left != null)
                {
                    current = current.Left;
                    stack.Push(current);
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
        while (MovePrev())
        {
            order = vcomparer.Compare(Current.Point, p.Point);
            if ((order < 0) && (Current.Line != p.Line))
            {
                return Current;
            }
        }
        return null;
    }


    public RBTree<PlaneEvent>.Node MoveToPoint(Vector3 P)
    {
        VecCompare vcomparer = new VecCompare();
        int order;

        Reset();
        stack.Clear();
        stack.Push(Root);
        while (stack.Count > 0)
        {
            current = stack.Peek();
            order = vcomparer.Compare(current.Item.Point, P);
            // P > current node, move right
            if (order < 0)
            {
                if (current.Right != null)
                {
                    current = current.Right;
                    stack.Push(current);
                }
                else
                {
                    return null;
                }
            }
            // P <= current node, move left
            else if (order > 0)
            {
                if (current.Left != null)
                {
                    current = current.Left;
                    stack.Push(current);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return current;
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
            stack.Push(node);
            node = node.Right;
        }
        return true;
    }

    public List<PlaneEvent> CollectAtPoint()
    {
        RBTree<PlaneEvent>.Node pointRoot = MoveToPoint(mCurrentPoint);

        if (pointRoot == null)
        {
            return null;
        }
        List<PlaneEvent> collected = new List<PlaneEvent>();
        Stack<RBTree<PlaneEvent>.Node> s = new Stack<RBTree<PlaneEvent>.Node>(stack.Reverse());
        VecCompare vcomparer = new VecCompare();

        while (MovePrev())
        {
            int order = vcomparer.Compare(mCurrentPoint, Current.Point);
            if (order == 0)
            {
                collected.Add(Current);
            }
            else
            {
                break;
            }
        }
        stack = s;
        MoveNext();
        while (MoveNext())
        {
            int order = vcomparer.Compare(mCurrentPoint, Current.Point);
            if (order == 0)
            {
                collected.Add(Current);
            }
            else
            {
                break;
            }
        }
        return collected;
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

};
