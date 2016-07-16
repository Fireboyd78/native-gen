using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;
using NDesk.Options;

/*
    Huge shoutout goes to JohnnyCrazy for writing most of the original code
    Without his work, this wouldn't have been possible!
    (https://github.com/JohnnyCrazy/scripthookvdotnet/blob/native-generator/helpers/NativeGenerator/NativeGenerator.cs)
*/
namespace NativeGenerator
{
    public static class Options
    {
        public static int verbosity { get; set; } = 0;
        private static String _lookupFile = "";
        public static String lookupFile
        {
            get { return _lookupFile; }
            set { 
                if (File.Exists(value)) {
                    _lookupFile = value; 
                } else {
                    throw new Exception("lookup file does not exist");
                }
            }
        }
    }

    class Program
    {
        private static Json.NativeFile DownloadNativesFile(string url)
        {
            using (var wc = new WebClient())
            {
                wc.Headers.Add("Accept-Encoding: gzip, deflate, sdch");

                var data = wc.DownloadData(url);
                var rawData = "{}";

                using (var ms = new MemoryStream())
                using (var gz = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
                {
                    var decompress = new byte[data.Length];
                    var count = 0;

                    while ((count = gz.Read(decompress, 0, decompress.Length)) > 0)
                        ms.Write(decompress, 0, count);

                    rawData = Encoding.UTF8.GetString(ms.ToArray());
                }

                return JsonConvert.DeserializeObject<Json.NativeFile>(rawData);
            }
        }

        static void listBuilds()
        {
            int[] buildList = {
                331,
                350,
                372,
                393,
                463,
                505,
                573,
                617,
                678,
                757,
                791 
            };
            Console.Write(System.AppDomain.CurrentDomain.FriendlyName + ": ");
            Console.WriteLine("Available builds for use with --build");
            foreach (var build in buildList)
            {
                Console.WriteLine("   {0}", build);
            }
        }

        static void Main(string[] args)
        {
            // this is temporary since I just barely went open source
            // eventually the program will require an input file

            // Lets get un-temporary! -- sfinktah

            /* Command Line Handling */
            IDAScriptWriterBase scriptWriter = null;
            scriptWriter = new IDAScriptWriter(); // Will change to IDAPythonWriter if --python option is present
            bool show_help = false;
            bool will_exit = false;
            List<string> names = new List<string>();
            int repeat = 1;

            var p = new OptionSet() {
                { "p|python", "output python script (default: idc)", v => { if (v != null) scriptWriter = new IDAPythonWriter(); } },
                { "b|build=", "GTA5 build number, see --build-help", (String v) =>
                {
                    Console.WriteLine("Selected build {0}", v);
                } },
                { "l|lookup=", "Path to file with hash/offsets", v => Options.lookupFile = v },
                { "build-help", "list available GTA5 builds", v => { if (v != null) listBuilds(); will_exit = true; }},
//              { "r|repeat=", "the number of {TIMES} to repeat the greeting.\n" +
//                      "this must be an integer.", (int v)  => repeat = v },
                { "v", "increase debug message verbosity", v => { if (v != null) ++Options.verbosity; ; } },
                { "h|help",  "show this message and exit", v => show_help = v != null },
            };

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write(System.AppDomain.CurrentDomain.FriendlyName + ": ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", System.AppDomain.CurrentDomain.FriendlyName);
                return;
            }

            if (show_help || will_exit)
            {
                if (show_help) ShowHelp(p);
                return;
            }

            string message;
            if (extra.Count > 0)
            {
                message = string.Join(" ", extra.ToArray());
                Debug("Using new message: {0}", message);
            }
            else
            {
                message = "Hello {0}!";
                Debug("Using default message: {0}", message);
            }

            foreach (string name in names)
            {
                for (int i = 0; i < repeat; ++i)
                    Console.WriteLine(message, name);
            }


            // original code resumes -- sfinktah
            var lookupDir = @".";
            var lookupFile = $@"{lookupDir}\nativeDumpFile.bin";

            if (Options.lookupFile.Length < 1 || !File.Exists(Options.lookupFile))
            {
                Console.WriteLine("Dump file not found, terminating...");
                return;
            }

            Console.WriteLine("Downloading natives.json");

            // var nativesFile = DownloadNativesFile("http://www.dev-c.com/nativedb/natives.json");
            var rawData = File.ReadAllText($@"{lookupDir}\natives-b757.tidy.json");
            var nativesFile = JsonConvert.DeserializeObject<Json.NativeFile>(rawData);
            var sb = new StringBuilder();

            if (nativesFile == null)
            {
                Console.WriteLine("Failed to download natives.json! Terminating...");
                return;
            }

            var nativeDump = NativeDumpFile.Open(lookupFile);

            var parsedNatives = 0;
            var foundNatives = 0;

            foreach (var nativeNamespace in nativesFile.Keys)
            {
                Console.WriteLine("Processing namespace: {0}", nativeNamespace);

                var natives = nativesFile[nativeNamespace];

                foreach (var nativeHash in natives.Keys)
                {
                    var native = natives[nativeHash];
                    var info = nativeDump[long.Parse(nativeHash.Substring(2), NumberStyles.HexNumber)];

                    if (info != null)
                    {
                        var name = (!String.IsNullOrEmpty(native.Name)) ? native.Name : nativeHash.Substring(2);
                        info.Name = $"{nativeNamespace}__{name}";

                        sb.AppendLine($"{nativeNamespace}::{name} @ 0x{info.FunctionOffset:X} // 0x{info.Hash:X} {native.JHash}");
                        foundNatives++;
                    }

                    parsedNatives++;
                }
            }

            Console.WriteLine($"Finished parsing {parsedNatives} natives. Found {foundNatives} / {nativeDump.Natives.Count} natives that matched the dump file.");
            File.WriteAllText($@"{lookupDir}\native_gen.log", sb.ToString());

            Console.WriteLine("Creating script");


            // TODO: make proper argument parser
            if (args.Contains("--py"))
                scriptWriter = new IDAPythonWriter();
            else
                scriptWriter = new IDAScriptWriter();

            scriptWriter.WritePreamble($"This file was automatically generated by NativeGenerator {Assembly.GetExecutingAssembly().GetName().Version}");
            scriptWriter.OpenMainBlock();

            var useLower = args.Contains("--lc");

            foreach (var native in nativeDump.Natives)
            {
                var name = (useLower) ? native.Name.ToLower() : native.Name;

                scriptWriter.WriteMethodCall("MakeName", $"0x{native.FunctionOffset:X}", $"\"{name}\"");
                scriptWriter.WriteComment($"{native.Hash:X}");
                scriptWriter.WriteLine();
            }

            scriptWriter.CloseMainBlock();
            scriptWriter.SaveFile(lookupDir, "native_gen");

            Console.WriteLine("Operation completed.");

            if (System.Diagnostics.Debugger.IsAttached)
                Console.ReadKey();
        }


        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: {0} [OPTIONS] more-stuff-todo", System.AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("Generates IDA scripts for identifying GTAV Native Functions.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        static void Debug(string format, params object[] args)
        {
            if (Options.verbosity > 0)
            {
                Console.Write("# ");
                Console.WriteLine(format, args);
            }
        }
    }
}
