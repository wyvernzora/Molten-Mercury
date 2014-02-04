using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoltenMercury.Interop;
using System.IO;
using MoltenMercury.DataModel;
using libWyvernzora.IO;
using libWyvernzora.IO.Packaging;
using System.Xml;
using MoltenMercury.ImageProcessing;

namespace MCU
{
    partial class Program
    {

        static int Pack(CommandLineArguments cmdargs)
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
                Console.WriteLine(Resources.PackHelp);
                return mcInteropMode ? 903 : 0;
            }

            // Define option variables
            Boolean saveState = false;
            Boolean trimArchive = false;
            Boolean lockArchive = false;
            Boolean verbose = false;
            Boolean patch = false;
            String input = null;
            String output = null;

            // Get options and arguments
            for (int i = 1; i < cmdargs.Count; i++)
            {
                if (cmdargs[i].Type == CommandLineArgument.ArgumentType.Argument)
                {
                    if (input == null)
                    {
                        input = cmdargs[i].Name;
                        output = Path.GetDirectoryName(input) + ".mcpak";
                    }
                    else if (input != null)
                    {
                        output = cmdargs[i].Name;
                    }
                }
                else
                {
                    switch (cmdargs[i].Name.ToLower())
                    {
                        case "savestate":
                            saveState = true;
                            break;
                        case "trim":
                            saveState = true;
                            trimArchive = true;
                            break;
                        case "lock":
                            lockArchive = true;
                            break;
                        case "patch":
                            patch = true;
                            break;
                        case "verbose":
                            verbose = true;
                            break;
                    }
                }
            }

            // Validate
            if (input == null)
            {
                WriteMessage(MessageType.Error, "Error 907: Input path not specified!\n");
                return 907;
            }
            if (!File.Exists(input))
            {
                WriteMessage(MessageType.Error, "Error 909: Specified input file does not exist!\n");
                return 909;
            }

            // Pack Resources
//#if !DEBUG
            try
            {
//#endif
                // Create Resource Manager
                CharacterResourceManager resmgr = null;

                // Load resource manager according to the input file supplied
                if (Path.GetExtension(input) == ".mcres")
                {
                    resmgr = CharacterResourceManager.FromXML(input);
                    resmgr.FileSystemProxy = new DefaultFileSystemProxy(Path.GetDirectoryName(input));
                }
                else if (Path.GetExtension(input) == ".mcpak")
                {
                    AFSFileSystemProxy proxy = new AFSFileSystemProxy(input);
                    PackageEntry mcresEntry = proxy.Archive.TryGetEntry("character.mcres");

                    if (mcresEntry == null)
                    {
                        WriteMessage(MessageType.Error, "Error 911: Cannot find resource descriptor entry!\n");
                        return 911;
                    }

                    using (ExStream exs = new ExStream())
                    {
                        proxy.Archive.Extract(mcresEntry, exs);
                        exs.Position = 0;
                        resmgr = CharacterResourceManager.FromXML(exs);
                        resmgr.FileSystemProxy = proxy;
                    }
                }
                else if (Path.GetExtension(input) == ".ini")
                {
                    WriteMessage(MessageType.Error, "Error 912: Directly packing Legacy directories is not supported!\n" +
                        "      You may try using LEGACY command on the input file (directory) first.\n" +
                        "      For more information type \"mcu LEGACY /?\"\n");
                    return 912;
                }
                else
                {
                    WriteMessage(MessageType.Error, "Error 913: Input file extension [{0}] not recognized!\n", Path.GetExtension(input));
                    return 913;
                }

                // Load state if needed
                if (saveState)
                {
                    try
                    {
                        Stream stateStream = resmgr.FileSystemProxy.GetSavedStateStream();
                        if (stateStream != null)
                            LoadCharacterFromStream(resmgr, stateStream);
                        else
                        {
                            if (!trimArchive)
                            {
                                WriteMessage(MessageType.Routine, "No saved state found! No state will be saved into the archive!\n");
                                saveState = false;
                            }
                            else
                            {
                                WriteMessage(MessageType.Error, "Error 914: No saved state found! Cannot apply /trim option!\n");
                                return 914;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!trimArchive)
                        {
                            WriteMessage(MessageType.Routine, "No saved state found! No state will be saved into the archive!\n");
                            saveState = false;
                        }
                        else
                        {
                            WriteMessage(MessageType.Error, "Error 914: No saved state found! Cannot apply /trim option!\n");
                            return 914;
                        }
                        throw ex;
                    }
                }
                
                // Create packager
                CharacterResourcePacker packer = new CharacterResourcePacker(output, resmgr);
                packer.SaveState = saveState;
                packer.LockCharacter = lockArchive;
                packer.OmitUnusedResources = trimArchive;
                packer.IsPatch = patch;

                // Set up verbose mode if needed
                if (verbose)
                {
                    packer.ArchiveGenerator.NotifyDetails = (String s) =>
                        {
                            Console.Write("      {0}", s);
                        };
                }

                // Start making archive
                if (verbose) WriteMessage(MessageType.Routine, "");
                packer.PackResources();
                if (verbose) Console.WriteLine();

                // Report success
                if (!packer.ArchiveGenerator.HasErrors)
                {
                    WriteMessage(MessageType.Routine, "Success!\n");
                    return 0;
                }
                else
                {
                    WriteMessage(MessageType.Routine, "Error 918: Archive generator reported error(s)!\n");
                    return 918;
                }
                
                
//#if !DEBUG
            }
            catch (Exception ex)
            {
                WriteMessage(MessageType.Error, "An exception of type {0} occured!\n" +
                    "      Exception Message: {1}\n" +
                    "      Stack Trace:\n{2}", ex.GetType().Name,
                    ex.Message, ex.StackTrace);
                return 999;
            }
//#endif
        }

        static void LoadCharacterFromStream(CharacterResourceManager resmgr, Stream s)
        {
                // Load XMLDocument
                XmlDocument doc = new XmlDocument();
                s.Position = 0;
                doc.Load(s);

                // Get Character Name and Verify it
                String name = doc.SelectSingleNode("MoltenMercuryState").Attributes["name"].InnerText;
                if (name != resmgr.Name)
                {
                    WriteMessage(MessageType.Routine, "Character names in resources and in the state don't seem to match!\n" +
                        "      Do you want to force-load state? [Y/N]");
                    while (true)
                    {
                        if (Console.ReadLine().ToUpper().StartsWith("Y")) break;
                        else if (Console.ReadLine().ToUpper().StartsWith("N")) return;
                    }
                }

                // Clear current selection and layer adjustment data
                resmgr.ClearSelection();

                foreach (CharaSetGroup csg in resmgr)
                    foreach (CharaSet cset in csg)
                        cset.LayerAdjustment = 0;


                // Load Selection
                foreach (XmlNode selNode in doc.SelectNodes("MoltenMercuryState/Selection"))
                {
                    String groupName = selNode.Attributes["group"].InnerText;
                    String setName = selNode.Attributes["set"].InnerText;

                    CharaSetGroup cgroup = resmgr.GetGroupByName(groupName);
                    if (cgroup == null)
                        throw new Exception(String.Format("CharaSetGroup {0} Not Found!", groupName));
                    CharaSet cset = cgroup.GetSetByName(setName);
                    if (cset == null)
                        throw new Exception(String.Format("CharaSet {0} Not Found!", setName));

                    if (cgroup.Multiselect)
                    {
                        Int32 ladj = Int32.Parse(selNode.Attributes["adjustment"].InnerText);
                        cset.LayerAdjustment = ladj;
                    }

                    cset.Selected = true;
                }

                // Regenerate layer adjustment values
                foreach (CharaSetGroup csg in resmgr)
                {
                    if (csg.Multiselect)
                        GenerateLayerAdjustments(csg);
                }

                resmgr.Processors.Clear();
                

                // Load Color Presets
                foreach (XmlNode colNode in doc.SelectNodes("MoltenMercuryState/ImageProcessor"))
                {
                    String group = colNode.Attributes["colorgroup"].InnerText;
                    String preset = colNode.InnerText;

                    if (resmgr.Processors.ContainsKey(group))
                        resmgr.Processors[group].DecodeSettings(preset);
                    else
                        resmgr.Processors.Add(group, (new ImageProcessor()).DecodeSettings(preset));
                }
                
        }

        static void GenerateLayerAdjustments(CharaSetGroup csg)
        {
            List<Int32> ordered = new List<int>();
            foreach (CharaSet set in csg)
                if (set.LayerAdjustment != 0) ordered.Add(set.LayerAdjustment);

            Int32 cid = 0;
            for (int i = 0; i < csg.Count; i++)
            {
                Int32 ladj = CalculateLayerAdjustment(csg, cid);

                while (ordered.Contains(ladj))
                {
                    cid++;
                    ladj = CalculateLayerAdjustment(csg, cid);
                }

                if (csg[i].LayerAdjustment != 0)
                    continue;
                else
                {
                    csg[i].LayerAdjustment = ladj;
                    cid++;
                }
            }
        }

        static Int32 CalculateLayerAdjustment(CharaSetGroup group, Int32 index)
        {
            Int32 ladj = (group.Count - index) * group.ResourceManager.Count + group.ResourceManager.IndexOf(group);
            return ladj;
        }
    }
}
