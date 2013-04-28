using System;
using System.Collections;
using System.IO;

namespace DocGen
{
    /// <summary>
    /// Represents a single source file.
    /// </summary>
    public class SourceFile : IComparable
    {
        /// <summary>
        /// The name of the file (without directory).
        /// </summary>
        public String       ShortName;

        /// <summary>
        /// The full pathname of the file.
        /// </summary>
        public String       FullPath;

        /// <summary>
        /// The topmost comment in the file.
        /// </summary>
        public String       Description;

        /// <summary>
        /// Is the source file a header (.h) file?
        /// </summary>
        public bool         IsHeaderFile = false;

        /// <summary>
        /// The list of #include files (SourceFile) that this file explicitly includes.
        /// </summary>
        public ArrayList    IncludeFiles = new ArrayList();

        /// <summary>
        /// All the classes (ClassDef) in this source file.
        /// </summary>
        public ArrayList    Classes = new ArrayList();

        /// <summary>
        /// All the macros (MacroDef) in this source file.
        /// </summary>
        public ArrayList    Macros = new ArrayList();
        
        /// <summary>
        /// All the enums (EnumDef) in this source file.
        /// </summary>
        public ArrayList    Enums = new ArrayList();
        
        /// <summary>
        /// All the enum groups (EnumGroup) in this source file.
        /// </summary>
        public ArrayList    EnumGroups = new ArrayList();
        
        /// <summary>
        /// All the templates (TemplateDef) in this source file.
        /// </summary>
        public ArrayList    Templates = new ArrayList();

        /// <summary>
        /// All the global objects (ObjectDef) in this source file.
        /// </summary>
        public ArrayList GlobalObjects = new ArrayList();

        /// <summary>
        /// All the global functions (FunctionDef) in this source file.
        /// </summary>
        public ArrayList GlobalFunctions = new ArrayList();

        /// <summary>
        /// Used by the preprocessor to determine the order in which
        /// the files should be parsed.
        /// </summary>
        public bool     SortFlag = false;

        /// <summary>
        /// Total number of lines in the file.
        /// </summary>
        public int  NumLines = 0;

        //=====================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">the full pathname of the source file</param>
        public SourceFile (String fullPath)
        {
            this.FullPath = fullPath;
            this.ShortName = Path.GetFileName(fullPath);
            this.IsHeaderFile = fullPath.EndsWith(".h");
        }

        //=====================================================================
        /// <summary>
        /// Performs various operations that can't be done until all the data
        /// has been parsed from all the source files.
        /// </summary>
        public void PostProcess (SymbolTable st)
        {
            this.Classes.Sort();
            this.Macros.Sort();
            this.Templates.Sort();
            this.GlobalObjects.Sort();
            this.GlobalFunctions.Sort();

            foreach (EnumGroup g in this.EnumGroups)
            {
                g.Enums.Sort();
                foreach (EnumDef e in g.Enums)
                    this.Enums.Add(e);
            }

            this.EnumGroups.Sort();
            this.Enums.Sort();
        }

        //=====================================================================
        /// <summary>
        /// Searches this source file and all its Include files for the named
        /// macro.  Returns null if not found.
        /// </summary>
        public MacroDef FindMacro (String name)
        {
            foreach (MacroDef m in this.Macros)
                if (m.Name == name)
                    return m;
            foreach (SourceFile f in this.IncludeFiles)
            {
                MacroDef m = f.FindMacro(name);
                if (m != null)
                    return m;
            }
            return null;
        }

        //=====================================================================
        /// <summary>
        /// Expands macros.
        /// </summary>
        /// <param name="argValues">macro argument values, or null</param>
        public String ExpandMacros (String inputLine)
        {
            ArrayList argValues = new ArrayList();
            foreach (MacroDef m in this.Macros)
            {
                int offset = 0, length = 0;
                while (0 <= (offset = m.FindIn(inputLine, offset, argValues, ref length)))
                {
                    inputLine = inputLine.Remove(offset, m.Name.Length);
                    inputLine = inputLine.Insert(offset, m.Expand(argValues));
                    offset += length;
                }
            }

            foreach (SourceFile f in this.IncludeFiles)
                inputLine = f.ExpandMacros(inputLine);
            return inputLine;
        }

        //=====================================================================

        public bool HasUnsortedDependencies ()
        {
            foreach (SourceFile f in this.IncludeFiles)
                if (! f.SortFlag)
                    return true;
            return false;
        }

        //=====================================================================
        /// <summary>
        /// For sorting; required by the IComparable interface.
        /// </summary>
        public int CompareTo (object that)
        {
            return this.ShortName.CompareTo(((SourceFile) that).ShortName);
        }

    }

}
