﻿using System.Collections.Generic;

namespace Refsa.EventBus
{
    public class EventBus
    {
        Dictionary<System.Type, IHandler<IMessage>> resolvers;

        public EventBus()
        {
            resolvers = new Dictionary<System.Type, IHandler<IMessage>>();
        }

        MessageHandler<TMessage, IMessage> GetResolver<TMessage>() where TMessage : IMessage
        {
            if (!resolvers.TryGetValue(typeof(TMessage), out var resolver))
            {
                resolver = new MessageHandler<TMessage, IMessage>();
                resolvers.Add(typeof(TMessage), resolver);
            }

            return (MessageHandler<TMessage, IMessage>)resolver;
        }

        public void Pub<TMessage>(in TMessage message) where TMessage : IMessage
        {
            GetResolver<TMessage>().Pub(message);
        }

        public void Pub<TMessage, HTarget>(in TMessage message) where TMessage : IMessage
        {
            GetResolver<TMessage>().Pub<HTarget>(message);
        }
 
        public void Pub<TMessage>(in TMessage message, object target) where TMessage : IMessage
        {
            GetResolver<TMessage>().Pub(message, target);
        }

        public void Sub<TMessage>(MessageHandlerDelegates.MessageHandler<TMessage> callback) where TMessage : IMessage
        {
            GetResolver<TMessage>().Sub(callback);
        }
        
        public void UnSub<TMessage>(MessageHandlerDelegates.MessageHandler<TMessage> callback) where TMessage : IMessage
        {
            GetResolver<TMessage>().UnSub(callback);
        }
    }
}