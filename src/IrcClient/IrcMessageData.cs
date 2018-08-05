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

using System.Collections.Generic;

namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// This class contains an IRC message in a parsed form
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    public class IrcMessageData
    {
        /// <summary>
        /// Gets the IrcClient object the message originated from
        /// </summary>
        public IrcClient Irc { get; }

        /// <summary>
        /// Gets the combined nickname, identity and hostname of the user that sent the message
        /// </summary>
        /// <example>
        /// nick!ident@host
        /// </example>
        public string From { get; }

        /// <summary>
        /// Gets the nickname of the user that sent the message
        /// </summary>
        public string Nick { get; }

        /// <summary>
        /// Gets the identity (username) of the user that sent the message
        /// </summary>
        public string Ident { get; }

        /// <summary>
        /// Gets the hostname of the user that sent the message
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Gets the channel the message originated from
        /// </summary>
        public string Channel { get; }

        /// <summary>
        /// Gets the message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the message as an array of strings (splitted by space)
        /// </summary>
        public string[] MessageArray { get; }

        /// <summary>
        /// Gets the raw message sent by the server
        /// </summary>
        public string RawMessage { get; }

        /// <summary>
        /// Gets the raw message sent by the server as array of strings (splitted by space)
        /// </summary>
        public string[] RawMessageArray { get; }

        /// <summary>
        /// Gets the message tags sent by the server as a dictionary
        /// </summary>
        public Dictionary<string, string> Tags { get; }

        /// <summary>
        /// Gets the message type
        /// </summary>
        public ReceiveType Type { get; }

        /// <summary>
        /// Gets the message reply code
        /// </summary>
        public ReplyCode ReplyCode { get; }

        /// <summary>
        /// Constructor to create an instance of IrcMessageData
        /// </summary>
        /// <param name="ircclient">IrcClient the message originated from</param>
        /// <param name="from">combined nickname, identity and host of the user that sent the message (nick!ident@host)</param>
        /// <param name="nick">nickname of the user that sent the message</param>
        /// <param name="ident">identity (username) of the userthat sent the message</param>
        /// <param name="host">hostname of the user that sent the message</param>
        /// <param name="channel">channel the message originated from</param>
        /// <param name="message">message</param>
        /// <param name="rawmessage">raw message sent by the server</param>
        /// <param name="type">message type</param>
        /// <param name="replycode">message reply code</param>
        /// <param name="tags">Dictionary of separated and unescaped tags</param>
        public IrcMessageData(IrcClient ircclient, string from, string nick, string ident, string host, string channel, string message, string rawmessage, ReceiveType type, ReplyCode replycode, Dictionary<string, string> tags)
        {
            Irc = ircclient;
            RawMessage = rawmessage;
            RawMessageArray = rawmessage.Split(new char[] { ' ' });
            Type = type;
            ReplyCode = replycode;
            From = from;
            Nick = nick;
            Ident = ident;
            Host = host;
            Channel = channel;
            if (message != null)
            {
                // message is optional
                Message = message;
                MessageArray = message.Split(new char[] { ' ' });
            }
            Tags = tags;
        }
    }
}
