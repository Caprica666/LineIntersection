using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LineEnumerator : RBTree<LineEvent>.Enumerator
{
    private Vector3 mCurrentPoint;

    public LineEnumerator(LineGroup lines)
    : base(lines.Events)
    {
        RBTree<LineEvent>.Node node = stack.Peek();
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

    public LineEvent FindRightNeighbor(Vector3 P)
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
                stack.Pop();
                if (current.Right != null)
                {
                    stack.Push(current.Right);
                }
                else
                {
                    if (MoveNext())
                    {
                        return Current;
                    }
                    break;
                }
            }
            // P < current node, move left or move next
            else if (order > 0)
            {
                if (current.Left != null)
                {
                    stack.Push(current.Left);
                }
                else
                {
                    MoveNext();
                    return Current;
                }
            }
            else // P == current point
            {
                MoveNext();
                if (MoveNext())
                {
                    return Current;
                }
                break;
            }
        }
        return null;
    }

    public LineEvent FindLeftNeighbor(Vector3 P)
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
            // P >= current node, move right or stop
            if (order < 0)
            {
                if (current.Right != null)
                {
                    stack.Push(current.Right);
                }
                else
                {
                    if (MovePrev())
                    {
                        return Current;
                    }
                    break;
                }
            }
            // P < current node, move left or move prev
            else if (order > 0)
            {
                stack.Pop();
                if (current.Left != null)
                {
                    stack.Push(current.Left);
                }
                else
                {
                    if (MovePrev())
                    {
                        return Current;
                    }
                    break;
                }
            }
            else // P == current point
            {
                MovePrev();
                if (MovePrev())
                {
                    return Current;
                }
                break;
            }
        }
        return null;
    }

    public RBTree<LineEvent>.Node MoveToPoint(Vector3 P)
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
                    break;
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
        RBTree<LineEvent>.Node node = current.Left;
        if (node != null)
        {
            stack.Push(node);
            node = node.Right;
            while (node != null)
            {
                stack.Push(node);
                node = node.Right;
            }
        }
        return true;
    }

    public LineEvent CollectAtPoint()
    {
        RBTree<LineEvent>.Node pointRoot = MoveToPoint(mCurrentPoint);

        if (pointRoot == null)
        {
            return null;
        }
        return pointRoot.Item;
    }

    public override string ToString()
    {
        String s = "";
        foreach (RBTree<LineEvent>.Node n in stack)
        {
            s += n.Item.ToString() + '\n';
        }
        return s;
    }

};
