/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2003-2010, 2012-2014 Mirco Bauer <meebey@meebey.net>
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// This layer is an event driven high-level API with all features you could need for IRC programming.
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    public class IrcClient : IrcCommands
    {
        private int _CurrentNickname;
        private Dictionary<string, string> _AutoRejoinChannels = new Dictionary<string, string>();
        private bool _SupportNonRfc;
        private bool _SupportNonRfcLocked;
        private bool _MotdReceived;
        private readonly ReplyCode[] _ReplyCodes = (ReplyCode[])Enum.GetValues(typeof(ReplyCode));
        private ConcurrentDictionary<string, Channel> _Channels = new ConcurrentDictionary<string, Channel>(StringComparer.OrdinalIgnoreCase);
        private ConcurrentDictionary<string, IrcUser> _IrcUsers = new ConcurrentDictionary<string, IrcUser>(StringComparer.OrdinalIgnoreCase);
        private List<ChannelInfo> _ChannelList;
        private readonly object _ChannelListSyncRoot = new object();
        private AutoResetEvent _ChannelListReceivedEvent;
        private List<WhoInfo> _WhoList;
        private readonly object _WhoListSyncRoot = new object();
        private AutoResetEvent _WhoListReceivedEvent;
        private List<BanInfo> _BanList;
        private AutoResetEvent _BanListReceivedEvent;
        private List<BanInfo> _BanExceptList;
        private readonly object _BanExceptListSyncRoot = new object();
        private AutoResetEvent _BanExceptListReceivedEvent;
        private List<BanInfo> _InviteExceptList;
        private readonly object _InviteExceptListSyncRoot = new object();
        private AutoResetEvent _InviteExceptListReceivedEvent;

        private static Regex _ReplyCodeRegex = new Regex("^:?[^ ]+? ([0-9]{3}) .+$", RegexOptions.Compiled);
        private static Regex _PingRegex = new Regex("^PING :.*", RegexOptions.Compiled);
        private static Regex _ErrorRegex = new Regex("^ERROR :.*", RegexOptions.Compiled);
        private static Regex _ActionRegex = new Regex("^:?.*? PRIVMSG (.).* :" + "\x1" + "ACTION .*" + "\x1" + "$", RegexOptions.Compiled);
        private static Regex _CtcpRequestRegex = new Regex("^:?.*? PRIVMSG .* :" + "\x1" + ".*" + "\x1" + "$", RegexOptions.Compiled);
        private static Regex _MessageRegex = new Regex("^:?.*? PRIVMSG (.).* :.*$", RegexOptions.Compiled);
        private static Regex _CtcpReplyRegex = new Regex("^:?.*? NOTICE .* :" + "\x1" + ".*" + "\x1" + "$", RegexOptions.Compiled);
        private static Regex _NoticeRegex = new Regex("^:?.*? NOTICE (.).* :.*$", RegexOptions.Compiled);
        private static Regex _InviteRegex = new Regex("^:?.*? INVITE .* .*$", RegexOptions.Compiled);
        private static Regex _JoinRegex = new Regex("^:?.*? JOIN .*$", RegexOptions.Compiled);
        private static Regex _TopicRegex = new Regex("^:?.*? TOPIC .* :.*$", RegexOptions.Compiled);
        private static Regex _NickRegex = new Regex("^:?.*? NICK .*$", RegexOptions.Compiled);
        private static Regex _KickRegex = new Regex("^:?.*? KICK .* .*$", RegexOptions.Compiled);
        private static Regex _PartRegex = new Regex("^:?.*? PART .*$", RegexOptions.Compiled);
        private static Regex _ModeRegex = new Regex("^:?.*? MODE (.*) .*$", RegexOptions.Compiled);
        private static Regex _QuitRegex = new Regex("^:?.*? QUIT :.*$", RegexOptions.Compiled);
        private static readonly Regex _BounceMessageRegex = new Regex("^Try server (.+), port ([0-9]+)$", RegexOptions.Compiled);

        private ChannelModeMap ChannelModeMap { get; set; }

        public event EventHandler OnRegistered;
        public event PingEventHandler OnPing;
        public event PongEventHandler OnPong;
        public event IrcEventHandler OnRawMessage;
        public event ErrorEventHandler OnError;
        public event IrcEventHandler OnErrorMessage;
        public event JoinEventHandler OnJoin;
        public event NamesEventHandler OnNames;
        public event ListEventHandler OnList;
        public event PartEventHandler OnPart;
        public event QuitEventHandler OnQuit;
        public event KickEventHandler OnKick;
        public event AwayEventHandler OnAway;
        public event IrcEventHandler OnUnAway;
        public event IrcEventHandler OnNowAway;
        public event InviteEventHandler OnInvite;
        public event BanEventHandler OnBan;
        public event UnbanEventHandler OnUnban;
        public event BanEventHandler OnBanException;
        public event UnbanEventHandler OnUnBanException;
        public event BanEventHandler OnInviteException;
        public event UnbanEventHandler OnUnInviteException;
        public event OwnerEventHandler OnOwner;
        public event DeownerEventHandler OnDeowner;
        public event ChannelAdminEventHandler OnChannelAdmin;
        public event DeChannelAdminEventHandler OnDeChannelAdmin;
        public event OpEventHandler OnOp;
        public event DeopEventHandler OnDeop;
        public event HalfopEventHandler OnHalfop;
        public event DehalfopEventHandler OnDehalfop;
        public event VoiceEventHandler OnVoice;
        public event DevoiceEventHandler OnDevoice;
        public event WhoEventHandler OnWho;
        public event MotdEventHandler OnMotd;
        public event TopicEventHandler OnTopic;
        public event TopicChangeEventHandler OnTopicChange;
        public event NickChangeEventHandler OnNickChange;
        public event IrcEventHandler OnModeChange;
        public event IrcEventHandler OnUserModeChange;
        public event EventHandler<ChannelModeChangeEventArgs> OnChannelModeChange;
        public event IrcEventHandler OnChannelMessage;
        public event ActionEventHandler OnChannelAction;
        public event IrcEventHandler OnChannelNotice;
        public event IrcEventHandler OnChannelActiveSynced;
        public event IrcEventHandler OnChannelPassiveSynced;
        public event IrcEventHandler OnQueryMessage;
        public event ActionEventHandler OnQueryAction;
        public event IrcEventHandler OnQueryNotice;
        public event CtcpEventHandler OnCtcpRequest;
        public event CtcpEventHandler OnCtcpReply;
        public event BounceEventHandler OnBounce;

        /// <summary>
        /// Enables/disables the active channel sync feature.
        /// Default: false
        /// </summary>
        public bool ActiveChannelSyncing { get; set; }

        /// <summary>
        /// Sets the ctcp version that should be replied on ctcp version request.
        /// </summary>
        public string CtcpVersion { get; set; }

        /// <summary>
        /// Enables/disables auto joining of channels when invited.
        /// Default: false
        /// </summary>
        public bool AutoJoinOnInvite { get; set; }

        /// <summary>
        /// Enables/disables automatic rejoining of channels when a connection to the server is lost.
        /// Default: false
        /// </summary>
        public bool AutoRejoin { get; set; }

        /// <summary>
        /// Enables/disables auto rejoining of channels when kicked.
        /// Default: false
        /// </summary>
        public bool AutoRejoinOnKick { get; set; }

        /// <summary>
        /// Enables/disables auto relogin to the server after a reconnect.
        /// Default: false
        /// </summary>
        public bool AutoRelogin { get; set; }

        /// <summary>
        /// Enables/disables auto nick handling on nick collisions
        /// Default: true
        /// </summary>
        public bool AutoNickHandling { get; set; } = true;

        /// <summary>
        /// Enables/disables support for non rfc features.
        /// Default: false
        /// </summary>
        public bool SupportNonRfc
        {
            get => _SupportNonRfc;
            set
            {
                if (_SupportNonRfcLocked)
                {
                    return;
                }
#if LOG4NET

                if (value) {
                    Logger.ChannelSyncing.Info("SupportNonRfc enabled");
                } else {
                    Logger.ChannelSyncing.Info("SupportNonRfc disabled");
                }
#endif
                _SupportNonRfc = value;
            }
        }

        /// <summary>
        /// Gets the nickname of us.
        /// </summary>
        public string Nickname { get; private set; } = "";

        /// <summary>
        /// Gets the list of nicknames of us.
        /// </summary>
        public string[] NicknameList { get; private set; }

        /// <summary>
        /// Gets the supposed real name of us.
        /// </summary>
        public string Realname { get; private set; } = "";

        /// <summary>
        /// Gets the username for the server.
        /// </summary>
        /// <remarks>
        /// System username is set by default
        /// </remarks>
        public string Username { get; private set; } = "";

        /// <summary>
        /// Gets the alphanumeric mode mask of us.
        /// </summary>
        public string Usermode { get; private set; } = "";

        /// <summary>
        /// Gets the numeric mode mask of us.
        /// </summary>
        public int IUsermode { get; private set; }

        /// <summary>
        /// Returns if we are away on this connection
        /// </summary>
        public bool IsAway { get; private set; }

        /// <summary>
        /// Gets the password for the server.
        /// </summary>
        public string Password { get; private set; } = "";

        /// <summary>
        /// Gets the list of channels we are joined.
        /// </summary>
        public List<string> JoinedChannels { get; } = new List<string>();

        /// <summary>
        /// Gets the server message of the day.
        /// </summary>
        public List<string> Motd { get; } = new List<string>();

        public object BanListSyncRoot { get; } = new object();

        /// <summary>
        /// Gets the special functionality supported by this server.
        /// </summary>
        public ServerProperties ServerProperties { get; } = new ServerProperties();

        /// <summary>
        /// This class manages the connection server and provides access to all the objects needed to send and receive messages.
        /// </summary>
        public IrcClient()
        {
#if LOG4NET
            Logger.Main.Debug("IrcClient created");
#endif
            OnReadLine += _Worker;
            OnDisconnected += _OnDisconnected;
            OnConnectionError += _OnConnectionError;

            ChannelModeMap = new ChannelModeMap();
        }

#if LOG4NET
        ~IrcClient()
        {
            Logger.Main.Debug("IrcClient destroyed");
        }
#endif

        /// <summary>
        /// Connection parameters required to establish an server connection.
        /// </summary>
        /// <param name="addresslist">The list of server hostnames.</param>
        /// <param name="port">The TCP port the server listens on.</param>
        public new void Connect(string[] addresslist, int port)
        {
            _SupportNonRfcLocked = true;
            ChannelModeMap = new ChannelModeMap();
            base.Connect(addresslist, port);
        }

        /// <overloads>
        /// Reconnects to the current server.
        /// </overloads>
        /// <param name="login">If the login data should be sent, after successful connect.</param>
        /// <param name="channels">If the channels should be rejoined, after successful connect.</param>
        public void Reconnect(bool login, bool channels)
        {
            if (channels)
            {
                _StoreChannelsToRejoin();
            }
            base.Reconnect();
            if (login)
            {
                //reset the nick to the original nicklist
                _CurrentNickname = 0;
                // FIXME: honor _Nickname (last used nickname)
                Login(NicknameList, Realname, IUsermode, Username, Password);
            }
            if (channels)
            {
                _RejoinChannels();
            }
        }

        /// <param name="login">If the login data should be sent, after successful connect.</param>
        public void Reconnect(bool login) => Reconnect(login, AutoRejoin);

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nicklist">The users list of 'nick' names which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param>
        /// <param name="usermode">A numeric mode parameter.
        ///   <remark>
        ///     Set to 0 to recieve wallops and be invisible.
        ///     Set to 4 to be invisible and not receive wallops.
        ///   </remark>
        /// </param>
        /// <param name="username">The user's machine logon name</param>
        /// <param name="password">The optional password can and MUST be set before any attempt to register
        ///  the connection is made.</param>
        public void Login(string[] nicklist, string realname, int usermode, string username, string password)
        {
#if LOG4NET
            Logger.Connection.Info("logging in");
#endif
            NicknameList = (string[])nicklist.Clone();
            // here we set the nickname which we will try first
            Nickname = NicknameList[0].Replace(" ", "");
            Realname = realname;
            IUsermode = usermode;

            if (!String.IsNullOrEmpty(username))
            {
                Username = username.Replace(" ", "");
            }
            else
            {
                Username = Environment.UserName.Replace(" ", "");
            }

            if (!String.IsNullOrEmpty(password))
            {
                Password = password;
                RfcPass(Password, Priority.Critical);
            }

            RfcNick(Nickname, Priority.Critical);
            RfcUser(Username, IUsermode, Realname, Priority.Critical);
        }

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nicklist">The users list of 'nick' names which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param>
        /// <param name="usermode">A numeric mode parameter.
        /// Set to 0 to recieve wallops and be invisible.
        /// Set to 4 to be invisible and not receive wallops.</param>
        /// <param name="username">The user's machine logon name</param>
        public void Login(string[] nicklist, string realname, int usermode, string username) => Login(nicklist, realname, usermode, username, "");

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nicklist">The users list of 'nick' names which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param>
        /// <param name="usermode">A numeric mode parameter.
        /// Set to 0 to recieve wallops and be invisible.
        /// Set to 4 to be invisible and not receive wallops.</param>
        public void Login(string[] nicklist, string realname, int usermode) => Login(nicklist, realname, usermode, "", "");

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nicklist">The users list of 'nick' names which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param>
        public void Login(string[] nicklist, string realname) => Login(nicklist, realname, 0, "", "");

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nick">The users 'nick' name which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param>
        /// <param name="usermode">A numeric mode parameter.
        /// Set to 0 to recieve wallops and be invisible.
        /// Set to 4 to be invisible and not receive wallops.</param>
        /// <param name="username">The user's machine logon name</param>
        /// <param name="password">The optional password can and MUST be set before any attempt to register
        ///  the connection is made.</param>
        public void Login(string nick, string realname, int usermode, string username, string password) => Login(new string[] { nick, nick + "_", nick + "__" }, realname, usermode, username, password);

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nick">The users 'nick' name which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param>
        /// <param name="usermode">A numeric mode parameter.
        /// Set to 0 to recieve wallops and be invisible.
        /// Set to 4 to be invisible and not receive wallops.</param>
        /// <param name="username">The user's machine logon name</param>
        public void Login(string nick, string realname, int usermode, string username) => Login(new string[] { nick, nick + "_", nick + "__" }, realname, usermode, username, "");

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nick">The users 'nick' name which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param>
        /// <param name="usermode">A numeric mode parameter.
        /// Set to 0 to recieve wallops and be invisible.
        /// Set to 4 to be invisible and not receive wallops.</param>
        public void Login(string nick, string realname, int usermode) => Login(new string[] { nick, nick + "_", nick + "__" }, realname, usermode, "", "");

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nick">The users 'nick' name which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param>
        public void Login(string nick, string realname) => Login(new string[] { nick, nick + "_", nick + "__" }, realname, 0, "", "");

        /// <summary>
        /// Determine if a specifier nickname is you
        /// </summary>
        /// <param name="nickname">The users 'nick' name which may NOT contain spaces</param>
        /// <returns>True if nickname belongs to you</returns>
        public bool IsMe(string nickname) => String.Compare(Nickname, nickname, StringComparison.OrdinalIgnoreCase) == 0;

        /// <summary>
        /// Determines if your nickname can be found in a specified channel
        /// </summary>
        /// <param name="channelname">The name of the channel you wish to query</param>
        /// <returns>True if you are found in channel</returns>
        public bool IsJoined(string channelname) => IsJoined(channelname, Nickname);

        /// <summary>
        /// Determine if a specified nickname can be found in a specified channel
        /// </summary>
        /// <param name="channelname">The name of the channel you wish to query</param>
        /// <param name="nickname">The users 'nick' name which may NOT contain spaces</param>
        /// <returns>True if nickname is found in channel</returns>
        public bool IsJoined(string channelname, string nickname)
        {
            if (channelname == null)
            {
                throw new ArgumentNullException(nameof(channelname));
            }

            if (nickname == null)
            {
                throw new ArgumentNullException(nameof(nickname));
            }

            Channel channel = GetChannel(channelname);
            return channel?.UnsafeUsers?.ContainsKey(nickname) ?? false;
        }

        /// <summary>
        /// Returns user information
        /// </summary>
        /// <param name="nickname">The users 'nick' name which may NOT contain spaces</param>
        /// <returns>IrcUser object of requested nickname</returns>
        public IrcUser GetIrcUser(string nickname)
        {
            if (nickname == null)
            {
                throw new ArgumentNullException(nameof(nickname));
            }

            return _IrcUsers.TryGetValue(nickname, out IrcUser user) ? user : null;
        }

        /// <summary>
        /// Returns extended user information including channel information
        /// </summary>
        /// <param name="channelname">The name of the channel you wish to query</param>
        /// <param name="nickname">The users 'nick' name which may NOT contain spaces</param>
        /// <returns>ChannelUser object of requested channelname/nickname</returns>
        public ChannelUser GetChannelUser(string channelname, string nickname)
        {
            if (channelname == null)
            {
                throw new ArgumentNullException(nameof(channelname));
            }

            if (nickname == null)
            {
                throw new ArgumentNullException(nameof(nickname));
            }

            Channel channel = GetChannel(channelname);
            return channel != null && channel.UnsafeUsers.TryGetValue(nickname, out ChannelUser cuser) ? cuser : null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="channelname">The name of the channel you wish to query</param>
        /// <returns>Channel object of requested channel</returns>
        public Channel GetChannel(string channelname)
        {
            if (channelname == null)
            {
                throw new ArgumentNullException(nameof(channelname));
            }

            return _Channels.TryGetValue(channelname, out Channel channel) ? channel : null;
        }

        /// <summary>
        /// Gets a list of all joined channels on server
        /// </summary>
        /// <returns>String array of all joined channel names</returns>
        public string[] GetChannels()
        {
            string[] channels = new string[_Channels.Values.Count];
            int i = 0;
            foreach (Channel channel in _Channels.Values)
            {
                channels[i++] = channel.Name;
            }

            return channels;
        }

        /// <summary>
        /// Fetches a fresh list of all available channels that match the passed mask
        /// </summary>
        /// <returns>List of ListInfo</returns>
        public IList<ChannelInfo> GetChannelList(string mask)
        {
            var list = new List<ChannelInfo>();
            lock (_ChannelListSyncRoot)
            {
                _ChannelList = list;
                _ChannelListReceivedEvent = new AutoResetEvent(false);

                // request list
                RfcList(mask);
                // wait till we have the complete list
                _ChannelListReceivedEvent.WaitOne();

                _ChannelListReceivedEvent = null;
                _ChannelList = null;
            }

            return list;
        }

        /// <summary>
        /// Fetches a fresh list of users that matches the passed mask
        /// </summary>
        /// <returns>List of ListInfo</returns>
        public IList<WhoInfo> GetWhoList(string mask)
        {
            var list = new List<WhoInfo>();
            lock (_WhoListSyncRoot)
            {
                _WhoList = list;
                _WhoListReceivedEvent = new AutoResetEvent(false);

                // request list
                RfcWho(mask);
                // wait till we have the complete list
                _WhoListReceivedEvent.WaitOne();

                _WhoListReceivedEvent = null;
                _WhoList = null;
            }

            return list;
        }

        /// <summary>
        /// Fetches a fresh ban list of the specified channel
        /// </summary>
        /// <returns>List of ListInfo</returns>
        public IList<BanInfo> GetBanList(string channel)
        {
            var list = new List<BanInfo>();
            lock (BanListSyncRoot)
            {
                _BanList = list;
                _BanListReceivedEvent = new AutoResetEvent(false);

                // request list
                Ban(channel);
                // wait till we have the complete list
                _BanListReceivedEvent.WaitOne();

                _BanListReceivedEvent = null;
                _BanList = null;
            }

            return list;
        }

        /// <summary>
        /// Fetches a fresh ban-exceptions list from the specified channel.
        /// </summary>
        public IList<BanInfo> GetBanExceptionList(string channel)
        {
            var list = new List<BanInfo>();
            if (!ServerProperties.BanExceptionCharacter.HasValue)
            {
                return list;
            }
            lock (_BanExceptListSyncRoot)
            {
                _BanExceptList = list;
                _BanExceptListReceivedEvent = new AutoResetEvent(false);

                BanException(channel);
                _BanExceptListReceivedEvent.WaitOne();

                _BanExceptListReceivedEvent = null;
                _BanExceptList = null;
            }

            return list;
        }

        /// <summary>
        /// Fetches a fresh invite-exceptions list from the specified channel.
        /// </summary>
        public IList<BanInfo> GetInviteExceptionList(string channel)
        {
            var list = new List<BanInfo>();
            if (!ServerProperties.InviteExceptionCharacter.HasValue)
            {
                return list;
            }
            lock (_InviteExceptListSyncRoot)
            {
                _InviteExceptList = list;
                _InviteExceptListReceivedEvent = new AutoResetEvent(false);

                InviteException(channel);
                _InviteExceptListReceivedEvent.WaitOne();

                _InviteExceptListReceivedEvent = null;
                _InviteExceptList = null;
            }

            return list;
        }

        public IrcMessageData MessageParser(string rawline)
        {
            if (rawline == null)
            {
                throw new ArgumentNullException(nameof(rawline));
            }

            string line;
            string[] linear;
            string messagecode;
            string from;
            string nick = null;
            string ident = null;
            string host = null;
            string channel = null;
            string message = null;
            string rawTags = null;
            var tags = new Dictionary<string, string>();
            ReceiveType type;
            ReplyCode replycode;
            int exclamationpos;
            int atpos;
            int colonpos;

            if (rawline.Length == 0)
            {
                throw new ArgumentException("Value must not be empty.", nameof(rawline));
            }

            // IRCv3.2 message tags: http://ircv3.net/specs/core/message-tags-3.2.html
            if (rawline[0] == '@')
            {
                int spcidx = rawline.IndexOf(' ');
                rawTags = rawline.Substring(1, spcidx - 1);
                // strip tags from further parsing for backwards compatibility
                line = rawline.Substring(spcidx + 1);

                foreach (string s in rawTags.Split(';'))
                {
                    int eqidx = s.IndexOf("=");

                    if (eqidx != -1)
                    {
                        tags.Add(s.Substring(0, eqidx), _UnescapeTagValue(s.Substring(eqidx + 1)));
                    }
                    else
                    {
                        tags.Add(s, null);
                    }
                }
            }
            else
            {
                line = rawline;
            }

            if (line[0] == ':')
            {
                line = line.Substring(1);
            }
            linear = line.Split(' ');

            // conform to RFC 2812
            from = linear[0];
            messagecode = linear[1];
            exclamationpos = from.IndexOf('!');
            atpos = from.IndexOf('@');
            colonpos = line.IndexOf(" :", StringComparison.Ordinal);
            if (colonpos != -1)
            {
                // we want the exact position of ":" not beginning from the space
                colonpos += 1;
            }
            if (exclamationpos != -1)
            {
                nick = from.Substring(0, exclamationpos);
            }
            else
            {
                if (atpos == -1)
                {
                    // no ident and no host, should be nick then
                    if (!from.Contains("."))
                    {
                        // HACK: from seems to be a nick instead of servername
                        nick = from;
                    }
                }
                else
                {
                    nick = from.Substring(0, atpos);
                }
            }
            if (atpos != -1 && exclamationpos != -1)
            {
                ident = from.Substring(exclamationpos + 1, (atpos - exclamationpos) - 1);
            }
            if (atpos != -1)
            {
                host = from.Substring(atpos + 1);
            }

            replycode = Int32.TryParse(messagecode, out int tmp) ? (ReplyCode)tmp : ReplyCode.Null;

            type = _GetMessageType(line);
            if (colonpos != -1)
            {
                message = line.Substring(colonpos + 1);
            }

            switch (type)
            {
                case ReceiveType.Join:
                case ReceiveType.Kick:
                case ReceiveType.Part:
                case ReceiveType.TopicChange:
                case ReceiveType.ChannelModeChange:
                case ReceiveType.ChannelMessage:
                case ReceiveType.ChannelAction:
                case ReceiveType.ChannelNotice:
                    channel = linear[2];
                    break;
                case ReceiveType.Who:
                case ReceiveType.Topic:
                case ReceiveType.Invite:
                case ReceiveType.BanList:
                case ReceiveType.ChannelMode:
                    channel = linear[3];
                    break;
                case ReceiveType.Name:
                    channel = linear[4];
                    break;
            }

            switch (replycode)
            {
                case ReplyCode.List:
                case ReplyCode.ListEnd:
                case ReplyCode.ErrorNoChannelModes:
                case ReplyCode.InviteList:
                case ReplyCode.ExceptionList:
                    channel = linear[3];
                    break;
            }

            if (channel != null && channel[0] == ':')
            {
                channel = channel.Substring(1);
            }

            var data = new IrcMessageData(this, from, nick, ident, host, channel, message, rawline, type, replycode, tags);
#if LOG4NET
            Logger.MessageParser.Debug("IrcMessageData "+
                                       "nick: '"+data.Nick+"' "+
                                       "ident: '"+data.Ident+"' "+
                                       "host: '"+data.Host+"' "+
                                       "type: '"+data.Type.ToString()+"' "+
                                       "from: '"+data.From+"' "+
                                       "channel: '"+data.Channel+"' "+
                                       "message: '"+data.Message+"' "+
                                       "tags: '"+rawTags+"' "
                                       );
#endif
            return data;
        }

        // ISUPPORT-honoring versions of some IrcCommands methods

        public override void BanException(string channel)
        {
            char? bexchar = ServerProperties.BanExceptionCharacter;
            if (bexchar.HasValue)
            {
                ListChannelMasks("+" + bexchar.Value, channel);
            }
            else
            {
                base.BanException(channel);
            }
        }

        public override void BanException(string channel, string hostmask, Priority priority)
        {
            char? bexchar = ServerProperties.BanExceptionCharacter;
            if (bexchar.HasValue)
            {
                ModifyChannelMasks("+" + bexchar.Value, channel, hostmask, priority);
            }
            else
            {
                base.BanException(channel, hostmask, priority);
            }
        }

        public override void BanException(string channel, string hostmask)
        {
            char? bexchar = ServerProperties.BanExceptionCharacter;
            if (bexchar.HasValue)
            {
                ModifyChannelMasks("+" + bexchar.Value, channel, hostmask);
            }
            else
            {
                base.BanException(channel, hostmask);
            }
        }

        public override void BanException(string channel, string[] hostmasks)
        {
            char? bexchar = ServerProperties.BanExceptionCharacter;
            if (bexchar.HasValue)
            {
                ModifyChannelMasks("+" + bexchar.Value, channel, hostmasks);
            }
            else
            {
                base.BanException(channel, hostmasks);
            }
        }

        public override void UnBanException(string channel, string hostmask, Priority priority)
        {
            char? bexchar = ServerProperties.BanExceptionCharacter;
            if (bexchar.HasValue)
            {
                ModifyChannelMasks("-" + bexchar.Value, channel, hostmask, priority);
            }
            else
            {
                base.UnBanException(channel, hostmask, priority);
            }
        }

        public override void UnBanException(string channel, string hostmask)
        {
            char? bexchar = ServerProperties.BanExceptionCharacter;
            if (bexchar.HasValue)
            {
                ModifyChannelMasks("-" + bexchar.Value, channel, hostmask);
            }
            else
            {
                base.UnBanException(channel, hostmask);
            }
        }

        public override void UnBanException(string channel, string[] hostmasks)
        {
            char? bexchar = ServerProperties.BanExceptionCharacter;
            if (bexchar.HasValue)
            {
                ModifyChannelMasks("-" + bexchar.Value, channel, hostmasks);
            }
            else
            {
                base.UnBanException(channel, hostmasks);
            }
        }

        public override void InviteException(string channel)
        {
            char? iexchar = ServerProperties.InviteExceptionCharacter;
            if (iexchar.HasValue)
            {
                ListChannelMasks("+" + iexchar.Value, channel);
            }
            else
            {
                base.InviteException(channel);
            }
        }

        public override void InviteException(string channel, string hostmask, Priority priority)
        {
            char? iexchar = ServerProperties.InviteExceptionCharacter;
            if (iexchar.HasValue)
            {
                ModifyChannelMasks("+" + iexchar.Value, channel, hostmask, priority);
            }
            else
            {
                base.InviteException(channel, hostmask, priority);
            }
        }

        public override void InviteException(string channel, string hostmask)
        {
            char? iexchar = ServerProperties.InviteExceptionCharacter;
            if (iexchar.HasValue)
            {
                ModifyChannelMasks("+" + iexchar.Value, channel, hostmask);
            }
            else
            {
                base.InviteException(channel, hostmask);
            }
        }

        public override void InviteException(string channel, string[] hostmasks)
        {
            char? iexchar = ServerProperties.InviteExceptionCharacter;
            if (iexchar.HasValue)
            {
                ModifyChannelMasks("+" + iexchar.Value, channel, hostmasks);
            }
            else
            {
                base.InviteException(channel, hostmasks);
            }
        }

        public override void UnInviteException(string channel, string hostmask, Priority priority)
        {
            char? iexchar = ServerProperties.InviteExceptionCharacter;
            if (iexchar.HasValue)
            {
                ModifyChannelMasks("-" + iexchar.Value, channel, hostmask, priority);
            }
            else
            {
                base.UnInviteException(channel, hostmask, priority);
            }
        }

        public override void UnInviteException(string channel, string hostmask)
        {
            char? iexchar = ServerProperties.InviteExceptionCharacter;
            if (iexchar.HasValue)
            {
                ModifyChannelMasks("-" + iexchar.Value, channel, hostmask);
            }
            else
            {
                base.UnInviteException(channel, hostmask);
            }
        }

        public override void UnInviteException(string channel, string[] hostmasks)
        {
            char? iexchar = ServerProperties.InviteExceptionCharacter;
            if (iexchar.HasValue)
            {
                ModifyChannelMasks("-" + iexchar.Value, channel, hostmasks);
            }
            else
            {
                base.UnInviteException(channel, hostmasks);
            }
        }

        protected virtual IrcUser CreateIrcUser(string nickname) => new IrcUser(nickname, this);

        protected virtual Channel CreateChannel(string name) => _SupportNonRfc ? new NonRfcChannel(name) : new Channel(name);

        protected virtual ChannelUser CreateChannelUser(string channel, IrcUser ircUser) => _SupportNonRfc ? new NonRfcChannelUser(channel, ircUser) : new ChannelUser(channel, ircUser);

        private void _Worker(object sender, ReadLineEventArgs e) =>
            // lets see if we have events or internal messagehandler for it
            _HandleEvents(MessageParser(e.Line));

        private void _OnDisconnected(object sender, EventArgs e)
        {
            if (AutoRejoin)
            {
                _StoreChannelsToRejoin();
            }
            _SyncingCleanup();
        }

        private void _OnConnectionError(object sender, EventArgs e)
        {
            try
            {
                // AutoReconnect is handled in IrcConnection._OnConnectionError
                if (AutoReconnect && AutoRelogin)
                {
                    Login(NicknameList, Realname, IUsermode, Username, Password);
                }
                if (AutoReconnect && AutoRejoin)
                {
                    _RejoinChannels();
                }
            }
            catch (NotConnectedException)
            {
                // HACK: this is hacky, we don't know if the Reconnect was actually successful
                // means sending IRC commands without a connection throws NotConnectedExceptions
            }
        }

        private void _StoreChannelsToRejoin()
        {
#if LOG4NET
            Logger.Connection.Info("Storing channels for rejoin...");
#endif
            lock (_AutoRejoinChannels)
            {
                _AutoRejoinChannels.Clear();
                if (ActiveChannelSyncing)
                {
                    // store the key using channel sync
                    foreach (Channel channel in _Channels.Values)
                    {
                        _AutoRejoinChannels.Add(channel.Name, channel.Key);
                    }
                }
                else
                {
                    foreach (string channel in JoinedChannels)
                    {
                        _AutoRejoinChannels.Add(channel, null);
                    }
                }
            }
        }

        private void _RejoinChannels()
        {
#if LOG4NET
            Logger.Connection.Info("Rejoining channels...");
#endif
            lock (_AutoRejoinChannels)
            {
                RfcJoin(_AutoRejoinChannels.Keys.ToArray(), _AutoRejoinChannels.Values.ToArray(), Priority.High);
                _AutoRejoinChannels.Clear();
            }
        }

        private void _SyncingCleanup()
        {
            // lets clean it baby, powered by Mr. Proper
#if LOG4NET
            Logger.ChannelSyncing.Debug("Mr. Proper action, cleaning good...");
#endif
            JoinedChannels.Clear();
            if (ActiveChannelSyncing)
            {
                _Channels.Clear();
                _IrcUsers.Clear();
            }

            IsAway = false;

            _MotdReceived = false;
            Motd.Clear();
        }

        private string _NextNickname()
        {
            _CurrentNickname++;
            //if we reach the end stay there
            if (_CurrentNickname >= NicknameList.Length)
            {
                _CurrentNickname--;
            }
            return NicknameList[_CurrentNickname];
        }

        private string _UnescapeTagValue(string tagValue)
        {
            int lastPos = 0;
            int pos = 0;
            string sequence;
            var unescaped = new StringBuilder(tagValue.Length);

            while (lastPos < tagValue.Length && (pos = tagValue.IndexOf('\\', lastPos)) >= 0)
            {
                unescaped.Append(tagValue.Substring(lastPos, pos - lastPos));
                sequence = tagValue.Substring(pos, 2);
                switch (sequence)
                {
                    case @"\:":
                        unescaped.Append(";");
                        break;
                    case @"\s":
                        unescaped.Append(" ");
                        break;
                    case @"\\":
                        unescaped.Append(@"\");
                        break;
                    case @"\r":
                        unescaped.Append("\r");
                        break;
                    case @"\n":
                        unescaped.Append("\n");
                        break;
                }

                lastPos = pos + sequence.Length;
            }

            if (lastPos < tagValue.Length)
            {
                unescaped.Append(tagValue.Substring(lastPos));
            }

            return unescaped.ToString();
        }

        private ReceiveType _GetMessageType(string rawline)
        {
            Match found = _ReplyCodeRegex.Match(rawline);
            if (found.Success)
            {
                string code = found.Groups[1].Value;
                var replycode = (ReplyCode)Int32.Parse(code);

                // check if this replycode is known in the RFC
                if (Array.IndexOf(_ReplyCodes, replycode) == -1)
                {
                    return ReceiveType.Unknown;
                }

                switch (replycode)
                {
                    case ReplyCode.Welcome:
                    case ReplyCode.YourHost:
                    case ReplyCode.Created:
                    case ReplyCode.MyInfo:
                    case ReplyCode.Bounce:
                        return ReceiveType.Login;
                    case ReplyCode.LuserClient:
                    case ReplyCode.LuserOp:
                    case ReplyCode.LuserUnknown:
                    case ReplyCode.LuserMe:
                    case ReplyCode.LuserChannels:
                        return ReceiveType.Info;
                    case ReplyCode.MotdStart:
                    case ReplyCode.Motd:
                    case ReplyCode.EndOfMotd:
                        return ReceiveType.Motd;
                    case ReplyCode.NamesReply:
                    case ReplyCode.EndOfNames:
                        return ReceiveType.Name;
                    case ReplyCode.WhoReply:
                    case ReplyCode.EndOfWho:
                        return ReceiveType.Who;
                    case ReplyCode.ListStart:
                    case ReplyCode.List:
                    case ReplyCode.ListEnd:
                        return ReceiveType.List;
                    case ReplyCode.BanList:
                    case ReplyCode.EndOfBanList:
                        return ReceiveType.BanList;
                    case ReplyCode.Topic:
                    case ReplyCode.NoTopic:
                        return ReceiveType.Topic;
                    case ReplyCode.WhoIsUser:
                    case ReplyCode.WhoIsServer:
                    case ReplyCode.WhoIsOperator:
                    case ReplyCode.WhoIsIdle:
                    case ReplyCode.WhoIsChannels:
                    case ReplyCode.EndOfWhoIs:
                        return ReceiveType.WhoIs;
                    case ReplyCode.WhoWasUser:
                    case ReplyCode.EndOfWhoWas:
                        return ReceiveType.WhoWas;
                    case ReplyCode.UserModeIs:
                        return ReceiveType.UserMode;
                    case ReplyCode.ChannelModeIs:
                        return ReceiveType.ChannelMode;
                    default:
                        if (((int)replycode >= 400) &&
                            ((int)replycode <= 599))
                        {
                            return ReceiveType.ErrorMessage;
                        }
                        else
                        {
#if LOG4NET
                            Logger.MessageTypes.Debug("replycode unknown ("+code+"): \""+rawline+"\"");
#endif
                            return ReceiveType.Unknown;
                        }
                }
            }

            found = _PingRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Unknown;
            }

            found = _ErrorRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Error;
            }

            found = _ActionRegex.Match(rawline);
            if (found.Success)
            {
                switch (found.Groups[1].Value)
                {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return ReceiveType.ChannelAction;
                    default:
                        return ReceiveType.QueryAction;
                }
            }

            found = _CtcpRequestRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.CtcpRequest;
            }

            found = _MessageRegex.Match(rawline);
            if (found.Success)
            {
                switch (found.Groups[1].Value)
                {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return ReceiveType.ChannelMessage;
                    default:
                        return ReceiveType.QueryMessage;
                }
            }

            found = _CtcpReplyRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.CtcpReply;
            }

            found = _NoticeRegex.Match(rawline);
            if (found.Success)
            {
                switch (found.Groups[1].Value)
                {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return ReceiveType.ChannelNotice;
                    default:
                        return ReceiveType.QueryNotice;
                }
            }

            found = _InviteRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Invite;
            }

            found = _JoinRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Join;
            }

            found = _TopicRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.TopicChange;
            }

            found = _NickRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.NickChange;
            }

            found = _KickRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Kick;
            }

            found = _PartRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Part;
            }

            found = _ModeRegex.Match(rawline);
            if (found.Success)
            {
                return IsMe(found.Groups[1].Value) ? ReceiveType.UserModeChange : ReceiveType.ChannelModeChange;
            }

            found = _QuitRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Quit;
            }

#if LOG4NET
            Logger.MessageTypes.Debug("messagetype unknown: \""+rawline+"\"");
#endif
            return ReceiveType.Unknown;
        }

        private void _HandleEvents(IrcMessageData ircdata)
        {
            OnRawMessage?.Invoke(this, new IrcEventArgs(ircdata));

            string code;
            // special IRC messages
            code = ircdata.RawMessageArray[0];
            switch (code)
            {
                case "PING":
                    _Event_PING(ircdata);
                    break;
                case "ERROR":
                    _Event_ERROR(ircdata);
                    break;
            }

            code = ircdata.RawMessageArray[1];
            switch (code)
            {
                case "PRIVMSG":
                    _Event_PRIVMSG(ircdata);
                    break;
                case "NOTICE":
                    _Event_NOTICE(ircdata);
                    break;
                case "JOIN":
                    _Event_JOIN(ircdata);
                    break;
                case "PART":
                    _Event_PART(ircdata);
                    break;
                case "KICK":
                    _Event_KICK(ircdata);
                    break;
                case "QUIT":
                    _Event_QUIT(ircdata);
                    break;
                case "TOPIC":
                    _Event_TOPIC(ircdata);
                    break;
                case "NICK":
                    _Event_NICK(ircdata);
                    break;
                case "INVITE":
                    _Event_INVITE(ircdata);
                    break;
                case "MODE":
                    _Event_MODE(ircdata);
                    break;
                case "PONG":
                    _Event_PONG(ircdata);
                    break;
            }

            if (ircdata.ReplyCode != ReplyCode.Null)
            {
                switch (ircdata.ReplyCode)
                {
                    case ReplyCode.Welcome:
                        _Event_RPL_WELCOME(ircdata);
                        break;
                    case ReplyCode.Topic:
                        _Event_RPL_TOPIC(ircdata);
                        break;
                    case ReplyCode.NoTopic:
                        _Event_RPL_NOTOPIC(ircdata);
                        break;
                    case ReplyCode.NamesReply:
                        _Event_RPL_NAMREPLY(ircdata);
                        break;
                    case ReplyCode.EndOfNames:
                        _Event_RPL_ENDOFNAMES(ircdata);
                        break;
                    case ReplyCode.List:
                        _Event_RPL_LIST(ircdata);
                        break;
                    case ReplyCode.ListEnd:
                        _Event_RPL_LISTEND(ircdata);
                        break;
                    case ReplyCode.WhoReply:
                        _Event_RPL_WHOREPLY(ircdata);
                        break;
                    case ReplyCode.EndOfWho:
                        _Event_RPL_ENDOFWHO(ircdata);
                        break;
                    case ReplyCode.ChannelModeIs:
                        _Event_RPL_CHANNELMODEIS(ircdata);
                        break;
                    case ReplyCode.BanList:
                        _Event_RPL_BANLIST(ircdata);
                        break;
                    case ReplyCode.EndOfBanList:
                        _Event_RPL_ENDOFBANLIST(ircdata);
                        break;
                    case ReplyCode.ErrorNoChannelModes:
                        _Event_ERR_NOCHANMODES(ircdata);
                        break;
                    case ReplyCode.Motd:
                        _Event_RPL_MOTD(ircdata);
                        break;
                    case ReplyCode.EndOfMotd:
                        _Event_RPL_ENDOFMOTD(ircdata);
                        break;
                    case ReplyCode.Away:
                        _Event_RPL_AWAY(ircdata);
                        break;
                    case ReplyCode.UnAway:
                        _Event_RPL_UNAWAY(ircdata);
                        break;
                    case ReplyCode.NowAway:
                        _Event_RPL_NOWAWAY(ircdata);
                        break;
                    case ReplyCode.TryAgain:
                        _Event_RPL_TRYAGAIN(ircdata);
                        break;
                    case ReplyCode.ErrorNicknameInUse:
                        _Event_ERR_NICKNAMEINUSE(ircdata);
                        break;
                    case ReplyCode.InviteList:
                        _Event_RPL_INVITELIST(ircdata);
                        break;
                    case ReplyCode.EndOfInviteList:
                        _Event_RPL_ENDOFINVITELIST(ircdata);
                        break;
                    case ReplyCode.ExceptionList:
                        _Event_RPL_EXCEPTLIST(ircdata);
                        break;
                    case ReplyCode.EndOfExceptionList:
                        _Event_RPL_ENDOFEXCEPTLIST(ircdata);
                        break;
                    case ReplyCode.Bounce:
                        _Event_RPL_BOUNCE(ircdata);
                        break;
                }
            }

            if (ircdata.Type == ReceiveType.ErrorMessage)
            {
                _Event_ERR(ircdata);
            }
        }

        /// <summary>
        /// Removes a specified user from all channel lists
        /// </summary>
        /// <param name="nickname">The users 'nick' name which may NOT contain spaces</param>
        private bool _RemoveIrcUser(string nickname)
        {
            IrcUser user = GetIrcUser(nickname);
            if (user != null && user.JoinedChannels.Length == 0)
            {
                // he is nowhere else, lets kill him
                _IrcUsers.Remove(nickname);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes a specified user from a specified channel list
        /// </summary>
        /// <param name="channelname">The name of the channel you wish to query</param>
        /// <param name="nickname">The users 'nick' name which may NOT contain spaces</param>
        private void _RemoveChannelUser(string channelname, string nickname)
        {
            Channel chan = GetChannel(channelname);
            chan.UnsafeUsers.Remove(nickname);
            chan.UnsafeOps.Remove(nickname);
            chan.UnsafeVoices.Remove(nickname);
            if (SupportNonRfc)
            {
                var nchan = (NonRfcChannel)chan;
                nchan.UnsafeOwners.Remove(nickname);
                nchan.UnsafeChannelAdmins.Remove(nickname);
                nchan.UnsafeHalfops.Remove(nickname);
            }
        }

        /// <param name="ircdata">Message data containing channel mode information</param>
        private void _InterpretChannelMode(IrcMessageData ircdata, List<ChannelModeChangeInfo> changeInfos)
        {
            Channel channel = null;
            if (ActiveChannelSyncing)
            {
                channel = GetChannel(ircdata.Channel);
            }
            foreach (var changeInfo in changeInfos)
            {
                string temp = changeInfo.Parameter;
                bool add = changeInfo.Action == ChannelModeChangeAction.Set;
                bool remove = changeInfo.Action == ChannelModeChangeAction.Unset;
                switch (changeInfo.Mode)
                {
                    case ChannelMode.Op:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                // sanity check
                                ChannelUser cuser = GetChannelUser(ircdata.Channel, temp);

                                if (cuser != null)
                                {
                                    // update the op list
                                    if (channel.UnsafeOps.TryAdd(temp, cuser))
                                    {
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("added op: "+temp+" to: "+ircdata.Channel);
#endif
                                    }
                                    else
                                    {
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("duplicate op: "+temp+" in: "+ircdata.Channel+" not added");
#endif
                                    }

                                    // update the user op status
                                    cuser.IsOp = true;
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("set op status: " + temp + " for: "+ircdata.Channel);
#endif
                                }
                                else
                                {
#if LOG4NET
                                    Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
#endif
                                }
                            }

                            OnOp?.Invoke(this, new OpEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                // sanity check
                                if (GetChannelUser(ircdata.Channel, temp) != null)
                                {
                                    // update the op list
                                    channel.UnsafeOps.Remove(temp);
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("removed op: "+temp+" from: "+ircdata.Channel);
#endif
                                    // update the user op status
                                    ChannelUser cuser = GetChannelUser(ircdata.Channel, temp);
                                    cuser.IsOp = false;
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("unset op status: " + temp + " for: "+ircdata.Channel);
#endif
                                }
                                else
                                {
#if LOG4NET
                                    Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
#endif
                                }
                            }

                            OnDeop?.Invoke(this, new DeopEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        break;
                    case ChannelMode.Owner:
                        if (SupportNonRfc)
                        {
                            if (add)
                            {
                                if (ActiveChannelSyncing && channel != null)
                                {
                                    // sanity check
                                    var cuser = (NonRfcChannelUser)GetChannelUser(ircdata.Channel, temp);

                                    if (cuser != null)
                                    {
                                        // update the owner list
                                        if (((NonRfcChannel)channel).UnsafeOwners.TryAdd(temp, cuser))
                                        {
#if LOG4NET
                                            Logger.ChannelSyncing.Debug("added owner: "+temp+" to: "+ircdata.Channel);
#endif
                                        }
#if LOG4NET
                                        else
                                        {
                                            Logger.ChannelSyncing.Debug("duplicate owner: "+temp+" in: "+ircdata.Channel+" not added");
                                        }
#endif

                                        // update the user owner status
                                        cuser.IsOwner = true;
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("set owner status: " + temp + " for: "+ircdata.Channel);
#endif
                                    }
#if LOG4NET
                                    else
                                    {
                                        Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
                                    }
#endif
                                }

                                OnOwner?.Invoke(this, new OwnerEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                            if (remove)
                            {
                                if (ActiveChannelSyncing && channel != null)
                                {
                                    // sanity check
                                    if (GetChannelUser(ircdata.Channel, temp) != null)
                                    {
                                        // update the owner list
                                        ((NonRfcChannel)channel).UnsafeOwners.Remove(temp);
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("removed owner: "+temp+" from: "+ircdata.Channel);
#endif
                                        // update the user owner status
                                        var cuser = (NonRfcChannelUser)GetChannelUser(ircdata.Channel, temp);
                                        cuser.IsOwner = false;
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("unset owner status: " + temp + " for: "+ircdata.Channel);
#endif
                                    }
                                    else
                                    {
#if LOG4NET
                                        Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
#endif
                                    }
                                }

                                OnDeowner?.Invoke(this, new DeownerEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                        }
                        break;
                    case ChannelMode.Admin:
                        if (SupportNonRfc)
                        {
                            if (add)
                            {
                                if (ActiveChannelSyncing && channel != null)
                                {
                                    // sanity check
                                    var cuser = (NonRfcChannelUser)GetChannelUser(ircdata.Channel, temp);

                                    if (cuser != null)
                                    {
                                        // update the channel admin list
                                        if (((NonRfcChannel)channel).UnsafeChannelAdmins.TryAdd(temp, cuser))
                                        {
#if LOG4NET
                                            Logger.ChannelSyncing.Debug("added channel admin: "+temp+" to: "+ircdata.Channel);
#endif
                                        }
#if LOG4NET
                                        else
                                        {
                                            Logger.ChannelSyncing.Debug("duplicate channel admin: "+temp+" in: "+ircdata.Channel+" not added");
                                        }
#endif

                                        // update the user channel admin status
                                        cuser.IsChannelAdmin = true;
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("set channel admin status: " + temp + " for: "+ircdata.Channel);
#endif
                                    }
                                    else
                                    {
#if LOG4NET
                                        Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
#endif
                                    }
                                }

                                OnChannelAdmin?.Invoke(this, new ChannelAdminEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                            if (remove)
                            {
                                if (ActiveChannelSyncing && channel != null)
                                {
                                    // sanity check
                                    if (GetChannelUser(ircdata.Channel, temp) != null)
                                    {
                                        // update the channel admin list
                                        ((NonRfcChannel)channel).UnsafeChannelAdmins.Remove(temp);
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("removed channel admin: "+temp+" from: "+ircdata.Channel);
#endif
                                        // update the user channel admin status
                                        var cuser = (NonRfcChannelUser)GetChannelUser(ircdata.Channel, temp);
                                        cuser.IsChannelAdmin = false;
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("unset channel admin status: " + temp + " for: "+ircdata.Channel);
#endif
                                    }
                                    else
                                    {
#if LOG4NET
                                        Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
#endif
                                    }
                                }

                                OnDeChannelAdmin?.Invoke(this, new DeChannelAdminEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                        }
                        break;
                    case ChannelMode.HalfOp:
                        if (SupportNonRfc)
                        {
                            if (add)
                            {
                                if (ActiveChannelSyncing && channel != null)
                                {
                                    // sanity check
                                    var cuser = (NonRfcChannelUser)GetChannelUser(ircdata.Channel, temp);

                                    if (cuser != null)
                                    {
                                        // update the halfop list
                                        if (((NonRfcChannel)channel).UnsafeHalfops.TryAdd(temp, cuser))
                                        {
#if LOG4NET
                                            Logger.ChannelSyncing.Debug("added halfop: "+temp+" to: "+ircdata.Channel);
#endif
                                        }
#if LOG4NET
                                        else
                                        {
                                            Logger.ChannelSyncing.Debug("duplicate halfop: "+temp+" in: "+ircdata.Channel+" not added");
                                        }
#endif

                                        // update the user halfop status
                                        cuser.IsHalfop = true;
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("set halfop status: " + temp + " for: "+ircdata.Channel);
#endif
                                    }
                                    else
                                    {
#if LOG4NET
                                        Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
#endif
                                    }
                                }

                                OnHalfop?.Invoke(this, new HalfopEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                            if (remove)
                            {
                                if (ActiveChannelSyncing && channel != null)
                                {
                                    // sanity check
                                    if (GetChannelUser(ircdata.Channel, temp) != null)
                                    {
                                        // update the halfop list
                                        ((NonRfcChannel)channel).UnsafeHalfops.Remove(temp);
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("removed halfop: "+temp+" from: "+ircdata.Channel);
#endif
                                        // update the user halfop status
                                        var cuser = (NonRfcChannelUser)GetChannelUser(ircdata.Channel, temp);
                                        cuser.IsHalfop = false;
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("unset halfop status: " + temp + " for: "+ircdata.Channel);
#endif
                                    }
                                    else
                                    {
#if LOG4NET
                                        Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
#endif
                                    }
                                }

                                OnDehalfop?.Invoke(this, new DehalfopEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                        }
                        break;
                    case ChannelMode.Voice:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                // sanity check
                                ChannelUser cuser = GetChannelUser(ircdata.Channel, temp);

                                if (cuser != null)
                                {
                                    // update the voice list
                                    if (channel.UnsafeVoices.TryAdd(temp, cuser))
                                    {
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("added voice: "+temp+" to: "+ircdata.Channel);
#endif
                                    }
#if LOG4NET
                                    else
                                    {
                                        Logger.ChannelSyncing.Debug("duplicate voice: "+temp+" in: "+ircdata.Channel+" not added");
                                    }
#endif

                                    // update the user voice status
                                    cuser.IsVoice = true;
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("set voice status: " + temp + " for: "+ircdata.Channel);
#endif
                                }
                                else
                                {
#if LOG4NET
                                    Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
#endif
                                }
                            }

                            OnVoice?.Invoke(this, new VoiceEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                // sanity check
                                if (GetChannelUser(ircdata.Channel, temp) != null)
                                {
                                    // update the voice list
                                    channel.UnsafeVoices.Remove(temp);
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("removed voice: "+temp+" from: "+ircdata.Channel);
#endif
                                    // update the user voice status
                                    ChannelUser cuser = GetChannelUser(ircdata.Channel, temp);
                                    cuser.IsVoice = false;
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("unset voice status: " + temp + " for: "+ircdata.Channel);
#endif
                                }
                                else
                                {
#if LOG4NET
                                    Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
#endif
                                }
                            }

                            OnDevoice?.Invoke(this, new DevoiceEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        break;
                    case ChannelMode.Ban:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                channel.Bans.Add(temp);
                                {
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("added ban: "+temp+" to: "+ircdata.Channel);
#endif
                                }
#if LOG4NET
                                else
                                {
                                    Logger.ChannelSyncing.Debug("duplicate ban: "+temp+" in: "+ircdata.Channel+" not added");
                                }
#endif
                            }
                            OnBan?.Invoke(this, new BanEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                channel.Bans.Remove(temp);
#if LOG4NET
                                Logger.ChannelSyncing.Debug("removed ban: "+temp+" from: "+ircdata.Channel);
#endif
                            }
                            OnUnban?.Invoke(this, new UnbanEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        break;
                    case ChannelMode.BanException:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                try
                                {
                                    channel.BanExceptions.Add(temp);
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("added ban exception: "+temp+" to: "+ircdata.Channel);
#endif
                                }
                                catch (ArgumentException)
                                {
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("duplicate ban exception: "+temp+" in: "+ircdata.Channel+" not added");
#endif
                                }
                            }
                            OnBanException?.Invoke(this, new BanEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                channel.BanExceptions.Remove(temp);
#if LOG4NET
                                Logger.ChannelSyncing.Debug("removed ban exception: "+temp+" from: "+ircdata.Channel);
#endif
                            }
                            OnUnBanException?.Invoke(this, new UnbanEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        break;
                    case ChannelMode.InviteException:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                try
                                {
                                    channel.InviteExceptions.Add(temp);
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("added invite exception: "+temp+" to: "+ircdata.Channel);
#endif
                                }
                                catch (ArgumentException)
                                {
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("duplicate invite exception: "+temp+" in: "+ircdata.Channel+" not added");
#endif
                                }
                            }
                            OnInviteException?.Invoke(this, new BanEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                channel.InviteExceptions.Remove(temp);
#if LOG4NET
                                Logger.ChannelSyncing.Debug("removed invite exception: "+temp+" from: "+ircdata.Channel);
#endif
                            }
                            OnUnInviteException?.Invoke(this, new UnbanEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        break;
                    case ChannelMode.UserLimit:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                try
                                {
                                    channel.UserLimit = Int32.Parse(temp);
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("stored user limit for: "+ircdata.Channel);
#endif
                                }
                                catch (FormatException)
                                {
#if LOG4NET
                                    Logger.ChannelSyncing.Error("could not parse user limit: "+temp+" channel: "+ircdata.Channel);
#endif
                                }
                            }
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                channel.UserLimit = 0;
#if LOG4NET
                                Logger.ChannelSyncing.Debug("removed user limit for: "+ircdata.Channel);
#endif
                            }
                        }
                        break;
                    case ChannelMode.Key:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                channel.Key = temp;
#if LOG4NET
                                Logger.ChannelSyncing.Debug("stored channel key for: "+ircdata.Channel);
#endif
                            }
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                channel.Key = "";
#if LOG4NET
                                Logger.ChannelSyncing.Debug("removed channel key for: "+ircdata.Channel);
#endif
                            }
                        }
                        break;
                    default:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                if (channel.Mode.IndexOf(changeInfo.ModeChar) == -1)
                                {
                                    channel.Mode += changeInfo.ModeChar;
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("added channel mode ("+changeInfo.ModeChar+") for: "+ircdata.Channel);
#endif
                                }
                            }
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                channel.Mode = channel.Mode.Replace(changeInfo.ModeChar.ToString(), "");
#if LOG4NET
                                Logger.ChannelSyncing.Debug("removed channel mode ("+changeInfo.ModeChar+") for: "+ircdata.Channel);
#endif
                            }
                        }
                        break;
                }
            }
        }

        #region Internal Messagehandlers
        /// <summary>
        /// Event handler for ping messages
        /// </summary>
        /// <param name="ircdata">Message data containing ping information</param>
        private void _Event_PING(IrcMessageData ircdata)
        {
            string server = ircdata.RawMessageArray[1].Substring(1);
#if LOG4NET
            Logger.Connection.Debug("Ping? Pong!");
#endif
            RfcPong(server, Priority.Critical);

            OnPing?.Invoke(this, new PingEventArgs(ircdata, server));
        }

        /// <summary>
        /// Event handler for PONG messages
        /// </summary>
        /// <param name="ircdata">Message data containing pong information</param>
        private void _Event_PONG(IrcMessageData ircdata) => OnPong?.Invoke(this, new PongEventArgs(ircdata, ircdata.Irc.Lag));

        /// <summary>
        /// Event handler for error messages
        /// </summary>
        /// <param name="ircdata">Message data containing error information</param>
        private void _Event_ERROR(IrcMessageData ircdata)
        {
            string message = ircdata.Message;
#if LOG4NET
            Logger.Connection.Info("received ERROR from IRC server");
#endif

            OnError?.Invoke(this, new ErrorEventArgs(ircdata, message));
        }

        /// <summary>
        /// Event handler for join messages
        /// </summary>
        /// <param name="ircdata">Message data containing join information</param>
        private void _Event_JOIN(IrcMessageData ircdata)
        {
            string who = ircdata.Nick;
            string channelname = ircdata.Channel;

            if (IsMe(who))
            {
                JoinedChannels.Add(channelname);
            }

            if (ActiveChannelSyncing)
            {
                Channel channel;
                if (IsMe(who))
                {
                    // we joined the channel
                    // we joined the channel
                    // HACK: only create and add the channel to _Channels if it
                    // doesn't exist yet. This check should not be needed but
                    // the IRCd could send a duplicate JOIN message and break
                    // our client state
                    channel = GetChannel(channelname);
                    if (channel == null)
                    {
#if LOG4NET
                        Logger.ChannelSyncing.DebugFormat(
                            "joining channel: '{0}'", channelname
                        );
#endif
                        channel = CreateChannel(channelname);
                    }
                    else
                    {
#if LOG4NET
                        Logger.ChannelSyncing.WarnFormat(
                            "joining already joined channel: '{0}', ignoring...",
                            channelname
                        );
#endif
                    }
                    _Channels[channelname] = channel;

                    // request channel mode
                    RfcMode(channelname);
                    // request wholist
                    RfcWho(channelname);
                    // request ban exception list
                    if (ServerProperties.BanExceptionCharacter.HasValue)
                    {
                        BanException(channelname);
                    }
                    // request invite exception list
                    if (ServerProperties.InviteExceptionCharacter.HasValue)
                    {
                        InviteException(channelname);
                    }
                    // request banlist
                    Ban(channelname);
                }
                else
                {
                    // someone else joined the channel
                    // request the who data
                    RfcWho(who);
                }

#if LOG4NET
                Logger.ChannelSyncing.Debug(who+" joins channel: "+channelname);
#endif
                channel = GetChannel(channelname);
                IrcUser ircuser = GetIrcUser(who);

                if (ircuser == null)
                {
                    ircuser = new IrcUser(who, this)
                    {
                        Ident = ircdata.Ident,
                        Host = ircdata.Host
                    };
                    _IrcUsers.TryAdd(who, ircuser);
                }

                // HACK: IRCnet's anonymous channel mode feature breaks our
                // channnel sync here as they use the same nick for ALL channel
                // users!
                // Example: :anonymous!anonymous@anonymous. JOIN :$channel
                if (who == "anonymous" &&
                    ircdata.Ident == "anonymous" &&
                    ircdata.Host == "anonymous." &&
                    IsJoined(channelname, who))
                {
                    // ignore
                }
                else
                {
                    ChannelUser channeluser = CreateChannelUser(channelname, ircuser);
                    channel.UnsafeUsers[who] = channeluser;
                }
            }

            OnJoin?.Invoke(this, new JoinEventArgs(ircdata, channelname, who));
        }

        /// <summary>
        /// Event handler for part messages
        /// </summary>
        /// <param name="ircdata">Message data containing part information</param>
        private void _Event_PART(IrcMessageData ircdata)
        {
            string who = ircdata.Nick;
            string channel = ircdata.Channel;
            string partmessage = ircdata.Message;

            if (IsMe(who))
            {
                JoinedChannels.Remove(channel);
            }

            if (ActiveChannelSyncing)
            {
                if (IsMe(who))
                {
#if LOG4NET
                    Logger.ChannelSyncing.Debug("parting channel: "+channel);
#endif
                    _Channels.Remove(channel);
                }
                else
                {
#if LOG4NET
                    Logger.ChannelSyncing.Debug(who+" parts channel: "+channel);
#endif
                    // HACK: IRCnet's anonymous channel mode feature breaks our
                    // channnel sync here as they use the same nick for ALL channel
                    // users!
                    // Example: :anonymous!anonymous@anonymous. PART $channel :$msg
                    if (who == "anonymous" &&
                        ircdata.Ident == "anonymous" &&
                        ircdata.Host == "anonymous." &&
                        !IsJoined(channel, who))
                    {
                        // ignore
                    }
                    else
                    {
                        _RemoveChannelUser(channel, who);
                        _RemoveIrcUser(who);
                    }
                }
            }

            OnPart?.Invoke(this, new PartEventArgs(ircdata, channel, who, partmessage));
        }

        /// <summary>
        /// Event handler for kick messages
        /// </summary>
        /// <param name="ircdata">Message data containing kick information</param>
        private void _Event_KICK(IrcMessageData ircdata)
        {
            string channelname = ircdata.Channel;
            string who = ircdata.Nick;
            if (String.IsNullOrEmpty(who))
            {
                // the server itself kicked
                who = ircdata.From;
            }
            string whom = ircdata.RawMessageArray[3];
            string reason = ircdata.Message;
            bool isme = IsMe(whom);

            if (isme)
            {
                JoinedChannels.Remove(channelname);
            }

            if (ActiveChannelSyncing)
            {
                if (isme)
                {
                    Channel channel = GetChannel(channelname);
                    _Channels.Remove(channelname);
                    if (AutoRejoinOnKick)
                    {
                        RfcJoin(channel.Name, channel.Key);
                    }
                }
                else
                {
                    _RemoveChannelUser(channelname, whom);
                    _RemoveIrcUser(whom);
                }
            }
            else
            {
                if (isme && AutoRejoinOnKick)
                {
                    RfcJoin(channelname);
                }
            }

            OnKick?.Invoke(this, new KickEventArgs(ircdata, channelname, who, whom, reason));
        }

        /// <summary>
        /// Event handler for quit messages
        /// </summary>
        /// <param name="ircdata">Message data containing quit information</param>
        private void _Event_QUIT(IrcMessageData ircdata)
        {
            string who = ircdata.Nick;
            string reason = ircdata.Message;

            // no need to handle if we quit, disconnect event will take care

            if (ActiveChannelSyncing)
            {
                // sanity checks, freshirc is very broken about RFC
                if (GetIrcUser(who) is IrcUser user)
                {
                    if (user.JoinedChannels is string[] joined_channels)
                    {
                        foreach (string channel in joined_channels)
                        {
                            _RemoveChannelUser(channel, who);
                        }
                        _RemoveIrcUser(who);
                    }
#if LOG4NET
                    else
                    {
                        Logger.ChannelSyncing.Error("user.JoinedChannels (for: '"+who+"') returned null in _Event_QUIT! Ignoring...");
                    }
#endif
                }
#if LOG4NET
                else
                {
                    Logger.ChannelSyncing.Error("GetIrcUser("+who+") returned null in _Event_QUIT! Ignoring...");
                }
#endif
            }

            OnQuit?.Invoke(this, new QuitEventArgs(ircdata, who, reason));
        }

        /// <summary>
        /// Event handler for private messages
        /// </summary>
        /// <param name="ircdata">Message data containing private message information</param>
        private void _Event_PRIVMSG(IrcMessageData ircdata)
        {
            switch (ircdata.Type)
            {
                case ReceiveType.ChannelMessage:
                    OnChannelMessage?.Invoke(this, new IrcEventArgs(ircdata));
                    break;
                case ReceiveType.ChannelAction:
                    if (OnChannelAction != null)
                    {
                        string action = ircdata.Message.Substring(8, ircdata.Message.Length - 9);
                        OnChannelAction(this, new ActionEventArgs(ircdata, action));
                    }
                    break;
                case ReceiveType.QueryMessage:
                    OnQueryMessage?.Invoke(this, new IrcEventArgs(ircdata));
                    break;
                case ReceiveType.QueryAction:
                    if (OnQueryAction != null)
                    {
                        string action = ircdata.Message.Substring(8, ircdata.Message.Length - 9);
                        OnQueryAction(this, new ActionEventArgs(ircdata, action));
                    }
                    break;
                case ReceiveType.CtcpRequest:
                    if (OnCtcpRequest != null)
                    {
                        int space_pos = ircdata.Message.IndexOf(' ');
                        string cmd = "";
                        string param = "";
                        if (space_pos != -1)
                        {
                            cmd = ircdata.Message.Substring(1, space_pos - 1);
                            param = ircdata.Message.Substring(space_pos + 1, ircdata.Message.Length - space_pos - 2);
                        }
                        else
                        {
                            cmd = ircdata.Message.Substring(1, ircdata.Message.Length - 2);
                        }
                        OnCtcpRequest(this, new CtcpEventArgs(ircdata, cmd, param));
                    }
                    break;
            }
        }

        /// <summary>
        /// Event handler for notice messages
        /// </summary>
        /// <param name="ircdata">Message data containing notice information</param>
        private void _Event_NOTICE(IrcMessageData ircdata)
        {
            switch (ircdata.Type)
            {
                case ReceiveType.ChannelNotice:
                    OnChannelNotice?.Invoke(this, new IrcEventArgs(ircdata));
                    break;
                case ReceiveType.QueryNotice:
                    OnQueryNotice?.Invoke(this, new IrcEventArgs(ircdata));
                    break;
                case ReceiveType.CtcpReply:
                    if (OnCtcpReply != null)
                    {
                        int space_pos = ircdata.Message.IndexOf(' ');
                        string cmd = "";
                        string param = "";
                        if (space_pos != -1)
                        {
                            cmd = ircdata.Message.Substring(1, space_pos - 1);
                            param = ircdata.Message.Substring(space_pos + 1, ircdata.Message.Length - space_pos - 2);
                        }
                        else
                        {
                            cmd = ircdata.Message.Substring(1, ircdata.Message.Length - 2);
                        }
                        OnCtcpReply(this, new CtcpEventArgs(ircdata, cmd, param));
                    }
                    break;
            }
        }

        /// <summary>
        /// Event handler for topic messages
        /// </summary>
        /// <param name="ircdata">Message data containing topic information</param>
        private void _Event_TOPIC(IrcMessageData ircdata)
        {
            string who = ircdata.Nick;
            string channel = ircdata.Channel;
            string newtopic = ircdata.Message;

            if (ActiveChannelSyncing && IsJoined(channel))
            {
                GetChannel(channel).Topic = newtopic;
#if LOG4NET
                Logger.ChannelSyncing.Debug("stored topic for channel: "+channel);
#endif
            }

            OnTopicChange?.Invoke(this, new TopicChangeEventArgs(ircdata, channel, who, newtopic));
        }

        /// <summary>
        /// Event handler for nickname messages
        /// </summary>
        /// <param name="ircdata">Message data containing nickname information</param>
        private void _Event_NICK(IrcMessageData ircdata)
        {
            string oldnickname = ircdata.Nick;
            //string newnickname = ircdata.Message;
            // the colon in the NICK message is optional, thus we can't rely on Message
            string newnickname = ircdata.RawMessageArray[2];

            // so let's strip the colon if it's there
            if (newnickname.StartsWith(":", StringComparison.Ordinal))
            {
                newnickname = newnickname.Substring(1);
            }

            if (IsMe(ircdata.Nick))
            {
                // nickname change is your own
                Nickname = newnickname;
            }

            if (ActiveChannelSyncing)
            {
                IrcUser ircuser = GetIrcUser(oldnickname);

                // if we don't have any info about him, don't update him!
                // (only queries or ourself in no channels)
                if (ircuser != null)
                {
                    string[] joinedchannels = ircuser.JoinedChannels;

                    // update his nickname
                    ircuser.Nick = newnickname;
                    // remove the old entry
                    // remove first to avoid duplication, Foo -> foo
                    _IrcUsers.Remove(oldnickname);
                    // add him as new entry and new nickname as key
                    _IrcUsers.TryAdd(newnickname, ircuser);
#if LOG4NET
                    Logger.ChannelSyncing.Debug("updated nickname of: "+oldnickname+" to: "+newnickname);
#endif
                    // now the same for all channels he is joined
                    foreach (string channelname in joinedchannels)
                    {
                        Channel channel = GetChannel(channelname);
                        ChannelUser channeluser = GetChannelUser(channelname, oldnickname);
                        // remove first to avoid duplication, Foo -> foo
                        channel.UnsafeUsers.Remove(oldnickname);
                        channel.UnsafeUsers.TryAdd(newnickname, channeluser);
                        if (channeluser.IsOp)
                        {
                            channel.UnsafeOps.Remove(oldnickname);
                            channel.UnsafeOps.TryAdd(newnickname, channeluser);
                        }
                        if (channeluser.IsVoice)
                        {
                            channel.UnsafeVoices.Remove(oldnickname);
                            channel.UnsafeVoices.TryAdd(newnickname, channeluser);
                        }
                        if (SupportNonRfc)
                        {
                            var channeluser1 = ((NonRfcChannelUser)channeluser);
                            if (channeluser1.IsOwner)
                            {
                                var nchannel = (NonRfcChannel)channel;
                                nchannel.UnsafeOwners.Remove(oldnickname);
                                nchannel.UnsafeOwners.TryAdd(newnickname, (NonRfcChannelUser)channeluser);
                            }
                            if (channeluser1.IsChannelAdmin)
                            {
                                var nchannel = (NonRfcChannel)channel;
                                nchannel.UnsafeChannelAdmins.Remove(oldnickname);
                                nchannel.UnsafeChannelAdmins.TryAdd(newnickname, (NonRfcChannelUser)channeluser);
                            }
                            if (channeluser1.IsHalfop)
                            {
                                var nchannel = (NonRfcChannel)channel;
                                nchannel.UnsafeHalfops.Remove(oldnickname);
                                nchannel.UnsafeHalfops.TryAdd(newnickname, (NonRfcChannelUser)channeluser);
                            }
                        }
                    }
                }
            }

            OnNickChange?.Invoke(this, new NickChangeEventArgs(ircdata, oldnickname, newnickname));
        }

        /// <summary>
        /// Event handler for invite messages
        /// </summary>
        /// <param name="ircdata">Message data containing invite information</param>
        private void _Event_INVITE(IrcMessageData ircdata)
        {
            string channel = ircdata.Channel;
            string inviter = ircdata.Nick;

            if (AutoJoinOnInvite)
            {
                if (channel.Trim() != "0")
                {
                    RfcJoin(channel);
                }
            }

            OnInvite?.Invoke(this, new InviteEventArgs(ircdata, channel, inviter));
        }

        /// <summary>
        /// Event handler for mode messages
        /// </summary>
        /// <param name="ircdata">Message data containing mode information</param>
        private void _Event_MODE(IrcMessageData ircdata)
        {
            if (IsMe(ircdata.RawMessageArray[2]))
            {
                // my user mode changed
                Usermode = ircdata.RawMessageArray[3].Substring(1);

                OnUserModeChange?.Invoke(this, new IrcEventArgs(ircdata));
            }
            else
            {
                // channel mode changed
                string mode = ircdata.RawMessageArray[3];
                string parameter = String.Join(" ", ircdata.RawMessageArray, 4, ircdata.RawMessageArray.Length - 4);
                var changeInfos = ChannelModeChangeInfo.Parse(ChannelModeMap, ircdata.Channel, mode, parameter);
                _InterpretChannelMode(ircdata, changeInfos);

                OnChannelModeChange?.Invoke(this, new ChannelModeChangeEventArgs(ircdata, ircdata.Channel, changeInfos));
            }

            OnModeChange?.Invoke(this, new IrcEventArgs(ircdata));
        }


        /// <summary>
        /// Event handler for channel mode reply messages
        /// </summary>
        /// <param name="ircdata">Message data containing reply information</param>
        private void _Event_RPL_CHANNELMODEIS(IrcMessageData ircdata)
        {
            if (ActiveChannelSyncing && IsJoined(ircdata.Channel))
            {
                // reset stored mode first, as this is the complete mode
                Channel chan = GetChannel(ircdata.Channel);
                chan.Mode = "";
                string mode = ircdata.RawMessageArray[4];
                string parameter = String.Join(" ", ircdata.RawMessageArray, 5, ircdata.RawMessageArray.Length - 5);
                var changeInfos = ChannelModeChangeInfo.Parse(ChannelModeMap, ircdata.Channel, mode, parameter);
                _InterpretChannelMode(ircdata, changeInfos);
            }
        }

        /// <summary>
        /// Event handler for welcome reply messages
        /// </summary>
        /// <remark>
        /// Upon success, the client will receive an RPL_WELCOME (for users) or
        /// RPL_YOURESERVICE (for services) message indicating that the
        /// connection is now registered and known the to the entire IRC network.
        /// The reply message MUST contain the full client identifier upon which
        /// it was registered.
        /// </remark>
        /// <param name="ircdata">Message data containing reply information</param>
        private void _Event_RPL_WELCOME(IrcMessageData ircdata)
        {
            // updating our nickname, that we got (maybe cutted...)
            Nickname = ircdata.RawMessageArray[2];

            OnRegistered?.Invoke(this, EventArgs.Empty);
        }

        private void _Event_RPL_TOPIC(IrcMessageData ircdata)
        {
            string topic = ircdata.Message;
            string channel = ircdata.Channel;

            if (ActiveChannelSyncing && IsJoined(channel))
            {
                GetChannel(channel).Topic = topic;
#if LOG4NET
                Logger.ChannelSyncing.Debug("stored topic for channel: "+channel);
#endif
            }

            OnTopic?.Invoke(this, new TopicEventArgs(ircdata, channel, topic));
        }

        private void _Event_RPL_NOTOPIC(IrcMessageData ircdata)
        {
            string channel = ircdata.Channel;

            if (ActiveChannelSyncing && IsJoined(channel))
            {
                GetChannel(channel).Topic = "";
#if LOG4NET
                Logger.ChannelSyncing.Debug("stored empty topic for channel: "+channel);
#endif
            }

            OnTopic?.Invoke(this, new TopicEventArgs(ircdata, channel, ""));
        }

        private void _Event_RPL_NAMREPLY(IrcMessageData ircdata)
        {
            string channelname = ircdata.Channel;
            string[] userlist = ircdata.MessageArray;
            // HACK: BIP skips the colon after the channel name even though
            // RFC 1459 and 2812 says it's mandantory in RPL_NAMREPLY
            if (userlist == null)
            {
                if (ircdata.RawMessageArray.Length > 5)
                {
                    userlist = new string[] { ircdata.RawMessageArray[5] };
                }
                else
                {
                    userlist = new string[0];
                }
            }

            if (ActiveChannelSyncing && IsJoined(channelname))
            {
                foreach (string user in userlist)
                {
                    if (user.Length <= 0)
                    {
                        continue;
                    }

                    bool owner = false;
                    bool chanadmin = false;
                    bool op = false;
                    bool halfop = false;
                    bool voice = false;

                    string nickname = user;

                    foreach (var kvp in ServerProperties.ChannelPrivilegeModesPrefixes)
                    {
                        if (nickname[0] == kvp.Value)
                        {
                            nickname = nickname.Substring(1);

                            switch (kvp.Key)
                            {
                                case 'q':
                                    owner = true;
                                    break;
                                case 'a':
                                    chanadmin = true;
                                    break;
                                case 'o':
                                    op = true;
                                    break;
                                case 'h':
                                    halfop = true;
                                    break;
                                case 'v':
                                    voice = true;
                                    break;
                            }
                        }
                    }

                    IrcUser ircuser = GetIrcUser(nickname);
                    ChannelUser channeluser = GetChannelUser(channelname, nickname);

                    if (ircuser == null)
                    {
#if LOG4NET
                        Logger.ChannelSyncing.Debug("creating IrcUser: "+nickname+" because he doesn't exist yet");
#endif
                        ircuser = new IrcUser(nickname, this);
                        _IrcUsers.TryAdd(nickname, ircuser);
                    }

                    if (channeluser == null)
                    {
#if LOG4NET
                        Logger.ChannelSyncing.Debug("creating ChannelUser: "+nickname+" for Channel: "+channelname+" because he doesn't exist yet");
#endif

                        channeluser = CreateChannelUser(channelname, ircuser);
                        Channel channel = GetChannel(channelname);

                        channel.UnsafeUsers.TryAdd(nickname, channeluser);
                        if (op)
                        {
                            channel.UnsafeOps.TryAdd(nickname, channeluser);
#if LOG4NET
                            Logger.ChannelSyncing.Debug("added op: " + nickname + " to: " + channelname);
#endif
                        }
                        if (voice)
                        {
                            channel.UnsafeVoices.TryAdd(nickname, channeluser);
#if LOG4NET
                            Logger.ChannelSyncing.Debug("added voice: " + nickname + " to: " + channelname);
#endif
                        }
                        if (SupportNonRfc)
                        {
                            if (owner)
                            {
                                ((NonRfcChannel)channel).UnsafeOwners.TryAdd(nickname, (NonRfcChannelUser)channeluser);
#if LOG4NET
                                Logger.ChannelSyncing.Debug("added owner: " + nickname + " to: " + channelname);
#endif
                            }
                            if (chanadmin)
                            {
                                ((NonRfcChannel)channel).UnsafeChannelAdmins.TryAdd(nickname, (NonRfcChannelUser)channeluser);
#if LOG4NET
                                Logger.ChannelSyncing.Debug("added channel admin: " + nickname + " to: " + channelname);
#endif
                            }
                            if (halfop)
                            {
                                ((NonRfcChannel)channel).UnsafeHalfops.TryAdd(nickname, (NonRfcChannelUser)channeluser);
#if LOG4NET
                                Logger.ChannelSyncing.Debug("added halfop: " + nickname + " to: " + channelname);
#endif
                            }
                        }
                    }

                    channeluser.IsOp = op;
                    channeluser.IsVoice = voice;
                    if (SupportNonRfc)
                    {
                        var nchanneluser = (NonRfcChannelUser)channeluser;
                        nchanneluser.IsOwner = owner;
                        nchanneluser.IsChannelAdmin = chanadmin;
                        nchanneluser.IsHalfop = halfop;
                    }
                }
            }

            var filteredUserlist = new List<string>(userlist.Length);
            // filter user modes from nicknames
            foreach (string user in userlist)
            {
                if (String.IsNullOrEmpty(user))
                {
                    continue;
                }

                string temp = user;
                foreach (var kvp in ServerProperties.ChannelPrivilegeModesPrefixes)
                {
                    if (temp[0] == kvp.Value)
                    {
                        temp = temp.Substring(1);
                    }
                }
                filteredUserlist.Add(temp);

            }

            OnNames?.Invoke(this, new NamesEventArgs(ircdata, channelname, filteredUserlist.ToArray(), userlist));
        }

        private void _Event_RPL_LIST(IrcMessageData ircdata)
        {
            string channelName = ircdata.Channel;
            int userCount = Int32.Parse(ircdata.RawMessageArray[4]);
            string topic = ircdata.Message;

            ChannelInfo info = null;
            if (OnList != null || _ChannelList != null)
            {
                info = new ChannelInfo(channelName, userCount, topic);
            }

            _ChannelList?.Add(info);

            OnList?.Invoke(this, new ListEventArgs(ircdata, info));
        }

        private void _Event_RPL_LISTEND(IrcMessageData ircdata) => _ChannelListReceivedEvent?.Set();

        private void _Event_RPL_TRYAGAIN(IrcMessageData ircdata) => _ChannelListReceivedEvent?.Set();

        /*
        // BUG: RFC2812 says LIST and WHO might return ERR_TOOMANYMATCHES which
        // is not defined :(
        private void _Event_ERR_TOOMANYMATCHES(IrcMessageData ircdata)
        {
            if (_ListInfosReceivedEvent != null) {
                _ListInfosReceivedEvent.Set();
            }
        }
        */

        private void _Event_RPL_ENDOFNAMES(IrcMessageData ircdata)
        {
            string channelname = ircdata.RawMessageArray[3];
            if (ActiveChannelSyncing && IsJoined(channelname))
            {
#if LOG4NET
                Logger.ChannelSyncing.Debug("passive synced: " + channelname);
#endif
                OnChannelPassiveSynced?.Invoke(this, new IrcEventArgs(ircdata));
            }
        }

        private void _Event_RPL_AWAY(IrcMessageData ircdata)
        {
            string who = ircdata.RawMessageArray[3];
            string awaymessage = ircdata.Message;

            if (ActiveChannelSyncing)
            {
                IrcUser ircuser = GetIrcUser(who);
                if (ircuser != null)
                {
#if LOG4NET
                    Logger.ChannelSyncing.Debug("setting away flag for user: " + who);
#endif
                    ircuser.IsAway = true;
                }
            }

            OnAway?.Invoke(this, new AwayEventArgs(ircdata, who, awaymessage));
        }

        private void _Event_RPL_UNAWAY(IrcMessageData ircdata)
        {
            IsAway = false;

            OnUnAway?.Invoke(this, new IrcEventArgs(ircdata));
        }

        private void _Event_RPL_NOWAWAY(IrcMessageData ircdata)
        {
            IsAway = true;

            OnNowAway?.Invoke(this, new IrcEventArgs(ircdata));
        }

        private void _Event_RPL_WHOREPLY(IrcMessageData ircdata)
        {
            var info = WhoInfo.Parse(ircdata);
            string channel = info.Channel;
            string nick = info.Nick;

            _WhoList?.Add(info);

            if (ActiveChannelSyncing && IsJoined(channel))
            {
                // checking the irc and channel user I only do for sanity!
                // according to RFC they must be known to us already via RPL_NAMREPLY
                // psyBNC is not very correct with this... maybe other bouncers too
                IrcUser ircuser = GetIrcUser(nick);
                ChannelUser channeluser = GetChannelUser(channel, nick);
#if LOG4NET
                if (ircuser == null)
                {
                    Logger.ChannelSyncing.Error("GetIrcUser(" + nick + ") returned null in _Event_WHOREPLY! Ignoring...");
                }
#endif

#if LOG4NET
                if (channeluser == null)
                {
                    Logger.ChannelSyncing.Error("GetChannelUser(" + nick + ") returned null in _Event_WHOREPLY! Ignoring...");
                }
#endif

                if (ircuser != null)
                {
#if LOG4NET
                    Logger.ChannelSyncing.Debug("updating userinfo (from whoreply) for user: " + nick + " channel: " + channel);
#endif

                    ircuser.Ident = info.Ident;
                    ircuser.Host = info.Host;
                    ircuser.Server = info.Server;
                    ircuser.Nick = info.Nick;
                    ircuser.HopCount = info.HopCount;
                    ircuser.Realname = info.Realname;
                    ircuser.IsAway = info.IsAway;
                    ircuser.IsIrcOp = info.IsIrcOp;
                    ircuser.IsRegistered = info.IsRegistered;

                    switch (channel[0])
                    {
                        case '#':
                        case '!':
                        case '&':
                        case '+':
                            // this channel may not be where we are joined!
                            // see RFC 1459 and RFC 2812, it must return a channelname
                            // we use this channel info when possible...
                            if (channeluser != null)
                            {
                                channeluser.IsOp = info.IsOp;
                                channeluser.IsVoice = info.IsVoice;
                            }
                            break;
                    }
                }
            }

            OnWho?.Invoke(this, new WhoEventArgs(ircdata, info));
        }

        private void _Event_RPL_ENDOFWHO(IrcMessageData ircdata) => _WhoListReceivedEvent?.Set();

        private void _Event_RPL_MOTD(IrcMessageData ircdata)
        {
            if (!_MotdReceived)
            {
                Motd.Add(ircdata.Message);
            }

            OnMotd?.Invoke(this, new MotdEventArgs(ircdata, ircdata.Message));
        }

        private void _Event_RPL_ENDOFMOTD(IrcMessageData ircdata) => _MotdReceived = true;

        private void _Event_RPL_BANLIST(IrcMessageData ircdata)
        {
            string channelname = ircdata.Channel;

            var info = BanInfo.Parse(ircdata);
            _BanList?.Add(info);

            if (ActiveChannelSyncing && IsJoined(channelname))
            {
                Channel channel = GetChannel(channelname);
                if (channel.IsSycned)
                {
                    return;
                }

                channel.Bans.Add(info.Mask);
            }
        }

        private void _Event_RPL_ENDOFBANLIST(IrcMessageData ircdata)
        {
            string channelname = ircdata.Channel;

            _BanListReceivedEvent?.Set();

            if (ActiveChannelSyncing && IsJoined(channelname))
            {
                Channel channel = GetChannel(channelname);
                if (channel.IsSycned)
                {
                    // only fire the event once
                    return;
                }

                channel.ActiveSyncStop = DateTime.Now;
                channel.IsSycned = true;
#if LOG4NET
                Logger.ChannelSyncing.Debug("active synced: " + channelname + " (in " + channel.ActiveSyncTime.TotalSeconds + " sec)");
#endif
                OnChannelActiveSynced?.Invoke(this, new IrcEventArgs(ircdata));
            }
        }

        private void _Event_RPL_EXCEPTLIST(IrcMessageData ircdata)
        {
            string channelname = ircdata.Channel;

            var info = BanInfo.Parse(ircdata);
            _BanExceptList?.Add(info);

            if (ActiveChannelSyncing && IsJoined(channelname))
            {
                Channel channel = GetChannel(channelname);
                if (channel.IsSycned)
                {
                    return;
                }

                channel.BanExceptions.Add(info.Mask);
            }
        }

        private void _Event_RPL_ENDOFEXCEPTLIST(IrcMessageData ircdata)
        {
            string channelname = ircdata.Channel;

            _BanExceptListReceivedEvent?.Set();
        }

        private void _Event_RPL_INVITELIST(IrcMessageData ircdata)
        {
            string channelname = ircdata.Channel;

            var info = BanInfo.Parse(ircdata);
            _InviteExceptList?.Add(info);

            if (ActiveChannelSyncing && IsJoined(channelname))
            {
                Channel channel = GetChannel(channelname);
                if (channel.IsSycned)
                {
                    return;
                }

                channel.InviteExceptions.Add(info.Mask);
            }
        }

        private void _Event_RPL_ENDOFINVITELIST(IrcMessageData ircdata)
        {
            string channelname = ircdata.Channel;

            _InviteExceptListReceivedEvent?.Set();
        }

        // MODE +b might return ERR_NOCHANMODES for mode-less channels (like +chan)
        private void _Event_ERR_NOCHANMODES(IrcMessageData ircdata)
        {
            string channelname = ircdata.RawMessageArray[3];
            if (ActiveChannelSyncing && IsJoined(channelname))
            {
                Channel channel = GetChannel(channelname);
                if (channel.IsSycned)
                {
                    // only fire the event once
                    return;
                }

                channel.ActiveSyncStop = DateTime.Now;
                channel.IsSycned = true;
#if LOG4NET
                Logger.ChannelSyncing.Debug("active synced: " + channelname + " (in " + channel.ActiveSyncTime.TotalSeconds + " sec)");
#endif
                OnChannelActiveSynced?.Invoke(this, new IrcEventArgs(ircdata));
            }
        }

        private void _Event_ERR(IrcMessageData ircdata) => OnErrorMessage?.Invoke(this, new IrcEventArgs(ircdata));

        private void _Event_ERR_NICKNAMEINUSE(IrcMessageData ircdata)
        {
#if LOG4NET
            Logger.Connection.Warn("nickname collision detected, changing nickname");
#endif
            if (!AutoNickHandling)
            {
                return;
            }

            string nickname;
            // if a nicklist has been given loop through the nicknames
            // if the upper limit of this list has been reached and still no nickname has registered
            // then generate a random nick
            if (_CurrentNickname == NicknameList.Length - 1)
            {
                var rand = new Random();
                int number = rand.Next(999);
                if (Nickname.Length > 5)
                {
                    nickname = Nickname.Substring(0, 5) + number;
                }
                else
                {
                    nickname = Nickname.Substring(0, Nickname.Length - 1) + number;
                }
            }
            else
            {
                nickname = _NextNickname();
            }
            // change the nickname
            RfcNick(nickname, Priority.Critical);
        }

        private void _Event_RPL_BOUNCE(IrcMessageData ircdata)
        {
            // HACK: might be BOUNCE or ISUPPORT; try to detect
            if (ircdata.Message?.StartsWith("Try server ", StringComparison.Ordinal) ?? false)
            {
                // BOUNCE
                string host = null;
                int port = -1;
                // try to parse out host and port
                var match = _BounceMessageRegex.Match(ircdata.Message);
                if (match.Success)
                {
                    host = match.Groups[1].Value;
                    port = Int32.Parse(match.Groups[2].Value);
                }

                OnBounce?.Invoke(this, new BounceEventArgs(ircdata, host, port));
                return;
            }

            // ISUPPORT
            ServerProperties.ParseFromRawMessage(ircdata.RawMessageArray);
            if (ircdata.RawMessageArray.Any(x => x.StartsWith("CHANMODES=", StringComparison.Ordinal)))
            {
                string chanModes = ServerProperties.RawProperties["CHANMODES"];
                if (!String.IsNullOrEmpty(chanModes))
                {
                    ChannelModeMap = new ChannelModeMap(chanModes);
                }
            }
            if (ServerProperties.RawProperties.ContainsKey("NAMESX"))
            {
                WriteLine("PROTOCTL NAMESX", Priority.Critical);
            }
        }
        #endregion
    }
}
