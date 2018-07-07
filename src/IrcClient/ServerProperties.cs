/*
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2013 Ondřej Hošek <ondra.hosek@gmail.com>
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
    /// <summary>
    /// This class stores information about the capabilities and idiosyncrasies
    /// of an IRC server.
    /// See http://tools.ietf.org/html/draft-hardy-irc-isupport-00 for more
    /// information.
    /// </summary>
    public class ServerProperties
    {
        /// <summary>
        /// Contains the properties as returned by the server. If a property has
        /// been specified without a value, it is mapped to null.
        /// </summary>
        public Dictionary<string, string> RawProperties { get; internal set; }

        /// <summary>
        /// Stores how the server maps between uppercase and lowercase letters.
        /// (raw property <c>CASEMAPPING</c>)
        /// </summary>
        public CaseMappingType CaseMapping
        {
            get
            {
                if (!HaveNonNullKey("CASEMAPPING"))
                {
                    // default is rfc1459
                    return CaseMappingType.Rfc1459;
                }

                switch (RawProperties["CASEMAPPING"])
                {
                    case "ascii":
                        return CaseMappingType.Ascii;
                    case "rfc1459":
                        return CaseMappingType.Rfc1459;
                    case "strict-rfc1459":
                        return CaseMappingType.StrictRfc1459;
                    default:
                        return CaseMappingType.Unknown;
                }
            }
        }

        /// <summary>
        /// Stores how many channels of a given type a user can join.
        /// A return value of null means none were supplied or the
        /// value was invalid. The key is a string of channel types
        /// which count towards the same total; a value of -1 means
        /// an infinite amount.
        /// (raw property <c>CHANLIMIT</c>)
        /// </summary>
        public IDictionary<string, int> ChannelJoinLimits => ParseStringNumberPairs("CHANLIMIT", null, null, -1);

        /// <summary>
        /// Stores the channel modes which store lists. When a
        /// change is sent by the server, it will always contain a
        /// parameter; when sent by a client without a parameter,
        /// the server will reply with the current list. A return
        /// value of null means none or invalid ones were supplied.
        /// (raw property <c>CHANMODES</c>, first value)
        /// </summary>
        public string ListChannelModes => SplitChannelModes?[0];

        /// <summary>
        /// Stores the channel modes which store a parameter. This
        /// parameter must be provided both when adding and when
        /// removing the mode.
        /// (raw property <c>CHANMODES</c>, second value)
        /// </summary>
        public string ParametricChannelModes => SplitChannelModes?[1];

        /// <summary>
        /// Stores the channel modes which store a parameter. This
        /// parameter must only be provided when adding the value.
        /// (raw property <c>CHANMODES</c>, third value)
        /// </summary>
        public string SetParametricChannelModes => SplitChannelModes?[2];

        /// <summary>
        /// Stores the channel modes which don't store a parameter.
        /// (raw property <c>CHANMODES</c>, fourth value)
        /// </summary>
        public string ParameterlessChannelModes => SplitChannelModes?[3];

        /// <summary>
        /// Stores the maximum length of a channel name. -1 means no limit.
        /// (raw property <c>CHANNELLEN</c>)
        /// </summary>
        public int ChannelNameLength => ParseNumber("CHANNELLEN", 200, 200) ?? -1; // defaults as specified by RFC1459

        /// <summary>
        /// Stores the types of channels supported by the server.
        /// An empty string means no channels are supported (!).
        /// (raw property <c>CHANTYPES</c>)
        /// </summary>
        public char[] ChannelTypes
        {
            get
            {
                if (!HaveNonNullKey("CHANTYPES"))
                {
                    // sane default
                    return "#&".ToCharArray();
                }

                return RawProperties["CHANTYPES"].ToCharArray();
            }
        }

        /// <summary>
        /// Stores whether the server supports the CNOTICE command,
        /// which allows users with a specific channel privilege to
        /// send a notice to another participant in that channel
        /// without some of the restrictions that the sever may have
        /// placed on NOTICE.
        /// (raw property <c>CNOTICE</c>)
        /// </summary>
        public bool SupportsChannelParticipantNotices => RawProperties.ContainsKey("CNOTICE");

        /// <summary>
        /// Stores whether the server supports the CPRIVMSG command,
        /// which allows users with a specific channel privilege to
        /// send a message to another participant in that channel
        /// without some of the restrictions that the sever may have
        /// placed on PRIVMSG.
        /// (raw property <c>CPRIVMSG</c>)
        /// </summary>
        public bool SupportsChannelParticipantPrivMsgs => RawProperties.ContainsKey("CPRIVMSG");

        /// <summary>
        /// Stores available extensions to the LIST command.
        /// (raw property <c>ELIST</c>)
        /// </summary>
        public ListExtensions ListExtensions
        {
            get
            {
                if (!HaveNonNullKey("ELIST"))
                {
                    return ListExtensions.None;
                }

                string eliststr = RawProperties["ELIST"];
                var exts = ListExtensions.None;
                foreach (char e in eliststr.ToUpperInvariant())
                {
                    switch (e)
                    {
                        case 'C':
                            exts |= ListExtensions.CreationTime;
                            break;
                        case 'M':
                            exts |= ListExtensions.ContainsParticipantWithMask;
                            break;
                        case 'N':
                            exts |= ListExtensions.DoesNotContainParticipantWithMask;
                            break;
                        case 'T':
                            exts |= ListExtensions.TopicAge;
                            break;
                        case 'U':
                            exts |= ListExtensions.ParticipantCount;
                            break;
                    }
                }

                return exts;
            }
        }

        /// <summary>
        /// Returns what channel mode character is used by the
        /// server to signify ban exceptions. null means the server
        /// does not support ban exceptions.
        /// (raw property <c>EXCEPTS</c>)
        /// </summary>
        public char? BanExceptionCharacter
        {
            get
            {
                if (!RawProperties.ContainsKey("EXCEPTS"))
                {
                    return null;
                }

                string exstr = RawProperties["EXCEPTS"];
                if (exstr == null)
                {
                    // default: +e
                    return 'e';
                }
                else if (exstr.Length != 1)
                {
                    // invalid; assume lack of support
                    return null;
                }
                return exstr[0];
            }
        }

        /// <summary>
        /// Returns what channel mode character is used by the
        /// server to signify invite exceptions. null means the server
        /// does not support ban exceptions.
        /// (raw property <c>INVEX</c>)
        /// </summary>
        public char? InviteExceptionCharacter
        {
            get
            {
                if (!RawProperties.ContainsKey("INVEX"))
                {
                    return null;
                }

                string exstr = RawProperties["INVEX"];
                if (exstr == null)
                {
                    // default: +I
                    return 'I';
                }
                else if (exstr.Length != 1)
                {
                    // invalid; assume lack of support
                    return null;
                }
                return exstr[0];
            }
        }

        /// <summary>
        /// Returns how long a kick message supplied by the client
        /// may be.
        /// (raw property <c>KICKLEN</c>)
        /// </summary>
        public int? KickMessageLength => ParseNumber("KICKLEN", null, null);

        /// <summary>
        /// Stores how many list channel modes (see ListChannelModes)
        /// of a given type a user can set on a channel. (Note that
        /// the server may always return more.) A return value of null
        /// means none were supplied or the value was invalid. The key
        /// is a string of list mode characters which count towards the
        /// same total; a value of -1 means an infinite amount.
        /// (raw property <c>MAXLIST</c>)
        /// </summary>
        public IDictionary<string, int> ListModeLimits => ParseStringNumberPairs("MAXLIST", null, null, -1);

        /// <summary>
        /// Stores how many non-parameterless (list, parametric or
        /// set-parametric) modes can be set using a single MODE call.
        /// A return value of null means an invalid value has been
        /// supplied; a return value of -1 means a theoretically
        /// unlimited number of simultaneous mode sets.
        /// (raw property <c>MODES</c>)
        /// </summary>
        public int? MaxParametricModeSets =>
                // 3 if not set, infinity if value-less
                ParseNumber("MODES", 3, -1);

        /// <summary>
        /// Stores the display name of the network the IRC
        /// server is participating in. A return value of null
        /// means the server is not participating in an IRC network.
        /// (raw property <c>NETWORK</c>)
        /// </summary>
        public string NetworkName => !HaveNonNullKey("NETWORK") ? null : RawProperties["NETWORK"];

        /// <summary>
        /// Stores the maximum length of the nickname the client
        /// may set. (This has no bearing on the nicknames of
        /// other clients.) A return value of null means no or an
        /// invalid value was specified.
        /// (raw property <c>NICKLEN</c>)
        /// </summary>
        public int? MaxNicknameLength => ParseNumber("NICKLEN", 9, null); // RFC1459 default if unset

        /// <summary>
        /// Stores the channel privilege modes (e.g. o for op, v for
        /// voice) and their corresponding prefixes (e.g. @, +),
        /// ordered from most to least powerful. A return value of
        /// null means no or an invalid value was specified.
        /// (raw property <c>PREFIX</c>)
        /// </summary>
        public IList<KeyValuePair<char, char>> ChannelPrivilegeModesPrefixes
        {
            get
            {
                var modesList = new List<KeyValuePair<char, char>>();

                if (!RawProperties.ContainsKey("PREFIX"))
                {
                    // assume voice and ops
                    modesList.Add(new KeyValuePair<char, char>('o', '@'));
                    modesList.Add(new KeyValuePair<char, char>('v', '+'));
                    return modesList;
                }
                string prefixstr = RawProperties["PREFIX"];
                if (prefixstr == null)
                {
                    // supports no modes (!)
                    return modesList;
                }

                // format: (modes)prefixes
                if (prefixstr[0] != '(')
                {
                    return null;
                }

                string[] modesPrefixes = prefixstr.Substring(1).Split(')');
                if (modesPrefixes.Length != 2)
                {
                    // assuming the pathological case of a ')' mode
                    // character is impossible, this is invalid
                    return null;
                }
                string modes = modesPrefixes[0];
                string prefixes = modesPrefixes[1];
                if (modes.Length != prefixes.Length)
                {
                    return null;
                }
                for (int i = 0; i < modes.Length; ++i)
                {
                    modesList.Add(new KeyValuePair<char, char>(modes[i], prefixes[i]));
                }

                return modesList;
            }
        }

        /// <summary>
        /// Stores whether using the LIST command is safe, i.e. whether
        /// the user won't be disconnected because of the large amount
        /// of traffic generated by LIST.
        /// (raw property <c>SAFELIST</c>)
        /// </summary>
        public bool ListIsSafe => RawProperties.ContainsKey("SAFELIST");

        /// <summary>
        /// Stores the maximum number of entries on a user's silence
        /// list. A value of 0 means silence lists are not supported
        /// on this server.
        /// (raw property <c>SILENCE</c>)
        /// </summary>
        public int MaxSilenceListEntries => ParseNumber("SILENCE", 0, 0) ?? 0; // SILENCE requires a value, but assume 0 if unspecified

        /// <summary>
        /// If this property is not set to an empty string, users may
        /// send NOTICEs to channel participants of a given status;
        /// e.g. <c>NOTICE @#help :I found a bug.</c> would send the
        /// message to the operators of #help. The property stores the
        /// modes that may be the recipients of such messages, e.g.
        /// "~&@" for "owners, admins and operators only".
        /// (raw property <c>STATUSMSG</c>)
        /// </summary>
        public string StatusNoticeParticipants
        {
            get
            {
                if (!HaveNonNullKey("STATUSMSG"))
                {
                    // STATUSMSG requires a value, but assume none
                    // if unspecified
                    return "";
                }
                return RawProperties["STATUSMSG"];
            }
        }

        /// <summary>
        /// Maps the commands which support multiple targets to the
        /// maximum number of targets each of them supports. A return
        /// value of null means the server specified an invalid value.
        /// An entry value of -1 means infinity.
        /// (raw property <c>TARGMAX</c>)
        /// </summary>
        public IDictionary<string, int> MaxCommandTargets => ParseStringNumberPairs("TARGMAX", new Dictionary<string, int>(), null, -1);

        /// <summary>
        /// Stores the maximum topic length that the client may set
        /// on a channel on the server. A length of -1 means an
        /// infinite length.
        /// (raw property <c>TOPICLEN</c>)
        /// </summary>
        public int MaxTopicLength => ParseNumber("TOPICLEN", -1, -1) ?? -1; // SILENCE requires a value, but assume infinity if unspecified or invalid

        /// <summary>
        /// Stores the maximum number of entries on a user's watch
        /// list. A value of 0 means watch lists are not supported
        /// on this server.
        /// (raw property <c>WATCH</c>)
        /// </summary>
        public int MaxWatchListEntries => ParseNumber("WATCH", 0, 0) ?? 0; // SILENCE requires a value, but assume 0 if unspecified

        /// <summary>
        /// Constructs an empty server properties object.
        /// </summary>
        internal ServerProperties() => RawProperties = new Dictionary<string, string>();

        internal void ParseFromRawMessage(string[] rawMessage)
        {
            // split the message (0 = server, 1 = code, 2 = my nick)
            for (int i = 3; i < rawMessage.Length; ++i)
            {
                string msg = rawMessage[i];
                if (msg.StartsWith(":"))
                {
                    // addendum; we're done
                    break;
                }

                string[] keyval = msg.Split('=');
                if (keyval.Length == 1)
                {
                    // keyword only
                    RawProperties[keyval[0]] = null;
                }
                else if (keyval.Length == 2)
                {
                    // key and value
                    RawProperties[keyval[0]] = keyval[1];
                }
                else
                {
#if LOG4NET
                    Logger.Connection.Warn("confusing ISUPPORT message, ignoring: " + msg);
#endif
                }
            }
        }

        /// <summary>
        /// Returns whether the property dictionary contains the given key and
        /// it is not null.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>Whether the given key maps to a non-null value in the
        /// dictionary.</returns>
        private bool HaveNonNullKey(string key) => RawProperties.TryGetValue(key, out string value) && value != null;

        /// <summary>
        /// Returns a dictionary from parsing a value in the format
        /// string:number[,string:number,...]. If the value is unset (i.e. not
        /// contained in the dictionary), returns unsetDefault. If the value is
        /// empty (i.e. maps to null), returns emptyDefault.
        /// defaultValue is used if no number is specified after a colon; if
        /// defaultValue is null, this method returns null.
        /// </summary>
        private IDictionary<string, int> ParseStringNumberPairs(string key, IDictionary<string, int> unsetDefault, IDictionary<string, int> emptyDefault, int? defaultValue)
        {
            if (!RawProperties.ContainsKey(key))
            {
                return unsetDefault;
            }

            string valstr = RawProperties[key];
            if (valstr == null)
            {
                return emptyDefault;
            }

            var valmap = new Dictionary<string, int>();
            // comma splits the specs
            foreach (string limit in valstr.Split(','))
            {
                // colon splits keys and value
                string[] split = limit.Split(':');
                if (split.Length != 2)
                {
                    // invalid spec; don't trust the whole thing
                    return null;
                }
                string chantypes = split[0];
                string valuestr = split[1];
                int value;
                if (valuestr == "")
                {
                    if (defaultValue.HasValue)
                    {
                        value = defaultValue.Value;
                    }
                    return null;
                }
                else if (!Int32.TryParse(valuestr, out value))
                {
                    // invalid integer; don't trust the whole thing
                    return null;
                }

                valmap[chantypes] = value;
            }

            return valmap;
        }

        /// <summary>
        /// Returns a numeric value. If the value is unset (i.e. not contained
        /// in the dictionary), returns unsetDefault. If the value is empty
        /// (i.e. maps to null), returns emptyDefault. On parse failure, returns
        /// null. Otherwise, returns the parsed value.
        /// </summary>
        private int? ParseNumber(string key, int? unsetDefault, int? emptyDefault)
        {
            if (!RawProperties.ContainsKey(key))
            {
                return unsetDefault;
            }
            string numstr = RawProperties[key];
            if (numstr == null)
            {
                return emptyDefault;
            }
            return !Int32.TryParse(numstr, out int num) ? null : (int?)num;
        }

        /// <summary>
        /// Returns the array value of the CHANMODES property, or null if
        /// it was invalid.
        /// </summary>
        private string[] SplitChannelModes
        {
            get
            {
                if (!HaveNonNullKey("CHANMODES"))
                {
                    return null;
                }
                string[] splits = RawProperties["CHANMODES"].Split(',');
                return splits.Length != 4 ? null : splits;
            }
        }
    }

    /// <summary>
    /// Represents how lowercase and uppercase are mapped by the server. This
    /// information is mostly supplied in the CASEMAPPING server property.
    /// </summary>
    public enum CaseMappingType
    {
        /// <summary>
        /// The server provided no or an unknown value.
        /// </summary>
        Unknown,

        /// <summary>
        /// The ASCII characters 0x61 to 0x7a (<c>a</c> to <c>z</c>) are defined
        /// as the lowercase variants of 0x41 to 0x5a (<c>A</c> to <c>Z</c>).
        /// The server provided the string <c>ascii</c>.
        /// </summary>
        Ascii,

        /// <summary>
        /// The ASCII characters 0x61 to 0x7e (<c>a</c> to <c>~</c>) are defined
        /// as the lowercase variants of 0x41 to 0x5e (<c>A</c> to <c>^</c>).
        /// The server provided the string <c>rfc1459</c>.
        /// </summary>
        Rfc1459,

        /// <summary>
        /// The ASCII characters 0x61 to 0x7d (<c>a</c> to <c>}</c>) are defined
        /// as the lowercase variants of 0x41 to 0x5d (<c>A</c> to <c>]</c>).
        /// The server provided the string <c>strict-rfc1459</c>.
        /// </summary>
        StrictRfc1459,
    }

    /// <summary>
    /// Represents additional functionality available in the LIST command.
    /// </summary>
    [Flags]
    public enum ListExtensions
    {
        /// <summary>
        /// No additional functionality is supported by LIST.
        /// </summary>
        None = 0,

        /// <summary>
        /// Channel lists may be requested by creation time, using the syntax
        /// <c>C&gt;time</c> to search for channels created after the given time
        /// and <c>C&lt;time</c> to search for channels created before the given
        /// time.
        /// (letter: <c>C</c>)
        /// </summary>
        CreationTime = (1 << 0),

        /// <summary>
        /// Channel lists may be requested by a mask, matching channels in which
        /// a user matching the given mask is participating.
        /// (letter: <c>M</c>)
        /// </summary>
        ContainsParticipantWithMask = (1 << 1),

        /// <summary>
        /// Channel lists may be requested by a mask, matching channels in which
        /// a user matching the given mask is not participating.
        /// (letter: <c>N</c>)
        /// </summary>
        DoesNotContainParticipantWithMask = (1 << 2),

        /// <summary>
        /// Channel lists may be requested by topic age, using the syntax
        /// <c>T&gt;time</c> to search for channels with topics last changed after
        /// the given time and <c>T&lt;time</c> to search for channels with topics
        /// last changed before the given time.
        /// (letter: <c>T</c>)
        /// </summary>
        TopicAge = (1 << 3),

        /// <summary>
        /// Channel lists may be requested by number of participants, using the
        /// syntax <c>U&gt;count</c> to search for channels with more than the given
        /// number of participants and <c>C&lt;time</c> to search for channels with
        /// fewer than the given number of participants.
        /// (letter: <c>U</c>)
        /// </summary>
        ParticipantCount = (1 << 4)
    }
}
