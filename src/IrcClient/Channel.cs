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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Meebey.SmartIrc4net
{
    public class Channel
    {
        private DateTime _ActiveSyncStop;

        internal Channel(string name)
        {
            Name = name;
            ActiveSyncStart = DateTime.Now;
        }

#if LOG4NET
        ~Channel()
        {
            Logger.ChannelSyncing.Debug("Channel ("+Name+") destroyed");
        }
#endif

        public string Name { get; }
        public string Key { get; set; } = "";
        public Dictionary<string, ChannelUser> Users => UnsafeUsers.ToDictionary(item => item.Key, item => item.Value);
        internal ConcurrentDictionary<string, ChannelUser> UnsafeUsers { get; } = new ConcurrentDictionary<string, ChannelUser>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, ChannelUser> Ops => UnsafeOps.ToDictionary(item => item.Key, item => item.Value);
        internal ConcurrentDictionary<string, ChannelUser> UnsafeOps { get; } = new ConcurrentDictionary<string, ChannelUser>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, ChannelUser> Voices => UnsafeVoices.ToDictionary(item => item.Key, item => item.Value);
        internal ConcurrentDictionary<string, ChannelUser> UnsafeVoices { get; } = new ConcurrentDictionary<string, ChannelUser>(StringComparer.OrdinalIgnoreCase);
        public List<string> Bans { get; } = new List<string>();
        public List<string> BanExceptions { get; } = new List<string>();
        public List<string> InviteExceptions { get; } = new List<string>();
        public string Topic { get; set; } = "";
        public int UserLimit { get; set; }
        public string Mode { get; set; } = "";
        public DateTime ActiveSyncStart { get; }
        public DateTime ActiveSyncStop
        {
            get => _ActiveSyncStop;
            set
            {
                _ActiveSyncStop = value;
                ActiveSyncTime = _ActiveSyncStop.Subtract(ActiveSyncStart);
            }
        }

        public TimeSpan ActiveSyncTime { get; private set; }
        public bool IsSycned { get; set; }
    }
}
