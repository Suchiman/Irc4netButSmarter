/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2003-2009 Mirco Bauer <meebey@meebey.net> <http://www.meebey.net>
 * Copyright (c) 2008-2009 Thomas Bruderer <apophis@apophis.ch>
 * 
 * Full LGPL License: <http://www.gnu.org/licenses/lgpl.txt>
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace Meebey.SmartIrc4net
{
    public class IrcConnection
    {
        private int _CurrentAddress;
        private StreamReader _Reader;
        private StreamWriter _Writer;
        private ReadThread _ReadThread;
        private WriteThread _WriteThread;
        private IdleWorkerThread _IdleWorkerThread;
        private TcpClient _TcpClient;
        private ConcurrentQueue<string>[] _SendBuffer = new ConcurrentQueue<string>[5];
        private bool _IsConnectionError;

        public bool EnableUTF8Recode { get; set; }

        private Stopwatch PingStopwatch { get; set; }
        private Stopwatch NextPingStopwatch { get; set; }

        /// <event cref="OnReadLine">
        /// Raised when a \r\n terminated line is read from the socket
        /// </event>
        public event ReadLineEventHandler OnReadLine;
        /// <event cref="OnWriteLine">
        /// Raised when a \r\n terminated line is written to the socket
        /// </event>
        public event WriteLineEventHandler OnWriteLine;
        /// <event cref="OnConnect">
        /// Raised before the connect attempt
        /// </event>
        public event EventHandler OnConnecting;
        /// <event cref="OnConnect">
        /// Raised on successful connect
        /// </event>
        public event EventHandler OnConnected;
        /// <event cref="OnConnect">
        /// Raised before the connection is closed
        /// </event>
        public event EventHandler OnDisconnecting;
        /// <event cref="OnConnect">
        /// Raised when the connection is closed
        /// </event>
        public event EventHandler OnDisconnected;
        /// <event cref="OnConnectionError">
        /// Raised when the connection got into an error state
        /// </event>
        public event EventHandler OnConnectionError;
        /// <event cref="AutoConnectErrorEventHandler">
        /// Raised when the connection got into an error state during auto connect loop
        /// </event>
        public event AutoConnectErrorEventHandler OnAutoConnectError;

        /// <summary>
        /// When a connection error is detected this property will return true
        /// </summary>
        protected bool IsConnectionError
        {
            get => _IsConnectionError;
            set
            {
                _IsConnectionError = value;

                if (value)
                {
                    // signal ReadLine() to check IsConnectionError state
                    _ReadThread.QueuedEvent.Set();
                }
            }
        }

        protected bool IsDisconnecting { get; set; }

        /// <summary>
        /// Gets the current address of the connection
        /// </summary>
        public string Address => AddressList[_CurrentAddress];

        /// <summary>
        /// Gets the address list of the connection
        /// </summary>
        public string[] AddressList { get; private set; } = { "localhost" };

        /// <summary>
        /// Gets the used port of the connection
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// By default nothing is done when the library looses the connection
        /// to the server.
        /// Default: false
        /// </summary>
        /// <value>
        /// true, if the library should reconnect on lost connections
        /// false, if the library should not take care of it
        /// </value>
        public bool AutoReconnect { get; set; }

        /// <summary>
        /// If the library should retry to connect when the connection fails.
        /// Default: false
        /// </summary>
        /// <value>
        /// true, if the library should retry to connect
        /// false, if the library should not retry
        /// </value>
        public bool AutoRetry { get; set; }

        /// <summary>
        /// Delay between retry attempts in Connect() in seconds.
        /// Default: 30
        /// </summary>
        public int AutoRetryDelay { get; set; } = 30;

        /// <summary>
        /// Maximum number of retries to connect to the server
        /// Default: 3
        /// </summary>
        public int AutoRetryLimit { get; set; } = 3;

        /// <summary>
        /// Returns the current amount of reconnect attempts
        /// Default: 3
        /// </summary>
        public int AutoRetryAttempt { get; private set; }

        /// <summary>
        /// To prevent flooding the IRC server, it's required to delay each
        /// message, given in milliseconds.
        /// Default: 200
        /// </summary>
        public int SendDelay { get; set; } = 200;

        /// <summary>
        /// On successful registration on the IRC network, this is set to true.
        /// </summary>
        public bool IsRegistered { get; private set; }

        /// <summary>
        /// On successful connect to the IRC server, this is set to true.
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Gets the SmartIrc4net version number
        /// </summary>
        public string VersionNumber { get; }

        /// <summary>
        /// Gets the full SmartIrc4net version string
        /// </summary>
        public string VersionString { get; }

        /// <summary>
        /// The encoding to use to write to and read from the socket.
        ///
        /// If EnableUTF8Recode is true, reading and writing will always happen
        /// using UTF-8; this encoding is only used to decode incoming messages
        /// that cannot be successfully decoded using UTF-8.
        ///
        /// Default: encoding of the system
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.Default;

        /// <summary>
        /// Enables/disables using SSL for the connection
        /// Default: false
        /// </summary>
        public bool UseSsl { get; set; }

        /// <summary>
        /// Specifies if the certificate of the server is validated
        /// Default: true
        /// </summary>
        public bool ValidateServerCertificate { get; set; }

        /// <summary>
        /// Specifies the client certificate used for the SSL connection
        /// Default: null
        /// </summary>
        public X509Certificate SslClientCertificate { get; set; }

        /// <summary>
        /// Timeout in seconds for receiving data from the socket
        /// Default: 600
        /// </summary>
        public int SocketReceiveTimeout { get; set; } = 600;

        /// <summary>
        /// Timeout in seconds for sending data to the socket
        /// Default: 600
        /// </summary>
        public int SocketSendTimeout { get; set; } = 600;

        /// <summary>
        /// Interval in seconds to run the idle worker
        /// Default: 60
        /// </summary>
        public int IdleWorkerInterval { get; set; } = 60;

        /// <summary>
        /// Interval in seconds to send a PING
        /// Default: 60
        /// </summary>
        public int PingInterval { get; set; } = 60;

        /// <summary>
        /// Timeout in seconds for server response to a PING
        /// Default: 600
        /// </summary>
        public int PingTimeout { get; set; } = 300;

        /// <summary>
        /// Latency between client and the server
        /// </summary>
        public TimeSpan Lag => PingStopwatch.Elapsed;

        /// <summary>
        /// Initializes the message queues, read and write thread
        /// </summary>
        public IrcConnection()
        {
#if LOG4NET
            Logger.Main.Debug("IrcConnection created");
#endif
            _SendBuffer[(int)Priority.High] = new ConcurrentQueue<string>();
            _SendBuffer[(int)Priority.AboveMedium] = new ConcurrentQueue<string>();
            _SendBuffer[(int)Priority.Medium] = new ConcurrentQueue<string>();
            _SendBuffer[(int)Priority.BelowMedium] = new ConcurrentQueue<string>();
            _SendBuffer[(int)Priority.Low] = new ConcurrentQueue<string>();

            // setup own callbacks
            OnReadLine += _SimpleParser;
            OnConnectionError += _OnConnectionError;

            _ReadThread = new ReadThread(this);
            _WriteThread = new WriteThread(this);
            _IdleWorkerThread = new IdleWorkerThread(this);
            PingStopwatch = new Stopwatch();
            NextPingStopwatch = new Stopwatch();

            var assm = Assembly.GetAssembly(GetType());
            AssemblyName assm_name = assm.GetName(false);

            var pr = assm.GetCustomAttribute<AssemblyProductAttribute>();

            VersionNumber = assm_name.Version.ToString();
            VersionString = pr.Product + " " + VersionNumber;
        }

#if LOG4NET
        ~IrcConnection()
        {
            Logger.Main.Debug("IrcConnection destroyed");
        }
#endif

        /// <overloads>this method has 2 overloads</overloads>
        /// <summary>
        /// Connects to the specified server and port, when the connection fails
        /// the next server in the list will be used.
        /// </summary>
        /// <param name="addresslist">List of servers to connect to</param>
        /// <param name="port">Portnumber to connect to</param>
        /// <exception cref="CouldNotConnectException">The connection failed</exception>
        /// <exception cref="AlreadyConnectedException">If there is already an active connection</exception>
        public void Connect(string[] addresslist, int port)
        {
            if (IsConnected)
            {
                throw new AlreadyConnectedException("Already connected to: " + Address + ":" + Port);
            }

            AutoRetryAttempt++;
#if LOG4NET
            Logger.Connection.Info(String.Format("connecting... (attempt: {0})", _AutoRetryAttempt));
#endif

            AddressList = (string[])addresslist.Clone();
            Port = port;

            OnConnecting?.Invoke(this, EventArgs.Empty);
            try
            {
                _TcpClient = new TcpClient
                {
                    NoDelay = true
                };
                _TcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                // set timeout, after this the connection will be aborted
                _TcpClient.ReceiveTimeout = SocketReceiveTimeout * 1000;
                _TcpClient.SendTimeout = SocketSendTimeout * 1000;
                _TcpClient.Connect(Address, port);

                Stream stream = _TcpClient.GetStream();
                if (UseSsl)
                {
                    RemoteCertificateValidationCallback certValidation;
                    if (ValidateServerCertificate)
                    {
                        certValidation = ServicePointManager.ServerCertificateValidationCallback;
                        if (certValidation == null)
                        {
                            certValidation = delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                            {
                                if (sslPolicyErrors == SslPolicyErrors.None)
                                {
                                    return true;
                                }

#if LOG4NET
                                Logger.Connection.Error("Connect(): Certificate error: " + sslPolicyErrors);
#endif
                                return false;
                            };
                        }
                    }
                    else
                    {
                        certValidation = delegate { return true; };
                    }

                    bool certValidationWithIrcAsSender(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                    {
                        return certValidation(this, certificate, chain, sslPolicyErrors);
                    }
                    X509Certificate selectionCallback(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
                    {
                        return localCertificates?.Count > 0 ? localCertificates[0] : null;
                    }

                    var sslStream = new SslStream(stream, false, certValidationWithIrcAsSender, selectionCallback);
                    try
                    {
                        if (SslClientCertificate != null)
                        {
                            var certs = new X509Certificate2Collection
                            {
                                SslClientCertificate
                            };
                            sslStream.AuthenticateAsClient(Address, certs, SslProtocols.Default, false);
                        }
                        else
                        {
                            sslStream.AuthenticateAsClient(Address);
                        }
                    }
                    catch (IOException ex)
                    {
#if LOG4NET
                        Logger.Connection.Error("Connect(): AuthenticateAsClient() failed!");
#endif
                        throw new CouldNotConnectException("Could not connect to: " + Address + ":" + Port + " " + ex.Message, ex);
                    }
                    stream = sslStream;
                }
                if (EnableUTF8Recode)
                {
                    _Reader = new StreamReader(stream, new PrimaryOrFallbackEncoding(new UTF8Encoding(false, true), Encoding));
                    _Writer = new StreamWriter(stream, new UTF8Encoding(false, false));
                }
                else
                {
                    _Reader = new StreamReader(stream, Encoding);
                    _Writer = new StreamWriter(stream, Encoding);

                    if (Encoding.GetPreamble().Length > 0)
                    {
                        // HACK: we have an encoding that has some kind of preamble
                        // like UTF-8 has a BOM, this will confuse the IRCd!
                        // Thus we send a \r\n so the IRCd can safely ignore that
                        // garbage.
                        _Writer.WriteLine();
                        // make sure we flush the BOM+CRLF correctly
                        _Writer.Flush();
                    }
                }

                // Connection was succeful, reseting the connect counter
                AutoRetryAttempt = 0;

                // updating the connection error state, so connecting is possible again
                IsConnectionError = false;
                IsConnected = true;

                // lets power up our threads
                _ReadThread.Start();
                _WriteThread.Start();
                _IdleWorkerThread.Start();

#if LOG4NET
                Logger.Connection.Info("connected");
#endif
                OnConnected?.Invoke(this, EventArgs.Empty);
            }
            catch (AuthenticationException ex)
            {
#if LOG4NET
                Logger.Connection.Error("Connect(): Exception", ex);
#endif
                throw new CouldNotConnectException("Could not connect to: " + Address + ":" + Port + " " + ex.Message, ex);
            }
            catch (Exception e)
            {
                try
                {
                    _Reader?.Close();
                }
                catch (ObjectDisposedException)
                {
                }
                try
                {
                    _Writer?.Close();
                }
                catch (ObjectDisposedException)
                {
                }

                _TcpClient?.Close();
                IsConnected = false;
                IsConnectionError = true;

#if LOG4NET
                Logger.Connection.Info("connection failed: "+e.Message, e);
#endif
                if (e is CouldNotConnectException)
                {
                    // error was fatal, bail out
                    throw;
                }

                if (AutoRetry &&
                    (AutoRetryLimit == -1 ||
                     AutoRetryLimit == 0 ||
                     AutoRetryLimit <= AutoRetryAttempt))
                {
                    OnAutoConnectError?.Invoke(this, new AutoConnectErrorEventArgs(Address, Port, e));
#if LOG4NET
                    Logger.Connection.Debug("delaying new connect attempt for "+_AutoRetryDelay+" sec");
#endif
                    Thread.Sleep(AutoRetryDelay * 1000);
                    _NextAddress();
                    // FIXME: this is recursion
                    Connect(AddressList, Port);
                }
                else
                {
                    throw new CouldNotConnectException("Could not connect to: " + Address + ":" + Port + " " + e.Message, e);
                }
            }
        }

        /// <summary>
        /// Connects to the specified server and port.
        /// </summary>
        /// <param name="address">Server address to connect to</param>
        /// <param name="port">Port number to connect to</param>
        public void Connect(string address, int port) => Connect(new string[] { address }, port);

        /// <summary>
        /// Reconnects to the server
        /// </summary>
        /// <exception cref="NotConnectedException">
        /// If there was no active connection
        /// </exception>
        /// <exception cref="CouldNotConnectException">
        /// The connection failed
        /// </exception>
        /// <exception cref="AlreadyConnectedException">
        /// If there is already an active connection
        /// </exception>
        public void Reconnect()
        {
#if LOG4NET
            Logger.Connection.Info("reconnecting...");
#endif
            Disconnect();
            Connect(AddressList, Port);
        }

        /// <summary>
        /// Disconnects from the server
        /// </summary>
        /// <exception cref="NotConnectedException">
        /// If there was no active connection
        /// </exception>
        public void Disconnect()
        {
            if (!IsConnected)
            {
                throw new NotConnectedException("The connection could not be disconnected because there is no active connection");
            }

#if LOG4NET
            Logger.Connection.Info("disconnecting...");
#endif
            OnDisconnecting?.Invoke(this, EventArgs.Empty);

            IsDisconnecting = true;

            _IdleWorkerThread.RequestStop();
            _ReadThread.RequestStop();
            _WriteThread.RequestStop();
            _TcpClient.Close();
            IsConnected = false;
            IsRegistered = false;

            // signal ReadLine() to check IsConnected state
            _ReadThread.QueuedEvent.Set();

            IsDisconnecting = false;

            OnDisconnected?.Invoke(this, EventArgs.Empty);

#if LOG4NET
            Logger.Connection.Info("disconnected");
#endif
        }

        public void Listen(bool blocking)
        {
            if (blocking)
            {
                while (IsConnected)
                {
                    ReadLine(true);
                }
            }
            else
            {
                while (ReadLine(false).Length > 0)
                {
                    // loop as long as we receive messages
                }
            }
        }

        public void Listen() => Listen(true);

        public void ListenOnce(bool blocking) => ReadLine(blocking);

        public void ListenOnce() => ListenOnce(true);

        public string ReadLine(bool blocking)
        {
            string data = null;
            while (IsConnected && !IsConnectionError && !_ReadThread.Queue.TryDequeue(out data))
            {
                if (!blocking)
                {
                    break;
                }

                // block till the queue has data, but bail out on connection error
                _ReadThread.QueuedEvent.WaitOne();
            }

            if (!String.IsNullOrEmpty(data))
            {
#if LOG4NET
                Logger.Queue.Debug("read: \""+data+"\"");
#endif
                OnReadLine?.Invoke(this, new ReadLineEventArgs(data));
            }

            if (IsConnectionError && !IsDisconnecting)
            {
                OnConnectionError?.Invoke(this, EventArgs.Empty);
            }

            return data;
        }

        public void WriteLine(string data, Priority priority)
        {
            if (priority == Priority.Critical)
            {
                if (!IsConnected)
                {
                    throw new NotConnectedException();
                }

                _WriteLine(data);
            }
            else
            {
                _SendBuffer[(int)priority].Enqueue(data);
                _WriteThread.QueuedEvent.Set();
            }
        }

        public void WriteLine(string data) => WriteLine(data, Priority.Medium);

        private bool _WriteLine(string data)
        {
            if (IsConnected)
            {
                try
                {
                    lock (_Writer)
                    {
                        _Writer.Write(data + "\r\n");
                        _Writer.Flush();
                    }
                }
                catch (IOException)
                {
#if LOG4NET
                    Logger.Socket.Warn("sending data failed, connection lost");
#endif
                    IsConnectionError = true;
                    return false;
                }
                catch (ObjectDisposedException)
                {
#if LOG4NET
                    Logger.Socket.Warn("sending data failed (stream error), connection lost");
#endif
                    IsConnectionError = true;
                    return false;
                }

#if LOG4NET
                Logger.Socket.Debug("sent: \""+data+"\"");
#endif
                OnWriteLine?.Invoke(this, new WriteLineEventArgs(data));
                return true;
            }

            return false;
        }

        private void _NextAddress()
        {
            _CurrentAddress++;
            if (_CurrentAddress >= AddressList.Length)
            {
                _CurrentAddress = 0;
            }
#if LOG4NET
            Logger.Connection.Info("set server to: "+Address);
#endif
        }

        private void _SimpleParser(object sender, ReadLineEventArgs args)
        {
            string rawline = args.Line;
            string[] rawlineex = rawline.Split(' ');
            string line = null;
            string prefix = null;
            string command = null;

            if (rawline[0] == ':')
            {
                prefix = rawlineex[0].Substring(1);
                line = rawline.Substring(prefix.Length + 2);
            }
            else
            {
                line = rawline;
            }
            string[] lineex = line.Split(' ');

            command = lineex[0];
            ReplyCode replycode = ReplyCode.Null;
            if (Int32.TryParse(command, out int intReplycode))
            {
                replycode = (ReplyCode)intReplycode;
            }
            if (replycode != ReplyCode.Null)
            {
                switch (replycode)
                {
                    case ReplyCode.Welcome:
                        IsRegistered = true;
#if LOG4NET
                        Logger.Connection.Info("logged in");
#endif
                        break;
                }
            }
            else
            {
                switch (command)
                {
                    case "ERROR":
                        // FIXME: handle server errors differently than connection errors!
                        //IsConnectionError = true;
                        break;
                    case "PONG":
                        PingStopwatch.Stop();
                        NextPingStopwatch.Reset();
                        NextPingStopwatch.Start();

#if LOG4NET
                        Logger.Connection.Debug("PONG received, took: " + PingStopwatch.ElapsedMilliseconds + " ms");
#endif
                        break;
                }
            }
        }

        private void _OnConnectionError(object sender, EventArgs e)
        {
            try
            {
                if (AutoReconnect)
                {
                    // prevent connect -> exception -> connect flood loop
                    Thread.Sleep(AutoRetryDelay * 1000);
                    // lets try to recover the connection
                    Reconnect();
                }
                else
                {
                    // make sure we clean up
                    Disconnect();
                }
            }
            catch (ConnectionException)
            {
            }
        }

        private abstract class FiniteThread
        {
            public Thread Thread { get; internal set; }
            public bool IsStopRequested { get; internal set; }
            public string ThreadName { get; internal set; }
            public void RequestStop() => IsStopRequested = true;

            protected FiniteThread()
            {
                IsStopRequested = false;
                ThreadName = "(unnamed thread)";
            }

            protected abstract void PrepareStart();

            protected abstract void Worker();

            /// <summary>
            /// 
            /// </summary>
            public void Start()
            {
                PrepareStart();

                Thread = new Thread(Worker) { IsBackground = true, Name = ThreadName };
                Thread.Start();
            }
        }

        private class ReadThread : FiniteThread
        {
#if LOG4NET
            private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
            private readonly IrcConnection _Connection;
            public AutoResetEvent QueuedEvent;

            public ConcurrentQueue<string> Queue { get; private set; } = new ConcurrentQueue<string>();

            public ReadThread(IrcConnection connection)
            {
                _Connection = connection;
                QueuedEvent = new AutoResetEvent(false);
                ThreadName = "ReadThread (" + _Connection.Address + ":" + _Connection.Port + ")";
            }

            protected override void PrepareStart() { }

            protected override void Worker()
            {
#if LOG4NET
                Logger.Socket.Debug("ReadThread Worker(): starting");
#endif
                try
                {
                    try
                    {
                        while (!IsStopRequested && _Connection.IsConnected && _Connection._Reader.ReadLine() is string data)
                        {
                            Queue.Enqueue(data);
                            QueuedEvent.Set();
#if LOG4NET
                            Logger.Socket.Debug("received: \""+data+"\"");
#endif
                        }
#if LOG4NET
                        Logger.Socket.Debug("ReadThread Worker(): loop ended");
                        Logger.Socket.Debug("ReadThread Worker(): closing reader");
#endif
                        _Connection._Reader.Close();

                        // clean up our receive queue else we continue processing old
                        // messages when the read thread is restarted!
                        Queue = new ConcurrentQueue<string>();
                    }
                    catch (IOException)
                    {
#if LOG4NET
                        Logger.Socket.Warn("IOException: "+e.Message);
#endif
                    }
                    finally
                    {
#if LOG4NET
                        Logger.Socket.Warn("connection lost");
#endif
                        // only flag this as connection error if we are not
                        // cleanly disconnecting
                        if (!_Connection.IsDisconnecting)
                        {
                            _Connection.IsConnectionError = true;
                        }
                    }
                }
                catch (Exception)
                {
#if LOG4NET
                    Logger.Socket.Error(ex);
#endif
                }
            }
        }

        private class WriteThread : FiniteThread
        {
            private readonly IrcConnection _Connection;
            private int _HighCount;
            private int _AboveMediumCount;
            private int _MediumCount;
            private int _BelowMediumCount;
            private int _LowCount;
            private int _AboveMediumSentCount;
            private int _MediumSentCount;
            private int _BelowMediumSentCount;
            private readonly int _AboveMediumThresholdCount = 4;
            private readonly int _MediumThresholdCount = 2;
            private readonly int _BelowMediumThresholdCount = 1;
            private int _BurstCount;

            public AutoResetEvent QueuedEvent;

            public WriteThread(IrcConnection connection)
            {
                _Connection = connection;
                QueuedEvent = new AutoResetEvent(false);
                ThreadName = "WriteThread (" + _Connection.Address + ":" + _Connection.Port + ")";
            }

            protected override void PrepareStart() { }

            protected override void Worker()
            {
#if LOG4NET
                Logger.Socket.Debug("WriteThread Worker(): starting");
#endif
                try
                {
                    try
                    {
                        while (!IsStopRequested && _Connection.IsConnected)
                        {
                            QueuedEvent.WaitOne();
                            bool isBufferEmpty = false;
                            do
                            {
                                isBufferEmpty = _CheckBuffer() == 0;
                                Thread.Sleep(_Connection.SendDelay);
                            } while (!isBufferEmpty);
                        }
#if LOG4NET
                        Logger.Socket.Debug("WriteThread Worker(): loop ended");
                        Logger.Socket.Debug("WriteThread Worker(): closing writer");
#endif
                        _Connection._Writer.Close();
                    }
                    catch (IOException)
                    {
#if LOG4NET
                        Logger.Socket.Warn("IOException: " + e.Message);
#endif
                    }
                    finally
                    {
#if LOG4NET
                        Logger.Socket.Warn("connection lost");
#endif
                        // only flag this as connection error if we are not
                        // cleanly disconnecting
                        if (!_Connection.IsDisconnecting)
                        {
                            _Connection.IsConnectionError = true;
                        }
                    }
                }
                catch (Exception)
                {
#if LOG4NET
                    Logger.Socket.Error(ex);
#endif
                }
            }

            #region WARNING: complex scheduler, don't even think about changing it!
            // WARNING: complex scheduler, don't even think about changing it!
            private int _CheckBuffer()
            {
                _HighCount = _Connection._SendBuffer[(int)Priority.High].Count;
                _AboveMediumCount = _Connection._SendBuffer[(int)Priority.AboveMedium].Count;
                _MediumCount = _Connection._SendBuffer[(int)Priority.Medium].Count;
                _BelowMediumCount = _Connection._SendBuffer[(int)Priority.BelowMedium].Count;
                _LowCount = _Connection._SendBuffer[(int)Priority.Low].Count;

                int msgCount = _HighCount +
                               _AboveMediumCount +
                               _MediumCount +
                               _BelowMediumCount +
                               _LowCount;

                // only send data if we are succefully registered on the IRC network
                if (!_Connection.IsRegistered)
                {
                    return msgCount;
                }

                if (_CheckHighBuffer() &&
                    _CheckAboveMediumBuffer() &&
                    _CheckMediumBuffer() &&
                    _CheckBelowMediumBuffer() &&
                    _CheckLowBuffer())
                {
                    // everything is sent, resetting all counters
                    _AboveMediumSentCount = 0;
                    _MediumSentCount = 0;
                    _BelowMediumSentCount = 0;
                    _BurstCount = 0;
                }

                if (_BurstCount < 3)
                {
                    _BurstCount++;
                    //_CheckBuffer();
                }

                return msgCount;
            }

            private bool _CheckHighBuffer()
            {
                if (_HighCount > 0 && _Connection._SendBuffer[(int)Priority.High].TryDequeue(out string data))
                {
                    if (_Connection._WriteLine(data) == false)
                    {
#if LOG4NET
                        Logger.Queue.Warn("Sending data was not sucessful, data is requeued!");
#endif
                        _Connection._SendBuffer[(int)Priority.High].Enqueue(data);
                        return false;
                    }

                    if (_HighCount > 1)
                    {
                        // there is more data to send
                        return false;
                    }
                }

                return true;
            }

            private bool _CheckAboveMediumBuffer()
            {
                if (_AboveMediumCount > 0 && _AboveMediumSentCount < _AboveMediumThresholdCount && _Connection._SendBuffer[(int)Priority.AboveMedium].TryDequeue(out string data))
                {
                    if (_Connection._WriteLine(data) == false)
                    {
#if LOG4NET
                        Logger.Queue.Warn("Sending data was not sucessful, data is requeued!");
#endif
                        _Connection._SendBuffer[(int)Priority.AboveMedium].Enqueue(data);
                        return false;
                    }
                    _AboveMediumSentCount++;

                    if (_AboveMediumSentCount < _AboveMediumThresholdCount)
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool _CheckMediumBuffer()
            {
                if (_MediumCount > 0 && _MediumSentCount < _MediumThresholdCount && _Connection._SendBuffer[(int)Priority.Medium].TryDequeue(out string data))
                {
                    if (_Connection._WriteLine(data) == false)
                    {
#if LOG4NET
                        Logger.Queue.Warn("Sending data was not sucessful, data is requeued!");
#endif
                        _Connection._SendBuffer[(int)Priority.Medium].Enqueue(data);
                        return false;
                    }
                    _MediumSentCount++;

                    if (_MediumSentCount < _MediumThresholdCount)
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool _CheckBelowMediumBuffer()
            {
                if (_BelowMediumCount > 0 && _BelowMediumSentCount < _BelowMediumThresholdCount && _Connection._SendBuffer[(int)Priority.BelowMedium].TryDequeue(out string data))
                {
                    if (_Connection._WriteLine(data) == false)
                    {
#if LOG4NET
                        Logger.Queue.Warn("Sending data was not sucessful, data is requeued!");
#endif
                        _Connection._SendBuffer[(int)Priority.BelowMedium].Enqueue(data);
                        return false;
                    }
                    _BelowMediumSentCount++;

                    if (_BelowMediumSentCount < _BelowMediumThresholdCount)
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool _CheckLowBuffer()
            {
                if (_LowCount > 0)
                {
                    if ((_HighCount > 0) ||
                        (_AboveMediumCount > 0) ||
                        (_MediumCount > 0) ||
                        (_BelowMediumCount > 0) ||
                        !_Connection._SendBuffer[(int)Priority.Low].TryDequeue(out string data))
                    {
                        return true;
                    }

                    if (_Connection._WriteLine(data) == false)
                    {
#if LOG4NET
                        Logger.Queue.Warn("Sending data was not sucessful, data is requeued!");
#endif
                        _Connection._SendBuffer[(int)Priority.Low].Enqueue(data);
                        return false;
                    }

                    if (_LowCount > 1)
                    {
                        return false;
                    }
                }

                return true;
            }
            // END OF WARNING, below this you can read/change again ;)
            #endregion
        }

        private class IdleWorkerThread : FiniteThread
        {
            private readonly IrcConnection _Connection;

            public IdleWorkerThread(IrcConnection connection)
            {
                _Connection = connection;
                ThreadName = "IdleWorkerThread (" + _Connection.Address + ":" + _Connection.Port + ")";
            }

            protected override void PrepareStart()
            {
                _Connection.PingStopwatch.Reset();
                _Connection.NextPingStopwatch.Reset();
                _Connection.NextPingStopwatch.Start();
            }

            protected override void Worker()
            {
#if LOG4NET
                Logger.Socket.Debug("IdleWorkerThread Worker(): starting");
#endif
                try
                {
                    while (!IsStopRequested && _Connection.IsConnected)
                    {
                        Thread.Sleep(_Connection.IdleWorkerInterval * 1000);

                        // only send active pings if we are registered
                        if (!_Connection.IsRegistered)
                        {
                            continue;
                        }

                        int last_ping_sent = (int)_Connection.PingStopwatch.Elapsed.TotalSeconds;
                        int last_pong_rcvd = (int)_Connection.NextPingStopwatch.Elapsed.TotalSeconds;
                        // determins if the resoponse time is ok
                        if (last_ping_sent < _Connection.PingTimeout)
                        {
                            if (_Connection.PingStopwatch.IsRunning)
                            {
                                // there is a pending ping request, we have to wait
                                continue;
                            }

                            // determines if it need to send another ping yet
                            if (last_pong_rcvd > _Connection.PingInterval)
                            {
                                _Connection.NextPingStopwatch.Stop();
                                _Connection.PingStopwatch.Reset();
                                _Connection.PingStopwatch.Start();
                                _Connection.WriteLine(Rfc2812.Ping(_Connection.Address), Priority.Critical);
                            } // else connection is fine, just continue
                        }
                        else
                        {
                            if (_Connection.IsDisconnecting)
                            {
                                break;
                            }
#if LOG4NET
                            Logger.Socket.Warn("ping timeout, connection lost");
#endif
                            // only flag this as connection error if we are not
                            // cleanly disconnecting
                            _Connection.IsConnectionError = true;
                            break;
                        }
                    }
#if LOG4NET
                    Logger.Socket.Debug("IdleWorkerThread Worker(): loop ended");
#endif
                }
                catch (Exception)
                {
#if LOG4NET
                    Logger.Socket.Error(ex);
#endif
                }
            }
        }
    }
}
