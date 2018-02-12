using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleSourceProtector
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Dictionary<string, string> arguments = new Dictionary<string, string>();
            int i = 0;
            int a = 0;
            while (i < args.Length)
            {
                if (args[i].StartsWith("-"))
                {
                    switch (args[i])
                    {
                        case "-h":
                            {
                                HelpMsg();
                                break;
                            }

                        case "-c":
                            {
                                arguments.Add("cc", "true");
                                break;
                            }

                        case "-n":
                            {
                                arguments.Add("nr", "true");
                                break;
                            }

                        case "-m":
                            {
                                arguments.Add("m", "true");
                                break;
                            }
                        default:
                            {
                                HelpMsg("Option '" + args[i] + "' not recognized.");
                                break;
                            }
                    }
                }
                else
                {
                    arguments.Add("arg" + a++, args[i]);
                }
                i++;
            }

            if (arguments.Count <= 0)
                HelpMsg("The required parameter is missing: project");
            if (!File.Exists(arguments["arg0"]))
                HelpMsg("Project file not found...");


            string projectPath = arguments["arg0"];
            string outputPath = (arguments.ContainsKey("arg1")) ? arguments["arg1"] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");

            bool checkCompilation = (arguments.ContainsKey("cc")) ? true : false;
            bool notrename = (arguments.ContainsKey("nr")) ? true : false;
            bool multiple = (arguments.ContainsKey("m")) ? true : false;


            Console.WriteLine($"Start processing project: {projectPath}");
            Console.WriteLine($"Output: {outputPath}");
            Console.WriteLine($"Options: check compilation = {checkCompilation}, do not rename = {notrename}, do multiple = {multiple}");

            int resCode = 0;
            using (Watcher.Start(ts => Console.WriteLine("All Timed: " + ts.ToString())))
            {
                var loader = new Loader();
                resCode = loader.Process(projectPath, outputPath, checkCompilation, !notrename, !multiple);
            }
            Environment.Exit(resCode);
        }

        static void HelpMsg(string arg = "")
        {
            Console.WriteLine(@"Usage: SimpleSourceProtector [Options] ""FullFilePath\Project.cproj"" [output]
  * if the output parameter is not specified, a ""output"" folder will be created inside the program directory
Options:
    -h      show this page
    -n      do not rename class, methods, vars and etc.
    -c      do a check compilation before renaming
    -m      make multiple files, not a single");
            if (!string.IsNullOrEmpty(arg))
                Console.WriteLine("\n" + arg);

            Environment.Exit(1);
        }
    }
}
