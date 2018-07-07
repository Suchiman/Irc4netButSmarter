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
using System.Text;
using System.Text.RegularExpressions;

namespace Meebey.SmartIrc4net
{
    public static class Rfc2812
    {
        // nickname   =  ( letter / special ) *8( letter / digit / special / "-" )
        // letter     =  %x41-5A / %x61-7A       ; A-Z / a-z
        // digit      =  %x30-39                 ; 0-9
        // special    =  %x5B-60 / %x7B-7D
        //                  ; "[", "]", "\", "`", "_", "^", "{", "|", "}"
        private static Regex _NicknameRegex = new Regex(@"^[A-Za-z\[\]\\`_^{|}][A-Za-z0-9\[\]\\`_\-^{|}]+$", RegexOptions.Compiled);

        /// <summary>
        /// Checks if the passed nickname is valid according to the RFC
        ///
        /// Use with caution, many IRC servers are not conform with this!
        /// </summary>
        public static bool IsValidNickname(string nickname) => !String.IsNullOrEmpty(nickname) && _NicknameRegex.Match(nickname).Success;

        public static string Pass(string password) => "PASS " + password;

        public static string Nick(string nickname) => "NICK " + nickname;

        public static string User(string username, int usermode, string realname) => "USER " + username + " " + usermode.ToString() + " * :" + realname;

        public static string Oper(string name, string password) => "OPER " + name + " " + password;

        public static string Privmsg(string destination, string message) => "PRIVMSG " + destination + " :" + message;

        public static string Notice(string destination, string message) => "NOTICE " + destination + " :" + message;

        public static string Join(string channel) => "JOIN " + channel;

        public static string Join(string[] channels) => "JOIN " + String.Join(",", channels);

        public static string Join(string channel, string key) => "JOIN " + channel + " " + key;

        public static string Join(string[] channels, string[] keys) => "JOIN " + String.Join(",", channels) + " " + String.Join(",", keys);

        public static string Part(string channel) => "PART " + channel;

        public static string Part(string[] channels) => "PART " + String.Join(",", channels);

        public static string Part(string channel, string partmessage) => "PART " + channel + " :" + partmessage;

        public static string Part(string[] channels, string partmessage) => "PART " + String.Join(",", channels) + " :" + partmessage;

        public static string Kick(string channel, string nickname) => "KICK " + channel + " " + nickname;

        public static string Kick(string channel, string nickname, string comment) => "KICK " + channel + " " + nickname + " :" + comment;

        public static string Kick(string[] channels, string nickname) => "KICK " + String.Join(",", channels) + " " + nickname;

        public static string Kick(string[] channels, string nickname, string comment) => "KICK " + String.Join(",", channels) + " " + nickname + " :" + comment;

        public static string Kick(string channel, string[] nicknames) => "KICK " + channel + " " + String.Join(",", nicknames);

        public static string Kick(string channel, string[] nicknames, string comment) => "KICK " + channel + " " + String.Join(",", nicknames) + " :" + comment;

        public static string Kick(string[] channels, string[] nicknames) => "KICK " + String.Join(",", channels) + " " + String.Join(",", nicknames);

        public static string Kick(string[] channels, string[] nicknames, string comment) => "KICK " + String.Join(",", channels) + " " + String.Join(",", nicknames) + " :" + comment;

        public static string Motd() => "MOTD";

        public static string Motd(string target) => "MOTD " + target;

        public static string Lusers() => "LUSERS";

        public static string Lusers(string mask) => "LUSER " + mask;

        public static string Lusers(string mask, string target) => "LUSER " + mask + " " + target;

        public static string Version() => "VERSION";

        public static string Version(string target) => "VERSION " + target;

        public static string Stats() => "STATS";

        public static string Stats(string query) => "STATS " + query;

        public static string Stats(string query, string target) => "STATS " + query + " " + target;

        public static string Links() => "LINKS";

        public static string Links(string servermask) => "LINKS " + servermask;

        public static string Links(string remoteserver, string servermask) => "LINKS " + remoteserver + " " + servermask;

        public static string Time() => "TIME";

        public static string Time(string target) => "TIME " + target;

        public static string Connect(string targetserver, string port) => "CONNECT " + targetserver + " " + port;

        public static string Connect(string targetserver, string port, string remoteserver) => "CONNECT " + targetserver + " " + port + " " + remoteserver;

        public static string Trace() => "TRACE";

        public static string Trace(string target) => "TRACE " + target;

        public static string Admin() => "ADMIN";

        public static string Admin(string target) => "ADMIN " + target;

        public static string Info() => "INFO";

        public static string Info(string target) => "INFO " + target;

        public static string Servlist() => "SERVLIST";

        public static string Servlist(string mask) => "SERVLIST " + mask;

        public static string Servlist(string mask, string type) => "SERVLIST " + mask + " " + type;

        public static string Squery(string servicename, string servicetext) => "SQUERY " + servicename + " :" + servicetext;

        public static string List() => "LIST";

        public static string List(string channel) => "LIST " + channel;

        public static string List(string[] channels) => "LIST " + String.Join(",", channels);

        public static string List(string channel, string target) => "LIST " + channel + " " + target;

        public static string List(string[] channels, string target) => "LIST " + String.Join(",", channels) + " " + target;

        public static string Names() => "NAMES";

        public static string Names(string channel) => "NAMES " + channel;

        public static string Names(string[] channels) => "NAMES " + String.Join(",", channels);

        public static string Names(string channel, string target) => "NAMES " + channel + " " + target;

        public static string Names(string[] channels, string target) => "NAMES " + String.Join(",", channels) + " " + target;

        public static string Topic(string channel) => "TOPIC " + channel;

        public static string Topic(string channel, string newtopic) => "TOPIC " + channel + " :" + newtopic;

        public static string Mode(string target) => "MODE " + target;

        public static string Mode(string target, string newmode) => "MODE " + target + " " + newmode;

        public static string Mode(string target, string[] newModes, string[] newModeParameters)
        {
            if (newModes == null)
            {
                throw new ArgumentNullException(nameof(newModes));
            }
            if (newModeParameters == null)
            {
                throw new ArgumentNullException(nameof(newModeParameters));
            }
            if (newModes.Length != newModeParameters.Length)
            {
                throw new ArgumentException("newModes and newModeParameters must have the same size.");
            }

            var newMode = new StringBuilder(newModes.Length);
            var newModeParameter = new StringBuilder();
            // as per RFC 3.2.3, maximum is 3 modes changes at once
            int maxModeChanges = 3;
            if (newModes.Length > maxModeChanges)
            {
                throw new ArgumentOutOfRangeException(
                    "newModes.Length",
                    newModes.Length,
                    String.Format("Mode change list is too large (> {0}).", maxModeChanges)
                );
            }

            for (int i = 0; i <= newModes.Length; i += maxModeChanges)
            {
                for (int j = 0; j < maxModeChanges; j++)
                {
                    if (i + j >= newModes.Length)
                    {
                        break;
                    }
                    newMode.Append(newModes[i + j]);
                }

                for (int j = 0; j < maxModeChanges; j++)
                {
                    if (i + j >= newModeParameters.Length)
                    {
                        break;
                    }
                    newModeParameter.Append(newModeParameters[i + j]);
                    newModeParameter.Append(" ");
                }
            }
            if (newModeParameter.Length > 0)
            {
                // remove trailing space
                newModeParameter.Length--;
                newMode.Append(" ");
                newMode.Append(newModeParameter.ToString());
            }

            return Mode(target, newMode.ToString());
        }

        public static string Service(string nickname, string distribution, string info) => "SERVICE " + nickname + " * " + distribution + " * * :" + info;

        public static string Invite(string nickname, string channel) => "INVITE " + nickname + " " + channel;

        public static string Who() => "WHO";

        public static string Who(string mask) => "WHO " + mask;

        public static string Who(string mask, bool ircop) => ircop ? "WHO " + mask + " o" : "WHO " + mask;

        public static string Whois(string mask) => "WHOIS " + mask;

        public static string Whois(string[] masks) => "WHOIS " + String.Join(",", masks);

        public static string Whois(string target, string mask) => "WHOIS " + target + " " + mask;

        public static string Whois(string target, string[] masks) => "WHOIS " + target + " " + String.Join(",", masks);

        public static string Whowas(string nickname) => "WHOWAS " + nickname;

        public static string Whowas(string[] nicknames) => "WHOWAS " + String.Join(",", nicknames);

        public static string Whowas(string nickname, string count) => "WHOWAS " + nickname + " " + count + " ";

        public static string Whowas(string[] nicknames, string count) => "WHOWAS " + String.Join(",", nicknames) + " " + count + " ";

        public static string Whowas(string nickname, string count, string target) => "WHOWAS " + nickname + " " + count + " " + target;

        public static string Whowas(string[] nicknames, string count, string target) => "WHOWAS " + String.Join(",", nicknames) + " " + count + " " + target;

        public static string Kill(string nickname, string comment) => "KILL " + nickname + " :" + comment;

        public static string Ping(string server) => "PING " + server;

        public static string Ping(string server, string server2) => "PING " + server + " " + server2;

        public static string Pong(string server) => "PONG " + server;

        public static string Pong(string server, string server2) => "PONG " + server + " " + server2;

        public static string Error(string errormessage) => "ERROR :" + errormessage;

        public static string Away() => "AWAY";

        public static string Away(string awaytext) => "AWAY :" + awaytext;

        public static string Rehash() => "REHASH";

        public static string Die() => "DIE";

        public static string Restart() => "RESTART";

        public static string Summon(string user) => "SUMMON " + user;

        public static string Summon(string user, string target) => "SUMMON " + user + " " + target;

        public static string Summon(string user, string target, string channel) => "SUMMON " + user + " " + target + " " + channel;

        public static string Users() => "USERS";

        public static string Users(string target) => "USERS " + target;

        public static string Wallops(string wallopstext) => "WALLOPS :" + wallopstext;

        public static string Userhost(string nickname) => "USERHOST " + nickname;

        public static string Userhost(string[] nicknames) => "USERHOST " + String.Join(" ", nicknames);

        public static string Ison(string nickname) => "ISON " + nickname;

        public static string Ison(string[] nicknames) => "ISON " + String.Join(" ", nicknames);

        public static string Quit() => "QUIT";

        public static string Quit(string quitmessage) => "QUIT :" + quitmessage;

        public static string Squit(string server, string comment) => "SQUIT " + server + " :" + comment;
    }
}
