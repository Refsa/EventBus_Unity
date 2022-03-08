using System.Collections.Generic;

namespace Refsa.EventBus
{
    /// <summary>
    /// Handles storing and getting the handler for a specific message type
    /// </summary>
    public interface IResolver
    {
        MessageHandler<TMessage> GetHandler<TMessage>() where TMessage : IMessage;
    }
    
    /// <summary>
    /// Tracks the ID of each message type in a static context
    /// </summary>
    static class MessageType
    {
        /// <summary>
        /// the global message type counter
        /// </summary>
        static volatile int messageTypeCounter;
        /// <summary>
        /// Keeps track of the id assigned to each message type
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        public static class TypeID<TMessage> where TMessage : IMessage
        {
            static readonly int id = messageTypeCounter++;
            public static int ID => id;
        }
    }

    /// <summary>
    /// A resolver using a dictionary as an interal lookup
    /// 
    /// Optimized for space
    /// </summary>
    public class DictionaryResolver : IResolver
    {
        Dictionary<int, IHandler<IMessage>> resolvers;

        public DictionaryResolver()
        {
            resolvers = new Dictionary<int, IHandler<IMessage>>();
        }

        public MessageHandler<TMessage> GetHandler<TMessage>() where TMessage : IMessage
        {
            if (!resolvers.TryGetValue(MessageType.TypeID<TMessage>.ID, out var resolver))
            {
                resolver = new MessageHandler<TMessage>();
                resolvers.Add(MessageType.TypeID<TMessage>.ID, resolver);
            }

            return (MessageHandler<TMessage>)resolver;
        }
    }

    /// <summary>
    /// A resolver using a sparse set for internal lookup <br/>
    /// 
    /// Optimized for speed <br/>
    /// </summary>
    public class SparseSetResolver : IResolver
    {
        /// <summary>
        /// Keeps track of handlers for each message type
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        static class Resolver<TMessage> where TMessage : IMessage
        {
            static List<IHandler<IMessage>> handlers = new List<IHandler<IMessage>>();

            public static IHandler<IMessage> Get(int index, out bool created)
            {
                created = false;
                while (index >= handlers.Count)
                {
                    handlers.Add(null);
                }
                if (handlers[index] == null)
                {
                    handlers[index] = new MessageHandler<TMessage>();
                    created = true;
                }

                return handlers[index];
            }

            public static void Clear(int index)
            {
                if (index >= handlers.Count) return;
                handlers[index] = null;
            }
        }

        static readonly SparseSet resolverIndices;
        static int resolverCounter;
        static readonly List<System.Action<int>> resolverClears;
        static SparseSetResolver()
        {
            resolverIndices = new SparseSet(1);
            resolverClears = new List<System.Action<int>>();
        }

        int resolverIndex;
        int resolverCount;

        public SparseSetResolver()
        {
            resolverCount = resolverCounter++;
            resolverIndex = resolverIndices.Add(resolverCount);
        }

        ~SparseSetResolver()
        {
            resolverIndices.Remove(resolverCount);
            foreach (var clear in resolverClears)
            {
                clear.Invoke(resolverIndex);
            }
        }

        public MessageHandler<TMessage> GetHandler<TMessage>() where TMessage : IMessage
        {
            var handler = (MessageHandler<TMessage>)Resolver<TMessage>.Get(resolverIndex, out bool created);

            if (created)
            {
                resolverClears.Add(Resolver<TMessage>.Clear);
            }

            return handler;
        }
    }

    /// <summary>
    /// Resolver for storing handlers in the global scope <br/>
    /// 
    /// All handlers are shared between any instance of GlobalResolver
    /// </summary>
    public class GlobalResolver : IResolver
    {
        static class Resolver<TMessage> where TMessage : IMessage
        {
            static readonly MessageHandler<TMessage> handler = new MessageHandler<TMessage>();
            public static MessageHandler<TMessage> Handler => handler;
        }

        public MessageHandler<TMessage> GetHandler<TMessage>() where TMessage : IMessage
        {
            return Resolver<TMessage>.Handler;
        }
    }

    /// <summary>
    /// A very simple sparse set implementation
    /// </summary>
    public class SparseSet
    {
        int max;
        int count;
        int[] dense;
        int[] sparse;

        public SparseSet(int max)
        {
            this.max = max;
            this.count = 0;
            this.sparse = new int[max];
            this.dense = new int[max];
        }

        /// <summary>
        /// Adds a value to the set
        /// </summary>
        /// <param name="value"></param>
        /// <returns>The index of the value into the dense set</returns>
        public int Add(int value)
        {
            if (value >= max)
            {
                Grow();
            }

            if (value >= 0 && !Contains(value))
            {
                dense[count] = value;
                sparse[value] = count;
                count++;
                return count - 1;
            }

            return Get(value);
        }

        /// <summary>
        /// Get the index into the dense set of the value
        /// </summary>
        /// <param name="value"></param>
        /// <returns>dense set index or -1 if it's not in the set</returns>
        public int Get(int value)
        {
            if (value >= 0 && value < max && Contains(value))
            {
                return sparse[value];
            }

            return -1;
        }

        /// <summary>
        /// Removes a value from the set
        /// </summary>
        /// <param name="value"></param>
        public void Remove(int value)
        {
            if (Contains(value))
            {
                dense[sparse[value]] = dense[count - 1];
                sparse[dense[count - 1]] = sparse[value];
                count--;
            }
        }

        /// <summary>
        /// Checks if value is in the set
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Contains(int value)
        {
            if (value >= max || value < 0) return false;

            return sparse[value] < count && dense[sparse[value]] == value;
        }

        /// <summary>
        /// Resets internal index counter, does not clear the contents
        /// </summary>
        public void Clear()
        {
            count = 0;
        }

        void Grow()
        {
            max = (max + 1) * 2 + 1;
            var new_d = new int[max];
            var new_s = new int[max];
            dense.CopyTo(new_d, 0);
            sparse.CopyTo(new_s, 0);

            dense = new_d;
            sparse = new_s;
        }
    }
}