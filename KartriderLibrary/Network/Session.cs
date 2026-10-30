using KartRider.IO.Packet;
using KartRider.Logging;
using KartRider.Common.Security;
using KartRider.Common.Utilities;
using KartRider_PacketName;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace KartRider.Common.Network
{
    /// <summary>
    /// TCP 会话基类。
    ///
    /// 相对原实现的关键修改：
    ///   1. 收包加入帧长校验（SessionFrame），消除"客户端给长度就分配"导致的远程 OOM；
    ///   2. 每包不再生成十六进制字符串（改由 Log.Packet 控制，默认关闭）；
    ///   3. 发送缓冲复用，避免每包 new byte[n+8]；队列有上限，慢客户端直接断开；
    ///   4. 修复 mSending 标志在"队列已空"分支不复位导致的发送永久停滞；
    ///   5. 开启 TCP_NODELAY，去掉小包 40 ms 级延迟；
    ///   6. 断开时用保存的委托取消订阅（原实现用新 lambda，永远取消不掉）。
    /// </summary>
    public abstract class Session
    {
        private const int InitialReceiveBuffer = 65536;
        private const int MaxReceiveBuffer = 1024 * 1024;
        private const long MaxQueuedSendBytes = 8L * 1024 * 1024;

        private readonly Socket _socket;
        private readonly byte[] mSharedBuffer = new byte[65536];
        private readonly AsyncCallback mReceiveCallback;
        private readonly EventHandler<SocketAsyncEventArgs> mWriteHandler;

        private byte[] mBuffer = new byte[InitialReceiveBuffer];
        private int mCursor;

        private readonly LockFreeQueue<ByteArraySegment> mSendSegments = new LockFreeQueue<ByteArraySegment>();
        private int mSending;
        private long mQueuedBytes;
        private byte[] mSendBuffer = new byte[4096];
        private int mCurrentFrameLength;

        private SocketAsyncEventArgs mWriteEventArgs;

        private string _nickname = "";
        private string _label = "";

        public int mDisconnected;

        public event Action<Session> OnDisconnected;

        public uint RIV { get; set; }

        public uint SIV { get; set; }

        public long LastReceiveTime { get; set; }

        public string Label => _label;

        public string Nickname
        {
            get => _nickname;
            set
            {
                string old = _nickname;
                _nickname = value ?? "";
                if (!string.Equals(old, _nickname, StringComparison.Ordinal))
                {
                    OnNicknameChanged(old, _nickname);
                }
            }
        }

        public Socket Socket => _socket;

        protected Session(Socket socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            LastReceiveTime = Environment.TickCount64;
            mReceiveCallback = EndReceive;
            mWriteHandler = (s, a) => EndSend(a);

            try
            {
                _socket.NoDelay = true;
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 30);
                _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 5);
                _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
                _socket.SendBufferSize = 64 * 1024;
                _socket.ReceiveBufferSize = 64 * 1024;
            }
            catch
            {
                // 平台不支持时忽略，由应用层心跳兜底
            }

            mWriteEventArgs = new SocketAsyncEventArgs { DisconnectReuseSocket = false };
            mWriteEventArgs.Completed += mWriteHandler;
            WaitForData();
        }

        /// <summary>昵称变更钩子：用于维护 昵称→会话 索引。</summary>
        protected virtual void OnNicknameChanged(string oldNickname, string newNickname)
        {
        }

        public void Append(byte[] pBuffer)
        {
            if (pBuffer != null)
            {
                Append(pBuffer, 0, pBuffer.Length);
            }
        }

        public void Append(byte[] pBuffer, int pStart, int pLength)
        {
            if (pBuffer == null || pLength <= 0)
            {
                return;
            }
            if (mBuffer.Length - mCursor < pLength)
            {
                int target = mBuffer.Length;
                while (target < mCursor + pLength)
                {
                    target *= 2;
                }
                if (target > MaxReceiveBuffer)
                {
                    throw new InvalidOperationException("接收缓冲区超过上限");
                }
                Array.Resize(ref mBuffer, target);
            }
            Buffer.BlockCopy(pBuffer, pStart, mBuffer, mCursor, pLength);
            mCursor += pLength;
        }

        public void BeginReceive()
        {
            if (mDisconnected != 0)
            {
                return;
            }
            try
            {
                if (!_socket.Connected)
                {
                    Disconnect();
                    return;
                }
                _socket.BeginReceive(mSharedBuffer, 0, mSharedBuffer.Length, SocketFlags.None, mReceiveCallback, _socket);
            }
            catch (ObjectDisposedException)
            {
                Disconnect();
            }
            catch (SocketException ex)
            {
                Log.Debug($"[Session] 接收启动失败 {Nickname}: {ex.SocketErrorCode}");
                Disconnect();
            }
        }

        private void EndReceive(IAsyncResult ar)
        {
            if (mDisconnected != 0)
            {
                return;
            }

            int received;
            try
            {
                received = _socket.EndReceive(ar);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException ex)
            {
                Log.Debug($"[Session] 接收中断 {Nickname}: {ex.SocketErrorCode}");
                Disconnect();
                return;
            }
            catch (Exception ex)
            {
                Log.Warn($"[Session] 接收异常 {Nickname}", ex);
                Disconnect();
                return;
            }

            if (received <= 0)
            {
                Disconnect();
                return;
            }

            LastReceiveTime = Environment.TickCount64;
            Metrics.Inc(ref Metrics.TcpPacketsIn);
            Metrics.Inc(ref Metrics.TcpBytesIn, received);

            try
            {
                Append(mSharedBuffer, 0, received);
            }
            catch (Exception ex)
            {
                Metrics.Inc(ref Metrics.ProtocolErrors);
                Log.Warn($"[Session] 接收缓冲溢出 {Nickname}: {ex.Message}");
                Disconnect();
                return;
            }

            ProcessBuffer();

            if (mDisconnected == 0)
            {
                BeginReceive();
            }
        }

        private void ProcessBuffer()
        {
            while (mDisconnected == 0)
            {
                uint riv = RIV;
                SessionFrame.FrameStatus status = SessionFrame.TryExtract(
                    mBuffer, mCursor, ref riv, out byte[] payload, out int consumed, out bool checksumOk, out string error);

                if (status == SessionFrame.FrameStatus.NeedMoreData)
                {
                    break;
                }
                if (status == SessionFrame.FrameStatus.ProtocolError)
                {
                    Metrics.Inc(ref Metrics.ProtocolErrors);
                    string head = mCursor > 0
                        ? BitConverter.ToString(mBuffer, 0, Math.Min(16, mCursor)).Replace("-", " ")
                        : "";
                    Log.Warn($"[Session] 协议错误 {Nickname}({GetRemoteAddress()}) 缓冲={mCursor}B head=[{head}]: {error}");
                    Disconnect();
                    return;
                }

                RIV = riv;
                if (!checksumOk)
                {
                    Metrics.Inc(ref Metrics.ChecksumMismatch);
                    Log.Debug($"[Session] 校验和不匹配 {Nickname}");
                }

                mCursor -= consumed;
                if (mCursor > 0)
                {
                    Buffer.BlockCopy(mBuffer, consumed, mBuffer, 0, mCursor);
                }
                if (mCursor == 0 && mBuffer.Length > InitialReceiveBuffer)
                {
                    mBuffer = new byte[InitialReceiveBuffer]; // 长期空闲时释放大缓冲
                }

                try
                {
                    using InPacket inPacket = new InPacket(payload);
                    OnPacket(inPacket);
                }
                catch (Exception ex)
                {
                    // 单个处理器的缺陷不应踢掉玩家，只记录并继续。
                    // 日志必须带包名：处理器一旦抛异常就不会回包，客户端会一直等（表现为卡死），
                    // 没有包名时现场几乎无法定位。
                    Metrics.Inc(ref Metrics.HandlerErrors);
                    Log.Error($"[Session] 处理器异常 nick={Nickname} op={DescribeOpcode(payload)}: {ex.GetType().Name}: {ex.Message}", ex);
                }
            }
        }

        /// <summary>把帧首 4 字节解释成包名，仅用于日志。</summary>
        private static string DescribeOpcode(byte[] payload)
        {
            if (payload == null || payload.Length < 4)
            {
                return "?";
            }
            uint opcode = BitConverter.ToUInt32(payload, 0);
            return Enum.GetName(typeof(PacketName), opcode) ?? "0x" + opcode.ToString("X8");
        }

        private void BeginSend()
        {
            ByteArraySegment segment = mSendSegments.Next;
            if (segment == null)
            {
                Volatile.Write(ref mSending, 0);
                // 生产者可能刚入队：这里必须二次确认，否则会永久停发
                if (mSendSegments.Next != null && Interlocked.CompareExchange(ref mSending, 1, 0) == 0)
                {
                    BeginSend();
                }
                return;
            }

            try
            {
                int payloadLength = segment.Length;
                if (payloadLength <= 0 || payloadLength > SessionFrame.MaxPayload)
                {
                    mSendSegments.Dequeue();
                    Interlocked.Add(ref mQueuedBytes, -payloadLength);
                    Volatile.Write(ref mSending, 0);
                    return;
                }

                uint siv = SIV;
                int frameLength;
                if (siv != 0)
                {
                    frameLength = payloadLength + 8;
                    EnsureSendBuffer(frameLength);
                    uint digest = KRPacketCrypto.HashEncrypt(segment.Buffer, (uint)payloadLength, siv);
                    Buffer.BlockCopy(BitConverter.GetBytes(siv ^ (uint)(payloadLength + 4) ^ SessionFrame.HeaderKey), 0, mSendBuffer, 0, 4);
                    Buffer.BlockCopy(segment.Buffer, 0, mSendBuffer, 4, payloadLength);
                    Buffer.BlockCopy(BitConverter.GetBytes(siv ^ digest ^ SessionFrame.ChecksumKey), 0, mSendBuffer, frameLength - 4, 4);
                    uint next = siv + SessionFrame.IvStep;
                    SIV = next == 0 ? 1u : next;
                }
                else
                {
                    frameLength = payloadLength + 4;
                    EnsureSendBuffer(frameLength);
                    Buffer.BlockCopy(BitConverter.GetBytes(payloadLength), 0, mSendBuffer, 0, 4);
                    Buffer.BlockCopy(segment.Buffer, 0, mSendBuffer, 4, payloadLength);
                }

                mCurrentFrameLength = frameLength;
                mWriteEventArgs.SetBuffer(mSendBuffer, 0, frameLength);
                Metrics.Inc(ref Metrics.TcpPacketsOut);
                Metrics.Inc(ref Metrics.TcpBytesOut, frameLength);
                if (!_socket.SendAsync(mWriteEventArgs))
                {
                    EndSend(mWriteEventArgs);
                }
            }
            catch (ObjectDisposedException)
            {
                Disconnect();
            }
            catch (Exception ex)
            {
                Log.Warn($"[Session] 发送异常 {Nickname}", ex);
                Disconnect();
            }
        }

        private void EnsureSendBuffer(int size)
        {
            if (mSendBuffer.Length < size)
            {
                mSendBuffer = new byte[Math.Max(size, mSendBuffer.Length * 2)];
            }
        }

        private void EndSend(SocketAsyncEventArgs args)
        {
            if (mDisconnected != 0)
            {
                return;
            }

            try
            {
                if (args.BytesTransferred != mCurrentFrameLength || args.SocketError != SocketError.Success)
                {
                    Log.Debug($"[Session] 发送未完成 {Nickname}: {args.SocketError}, {args.BytesTransferred}/{mCurrentFrameLength}");
                    Disconnect();
                    return;
                }

                ByteArraySegment segment = mSendSegments.Next;
                if (segment != null)
                {
                    Interlocked.Add(ref mQueuedBytes, -segment.Length);
                    mSendSegments.Dequeue();
                }
                mCurrentFrameLength = 0;

                if (mSendSegments.Next == null)
                {
                    Volatile.Write(ref mSending, 0);
                    if (mSendSegments.Next != null && Interlocked.CompareExchange(ref mSending, 1, 0) == 0)
                    {
                        BeginSend();
                    }
                }
                else
                {
                    BeginSend();
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[Session] 发送完成处理异常 {Nickname}", ex);
                Disconnect();
            }
        }

        private void Enqueue(byte[] payload, bool ownsBuffer)
        {
            if (mDisconnected != 0 || payload == null || payload.Length == 0)
            {
                return;
            }

            long queued = Interlocked.Read(ref mQueuedBytes);
            if (queued + payload.Length > MaxQueuedSendBytes)
            {
                Metrics.Inc(ref Metrics.SlowClientDrops);
                Log.Warn($"[Session] 发送队列超限（{queued} 字节），断开慢客户端 {Nickname}");
                Disconnect();
                return;
            }

            Interlocked.Add(ref mQueuedBytes, payload.Length);
            mSendSegments.Enqueue(new ByteArraySegment(payload, ownsBuffer));
            if (Interlocked.CompareExchange(ref mSending, 1, 0) == 0)
            {
                BeginSend();
            }
        }

        public void Send(OutPacket pPacket)
        {
            if (pPacket == null)
            {
                return;
            }
            try
            {
                if (mDisconnected != 0 || !_socket.Connected)
                {
                    return;
                }
                byte[] payload = pPacket.ToArray();
                Log.Packet("S", Nickname, payload, payload.Length);
                Enqueue(payload, true);
            }
            catch (ObjectDisposedException)
            {
                Disconnect();
            }
            catch (Exception ex)
            {
                Log.Warn($"[Session] 序列化异常 {Nickname}", ex);
            }
        }

        /// <summary>发送已序列化的 payload（由调用方保证不被复用）。</summary>
        public void SendPayload(byte[] payload)
        {
            if (mDisconnected != 0 || payload == null || payload.Length == 0)
            {
                return;
            }
            Log.Packet("S", Nickname, payload, payload.Length);
            Enqueue(payload, true);
        }

        public void SendRaw(byte[] pBuffer)
        {
            if (pBuffer == null)
            {
                return;
            }
            byte[] copy = new byte[pBuffer.Length];
            Buffer.BlockCopy(pBuffer, 0, copy, 0, pBuffer.Length);
            Enqueue(copy, true);
        }

        public void Disconnect()
        {
            if (Interlocked.CompareExchange(ref mDisconnected, 1, 0) != 0)
            {
                return;
            }

            Metrics.Inc(ref Metrics.Disconnects);
            try
            {
                OnDisconnect();
            }
            catch (Exception ex)
            {
                Log.Warn($"[Session] OnDisconnect 异常 {Nickname}", ex);
            }

            try
            {
                OnDisconnected?.Invoke(this);
            }
            catch (Exception ex)
            {
                Log.Warn($"[Session] OnDisconnected 异常 {Nickname}", ex);
            }

            try
            {
                if (_socket.Connected)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
            }
            catch
            {
            }

            try
            {
                _socket.Close();
            }
            catch
            {
            }

            SocketAsyncEventArgs args = Interlocked.Exchange(ref mWriteEventArgs, null);
            if (args != null)
            {
                args.Completed -= mWriteHandler;
                args.Dispose();
            }
        }

        public string GetRemoteAddress()
        {
            try
            {
                return ((IPEndPoint)_socket.RemoteEndPoint).Address.ToString();
            }
            catch
            {
                return "";
            }
        }

        public IPEndPoint GetRemoteEndPoint()
        {
            try
            {
                return (IPEndPoint)_socket.RemoteEndPoint;
            }
            catch
            {
                return new IPEndPoint(0, 0);
            }
        }

        public abstract void OnDisconnect();

        public abstract void OnPacket(InPacket inPacket);

        public void WaitForData()
        {
            BeginReceive();
        }
    }
}
