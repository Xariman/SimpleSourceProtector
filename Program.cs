using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleSourceProtector
{
    internal static class Program
    {
        [Flags]
        public enum ObfuscationOptions
        {
            NONE = 0,
            CLASS = 1,
            METHODS = 1 << 1,
            VARS = 1 << 2,
            OTHERS = 1 << 3,
            FILENAMES = 1 << 4,
            ALL = CLASS | METHODS | VARS | OTHERS | FILENAMES
        };

        static void Main(string[] args)
        {
            HashSet<string> m_allowed = new HashSet<string> { "h", "c", "n", "m", "dm", "rc", "rm", "rv", "ro", "rf" };
            Dictionary<string, string> arguments = new Dictionary<string, string>();
            int i = 0;
            int a = 0;
            while (i < args.Length)
            {
                if (args[i].StartsWith("-"))
                {
                    var option = args[i].Substring(1);
                    if (m_allowed.Contains(option))
                        arguments.Add(option, "true");
                    else

                        HelpMsg("Option '" + args[i] + "' not recognized.");
                }
                else
                    arguments.Add("arg" + a++, args[i]);
                i++;
            }

            if (arguments.Count <= 0 || !arguments.ContainsKey("arg0"))
                HelpMsg("The required parameter is missing: project");
            if (!File.Exists(arguments["arg0"]))
                HelpMsg("Project file not found...");


            string projectPath = arguments["arg0"];
            string outputPath = (arguments.ContainsKey("arg1")) ? arguments["arg1"] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");

            bool checkCompilation = (arguments.ContainsKey("c")) ? true : false;
            bool single = (arguments.ContainsKey("m")) ? false : true;
            bool minify = (arguments.ContainsKey("dm")) ? false : true;

            ObfuscationOptions obfuscationOptions = (arguments.ContainsKey("n")) ? ObfuscationOptions.NONE : ObfuscationOptions.ALL;
            if (!arguments.ContainsKey("n") && (arguments.ContainsKey("rc") || arguments.ContainsKey("rm") || arguments.ContainsKey("rv") || arguments.ContainsKey("ro") || arguments.ContainsKey("rf")))
            {
                obfuscationOptions = ObfuscationOptions.NONE;
                if (arguments.ContainsKey("rc"))
                    obfuscationOptions |= ObfuscationOptions.CLASS;
                if (arguments.ContainsKey("rm"))
                    obfuscationOptions |= ObfuscationOptions.METHODS;
                if (arguments.ContainsKey("rv"))
                    obfuscationOptions |= ObfuscationOptions.VARS;
                if (arguments.ContainsKey("ro"))
                    obfuscationOptions |= ObfuscationOptions.OTHERS;
                if (arguments.ContainsKey("rf"))
                    obfuscationOptions |= ObfuscationOptions.FILENAMES;
            }

            Console.WriteLine($"Start processing project: {projectPath}");
            Console.WriteLine($"Output: {outputPath}");
            Console.WriteLine($"Options: check compilation = {checkCompilation}, do multiple = {!single}, minify = {minify}, obfuscate options = {obfuscationOptions.ToString()}");

            int resCode = 0;
            using (Watcher.Start(ts => Console.WriteLine("All Timed: " + ts.ToString())))
            {
                var loader = new Loader();
                resCode = loader.Process(projectPath, outputPath, checkCompilation, obfuscationOptions, single, minify);
            }
            Environment.Exit(resCode);
        }

        static void HelpMsg(string arg = "")
        {
            Console.WriteLine(@"Usage: SimpleSourceProtector [Options] ""FullFilePath\Project.cproj"" [output]
  * if the output parameter is not specified, a ""output"" folder will be created inside the program directory
Options:
    -h      show this page
    -c      do a check compilation before renaming
    -m      make multiple files, not a single
    -dm     do not minify
    -n      do not rename class, methods, vars and etc.
  * if -n is not used, then the flags below can be combined:
    -rc     rename classes
    -rm     rename methods
    -rv     rename vars
    -ro     rename others
    -rf     remame filenames");
            if (!string.IsNullOrEmpty(arg))
                Console.WriteLine("\n" + arg);

            Environment.Exit(1);
        }
    }
}
