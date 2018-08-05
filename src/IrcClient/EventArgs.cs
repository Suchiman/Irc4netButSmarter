/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2003-2005 Mirco Bauer <meebey@meebey.net> <http://www.meebey.net>
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
using System.Collections.Generic;

namespace Meebey.SmartIrc4net
{
    public class ActionEventArgs : CtcpEventArgs
    {
        public string ActionMessage { get; }

        internal ActionEventArgs(IrcMessageData data, string actionmsg) : base(data, "ACTION", actionmsg) => ActionMessage = actionmsg;
    }

    public class CtcpEventArgs : IrcEventArgs
    {
        public string CtcpCommand { get; }
        public string CtcpParameter { get; }

        internal CtcpEventArgs(IrcMessageData data, string ctcpcmd, string ctcpparam) : base(data)
        {
            CtcpCommand = ctcpcmd;
            CtcpParameter = ctcpparam;
        }
    }

    public class ErrorEventArgs : IrcEventArgs
    {
        public string ErrorMessage { get; }

        internal ErrorEventArgs(IrcMessageData data, string errormsg) : base(data) => ErrorMessage = errormsg;
    }

    public class MotdEventArgs : IrcEventArgs
    {
        public string MotdMessage { get; }

        internal MotdEventArgs(IrcMessageData data, string motdmsg) : base(data) => MotdMessage = motdmsg;
    }

    public class PingEventArgs : IrcEventArgs
    {
        public string PingData { get; }

        internal PingEventArgs(IrcMessageData data, string pingdata) : base(data) => PingData = pingdata;
    }

    public class PongEventArgs : IrcEventArgs
    {
        public TimeSpan Lag { get; }

        internal PongEventArgs(IrcMessageData data, TimeSpan lag) : base(data) => Lag = lag;
    }

    public class KickEventArgs : IrcEventArgs
    {
        public string Channel { get; }
        public string Who { get; }
        public string Whom { get; }
        public string KickReason { get; }

        internal KickEventArgs(IrcMessageData data, string channel, string who, string whom, string kickreason) : base(data)
        {
            Channel = channel;
            Who = who;
            Whom = whom;
            KickReason = kickreason;
        }
    }

    public class JoinEventArgs : IrcEventArgs
    {
        public string Channel { get; }
        public string Who { get; }

        internal JoinEventArgs(IrcMessageData data, string channel, string who) : base(data)
        {
            Channel = channel;
            Who = who;
        }
    }

    public class NamesEventArgs : IrcEventArgs
    {
        public string[] RawUserList { get; }
        public string Channel { get; }
        public string[] UserList { get; }

        internal NamesEventArgs(IrcMessageData data, string channel, string[] userlist, string[] rawUserList) : base(data)
        {
            Channel = channel;
            UserList = userlist;
            RawUserList = rawUserList;
        }
    }

    public class ListEventArgs : IrcEventArgs
    {
        public ChannelInfo ListInfo { get; }

        internal ListEventArgs(IrcMessageData data, ChannelInfo listInfo) : base(data) => ListInfo = listInfo;
    }

    public class InviteEventArgs : IrcEventArgs
    {
        public string Channel { get; }
        public string Who { get; }

        internal InviteEventArgs(IrcMessageData data, string channel, string who) : base(data)
        {
            Channel = channel;
            Who = who;
        }
    }

    public class PartEventArgs : IrcEventArgs
    {
        public string Channel { get; }
        public string Who { get; }
        public string PartMessage { get; }

        internal PartEventArgs(IrcMessageData data, string channel, string who, string partmessage) : base(data)
        {
            Channel = channel;
            Who = who;
            PartMessage = partmessage;
        }
    }

    public class WhoEventArgs : IrcEventArgs
    {
        public WhoInfo WhoInfo { get; }

        internal WhoEventArgs(IrcMessageData data, WhoInfo whoInfo) : base(data) => WhoInfo = whoInfo;
    }

    public class QuitEventArgs : IrcEventArgs
    {
        public string Who { get; }
        public string QuitMessage { get; }

        internal QuitEventArgs(IrcMessageData data, string who, string quitmessage) : base(data)
        {
            Who = who;
            QuitMessage = quitmessage;
        }
    }

    public class AwayEventArgs : IrcEventArgs
    {
        public string Who { get; }
        public string AwayMessage { get; }

        internal AwayEventArgs(IrcMessageData data, string who, string awaymessage) : base(data)
        {
            Who = who;
            AwayMessage = awaymessage;
        }
    }

    public class NickChangeEventArgs : IrcEventArgs
    {
        public string OldNickname { get; }
        public string NewNickname { get; }

        internal NickChangeEventArgs(IrcMessageData data, string oldnick, string newnick) : base(data)
        {
            OldNickname = oldnick;
            NewNickname = newnick;
        }
    }

    public class TopicEventArgs : IrcEventArgs
    {
        public string Channel { get; }
        public string Topic { get; }

        internal TopicEventArgs(IrcMessageData data, string channel, string topic) : base(data)
        {
            Channel = channel;
            Topic = topic;
        }
    }

    public class TopicChangeEventArgs : IrcEventArgs
    {
        public string Channel { get; }
        public string Who { get; }
        public string NewTopic { get; }

        internal TopicChangeEventArgs(IrcMessageData data, string channel, string who, string newtopic) : base(data)
        {
            Channel = channel;
            Who = who;
            NewTopic = newtopic;
        }
    }

    public class BanEventArgs : IrcEventArgs
    {
        public string Channel { get; }
        public string Who { get; }
        public string Hostmask { get; }

        internal BanEventArgs(IrcMessageData data, string channel, string who, string hostmask) : base(data)
        {
            Channel = channel;
            Who = who;
            Hostmask = hostmask;
        }
    }

    public class UnbanEventArgs : IrcEventArgs
    {
        public string Channel { get; }
        public string Who { get; }
        public string Hostmask { get; }

        internal UnbanEventArgs(IrcMessageData data, string channel, string who, string hostmask) : base(data)
        {
            Channel = channel;
            Who = who;
            Hostmask = hostmask;
        }
    }

    /// <summary>
    /// Event arguments for any change in channel role.
    /// </summary>
    public class ChannelRoleChangeEventArgs : IrcEventArgs
    {
        public string Channel { get; }
        public string Who { get; }
        public string Whom { get; }

        internal ChannelRoleChangeEventArgs(IrcMessageData data, string channel, string who, string whom) : base(data)
        {
            Channel = channel;
            Who = who;
            Whom = whom;
        }
    }

    /// <summary>
    /// User gained owner status (non-RFC, channel mode +q, prefix ~).
    /// </summary>
    public class OwnerEventArgs : ChannelRoleChangeEventArgs
    {
        internal OwnerEventArgs(IrcMessageData data, string channel, string who, string whom) : base(data, channel, who, whom)
        {
        }
    }

    /// <summary>
    /// User lost owner status (non-RFC, channel mode -q).
    /// </summary>
    public class DeownerEventArgs : ChannelRoleChangeEventArgs
    {
        internal DeownerEventArgs(IrcMessageData data, string channel, string who, string whom) : base(data, channel, who, whom)
        {
        }
    }

    /// <summary>
    /// User gained channel admin status (non-RFC, channel mode +a, prefix &amp;).
    /// </summary>
    public class ChannelAdminEventArgs : ChannelRoleChangeEventArgs
    {
        internal ChannelAdminEventArgs(IrcMessageData data, string channel, string who, string whom) : base(data, channel, who, whom)
        {
        }
    }

    /// <summary>
    /// User lost channel admin status (non-RFC, channel mode -a).
    /// </summary>
    public class DeChannelAdminEventArgs : ChannelRoleChangeEventArgs
    {
        internal DeChannelAdminEventArgs(IrcMessageData data, string channel, string who, string whom) : base(data, channel, who, whom)
        {
        }
    }

    /// <summary>
    /// User gained op status (channel mode +o, prefix @).
    /// </summary>
    public class OpEventArgs : ChannelRoleChangeEventArgs
    {
        internal OpEventArgs(IrcMessageData data, string channel, string who, string whom) : base(data, channel, who, whom)
        {
        }
    }

    /// <summary>
    /// User lost op status (channel mode -o).
    /// </summary>
    public class DeopEventArgs : ChannelRoleChangeEventArgs
    {
        internal DeopEventArgs(IrcMessageData data, string channel, string who, string whom) : base(data, channel, who, whom)
        {
        }
    }

    /// <summary>
    /// User gained halfop status (non-RFC, channel mode +h, prefix %).
    /// </summary>
    public class HalfopEventArgs : ChannelRoleChangeEventArgs
    {
        internal HalfopEventArgs(IrcMessageData data, string channel, string who, string whom) : base(data, channel, who, whom)
        {
        }
    }

    /// <summary>
    /// User lost halfop status (non-RFC, channel mode -h).
    /// </summary>
    public class DehalfopEventArgs : ChannelRoleChangeEventArgs
    {
        internal DehalfopEventArgs(IrcMessageData data, string channel, string who, string whom) : base(data, channel, who, whom)
        {
        }
    }

    /// <summary>
    /// User gained voice status (channel mode +v, prefix +).
    /// </summary>
    public class VoiceEventArgs : ChannelRoleChangeEventArgs
    {
        internal VoiceEventArgs(IrcMessageData data, string channel, string who, string whom) : base(data, channel, who, whom)
        {
        }
    }

    /// <summary>
    /// User lost voice status (channel mode -v).
    /// </summary>
    public class DevoiceEventArgs : ChannelRoleChangeEventArgs
    {
        internal DevoiceEventArgs(IrcMessageData data, string channel, string who, string whom) : base(data, channel, who, whom)
        {
        }
    }

    public class BounceEventArgs : IrcEventArgs
    {
        /// <summary>
        /// Hostname/address of the server to which the user is being redirected.
        /// May be null if not successfully parsed from the message.
        /// </summary>
        public string Server { get; }

        /// <summary>
        /// Port of the server to which the user is being redirected.
        /// May be -1 if not successfully parsed from the message.
        /// </summary>
        public int Port { get; }

        internal BounceEventArgs(IrcMessageData data, string server, int port) : base(data)
        {
            Server = server;
            Port = port;
        }
    }

    public class ChannelModeChangeEventArgs : IrcEventArgs
    {
        public string Channel { get; }
        public List<ChannelModeChangeInfo> ModeChanges { get; }

        internal ChannelModeChangeEventArgs(IrcMessageData data, string channel, List<ChannelModeChangeInfo> modeChanges) : base(data)
        {
            Channel = channel;
            ModeChanges = modeChanges;
        }
    }
}
