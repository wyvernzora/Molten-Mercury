using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

/* ============================================================================
 * DebugHelper.cs
 * ----------------------------------------------------------------------------
 * 2012 (C) Jieni Luchijinzhou a.k.a Aragorn Wyvernzora
 *   
 * This is a helper class that handles debug logging.
 * Pretty much everything here is self explanatory.
 * 
 */

namespace MoltenMercury
{
    public static class DebugHelper
    {
        private static StreamWriter log = new StreamWriter(Path.Combine(Application.StartupPath, "debug.log"), false, Encoding.UTF8);
        private static Boolean m_logEnabled = true;

        public static Boolean LoggingEnabled
        {
            get { return m_logEnabled; }
            set { m_logEnabled = value; }
        }

        public static void DebugPrint(String format, params object[] args)
        {
            if (!m_logEnabled) return;


            System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace();

#if DEBUG
            System.Diagnostics.Debug.Print("{0,2}:{1,2}:{2,2}.{3,-10}{4,-30}{5}",
                DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond,
                stack.GetFrame(1).GetMethod().Name,
                String.Format(format, args));
#endif

            log.WriteLine("{0,2}:{1,2}:{2,2}.{3,-10}{4,-30}{5}",
                DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond,
                stack.GetFrame(1).GetMethod().Name,
                String.Format(format, args));
           log.Flush();
        }

        public static void ForceDebugPrint(String format, params object[] args)
        {
            System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace();

#if DEBUG
            System.Diagnostics.Debug.Print("{0,2}:{1,2}:{2,2}.{3,-10}{4,-30}{5}",
                DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond,
                stack.GetFrame(1).GetMethod().Name,
                String.Format(format, args));
#endif

            log.WriteLine("{0,2}:{1,2}:{2,2}.{3,-10}{4,-30}{5}",
                DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond,
                stack.GetFrame(1).GetMethod().Name,
                String.Format(format, args));
            log.Flush();
        }

    }
}
