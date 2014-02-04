using System;
using System.IO;
using MoltenMercury.Interop;

namespace MCU
{
	partial class Program
	{
        static int Legacy(CommandLineArguments cmdargs)
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
                Console.WriteLine(Resources.LegacyHelp);
                return mcInteropMode ? 903 : 0; // MC Interop Mode does not support /? flag and treats it as an error
            }

            // Define arg variables
            Boolean ini = true;
            Int32 width = -1;
            Int32 height = -1;
            String charname = null;
            String dir = null;

            #region Arg Validation
            // Check all args
            for (int i = 1; i < cmdargs.Count; i++)
            {
                if (cmdargs[i].Type == CommandLineArgument.ArgumentType.Argument)
                    dir = cmdargs[i].Name;
                else
                {
                    switch (cmdargs[i].Name.ToLower())
                    {
                        case "ini":
                            {
                                Boolean tmp;

                                if (cmdargs[i].Arguments.Length == 0
                                    || !Boolean.TryParse(cmdargs[i].Arguments[0], out tmp))
                                {
                                    WriteMessage(MessageType.Error, "Error 905: /ini must have a boolean argument!\n");
                                    return 905;
                                }

                                ini = tmp;
                            }
                            break;
                        case "width":
                            {
                                Int32 tmp;

                                if (cmdargs[i].Arguments.Length == 0
                                    || !Int32.TryParse(cmdargs[i].Arguments[0], out tmp))
                                {
                                    WriteMessage(MessageType.Error, "Error 905: /width must have an integer argument!\n");
                                    return 905;
                                }

                                width = tmp;
                            }
                            break;
                        case "height":
                            {
                                Int32 tmp;

                                if (cmdargs[i].Arguments.Length == 0
                                    || !Int32.TryParse(cmdargs[i].Arguments[0], out tmp))
                                {
                                    WriteMessage(MessageType.Error, "Error 905: /height must have an integer argument!\n");
                                    return 905;
                                }

                                height = tmp;
                            }
                            break;
                        case "charname":
                            {
                                if (cmdargs[i].Arguments.Length == 0)
                                {
                                    WriteMessage(MessageType.Error, "Error 905: /charaname must have a string argument!\n");
                                    return 905;
                                }

                                charname = cmdargs[i].Arguments[0];
                            }
                            break;
                    }
                }
            }
            #endregion

            // Validate Input
            if (ini && Path.GetFileName(dir) == "character.ini")
            {
                dir = Path.GetDirectoryName(dir);
            }
            if (!ini && (height < 0 || width < 0))
            {
                WriteMessage(MessageType.Error, "Error 906: When /ini:false, /height and /width must be specified\n");
                return 906;
            }
            if (dir == null)
            {
                WriteMessage(MessageType.Error, "Error 906: Directory to load was not specified!\n");
                return 907;
            }
            if (!Directory.Exists(dir) || (ini && !File.Exists(Path.Combine(dir, "character.ini"))))
            {
                WriteMessage(MessageType.Error, "Error 906: Specified directory (or character.ini) does not exist!\n");
                return 909;
            }

            // Start working on the task
            CharaxDirHelper dirHelper = null;
            if (ini)
                dirHelper = new CharaxDirHelper(Path.Combine(dir, "character.ini"));
            else
                dirHelper = new CharaxDirHelper(dir, width, height);

            // Set charname if it was specified
            if (charname != null)
                dirHelper.CharaName = charname;

            // Write the descriptor
            dirHelper.GenerateDescriptor(Path.Combine(dir, "character.mcres"));

            // Report success
            WriteMessage(MessageType.Routine, "Success!\n");
            return 0;
        }

	}
}
