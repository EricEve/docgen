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
 *  This file contains the parser, which reads the TADS3 source files.
 *  This parser is pretty stupid, and depends on the format of the source files
 *  nearly as much as the language syntax.  It pretty much ignores anything
 *  it doesn't understand, and generates no error messages.
 *  The parser is independent of the rest of the program, and can easily be
 *  replaced or improved.
 */

namespace DocGen
{
    /// <summary>
    /// The parser.
    /// </summary>
    public class Parser
    {
        /// <summary>
        /// Where to put all the stuff we collect.
        /// </summary>
        private SymbolTable SymbolTable;

        /// <summary>
        /// The current input file stream.
        /// </summary>
        private StreamReader CurrentFile;

        /// <summary>
        /// The current macro-expanded input file stream.
        /// </summary>
        private StreamReader CurrentExpFile = null;

        /// <summary>
        /// The current line in the macro-expanded file
        /// </summary>
        private String CurrentExpLine = null;

        /// <summary>
        /// The current line number in the current input file.
        /// </summary>
        private int LineNumber;

        /// <summary>
        /// Are we in an intrinsic function set?
        /// </summary>
        private bool InIntrinsicFuncs = false;

        /// <summary>
        /// Are we inside a function?
        /// </summry>
        private bool InFunc = false;

        /// <summary>
        /// The name of the current input file.
        /// </summary>
        private String CurrentFileName;

        /// <summary>
        /// The current source (input) file.
        /// </summary>
        private SourceFile CurrentSourceFile;

        /// <summary>
        /// The current class or object, or null.
        /// </summary>
        Modification CurrClassOrObj = null;

        /// <summary>
        /// The most recently parsed comment in the current input file.
        /// </summary>
        String LastComment = "";

        /// <summary>
        /// Does LastComment contains the first comment in the current input file?
        /// </summary>
        private bool FirstComment;

        /// <summary>
        /// For holding expanded multi-line macros.
        /// </summary>
        private ArrayList BufferedLines = new ArrayList();

        /// <summary>
        /// Whether to expand macros; mainly for debugging.
        /// </summary>
        public bool ExpandMacros = false;

        /// <summary>
        /// Had the parser encountered DefLangDir before?
        /// </summary>
        public bool DefLangDirSeen = false;
       

        //=====================================================================
        /// <summary>
        /// Constructor.
        /// </summary>
        public Parser (SymbolTable st)
        {
            this.SymbolTable = st;
        }

        //=====================================================================

        public void ParseFiles (ArrayList files)
        {
            int n = 1;
            foreach (SourceFile f in files)
            {
                System.Console.Out.Write("parsing file " + n.ToString() + " of " + 
                    files.Count.ToString() + " " + f.FullPath + " line " + 
                    "               ".Substring(0, maxProgLen));
                this.ParseInputFile(f);
            }
        }

        //=====================================================================
        /// <summary>
        /// Parses a single input file.
        /// </summary>
        public void ParseInputFile (SourceFile file)
        {
            this.CurrentSourceFile = file;
            this.CurrentFileName = file.FullPath;
            this.LineNumber = 0;
            this.FirstComment = true;   
            this.CurrentFile = new StreamReader(file.FullPath);
            String line;

            if (DocGenApp.MacroExpPath != null)
            {
                try
                {
                    this.CurrentExpFile = new StreamReader(
                        DocGenApp.MacroExpPath + "/" + file.ShortName);
                }
                catch (Exception)
                {
                    CurrentExpFile = null;
                    CurrentExpLine = null;
                }
            }

            while (null != (line = GetNextLine()))
                ProcessLine(line, CurrentExpLine);

            System.Console.Out.WriteLine();
            CurrentFile.Close();
        }

        //=====================================================================

        private void ShowProgress ()
        {
            String progress = LineNumber.ToString() + " of " + 
                this.CurrentSourceFile.NumLines.ToString() + "           ";
            progress = progress.Substring(0, maxProgLen);
            System.Console.Out.Write(backspaces + progress);
        }

        static int maxProgLen = 4 + 4 + 4;      // #### of ####
        static String backspaces = "\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b".Substring(0, maxProgLen);

        //=====================================================================
        /// <summary>
        /// Returns the next line in the current input file, or null on end-of-file.
        /// </summary>
        private String GetNextLine ()
        {
            if (! this.ExpandMacros)
            {
                LineNumber++;       
                this.ShowProgress();
                if (CurrentExpFile != null)
                    this.CurrentExpLine = CurrentExpFile.ReadLine();
                return CurrentFile.ReadLine();
            }

            String line;
            if (this.BufferedLines.Count == 0)
            {
                LineNumber++;       
                this.ShowProgress();
                line = CurrentFile.ReadLine();
                if (line == null)
                    return null;
                line = this.CurrentSourceFile.ExpandMacros(line);
                line = line.Replace("\r\n", "\n");
                line = line.Replace('\r', 'n');
                int n;
                while (0 <= (n = line.IndexOf('\n')))
                {
                    this.BufferedLines.Add(line.Substring(0, n));
                    line = line.Substring(n+1);
                }
                this.BufferedLines.Add(line);
            }

            line = (String) this.BufferedLines[0];
            this.BufferedLines.RemoveAt(0);
            return line;
        }

        //=====================================================================
        /// <summary>
        /// Processes the given input line.
        /// </summary>
        private void ProcessLine (String origLine, String expLine)
        {
            String line;
            bool ifcOnly = false;
            bool isTransient = false;
            
            // process everything except 'propertyset' lines from the expanded
            // version
            if (expLine == null || expLine.Trim().StartsWith("propertyset "))
                line = origLine;
            else
                line = expLine;

            line = line.TrimEnd();      // don't trim the beginning!

            if (line == "")     // ignore blank lines
                return;

            // count and strip leading spaces
            int initialSpaces = 0;
            while (line.StartsWith(" "))
            {
                initialSpaces++;
                line = line.Substring(1);
            }

            // look for DMsg and BMsg definitions

            if ((line.Contains("BMsg(") || line.Contains("DMsg(")) && !CurrentFileName.Contains(".h"))
                ProcessMessage(line);


            String token1 = GetNextToken(ref line);

            // skip "transient"
            if (token1 == "transient")
            {
                isTransient = true;
                token1 = GetNextToken(ref line);
            }

            if (token1 == "intrinsic")
            {
                token1 = GetNextToken(ref line);
                if (token1 == "class")
                    ProcessIntrinsicClass(line);
                else if (token1 == "'")
                    InIntrinsicFuncs = true;
                return;
            }
            if (token1 == "class")
            {
                InIntrinsicFuncs = InFunc = false;
                ProcessClass(line, origLine);
                return;
            }
            if (token1 == "grammar" || token1 == "VerbRule")
            {
                InIntrinsicFuncs = InFunc = false;
                ProcessGrammar(line, origLine);
                return;
            }
            if (token1 == "modify")
            {
                InIntrinsicFuncs = InFunc = false;
                ProcessModify(line);
                return;
            }
            if (token1 == "/*")
            {
                ProcessComment(line);
                return;
            }
            if (this.CurrentSourceFile.IsHeaderFile && (token1 == "enum"))
            {
                ProcessEnums(line);
                return;
            }
            if (token1 == "#")
            {
                token1 = GetNextToken(ref line);
                if (this.CurrentSourceFile.IsHeaderFile && (token1 == "define"))
                {
                    ProcessMacro(line);
                    return;
                }
            }

            // check for special interface-only comments
            if (token1 == "//" && initialSpaces == 4)
            {
                // set the 'interface only' flag, and then parse the
                // rest of the line as though it were real
                ifcOnly = true;
                token1 = GetNextToken(ref line);
            }

            if (token1 == "}" && initialSpaces == 0)
                InIntrinsicFuncs = InFunc = false;

            String token2 = GetNextToken(ref line);

            // for interface-only comments, be a little more selective, to
            // avoid accidentally matching other comments
            if (ifcOnly && (token2 != "=" && token2 != "(" && token2 != "{"))
                return;

            if (this.CurrentSourceFile.IsHeaderFile && (token2 == "template"))
            {
                InIntrinsicFuncs = InFunc = false;
                ProcessTemplate(token1, line);
                return;
            }

            if (token2 == "=")
            {
                if (initialSpaces == 4 && !InFunc)
                    ProcessProperty(token1, ifcOnly);
                return;
            }
            if (token2 == ":")
            {
                if (initialSpaces != 0 || InFunc)
                    return;
                ProcessObject(token1, line, isTransient, false);
                return;
            }
            if (token2 == "(")
            {
                if (token1 == "DefineIAction" || token1 == "DefineTAction"
                    || token1 == "DefineTIAction" || token1 == "DefineLiteralAction"
                    || token1 == "DefineLiteralTAction" || token1 == "DefineTopicAction"
                    || token1 == "DefineTopicTAction" || token1 == "DefineSystemAction"
                    || token1 == "DefineAction")
                {
                    ProcessActionDefinition(token1, line);
                    return;
                }

                // Certain function-like macros and statements can be ignored altogether
                if (token1 == "DefDigit" || token1 == "defOrdinal"
                    || token1 == "defTeen" || token1 == "defTens"
                    || token1 == "defDigit" || token1 == "if" || token1 == "//")
                {
                    return;
                }

                // Only process DefineLangDir the first time we see it //
                if (token1 == "DefineLangDir")
                {
                    if (DefLangDirSeen)
                        return;
                    else
                        DefLangDirSeen = true;
                }

                
                if (initialSpaces == 0)
                {
                    ProcessFunction(token1, line);
                    InFunc = true;
                }
                else if (InIntrinsicFuncs)
                    ProcessFunction(token1, line);
                else if (initialSpaces == 4 && !InFunc)
                    ProcessMethod(token1, line, ifcOnly);
                return;
            }
            if (token2 == "{")
            {
                if (initialSpaces == 4 && !InFunc)
                    ProcessMethod(token1, "{" + line, ifcOnly);
                return;
            }
        }

        //=====================================================================
        /// <summary>
        /// Processes a line which appears to define an action.
        /// DefineIdentifierAction(identifier)
        /// </summary>
        /// <param name="deftok"></param>
        /// <param name="line"></param>
        private void ProcessActionDefinition(String deftok, String line)
        {
            String actionClass;
            String actionName;

            actionClass = deftok.Substring(6, deftok.Length - 6);
            actionName = GetNextToken(ref line);

            ProcessObject(actionName, actionClass, false, true);
        }


       

        //=====================================================================
        /// <summary>
        /// Processes a line which appears to define a class.
        /// "class" space identifier ":" [identifier List]
        /// </summary>
        private void ProcessClass (String line, String origLine)
        {
            // note: the keyword "class" has already been parsed
            ClassDef cr = new ClassDef(Path.GetFileName(CurrentFileName), LineNumber);
            cr.Name = GetNextToken(ref line);
            cr.Description = LastComment;
            LastComment = "";
            if (cr.Name == "")
                return;     // must not be a class after all
            if (GetNextToken(ref line) != ":")
                return;     // must not be a class after all
            line = line.Trim();
            while (line != "")      // collect base class names
            {
                cr.BaseClasses.Add(GetNextToken(ref line));

                // stop when we reach anything other than a comma
                if (GetNextToken(ref line) != ",")
                    break;
            }

            // add the defined-as string to the description if applicable
            String origLine2 = origLine;
            String otok = GetNextToken(ref origLine2);
            if (otok.StartsWith("Define")
                && (otok.EndsWith("Action") || otok.EndsWith("ActionSub")))
            {
                origLine = origLine.Trim();
                int idx = origLine.IndexOf(")");
                cr.OrigDef = origLine.Substring(0, idx + 1);
                cr.IsAction = true;
            }

            SymbolTable.SetObjectFileName(cr);
            SymbolTable.Classes.Add(cr);
            this.CurrentSourceFile.Classes.Add(cr);
            CurrClassOrObj = cr;
        }

        //=====================================================================
        /// <summary>
        /// Processes a line which appears to define a grammar object.
        /// "grammar" identifier "(" identifier ")" ":" [token-list]
        ///           ":" [class-identifier-list]
        /// </summary>
        private void ProcessGrammar (String line, String origLine)
        {
            // note: the keyword "grammar" has already been parsed
            ClassDef cr = new ClassDef(Path.GetFileName(CurrentFileName),
                                       LineNumber);
            cr.Name = GetNextToken(ref line);
            cr.Description = LastComment;
            cr.IsGrammar = true;
            String origLine2 = origLine;
            bool isVerbRule = (GetNextToken(ref origLine2) == "VerbRule");
            LastComment = "";
            if (cr.Name == "")
                return;     // must not be a class after all

            // parse the tag, if present
            if (cr.Name == "(" || GetNextToken(ref line) == "(")
            {
                // store the tagged version of the name
                String tag = GetNextToken(ref line);
                if (GetNextToken(ref line) != ")")
                    return;

                if (isVerbRule)
                    cr.Name = "VerbRule";

                cr.Name += "(" + tag + ")";

                // if we created this with VerbRule, remember this
                if (isVerbRule)
                {
                    origLine = origLine.Trim();
                    int idx = origLine.IndexOf(")");
                    cr.OrigDef = origLine.Substring(0, idx + 1);
                }
            }

            // check for the ":"
            if (GetNextToken(ref line) != ":" && !isVerbRule)
                return;     // must not be a grammar statement after all

            // skip the production token list, which probably spans
            // multiple lines
            String rule = "";
            for (char qu = '\0' ; ; )
            {
                int idx;
                bool found = false;
                
                // scan to the ":", or to end of line
                for (idx = 0 ; idx < line.Length ; ++idx)
                {
                    switch (line[idx])
                    {
                    case '\\':
                        // skip the next character
                        ++idx;
                        break;

                    case ':':
                        // stop unless in a string
                        if (qu == '\0')
                            found = true;
                        break;

                    case '\'':
                    case '"':
                        if (qu != '\0')
                        {
                            if (qu == line[idx])
                                qu = '\0';
                        }
                        else
                            qu = line[idx];
                        break;
                    }

                    // if we found the ':', stop here
                    if (found)
                        break;
                }

                // add up to the end of this scan to the rule
                if (found)
                {
                    // found it - add up to the ':' to the rule
                    rule += line.Substring(0, idx).TrimEnd();

                    // continue from just past the ':'
                    line = line.Substring(idx + 1);

                    // we're done scanning the rule
                    break;
                }

                // didn't find it - add this line and a newline to the rule
                if (line.Trim() != "")
                    rule += line + "\u0001";

                // continue to the next line of the rule
                line = GetNextLine();
            }

            // now parse the class list
            line = line.Trim();
            while (line != "")      // collect base class names
            {
                // get the token
                cr.BaseClasses.Add(GetNextToken(ref line));

                // check for and skip the comma
                if (GetNextToken(ref line) != ",")
                    break;
            }

            // add the grammar list to the class
            cr.GrammarRule = rule;

            SymbolTable.SetObjectFileName(cr);
            SymbolTable.Classes.Add(cr);
            this.CurrentSourceFile.Classes.Add(cr);
            CurrClassOrObj = cr;

            // get the GrammarProd name - this is just the class name
            // sans the (tag) part
            int n;
            String gpName;
            if ((n = cr.Name.IndexOf('(')) >= 0)
                gpName = cr.Name.Substring(0, n);
            else
                gpName = cr.Name;

            // find the existing GrammarProd
            GrammarProd g = SymbolTable.FindGrammarProd(gpName);

            // if we didn't find one, create it
            if (g == null)
            {
                g = new GrammarProd(Path.GetFileName(CurrentFileName),
                                    LineNumber);
                g.Name = gpName;
                SymbolTable.SetObjectFileName(g);
                SymbolTable.GrammarProds.Add(g);
            }

            // add me to the GrammarProd's list of match objects
            g.MatchObjects.Add(cr);
            cr.GrammarProdObj = g;

            //if (isVerbRule)
            //{
            //    while (!line.Contains("action"))
            //    {
            //        line = GetNextLine();
            //        if (line.Contains(";"))
            //            break;
            //    }

            //    if (line.Contains("action"))
            //    {
            //        while (GetNextToken(ref line) != "action")
            //            ;

            //        if (GetNextToken(ref line) != "=")
            //            return;

            //        String action = GetNextToken(ref line);

            //    }
            //}
        }

        //====================================================================
        /// <summary>
        /// Extracts a DMsg() or BMsg() definition from a line and processes the result
        /// </summary>
        /// <param name="line"></param>
        private void ProcessMessage(String line)
        {
            String tok;
            String messName = "";
            String messType;
            String messText = "";

            Message mes = new Message(Path.GetFileName(CurrentFileName), LineNumber);

            tok = GetNextToken(ref line);
            while (tok != "BMsg" && tok != "DMsg")
            {
                tok = GetNextToken(ref line);
            }

            messType = tok;
           

            GetNextToken(ref line);
            do
            {
                tok = GetNextToken(ref line);
                if (tok == ",")
                    break;
                else
                    messName += (tok + " ");
            } while (tok != ",");

            messText = line.Trim();
            mes.Name = messName.Trim();            
            mes.type = messType;

            

            while (!line.Contains(")"))
            {
                line = GetNextLine();
                messText += (" " + line);
            }

            /* Replace common HTML entities */
            messText = messText.Replace("&", "&amp;");
            messText = messText.Replace(">", "&gt;");
            messText = messText.Replace("<", "&lt;");

            mes.text = messText;
            SymbolTable.Messages.Add(mes);
            
        }

        //=====================================================================
        /// <summary>
        /// Processes a line which appears to be a "modify" statement extending an intrinsic class.
        /// "modify" space identifier 
        /// </summary>
        private void ProcessModify (String line)
        {
            // note: the keyword "modify" has already been parsed
            Modification mod = new Modification(Path.GetFileName(CurrentFileName), LineNumber);
            mod.Name = GetNextToken(ref line);
            mod.Description = LastComment;
            LastComment = "";
            if (mod.Name == "")
                return;     // must not be a modify statement after all

            // Note: we don't set the name for the object's html file here,
            // since we want to refer to the file generated for the original
            // base (pre-'modify') object.  We might not have parsed the
            // base object yet, so we'll have to go back and fix these up
            // after we've finished parsing the entire library, since
            // we'll know the correct filenames at that point and can
            // correlate the modification entries with the base entries
            // based on the symbol name.
            SymbolTable.Modifications.Add(mod);
            CurrClassOrObj = mod;
        }

        //=====================================================================
        /// <summary>
        /// Processes a line which appears to define an intrinsic class.
        /// "intrinsic class" space identifier "'" stuff "'" ":" [identifier List]
        /// </summary>
        private void ProcessIntrinsicClass (String line)
        {
            // note: the keyboards "intrinsic class" have already been parsed
            ClassDef cr = new ClassDef(Path.GetFileName(CurrentFileName), LineNumber);
            cr.Name = GetNextToken(ref line);
            cr.Description = LastComment;
            LastComment = "";
            if (cr.Name == "")
                return;     // must not be a class after all
            int n = line.IndexOf(':');
            if (n >= 0)
            {
                line = line.Substring(n+1);
                while (line != "")      // collect base class names
                {
                    cr.BaseClasses.Add(GetNextToken(ref line));
                    GetNextToken(ref line);     // throw away comma
                }
            }

            cr.IsIntrinsic = true;
            SymbolTable.SetObjectFileName(cr);
            SymbolTable.Classes.Add(cr);
            this.CurrentSourceFile.Classes.Add(cr);
            CurrClassOrObj = cr;
        }

        //=====================================================================
        /// <summary>
        /// Processes a line which appears to define a global object.
        /// identifier ":" [identifier List]
        /// </summary>
        private void ProcessObject (String name, String line, bool isTransient, bool isAction)
        {
            // note: the identifer and colon have already been parsed
            ObjectDef od = new ObjectDef(Path.GetFileName(CurrentFileName), LineNumber);
            od.Name = name;
            od.Description = LastComment;
            od.IsTransient = isTransient;
            od.isAction = isAction;
            LastComment = "";
            if (od.Name == "")
                return;     // must not be a class after all
            line = line.Trim();
            while (line != "")      // collect base class names
            {
                String t;
                t = GetNextToken(ref line);
                if (t.Contains("Action"))
                    od.isAction = true;

                od.BaseClasses.Add(t);
                t = GetNextToken(ref line);
                if (t != ",")
                    break;
            }

            SymbolTable.SetObjectFileName(od);
            SymbolTable.GlobalObjects.Add(od);
            this.CurrentSourceFile.GlobalObjects.Add(od);
            CurrClassOrObj = od;
        }

        //=====================================================================
        /// <summary>
        /// Processes a line which appears to define a method.
        /// identifier "(" identifierList ")"
        /// </summary>
        /// <param name="name">the name of the method, which has already been parsed</param>
        private void ProcessMethod (String name, String line, bool ifcOnly)
        {
            if (CurrClassOrObj == null)
                return;
            MethodDef m = new MethodDef(Path.GetFileName(CurrentFileName),
                                        LineNumber);
            m.Name = name;
            m.IsIfcOnly = ifcOnly;
            m.Description = LastComment;
            m.ClassOrObject = CurrClassOrObj;
            if (! ParseArgList(ref line, m))
                return;

            if ((name == "dobjFor" || name == "iobjFor")
                && m.Parameters.Count == 1)
            {
                m.Name += "(" + m.Parameters[0] + ")";
                m.Parameters = null;
            }

            CurrClassOrObj.Methods.Add(m);
            LastComment = "";
        }

        //=====================================================================
        /// <summary>
        /// Processes a line which appears to define a global function.
        /// identifier "(" identifierList ")"
        /// </summary>
        /// <param name="name">the name of the function, which has already been parsed</param>
        private void ProcessFunction (String name, String line)
        {
            FunctionDef f = new FunctionDef(Path.GetFileName(CurrentFileName), LineNumber);
            f.Name = name;
            f.Description = LastComment;
            LastComment = "";
            if (! ParseArgList(ref line, f))
                return;
            this.SymbolTable.GlobalFunctions.Add(f);
            this.CurrentSourceFile.GlobalFunctions.Add(f);
        }

        //=====================================================================
        /// <summary>
        /// Processes a line which appears to define a macro.
        /// #define identifier ["(" identifierList ")"]
        /// </summary>
        private void ProcessMacro (String line)
        {
            // note: the keyword "#define" has already been parsed
            MacroDef m = new MacroDef(Path.GetFileName(CurrentFileName), LineNumber);
            m.Name = GetNextToken(ref line);
            m.Description = LastComment;
            LastComment = "";

            if (line.StartsWith("("))   // look for macro args
            {
                line = line.Substring(1).Trim();
                if (! ParseArgList(ref line, m))
                    return;
            }

            // get the body of the macro
            m.Body = line.Trim();
            if (m.Body == "\\")
                m.Body = "";
            while (line.EndsWith("\\"))
            {
                if (m.Body != "")
                    m.Body += "<br>";
                line = GetNextLine().Trim();
                m.Body += line;
            }

            SymbolTable.Macros.Add(m);
            this.CurrentSourceFile.Macros.Add(m);
        }

        //=====================================================================
        /// <summary>
        /// Destructively parses an argument list from the given string and
        /// puts the arguments in the given FunctionDef.
        /// </summary>
        /// <returns>true on success, false on error</returns>
        private bool ParseArgList (ref String line, FunctionDef f)
        {
            line = line.Trim();
            for (;;)
            {
                // if we're out of text on this line, fetch another
                if (line == "")
                    line = GetNextLine().Trim();

                // check what we have next
                String token = GetNextToken(ref line);
                if (token == ")" || token == "{")
                    break;
                if (token == "")
                    return false;
                if (token == "[")
                {
                    // [arglist] parameter - build the whole string
                    token = GetNextToken(ref line);
                    if (GetNextToken(ref line) == "]")
                        token = "[" + token + "]";
                }
                if (token == "?" && f.Parameters.Count != 0)
                    f.Parameters[f.Parameters.Count - 1] += "?";
                else if (token != ",")
                    f.Parameters.Add(token);
            }
            return true;
        }

        //=====================================================================
        /// <summary>
        /// Processes a line which appears to define a property.
        /// identifier "=" value
        /// </summary>
        /// <param name="name">the name of the property</param>
        private void ProcessProperty (String name, bool ifcOnly)
        {
            PropertyDef p;
            
            if (CurrClassOrObj == null)
                return;
            p = new PropertyDef(name, LastComment, this.CurrClassOrObj,
                                Path.GetFileName(CurrentFileName), LineNumber);
            p.IsIfcOnly = ifcOnly;
            CurrClassOrObj.Properties.Add(p);
            LastComment = "";
        }

        //=====================================================================
        /// <summary>
        /// Processes a line which appears to define one or more enums.
        /// All enums defined on a single line are placed in an EnumGroup.
        /// This means that some enums are not grouped as they ought to be,
        /// but I did say this parser is pretty stupid.
        /// "enum" identifier ["," identifier]... ";"
        /// </summary>
        private void ProcessEnums (String line)
        {
            // note: the keyword "enum" has already been parsed
            EnumGroup group = new EnumGroup(Path.GetFileName(CurrentFileName), LineNumber);
            group.Description = LastComment;
            LastComment = "";

            line = line.Trim();
            while (line != "")
            {
                String token = GetNextToken(ref line);
                if (token == ";")
                    break;
                if (token == "")
                    return;
                if (token != ",")
                {
                    EnumDef e = new EnumDef(Path.GetFileName(CurrentFileName), LineNumber);
                    e.Name = token;
                    e.Group = group;
                    group.Enums.Add(e);
                }
            }

            this.SymbolTable.EnumGroups.Add(group);
            this.CurrentSourceFile.EnumGroups.Add(group);
        }

        //=====================================================================
        /// <summary>
        /// Processes a line which appears to define a template.
        /// identifier "template" body
        /// </summary>
        /// <param name="name">the name of the template (already parsed)</param>
        private void ProcessTemplate (String name, String line)
        {
            // note: the template name and keyword "template" have already been parsed
            TemplateDef t = new TemplateDef(
                Path.GetFileName(CurrentFileName), LineNumber);
            t.Name = name;
            t.Description = LastComment;
            LastComment = "";

            // start the token body with the current (trimmed) line
            line = line.Trim();
            String tBody = line.Trim();

            // scan tokens until we find a semicolon
            for (;;)
            {
                // if necessary, read a new line
                if (line == "")
                {
                    line = GetNextLine().Trim();
                    tBody += " " + line;
                }

                // get the next token
                if (GetNextToken(ref line) == ";")
                    break;
            }

            // set the template body
            t.Body = tBody;

            // add the template to the tables
            this.SymbolTable.Templates.Add(t);
            this.CurrentSourceFile.Templates.Add(t);
        }

        //=====================================================================
        /// <summary>
        /// Processes a line which appears to be a comment, and all subsequent
        /// lines that are part of the comment.
        /// </summary>
        private void ProcessComment (String line)
        {
            int n;
            LastComment = "";

            /* check for a single-line comment */
            if ((n = line.IndexOf("*/")) >= 0)
            {
                /* pull out the single-line comment */
                LastComment = line.Substring(0, n).Trim();
            }
            else
            {
                /* process a multi-line comment */
                while (0 > (n = line.IndexOf("*/")))
                {
                    line = line.Trim();
                    if (line.StartsWith("* "))
                        line = line.Substring(2);
                    else if (line.StartsWith("*. "))
                        line = "\u0001" + line.Substring(3);
                    else if (line.StartsWith(" * "))
                        line = line.Substring(3);
                    else if (line.StartsWith(" *. "))
                        line = "\u0001" + line.Substring(4);
                    else if (line == "*" || line == " *")
                        line = "\n";
                    
                    // skip copyright headers
                    String lt = line.Trim();
                    if (!lt.StartsWith("Copyright ")
                        && !lt.StartsWith("This file is part of TADS 3")
                        && !lt.StartsWith(".  This file is part of TADS 3"))
                        LastComment += line + " ";
                    
                    line = GetNextLine();
                }
            }

            // the first comment in the file gets put in the SourceFile
            if (this.FirstComment)
            {
                this.CurrentSourceFile.Description = this.LastComment;
                this.LastComment = "";
                this.FirstComment = false;
            }
        }

        //=====================================================================
        // The Lexer

        private static Char[] WordBreakChars = " \t\r\n=(){}[]:;".ToCharArray();
        private static String[] Operators = new String[]
        {
            "(", ")", "[", "]", "=", ":", ";", "...",
            "/*", "*/", "//", "{", "}", "##", "#"
        };
        private static String SymbolChars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";

        /// <summary>
        /// Removes the next input token from the given string and returns it.
        /// </summary>
        private String GetNextToken (ref String str)
        {
            str = str.TrimStart();
            if (str == "")
                return "";

            // look for an operator
            foreach (String op in Operators)
            {
                if (str.StartsWith(op))
                {
                    str = str.Substring(op.Length);
                    return op;
                }
            }

            // look for a symbol or number
            String symbol = "";
            for (int i = 0; i < str.Length; i++)
                if (SymbolChars.IndexOf(str[i]) >= 0)
                    symbol += str[i];
                else break;
            if (symbol != "")
            {
                str = str.Substring(symbol.Length);
                return symbol;
            }

            // return next character
            String s = str.Substring(0, 1);
            str = str.Substring(1);
            return s;
        }
    }
}
