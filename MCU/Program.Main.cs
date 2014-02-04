using System;
using System.Collections.Generic;
using System.Text;
using MoltenMercury;
using MoltenMercury.Interop;
using System.IO;

/* ============================================================================
 * Program.Main.cs
 * ----------------------------------------------------------------------------
 * 2012 (C) Jieni Luchijinzhou a.k.a Aragorn Wyvernzora
 *   
 * Main entries of MCU are located here.
 * Main() is the default entry used when MCU is started as a separate process.
 * MC_Main() is the entry used by MoltenChara when this program is loaded
 *      via reflection.
 *      
 * THerefore this program has an MC Interop Mode which is triggered when MC_Main
 * is called. 
 * 
 */

namespace MCU
{
    partial class Program
    {
        enum MessageType
        {
            Routine,
            Error
        }

        //static CommandLineArguments cmdargs;
        static Boolean mcInteropMode = false;

        // Entry for MC
        public static int MC_Main(TextWriter stdout, TextWriter stderr, String args)
        {
            mcInteropMode = true;

            Console.SetOut(stdout);
            Console.SetError(stderr);

            CommandLineArguments cmdargs = new CommandLineArguments(args);
            return InnerMain(cmdargs);
        }

        // Entry for system
        static void Main()
        {
            mcInteropMode = false;
            Console.Title = "Project Molten Mercury - Developer Tools";
            Console.Clear();

            InnerMain(new CommandLineArguments());
        }

        // Actual inner that executes commands
        static int InnerMain(CommandLineArguments cmdargs)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(Resources.CopyrightNotice);
            Console.ForegroundColor = ConsoleColor.Gray;

            if (cmdargs.Count == 0)
            {
                Console.WriteLine(Resources.NoArgsMessage);
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                Console.Clear();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(Resources.CopyrightNotice);
                Console.ForegroundColor = ConsoleColor.Gray;

                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("MCU> ");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    CommandLineArguments args = new CommandLineArguments("MCU " + Console.ReadLine());
                    
                    if (args.Count == 0)
                        continue;

                    Int32 code = MCU_Run(args);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Command finished and returned code {0}\n", code);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
            else
            {
                return MCU_Run(cmdargs);
            }
        }

        static int MCU_Run(CommandLineArguments cmdargs)
        {
            if (cmdargs[0].Type == CommandLineArgument.ArgumentType.Option)
            {
                WriteMessage(MessageType.Error, "Error 901: Command cannot be a command line option\n");
                return 901;
            }
            try
            {
                String command = cmdargs[0].Name.ToUpper();
                switch (command)
                {
                    case "HELP":
                        return Help(cmdargs);
                    case "LEGACY":
                        return Legacy(cmdargs);
                    case "PACK":
                        return Pack(cmdargs);
                    case "CLEANUP":
                        return Cleanup(cmdargs);
                    case "VERIFY":
                        return Verify(cmdargs);
                    case "DEPLOY":
                        return Deploy(cmdargs);
                    default:
                        {
                            WriteMessage(MessageType.Error, "Error 902: Command not recognized!\n");
                            return 902;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("An exception of type {0} occured!", ex.GetType().Name);
                Console.Error.WriteLine("Exception Message: {0}", ex.Message);
                Console.Error.WriteLine("========== Stack Trace ==========\n{0}", ex.StackTrace);
                return 999;
            }
        }

        static void WriteMessage(MessageType type, String format, params object[] args)
        {
            System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace();
            String callerName = stack.GetFrame(1).GetMethod().Name.ToUpper();
            if (type == MessageType.Routine)
            {
                Console.Write("{0}> {1}", callerName, String.Format(format, args));
            }
            else
            {
                Console.Beep();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Error.Write("{0}> {1}", callerName, String.Format(format, args));
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
  
        static int Help(CommandLineArguments cmdargs)
        {
            Console.WriteLine(Resources.Help);
            return mcInteropMode ? 903 : 0;
        }
    }
}
