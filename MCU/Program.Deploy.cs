using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoltenMercury.DataModel;
using MoltenMercury.Interop;
using System.IO;
using libWyvernzora.IO;
using libWyvernzora.IO.Packaging;
using System.Xml;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace MCU
{
    partial class Program
    {
        enum CollisionHandling
        {
            Prompt,
            Error = 1,
            Rename = 2,
            Overwrite = 3,
            Skip = 4
        }

        static int Deploy(CommandLineArguments cmdargs)
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
                Console.WriteLine(Resources.DeployHelp);
                return mcInteropMode ? 903 : 0;
            }

            // Get Options
            String deployRoot = "patches\\%PATCHNAME%";
            String targetPath = null;
            String patchPath = null;
            Boolean merge = false;
            Boolean update = false;
            Boolean checkhash = false;
            CollisionHandling collisionMode = CollisionHandling.Prompt;

            #region Get Options

            for (int i = 1; i < cmdargs.Count; i++)
            {
                if (cmdargs[i].Type == CommandLineArgument.ArgumentType.Argument)
                {
                    if (patchPath == null)
                        patchPath = cmdargs[i].Name;
                    else if (targetPath == null)
                        targetPath = cmdargs[i].Name;
                }
                else
                {
                    switch (cmdargs[i].Name.ToLower())
                    {
                        case "deployroot":
                            {
                                if (cmdargs[i].Arguments.Length == 0)
                                {
                                    WriteMessage(MessageType.Error, "Error 905: /deployroot must have a string argument!\n");
                                    return 905;
                                }

                                deployRoot = cmdargs[i].Arguments[0];
                            }
                            break;
                        case "merge":
                            {
                                merge = true;
                            }
                            break;
                        case "update":
                            {
                                update = true;
                            }
                            break;
                        case "checkhash":
                            checkhash = true;
                            break;
                        case "collision":
                            {
                                if (cmdargs[i].Arguments.Length == 0)
                                {
                                    WriteMessage(MessageType.Error, "Error 905: /collision must have a string argument!\n");
                                    return 905;
                                }

                                switch (cmdargs[i].Arguments[0].ToLower())
                                {
                                    case "prompt":
                                        collisionMode = CollisionHandling.Prompt;
                                        break;
                                    case "error":
                                        collisionMode = CollisionHandling.Error;
                                        break;
                                    case "overwrite":
                                        collisionMode = CollisionHandling.Overwrite;
                                        break;
                                    case "rename":
                                        collisionMode = CollisionHandling.Rename;
                                        break;
                                    case "skip":
                                        collisionMode = CollisionHandling.Skip;
                                        break;
                                    default:
                                        {
                                            WriteMessage(MessageType.Error, "Error 905: /collision argument \"{0}\" not recognized!\n", cmdargs[i].Arguments[0]);
                                            return 905;
                                        }
                                }

                            }
                            break;
                    }
                }
            }
            #endregion

            // Verify
            if (patchPath == null)
            { WriteMessage(MessageType.Error, "Error 907: Pacth file not specified!\n"); return 907; }
            if (targetPath == null)
            { WriteMessage(MessageType.Error, "Error 908: Target path not specified!\n"); return 908; }
            if (!File.Exists(patchPath))
            { WriteMessage(MessageType.Error, "Error 909: Specified patch file does not exist!\n"); return 909; }
            if (!File.Exists(targetPath))
            { WriteMessage(MessageType.Error, "Error 910: Specified target resource descriptor does not exist!\n"); return 910; }
            if (Path.GetExtension(targetPath).ToLower() != ".mcres")
            { WriteMessage(MessageType.Error, "Error 916: Target file not supported!\n"); return 916; }
            if (Path.GetExtension(patchPath).ToLower() != ".mcpak")
            { WriteMessage(MessageType.Error, "Error 913: Patch file not supported!\n"); return 913; }

            // Change settings
            if (merge) deployRoot = "";
            if (update) collisionMode = CollisionHandling.Overwrite;

            // load both resources
            // load target resource manager
            CharacterResourceManager target = CharacterResourceManager.FromXML(targetPath);
            String targetRoot = Path.GetDirectoryName(targetPath);
            target.FileSystemProxy = new DefaultFileSystemProxy(targetRoot);

            // load patch resource manager
            CharacterResourceManager patch = null;
            AFSFileSystemProxy afsproxy = new AFSFileSystemProxy(patchPath);
            using (ExStream descrs = new ExStream())
            {
                PackageEntry mcres = afsproxy.Archive.TryGetEntry("character.mcres");
                if (mcres == null)
                { WriteMessage(MessageType.Error, "Error 911: Cannot find descriptor in patch archive!\n"); return 911; }
                afsproxy.Archive.Extract(mcres, descrs);
                descrs.Position = 0;
                patch = CharacterResourceManager.FromXML(descrs);
            }
            // load patch filename mapping if it exists
            Dictionary<String, String> fileNameMap = null;
            PackageEntry fmapentry = afsproxy.Archive.TryGetEntry(".patch");
            if (fmapentry != null)
            {
                fileNameMap = new Dictionary<string, string>();
                XmlDocument fmapdoc = new XmlDocument();
                using (ExStream fmaps = new ExStream())
                {
                    afsproxy.Archive.Extract(fmapentry, fmaps);
                    fmaps.Position = 0;
                    fmapdoc.Load(fmaps);
                }
                foreach (XmlNode mapNode in fmapdoc.SelectNodes("FileNameMap/Mapping"))
                {
                    String hashed = mapNode.Attributes["key"].InnerText;
                    String original = mapNode.InnerText;

                    fileNameMap.Add(hashed, original);
                }
            }
            patch.FileSystemProxy = afsproxy;


            // finalize deploy root
            deployRoot = deployRoot.Replace("%PATCHNAME%", Path.GetFileNameWithoutExtension(patchPath));

            // Virtual Extraction to identify possible problems
            Dictionary<PackageEntry, String> extractionPaths = new Dictionary<PackageEntry, string>();
            Dictionary<String, String> relink = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            List<PackageEntry> problems = new List<PackageEntry>();

            foreach (PackageEntry entry in afsproxy.Archive.Entries)
            {
                if (entry.Name.ToLower() == ".patch" || entry.Name.ToLower() == ".lock") continue;
                String ext = Path.GetExtension(entry.Name).ToLower();
                if (ext == ".mcres" || ext == ".mcs") continue;

                String entryName = (fileNameMap != null && fileNameMap.ContainsKey(entry.Name)) ?
                    fileNameMap[entry.Name] : entry.Name;

                String relExtrPath = Path.Combine(deployRoot, entryName);
                String absExtrPath = Path.Combine(targetRoot, relExtrPath);

                if (File.Exists(absExtrPath))
                {
                    if (checkhash)
                    {
                        // Files collided, verify whether thay are identical
                        String targetHash = GetSHA(absExtrPath);
                        String patchHash = null;
                        using (ExStream exs = new ExStream())
                        {
                            afsproxy.Archive.Extract(entry, exs);
                            patchHash = GetSHA(exs);
                        }

                        if (targetHash == patchHash)
                            continue;  // Both files are identical, skip extraction
                    }

                    // they are not identical or checkhash disabled... need to do something
                    CollisionHandling handling = collisionMode;
                    if (collisionMode == CollisionHandling.Prompt)
                    {
                        WriteMessage(MessageType.Routine, "File collision detected:\n");
                        Console.WriteLine("        Target: {0}", relExtrPath);
                        Console.WriteLine("        Patch: {0}", entryName);
                        Console.WriteLine("        1) Error\n        2) Rename\n        3) Overwrite\n        4) Skip");
                        while (true)
                        {
                            Console.Write("        Action:");
                            Int32 result;
                            if (!Int32.TryParse(Console.ReadLine(), out result)) continue;
                            if (result < 1 || result > 4) continue;
                            handling = (CollisionHandling)result;
                            if (handling == CollisionHandling.Error) // if user choses error, no need to prompt for further collisions
                                collisionMode = CollisionHandling.Error;
                            break;
                        }
                    }

                    // handle collision
                    switch (handling)
                    {
                        case CollisionHandling.Error:
                            {
                                problems.Add(entry);
                                continue;
                            }
                        case CollisionHandling.Rename:
                            {
                                Int32 cid = 1;
                                string nfname = relExtrPath;
                                while (true)
                                {
                                    nfname = Path.Combine(Path.GetDirectoryName(relExtrPath),
                                        String.Format("{0}({1}){2}", Path.GetFileNameWithoutExtension(relExtrPath),
                                        cid++, Path.GetExtension(relExtrPath)));
                                    if (!File.Exists(Path.Combine(targetRoot, nfname))) break;
                                }

                                extractionPaths.Add(entry, nfname);
                                relink.Add(entry.Name, nfname);
                                continue;
                            }
                        case CollisionHandling.Overwrite:
                            extractionPaths.Add(entry, relExtrPath);
                            relink.Add(entry.Name, relExtrPath);
                            continue;
                        case CollisionHandling.Skip:
                            relink.Add(entry.Name, relExtrPath);
                            continue;
                    }

                }
                else
                {
                    extractionPaths.Add(entry, relExtrPath);
                    relink.Add(entry.Name, relExtrPath);
                }
            }

            if (problems.Count != 0)
            {
                WriteMessage(MessageType.Error, "{0} Error(s) have been detected!\n", problems.Count);
                foreach (PackageEntry entry in problems)
                {
                    String entryName = (fileNameMap != null && fileNameMap.ContainsKey(entry.Name)) ?
                    fileNameMap[entry.Name] : entry.Name;

                    String relExtrPath = Path.Combine(deployRoot, entryName);
                    Console.WriteLine("        Collision with {0}", relExtrPath);
                }
                WriteMessage(MessageType.Error, "Error 917: Deployment failed due to unresolved file collisions!\n");

                return 917;
            }

            // Extract Files
            foreach (KeyValuePair<PackageEntry, String> extr in extractionPaths)
            {
                String path = Path.Combine(targetRoot, extr.Value);
                afsproxy.Archive.ExtractSingle(extr.Key, path);
            }

            // Merge Color Presets
            foreach (String colorg in patch.Presets.Keys)
            {
                if (!target.Presets.ContainsKey(colorg))
                    target.Presets.Add(colorg, patch.Presets[colorg]);
            }

            // Merge Character Part Records
            foreach (CharaSetGroup patchsg in patch)
            {
                // Skip empty set groups 
                if (patchsg.Count == 0) continue;

                // Get target chara set group
                CharaSetGroup targetsg = target.GetGroupByName(patchsg.Name);
                if (targetsg == null)
                {
                    // if it doesn't exist, create it
                    targetsg = new CharaSetGroup(patchsg.Name, patchsg.Multiselect);
                    target.Add(targetsg);
                }

                // foreach chara set in the patch group
                foreach (CharaSet patchset in patchsg)
                {
                    // Get target chara set
                    CharaSet targetset = targetsg.GetSetByName(patchset.Name);

                    // if the set exists and update flag is up, delete it
                    if (targetset != null && update)
                    {
                        targetsg.Remove(targetset);
                        targetset = new CharaSet(patchset.Name);
                    }
                    else if (targetset != null)
                    { // if the set exists but update flag is not up, rename the new set
                        Int32 index = 1;

                        Match m = Regex.Match(patchset.Name, "(?<name>.+?)(?<index>[0-9]+)");
                        if (m.Success)
                        {
                            index = Int32.Parse(m.Result("${index}"));
                            patchset.Name = m.Result("${name}");
                        }
                        
                        while (true)
                        {
                            if (targetsg.GetSetByName(patchset.Name + (index++).ToString()) == null)
                                break;
                        }
                        targetset = new CharaSet(patchset.Name + (index).ToString());
                    }
                    else
                    { // if it doesn't exist, create it
                        targetset = new CharaSet(patchset.Name);
                    }

                    // Add target set to target
                    targetsg.Add(targetset);

                    // copy all parts taking file renaming into consideration
                    foreach (CharaPart patchpart in patchset)
                    {
                        String filePath = relink[patchpart.FileName];

                        targetset.Add(new CharaPart(patchpart.UnadjustedLayer, filePath, patchpart.ColorScheme));
                    }
                }

                targetsg.SortSets();
            }

            if (File.Exists(Path.ChangeExtension(targetPath, "bak")))
                File.Delete(Path.ChangeExtension(targetPath, "bak"));
            File.Move(targetPath, Path.ChangeExtension(targetPath, "bak"));
            using (XmlWriter xw = XmlTextWriter.Create(targetPath, 
                new XmlWriterSettings() { Indent = true, IndentChars = "\t", Encoding = Encoding.UTF8 }))
            {
                target.ToXml(xw);
                xw.Flush();
            }

            WriteMessage(MessageType.Routine, "Success!\n");
            return 0;
        }


        static string GetSHA(String path)
        {
            String sha = null;
            using (FileStream fs = new FileStream(path, FileMode.Open))
                sha = GetSHA(fs);
            return sha;
        }
        static string GetSHA(Stream s)
        {
            s.Position = 0;
            SHA256 hasher = SHA256.Create();
            return libWyvernzora.HexTools.Byte2String(hasher.ComputeHash(s)).ToUpper();
        }


    }

}