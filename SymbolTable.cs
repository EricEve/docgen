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

/*
 *  The classes in this file contain all the information gathered from the source
 *  files.  These classes are independent of both the front-end (parser) and
 *  back-end (output generator).
 */

namespace DocGen
{
    /// <summary>
    /// Contains all the information gathered from the source files.
    /// </summary>
    public class SymbolTable
    {
        /// <summary>
        /// All the files (SourceFile) in all the source files.
        /// </summary>
        public ArrayList Files = new ArrayList();

        /// <summary>
        /// All the classes (ClassDef) in all the source files.
        /// </summary>
        public ArrayList Classes = new ArrayList();

        /// <summary>
        /// All the GrammarProd objects (GrammarProd) in all the source files.
        /// </summary>
        public ArrayList GrammarProds = new ArrayList();

        /// <summary>
        /// All the macros (MacroDef) in all the source files.
        /// </summary>
        public ArrayList Macros = new ArrayList();

        /// <summary>
        /// All the enum groups (EnumGroup) in all the source files.
        /// </summary>
        public ArrayList EnumGroups = new ArrayList();

        /// <summary>
        /// All the enums (EnumDef) in all the source files.
        /// </summary>
        public ArrayList Enums = new ArrayList();

        /// <summary>
        /// All the templates (TemplateDef) in all the source files.
        /// </summary>
        public ArrayList Templates = new ArrayList();

        /// <summary>
        /// All the global objects (ObjectDef) in all the source files.
        /// </summary>
        public ArrayList GlobalObjects = new ArrayList();

        /// <summary>
        /// All the global functions (FunctionDef) in all the source files.
        /// </summary>
        public ArrayList GlobalFunctions = new ArrayList();

        /// <summary>
        /// All the "modify" statements (Modification) in all the source files.
        /// </summary>
        public ArrayList Modifications = new ArrayList();

        /// <summary>
        /// All object filenames.  We use this to ensure that object file
        /// names are unique, even in case-insensitive file systems.  See
        /// SetObjectFileName() for details.
        /// </summary>
        public ArrayList ObjectFileNames = new ArrayList();

        //=====================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        public SymbolTable()
        {
        }

        //=====================================================================
        /// <summary>
        /// Adds a new SourceFile to the SymbolTable and returns it.
        /// </summary>
        /// <param name="fileName">the name of the source file</param>
        public SourceFile AddFile (String fileName)
        {
            SourceFile sf = new SourceFile(fileName);
            this.Files.Add(sf);
            return sf;
        }

        //=====================================================================
        /// <summary>
        /// Sets an object symbol's filename member such that the filename
        /// is unique among all object symbols so far, even in file systems
        /// that are insensitive to case.
        /// </summary>
        public void SetObjectFileName(Modification obj)
        {
            // start with the object name
            String fname = obj.Name;
            String lcname;

            // keep going until we find a unique name
            for (int i = 1 ; ; i++)
            {
                // get the lower-case version of the name - we'll use this
                // as the filename for indexing purposes, since we want to
                // ignore case in order to respect case-insensitive file
                // systems
                lcname = fname.ToLower();

                // see if this is in the table already
                bool found = false;
                foreach (String s in ObjectFileNames)
                {
                    if (s.CompareTo(lcname) == 0)
                    {
                        found = true;
                        break;
                    }
                }

                // if we didn't find it, we can use the current name
                if (!found)
                    break;

                // this name collides with a name already in the table;
                // add a numeric suffix to the base name and try again
                fname = obj.Name + i;
            }

            // add the lower-case name to the file list, so that we'll find
            // any collisions with future objects added to the table
            ObjectFileNames.Add(lcname);

            // remember the mixed-case name as the object filename
            obj.FileName = fname;
        }

        //=====================================================================
        /// <summary>
        /// Performs various operations that can't be done until all the data
        /// has been parsed from the source files.
        /// </summary>
        public void PostProcess ()
        {
            this.Classes.Sort();
            this.Files.Sort();
            this.Macros.Sort();
            this.Templates.Sort();
            this.GlobalFunctions.Sort();
            this.GlobalObjects.Sort();
            this.GrammarProds.Sort();

            foreach (Modification mod in this.Modifications)
            {
                ObjectOrClass cd = this.FindObjectOrClass(mod.Name);
                if (cd != null)
                {
                    // add the 'modify' source to the base object's source list
                    cd.Modifications.Add(mod.Source);

                    // add the 'modify' object to the base object's mod list
                    cd.ModificationMods.Add(mod);

                    // merge the methods and properties into the base object
                    cd.MergeModification(mod);

                    // the 'modify' object's documentation goes in the same
                    // file as the base object
                    mod.FileName = cd.FileName;
                }
                //  else throw new Exception("modification of undefined class or object: " + mod.Name);
                // PROBLEM: the preceding line triggers because we don't expand macros
            }

            foreach (ClassDef c in this.Classes)
            {
                c.LinkBaseClasses(this);
                c.Methods.Sort();
                c.Properties.Sort();
                c.GlobalObjects.Sort();
                // note: do NOT sort sub-classes; order matters!
            }
            
            foreach (ObjectDef o in this.GlobalObjects)
            {
                o.LinkBaseClasses(this);
                o.Methods.Sort();
                o.Properties.Sort();
                // note: do NOT sort sub-classes; order matters!
            }

            foreach (SourceFile f in this.Files)
                f.PostProcess(this);

            foreach (ClassDef c in this.Classes)
                c.CheckForOverridden();

            foreach (ObjectDef o in this.GlobalObjects)
                o.CheckForOverridden();

            // collect all the enums from the enum groups
            foreach (EnumGroup g in this.EnumGroups)
                foreach (EnumDef e in g.Enums)
                    this.Enums.Add(e);
            this.Enums.Sort();
        }

        //=====================================================================
        /// <summary>
        /// Returns the class with the given name, or null if not found.
        /// </summary>      
        public ClassDef FindClass (String name)
        {
            foreach (ClassDef c in this.Classes)
                if (c.Name == name)
                    return c;
            return null;
        }

        //=====================================================================
        /// <summary>
        /// Returns the GrammarProd with the given name, or null if not found.
        /// </summary>      
        public GrammarProd FindGrammarProd (String name)
        {
            // search for it
            foreach (GrammarProd g in this.GrammarProds)
            {
                if (g.Name == name)
                    return g;
            }

            // didn't find it
            return null;
        }

        //=====================================================================
        /// <summary>
        /// Returns the file with the given name, or null if not found.
        /// </summary>      
        public SourceFile FindFile (String shortName)
        {
            foreach (SourceFile s in this.Files)
                if (s.ShortName == shortName)
                    return s;
            return null;
        }

        //=====================================================================
        /// <summary>
        /// Returns the class or object with the given name, or null if not found.
        /// </summary>      
        public ObjectOrClass FindObjectOrClass (String name)
        {
            foreach (ClassDef c in this.Classes)
                if (c.Name == name)
                    return c;
            foreach (ObjectDef o in this.GlobalObjects)
                if (o.Name == name)
                    return o;
            return null;
        }

        //=====================================================================
        /// <summary>
        /// Dumps the symbol table to the named file.
        /// </summary>
        public void Dump (String fileName)
        {
            StreamWriter sw = new StreamWriter(fileName);
            this.Dump(sw);
            sw.Close();
        }

        //=====================================================================
        /// <summary>
        /// Dumps the symbol table to the given stream.
        /// </summary>
        /// <param name="sw"></param>
        public void Dump (StreamWriter sw)
        {
            foreach (ClassDef cd in this.Classes)
                cd.Dump(sw);
        }

    }

    //#########################################################################
    /// <summary>
    /// Describes the location of a symbol in a source file.
    /// </summary>
    public class SourceLoc
    {
        /// <summary>
        /// The name of the source file.
        /// </summary>
        public String       Name;

        /// <summary>
        /// The line number in the source file.
        /// </summary>
        public int          Line;

        //=====================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">file name</param>
        /// <param name="line">line number</param>
        public SourceLoc (String name, int line)
        {
            this.Name = name;
            this.Line = line;
        }
    }

    //#########################################################################
    /// <summary>
    /// Base class for everything we extract from a source file.
    /// </summary>
    public class Symbol : IComparable
    {
        /// <summary>
        /// The name of the symbol.
        /// </summary>
        public String       Name;

        /// <summary>
        /// The comment immediately preceding the symbol's definition.
        /// </summary>
        public String       Description;

        /// <summary>
        /// Where the symbol was found.
        /// </summary>
        public SourceLoc    Source;

        //=====================================================================
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">the source file where the symbol was found</param>
        /// <param name="line">the line where the symbol was found</param>
        protected Symbol (String file, int line)
        {
            this.Source = new SourceLoc(file, line);
        }

        //=====================================================================
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">the source file where the symbol was found</param>
        /// <param name="line">the line where the symbol was found</param>
        public Symbol (String name, String descr, String file, int line)
            : this(file, line)
        {
            this.Name = name;
            this.Description = descr;
        }

        //=====================================================================
        
        public override String ToString () { return this.Name; }

        //=====================================================================
        /// <summary>
        /// For sorting; required by the IComparable interface.
        /// </summary>
        public int CompareTo (object that)
        {
            // compare the names
            int d = this.Name.ToLower().CompareTo(
                ((Symbol) that).Name.ToLower());

            // if the names are the same sort by source file
            if (d == 0)
                d = this.Source.Name.ToLower().CompareTo(
                    ((Symbol) that).Source.Name.ToLower());

            // if the names and source file are the same, sort by source line
            if (d == 0)
                d = this.Source.Line - ((Symbol) that).Source.Line;

            // return the result
            return d;
        }

        //=====================================================================
        /// <summary>
        /// Dumps the symbol to the given stream.
        /// </summary>
        public virtual void Dump (StreamWriter sw)
        {
            sw.WriteLine("symbol " + this.Name + " // " + this.Description);
        }
    }

    //#########################################################################
    /// <summary>
    /// For temporarily storing "modify" statements.
    /// </summary>
    public class Modification : Symbol
    {
        /// <summary>
        /// The base filename for the symbol.  This is normally just the
        /// object name; however, symbol names are case sensitive, but
        /// some file systems aren't, so we need to use different filenames
        /// when symbol names differ only in case.
        /// </summary>
        public String       FileName;

        /// <summary>
        /// The object's methods (Method).
        /// </summary>
        public ArrayList    Methods = new ArrayList();      

        /// <summary>
        /// The object's properties (PropertyDef).
        /// </summary>
        public ArrayList    Properties = new ArrayList();   

        //=====================================================================
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">the source file where the object was found</param>
        /// <param name="line">the line where the object was found</param>
        public Modification (String file, int line)
            : base (file, line)
        {
        }

        //=====================================================================
        /// <summary>
        /// Returns the method with the given name, or null if not found.
        /// </summary>
        public MethodDef FindMethod (String name)
        {
            foreach (MethodDef m in this.Methods)
                if (m.Name == name)
                    return m;
            return null;
        }

        //=====================================================================
        /// <summary>
        /// Returns the property with the given name, or null if not found.
        /// </summary>
        public PropertyDef FindProperty (String name)
        {
            foreach (PropertyDef p in this.Properties)
                if (p.Name == name)
                return p;
            return null;
        }

        //=====================================================================
    }

    //#########################################################################
    /// <summary>
    /// For GrammarProd objects.  A GrammarProd object is implicitly
    /// defined by a 'grammar' statement, but only implicitly.  The
    /// 'grammar' statement explicitly defines a match-object class,
    /// and the properties and methods defined in the 'grammar' are
    /// attached to the match-object class.  However, the statement
    /// also defines or adds to a GrammarProd object with the root
    /// (untagged) name of the production, and adds the token-list
    /// rules it defines to the alternative list for the GrammarProd
    /// object.
    /// </summary>
    public class GrammarProd : Modification
    {
        /// <summary>
        /// The associated match-object classes (ClassDef)
        /// </summary>
        public ArrayList    MatchObjects = new ArrayList();
    

        //=====================================================================
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">the source file where the object was found</param>
        /// <param name="line">the line where the object was found</param>
        public GrammarProd (String file, int line)
            : base (file, line)
        {
        }
    }


    //#########################################################################
    /// <summary>
    /// Base class for TADS3 objects and classes..
    /// </summary>
    public abstract class ObjectOrClass : Modification
    {
        /// <summary>
        /// The object's base classes (ClassDef).
        /// </summary>
        public ArrayList    BaseClasses = new ArrayList();

        /// <summary>
        /// Places where this class or object was modified (SourceLoc).
        /// </summary>      
        public ArrayList    Modifications = new ArrayList();

        /// <summary>
        /// The actual modifier statements
        /// </summary>
        public ArrayList    ModificationMods = new ArrayList();

        //=====================================================================
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">the source file where the object was found</param>
        /// <param name="line">the line where the object was found</param>
        protected ObjectOrClass (String file, int line)
            : base (file, line)
        {
        }

        //=====================================================================
        /// <summary>
        /// Looks for overridden properties and methods.
        /// </summary>
        public void CheckForOverridden ()
        {
            foreach (MethodDef m in this.Methods)
                m.CheckForOverridden(this);
            foreach (PropertyDef p in this.Properties)
                p.CheckForOverridden(this);
        }

        //=====================================================================
        /// <summary>
        /// Merges the given modification into the class.
        /// </summary>
        public void MergeModification (Modification mod)
        {
//          if (mod.Description != "")
//              this.Description += "\r\n" + mod.Description;
            foreach (MethodDef modMethod in mod.Methods)
            {
                MethodDef origMethod = this.FindMethod(modMethod.Name);
                if (origMethod == null)
                    this.Methods.Add(modMethod);
                else
                {
                    origMethod.Modifications.Add(modMethod.Source);
                    origMethod.ModificationMods.Add(modMethod);
//                  if (modMethod.Description != "")
//                      origMethod.Description += "\r\n" + modMethod.Description;
                }
            }
            foreach (PropertyDef modProp in mod.Properties)
            {
                PropertyDef origProp = this.FindProperty(modProp.Name);
                if (origProp == null)
                    this.Properties.Add(modProp);
                else
                {
                    origProp.Modifications.Add(modProp.Source);
                    origProp.ModificationMods.Add(modProp);
                }
            }
        }

        //=====================================================================
    }

    //#########################################################################
    /// <summary>
    /// Represents a TADS3 global object.
    /// </summary>
    public class ObjectDef : ObjectOrClass
    {
        /// <summary>
        /// Is this a transient object?
        /// </summary>
        public bool IsTransient = false;

        /// <summary>
        /// Does this object represent an action?
        /// </summary>
        public bool isAction = false;
        //=====================================================================
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">the source file where the object was found</param>
        /// <param name="line">the line where the object was found</param>
        public ObjectDef (String file, int line)
            : base (file, line)
        {
        }

        //=====================================================================
        /// <summary>
        /// Populates the lists of global objects in this class's base classes.
        /// </summary>
        public void LinkBaseClasses (SymbolTable st)
        {
            for (int i = 0; i < this.BaseClasses.Count; i++)
            {
                String name = this.BaseClasses[i] as string;
                if (name == null)
                    continue;
                ObjectOrClass oc = st.FindObjectOrClass(name);
                if (oc == null)
                    continue;
                this.BaseClasses[i] = oc;
                if (oc is ClassDef)
                {
                    ClassDef c = (ClassDef)oc;
                    if (!c.GlobalObjects.Contains(this))
                        c.GlobalObjects.Add(this);
                }
            }
        }

        //=====================================================================
        /// <summary>
        /// Dumps the class to the given stream.
        /// </summary>
        public override void Dump (StreamWriter sw)
        {
            sw.Write("object " + this.Name + " :");
            foreach (object o in this.BaseClasses)
                sw.Write(" " + o.ToString());
            sw.WriteLine(" // " + this.Description);
            foreach (PropertyDef p in this.Properties)
                p.Dump(sw);
            foreach (MethodDef m in this.Methods)
                m.Dump(sw);
        }
    }

    //#########################################################################
    /// <summary>
    /// Represents a TADS3 class.
    /// </summary>
    public class ClassDef : ObjectOrClass
    {
        /// <summary>
        /// The class's subclasses (ClassDef).
        /// </summary>
        public ArrayList    SubClasses = new ArrayList();   

        /// <summary>
        /// Global objects that are of this class (ObjectDef)..
        /// </summary>
        public ArrayList    GlobalObjects = new ArrayList();    

        /// <summary>
        /// Was the class declared as "intrinsic"?
        /// </summary>
        public bool         IsIntrinsic = false;

        /// <summary>
        /// Was the class declared as "grammar"?
        /// </summary>
        public bool     IsGrammar = false;

        /// <summary>
        /// For a "grammar" class, the rule defining the grammar
        /// </summary>
        public String   GrammarRule = null;

        /// <summary>
        /// For a "grammar" class, the associated GrammarProd object
        /// </summary>
        public GrammarProd GrammarProdObj = null;

        /// <summary>
        /// Was the class declared using a Define.*Action() macro?
        /// </summary>
        public bool     IsAction = false;
        
        /// <summary>
        /// The original definition, if defined via a VerbRule() or
        /// DefineXAction macro.
        /// </summary>
        public String   OrigDef = null;
        
        //=====================================================================
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">the source file where the class was found</param>
        /// <param name="line">the line where the class was found</param>
        public ClassDef (String file, int line)
            : base (file, line)
        {
        }

        //=====================================================================
        /// <summary>
        /// Populates the lists of subclasses in this class's base classes.
        /// </summary>
        public void LinkBaseClasses (SymbolTable st)
        {
            for (int i = 0; i < this.BaseClasses.Count; i++)
            {
                String name = this.BaseClasses[i] as string;
                if (name == null)
                    continue;
                ObjectOrClass oc = st.FindObjectOrClass(name);
                if (oc == null)
                    continue;
                this.BaseClasses[i] = oc;
                if (oc is ClassDef)
                {
                    ClassDef c = (ClassDef)oc;
                    
                    if (!c.SubClasses.Contains(this))
                        c.SubClasses.Add(this);
                }
            }
        }

        //=====================================================================
        /// <summary>
        /// Dumps the class to the given stream.
        /// </summary>
        public override void Dump (StreamWriter sw)
        {
            sw.Write("class " + this.Name + " :");
            foreach (object o in this.BaseClasses)
                sw.Write(" " + o.ToString());
            sw.WriteLine(" // " + this.Description);
            foreach (PropertyDef p in this.Properties)
                p.Dump(sw);
            foreach (MethodDef m in this.Methods)
                m.Dump(sw);
        }
    }

    //#########################################################################
    /// <summary>
    /// Represents a single global function (subroutine).
    /// </summary>
    public class FunctionDef : Symbol
    {
        /// <summary>
        /// The parameter names, in the order in which they were defined.
        /// Each element in the array is a String.
        /// </summary>
        public ArrayList    Parameters = new ArrayList();   

        //=====================================================================
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">the source file where the symbol was found</param>
        /// <param name="line">the line where the symbol was found</param>
        public FunctionDef (String file, int line)
            : base (file, line)
        {
        }

        //=====================================================================
        /// <summary>
        /// Dumps the method to the given stream.
        /// </summary>
        public override void Dump (StreamWriter sw)
        {
            sw.Write("function " + this.Name + " (");
            foreach (String s in this.Parameters)
                sw.Write(" " + s);
            sw.WriteLine(" ) // " + this.Description);
        }
    }

    //#########################################################################
    /// <summary>
    /// Represents a single method in a class.
    /// </summary>
    public class MethodDef : FunctionDef
    {
        /// <summary>
        /// Does this method override a method in a base class?
        /// </summary>
        public bool         Overridden = false;

        /// <summary>
        /// The class to which this method belongs.
        /// </summary>
        public Modification     ClassOrObject;

        /// <summary>
        /// Places where this class was modified (SourceLoc).
        /// </summary>      
        public ArrayList    Modifications = new ArrayList();

        /// <summary>
        /// Actual statements containing modifications (MethodDef)
        /// </summary>
        public ArrayList    ModificationMods = new ArrayList();

        /// <summary>
        /// Is this an interface property definition only (a '//' line)?
        /// </summary>
        public bool IsIfcOnly = false;


        //=====================================================================
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">the source file where the symbol was found</param>
        /// <param name="line">the line where the symbol was found</param>
        public MethodDef (String file, int line)
            : base (file, line)
        {
        }

        //=====================================================================
        /// <summary>
        /// Checks for overridden methods.
        /// </summary>
        public void CheckForOverridden (ObjectOrClass baseClass)
        {
            foreach (object o in baseClass.BaseClasses)
            {
                if ((o is ClassDef) && (this._CheckForOverridden((ClassDef) o)))
                {
                    this.Overridden = true;
                    return;
                }
            }
        }

        //=====================================================================
        /// <summary>
        /// Checks for overridden methods.
        /// </summary>
        private bool _CheckForOverridden (ObjectOrClass baseClass)
        {
            foreach (MethodDef m in baseClass.Methods)
                if (m.Name == this.Name)
                    return true;
            foreach (object o in baseClass.BaseClasses)
            {
                if (o is ClassDef)
                    if (this._CheckForOverridden((ClassDef) o))
                        return true;
            }
            return false;
        }

        //=====================================================================
        /// <summary>
        /// Dumps the method to the given stream.
        /// </summary>
        public override void Dump (StreamWriter sw)
        {
            sw.Write("\tmethod " + this.Name + " (");
            foreach (String s in this.Parameters)
                sw.Write(" " + s);
            sw.WriteLine(" ) // " + this.Description);
        }
    }

    //#########################################################################
    /// <summary>
    /// Represents a single property within a class.
    /// </summary>
    public class PropertyDef : Symbol
    {
        /// <summary>
        /// Does this property override a property in a base class?
        /// </summary>
        public bool         Overridden = false;

        /// <summary>
        /// The class or object to which this property belongs.
        /// </summary>
        public Modification ClassOrObject;

        /// <summary>
        /// Places where this property was modified (SourceLoc).
        /// </summary>      
        public ArrayList    Modifications = new ArrayList();

        /// <summary>
        /// Actual statements containing modifications (MethodDef)
        /// </summary>
        public ArrayList    ModificationMods = new ArrayList();

        /// <summary>
        /// Is this an interface property definition only (a '//' line)?
        /// </summary>
        public bool IsIfcOnly = false;


        //=====================================================================
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">the name of the property</param>
        /// <param name="desc">the comment immediately preceding the property</param>
        /// <param name="cl">the class or object to which the property belongs</param>
        /// <param name="file">the source file where the property was found</param>
        /// <param name="line">the line where the property was found</param>
        public PropertyDef (String name, String desc, Modification cl, String file, int line)
            : base (file, line)
        {
            this.Name = name;
            this.Description = desc;
            this.ClassOrObject = cl;
        }

        //=====================================================================
        /// <summary>
        /// Checks for overridden properties.
        /// </summary>
        public void CheckForOverridden (ObjectOrClass baseClass)
        {
            foreach (object o in baseClass.BaseClasses)
            {
                if ((o is ClassDef) && (this._CheckForOverridden((ClassDef) o)))
                {
                    this.Overridden = true;
                    return;
                }
            }
        }

        //=====================================================================
        /// <summary>
        /// Checks for overridden properties.
        /// </summary>
        private bool _CheckForOverridden (ObjectOrClass baseClass)
        {
            foreach (PropertyDef p in baseClass.Properties)
                if (p.Name == this.Name)
                    return true;
            foreach (object o in baseClass.BaseClasses)
            {
                if (o is ClassDef)
                    if (this._CheckForOverridden((ClassDef) o))
                        return true;
            }
            return false;
        }

        //=====================================================================
        /// <summary>
        /// Dumps this property to the given stream.
        /// </summary>
        public override void Dump (StreamWriter sw)
        {
            sw.WriteLine("\tproperty " + this.Name + " // " + this.Description);
        }
    }

    //#########################################################################
    /// <summary>
    /// Represents a TADS3 macro.
    /// </summary>
    public class MacroDef : FunctionDef
    {
        /// <summary>
        /// The body of the macro; that is, what it expands to.
        /// </summary>
        public String       Body;

        //=====================================================================
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">the source file where the macro was found</param>
        /// <param name="line">the line where the macro was found</param>
        public MacroDef (String file, int line)
            : base (file, line)
        {
        }

        //=====================================================================
        /// <summary>
        /// Expands the macro.
        /// </summary>
        /// <returns></returns>
        public String Expand (ArrayList argValues)
        {
            if (argValues == null)
            {
                if (this.Parameters.Count != 0)
                    throw new Exception("WARNING: macro arg count mismatch");
            }
            else if (this.Parameters.Count != argValues.Count)
            {
                //  throw new Exception("macro arg count mismatch");
                System.Console.Out.WriteLine("macro arg count mismatch");
                return this.Body;
            }

            String expanded = this.Body;

            for (int argNum = 0; argNum < this.Parameters.Count; argNum++)
            {
                int searchOffset = 0;
                String argName = (String) this.Parameters[argNum];
                int argOffset;
                while (0 <= (argOffset = FindArgName(argName, expanded, searchOffset)))
                {
                    String argValue = (String) argValues[argNum];
                    expanded = expanded.Remove(argOffset, argName.Length);
                    expanded = expanded.Insert(argOffset, argValue);
                    searchOffset = argOffset + argValue.Length;
                }
            }

            int n;

            while (0 <= (n = expanded.IndexOf("#@")))
            {
                // MISSING: need make sure it's not inside a string

                // grab next token and put quotes around it
                int length = 0;
                int sym = FindNextSymbol(expanded, n, ref length);
                expanded = expanded.Remove(n, 2);
                expanded = expanded.Insert(n, "\'");
                expanded = expanded.Insert(sym+length-1, "\'");
            }

            n = 0;
            while (0 <= (n = expanded.IndexOf("#", n)))
            {
                // MISSING: need make sure it's not inside a string

                // skip over '##' operators
                if ((n > 0) && (expanded[n-1] == '#'))
                {
                    n++;
                    continue;
                }
                if ((n < expanded.Length-1) && (expanded[n+1] == '#'))
                {
                    n++;
                    continue;
                }

                // grab next token and put quotes around it
                int length = 0;
                int sym = FindNextSymbol(expanded, n, ref length);
                expanded = expanded.Remove(n, 1);
                expanded = expanded.Insert(n, "\'");
                expanded = expanded.Insert(sym+length, "\'");
            }

            while (0 <= (n = expanded.IndexOf("##")))
            {
                // MISSING: need make sure it's not inside a string

                String left = expanded.Substring(0, n);
                String right = expanded.Substring(n+2);
                left = left.TrimEnd();
                right = right.TrimStart();
                if (left.EndsWith("\"") && right.StartsWith("\""))
                    expanded = left.Substring(0, left.Length-1) + right.Substring(1);
                else if (left.EndsWith("\'") && right.StartsWith("\'"))
                    expanded = left.Substring(0, left.Length-1) + right.Substring(1);
                else expanded = left + right;
            }

            return expanded;
        }

        //=====================================================================
        /// <summary>
        /// Searches the target string for the given argument name, starting
        /// at the given offset.
        /// </summary>
        /// <param name="argName">what to search for</param>
        /// <param name="target">the string to search</param>
        /// <param name="offset">where to start searching</param>
        /// <returns>the offset of the argument name, or -1 if not found</returns>
        private int FindArgName (String argName, String target, int offset)
        {
            // COULD BE MORE EFFICIENT
            int n, length = 0;
            while (0 <= (n = FindNextSymbol(target, offset, ref length)))
            {
                if (target.Substring(offset, length) == argName)
                    return offset;
                offset++;
            }
            return -1;
        }

        //=====================================================================
        /// <summary>
        /// Searches the target string for the macro's name.
        /// </summary>
        /// <returns>the offset of the name, or -1 if not found</returns>
        public int FindIn (String target, int offset)
        {
            int n, length = 0;
            while (0 <= (n = FindNextSymbol(target, offset, ref length)))
            {
                if (target.Substring(offset, length) == this.Name)
                    return offset;
                offset++;
            }
            return -1;
        }

        //=====================================================================
        /// <summary>
        /// Searches the target string for an invocation of the macro.
        /// </summary>
        /// <param name="target">the string to search in</param>
        /// <param name="offset">where to start searching</param>
        /// <param name="argValues">receives the macro arguments</param>
        /// <param name="length">receives the length of the macro invocation, including args</param>
        /// <returns>the offset of the beginning of the invocation, or -1 if not found</returns>
        public int FindIn (String target, int offset, ArrayList argValues, ref int length)
        {
            int start = this.FindIn(target, offset);
            if (start < 0)
                return -1;
            if (this.Parameters.Count == 0)
            {
                argValues.Clear();
                length = this.Name.Length;
                return start;
            }

            int next = start + this.Name.Length;
            if (next >= target.Length)
                return -1;
            if (target[next] != '(')
                return -1;
            next++;

            String arg = "";
            int parenLevel = 0;
            while (true)
            {
                // MISSING: checking for strings

                if (next >= target.Length)
                    return -1;      // didn't find close paren
                if (target[next] == ',')
                {
                    argValues.Add(arg);
                    arg = "";
                }
                else if (target[next] == '(')
                    parenLevel++;
                else if (target[next] == ')')
                {
                    if (parenLevel > 0)
                        parenLevel--;
                    else
                    {
                        argValues.Add(arg);
                        break;
                    }
                }
                else arg += target[next];

                next++;
            }

            length = next - start;
            return start;
        }

        //=====================================================================
        /// <summary>
        /// Searches the target string for the next symbolic name, starting at
        /// the given offset.
        /// </summary>
        /// <param name="target">the string to search</param>
        /// <param name="offset">where to start searching</param>
        /// <param name="length">receives the length of the found symbol</param>
        /// <returns>the offset of the beginning of the symbol, or -1 if not found</returns>
        private static int FindNextSymbol (String target, int offset, ref int length)
        {
            if (offset >= target.Length)
                return -1;

            // MISSING: making sure we're not in the middle of a string

            // make sure we're not starting in the middle of a token
            if ((offset > 0) && (SymbolChars.IndexOf(target[offset-1]) >= 0))
                while ((offset < target.Length) && (SymbolChars.IndexOf(target[offset]) >= 0))
                    offset++;
            if (offset >= target.Length)
                return -1;
            // find beginning of token
            while ((offset < target.Length) && (SymbolChars.IndexOf(target[offset]) < 0))
                offset++;
            if (offset >= target.Length)
                return -1;
            int startOffset = offset;
            while ((offset < target.Length) && (SymbolChars.IndexOf(target[offset]) >= 0))
                offset++;
            length = offset - startOffset;
            return startOffset;         
        }

        static String SymbolChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";

        //=====================================================================
    }

    //#########################################################################
    /// <summary>
    /// Represents a logical group of enums.
    /// </summary>
    public class EnumGroup : IComparable
    {
        /// <summary>
        /// The enums that belong to this group (EnumDef).
        /// </summary>
        public ArrayList    Enums = new ArrayList();

        /// <summary>
        /// The comment immediately preceding the enum group.
        /// </summary>
        public String       Description;

        /// <summary>
        /// Where the enum group was found.
        /// </summary>
        public SourceLoc    Source;

        //=====================================================================
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">the source file where the enum group was found</param>
        /// <param name="line">the line where the enum group was found</param>
        public EnumGroup (String file, int line)
        {
            this.Source = new SourceLoc(file, line);
        }

        //=====================================================================
        /// <summary>
        /// For sorting; required by the IComparable interface.
        /// </summary>
        public int CompareTo (object that)
        {
            return this.GetSortName().ToLower().CompareTo(
                ((EnumGroup) that).GetSortName().ToLower());
        }

        // get my name for sorting purposes
        public String GetSortName()
        {
            if (Enums.Count == 0)
                return "";
            else
                return ((EnumDef)Enums[0]).Name;
        }
    }

    //#########################################################################
    /// <summary>
    /// Represents a single TADS3 enum.
    /// </summary>
    public class EnumDef : Symbol
    {
        /// <summary>
        /// The group to which the enum belongs.
        /// </summary>
        public EnumGroup    Group;

        //=====================================================================
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">the source file where the enum was found</param>
        /// <param name="line">the line where the enum was found</param>
        public EnumDef (String file, int line)
            : base (file, line)
        {
        }
    }

    //#########################################################################
    /// <summary>
    /// Represents a TADS3 template.
    /// </summary>
    public class TemplateDef : Symbol
    {
        /// <summary>
        /// The body of the template; that is, what it expands to.
        /// </summary>
        public String       Body;

        //=====================================================================
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">the source file where the template was found</param>
        /// <param name="line">the line where the template was found</param>
        public TemplateDef (String file, int line)
            : base (file, line)
        {
        }

    }
}
