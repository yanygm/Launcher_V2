using System;
using System.IO;
using KartRider.Common.Security;

namespace KartRider.Common.Network
{
    /// <summary>
    /// 会话帧编解码。
    /// 线格式（与客户端一致）：
    ///   加密： [4B header][payload][4B checksum]
    ///          header   = SIV ^ (payloadLen + 4) ^ 4164199944
    ///          checksum = SIV ^ (KRPacketCrypto.HashEncrypt 返回值) ^ 3388492432
    ///          SIV 每次发送后 += 21446425，回绕到 0 时置 1
    ///   明文： [4B payloadLen][payload]（仅握手首包使用）
    ///
    /// 与原实现的差异：帧长必须落在 (0, MaxPayload] 内，越界视为协议错误并断开，
    /// 从而消除"客户端给一个长度就 new byte[len-4]"造成的远程 OOM。
    /// </summary>
    public static class SessionFrame
    {
        /// <summary>单帧 payload 上限（1 MiB）。协议内最大包约 1 KB，留足余量。</summary>
        public const int MaxPayload = 1024 * 1024;

        public const uint HeaderKey = 4164199944u;
        public const uint ChecksumKey = 3388492432u;
        public const uint IvStep = 21446425u;

        public enum FrameStatus
        {
            NeedMoreData = 0,
            FrameReady = 1,
            ProtocolError = 2
        }

        /// <summary>
        /// 尝试从缓冲区头部取出一帧。
        /// 成功时 payload 为新数组（拥有权交给调用方），consumed 为应从缓冲区移除的字节数，
        /// 并按协议递增 riv。
        /// </summary>
        public static FrameStatus TryExtract(byte[] buffer, int available, ref uint riv, out byte[] payload, out int consumed, out bool checksumOk, out string error)
        {
            payload = null;
            consumed = 0;
            checksumOk = true;
            error = null;

            if (buffer == null || available < 4)
            {
                return FrameStatus.NeedMoreData;
            }

            uint rawHeader = BitConverter.ToUInt32(buffer, 0);
            long frameLength;
            int payloadLength;

            if (riv != 0)
            {
                uint declared = riv ^ rawHeader ^ HeaderKey;
                if (declared < 4 || declared > MaxPayload + 4)
                {
                    error = $"非法帧长 {declared}（riv=0x{riv:X8}）";
                    return FrameStatus.ProtocolError;
                }
                payloadLength = (int)declared - 4;
                frameLength = (long)declared + 4;
            }
            else
            {
                if (rawHeader > MaxPayload)
                {
                    error = $"非法明文帧长 {rawHeader}";
                    return FrameStatus.ProtocolError;
                }
                payloadLength = (int)rawHeader;
                frameLength = 4L + rawHeader;
            }

            if (available < frameLength)
            {
                return FrameStatus.NeedMoreData;
            }

            payload = new byte[payloadLength];
            if (payloadLength > 0)
            {
                Buffer.BlockCopy(buffer, 4, payload, 0, payloadLength);
            }

            if (riv != 0)
            {
                uint expected = riv ^ BitConverter.ToUInt32(buffer, (int)(frameLength - 4)) ^ ChecksumKey;
                uint actual = KRPacketCrypto.HashDecrypt(payload, (uint)payloadLength, riv);
                checksumOk = expected == actual;
                uint next = riv + IvStep;
                riv = next == 0 ? 1u : next;
            }

            consumed = (int)frameLength;
            return FrameStatus.FrameReady;
        }

        /// <summary>
        /// 生成一帧。返回的数组由调用方拥有（发送路径会复用内部缓冲，故这里总是新建）。
        /// 传入的 payload 会被原地加密（调用方应传入独占副本）。
        /// </summary>
        public static byte[] BuildFrame(byte[] payload, int length, ref uint siv)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            if (length < 0 || length > MaxPayload)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (siv != 0)
            {
                byte[] frame = new byte[length + 8];
                uint digest = KRPacketCrypto.HashEncrypt(payload, (uint)length, siv);
                Buffer.BlockCopy(BitConverter.GetBytes(siv ^ (uint)(length + 4) ^ HeaderKey), 0, frame, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(siv ^ digest ^ ChecksumKey), 0, frame, frame.Length - 4, 4);
                Buffer.BlockCopy(payload, 0, frame, 4, length);
                uint next = siv + IvStep;
                siv = next == 0 ? 1u : next;
                return frame;
            }
            else
            {
                byte[] frame = new byte[length + 4];
                Buffer.BlockCopy(BitConverter.GetBytes(length), 0, frame, 0, 4);
                Buffer.BlockCopy(payload, 0, frame, 4, length);
                return frame;
            }
        }
    }
}
