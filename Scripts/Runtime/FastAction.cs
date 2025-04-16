using System.Collections.Generic;


namespace TMPro
{
    public class FastAction<A, B>
    {
        readonly LinkedList<System.Action<A, B>> delegates = new();
        readonly Dictionary<System.Action<A, B>, LinkedListNode<System.Action<A, B>>> lookup = new();

        public void Add(System.Action<A, B> rhs)
        {
            if (lookup.ContainsKey(rhs)) return;
            lookup[rhs] = delegates.AddLast(rhs);
        }

        public void Remove(System.Action<A, B> rhs)
        {
            if (lookup.Remove(rhs, out var node))
                delegates.Remove(node);
        }

        public void Call(A a, B b)
        {
            var node = delegates.First;
            while (node != null)
            {
                node.Value(a, b);
                node = node.Next;
            }
        }
    }


    public class FastAction<A, B, C>
    {
        readonly LinkedList<System.Action<A, B, C>> delegates = new();
        readonly Dictionary<System.Action<A, B, C>, LinkedListNode<System.Action<A, B, C>>> lookup = new();

        public void Add(System.Action<A, B, C> rhs)
        {
            if (lookup.ContainsKey(rhs)) return;
            lookup[rhs] = delegates.AddLast(rhs);
        }

        public void Remove(System.Action<A, B, C> rhs)
        {
            if (lookup.Remove(rhs, out var node))
                delegates.Remove(node);
        }

        public void Call(A a, B b, C c)
        {
            var node = delegates.First;
            while (node != null)
            {
                node.Value(a, b, c);
                node = node.Next;
            }
        }
    }
}