/*
 *[C#] Proxy Toolkit
 *Copyright (C) 2017  Juan Xuereb
 *
 *This program is free software: you can redistribute it and/or modify
 *it under the terms of the GNU General Public License as published by
 *the Free Software Foundation, either version 3 of the License, or
 *(at your option) any later version.
 *
 *This program is distributed in the hope that it will be useful,
 *but WITHOUT ANY WARRANTY; without even the implied warranty of
 *MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *GNU General Public License for more details.
 *You should have received a copy of the GNU General Public License
 *along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using xNet;
using CS_Proxy.Proxy;

namespace CS_Proxy.Lists
{
    /// <summary>
    /// Best Implement these classes once and statically.
    /// </summary>
    public class ProxyManager
    {
        private HashSet<string> Proxies = new HashSet<string>(); //added on initialization BY REFERENCE from other hashset
        private LinkedList<string> Unscanned = new LinkedList<string>(); //ideal for removing items, specifically first or last item (as is the case here), which only involves a change in pointer
        public List<MyProxy> Alive = new List<MyProxy>();
        public List<MyProxy> Dead = new List<MyProxy>();


        private static readonly Object proxiesLock = new Object();
        private static readonly Object addLock = new Object();

        public int Count { get { return Proxies.Count; } }

        public void Initialize(HashSet<string> proxies)
        {
            Alive.Clear();
            Dead.Clear();
            if (proxies!=null)
                Proxies = proxies; //ref
            if(Proxies.Count>0)
                Unscanned = new LinkedList<string>(Proxies);
        }

        public void Reset()
        {
            Unscanned = new LinkedList<string>(Proxies);
        }

        public bool Add(string proxy)
        {
            return Proxies.Add(proxy);
        }

        public void AddToAlive(MyProxy proxy)
        {
            lock (addLock) //IndexOutOfRangeException exception
            {
                Alive.Add(proxy);
            }
        }

        public void Clear()
        {
            Proxies.Clear();
            Unscanned.Clear();
            Alive.Clear();
            Dead.Clear();
        }

        public enum ProxyGeneralType { HTTP, SOCKS, SOCKS4, SOCKS5, ALL };
        public bool Output(ProxyGeneralType type, bool toClip, string fileLoc, bool elite, bool high, bool trans)
        {
            bool success = true;

            //Populate toWrite List
            List<string> toWrite = new List<string>();
            for (int i = 0; i < Alive.Count; ++i)
            {
                if (!Alive[i].isAlive)
                    continue; //not alive

                if (type == ProxyGeneralType.HTTP && Alive[i].Type != ProxyType.Http)
                    continue; //not HTTP
                if (type == ProxyGeneralType.SOCKS && (Alive[i].Type != ProxyType.Socks4 && Alive[i].Type != ProxyType.Socks4a && Alive[i].Type != ProxyType.Socks5))
                    continue; //not a SOCK
                if (type == ProxyGeneralType.SOCKS4 && (Alive[i].Type != ProxyType.Socks4 && Alive[i].Type != ProxyType.Socks4a))
                    continue; //not a SOCK4
                if (type == ProxyGeneralType.SOCKS5 && Alive[i].Type != ProxyType.Socks5)
                    continue; //not a SOCK5

                if (!trans && Alive[i].AnonLevel == Anonymity.Transparent)
                    continue;
                if (!high && Alive[i].AnonLevel == Anonymity.High)
                    continue;
                if (!elite && Alive[i].AnonLevel == Anonymity.Elite)
                    continue;

                toWrite.Add(Alive[i].ToString());
            }

            //Output
            if (toClip)
            {
                StringBuilder sb = new StringBuilder();
                try
                {
                    foreach (string proxy in toWrite)
                        sb.AppendLine(proxy);

                    System.Windows.Forms.Clipboard.SetText(sb.ToString());
                }
                catch (ArgumentOutOfRangeException) { success = false; }
            }

            if (fileLoc != string.Empty)
            {
                StreamWriter sw = new StreamWriter(fileLoc);
                try
                {
                    foreach (string proxy in toWrite)
                        sw.WriteLine(proxy);
                }
                catch (UnauthorizedAccessException) { success = false; }
                catch (ObjectDisposedException) { success = false; } //WriteLine
                catch (IOException) { success = false; } //WriteLine {DirectoryNotFound, PathTooLong too}
                catch (ArgumentOutOfRangeException) { success = false; } //AppendLine
                catch (ArgumentException) { success = false; }
                catch (System.Security.SecurityException) { success = false; }
                finally
                {
                    if (sw != null)
                        sw.Dispose();
                }
                System.Diagnostics.Process.Start(Path.GetDirectoryName(fileLoc)); //take me to the directory
            }

            return success;
        }


        public MyProxy RecommendProxy()
        {
            lock (proxiesLock)
            {
                if (Unscanned.Count == 0)
                    return null;

                MyProxy proxy = new MyProxy(Unscanned.First(), false);
                Unscanned.RemoveFirst();
                return proxy;
            }
        }
    }

    public class URLManager
    {
        private List<string> URLs = new List<string>(); //are added externally on import button via List<T>.Add();
        private LinkedList<string> Unscanned = new LinkedList<string>(); //ideal for removing items, specifically first or last item (as is the case here), which only involves a change in pointer
        public static readonly Object urlLock = new Object();

        public int Count { get { return URLs.Count; } }

        public void Reset()
        {
            Unscanned = new LinkedList<string>(URLs);
        }

        public bool Contains(string url)
        {
            return URLs.Contains(url);
        }

        public void Clear()
        {
            URLs.Clear();
            Unscanned.Clear();
        }

        public bool Add(string url)
        {
            if(URLs.Contains(url))
                return false;

            URLs.Add(url);
            return true;
        }

        public string RecommendURL()
        {
            lock (urlLock)
            {
                if (Unscanned.Count == 0)
                    return string.Empty;

                string url = string.Copy(Unscanned.First());
                Unscanned.RemoveFirst();
                return url;
            }
        }
    }
}
