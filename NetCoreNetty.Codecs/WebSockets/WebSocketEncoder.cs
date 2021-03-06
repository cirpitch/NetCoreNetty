﻿using System;
using NetCoreNetty.Buffers;
using NetCoreNetty.Core;
using NetCoreNetty.Utils.ByteConverters;

namespace NetCoreNetty.Codecs.WebSockets
{
    // TODO: посмотреть в спецификации, требуется ли маска для серверного ответа (по-моему нет)
    public class WebSocketEncoder : MessageToByteEncoder<WebSocketFrame>
    {
        protected override ByteBuf Encode(IChannelHandlerContext ctx, WebSocketFrame message, out bool continueEncoding)
        {
            // TODO: временно
            continueEncoding = false;
            return EncodeFrame(ctx, message);
        }

        private ByteBuf EncodeFrame(IChannelHandlerContext ctx, WebSocketFrame frame)
        {
            // TODO: пулинг + нормальная реализация

            // TODO: т.к. вебсокеты работают с буферами, которые в итоге попадут прямо в канал передачи,
            // то логично сделать так, чтобы канал определял, какой конкретный экземпляр буфера алоцировать.

            // TODO: RemainBytes?
            int frameDataSize = frame.Bytes.Length;

            // TODO: примерно!
            if (frameDataSize > 65536)
            {
                throw new NotImplementedException("Big message is not supported.");
            }

            // TODO: Mask
            bool mask = false;
            int maskingKey = 0;

            int len =
                frameDataSize +
                2 /* headerSize */ +
                (mask ? 4 : 0) +
                (frameDataSize <= 125
                    ? 0
                    : (frameDataSize == 65536
                        ? 2
                        : 8));

            // TODO: разбиение по буферам, буферы фиксированного размера.
            ByteBuf byteBuf = ctx.ChannelByteBufAllocator.GetDefault();

            byte opCode = Utils.GetFrameOpCode(frame.Type);
            if (frame.IsFinal)
            {
                opCode = (byte)(opCode | Utils.MaskFin);
            }

            byteBuf.Write(opCode);

            byte payloadLenAndMask;

            if (frameDataSize <= 125)
            {
                payloadLenAndMask = (byte) frameDataSize;
            }
            else if (frameDataSize <= 65536)
            {
                payloadLenAndMask = 126;
            }
            else
            {
                // TODO: сюда пока что попасть не можем - вверху есть проверка и исключение
                payloadLenAndMask = 127;
            }

            byte payloadLen = payloadLenAndMask;

            if (mask)
            {
                payloadLenAndMask = (byte)(payloadLenAndMask | Utils.MaskMask);
            }

            byteBuf.Write(payloadLenAndMask);

            if (payloadLen == 126)
            {
                ByteUnion2 byteUnion2 = new ByteUnion2();
                byteUnion2.UShort = (ushort)frameDataSize;
                byteBuf.Write(byteUnion2.B2);
                byteBuf.Write(byteUnion2.B1);
            }
            else
            {
                // TODO: сюда пока что попасть не можем - вверху есть проверка и исключение
            }

            if (mask)
            {
                ByteUnion4 byteUnion4 = new ByteUnion4();
                byteUnion4.Int = maskingKey;
                byteBuf.Write(byteUnion4.B4);
                byteBuf.Write(byteUnion4.B3);
                byteBuf.Write(byteUnion4.B2);
                byteBuf.Write(byteUnion4.B1);
            }

            // TODO: Маска + оптимизация
            for (int i = 0; i < frameDataSize; i++)
            {
                byteBuf.Write(frame.Bytes[i]);
            }

            return byteBuf;
        }
    }
}