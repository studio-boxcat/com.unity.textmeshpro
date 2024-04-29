using System;
using System.Diagnostics;

namespace TMPro
{
    /// <summary>
    /// Structure used to track basic XML tags which are binary (on / off)
    /// </summary>
    public struct TMP_FontStyleStack
    {
        public byte bold;
        public byte italic;

        /// <summary>
        /// Clear the basic XML tag stack.
        /// </summary>
        public void Clear()
        {
            bold = 0;
            italic = 0;
        }

        public byte Add(FontStyles style)
        {
            switch (style)
            {
                case FontStyles.Bold:
                    bold++;
                    return bold;
                case FontStyles.Italic:
                    italic++;
                    return italic;
            }

            return 0;
        }

        public byte Remove(FontStyles style)
        {
            switch (style)
            {
                case FontStyles.Bold:
                    if (bold > 1)
                        bold--;
                    else
                        bold = 0;
                    return bold;
                case FontStyles.Italic:
                    if (italic > 1)
                        italic--;
                    else
                        italic = 0;
                    return italic;
            }

            return 0;
        }
    }


    /// <summary>
    /// Structure used to track XML tags of various types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Item count = {m_Count}")]
    public struct TMP_TextProcessingStack<T>
    {
        public T[] itemStack;
        public int index;

        T m_DefaultItem;
        int m_Capacity;
        int m_RolloverSize;
        int m_Count;

        const int k_DefaultCapacity = 4;


        /// <summary>
        /// Constructor to create a new item stack.
        /// </summary>
        /// <param name="stack"></param>
        public TMP_TextProcessingStack(T[] stack)
        {
            itemStack = stack;
            m_Capacity = stack.Length;
            index = 0;
            m_RolloverSize = 0;

            m_DefaultItem = default(T);
            m_Count = 0;
        }


        /// <summary>
        /// Constructor for a new item stack with the given capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public TMP_TextProcessingStack(int capacity)
        {
            itemStack = new T[capacity];
            m_Capacity = capacity;
            index = 0;
            m_RolloverSize = 0;

            m_DefaultItem = default(T);
            m_Count = 0;
        }


        public TMP_TextProcessingStack(int capacity, int rolloverSize)
        {
            itemStack = new T[capacity];
            m_Capacity = capacity;
            index = 0;
            m_RolloverSize = rolloverSize;

            m_DefaultItem = default(T);
            m_Count = 0;
        }


        /// <summary>
        ///
        /// </summary>
        public int Count
        {
            get { return m_Count; }
        }


        /// <summary>
        /// Returns the current item on the stack.
        /// </summary>
        public T current
        {
            get
            {
                if (index > 0)
                    return itemStack[index - 1];

                return itemStack[0];
            }
        }


        /// <summary>
        ///
        /// </summary>
        public int rolloverSize
        {
            get { return m_RolloverSize; }
            set
            {
                m_RolloverSize = value;

                //if (m_Capacity < m_RolloverSize)
                //    Array.Resize(ref itemStack, m_RolloverSize);
            }
        }


        /// <summary>
        /// Set stack elements to default item.
        /// </summary>
        /// <param name="stack">The stack of elements.</param>
        /// <param name="item"></param>
        internal static void SetDefault(TMP_TextProcessingStack<T>[] stack, T item)
        {
            for (int i = 0; i < stack.Length; i++)
                stack[i].SetDefault(item);
        }


        /// <summary>
        /// Function to clear and reset stack to first item.
        /// </summary>
        public void Clear()
        {
            index = 0;
            m_Count = 0;
        }


        /// <summary>
        /// Function to set the first item on the stack and reset index.
        /// </summary>
        /// <param name="item"></param>
        public void SetDefault(T item)
        {
            if (itemStack == null)
            {
                m_Capacity = k_DefaultCapacity;
                itemStack = new T[m_Capacity];
                m_DefaultItem = default(T);
            }

            itemStack[0] = item;
            index = 1;
        }


        /// <summary>
        /// Function to add a new item to the stack.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            if (index < itemStack.Length)
            {
                itemStack[index] = item;
                index += 1;
            }
        }


        /// <summary>
        /// Function to retrieve an item from the stack.
        /// </summary>
        /// <returns></returns>
        public T Remove()
        {
            index -= 1;

            if (index <= 0)
            {
                index = 1;
                return itemStack[0];

            }

            return itemStack[index - 1];
        }

        public void Push(T item)
        {
            if (index == m_Capacity)
            {
                m_Capacity *= 2;
                if (m_Capacity == 0)
                    m_Capacity = k_DefaultCapacity;

                Array.Resize(ref itemStack, m_Capacity);
            }

            itemStack[index] = item;

            if (m_RolloverSize == 0)
            {
                index += 1;
                m_Count += 1;
            }
            else
            {
                index = (index + 1) % m_RolloverSize;
                m_Count = m_Count < m_RolloverSize ? m_Count + 1 : m_RolloverSize;
            }

        }

        public T Pop()
        {
            if (index == 0 && m_RolloverSize == 0)
                return default(T);

            if (m_RolloverSize == 0)
                index -= 1;
            else
            {
                index = (index - 1) % m_RolloverSize;
                index = index < 0 ? index + m_RolloverSize : index;
            }

            T item = itemStack[index];
            itemStack[index] = m_DefaultItem;

            m_Count = m_Count > 0 ? m_Count - 1 : 0;

            return item;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
            if (index == 0)
                return m_DefaultItem;

            return itemStack[index - 1];
        }


        /// <summary>
        /// Function to retrieve the current item from the stack.
        /// </summary>
        /// <returns>itemStack <T></returns>
        public T CurrentItem()
        {
            if (index > 0)
                return itemStack[index - 1];

            return itemStack[0];
        }


        /// <summary>
        /// Function to retrieve the previous item without affecting the stack.
        /// </summary>
        /// <returns></returns>
        public T PreviousItem()
        {
            if (index > 1)
                return itemStack[index - 2];

            return itemStack[0];
        }
    }
}
