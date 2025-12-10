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
    public struct TMP_TextProcessingStack<T>
    {
        public T[] itemStack;
        public int index;

        T m_DefaultItem;
        int m_Capacity;

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

            m_DefaultItem = default(T);
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

            m_DefaultItem = default(T);
        }


        /// <summary>
        /// Function to clear and reset stack to first item.
        /// </summary>
        public void Clear()
        {
            index = 0;
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

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
            return index == 0 ? m_DefaultItem : itemStack[index - 1];
        }
    }
}
