/*
 *  TADS3 documentation generator.
 * 
 *  Copyright Edward L. Stauff 2003.  
 *  This code may be freely distributed and modified for non-commercial purposes. 
 *  
 */
using System;
using System.Collections;
using System.IO;

namespace DocGen
{
    /// <summary>
    /// Top-level class for the application.
    /// </summary>
    class DocGenApp
    {
        /// <summary>
        /// The symbol table.
        /// </summary>
        static SymbolTable  SymbolTable = new SymbolTable();

        /// <summary>
        /// List of input paths (String) from the command line.
        /// </summary>
        static ArrayList    InputPaths = new ArrayList();

        /// <summary>
        /// The directory in which to place the output files.
        /// </summary>
        static String       OutputPath = null;

        /// <summary>
        /// The directory where we find the macro-expanded source files
        /// </summary>
        static public String       MacroExpPath = null;

        /// <summary>
        /// The TADS version number (from the command line).
        /// </summary>
        static String       TadsVersion = null;

        /// <summary>
        /// The name of the dump file (for debugging), or null.
        /// </summary>
        static String       DumpFileName = null;

        /// <summary>
        /// The name of the intro file.
        /// </summary>
        static String       IntroFileName;

        static Preprocessor Preprocessor = new Preprocessor();

        //=====================================================================
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            ProcessInputArgs(args);

            if (OutputPath == null)
                throw new Exception("no output path specified; use -h for help");
            if (InputPaths.Count == 0)
                throw new Exception("no inputs specified; use -h for help");
            if (IntroFileName == null)
                throw new Exception("no intro file specified; use -h for help");

            foreach (String s in InputPaths)
                ProcessOneInputPath(s);

            Preprocessor.Preprocess();

            SymbolTable.Files = Preprocessor.SortedFiles;
            Parser parser = new Parser(SymbolTable);
            parser.ParseFiles(Preprocessor.SortedFiles);

            SymbolTable.PostProcess();

            if (DumpFileName != null)
                SymbolTable.Dump(DumpFileName);
            System.Console.Out.WriteLine("generating output");
            HtmlGenerator hgen = new HtmlGenerator(SymbolTable, IntroFileName, TadsVersion, OutputPath);
            hgen.WriteAll();
        }

        //=====================================================================

        static String Help =
            "TADS3 documentation generator, version 0.3 by Edward L. Stauff\r\n" +
            "Switches:\r\n" +
            "-i <file>     the name of the introduction file\r\n" +
            "-m <dir>      directory path to macro-expanded files\r\n" +
            "-o <dir>      the name of the output directory\r\n" +
            "-v <version>  the TADS version\r\n" +
            "-vf <file>    get the TADS version from the given file\r\n" +
            "-d <file>     where to dump the symbol table (for debugging)\r\n" +
            "-h or -?      display this help message\r\n" +
            "All non-switch parameters are input paths, each of which can be a single\r\n" +
            "file, a directory, or a path containing wildcards.\r\n"
        ;

        //=====================================================================
        /// <summary>
        /// Processes input arguments from the command line.
        /// </summary>
        static void ProcessInputArgs (string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))    // is it a switch?
                {
                    switch (args[i])
                    {
                        case "-i":
                            if (++i >= args.Length)
                                throw new Exception("expecting file name after -i");
                            IntroFileName = args[i];
                            break;
                        case "-d":
                            if (++i >= args.Length)
                                throw new Exception("expecting file name after -d");
                            DumpFileName = args[i];
                            break;
                        case "-m":
                            if (++i >= args.Length)
                                throw new Exception("expecting macro-expanded directory after -m");
                            MacroExpPath = args[i];
                            break;
                        case "-o":
                            if (++i >= args.Length)
                                throw new Exception("expecting directory name after -o");
                            OutputPath = args[i];
                            break;
                        case "-v":
                            if (++i >= args.Length)
                                throw new Exception("expecting version number after -v");
                            TadsVersion = args[i];
                            break;
                        case "-vf":
                            if (++i >= args.Length)
                                throw new Exception("expecting version file after -vf");
                            {
                                StreamReader vf = new StreamReader(args[i]);
                                String l = vf.ReadLine();
                                TadsVersion = l.Trim();
                            }
                            break;
                        case "-h":
                        case "-H":
                        case "-?":
                            System.Console.Out.Write(Help);
                            Environment.Exit(0);
                            break;
                        default:
                            throw new Exception("bogus switch: " + args[i]);
                    }
                }
                else InputPaths.Add(args[i]);
            }
        }

        //=====================================================================
        /// <summary>
        /// Processes a single input file.
        /// </summary>
        static void ProcessOneFile (String fileName)
        {
            Preprocessor.TakeFile(fileName);
        }

        //=====================================================================
        /// <summary>
        /// Processes a single input path (which may contain wildcards).
        /// </summary>
        static void ProcessOneInputPath (String s)
        {
            if (s.IndexOfAny("*?".ToCharArray()) >= 0)
                ProcessWildCardPath(s);
            else if ((File.GetAttributes(s) & FileAttributes.Directory) != 0)
                ProcessDirectory(s);
            else ProcessOneFile(s);
        }

        //=====================================================================
        /// <summary>
        /// Processes a single input directory.
        /// </summary>
        static void ProcessDirectory (String s)
        {
            DirectoryInfo di = new DirectoryInfo(s);
            FileInfo[] files = di.GetFiles();

            foreach (FileInfo file in files)
                ProcessOneFile(di.FullName + "\\" + file.Name);
        }

        //=====================================================================
        /// <summary>
        /// Process a single input path containing wildcards.
        /// </summary>
        static void ProcessWildCardPath (String path)
        {
            String dir = Path.GetDirectoryName(path);
            String file = Path.GetFileName(path);
            string[] fileNames = Directory.GetFiles(dir, file);
            foreach (String s in fileNames)
                ProcessOneFile(s);
        }

        //=====================================================================
    }

}
