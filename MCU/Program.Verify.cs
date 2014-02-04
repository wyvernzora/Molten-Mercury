using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoltenMercury;
using MoltenMercury.DataModel;
using System.IO;
using MoltenMercury.Interop;
using System.Xml;

namespace MCU
{
    partial class Program
    {
        static int Verify(CommandLineArguments cmdargs)
        {
            // Handle no-arg case
            if (cmdargs.Count == 1)
            {
                WriteMessage(MessageType.Error, "Error 904: No arguments detected\n");
                return 904;
            }

            // Handle help request
            if (cmdargs[1].Type == CommandLineArgument.ArgumentType.Option && cmdargs[1].Name == "?")
            {
                Console.WriteLine(Resources.VerifyHelp);
                return mcInteropMode ? 903 : 0;
            }

            // Get options
            String input = null;
            Boolean delMissing = false;
            Boolean noconfirm = false;

            for (int i = 1; i < cmdargs.Count; i++)
            {
                if (cmdargs[i].Type == CommandLineArgument.ArgumentType.Argument)
                    input = cmdargs[i].Name;
                else
                {
                    switch (cmdargs[i].Name.ToLower())
                    {
                        case "deletemissing":
                            delMissing = true;
                            break;
                        case "noconfirm":
                            noconfirm = true;
                            break;
                    }
                }
            }

            // validate
            if (input == null)
            {
                WriteMessage(MessageType.Error, "Error 907: Input Path not specified!\n");
                return 907;
            }
            if (!File.Exists(input))
            {
                WriteMessage(MessageType.Error, "Error 909: Input file does not exist!\n");
                return 909;
            }
            if (Path.GetExtension(input).ToLower() != ".mcres")
            {
                WriteMessage(MessageType.Error, "Error 913: Input file not supported!\n");
                return 913;
            }

            // Load Resource manager
            CharacterResourceManager resmgr = CharacterResourceManager.FromXML(input);
            String root = Path.GetDirectoryName(input);

            // Verify
            List<CharaSet> missing = new List<CharaSet>();
            foreach (CharaSetGroup csg in resmgr)
                foreach (CharaSet cset in csg)
                    foreach (CharaPart cpart in cset)
                    {
                        if (!File.Exists(Path.Combine(root, cpart.FileName)))
                        {
                            missing.Add(cset);
                            break;
                        }
                    }

            if (missing.Count != 0)
            {
                // print out missing list
                WriteMessage(MessageType.Routine, "Mising files have been detected in following sets:\n");
                foreach (CharaSet cset in missing)
                {
                    Console.WriteLine("      {0}/{1}", cset.Parent.Name, cset.Name);
                }

                // confirmation
                if (delMissing && !noconfirm)
                {
                    WriteMessage(MessageType.Routine, "These sets will now be deleted, are you sure to proceed? [Y/N]");
                    while (true)
                    {
                        String response = Console.ReadLine();
                        if (response.ToUpper().StartsWith("Y")) break;
                        else if (response.ToUpper().StartsWith("N"))
                        {
                            WriteMessage(MessageType.Routine, "Action cancelled by user.\n");
                            return 915;
                        }
                    }
                }

                if (delMissing)
                {
                    foreach (CharaSet set in missing)
                    {
                        set.Parent.Remove(set);
                    }

                    using (FileStream fs = new FileStream(input, FileMode.Create))
                    {
                        XmlWriter xw = XmlTextWriter.Create(fs, new XmlWriterSettings()
                        {
                            Encoding = Encoding.UTF8,
                            Indent = true,
                            IndentChars = "\t"
                        });

                        resmgr.ToXml(xw);
                        xw.Flush();
                    }

                }

                WriteMessage(MessageType.Routine, "Success!\n");
            }
            else
            {
                WriteMessage(MessageType.Routine, "No missing files have been detected!\n");
            }
            return 0;
        }
    }
}
