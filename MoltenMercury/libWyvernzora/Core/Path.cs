/*===================================================
 * LibHex2\\Core\\Path.cs
 * -------------------------------------------------------------------------------
 * Copyright (C) Aragorn Wyvernzora 2011
 *      MODIFIED FROM THE Firefly.Core.FilenameHandling CLASS
 *      ORIGINALLY WRITTEN BY F.R.C. IN 2010
 * -------------------------------------------------------------------------------
 *  This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * ===================================================
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace libWyvernzora
{
    public static class Path
    {
        public static String GetFileName(String path)
        {
            if (path == "") return "";
            Int32 names = 0;
            Int32 names2 = path.Replace("/", "\\").IndexOf('\\', names);

            while (names2 != -1)
            {
                names = names2 + 1;
                names2 = path.Replace("/", "\\").IndexOf('\\', names);
            }

            return path.Substring(names);
        }

        public static String GetMainFileName(String path)
        {
            if (path == "") return "";

            int names = 0;
            int names2 = path.Replace("/", "\\").IndexOf('\\', names);

            while (names2 != -1)
            {
                names = names2 + 1;
                names2 = path.Replace("/", "\\").IndexOf('\\', names);
            }

            int name = path.Length - 1;
            int name2 = path.LastIndexOf('.', name);

            if (name2 != -1)
                name = name2 - 1;

            return path.Substring(names, name - names + 1);
        }

        public static String GetExtension(String path)
        {
            if (path == "") return "";
            if (!path.Contains(".")) return "";
            return path.Substring(path.LastIndexOf(".") + 1);
        }

        public static String GetFileDirectory(String path)
        {
            if (path == "") return "";
            int name = 0;
            int name2 = 0;
            while (name2 != -1)
            {
                name = name2 + 1;
                name2 = path.Replace("/", "\\").IndexOf('\\', name);
            }
            return path.Substring(0, name - 1);
        }

        public static String GetRelativePath(String path, String basepath)
        {
            if (path == "" || basepath == "") return "";

            String a = path.TrimEnd('/', '\\');
            String b = basepath.TrimEnd('/', '\\');
            String c = PopFirstDir(ref a);
            String d = PopFirstDir(ref b);

            if (c != d) return path;
            while (c == d)
            {
                if (c == "") return ".";
                c = PopFirstDir(ref a);
                d = PopFirstDir(ref b);
            }

            a = (c + "\\" + a).TrimEnd('/', '\\');
            b = (d + "\\" + b).TrimEnd('/', '\\');

            while (PopFirstDir(ref b) != "")
            {
                a = "..\\" + a;
            }
            return a.Replace('\\', System.IO.Path.DirectorySeparatorChar);
        }

        public static String GetReducedPath(String path)
        {
            Stack<String> l = new Stack<string>();
            if (path != "")
            {
                foreach (String d in Regex.Split(path, "\\|/"))
                {
                    if (d == ".") continue;
                    if (d == "..")
                    {
                        if (l.Count > 0)
                        {
                            String p = l.Pop();
                            if (p == "..")
                            {
                                l.Push(p);
                                l.Push(d);
                            }
                        }
                        else
                        {
                            l.Push(d);
                        }
                        continue;
                    }
                    if (d.Contains(":")) l.Clear();
                    l.Push(d);
                }
            }
            String[] arr = l.ToArray();
            Array.Reverse(arr);
            return String.Join(System.IO.Path.DirectorySeparatorChar.ToString(), arr);
        }

        public static String GetDirectoryNameWoutTailingSeparator(String path)
        {
            if (path == "") return "";
            return path.TrimEnd('/', '\\');
        }

        public static String GetDirectoryNameWithTailingSeparator(String path)
        {
            String d = GetDirectoryNameWoutTailingSeparator(path);
            if (d == "") return "";
            return d + System.IO.Path.DirectorySeparatorChar.ToString();
        }

        public static String GetAbsolutePath(String path, String basepath)
        {
            basepath = GetDirectoryNameWoutTailingSeparator(basepath);
            if (path != "") path = path.TrimStart('/', '\\');
            Stack<String> s = new Stack<string>();
            if (basepath != "")
            {
                foreach (String d in Regex.Split(basepath, "\\|/"))
                {
                    if (d == ".") continue;
                    if (d == "..")
                    {
                        if (s.Count > 0)
                        {
                            String p = s.Pop();
                            if (p == "..")
                            {
                                s.Push(p);
                                s.Push(d);
                            }
                        }
                        else
                        {
                            s.Push(d);
                        }
                        continue;
                    }
                    if (d.Contains(":")) s.Clear();
                    s.Push(d);
                }
            }
            if (path != "")
            {
                foreach (String d in Regex.Split(path, "\\|/"))
                {
                    if (d == ".") continue;
                    if (d == "..")
                    {
                        if (s.Count > 0)
                        {
                            String p = s.Pop();
                            if (p == "..")
                            {
                                s.Push(p);
                                s.Push(d);
                            }
                        }
                        else
                        {
                            s.Push(d);
                        }
                        continue;
                    }
                    if (d.Contains(":")) s.Clear();
                    s.Push(d);
                }
            }
            String[] arr = s.ToArray();
            Array.Reverse(arr);
            return String.Join(System.IO.Path.DirectorySeparatorChar.ToString(), arr);
        }

        public static String PopFirstDir(ref String path)
        {
            String ret;
            if (path == "") return "";
            int names = path.Replace("/", "\\").IndexOf('\\', 0);
            if (names < 0)
            {
                ret = path;
                path = "";
                return ret;
            }
            else
            {
                ret = path.Substring(0, names);
                path = path.Substring(names + 1);
                return ret;
            }
        }

        public static String GetPath(string dir, string file)
        {
            if (dir == "") return file;
            dir = dir.TrimEnd('/', '\\');
            return (dir + "\\" + file).Replace("\\", System.IO.Path.DirectorySeparatorChar.ToString());
        }

        public static String ChangeExtension(String path, String ext)
        {
            return System.IO.Path.ChangeExtension(path, ext);
        }
    }
}
