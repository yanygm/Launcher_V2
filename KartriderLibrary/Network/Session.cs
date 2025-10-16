using KartLibrary.IO;
using KartRider.Common.Security;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
using KartRider_PacketName;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

namespace KartRider.Common.Network
{
    public abstract class Session
    {
        private readonly System.Net.Sockets.Socket _socket;

        //public uint _RIV;

        //public uint _SIV;

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
                return this._label;
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

        public System.Net.Sockets.Socket Socket
        {
            get
            {
                return this._socket;
            }
        }

        public Session(System.Net.Sockets.Socket socket)
        {
            this._socket = socket;
            this.mWriteEventArgs = new SocketAsyncEventArgs()
            {
                DisconnectReuseSocket = false
            };
            this.mWriteEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>((object s, SocketAsyncEventArgs a) => this.EndSend(a));
            this.WaitForData();
        }

        public void Append(byte[] pBuffer)
        {
            this.Append(pBuffer, 0, (int)pBuffer.Length);
        }

        public void Append(byte[] pBuffer, int pStart, int pLength)
        {
            try
            {
                if ((int)this.mBuffer.Length - this.mCursor < pLength)
                {
                    int length = (int)this.mBuffer.Length * 2;
                    while (length < this.mCursor + pLength)
                    {
                        length *= 2;
                    }
                    Array.Resize<byte>(ref this.mBuffer, length);
                }
                Buffer.BlockCopy(pBuffer, pStart, this.mBuffer, this.mCursor, pLength);
                this.mCursor += pLength;
            }
            catch
            {
            }
        }

        public void BeginReceive()
        {
            if ((this.mDisconnected != 0 ? false : this._socket.Connected))
            {
                try
                {
                    this._socket.BeginReceive(this.mSharedBuffer, 0, 65536, SocketFlags.None, new AsyncCallback(this.EndReceive), this._socket);
                }
                catch
                {
                    this.Disconnect();
                }
            }
            else
            {
                this.Disconnect();
            }
        }

        private void BeginSend()
        {
            ByteArraySegment next = this.mSendSegments.Next;
            try
            {
                if (next == null)
                {
                    this.mSendSegments.Dequeue();
                }
                else if ((int)next.Buffer.Length >= next.Length)
                {
                    IPEndPoint clientEndPoint = this._socket.RemoteEndPoint as IPEndPoint;
                    var clientGroup = ClientManager.ClientGroups.FirstOrDefault(cg => cg.ClientID == ClientManager.GetClientId(clientEndPoint));
                    byte[] buffer = next.Buffer;
                    byte[] numArray = new byte[(int)buffer.Length + (clientGroup.SIV != 0 ? 8 : 4)];
                    if (clientGroup.SIV != 0)
                    {
                        uint num = KRPacketCrypto.HashEncrypt(buffer, (uint)buffer.Length, clientGroup.SIV);
                        Buffer.BlockCopy(BitConverter.GetBytes((int)((ulong)clientGroup.SIV ^ (ulong)((int)buffer.Length + 4) ^ (ulong)4164199944)), 0, numArray, 0, 4);
                        Buffer.BlockCopy(BitConverter.GetBytes(clientGroup.SIV ^ num ^ 3388492432), 0, numArray, (int)numArray.Length - 4, 4);
                        clientGroup.SIV += 21446425;
                        if (clientGroup.SIV == 0)
                        {
                            clientGroup.SIV = 1;
                        }
                    }
                    else
                    {
                        Buffer.BlockCopy(BitConverter.GetBytes((int)buffer.Length), 0, numArray, 0, 4);
                    }
                    Buffer.BlockCopy(buffer, 0, numArray, 4, (int)buffer.Length);
                    this.mWriteEventArgs.SetBuffer(numArray, 0, (int)numArray.Length);
                    next = null;
                    try
                    {
                        if (!this.Socket.SendAsync(this.mWriteEventArgs))
                        {
                            this.EndSend(this.mWriteEventArgs);
                        }
                    }
                    catch (ObjectDisposedException objectDisposedException)
                    {
                        Console.WriteLine("[SOCKET ERR] {0}", objectDisposedException.ToString());
                        this.Disconnect();
                    }
                }
                else
                {
                    Console.WriteLine("[SOCKET ERR] Tried to send a packet that has a bufferlength value that is lower than the length: {0} {1}", (int)next.Buffer.Length, next.Length);
                    this.mSendSegments.Dequeue();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("[SOCKET ERR] {0}", exception.ToString());
                this.Disconnect();
            }
        }

        public void Disconnect()
        {
            if (Interlocked.CompareExchange(ref this.mDisconnected, 1, 0) == 0)
            {
                this.OnDisconnect();
                try
                {
                    this.Socket.Shutdown(SocketShutdown.Both);
                    this.Socket.Close();
                    ClientManager.RemoveClient(this.Socket);
                }
                catch
                {
                }
                this.mWriteEventArgs.Completed -= new EventHandler<SocketAsyncEventArgs>((object s, SocketAsyncEventArgs a) => this.EndSend(a));
                this.mWriteEventArgs.Dispose();
                this.mWriteEventArgs = null;
            }
        }

        private void EndReceive(IAsyncResult ar)
        {
            if (this.mDisconnected == 0)
            {
                try
                {
                    int num = 0;
                    try
                    {
                        num = this._socket.EndReceive(ar);
                        if (num == 0)
                        {
                            // 读取到0字节，说明客户端主动断开
                            Console.WriteLine("客户端主动断开连接");
                            ClientManager.RemoveClient(this._socket);
                            this.Disconnect();
                            return;
                        }
                    }
                    catch
                    {
                        this.Disconnect();
                        return;
                    }
                    if (num > 0)
                    {
                        this.Append(this.mSharedBuffer, 0, num);
                        while (true)
                        {
                            if (this.mCursor >= 4)
                            {
                                IPEndPoint clientEndPoint = this._socket.RemoteEndPoint as IPEndPoint;
                                var clientGroup = ClientManager.ClientGroups.FirstOrDefault(cg => cg.ClientID == ClientManager.GetClientId(clientEndPoint));
                                uint num1 = BitConverter.ToUInt32(this.mBuffer, 0);
                                if (clientGroup.RIV != 0)
                                {
                                    num1 = clientGroup.RIV ^ num1 ^ 4164199944;
                                }
                                if ((ulong)this.mCursor >= (ulong)(num1 + 4))
                                {
                                    byte[] numArray = new byte[num1 - 4];
                                    Buffer.BlockCopy(this.mBuffer, 4, numArray, 0, (int)(num1 - 4));
                                    if (clientGroup.RIV != 0)
                                    {
                                        if ((clientGroup.RIV ^ BitConverter.ToUInt32(this.mBuffer, (int)num1) ^ 3388492432) != KRPacketCrypto.HashDecrypt(numArray, num1 - 4, clientGroup.RIV))
                                        {
                                            Console.WriteLine("Different checksum while decrypting");
                                        }
                                        clientGroup.RIV += 21446425;
                                        if (clientGroup.RIV == 0)
                                        {
                                            clientGroup.RIV = 1;
                                        }
                                    }
                                    this.mCursor = (int)(this.mCursor - (num1 + 4));
                                    if (this.mCursor > 0)
                                    {
                                        Buffer.BlockCopy(this.mBuffer, (int)(num1 + 4), this.mBuffer, 0, this.mCursor);
                                    }
                                    if (this.mDisconnected == 0)
                                    {
                                        using (InPacket inPacket = new InPacket(numArray))
                                        {
                                            this.OnPacket(inPacket);
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
                        this.BeginReceive();
                    }
                    else
                    {
                        this.Disconnect();
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.ToString());
                    this.Disconnect();
                }
            }
        }

        private void EndSend(SocketAsyncEventArgs pArguments)
        {
            if (this.mDisconnected == 0)
            {
                try
                {
                    if (pArguments.BytesTransferred > 0)
                    {
                        if (this.mSendSegments.Next.Advance(pArguments.BytesTransferred))
                        {
                            this.mSendSegments.Dequeue();
                        }
                        if (this.mSendSegments.Next == null)
                        {
                            this.mSending = 0;
                        }
                        else
                        {
                            this.BeginSend();
                        }
                    }
                    else
                    {
                        if (pArguments.SocketError != SocketError.Success)
                        {
                            Console.WriteLine("Send Error: {0}", pArguments.SocketError);
                        }
                        Console.WriteLine("Disconnected session 1 {0}", pArguments.SocketError.ToString());
                        this.Disconnect();
                    }
                }
                catch
                {
                    this.Disconnect();
                }
            }
        }

        public string GetRemoteAddress()
        {
            string str;
            try
            {
                str = ((IPEndPoint)this._socket.RemoteEndPoint).Address.ToString();
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
                remoteEndPoint = (IPEndPoint)this._socket.RemoteEndPoint;
            }
            catch
            {
                remoteEndPoint = new IPEndPoint((long)0, 0);
            }
            return remoteEndPoint;
        }

        public abstract void OnDisconnect();

        public abstract void OnPacket(InPacket inPacket);

        public void Send(OutPacket pPacket)
        {
            Console.WriteLine((PacketName)BitConverter.ToUInt32(pPacket.ToArray(), 0) + "：" + BitConverter.ToString(pPacket.ToArray()).Replace("-", ""));
            try
            {
                if (this.mDisconnected == 0)
                {
                    this.mSendSegments.Enqueue(new ByteArraySegment(pPacket.ToArray(), true));
                    if (Interlocked.CompareExchange(ref this.mSending, 1, 0) == 0)
                    {
                        this.BeginSend();
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Disconnected session 11 {0}", exception.ToString());
                this.Disconnect();
            }
        }

        public void SendRaw(byte[] pBuffer)
        {
            this.mSendSegments.Enqueue(new ByteArraySegment(pBuffer, false));
            if (Interlocked.CompareExchange(ref this.mSending, 1, 0) == 0)
            {
                this.BeginSend();
            }
        }

        public void WaitForData()
        {
            this.BeginReceive();
        }
    }
}
