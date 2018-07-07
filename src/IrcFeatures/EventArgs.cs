/*
 *
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2008-2009 Thomas Bruderer <apophis@apophis.ch> <http://www.apophis.ch>
 * Copyright (c) 2015 Katy Coe <djkaty@start.no> <http://www.djkaty.com>
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

namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// Base DCC Event Arguments
    /// </summary>
    public class DccEventArgs : EventArgs
    {
        public DccConnection Dcc { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dccClient"></param>
        /// <param name="stream">If there are multiple streams on a DCC (a channel DCC) this identifies the stream</param>
        internal DccEventArgs(DccConnection dcc) => Dcc = dcc;
    }

    /// <summary>
    /// Dcc Event Args Involving Lines of Text
    /// </summary>
    public class DccChatEventArgs : DccEventArgs
    {
        public string Message { get; }
        public string[] MessageArray { get; }

        internal DccChatEventArgs(DccConnection dcc, string messageLine) : base(dcc)
        {
            char[] whiteSpace = { ' ' };
            Message = messageLine;
            MessageArray = messageLine.Split(new char[] { ' ' });
        }
    }

    /// <summary>
    /// Dcc Event Args involving Packets of Bytes
    /// </summary>
    public class DccSendEventArgs : DccEventArgs
    {
        public byte[] Package { get; }
        public int PackageSize { get; }

        internal DccSendEventArgs(DccConnection dcc, byte[] package, int packageSize) : base(dcc)
        {
            Package = package;
            PackageSize = packageSize;
        }
    }

    /// <summary>
    /// Special DCC Event Arg for Receiving File Requests
    /// </summary>
    public class DccSendRequestEventArgs : DccEventArgs
    {
        public string Filename { get; }
        public long Filesize { get; }

        internal DccSendRequestEventArgs(DccConnection dcc, string filename, long filesize) : base(dcc)
        {
            Filename = filename;
            Filesize = filesize;
        }
    }
}
