#define SAFE_UPDATE

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using MoltenMercury.DataModel;
using MoltenMercury.ImageProcessing;
using System.Xml;
using libWyvernzora.IO;
using libWyvernzora.IO.Packaging;
using MoltenMercury.Localization;

/* ============================================================================
 * MoltenChara.cs
 * ----------------------------------------------------------------------------
 * 2012 (C) Jieni Luchijinzhou a.k.a Aragorn Wyvernzora
 * 
 * This file is a part of the Molten Mercury Project
 * This is the main form of the Molten Chara anime character creator.
 * 
 * Notes:
 * Originally I had a method named UpdatePreview which created the composite
 * bitmap from selected character parts. It was tied to various control events
 * so that whenever a value is changed preview image can be updated accordingly.
 * However, since each change fires its own update request, I got over 100 updates
 * each time the program starts (due to initialization of controls) and even more
 * when values of some controls change (because changing values of one control may
 * trigger changes in other controls). Considering that updating preview is the most
 * expensive operation in this program, I created a few workarounds to address the
 * problem:
 * 
 * 1)   I created CharaBlocker class which acts as a lock. When an instance is created
 *      it locks the MoltenChara form in a way that attempts to update the preview will
 *      be blocked as long as the lock is in place. When CharaBlocker instance is disposed
 *      it will unlock the MoltenChara form and updates of the preview will be allowed.
 *      
 * 2)   I moved preview updating operations to a background thread so that it doesn't
 *      block UI thread even if updates occur frequently and take longer than expected.
 *      When an update is requested when there is already a running update, there is a 
 *      flag that will be checked upon completion of the running update. If the flag is
 *      true, another update will be launched immediately.
 *      
 */

namespace MoltenMercury
{
    public partial class MoltenChara : Form
    {
        #region Internal Data Types

        /// <summary>
        /// This is a utility class used for blocking UpdateCharacterPreviewAsync
        /// from being called.
        /// </summary>
        /// <remarks>
        /// This class implements IDisposable interface, therefore it's only necessary
        /// to surround a block of code with a using statement and while the surrounded
        /// code is being executed all calls to UpdateCharacterPreviewAsync will be
        /// blocked.
        /// </remarks>
        class CharaBlocker : IDisposable
        {
            MoltenChara mchar;  // Reference to the MoltenChara form
            byte dummy;     // Dummy for maintaining illusion of data IO

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="mc">MoltenChara form, usually this</param>
            public CharaBlocker(MoltenChara mc)
            { mchar = mc; mc.BlockCharacterCreation(); }

            /// <summary>
            /// Dummy method to be called after blockade is no longer needed
            /// </summary>
            /// <remarks>
            /// .Net framework has a bad habit of preliminary disposing objects if
            /// it's not used in the code ahead. This method is to be called
            /// before disposing the object to maintain an illusion that it is used.
            /// </remarks>
            public void DummyMethod()
            { dummy++; }
            public void Dispose()
            {
                mchar.UnblockCharacterCreation();
            }
        }

        /// <summary>
        /// Structure for displaying locale info
        /// </summary>
        struct Locale
        {
            public String ID { get; set; }
            public String Name { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        #endregion

        #region Fields

        Boolean noResourceMode = false; // flag indicating whether program is in no resource mode
        Dictionary<String, String> characters = new Dictionary<string, string>();       // All available characters
        CharacterResourceManager resourceManager;  // resource manager for character parts

        Boolean colorEditorActive = false;     // flag indicating whether color edit mode is active
        Dictionary<CharaPart, Bitmap> dimCache = new Dictionary<CharaPart, Bitmap>();   // Cache for semitransparent character parts

        // Boolean flags used for preventing preview from being updated
        Boolean programInitialized = false;     // Flag for program initialization
        Boolean blockCharaCreation = true;      // Flag for blocking initiated bu Block/Unblock methods
        Boolean charaCreatorThreadRunning = false;  // Flag indicating whether preview is being updated right now
        Boolean charaUpdateQueue = false;       // Flag indicating whetner another update is pending

        // Color processing related fields
        ImageProcessor tmpProcessor = new ImageProcessor();     // Temporary processor for realtime color preview

        // Lookup of all processors available, always changing to fit selection
       // Dictionary<String, ImageProcessor> resourceManager.Processors = new Dictionary<string, ImageProcessor>();

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public MoltenChara()
        {
            // Determine whether debug logging is enabled
            DebugHelper.LoggingEnabled = Properties.Settings.Default.DebugLoggingEnabled;
            DebugHelper.DebugPrint("Molten Mercury Project");
            DebugHelper.DebugPrint("2012 (C) Aragorn Wyvernzora\n");

            DebugHelper.DebugPrint("Molten Chara Start");
            InitializeComponent();

            // Hide developer options if not in debug configuration
#if !DEBUG
            miDebug.Visible = false;
#endif

            // Check for MCU
            miTools.Enabled = File.Exists(Path.Combine(Application.StartupPath, "mcu.exe"));

            // Load Default Locale
            XmlDocument defLocale = new XmlDocument();
            defLocale.LoadXml(Properties.Resources.EN_US_Locale);
            Localization.LocalizationDictionary.Initialize(defLocale);

            // Load external Localization file if it's there
            String localizationFilePath = Path.Combine(Application.StartupPath, "locale.xml");
            if (File.Exists(localizationFilePath))
            {
                DebugHelper.DebugPrint("External Localization File Found!");
                XmlDocument locdoc = new XmlDocument();
                locdoc.Load(localizationFilePath);
                Localization.LocalizationDictionary.Instance.LoadXml(locdoc);

            }
            else
                DebugHelper.DebugPrint("Localization File Not Found!");

            String cloc = Properties.Settings.Default.Locale;
            if (Localization.LocalizationDictionary.Instance.HasLocale(cloc))
                Localization.LocalizationDictionary.Instance.ChangeLocale(cloc);
            else
                Localization.LocalizationDictionary.Instance.ChangeLocale("en-us");
        
            Localization.LocalizationDictionary.Instance.OnLocaleChanged
                += (Object o, EventArgs a) => { InitializeLocale(); };
            
            InitializeLocale();
            foreach (KeyValuePair<String, String> locid in Localization.LocalizationDictionary.Instance.Locales)
            {
                Locale loc = new Locale() { ID = locid.Key, Name = locid.Value };
                cmbLocales.Items.Add(loc);
                if (loc.ID == cloc)
                    cmbLocales.SelectedItem = loc;
            }
                    

            // Detect all available characters
            DetectAvailableCharacters();
            DebugHelper.DebugPrint("{0} character resources detected!", characters.Count);

            // Use default loading path
            String loadPath = null;

            if (Environment.GetCommandLineArgs().Length > 1)        // if there are command line arguments...
            {
                String[] args = Environment.GetCommandLineArgs();       // ..fetch them
                loadPath = args[1];
                DebugHelper.DebugPrint("Command Line Arg: {0}", args[1]);
            }
            else   // No command line args
            {
                if (characters.ContainsKey(MoltenMercury.Properties.Settings.Default.Character))
                    loadPath = characters[MoltenMercury.Properties.Settings.Default.Character];
                DebugHelper.DebugPrint("No Args, Current Char: {0}", Properties.Settings.Default.Character);

                if (!File.Exists(loadPath) && characters.Count > 0)
                {
                    loadPath = characters.Values.First();
                    DebugHelper.DebugPrint("Load Path Not Found, Override: {0}", loadPath);
                }
            }


            try
            {
                // Load character resources
                LoadCharacterResourcesGeneric(loadPath);
                DebugHelper.DebugPrint("Character Resources Successfully Loaded!");
            }
            catch (Exception x)
            {
                DebugHelper.DebugPrint("{0} while loading character resources! See details below", x.GetType().FullName);
                DebugHelper.DebugPrint("Load Path: {0}", loadPath);
                DebugHelper.DebugPrint("Dir Exists: {0}", Directory.Exists(Path.GetDirectoryName(loadPath)));
                DebugHelper.DebugPrint("File Exists: {0}", File.Exists(loadPath));
                DebugHelper.DebugPrint("Exception Message: {0}", x.Message);
            }

            // Load Settings
            chkDimItems.Checked = Properties.Settings.Default.CEDimUnselected;
            chkOptionsShowImProcConfig.Checked = Properties.Settings.Default.CEShowProcConfig;

            this.Load += MoltenCharaLoaded;
        }

        void MoltenCharaLoaded(Object o, EventArgs e)
        {
            // At this point the program is initialized
            programInitialized = true;
            DebugHelper.DebugPrint("Lifted program initialization update block.");

            // if there was an exception while loading resource manager...
            if (resourceManager == null)
            {
                DebugHelper.DebugPrint("Failed to load character resources! (exception info above?)");
                DebugHelper.DebugPrint("Switching into no-resource mode");
                // go into no-resource mode
                noResourceMode = true;
                splitContainer1.Enabled = false;
                miLoadCharacterResources.Visible = true;
                miSaveCharacterImage.Enabled = false;
                miSaveCharacterState.Enabled = false;
                miSelectCharacter.DropDownItems.Add(new ToolStripMenuItem("(No Characters Available)") { Enabled = false });
            }
            else
            {
                // Update preview
                UpdateCharacterPreviewAsync();
            }

            DebugHelper.DebugPrint("Program successfully initialized!");
        }

        #region Loading / Saving

        /// <summary>
        /// Detects character resources available for loading and initializes UI accordingly
        /// </summary>
        public void DetectAvailableCharacters()
        {
            // Get application reource directory path
            String appDataRoot = Path.Combine(Application.StartupPath, "data");
            if (!Directory.Exists(appDataRoot))
                return;

            // Detect all AFS characters (extension MCPAK)
            foreach (String afspath in Directory.GetFiles(appDataRoot, "*.mcpak"))
            {
                String charaName = Path.GetFileNameWithoutExtension(afspath);
                if (!characters.ContainsKey(charaName))
                {
                    DebugHelper.DebugPrint("AFS Found: [{0}] {1}", charaName, afspath);
                    characters.Add(charaName, afspath);
                    ToolStripMenuItem item = new ToolStripMenuItem(Path.GetFileNameWithoutExtension(afspath) + " (.mcpak)");
                    item.Click += Menu_SelectCharacterClicked;
                    item.Tag = charaName;
                    miSelectCharacter.DropDownItems.Add(item);
                }
            }

            // Detect all MoltenChara resource dirs
            foreach (String dir in Directory.GetDirectories(appDataRoot))
            {
                if (File.Exists(Path.Combine(dir, "character.mcres")))
                {
                    String charaName = Path.GetFileNameWithoutExtension(dir);
                    if (!characters.ContainsKey(charaName))
                    {
                        DebugHelper.DebugPrint("MCRES Found: [{0}] {1}", charaName, dir);
                        characters.Add(charaName, Path.Combine(dir, "character.mcres"));
                        ToolStripMenuItem item = new ToolStripMenuItem(charaName + " (.mcres)");
                        item.Click += Menu_SelectCharacterClicked;
                        item.Tag = charaName;
                        miSelectCharacter.DropDownItems.Add(item);
                    }
                }
            }

            // Detect all CharaEX resource dirs (in interop mode)
            foreach (String dir in Directory.GetDirectories(appDataRoot))
            {
                if (File.Exists(Path.Combine(dir, "character.ini")))
                {
                    String charaName = Path.GetFileNameWithoutExtension(dir);
                    if (!characters.ContainsKey(charaName))
                    {
                        DebugHelper.DebugPrint("Legacy Found: [{0}] {1}", charaName, dir);
                        characters.Add(charaName, Path.Combine(dir, "character.ini"));
                        ToolStripMenuItem item = new ToolStripMenuItem(charaName + " (Legacy)");
                        item.Click += Menu_SelectCharacterClicked;
                        item.Tag = charaName;
                        miSelectCharacter.DropDownItems.Add(item);
                    }
                }
            }

        }

        /// <summary>
        /// Loads character resources.
        /// Automatically determines which loader method to use.
        /// </summary>
        /// <param name="path"></param>
        public void LoadCharacterResourcesGeneric(String path)
        {
            String ext = Path.GetExtension(path).ToLower();
            if (ext == ".mcres" || ext == ".xml")
                LoadCharacterResourcesRaw(path);
            else if (ext == ".mcpak" || ext == ".afs")
                LoadCharacterResourcesAFS(path);
            else if (ext == ".ini")
                LoadCharacterResourcesInterop(path);
            else
                throw new InvalidDataException("File extension not supported!");

            UpdateCharacterPreviewAsync();
        }

        /// <summary>
        /// Loads character resources from an XML file
        /// </summary>
        /// <param name="path">Path to the extension</param>
        public void LoadCharacterResourcesRaw(String path)
        {
            using (CharaBlocker cb = new CharaBlocker(this))
            {

                // Load Resources and reflect them on UI
                resourceManager = CharacterResourceManager.FromXML(path);
                // Set up bitmap loader with root at path dir
                resourceManager.FileSystemProxy = new DefaultFileSystemProxy(Path.GetDirectoryName(path));
                // Update UI
                ReloadResourceManager();

                // Reinitialize selection buffer
                ResetSelection();

                // Reflect color processors on the UI
                UpdateColorProcessorList();
                cmbColorProcessors.SelectedIndex = 0;

                // If there is a saved state, restore it as well
                // Here we usa a FileSystemProxy because we have no idea whether
                // the state is stored on a file or a package
                Stream state = resourceManager.FileSystemProxy.GetSavedStateStream();
                if (state != null)
                {
                    LoadCharacterFromStream(state, false);
                    state.Dispose();
                }

                // Generate Layer Adjustment values
                foreach (CharaSetGroup csg in resourceManager)
                {
                    if (!csg.Multiselect) continue;
                    GenerateLayerAdjustments(csg);
                }

                // Enable tool menu
                miTools.Enabled = File.Exists(Path.Combine(Application.StartupPath, "mcu.exe"));

                // Maintain CharaBlocker instance
                cb.DummyMethod();
            }

            DebugHelper.DebugPrint("Successfully loaded MCRES: {0}", resourceManager.Name);
        }

        /// <summary>
        /// Loads a character packed into AFS file
        /// File extension doesn't have to be AFS
        /// </summary>
        /// <param name="path"></param>
        public void LoadCharacterResourcesAFS(String path)
        {
            using (CharaBlocker cb = new CharaBlocker(this))
            {
                AFSFileSystemProxy proxy = new AFSFileSystemProxy(path);

                using (ExStream descriptor = new ExStream())
                {
                    PackageEntry entry = proxy.Archive.TryGetEntry("character.mcres");
                    proxy.Archive.Extract(entry, descriptor);
                    descriptor.Position = 0;

                    resourceManager = CharacterResourceManager.FromXML(descriptor);
                }

                resourceManager.FileSystemProxy = proxy;
                resourceManager.AllowChange = proxy.Archive.TryGetEntry(".lock") == null;


                ReloadResourceManager();
                ResetSelection();
                UpdateColorProcessorList();
                cmbColorProcessors.SelectedIndex = 0;

                // If there is a saved state, restore it as well
                // Here we usa a FileSystemProxy because we have no idea whether
                // the state is stored on a file or a package
                Stream state = resourceManager.FileSystemProxy.GetSavedStateStream();
                if (state != null)
                {
                    LoadCharacterFromStream(state, false);
                    state.Dispose();
                }
                else
                {
                    ResetSelection();
                    resourceManager.Processors.Clear();
                    UpdateColorProcessorList();
                }

                // Reflect color processors on the UI
                UpdateColorProcessorList();

                // Reload Chara Set List
                ReloadPropertyListView((CharaSetGroup)cmbCharaProp.SelectedItem);

                // Generate Layer Adjustment values
                foreach (CharaSetGroup csg in resourceManager)
                {
                    if (!csg.Multiselect) continue;
                    GenerateLayerAdjustments(csg);
                }

                // Maintain CharaBlocker instance
                cb.DummyMethod();
                
            }

            miTools.Enabled = false;
            UpdateCharacterPreviewAsync();
            DebugHelper.DebugPrint("Successfully loaded AFS: {0}", resourceManager.Name);
        }

        /// <summary>
        /// Loads character resources from another application whose 
        /// data formats are supported by this one.
        /// </summary>
        /// <param name="path"></param>
        public void LoadCharacterResourcesInterop(String path)
        {
            Boolean hasState = true;
            using (CharaBlocker cb = new CharaBlocker(this))
            {

                // Load Resources and reflect them on UI
                Interop.CharaxDirHelper interopHelper = new Interop.CharaxDirHelper(path);
                using (MemoryStream tmp = new MemoryStream())
                {
                    interopHelper.GenerateDescriptor(tmp);
                    tmp.Position = 0;
                    resourceManager = CharacterResourceManager.FromXML(tmp);
                }

                // Set up bitmap loader with root at path dir
                resourceManager.FileSystemProxy = new DefaultFileSystemProxy(Path.GetDirectoryName(path));

                ReloadResourceManager();
                ResetSelection();
                UpdateColorProcessorList();
                cmbColorProcessors.SelectedIndex = 0;

                // If there is a saved state, restore it as well
                // Here we usa a FileSystemProxy because we have no idea whether
                // the state is stored on a file or a package
                Stream state = resourceManager.FileSystemProxy.GetSavedStateStream();
                if (state != null)
                {
                    LoadCharacterFromStream(state, false);
                    state.Dispose();
                }
                else
                {
                    ResetSelection();
                    resourceManager.Processors.Clear();
                    UpdateColorProcessorList();

                    hasState = false;
                }

                // Reflect color processors on the UI
                UpdateColorProcessorList();

                // Reload Chara Set List
                ReloadPropertyListView((CharaSetGroup)cmbCharaProp.SelectedItem);

                // Generate Layer Adjustment values
                foreach (CharaSetGroup csg in resourceManager)
                {
                    if (!csg.Multiselect) continue;
                    GenerateLayerAdjustments(csg);
                }

                // Maintain CharaBlocker instance
                cb.DummyMethod();

            }
            miTools.Enabled = false;
            if (!hasState) UpdateCharacterPreviewAsync();
            DebugHelper.DebugPrint("Successfully loaded Legacy: {0}", resourceManager.Name);
        }

        /// <summary>
        /// Saves current state (selections and presets) to default save file
        /// </summary>
        public void SaveCharacterState()
        {
            // Here save procedure goes through FileSystemProxy because
                // we do not know whether currently loaded resource set is a 
                // directory or package
            using (MemoryStream ms = new MemoryStream())
            {
                SaveCharacterToStream(ms);
                resourceManager.FileSystemProxy.WriteSavedState(ms);
            }
        }

        /// <summary>
        /// Write current character state to a file
        /// </summary>
        /// <param name="path">Path of the save file</param>
        public void SaveCharacterToFile(String path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
                SaveCharacterToStream(fs);
        }

        /// <summary>
        /// Writes current character state to a stream
        /// </summary>
        /// <param name="s">Target stream</param>
        public void SaveCharacterToStream(Stream s)
        {
            using (XmlWriter xw = XmlTextWriter.Create(s, new XmlWriterSettings() { Encoding = Encoding.UTF8, Indent = true, IndentChars = "\t" }))
            {
                xw.WriteStartDocument();

                xw.WriteStartElement("MoltenMercuryState");
                xw.WriteAttributeString("name", resourceManager.Name);

                foreach (KeyValuePair<String, ImageProcessor> kvp in resourceManager.Processors)
                {
                    xw.WriteStartElement("ImageProcessor");
                    xw.WriteAttributeString("colorgroup", kvp.Key);
                    xw.WriteString(kvp.Value.EncodeSettings());
                    xw.WriteEndElement();
                }

                foreach (CharaSet set in resourceManager.SelectedSets)
                {
                    xw.WriteStartElement("Selection");
                    xw.WriteAttributeString("group", set.Parent.Name);
                    xw.WriteAttributeString("set", set.Name);
                    if (set.Parent.Multiselect)
                        xw.WriteAttributeString("adjustment", set.LayerAdjustment.ToString());
                    xw.WriteEndElement();
                }

                xw.WriteEndDocument();
            }
        }

        /// <summary>
        /// Loads character state from a file
        /// </summary>
        /// <param name="path">Path of the save file</param>
        public void LoadCharacterFromFile(String path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open))
                LoadCharacterFromStream(fs, true);
        }

        /// <summary>
        /// Loads character state from a stream
        /// </summary>
        /// <param name="s"></param>
        public void LoadCharacterFromStream(Stream s, Boolean autoLoadResources = false)
        {
            try
            {
                // Load XMLDocument
                XmlDocument doc = new XmlDocument();
                s.Position = 0;
                doc.Load(s);

                // Get Character Name and Verify it
                String name = doc.SelectSingleNode("MoltenMercuryState").Attributes["name"].InnerText;
                if (name != resourceManager.Name)
                {
                    if (autoLoadResources && characters.ContainsKey(name))
                    {
                        LoadCharacterResourcesGeneric(characters[name]);
                    }
                    else
                    {
                        MessageBox.Show(String.Format(Localization.LocalizationDictionary.Instance["ERR_701"], name));
                        return;
                    }
                }

                // Clear current selection and layer adjustment data
                resourceManager.ClearSelection();
                ClearLayerAdjustments();

                // Load Selection
                foreach (XmlNode selNode in doc.SelectNodes("MoltenMercuryState/Selection"))
                {
                    String groupName = selNode.Attributes["group"].InnerText;
                    String setName = selNode.Attributes["set"].InnerText;

                    CharaSetGroup cgroup = resourceManager.GetGroupByName(groupName);
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
                foreach (CharaSetGroup csg in resourceManager)
                {
                    if (csg.Multiselect)
                        GenerateLayerAdjustments(csg);
                }

                resourceManager.Processors.Clear();
                UpdateColorProcessorList();

                // Load Color Presets
                foreach (XmlNode colNode in doc.SelectNodes("MoltenMercuryState/ImageProcessor"))
                {
                    String group = colNode.Attributes["colorgroup"].InnerText;
                    String preset = colNode.InnerText;

                    if (resourceManager.Processors.ContainsKey(group))
                        resourceManager.Processors[group].DecodeSettings(preset);
                }

                tmpProcessor = (ImageProcessor)resourceManager.Processors[cmbColorProcessors.Text].Clone();
                SyncTmpToUI();

                // Reflect changes on UI
                using (CharaBlocker cb = new CharaBlocker(this))
                {
                    ReloadPropertyListView((CharaSetGroup)cmbCharaProp.SelectedItem);
                    cb.DummyMethod();
                }

            }
            catch
            {


                // Done, Delete all cached data and Update Stuff
                UpdateCharacterPreviewAsync();
            }
        }

        #endregion

        #region Selection and Preview Updates
        
        /// <summary>
        /// Restore default selection values
        /// </summary>
        public void ResetSelection()
        {
            DebugHelper.DebugPrint("Selection Reset");

            // Clear all present selections
            resourceManager.ClearSelection();

            // Add the first item to selection unless set group is multiselectable
            for (int i = 0; i < resourceManager.Count; i++)
            {
                if (!resourceManager[i].Multiselect)
                    resourceManager[i][0].Selected = true;
            }
        }

        /// <summary>
        /// Generates default layer adjustment values
        /// </summary>
        /// <remarks>
        /// All CharaSet object whose layer adjustment is not 0 will
        /// 
        /// </remarks>
        /// <param name="csg"></param>
        void GenerateLayerAdjustments(CharaSetGroup csg)
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

        /// <summary>
        /// Set all layer adjustment values to 0
        /// </summary>
        public void ClearLayerAdjustments()
        {
            foreach (CharaSetGroup csg in resourceManager)
            {
                if (!csg.Multiselect) continue;
                foreach (CharaSet set in csg)
                    set.LayerAdjustment = 0;
            }
        }

        /// <summary>
        /// Updates character preview
        /// </summary>
        public void UpdateCharacterPreviewAsync()
        {
            // Fetch some values from controls since they can't be
                // accessed from another thread
            Boolean dimItems = chkDimItems.Checked;
            String currentProcessor = (String)cmbColorProcessors.Text.Clone();

            // If there is a reason to block this call, do it
            if (blockCharaCreation || !programInitialized)
            {
                return;
            }

            System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace();

            // If the preview is already being updated, make this request pending
            if (charaCreatorThreadRunning)
            {
                DebugHelper.DebugPrint("Queued From: {0}", stack.GetFrame(1).GetMethod().Name);
                charaUpdateQueue = true;
                return;
            }

            // Some debug code
            DebugHelper.DebugPrint("Called From {0}", stack.GetFrame(1).GetMethod().Name);

            // Save Character State
            SaveCharacterState();

            // Create a worker for updating
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (Object o, DoWorkEventArgs e) =>
                {

                    // acquire a lock, just in case if 2 calls come together
                        lock (this)
                        {

#if SAFE_UPDATE
                            try
                            {
#endif
                                // temporary list of all selected parts
                                List<CharaPart> parts = new List<CharaPart>();

                                // Fill the list with selected parts
                                foreach (CharaSet selSet in resourceManager.SelectedSets)
                                    parts.AddRange(selSet);

                                // sort the list according to layer indecies
                                parts.Sort(new Comparison<CharaPart>((CharaPart a, CharaPart b) =>
                                { return a.Layer.CompareTo(b.Layer); }));

                                // Create a new bitmap where the new preview will be drawn
                                Bitmap bmp = new Bitmap(resourceManager.Width, resourceManager.Height);
                                // Graphics object from that bitmap
                                Graphics g = Graphics.FromImage(bmp);

                                // Draw all the parts
                                for (int i = 0; i < parts.Count; i++)
                                {
                                    // Get default bitmap of the character part
                                    Bitmap part = parts[i].Bitmap;

                                    // if this part's color scheme is not none...
                                    if (parts[i].ColorScheme != "none")
                                    {
                                        // if the bitmap is not processed yet or it's out of date(buffer empty)...
                                        if (parts[i].ProcessedBuffer == null || parts[i].ProcessedBufferVersion != resourceManager.Processors[parts[i].ColorScheme].GetHashCode())
                                        {
                                            // process it and put the result into buffer
                                            parts[i].ProcessedBuffer = resourceManager.Processors[parts[i].ColorScheme].ProcessBitmap(parts[i].Bitmap);
                                            parts[i].ProcessedBufferVersion = resourceManager.Processors[parts[i].ColorScheme].GetHashCode();
                                        }

                                        // fetch processed varsion of the bitmap
                                        part = parts[i].ProcessedBuffer;
                                    }

                                    // if the bitmap needs to be dimmed (semitransparent)
                                    Boolean dim = colorEditorActive && dimItems && (parts[i].ColorScheme != currentProcessor);
                                    if (dim)
                                    {
                                        // if it was not processed due to absence of color scheme
                                        if (parts[i].ColorScheme == "none")
                                            // copy original bitmap to processed buffer
                                            parts[i].ProcessedBuffer = parts[i].Bitmap;

                                        // check if dimmed version of the bitmap is already there
                                        if (!dimCache.ContainsKey(parts[i]))
                                            // if it's not, create it and put it into the buffer
                                            dimCache.Add(parts[i], ImageProcessor.TRANSPARENCY_PROCESSOR.ProcessBitmap(parts[i].ProcessedBuffer));

                                        // fetch the dimmed version of the bitmap
                                        part = dimCache[parts[i]];
                                    }

                                    // draw the bitmap onto the result image using graphics object
                                    g.DrawImage(part, 0, 0);
                                }

                                // tell worker to pass resulting bitmap as result
                                e.Result = bmp;
#if SAFE_UPDATE
                            }
                            catch
                            {
                                // Set result to null meaning update failed
                                e.Result = null;
                            }
#endif
                        }
                };
            bw.RunWorkerCompleted += (Object o, RunWorkerCompletedEventArgs e) =>
                {
                    // Check whether update was successful
                    if (e.Result != null)
                    {
                        // when worker is done, update UI with the new preview
                        pbPreview.Image = (Bitmap)e.Result;
                    }
                    else
                    {
                        // if failed, set a pending update request
                        charaUpdateQueue = true;
                    }
                        
                    // since update is finished, set the flag to false
                    charaCreatorThreadRunning = false;
                    // if there are pending updates
                    if (charaUpdateQueue)
                    {
                        // execute pending updates
                        charaUpdateQueue = false;
                        DebugHelper.DebugPrint("Executing Queued Update!");
                        UpdateCharacterPreviewAsync();
                    }
                };

            // turn on the updating flag and run the update
            charaCreatorThreadRunning = true;
            bw.RunWorkerAsync();
        }

        /// <summary>
        /// Swaps two items in the chara set list view
        /// </summary>
        /// <param name="indexA"></param>
        /// <param name="indexB"></param>
        public void SwapCharacterSetListItems(Int32 indexA, Int32 indexB)
        {
            // Swap if necessary to make sure B >= A
            if (indexB < indexA)
            {
                Int32 tmp = indexA;
                indexA = indexB;
                indexB = tmp;
            }

            // Check whether they are selected to restore selection later
            Boolean indexAselected = lvCharaSets.SelectedIndices.Contains(indexA);
            Boolean indexBselected = lvCharaSets.SelectedIndices.Contains(indexB);

            // Suppress auto update
            lvCharaSets.BeginUpdate();

            // Unselect both items
            lvCharaSets.SelectedIndices.Remove(indexA);
            lvCharaSets.SelectedIndices.Remove(indexB);

            // Get them and their respective layer adjustment values
            CharaSetListViewItemAdapter csetB = (CharaSetListViewItemAdapter)lvCharaSets.Items[indexB];
            CharaSetListViewItemAdapter csetA = (CharaSetListViewItemAdapter)lvCharaSets.Items[indexA];
            Int32 adjA = csetA.CharaSet.LayerAdjustment;
            Int32 adjB = csetB.CharaSet.LayerAdjustment;

            // Remove both items (large index first!!)
            lvCharaSets.Items.RemoveAt(indexB);
            lvCharaSets.Items.RemoveAt(indexA);

            // Insert them into new positions and assign new layer adjustment values
            csetB.CharaSet.LayerAdjustment = adjA;
            lvCharaSets.Items.Insert(indexA, new CharaSetListViewItemAdapter(csetB.CharaSet));
            csetA.CharaSet.LayerAdjustment = adjB;
            lvCharaSets.Items.Insert(indexB, new CharaSetListViewItemAdapter(csetA.CharaSet));

            // Restore selection
            if (indexAselected) lvCharaSets.SelectedIndices.Add(indexB);
            if (indexBselected) lvCharaSets.SelectedIndices.Add(indexA);

            // Enable auto update
            lvCharaSets.EndUpdate();
        }

        /// <summary>
        /// Utility method for blocking preview from being updated
        /// </summary>
        public void BlockCharacterCreation()
        {
            blockCharaCreation = true;
        }
        /// <summary>
        /// Utility method for unblocking preview from being updated
        /// </summary>
        public void UnblockCharacterCreation()
        {
            blockCharaCreation = false;
        }

        #endregion

        #region Color Handling


        public void UpdateColorProcessorList()
        {
            using (CharaBlocker cb = new CharaBlocker(this))
            {
                resourceManager.UpdateColorProcessorList();

                // Update UI controls to reflect changes
                cmbColorProcessors.BeginUpdate();
                cmbColorProcessors.Items.Clear();
                cmbColorProcessors.Items.AddRange(resourceManager.Processors.Keys.ToArray());
                if (cmbColorProcessors.Items.Count > 0)
                    cmbColorProcessors.SelectedIndex = 0;
                cmbColorProcessors.EndUpdate();

                cb.DummyMethod();
            }
        }

        public void ApplyColorScheme()
        {
            // Save Processor
            ImageProcessor p = resourceManager.Processors[cmbColorProcessors.Text];

            p.HueMode = (ImageProcessor.AdjustmentMode)cmbColorHM.SelectedIndex;
            p.SaturationMode = (ImageProcessor.AdjustmentMode)cmbColorSM.SelectedIndex;
            p.LightnessMode = (ImageProcessor.AdjustmentMode)cmbColorLM.SelectedIndex;

            p.HA = tmpProcessor.HA;
            p.SA = tmpProcessor.SA;
            p.LA = tmpProcessor.LA;
            p.AA = tmpProcessor.AA;
            p.Step = tmpProcessor.Step;

            p.BaseColor = tmpProcessor.BaseColor;
            
            // Recache
            if (colorEditorActive)
                // pbPreview.Image = CreateCharacter();
                UpdateCharacterPreviewAsync();

            txtImProcInfo.Text = tmpProcessor.ToString();

            DebugHelper.DebugPrint("Color Scheme Applied");
        }           

        public void SyncTmpToUI()
        {
            using (CharaBlocker cb = new CharaBlocker(this))
            {
                chkColorHideSelection.Checked = tmpProcessor.AA == 0;
                lblColorBase.BackColor = tmpProcessor.BaseColor;
                cmbColorHM.SelectedIndex = (int)tmpProcessor.HueMode;
                cmbColorSM.SelectedIndex = (int)tmpProcessor.SaturationMode;
                cmbColorLM.SelectedIndex = (int)tmpProcessor.LightnessMode;

                tbColorHA.Value = (int)(tmpProcessor.HA * tbColorHA.Maximum);
                tbColorSA.Value = (int)(tmpProcessor.SA * (tbColorSA.Maximum / 2));
                tbColorLA.Value = (int)(tmpProcessor.LA * (tbColorLA.Maximum / 2));
                tbColorStep.Value = (int)(tmpProcessor.Step * tbColorStep.Maximum);

                txtImProcInfo.Text = tmpProcessor.ToString();
            }
        }

        private double GetRelation(ImageProcessor.AdjustmentMode mode, double original, double newval)
        {
            switch (mode)
            {
                case ImageProcessor.AdjustmentMode.None:
                    return 0;
                case ImageProcessor.AdjustmentMode.Multiplication:
                    return newval / original;
                case ImageProcessor.AdjustmentMode.Addition:
                    return newval - original;
                case ImageProcessor.AdjustmentMode.Absolute:
                    return newval;
            }
            throw new Exception();
        }


        #endregion

        #region UI utility Methods

        void InitializeLocale()
        {
            // Get Locale for convenience
            Localization.LocalizationDictionary d = 
                Localization.LocalizationDictionary.Instance;

            // Main Menu Strip
            miFile.Text = d["MMS_FILE"];
            miSelectCharacter.Text = d["MMS_SEL_CHAR"];
            miLoadCharacterResources.Text = d["MMS_LOAD_CHAR_RES"];
            miLoadCharacterState.Text = d["MMS_LOAD_CHAR_STATE"];
            miSaveCharacterState.Text = d["MMS_SAVE_CHAR_STATE"];
            miSaveCharacterImage.Text = d["MMS_SAVE_CHAR_IMG"];
            miAbout.Text = d["MMS_ABOUT"];
            miAboutMC.Text = d["MMS_ABOUT_MC"];
            miTools.Text = d["MMS_TOOL"];
            miCleanup.Text = d["MMS_TOOL_CLEANUP"];
            miVerify.Text = d["MMS_TOOL_VERIFY"];
            miAddParts.Text = d["MMS_TOOL_ADDPARTS"];
            miAddLegacy.Text = d["MMS_TOOL_ADDLEGACY"];

            // Chara Locked Strip
            lblCLTitle.Text = d["CL_TITLE"];
            lblCLHide.Text = d["CL_HIDE"];
            lblCLDescr.Text = d["CL_DESCR"];
             
            // Patch Strip
            lblCPTitle.Text = d["CP_TITLE"];
            lblCPDescr.Text = d["CP_DESCR"];
            lblCPHide.Text = d["CL_HIDE"];

            // Chara Tab
            tpageChara.Text = d["CT_TITLE"];
            gbCharaPartGroup.Text = d["CT_SET_GROUPS"];
            gbCharaPart.Text = d["CT_SETS"];
            lvcCharaPartName.Text = d["CT_LV_NAME"];
            lvcCharaPartFrames.Text = d["CT_LV_FRAMES"];
            lvcCharaPartLayerAdj.Text = d["CT_LV_LAYER_ADJ"];

            // Color Tab
            tpageColors.Text = d["CO_TITLE"];
            gbColorProcessors.Text = d["CO_PROCS"];
            gbColorSettings.Text = d["CO_SETTINGS"];
            lblColorBase.Text = d["CO_BASE_COL"];
            lblColorNew.Text = d["CO_NEW_COL"];
            lblColPresets.Text = d["CO_PRESETS"];
            lblColAdvOpts.Text = d["CO_ADV_OPTS"];
            chkColorHideSelection.Text = d["CO_HIDE_GROUP"];
            lblColHA.Text = d["CO_HA"];
            lblColSA.Text = d["CO_SA"];
            lblColLA.Text = d["CO_LA"];
            lblColStep.Text = d["CO_STEP"];
            lblColHM.Text = lblColSM.Text = lblColLM.Text = d["CO_MODE"];

            cmbColorHM.Items[0] = d["CO_M_MUL"];
            cmbColorHM.Items[1] = d["CO_M_ADD"];
            cmbColorHM.Items[2] = d["CO_M_ABS"];
            cmbColorHM.Items[3] = d["CO_M_NONE"];
            cmbColorSM.Items[0] = d["CO_M_MUL"];
            cmbColorSM.Items[1] = d["CO_M_ADD"];
            cmbColorSM.Items[2] = d["CO_M_ABS"];
            cmbColorSM.Items[3] = d["CO_M_NONE"];
            cmbColorLM.Items[0] = d["CO_M_MUL"];
            cmbColorLM.Items[1] = d["CO_M_ADD"];
            cmbColorLM.Items[2] = d["CO_M_ABS"];
            cmbColorLM.Items[3] = d["CO_M_NONE"];

            // Options Tab
            tpageOptions.Text = d["OP_TITLE"];
            gbOptLocale.Text = d["OP_LANG"];
            gbOptCE.Text = d["OP_CE"];
            chkOptionsShowImProcConfig.Text = d["OP_CE_SHOW_ADV_INFO"];
            lblOptShowAdvInfoD.Text = d["OP_CE_SHOW_ADV_INFO_D"];
            chkDimItems.Text = d["OP_CE_DIM_UNSEL"];
            lblOptDimUnselD.Text = d["OP_CE_DIM_UNSEL_D"];
        }

        void ChangeCharaSetSelection(ListViewItem i, Boolean check)
        {
            i.Checked = check;
            ((CharaSetListViewItemAdapter)i).CharaSet.Selected = check;
        }

        void ReloadResourceManager()
        {
            pbPreview.Width = resourceManager.Width;
            pbPreview.Height = resourceManager.Height;

            cmbCharaProp.Items.Clear();
            cmbCharaProp.Items.AddRange(resourceManager.ToArray());
            if (cmbCharaProp.Items.Count != 0)
                cmbCharaProp.SelectedIndex = 0;

            gbColorSettings.Enabled = resourceManager.AllowChange;
            pnlLockedNotification.Visible = !resourceManager.AllowChange;
            pnlPatchNotification.Visible = resourceManager.IsPatch;

            DebugHelper.DebugPrint("Resource Manager Reloaded");
        }

        void ReloadPropertyListView(CharaSetGroup setg)
        {
            using (CharaBlocker cb = new CharaBlocker(this))
            {
                lvCharaSets.BeginUpdate();
                // Get Group ID
                Int32 gid = cmbCharaProp.SelectedIndex;
                if (gid < 0)
                {
                    lvCharaSets.EndUpdate();
                    return;
                }

                // Clear property options
                lvCharaSets.Items.Clear();

                // If no CharaSetGroup selected, there is no need to continue reloading
                if (setg == null)
                {
                    lvCharaSets.EndUpdate();
                    return;
                }

                // Start updating listview
                List<CharaSet> orderedItems = new List<CharaSet>();
                orderedItems.AddRange(setg);
                if (setg.Multiselect)
                    orderedItems.Sort(new Comparison<CharaSet>(
                        (CharaSet a, CharaSet b) => { return b.LayerAdjustment.CompareTo(a.LayerAdjustment); }));
                
                foreach (CharaSet cset in orderedItems)
                    lvCharaSets.Items.Add(new CharaSetListViewItemAdapter(cset));
                
                lvCharaSets.EndUpdate();
                cb.DummyMethod();
            }

            DebugHelper.DebugPrint("UI Reloaded for {0}", setg.Name);
        }

        Double GetTrackBarValue(TrackBar tb, Double weight = 1)
        {
            return (tb.Value / (double)tb.Maximum * weight);
        }

        Int32 CalculateLayerAdjustment(CharaSetGroup group, Int32 index)
        {
            Int32 ladj = (group.Count - index) * resourceManager.Count + resourceManager.IndexOf(group);
            return ladj;
        }

        #endregion

        #region UI Event Handling (Menu Strip)

        private void Menu_SelectCharacterClicked(Object sender, EventArgs e)
        {
            ToolStripMenuItem mi = (ToolStripMenuItem)sender;
            String name = (String)mi.Tag;

            LoadCharacterResourcesGeneric(characters[name]);
            Properties.Settings.Default.Character = name;
            Properties.Settings.Default.Save();
        }

        private void Menu_AboutClicked(object sender, EventArgs e)
        {
            AboutDialog adlg = new AboutDialog();
            adlg.ShowDialog(this);
        }

        private void Menu_SaveCharacterStateClicked(object sender, EventArgs e)
        {
            if (!resourceManager.AllowChange)
            {
                MessageBox.Show(Localization.LocalizationDictionary.Instance["ERR_601"], 
                    Localization.LocalizationDictionary.Instance["ERR_000"], 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = String.Format("{0}|*.mcs|{1}|*.mcpak",
                Localization.LocalizationDictionary.Instance["EXT_MCS"],
                Localization.LocalizationDictionary.Instance["EXT_MCPAK"]);
            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
            {
                if (sfd.FilterIndex == 1)
                {
                    SaveCharacterToFile(sfd.FileName);
                }
                else if (sfd.FilterIndex == 2)
                {
                    PackerOptionsDialog optdlg = new PackerOptionsDialog(resourceManager);
                    optdlg.CharacterName = resourceManager.Name;
                    if (optdlg.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
                    {
                        CharacterResourcePacker packer = new CharacterResourcePacker(sfd.FileName, resourceManager);
                        packer.ImageProcessors = resourceManager.Processors;
                        packer.SaveState = optdlg.SaveState;
                        packer.LockCharacter = optdlg.LockFile;
                        packer.OmitUnusedResources = optdlg.OmitUnusedFiles;
                        packer.CharacterName = optdlg.CharacterName;
                        packer.PackResources();
                    }
                }
            }

            
        }

        private void Menu_LoadCharacterStateClicked(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = String.Format("{0}|*.mcs|{1}|*.mcpak",
                Localization.LocalizationDictionary.Instance["EXT_MCS"],
                Localization.LocalizationDictionary.Instance["EXT_MCPAK"]);
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
            {
                if (ofd.FilterIndex == 1)
                {
                    if (noResourceMode)
                    {
                        MessageBox.Show(Localization.LocalizationDictionary.Instance["ERR_702"], 
                            Localization.LocalizationDictionary.Instance["ERR_000"],
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }
                    if (!resourceManager.AllowChange)
                    {
                        MessageBox.Show(Localization.LocalizationDictionary.Instance["ERR_703"],
                            Localization.LocalizationDictionary.Instance["ERR_000"],
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    LoadCharacterFromFile(ofd.FileName);
                }
                else
                {
                    LoadCharacterResourcesAFS(ofd.FileName);
                }
            }
        }

        private void Menu_SaveCharacterImageClicked(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PNG Image|*.png";
            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
            {
                String path = sfd.FileName;
                pbPreview.Image.Save(path);
            }
        }

        private void Menu_LoadCharacterResourcesClicked(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = String.Format("{0}|*.mcres|{1}|*.mcpak{2}|character.ini",
                Localization.LocalizationDictionary.Instance["EXT_MCRES"],
                Localization.LocalizationDictionary.Instance["EXT_MCPAK"],
                Localization.LocalizationDictionary.Instance["EXT_CHARINI"]);
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
            {
                try
                {
                    LoadCharacterResourcesGeneric(ofd.FileName);

                    noResourceMode = false;
                    miLoadCharacterResources.Visible = false;
                    miSaveCharacterImage.Enabled = true;
                    miSaveCharacterState.Enabled = true;
                    splitContainer1.Enabled = true;

                    UpdateCharacterPreviewAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("{0}{1}",
                        Localization.LocalizationDictionary.Instance["ERR_704"],
                        ex.Message), 
                        Localization.LocalizationDictionary.Instance["ERR_000"]
                        , MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Menu_AddLegacyParts(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
            {
                Interop.CharaxDirHelper helper = new Interop.CharaxDirHelper(fbd.SelectedPath, resourceManager);
                CharacterResourceManager patchmgr;
                using (MemoryStream ms = new MemoryStream())
                {
                    helper.GenerateDescriptor(ms);
                    ms.Position = 0;
                    patchmgr = CharacterResourceManager.FromXML(ms);
                }
                patchmgr.FileSystemProxy = new DefaultFileSystemProxy(fbd.SelectedPath);
                resourceManager.Merge(patchmgr);

                ReloadResourceManager();
            }
        }

        private void Menu_AddMCPatch(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = String.Format("{0}|*.mcpak", Localization.LocalizationDictionary.Instance["EXT_MCPAK"]);
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
            {
                AFSFileSystemProxy proxy = new AFSFileSystemProxy(ofd.FileName);

                CharacterResourceManager patch;

                using (ExStream descriptor = new ExStream())
                {
                    PackageEntry entry = proxy.Archive.TryGetEntry("character.mcres");
                    proxy.Archive.Extract(entry, descriptor);
                    descriptor.Position = 0;

                    patch = CharacterResourceManager.FromXML(descriptor);
                }

                patch.FileSystemProxy = proxy;

                resourceManager.Merge(patch);

                ReloadResourceManager();
            }
        }

        #endregion

        #region UI Event Handling (Character Creator)

        // Another character property selected, reload list
        private void Character_SelectedSetGroupChanged(object sender, EventArgs e)
        {
            ReloadPropertyListView((CharaSetGroup)cmbCharaProp.SelectedItem);
        }

        // An item was checked, update selection and preview
        private void Character_SelectedSetChanged(object sender, ItemCheckedEventArgs e)
        {
            // In case if preview updates are blocked, stop here
            if (blockCharaCreation || !programInitialized) return;

            // Get current resource group
            CharaSetGroup setg = (CharaSetGroup)cmbCharaProp.SelectedItem;

            // Apply selection to the resource set
            ChangeCharaSetSelection(e.Item, e.Item.Checked);

            // if current resource group is a single selection group
            if (!setg.Multiselect)
            {
                // if the item was selected
                if (e.Item.Checked)
                {
                    if (lvCharaSets.CheckedItems.Count > 1)
                    {
                        // if this group is not multiselect and we have more than one item selected
                            // Clear all other selections
                        foreach (ListViewItem lvi in lvCharaSets.CheckedItems)
                            if (lvi != e.Item)
                                ChangeCharaSetSelection(lvi, false);

                    }
                }
                // if the item was deselected
                else if (lvCharaSets.CheckedItems.Count == 0)
                {
                    // if unchecking this item will result in zero selections, stop it from being deselected
                    ChangeCharaSetSelection(e.Item, true);
                }
            }

            // Make sure that preview is updated only once per change
            if (!e.Item.Checked || setg.Multiselect)
            {
                UpdateColorProcessorList();
                UpdateCharacterPreviewAsync();
            }

        }

        // An item is about to be checked, change blocks take effect here
        private void Character_SelectedSetChanging(object sender, ItemCheckEventArgs e)
        {
            if (!resourceManager.AllowChange)
                e.NewValue = e.CurrentValue;
        }

        // Character part Set listview selection (not check state!!) changed
        private void Character_SetListSelectionChanged(object sender, EventArgs e)
        {
            CharaSetGroup setg = (CharaSetGroup)cmbCharaProp.SelectedItem;

            if (!setg.Multiselect)
            {
                if (lvCharaSets.SelectedIndices.Count > 0)
                    lvCharaSets.Items[lvCharaSets.SelectedIndices[0]].Checked = true;
            }
        }

        private void CharacterMenu_CharaSetMoveUp(object sender, EventArgs e)
        {
            if (lvCharaSets.SelectedIndices.Count == 0) return;
            
            Int32 selIndex = lvCharaSets.SelectedIndices[0];

            SwapCharacterSetListItems(selIndex, selIndex - 1);

        }

        private void CharacterMenu_CharaSetMenuOpening(object sender, CancelEventArgs e)
        {
            CharaSetGroup setg = (CharaSetGroup)cmbCharaProp.SelectedItem;

            if (!setg.Multiselect)
                e.Cancel = true;
        }

        private void LockedNotificationHideLabelClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            pnlLockedNotification.Visible = false;
        }

        private void PatchNotificationHideLabelClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            pnlPatchNotification.Visible = false;
        }

        #endregion

        #region UI Event Handling (Color Editor)

        // Selected Image Processor Changed
        private void ColorEditor_SelectedColorProcessorChanged(object sender, EventArgs e)
        {
            ImageProcessor processor = resourceManager.Processors[cmbColorProcessors.Text];

            tmpProcessor.HueMode = processor.HueMode;
            tmpProcessor.SaturationMode = processor.SaturationMode;
            tmpProcessor.LightnessMode = processor.LightnessMode;

            tmpProcessor.HA = processor.HA;
            tmpProcessor.SA = processor.SA;
            tmpProcessor.LA = processor.LA;
            tmpProcessor.AA = processor.AA;
            tmpProcessor.Step = processor.Step;

            tmpProcessor.BaseColor = processor.BaseColor;

            cmbColorProcessorPresets.Items.Clear();
            cmbColorProcessorPresets.Items.AddRange(resourceManager.GetPresets(cmbColorProcessors.Text).ToArray());

            SyncTmpToUI();
            if (colorEditorActive)
                UpdateCharacterPreviewAsync();
        }

        // Selected Color Preset was changed
        private void ColorEditor_SelectedPresetChanged(object sender, EventArgs e)
        {
            ColorPreset preset = resourceManager.GetPresets(cmbColorProcessors.Text).FirstOrDefault(
                new Func<ColorPreset, bool>((ColorPreset s) => { return s.Name == cmbColorProcessorPresets.Text; }));

            if (preset == null) return;

            tmpProcessor.DecodeSettings(preset.Preset);

            SyncTmpToUI();
            if (colorEditorActive)
                ApplyColorScheme();
        }
        
        // New Color Label clicked, calculate optimal params and update
        private void ColorEditor_RequestSetNewColor(object sender, EventArgs e)
        {
            ColorDialog cdial = new ColorDialog();
            DialogResult dr = cdial.ShowDialog();

            if (dr == System.Windows.Forms.DialogResult.Cancel) return;
           
            ImageProcessor p = resourceManager.Processors[cmbColorProcessors.Text];
            ColorRGB exc = new ColorRGB(cdial.Color);

            tmpProcessor.HA = GetRelation(tmpProcessor.HueMode, tmpProcessor.BaseColor.H, exc.H);
            tmpProcessor.SA = GetRelation(tmpProcessor.SaturationMode, tmpProcessor.BaseColor.S, exc.S);
            tmpProcessor.LA = GetRelation(tmpProcessor.LightnessMode, tmpProcessor.BaseColor.L, exc.L);

            SyncTmpToUI();
            ApplyColorScheme();
        }

        // track bar value changed, update UI
        private void ColorEditor_TrackBarValueChanged(object sender, EventArgs e)
        {
            if (sender == tbColorHA)
            {
                lblColorHA.Text = Math.Round(GetTrackBarValue(tbColorHA), 2).ToString();
                tmpProcessor.HA = GetTrackBarValue(tbColorHA);
            }
            else if (sender == tbColorSA)
            {
                lblColorSA.Text = Math.Round(GetTrackBarValue(tbColorSA, 2), 2).ToString();
                tmpProcessor.SA = GetTrackBarValue(tbColorSA, 2);
            }
            else if (sender == tbColorLA)
            {
                lblColorLA.Text = Math.Round(GetTrackBarValue(tbColorLA, 2), 2).ToString();
                tmpProcessor.LA = GetTrackBarValue(tbColorLA, 2);
            }
            else if (sender == tbColorStep)
            {
                tmpProcessor.Step = GetTrackBarValue(tbColorStep);
                lblColorStep.Text = Math.Round(tmpProcessor.Step, 2).ToString();
            }

            lblColorNew.BackColor = tmpProcessor.TransformColor(tmpProcessor.BaseColor);
        }
        
        // adjustment mode changed, update UI
        private void ColorEditor_AdjustmentModeChanged(object sender, EventArgs e)
        {
            if (sender == cmbColorHM)
            {
                tmpProcessor.HueMode = (ImageProcessor.AdjustmentMode)cmbColorHM.SelectedIndex;
                lblColorNew.BackColor = tmpProcessor.TransformColor(tmpProcessor.BaseColor);
            }
            else if (sender == cmbColorSM)
            {
                tmpProcessor.SaturationMode = (ImageProcessor.AdjustmentMode)cmbColorSM.SelectedIndex;
                lblColorNew.BackColor = tmpProcessor.TransformColor(tmpProcessor.BaseColor);
            }
            else if (sender == cmbColorLM)
            {
                tmpProcessor.LightnessMode = (ImageProcessor.AdjustmentMode)cmbColorLM.SelectedIndex;
                lblColorNew.BackColor = tmpProcessor.TransformColor(tmpProcessor.BaseColor);
            }

            if (colorEditorActive)
                ApplyColorScheme();
        }

        // Some track bar done dragging, update preview
        private void ColorEditor_TrackBarDoneDragging(object sender, MouseEventArgs e)
        {
            if (colorEditorActive)
                ApplyColorScheme();
        }

        // AA value changed, update UI and preview
        private void ColorEditor_HideGroupChanged(object sender, EventArgs e)
        {
            if (chkColorHideSelection.Checked)
                tmpProcessor.AA = 0;
            else
                tmpProcessor.AA = 1;

            if (colorEditorActive)
                ApplyColorScheme();
        }

        // Click on preview box
        private void ColorEditor_PreviewBoxClicked(object sender, MouseEventArgs e)
        {
            if (!colorEditorActive) return;

            Double scaleX = resourceManager.Width / (double)pbPreview.Width;
            Double scaleY = resourceManager.Height / (double)pbPreview.Height;

            Int32 imgX = (int)(Math.Round(e.X * scaleX));
            Int32 imgY = (int)(Math.Round(e.Y * scaleY));

            Color bpixel = ((Bitmap)pbPreview.Image).GetPixel(imgX, imgY);

            if (bpixel.A == 0) return;

            tmpProcessor.BaseColor = new ColorRGB(bpixel);

            SyncTmpToUI();
        }

        // Tab page selection changed
        private void ColorEditor_TabPageSelectionChanged(object sender, TabControlEventArgs e)
        {
            if (e.TabPage == tpageColors)
            {
                if (e.Action == TabControlAction.Selected)
                {
                    colorEditorActive = true;
                    lblColorBase.Visible = true;
                    lblColorNew.Visible = true;

                }
                else if (e.Action == TabControlAction.Deselected)
                {
                    colorEditorActive = false;
                    lblColorBase.Visible = false;
                    lblColorNew.Visible = false;

                    dimCache.Clear();
                }


                pnlImProcInfo.Visible = chkOptionsShowImProcConfig.Checked && colorEditorActive;
                UpdateCharacterPreviewAsync();
            }
        }

        #endregion

        #region UI Event Handling (Options)

        private void Options_ShowColorProcessorConfigChanged(object sender, EventArgs e)
        {
            if (colorEditorActive)
            {
                pnlImProcInfo.Visible = chkOptionsShowImProcConfig.Checked;
            }
            Properties.Settings.Default.CEShowProcConfig = chkOptionsShowImProcConfig.Checked;
            Properties.Settings.Default.Save();
        }
  
        private void Options_DimUnselectedItemsChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.CEDimUnselected = chkDimItems.Checked;
            Properties.Settings.Default.Save();
        }

        private void Options_LocaleChanged(object sender, EventArgs e)
        {
            Locale loc = (Locale)cmbLocales.SelectedItem;
            Localization.LocalizationDictionary.Instance.ChangeLocale(loc.ID);
            Properties.Settings.Default.Locale = loc.ID;
            Properties.Settings.Default.Save();
        }

        #endregion

        private void tryLoadMCUToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunMCU runner = new RunMCU(new String[]
                {
                    "mcu DEPLOY"
                }, true);
            runner.ShowDialog();
        }

        private void addNewPartsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = String.Format("{0}|*.mcpak", LocalizationDictionary.Instance["EXT_MCPAK"]);
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
            {
                String command =
                    String.Format("mcu DEPLOY /update \"{0}\" \"{1}\"", ofd.FileName, resourceManager.FileSystemProxy.LoadedPath);
                String cleanup  = 
                    String.Format("mcu CLEANUP /noconfirm \"{0}\"", resourceManager.FileSystemProxy.LoadedPath);


                RunMCU runner = new RunMCU(new String[] { command, cleanup }, false);
                runner.ShowDialog();

                LoadCharacterResourcesGeneric(resourceManager.FileSystemProxy.LoadedPath);
            }
        }

        private void verifyCurrentlySelectedCHaracterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String command = String.Format("mcu VERIFY /deletemissing /noconfirm \"{0}\"", resourceManager.FileSystemProxy.LoadedPath);
            RunMCU runner = new RunMCU(new String[] { command }, false);
            runner.ShowDialog();
        }

        private void cleanupCurrentCharacterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String command = String.Format("mcu CLEANUP /noconfirm \"{0}\"", resourceManager.FileSystemProxy.LoadedPath);
            RunMCU runner = new RunMCU(new String[] { command }, false);
            runner.ShowDialog();
        }




    }
}
