using Launcher.Library.IO;
using Launcher.Library.Security;
using Launcher.Library.Utilities;
using System.Net;
using System.Net.Sockets;

namespace Launcher.Library.Network
{
    public abstract class Session
    {
        private readonly Socket _socket;

        public uint _RIV;

        public uint _SIV;

        private const int DEFAULT_SIZE = 65536;

        private byte[] mBuffer = new byte[65536];

        private byte[] mSharedBuffer = new byte[65536];

        private int mCursor = 0;

        public int mDisconnected = 0;

        private LockFreeQueue<ByteArraySegment> mSendSegments = new LockFreeQueue<ByteArraySegment>();

        private int mSending = 0;

        private string _label = "";

        public string Label
        {
            get
            {
                return _label;
            }
        }

        private SocketAsyncEventArgs mReadEventArgs
        {
            get;
            set;
        }

        private SocketAsyncEventArgs mWriteEventArgs
        {
            get;
            set;
        }

        public Socket Socket
        {
            get
            {
                return _socket;
            }
        }

        public Session(Socket socket)
        {
            _socket = socket;
            mWriteEventArgs = new SocketAsyncEventArgs()
            {
                DisconnectReuseSocket = false
            };
            mWriteEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>((s, a) => EndSend(a));
            WaitForData();
        }

        public void Append(byte[] pBuffer)
        {
            Append(pBuffer, 0, pBuffer.Length);
        }

        public void Append(byte[] pBuffer, int pStart, int pLength)
        {
            try
            {
                if (mBuffer.Length - mCursor < pLength)
                {
                    int length = mBuffer.Length * 2;
                    while (length < mCursor + pLength)
                    {
                        length *= 2;
                    }
                    Array.Resize(ref mBuffer, length);
                }
                Buffer.BlockCopy(pBuffer, pStart, mBuffer, mCursor, pLength);
                mCursor += pLength;
            }
            catch
            {
            }
        }

        public void BeginReceive()
        {
            if (mDisconnected != 0 ? false : _socket.Connected)
            {
                try
                {
                    _socket.BeginReceive(mSharedBuffer, 0, 65536, SocketFlags.None, new AsyncCallback(EndReceive), _socket);
                }
                catch
                {
                    Disconnect();
                }
            }
            else
            {
                Disconnect();
            }
        }

        private void BeginSend()
        {
            ByteArraySegment next = mSendSegments.Next;
            try
            {
                if (next == null)
                {
                    mSendSegments.Dequeue();
                }
                else if (next.Buffer.Length >= next.Length)
                {
                    byte[] buffer = next.Buffer;
                    byte[] numArray = new byte[buffer.Length + (_SIV != 0 ? 8 : 4)];
                    if (_SIV != 0)
                    {
                        uint num = KRPacketCrypto.HashEncrypt(buffer, (uint)buffer.Length, _SIV);
                        Buffer.BlockCopy(BitConverter.GetBytes((int)(_SIV ^ (ulong)(buffer.Length + 4) ^ 4164199944)), 0, numArray, 0, 4);
                        Buffer.BlockCopy(BitConverter.GetBytes(_SIV ^ num ^ 3388492432), 0, numArray, numArray.Length - 4, 4);
                        _SIV += 21446425;
                        if (_SIV == 0)
                        {
                            _SIV = 1;
                        }
                    }
                    else
                    {
                        Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, numArray, 0, 4);
                    }
                    Buffer.BlockCopy(buffer, 0, numArray, 4, buffer.Length);
                    mWriteEventArgs.SetBuffer(numArray, 0, numArray.Length);
                    next = null;
                    try
                    {
                        if (!Socket.SendAsync(mWriteEventArgs))
                        {
                            EndSend(mWriteEventArgs);
                        }
                    }
                    catch (ObjectDisposedException objectDisposedException)
                    {
                        Console.WriteLine("[SOCKET ERR] {0}", objectDisposedException.ToString());
                        Disconnect();
                    }
                }
                else
                {
                    Console.WriteLine("[SOCKET ERR] Tried to send a packet that has a bufferlength value that is lower than the length: {0} {1}", next.Buffer.Length, next.Length);
                    mSendSegments.Dequeue();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("[SOCKET ERR] {0}", exception.ToString());
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (Interlocked.CompareExchange(ref mDisconnected, 1, 0) == 0)
            {
                OnDisconnect();
                try
                {
                    Socket.Shutdown(SocketShutdown.Both);
                    Socket.Close();
                }
                catch
                {
                }
                mWriteEventArgs.Completed -= new EventHandler<SocketAsyncEventArgs>((s, a) => EndSend(a));
                mWriteEventArgs.Dispose();
                mWriteEventArgs = null;
            }
        }

        private void EndReceive(IAsyncResult ar)
        {
            if (mDisconnected == 0)
            {
                try
                {
                    int num = 0;
                    try
                    {
                        num = _socket.EndReceive(ar);
                    }
                    catch
                    {
                        Disconnect();
                        return;
                    }
                    if (num > 0)
                    {
                        Append(mSharedBuffer, 0, num);
                        while (true)
                        {
                            if (mCursor >= 4)
                            {
                                uint num1 = BitConverter.ToUInt32(mBuffer, 0);
                                if (_RIV != 0)
                                {
                                    num1 = _RIV ^ num1 ^ 4164199944;
                                }
                                if ((ulong)mCursor >= num1 + 4)
                                {
                                    byte[] numArray = new byte[num1 - 4];
                                    Buffer.BlockCopy(mBuffer, 4, numArray, 0, (int)(num1 - 4));
                                    if (_RIV != 0)
                                    {
                                        if ((_RIV ^ BitConverter.ToUInt32(mBuffer, (int)num1) ^ 3388492432) != KRPacketCrypto.HashDecrypt(numArray, num1 - 4, _RIV))
                                        {
                                            Console.WriteLine("Different checksum while decrypting");
                                        }
                                        _RIV += 21446425;
                                        if (_RIV == 0)
                                        {
                                            _RIV = 1;
                                        }
                                    }
                                    mCursor = (int)(mCursor - (num1 + 4));
                                    if (mCursor > 0)
                                    {
                                        Buffer.BlockCopy(mBuffer, (int)(num1 + 4), mBuffer, 0, mCursor);
                                    }
                                    if (mDisconnected == 0)
                                    {
                                        using (InPacket inPacket = new InPacket(numArray))
                                        {
                                            OnPacket(inPacket);
                                        }
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        BeginReceive();
                    }
                    else
                    {
                        Disconnect();
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.ToString());
                    Disconnect();
                }
            }
        }

        private void EndSend(SocketAsyncEventArgs pArguments)
        {
            if (mDisconnected == 0)
            {
                try
                {
                    if (pArguments.BytesTransferred > 0)
                    {
                        if (mSendSegments.Next.Advance(pArguments.BytesTransferred))
                        {
                            mSendSegments.Dequeue();
                        }
                        if (mSendSegments.Next == null)
                        {
                            mSending = 0;
                        }
                        else
                        {
                            BeginSend();
                        }
                    }
                    else
                    {
                        if (pArguments.SocketError != SocketError.Success)
                        {
                            Console.WriteLine("Send Error: {0}", pArguments.SocketError);
                        }
                        Console.WriteLine("Disconnected session 1 {0}", pArguments.SocketError.ToString());
                        Disconnect();
                    }
                }
                catch
                {
                    Disconnect();
                }
            }
        }

        public string GetRemoteAddress()
        {
            string str;
            try
            {
                str = ((IPEndPoint)_socket.RemoteEndPoint).Address.ToString();
            }
            catch
            {
                str = "";
            }
            return str;
        }

        public IPEndPoint GetRemoteEndPoint()
        {
            IPEndPoint remoteEndPoint;
            try
            {
                remoteEndPoint = (IPEndPoint)_socket.RemoteEndPoint;
            }
            catch
            {
                remoteEndPoint = new IPEndPoint(0, 0);
            }
            return remoteEndPoint;
        }

        public abstract void OnDisconnect();

        public abstract void OnPacket(InPacket inPacket);

        public void Send(OutPacket pPacket)
        {
            Console.WriteLine((Constant.PacketName)BitConverter.ToUInt32(pPacket.ToArray(), 0) + "：" + BitConverter.ToString(pPacket.ToArray()).Replace("-", ""));
            try
            {
                if (mDisconnected == 0)
                {
                    mSendSegments.Enqueue(new ByteArraySegment(pPacket.ToArray(), true));
                    if (Interlocked.CompareExchange(ref mSending, 1, 0) == 0)
                    {
                        BeginSend();
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Disconnected session 11 {0}", exception.ToString());
                Disconnect();
            }
        }

        public void SendRaw(byte[] pBuffer)
        {
            if (mDisconnected == 0)
            {
                mSendSegments.Enqueue(new ByteArraySegment(pBuffer, false));
                if (Interlocked.CompareExchange(ref mSending, 1, 0) == 0)
                {
                    BeginSend();
                }
            }
        }

        public void WaitForData()
        {
            BeginReceive();
        }
    }
}
