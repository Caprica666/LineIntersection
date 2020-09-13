using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LineEnumerator : RBTree<LineSegment>.Enumerator
{
    public LineEnumerator(LineGroup lines)
    : base(lines.ActiveLines)
    {
    }

    public override void Reset()
    {
        stack.Clear();
        Intialize();
        version = tree.Version;
    }

    public LineSegment FindTopNeighbor(LineSegment l)
    {
        IComparer<LineSegment> comparer = tree.Comparer;
        int order;

        if (Root == null)
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
            order = comparer.Compare(Current, l);

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

    public LineSegment FindBottomNeighbor(LineSegment l)
    {
        IComparer<LineSegment> comparer = tree.Comparer;
        int order;

        if (Root == null)
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
            order = comparer.Compare(Current, l);
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

    public virtual bool MovePrev()
    {
        if (stack.Count == 0)
        {
            current = null;
            return false;
        }
        current = stack.Pop();
        RBTree<LineSegment>.Node node = current.Left;
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

    public RBTree<LineSegment>.Node FindNode(LineSegment item)
    {
        RBTree<LineSegment>.Node current = Root;
        IComparer<LineSegment> comparer = tree.Comparer;

        Reset();
        stack.Clear();
        while (current != null)
        {
            stack.Push(current);
            if (item == current.Item)
            {
                return current;
            }
            int order = comparer.Compare(item, current.Item);

            current = (order < 0) ? current.Left : current.Right;
        }
        return null;
    }

    public List<LineSegment> CollectAt(LineSegment l, Vector3 p)
    {
        RBTree<LineSegment>.Node outVal = FindNode(l);
        List<LineSegment> collected = new List<LineSegment>();
        if (outVal == null)
        {
            return collected;
        }
        Stack<RBTree<LineSegment>.Node> s = new Stack<RBTree<LineSegment>.Node>(stack.Reverse());
        MoveNext();
        while (MovePrev())
        {
            float t = p.y - Current.CalcY(p.x);
            if (Math.Abs(t) > LineSegment.EPSILON)
            {
                break;
            }
        }
        stack = s;
        while (MoveNext())
        {
            float t = p.y - Current.CalcY(p.x);
            if (Math.Abs(t) > LineSegment.EPSILON)
            {
                break;
            }
            collected.Add(Current);
        }
        return collected;
    }

    public override string ToString()
    {
        string s = "";
        foreach (RBTree<LineSegment>.Node n in stack)
        {
            s += n.Item.ToString() + '\n';
        }
        return s;
    }

};
