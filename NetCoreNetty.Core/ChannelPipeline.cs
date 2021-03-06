﻿using System;
using NetCoreNetty.Buffers;

namespace NetCoreNetty.Core
{
    internal class ChannelPipeline : IChannelPipeline
    {
        class TerminatorChannelHandler : ChannelHandlerBase
        {
            public override void Read(IChannelHandlerContext ctx, object message)
            {
                ctx.Read(message);
            }

            public override void Write(IChannelHandlerContext ctx, object message)
            {
                if (message is ByteBuf == false)
                {
                    throw new InvalidOperationException();
                }

                ctx.Channel.Write((ByteBuf)message);
            }
        }

        private IChannel _channel;

        private ChannelHandlerContext _terminatorHandlerContext;

        public ChannelPipeline(IChannel channel)
        {
            _channel = channel;
            _terminatorHandlerContext = CreateContext(
                "_firstHandlerContext",
                new TerminatorChannelHandler()
            );
        }

        public void StartReceiving()
        {
            _channel.StartRead(ChannelReadCallback);
        }

        public void StopReceiving()
        {
            _channel.StopRead();
        }

        public void AddFirst<TChannelHandler>(string name)
            where TChannelHandler : IChannelHandler, new()
        {
            IChannelHandler handler = new TChannelHandler();
            AddFirst(name, handler);
        }

        public void AddLast<TChannelHandler>(string name)
            where TChannelHandler : IChannelHandler, new()
        {
            IChannelHandler handler = new TChannelHandler();
            AddLast(name, handler);
        }

        public void AddLast(string name, IChannelHandlerProvider channelHandlerProvider)
        {
            IChannelHandler handler = channelHandlerProvider.GetHandler();
            AddLast(name, handler);
        }

        public void AddBefore<TChannelHandler>(string targetName, string name)
            where TChannelHandler : IChannelHandler, new()
        {
            IChannelHandler handler = new TChannelHandler();
            AddBefore(targetName, name, handler);
        }

        public void AddBefore(string targetName, string name, IChannelHandlerProvider channelHandlerProvider)
        {
            IChannelHandler handler = channelHandlerProvider.GetHandler();
            AddBefore(targetName, name, handler);
        }

        public void Replace<TChannelHandler>(string targetName, string name)
            where TChannelHandler : IChannelHandler, new()
        {
            IChannelHandler handler = new TChannelHandler();
            Replace(targetName, name, handler);
        }

        public void Replace(string targetName, string name, IChannelHandlerProvider channelHandlerProvider)
        {
            IChannelHandler handler = channelHandlerProvider.GetHandler();
            Replace(targetName, name, handler);
        }

        public void ChannelReadCallback(IChannel channel, ByteBuf byteBuf)
        {
            _terminatorHandlerContext.InvokeRead(byteBuf);
        }

        private void AddFirst(string name, IChannelHandler handler)
        {
            ChannelHandlerContext ctx = CreateContext(name, handler);

            ctx.LinkAfter(_terminatorHandlerContext);
        }

        private void AddLast(string name, IChannelHandler handler)
        {
            ChannelHandlerContext ctx = CreateContext(name, handler);

            ctx.LinkAfter(GetLastContext());
        }

        private void AddBefore(string targetName, string name, IChannelHandler handler)
        {
            ChannelHandlerContext ctx = CreateContext(name, handler);

            ChannelHandlerContext targetCtx = FindContextByName(targetName);
            ctx.LinkBefore(targetCtx);
        }

        private void Replace(string targetName, string name, IChannelHandler handler)
        {
            ChannelHandlerContext ctx = CreateContext(name, handler);

            ChannelHandlerContext ctxToReplace = FindContextByName(targetName);
            ctx.LinkReplace(ctxToReplace);
        }

        private ChannelHandlerContext CreateContext(string name, IChannelHandler handler)
        {
            var ctx = new ChannelHandlerContext(name, _channel, handler, this);
            return ctx;
        }

        private ChannelHandlerContext GetLastContext()
        {
            ChannelHandlerContext ctx = _terminatorHandlerContext;
            while (ctx.Next != null)
            {
                ctx = ctx.Next;
            }

            return ctx;
        }

        private ChannelHandlerContext FindContextByName(string name)
        {
            ChannelHandlerContext ctx = _terminatorHandlerContext.Next;
            while (!string.Equals(ctx.Name, name))
            {
                ctx = ctx.Next;
            }

            if (ctx == null)
            {
                throw new InvalidOperationException();
            }

            return ctx;
        }
    }
}