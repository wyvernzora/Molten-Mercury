using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoltenMercury.Interop;
using System.IO;
using MoltenMercury.DataModel;
using System.Security.Cryptography;
using System.Xml;

namespace MCU
{
    partial class Program
    {
        static int Cleanup(CommandLineArguments cmdargs)
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
                Console.WriteLine(Resources.CleanupHelp);
                return mcInteropMode ? 903 : 0;
            }

            String input = null;
            String moveLocation = "mcu.unused";
            Boolean delete = false;
            Boolean noconfirm = false;
            Boolean deepClean = false;
            String[] ignore = new String[] { "mcres", "mcs", "ini" };

            for (int i = 1; i < cmdargs.Count; i++)
            {
                if (cmdargs[i].Type == CommandLineArgument.ArgumentType.Argument)
                    input = cmdargs[i].Name;
                else
                {
                    switch (cmdargs[i].Name.ToLower())
                    {
                        case "move":
                            {
                                if (cmdargs[i].Arguments.Length == 0)
                                {
                                    WriteMessage(MessageType.Error, "Error 905: /move must have a string argument!");
                                    return 905;
                                }

                                moveLocation = cmdargs[i].Arguments[0];
                            }
                            break;
                        case "delete":
                            delete = true;
                            break;
                        case "noconfirm":
                            noconfirm = true;
                            break;
                        case "deepclean":
                            deepClean = true;
                            break;
                        case "ignore":
                            {
                                String[] tmpIgnore = new String[ignore.Length + cmdargs[i].Arguments.Length];
                                Array.Copy(ignore, tmpIgnore, ignore.Length);
                                Array.Copy(cmdargs[i].Arguments, 0, tmpIgnore, ignore.Length, cmdargs[i].Arguments.Length);
                                //ignore = cmdargs[i].Arguments;
                                ignore = tmpIgnore;
                            }
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
                
            
            // Get All Files
            String root = Path.GetDirectoryName(input);
            Dictionary<String, Byte> files = new Dictionary<string, byte>(StringComparer.CurrentCultureIgnoreCase);
            foreach (CharaSetGroup csg in resmgr)
                foreach (CharaSet cset in csg)
                    foreach (CharaPart cpart in cset)
                    {
                        if (!files.ContainsKey(cpart.FileName))
                            files.Add(cpart.FileName, 0);
                    }



            // Recursive Lambda =w=
            List<String> unused = new List<string>();
            Dictionary<String, String> hash2name = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            Dictionary<String, String> relink = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            SHA256 hasher = SHA256.Create();
            Action<String> processDir = null;
            processDir = (String path) =>
                {
                    if (File.Exists(path))
                    {
                        if (Path.GetExtension(path).Length > 1 && ignore.Contains(Path.GetExtension(path).Substring(1)))
                            return;

                        if (!files.ContainsKey(libWyvernzora.Path.GetRelativePath(path, root)))
                        {
                            unused.Add(libWyvernzora.Path.GetRelativePath(path, root));
                        }
                        else if (deepClean)
                        {
                            String hash = null;
                            using (FileStream fs = new FileStream(path, FileMode.Open))
                                hash = libWyvernzora.HexTools.Byte2String(hasher.ComputeHash(fs)).ToUpper();

                            String relPath = libWyvernzora.Path.GetRelativePath(path, root);
                            if (!hash2name.ContainsKey(hash))
                                hash2name.Add(hash, relPath);
                            else
                            {
                                String identicalFile = hash2name[hash];
                                relink.Add(relPath, identicalFile);
                                unused.Add(relPath);    
                            }
                        }
                    }
                    else if (Directory.Exists(path))
                    {
                        if (Path.GetDirectoryName(path).ToLower().StartsWith("mcu."))
                            return;
                        if (StringComparer.CurrentCultureIgnoreCase.Equals(Path.Combine(root, moveLocation), path))
                            return;
                        if (Path.IsPathRooted(moveLocation) && StringComparer.CurrentCultureIgnoreCase.Equals(moveLocation, path))
                            return;

                        foreach (String dir in Directory.GetDirectories(path))
                            processDir(dir);
                        foreach (String file in Directory.GetFiles(path))
                            processDir(file);
                    }
                };

            processDir(root);

            if (unused.Count != 0)
            {
                WriteMessage(MessageType.Routine, "Following files have been detected as unused:\n");
                foreach (String path in unused)
                {
                    Console.WriteLine("      {0}", path);
                }
                Console.WriteLine();
                if (!noconfirm)
                {
                    WriteMessage(MessageType.Routine, "These files will now be {0}, are you sure to proceed? [Y/N]", delete ? "deleted" : "moved");
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

                if (deepClean)
                {
                    foreach (CharaSetGroup csg in resmgr)
                        foreach (CharaSet cset in csg)
                            foreach (CharaPart cpart in cset)
                            {
                                if (relink.ContainsKey(cpart.FileName))
                                    cpart.FileName = relink[cpart.FileName];
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

                if (!Path.IsPathRooted(moveLocation))
                    moveLocation = Path.Combine(root, moveLocation);

                foreach (String path in unused)
                {
                    String fname = Path.Combine(root, path);
                    if (delete)
                    {
                        File.Delete(fname);
                    }
                    else
                    {
                        String newPath = Path.Combine(moveLocation, path);
                        if (File.Exists(newPath))
                            File.Delete(newPath);
                        if (!Directory.Exists(Path.GetDirectoryName(newPath)))
                            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                        File.Move(fname, newPath);
                    }
                }

                WriteMessage(MessageType.Routine, "Success!\n");
                return 0;
            }
            else
            {
                WriteMessage(MessageType.Routine, "Resource set is already clean, nothing needs to be done.\n");
                return 0;
            }

        }
    }
}
