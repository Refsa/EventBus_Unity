﻿using System.Collections.Generic;

namespace Refsa.EventBus
{
    /// <summary>
    /// A simple message bus
    /// </summary>
    public class MessageBus
    {
        object locker = new object();
        IResolver resolver;

        /// <summary>
        /// Default message handler resolver is DictionaryResolver
        /// </summary>
        public MessageBus()
        {
            resolver = new DictionaryResolver();
        }

        public MessageBus(IResolver resolver)
        {
            this.resolver = resolver;
        }

        MessageHandler<TMessage> GetResolver<TMessage>() where TMessage : IMessage
        {
            lock(locker)
            {
                return resolver.GetResolver<TMessage>();
            }
        }

        /// <summary>
        /// Pub a message
        /// </summary>
        /// <param name="message"></param>
        /// <typeparam name="TMessage"></typeparam>
        public void Pub<TMessage>(in TMessage message) where TMessage : IMessage
        {
            GetResolver<TMessage>().Pub(message);
        }

        /// <summary>
        /// Pub a message to targets of specific type
        /// </summary>
        public void Pub<TMessage, HTarget>(in TMessage message) where TMessage : IMessage
        {
            GetResolver<TMessage>().Pub<HTarget>(message);
        }

        /// <summary>
        /// Pub a message to a specific object
        /// </summary>
        public void Pub<TMessage>(in TMessage message, object target) where TMessage : IMessage
        {
            GetResolver<TMessage>().Pub(message, target);
        }

        /// <summary>
        /// Sub to message
        /// </summary>
        /// <param name="callback"></param>
        /// <typeparam name="TMessage"></typeparam>
        public void Sub<TMessage>(MessageHandlerDelegates.MessageHandler<TMessage> callback) where TMessage : IMessage
        {
            var resolver = GetResolver<TMessage>();
            lock (locker)
            {
                resolver.Sub(callback);
            }
        }

        /// <summary>
        /// Unsub from message
        /// </summary>
        /// <param name="callback"></param>
        /// <typeparam name="TMessage"></typeparam>
        public void UnSub<TMessage>(MessageHandlerDelegates.MessageHandler<TMessage> callback) where TMessage : IMessage
        {
            var resolver = GetResolver<TMessage>();
            lock (locker)
            {
                resolver.UnSub(callback);
            }
        }
    }
}