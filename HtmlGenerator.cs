/*
 *  TADS3 documentation generator.
 * 
 *  Copyright Edward L. Stauff 2003.  
 *  This code may be freely distributed and modified for non-commercial purposes. 
 *  
 */
using System;
using System.IO;
using System.Collections;

/*
 *  This file contains code to generate HTML documentation based on the
 *  information in a SymbolTable.  It is independent of the rest of the
 *  documentation generator, and can easily be replaced with a module
 *  to generate documentation in some other format.
 * 
 *  One file is generated for each source file, and for each class.
 */

namespace DocGen
{
    /// <summary>
    /// Generates documentation in HTML form.
    /// </summary>
    public class HtmlGenerator
    {
        private SymbolTable SymbolTable;
        private String TadsVersion;
        private String OriginalIntroFileName;

        const String FileDir = "file";
        const String ObjectDir = "object";
        const String IndexDir = "index";
        const String SourceDir = "source";

        const String IntroFile = "Intro.html";
        const String FileIndexFile = "FileIndex.html";
        const String ClassIndexFile = "ClassIndex.html";
        const String GrammarIndexFile = "GrammarIndex.html";
        const String ActionIndexFile = "ActionIndex.html";
        const String ObjectIndexFile = "ObjectIndex.html";
        const String MacroIndexFile = "MacroIndex.html";
        const String EnumIndexFile = "EnumIndex.html";
        const String TemplateIndexFile = "TemplateIndex.html";
        const String FunctionIndexFile = "FunctionIndex.html";
        const String SymbolTOCfile = "TOC.html";

        private String OutputDir;

        //=====================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tadsVer">the TADS3 version number</param>
        public HtmlGenerator (SymbolTable symTab, String introFile, String tadsVer, String outputDir)
        {
            this.SymbolTable = symTab;
            this.TadsVersion = tadsVer;
            this.OriginalIntroFileName = introFile;

            // standardize the format of the output directory path
            this.OutputDir = outputDir.Replace("\\", "/");
            if (! this.OutputDir.EndsWith("/"))
                this.OutputDir = this.OutputDir + "/";
        }

        //=====================================================================
        /// <summary>
        /// Returns the name of the object documentation file for the given object,
        /// relative to the OutputDir.
        /// </summary>
        private String ObjectFileName (Modification od)
        {
            return ObjectDir + "/" + od.FileName + ".html";
        }

        //=====================================================================
        /// <summary>
        /// Returns the name of the class index file for the given file,
        /// relative to the OutputDir.
        /// </summary>
        private String ClassIndexFileName (SourceFile sf)
        {
            return IndexDir + "/" + sf.ShortName + ".html";
        }

        //=====================================================================
        /// <summary>
        /// Returns the name of the file documentation file for the given file,
        /// relative to the OutputDir.
        /// </summary>
        private String FileFileName (SourceFile sf)
        {
            return FileDir + "/" + sf.ShortName + ".html";
        }

        //=====================================================================
        /// <summary>
        /// Returns the name of the file documentation file for the given file,
        /// relative to the OutputDir.
        /// </summary>
        private String FileFileName (SourceLoc s)
        {
            return FileDir + "/" + s.Name + ".html";
        }

        //=====================================================================
        /// <summary>
        /// Returns the name of the file documentation file for the given file,
        /// relative to the OutputDir.
        /// </summary>
        private String SourceFileName (SourceLoc s)
        {
            return SourceDir + "/" + s.Name + ".html";
        }

        //=====================================================================
        /// <summary>
        /// Returns the name of the file documentation file for the given file,
        /// relative to the OutputDir.
        /// </summary>
        private String SourceFileName (SourceFile s)
        {
            return SourceDir + "/" + s.ShortName + ".html";
        }

        //=====================================================================
        /// <summary>
        /// Writes everything to the given directory.
        /// </summary>
        public void WriteAll ()
        {
            // make sure directories exist

            Directory.CreateDirectory(this.OutputDir);
            Directory.CreateDirectory(this.OutputDir + FileDir);
            Directory.CreateDirectory(this.OutputDir + SourceDir);
            Directory.CreateDirectory(this.OutputDir + ObjectDir);
            Directory.CreateDirectory(this.OutputDir + IndexDir);
        

            foreach (ClassDef c in this.SymbolTable.Classes)
                this.WriteClassFile(
                    this.OutputDir + this.ObjectFileName(c), c);

            foreach (ObjectDef o in this.SymbolTable.GlobalObjects)
                this.WriteObjectFile(
                    this.OutputDir + this.ObjectFileName(o), o);

            foreach (GrammarProd g in this.SymbolTable.GrammarProds)
                this.WriteGrammarFile(
                    this.OutputDir + this.ObjectFileName(g), g);

            foreach (SourceFile f in this.SymbolTable.Files)
            {
                this.WriteFileFile(this.OutputDir + this.FileFileName(f), f);
                this.WriteClassIndex(f);
            }

            {
                StreamReader r = new StreamReader(OriginalIntroFileName);
                StreamWriter w = new StreamWriter(OutputDir + IntroFile);
                String vsn = (TadsVersion == null ? "3" : TadsVersion);
                String date = DateTime.Now.ToShortDateString();
                String line;

                while ((line = r.ReadLine()) != null)
                {
                    line = line.Replace("$$VERSION$$", vsn);
                    line = line.Replace("$$DATE$$", date);
                    w.WriteLine(line);
                }
                w.Close();
            }

            this.WriteMainFile();
            this.WriteTopFrameFile();
            this.WriteFileIndex();
            this.WriteClassIndex();
            this.WriteGrammarIndex();
            this.WriteActionIndex();
            this.WriteObjectIndex();
            this.WriteMacroIndex();
            this.WriteEnumIndex();
            this.WriteTemplateIndex();
            this.WriteFunctionIndex();

            ArrayList symbols = this.CollectAllSymbols();
//          this.WriteSymbolIndex(symbols);
            this.WriteSymbolIndexes(symbols);
            this.WriteSymbolIndexTOC();

            this.CopySourceFiles();
        }

        //=====================================================================
        /// <summary>
        /// Writes the main "index.html" file.
        /// </summary>
        public void WriteMainFile ()
        {
            StreamWriter w = new StreamWriter(this.OutputDir + "index.html");
            w.WriteLine("<html>\r\n<head>"
                        + "<title>Adv3Lite Library Reference Manual</title>"
                        + "</head>");
            w.WriteLine("<FRAMESET rows=\"33,*\">");
            w.WriteLine("<FRAME SRC=\"TopFrame.html\" NAME=top "
                        + "SCROLLING=no MARGINWIDTH=0 "
                        + "MARGINHEIGHT=5>");
            w.WriteLine("<FRAMESET COLS=\"150,*\">");

            w.WriteLine("<FRAMESET rows=\"300,*\">");
            w.WriteLine("<FRAME name=files SRC=\"" + FileIndexFile + "\">");
            w.WriteLine("<FRAME name=classes SRC=\"" + ClassIndexFile + "\">");
            w.WriteLine("</FRAMESET>");

            w.WriteLine("<FRAME name=main SRC=\"" + IntroFile +"\">");
            w.WriteLine("</FRAMESET>");
            w.WriteLine("</FRAMESET>");
            w.WriteLine("</html>");
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes the file containing the top frame.
        /// </summary>
        public void WriteTopFrameFile ()
        {
            StreamWriter w = new StreamWriter(this.OutputDir + "TopFrame.html");
            w.WriteLine("<html>\r\n<head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"libref.css\">"
                        + "</head><body>");
            w.WriteLine("<table class=hdr><tr>");

            w.WriteLine("<td>" + Hyperlink(IntroFile,          "main",
                                           "<i>Intro</i>"));
            w.WriteLine("<td>" + Hyperlink(ClassIndexFile,     "classes",
                                           "<i>Classes</i>"));
            w.WriteLine("<td>" + Hyperlink(ActionIndexFile,    "classes",
                                           "<i>Actions</i>"));
            w.WriteLine("<td>" + Hyperlink(GrammarIndexFile,   "classes",
                                           "<i>Grammar</i>"));
            w.WriteLine("<td>" + Hyperlink(ObjectIndexFile,    "classes",
                                           "<i>Objects</i>"));
            w.WriteLine("<td>" + Hyperlink(FunctionIndexFile,  "classes",
                                           "<i>Functions</i>"));
            w.WriteLine("<td>" + Hyperlink(MacroIndexFile,     "classes",
                                           "<i>Macros</i>"));
            w.WriteLine("<td>" + Hyperlink(EnumIndexFile,      "classes",
                                           "<i>Enums</i>"));
            w.WriteLine("<td>" + Hyperlink(TemplateIndexFile,  "classes",
                                           "<i>Templates</i>"));
            w.WriteLine("<td>" + Hyperlink(IndexDir + "/" + SymbolTOCfile,
                                           "classes",  "<i>all symbols</i>"));

            w.WriteLine("</table></body></html>");
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes the index of source files.
        /// </summary>
        public void WriteFileIndex ()
        {
            StreamWriter w = new StreamWriter(this.OutputDir + FileIndexFile);
            w.WriteLine("<html><head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"libref.css\">"
                        + "<title>Adv3Lite: Index of Files</title>"
                        + "</head><body>"
                        + "<h2>Files</h2>");

            foreach (SourceFile f in this.SymbolTable.Files)
            {
                w.Write(Hyperlink(this.FileFileName(f), f.ShortName, "main",
                                  "<code>" + f.ShortName + "</code>")
                        + " &nbsp; ");
                w.Write(Hyperlink(this.ClassIndexFileName(f), "classes",
                                  "<i>classes</i>") + " <br>");
            }

            w.WriteLine("</body></html>");
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes the index of all classes.
        /// </summary>
        public void WriteClassIndex ()
        {
            StreamWriter w = new StreamWriter(this.OutputDir + ClassIndexFile);
            w.WriteLine("<html><head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"libref.css\">"
                        + "<title>Adv3Lite: Index of Classes</title>"
                        + "</head><body>"
                        + "<h2>Classes</h2>");
            foreach (ClassDef c in this.SymbolTable.Classes)
            {
                if (!c.IsGrammar && !c.IsAction)
                    w.WriteLine(Hyperlink(this.ObjectFileName(c), "main",
                                          "<code>" + c.Name + "</code>")
                                + "<br>");
            }
            w.WriteLine("</body></html>");
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes the index of all actions.
        /// </summary>
        public void WriteActionIndex ()
        {
            StreamWriter w = new StreamWriter(this.OutputDir + ActionIndexFile);
            w.WriteLine("<html><head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"libref.css\">"
                        + "<title>Adv3Lite: Index of Actions</title>"
                        + "</head><body>"
                        + "<h2>Actions</h2>");
            foreach (ObjectDef c in this.SymbolTable.GlobalObjects)
            {
                if (c.isAction)
                    w.WriteLine(Hyperlink(this.ObjectFileName(c), "main",
                                          "<code>" + c.Name + "</code>")
                                + "<br>");
            }
            w.WriteLine("</body></html>");
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes the index of all 'grammar' definitions.
        /// </summary>
        public void WriteGrammarIndex ()
        {
            StreamWriter w = new StreamWriter(this.OutputDir + GrammarIndexFile);
            w.WriteLine("<html><head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"libref.css\">"
                        + "<title>Adv3Lite: Index of Grammar Rules</title>"
                        + "</head><body>"
                        + "<h2>Grammar&nbsp;Rules</h2>");

            foreach (GrammarProd g in this.SymbolTable.GrammarProds)
            {
                w.WriteLine(Hyperlink(this.ObjectFileName(g), "main",
                                      "<code>" + g.Name + "</code>")
                            + "<br>");
            }
//          foreach (ClassDef c in this.SymbolTable.Classes)
//          {
//              if (c.IsGrammar)
//                  w.WriteLine(Hyperlink(this.ObjectFileName(c), "main",
//                                        "<code> "+ c.Name + "</code>")
//                              + "<br>");
//            }
            w.WriteLine("</body></html>");
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes the index of all global objects.
        /// </summary>
        public void WriteObjectIndex ()
        {
            StreamWriter w = new StreamWriter(this.OutputDir + ObjectIndexFile);
            w.WriteLine("<html><head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"libref.css\">"
                        + "<title>Adv3Lite: Index of Objects</title>"
                        + "</head><body>"
                        + "<h2>Objects</h2>");
            foreach (ObjectDef o in this.SymbolTable.GlobalObjects)
                w.WriteLine(Hyperlink(this.ObjectFileName(o), "main",
                                      "<code>" + o.Name + "</code>") + "<br>");
            w.WriteLine("</body></html>");
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes an index of classes for a single source file.
        /// </summary>
        public void WriteClassIndex (SourceFile sf)
        {
            StreamWriter w = new StreamWriter(this.OutputDir
                                              + this.ClassIndexFileName(sf));
            w.WriteLine("<html><head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"../libref.css\">"
                        + "<title>Adv3Lite: Index of Classes in "
                        + sf.ShortName + "</title>"
                        + "</head><body>"
                        + "<h2>Classes</h2>");
            if (sf.Classes.Count == 0)
                w.WriteLine("<i>(none)</i><br>");
            
            foreach (ClassDef c in sf.Classes)
                w.WriteLine(Hyperlink("../" + this.ObjectFileName(c), c.Name,
                                      "main", "<code>" + c.Name + "</code>")
                            + "<br>");
            
            w.WriteLine("</body></html>");
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes the index of all macros.
        /// </summary>
        public void WriteMacroIndex ()
        {
            StreamWriter w = new StreamWriter(this.OutputDir + MacroIndexFile);
            w.WriteLine("<html><head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"libref.css\">"
                        + "<title>Adv3Lite: Index of Macros</title>"
                        + "</head><body>"
                        + "<h2>Macros</h2>");
            foreach (MacroDef m in this.SymbolTable.Macros)
                w.WriteLine(Hyperlink(this.FileFileName(m.Source),
                                      m.Name, "main",
                                      "<code>" + m.Name + "</code>") + "<br>");
            w.WriteLine("</body></html>");
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes the index of all enums.
        /// </summary>
        public void WriteEnumIndex ()
        {
            StreamWriter w = new StreamWriter(this.OutputDir + EnumIndexFile);
            w.WriteLine("<html><head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"libref.css\">"
                        + "<title>Adv3Lite: Index of Enums</title>"
                        + "</head><body>"
                        + "<h2>Enums</h2>");
            foreach (EnumDef e in this.SymbolTable.Enums)
                w.WriteLine(Hyperlink(this.FileFileName(e.Source), e.Name,
                                      "main", "<code>" + e.Name + "</code>")
                            + "<br>");
            w.WriteLine("</body></html>");
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes the index of all templates.
        /// </summary>
        public void WriteTemplateIndex ()
        {
            StreamWriter w = new StreamWriter(this.OutputDir + TemplateIndexFile);
            w.WriteLine("<html><head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"libref.css\">"
                        + "<title>Adv3Lite: Index of Templates</title>"
                        + "</head><body>"
                        + "<h2>Templates</h2>");
            foreach (TemplateDef t in this.SymbolTable.Templates)
                w.WriteLine(Hyperlink(this.FileFileName(t.Source), t.Name,
                                      "main", "<code>" + t.Name + "</code>")
                            + "<br>");
            w.WriteLine("</body></html>");
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes the index of all global functions.
        /// </summary>
        public void WriteFunctionIndex ()
        {
            StreamWriter w = new StreamWriter(this.OutputDir + FunctionIndexFile);
            w.WriteLine("<html><head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"libref.css\">"
                        + "<title>Adv3Lite: Index of Functions</title>"
                        + "</head><body>"
                        + "<h2>Global&nbsp;Functions</h2>");
            foreach (FunctionDef f in this.SymbolTable.GlobalFunctions)
                w.WriteLine(Hyperlink(this.FileFileName(f.Source), f.Name,
                                      "main", "<code>" + f.Name + "</code>")
                            + "<br>");
            w.WriteLine("</body></html>");
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes a file that documents a single source file.
        /// </summary>
        public void WriteFileFile (String fileName, SourceFile sf)
        {
            System.Console.Out.WriteLine("writing " + fileName);

            StreamWriter w = new StreamWriter(fileName);
            w.WriteLine("<html>\r\n<head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"../libref.css\">"
                        + "<title>" + sf.ShortName + "</title>"
//                      + "<script type=\"text/javascript\" "
//                      + "src=\"../libref.js\"></script>"
//                      + "</head><body onload=\"frmLd()\">");
                        + "</head><body>");

            this.WriteFileTitle(w, "file", sf.ShortName,
                                Hyperlink("../" + this.SourceFileName(sf),
                                          "source file"), null);

            w.WriteLine("<table class=nav><tr>");

            WriteFileTOCentry(w, "Classes", "_ClassSummary_", null);
            if (! sf.IsHeaderFile)
                WriteFileTOCentry(w, "Objects", "_ObjectSummary_", null);
            WriteFileTOCentry(w, "Functions", "_FunctionSummary_",
                              "_Functions_");

            if (sf.IsHeaderFile)
            {
                WriteFileTOCentry(w, "Macros", "_MacroSummary_", "_Macros_");
                WriteFileTOCentry(w, "Enums", "_EnumSummary_", "_Enums_");
                WriteFileTOCentry(w, "Templates", "_TemplateSummary_",
                                  "_Templates_");
            }

            w.WriteLine("</table><div class=fdesc>");
        
            if ((sf.Description == null) || (sf.Description == ""))
                w.WriteLine("<i>no description available</i>");
            else
                w.WriteLine(EncodeEntities(sf.Description));
            w.WriteLine("</div>");

            this.WriteMajorHeading(w, "_ClassSummary_",
                                   "Summary of Classes", "");
            if (sf.Classes.Count == 0)
                w.WriteLine("<i>(none)</i>");
            else
            {
                w.WriteLine("<code>");
                foreach (ClassDef c in sf.Classes)
                    this.WriteSummaryEntry(w, null, c.Name, "../" + this.ObjectFileName(c));
                w.WriteLine("</code>");
            }
            
            if (! sf.IsHeaderFile)
            {
                this.WriteMajorHeading(w, "_ObjectSummary_",
                                       "Summary of Global Objects", "");
                if (sf.GlobalObjects.Count == 0)
                    w.WriteLine("<i>(none)</i>");
                else
                {
                    w.WriteLine("<code>");
                    foreach (ObjectDef o in sf.GlobalObjects)
                        this.WriteSummaryEntry(w, null, o.Name, "../" + this.ObjectFileName(o));
                    w.WriteLine("</code>");
                }
            }

            this.WriteMajorHeading(w, "FunctionSummary_",
                                   "Summary of Global Functions", "");
            this.WriteSymbolSummary(w, sf.GlobalFunctions, "");

            if (sf.IsHeaderFile)
            {
                this.WriteMajorHeading(w, "_MacroSummary_",
                                       "Summary of Macros", "");
                this.WriteSymbolSummary(w, sf.Macros, "");

                this.WriteMajorHeading(w, "_EnumSummary_",
                                       "Summary of Enums", "");
                this.WriteSymbolSummary(w, sf.Enums, "");

                this.WriteMajorHeading(w, "_TemplateSummary_",
                                       "Summary of Templates", "");
                this.WriteSymbolSummary(w, sf.Templates, "");
            }

            this.WriteMajorHeading(w, "_Functions_", "Global Functions", "");
            if (sf.GlobalFunctions.Count == 0)
                w.WriteLine("<i>(none)</i>");
            else
            {
                foreach (FunctionDef f in sf.GlobalFunctions)
                    this.WriteFunction(w, f);
            }
        
            if (sf.IsHeaderFile)
            {
                this.WriteMajorHeading(w, "_Macros_", "Macros", "");
                foreach (MacroDef m in sf.Macros)
                    this.WriteMacro(w, m);
        
                this.WriteMajorHeading(w, "_Enums_", "Enums", "");
                if (sf.EnumGroups.Count == 0)
                    w.WriteLine("<i>(none)</i>");
                else
                {
                    foreach (EnumGroup g in sf.EnumGroups)
                        this.WriteEnums(w, g);
                }
        
                this.WriteMajorHeading(w, "_Templates_", "Templates", "");
                if (sf.Templates.Count == 0)
                    w.WriteLine("<i>(none)</i>");
                else
                {
                    foreach (TemplateDef t in sf.Templates)
                        this.WriteTemplate(w, t);
                }
            }
        
            this.WriteHtmlFooter(w);
            w.Close();
        }

        //=====================================================================
        
        private void WriteFileTOCentry (StreamWriter w, String type, String summaryLink, String detailsLink)
        {
            w.Write("<td><b>" + type + "</b>");
            if (summaryLink == null)
                w.Write("<br>&nbsp;");
            else w.Write("<br>" + Hyperlink(null, summaryLink, null, "Summary"));
            if (detailsLink == null)
                w.Write("<br>&nbsp;");
            else
                w.Write("<br>" + Hyperlink(null, detailsLink, null, "Details"));
        }

        //=====================================================================
        /// <summary>
        /// Writes a file that documents a single class.
        /// </summary>
        public void WriteClassFile (String fileName, ClassDef cd)
        {
            System.Console.Out.WriteLine("writing " + fileName);

            StreamWriter w = new StreamWriter(fileName);
            w.WriteLine("<html>\r\n<head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"../libref.css\">"
                        + "<title>" + cd.Name + "</title>"
//                      + "<script type=\"text/javascript\" "
//                      + "src=\"../libref.js\"></script>"
//                      + "</head><body onload=\"frmLd()\">");
                        + "</head><body>");

            this.WriteFileTitle(w, (cd.IsGrammar ? "grammar" : "class"),
                                cd.Name, this.FileLinks(cd.Source),
                                cd.Modifications);

            w.WriteLine("<table class=nav><tr>");

            w.WriteLine("<td>" + Hyperlink(null, "_SuperClassTree_", null,
                                           "Superclass<br>Tree"));
            if (!cd.IsGrammar)
            {
                w.WriteLine("<td>" + Hyperlink(null, "_SubClassTree_", null,
                                               "Subclass<br>Tree"));
                w.WriteLine("<td>" + Hyperlink(null, "_ObjectSummary_", null,
                                               "Global<br>Objects"));
            }
            w.WriteLine("<td>" + Hyperlink(null, "_PropSummary_", null,
                                           "Property<br>Summary"));
            w.WriteLine("<td>" + Hyperlink(null, "_MethodSummary_", null,
                                           "Method<br>Summary"));
            w.WriteLine("<td>" + Hyperlink(null, "_Properties_", null,
                                           "Property<br>Details"));
            w.WriteLine("<td>" + Hyperlink(null, "_Methods_", null,
                                           "Method<br>Details"));

            w.WriteLine("</table><div class=fdesc>");

            if (cd.Description == "")
                w.WriteLine("<i>no description available</i>");
            else
                w.WriteLine(EncodeEntities(cd.Description));

            foreach (Modification mod in cd.ModificationMods)
            {
                if (mod.Description != "")
                {
                    w.WriteLine("<p><i>Modified in " + FileLinks(mod.Source)
                                + ":</i><br>");
                    w.WriteLine(EncodeEntities(mod.Description));
                }
            }

            w.WriteLine("<p>");

            WriteObjectDecl(w, cd, false);

            w.WriteLine("</div>");
/*
            w.Write("Subclasses: <code>");
            foreach (ClassDef sc in cd.SubClasses)
                w.Write(" &nbsp; <a href=\"" + sc.Name + ".html\">" + sc.Name + "</a>");
            w.WriteLine("</code>");
            w.WriteLine("<p>");
*/
            this.WriteMajorHeading(w, "_SuperClassTree_", "Superclass Tree",
                                   "(in declaration order)");
            this.WriteParentTree(w, cd);

            if (!cd.IsGrammar)
            {
                this.WriteMajorHeading(w, "_SubClassTree_",
                                       "Subclass Tree", "");
                this.WriteChildTree(w, cd);
                
                this.WriteMajorHeading(w, "_ObjectSummary_",
                                       "Global Objects", "");
                if (cd.GlobalObjects.Count == 0)
                    w.WriteLine("<i>(none)</i>");
                else
                {
                    w.WriteLine("<code>");
                    foreach (ObjectDef od in cd.GlobalObjects)
                        w.WriteLine(Hyperlink(
                            "../" + this.ObjectFileName(od), od.Name)
                                    + "&nbsp; ");
                    w.WriteLine("</code>");
                }
            }

            this.WriteMajorHeading(w, "_PropSummary_",
                                   "Summary of Properties", "");
            this.WritePropertySummary(w, cd);

            this.WriteMajorHeading(w, "_MethodSummary_",
                                   "Summary of Methods", "");
            this.WriteMethodSummary(w, cd);

            this.WriteMajorHeading(w, "_Properties_", "Properties", "");
            if (cd.Properties.Count == 0)
                w.WriteLine("<i>(none)</i>");
            else
            {
                foreach (PropertyDef p in cd.Properties)
                    this.WriteProperty(w, p);
            }

            this.WriteMajorHeading(w, "_Methods_", "Methods", "");
            if (cd.Methods.Count == 0)
                w.WriteLine("<i>(none)</i>");
            else
            {
                foreach (MethodDef m in cd.Methods)
                    this.WriteMethod(w, m);
            }

            this.WriteHtmlFooter(w);
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes a file that documents a GrammarProd object
        /// </summary>
        public void WriteGrammarFile (String fileName, GrammarProd g)
        {
            System.Console.Out.WriteLine("writing " + fileName);

            StreamWriter w = new StreamWriter(fileName);
            w.WriteLine("<html>\r\n<head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"../libref.css\">"
                        + "<title>" + g.Name + "</title>"
//                      + "<script type=\"text/javascript\" "
//                      + "src=\"../libref.js\"></script>"
//                      + "</head><body onload=\"frmLd()\">");
                        + "</head><body>");

            this.WriteFileTitle(w, "GrammarProd", g.Name, null, null);

            g.MatchObjects.Sort();

            foreach (ClassDef c in g.MatchObjects)
            {
                WriteObjectDecl(w, c, true);

                if (c.GrammarRule != null)
                    w.WriteLine("<div class=gramrule>"
                                + EncodeEntities(c.GrammarRule)
                                + "</div>");
            }

            this.WriteHtmlFooter(w);
            w.Close();
        }


        //=====================================================================
        /// <summary>
        /// Writes a file that documents a single global object.
        /// </summary>
        public void WriteObjectFile (String fileName, ObjectDef od)
        {
            System.Console.Out.WriteLine("writing " + fileName);

            StreamWriter w = new StreamWriter(fileName);
            w.WriteLine("<html>\r\n<head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"../libref.css\">"
                        + "<title>" + od.Name + "</title>"
//                      + "<script type=\"text/javascript\" "
//                      + "src=\"../libref.js\"></script>"
//                      + "</head><body onload=\"frmLd()\">");
                        + "</head><body>");

            this.WriteFileTitle(w, "object", od.Name,
                                this.FileLinks(od.Source), null);

            w.WriteLine("<table class=nav><tr>");

            w.WriteLine("<td>" + Hyperlink(null, "_SuperClassTree_", null,
                                           "Superclass<br>Tree"));
            w.WriteLine("<td>" + Hyperlink(null, "_PropSummary_", null,
                                           "Property<br>Summary"));
            w.WriteLine("<td>" + Hyperlink(null, "_MethodSummary_", null,
                                           "Method<br>Summary"));
            w.WriteLine("<td>" + Hyperlink(null, "_Properties_", null,
                                           "Property<br>Details"));
            w.WriteLine("<td>" + Hyperlink(null, "_Methods_", null,
                                           "Method<br>Details"));

            w.WriteLine("</table><div class=fdesc>");

            if (od.Description == "")
                w.WriteLine("<i>no description available</i>");
            else
                w.WriteLine(EncodeEntities(od.Description));

            w.WriteLine("<p>");
            WriteObjectDecl(w, od, false);
            w.WriteLine("</div>");

            this.WriteMajorHeading(w, "_SuperClassTree_", "Superclass Tree",
                                   "(in declaration order)");
            this.WriteParentTree(w, od);

            this.WriteMajorHeading(w, "_PropSummary_",
                                   "Summary of Properties", "");
            this.WritePropertySummary(w, od);

            this.WriteMajorHeading(w, "_MethodSummary_",
                                   "Summary of Methods", "");
            this.WriteMethodSummary(w, od);

            this.WriteMajorHeading(w, "_Properties_", "Properties", "");
            if (od.Properties.Count == 0)
                w.WriteLine("<i>(none)</i>");
            else
            {
                foreach (PropertyDef p in od.Properties)
                    this.WriteProperty(w, p);
            }

            this.WriteMajorHeading(w, "_Methods_", "Methods", "");
            if (od.Methods.Count == 0)
                w.WriteLine("<i>(none)</i>");
            else
            {
                foreach (MethodDef m in od.Methods)
                    this.WriteMethod(w, m);
            }

            this.WriteHtmlFooter(w);
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes a class definition line - object: class1, class2, ...
        /// </summary>
        public void WriteObjectDecl (StreamWriter w, ObjectOrClass o,
                                     bool isExt)
        {
            ClassDef c = o as ClassDef;
            ObjectDef obj = o as ObjectDef;

            // if it's a class with a macro declaration, write the original
            if (c != null && c.OrigDef != null)
                w.Write("<code>"  + c.OrigDef
                        + " &nbsp;&nbsp;&nbsp;&nbsp; "
                        + "<i>// original source text"
                        + "</i></code><br>");

            // it it's external, put it in a table with a link to the file
            if (isExt)
                w.Write("<table class=decl><tr><td align=left>");

            w.Write("<code>");

            if (c != null)
            {
                if (c.IsIntrinsic)
                    w.Write("intrinsic ");
                w.WriteLine((c.IsGrammar ? "grammar" : "class") + " ");
            }
            else if (obj != null)
            {
                if (obj.IsTransient)
                    w.Write("transient ");
            }

            // if it's external, link the class name to its file
            if (isExt)
            {
                w.Write("<b>"
                        + Hyperlink("../" + this.ObjectFileName(o), o.Name)
                        + "</b> : ");
            }
            else if (c != null && c.GrammarProdObj != null)
            {
                int n;
                
                // link to the GrammarProd object definition
                n = c.Name.IndexOf('(');
                String gpName = (n < 0 ? c.Name : c.Name.Substring(0, n));
                String tag = (n < 0 ? "" : c.Name.Substring(n));

                w.Write("<b>"
                        + Hyperlink("../"
                                    + this.ObjectFileName(c.GrammarProdObj),
                                    gpName)
                        + tag + "</b> : ");
            }
            else
                w.Write("<b>" + o.Name + "</b> : ");
            
            foreach (object bc in o.BaseClasses)
            {
                if (bc is ObjectOrClass)
                    w.Write(" &nbsp; " + Hyperlink(
                        "../"
                        + this.ObjectFileName((ObjectOrClass)bc),
                        ((ObjectOrClass)bc).Name));
                else
                    w.Write(" &nbsp; " + bc.ToString());
            }

            if (c != null)
            {
                if (c.OrigDef != null)
                    w.Write(" &nbsp;&nbsp;&nbsp;&nbsp; "
                            + "<i>// after macro expansion</i>");
            }

            w.WriteLine("</code>");

            if (isExt)
                w.Write("<td align=right><code>" + FileLinks(o.Source)
                        + "</code></table>");

        }

        //=====================================================================
        /// <summary>
        /// Writes a summary of properties.
        /// </summary>
        public void WritePropertySummary (StreamWriter w, ObjectOrClass cd)
        {
            int count = 0;

            ArrayList propsWritten = new ArrayList();

            w.WriteLine("<code>");
            foreach (PropertyDef p in cd.Properties)
            {
                this.WriteSummaryEntry(w, p.Name, p.Name, "");
                propsWritten.Add(p.Name);
                count++;
            }
            w.WriteLine("</code><p>");

            foreach (object o in cd.BaseClasses)
            {
                ObjectOrClass bc = o as ObjectOrClass;
                if (bc != null)
                    count += this.WriteInheritedPropertySummary(w, bc, propsWritten);
            }

            if (count == 0)
                w.WriteLine("<i>(none)</i>");
        }

        //=====================================================================
        /// <summary>
        /// Writes a summary of inherited properties.
        /// </summary>
        /// <param name="propsWritten">list of properties already written</param>
        /// <returns>the number of properties written</returns>
        private int WriteInheritedPropertySummary (StreamWriter w, ObjectOrClass cd, ArrayList propsWritten)
        {
            int count = 0;

            ArrayList propsToWrite = this.GetUnwrittenProps(cd, propsWritten);
            if (propsToWrite.Count > 0)
                w.WriteLine("<p>Inherited from <code>" + cd.Name
                            + "</code> :<br>");

            w.WriteLine("<code>");
            foreach (PropertyDef p in propsToWrite)
            {
                this.WriteSummaryEntry(w, p.Name, p.Name, "../" + this.ObjectFileName(cd));
                propsWritten.Add(p.Name);
                count++;
            }
            w.WriteLine("</code><p>");

            foreach (object o in cd.BaseClasses)
            {
                ObjectOrClass bc = o as ObjectOrClass;
                if (bc != null)
                    count += this.WriteInheritedPropertySummary(w, bc, propsWritten);
            }

            return count;
        }

        //=====================================================================
        /// <summary>
        /// Returns a list of the properties in the given class which are not
        /// in the given list of properties already written.
        /// </summary>
        private ArrayList GetUnwrittenProps (ObjectOrClass cd, ArrayList propsWritten)
        {
            ArrayList retList = new ArrayList();

            foreach (PropertyDef p in cd.Properties)
            {
                if (! propsWritten.Contains(p.Name))
                {
                    propsWritten.Add(p);
                    retList.Add(p);
                }
            }

            return retList;
        }

        //=====================================================================
        /// <summary>
        /// Writes a summary of methods.
        /// </summary>
        public void WriteMethodSummary (StreamWriter w, ObjectOrClass cd)
        {
            int count = 0;

            ArrayList methodsWritten = new ArrayList();

            w.WriteLine("<code>");
            foreach (MethodDef m in cd.Methods)
            {
                this.WriteSummaryEntry(w, m.Name, m.Name, "");
                methodsWritten.Add(m.Name);
                count++;
            }
            w.WriteLine("</code><p>");

            foreach (object o in cd.BaseClasses)
            {
                ObjectOrClass bc = o as ObjectOrClass;
                if (bc != null)
                    count += this.WriteInheritedMethodSummary(w, bc, methodsWritten);
            }

            if (count == 0)
                w.WriteLine("<i>(none)</i>");
        }

        //=====================================================================
        /// <summary>
        /// Writes a summary of inherited methods.
        /// </summary>
        /// <param name="propsWritten">list of methods already written</param>
        /// <returns>the number of properties written</returns>
        private int WriteInheritedMethodSummary (StreamWriter w, ObjectOrClass cd, 
                                    ArrayList methodsWritten)
        {
            int count = 0;

            ArrayList methodsToWrite = this.GetUnwrittenMethods(cd, methodsWritten);
            if (methodsToWrite.Count > 0)
                w.WriteLine("<p>Inherited from <code>" + cd.Name + "</code> :<br>");

            w.WriteLine("<code>");
            foreach (MethodDef m in methodsToWrite)
            {
                this.WriteSummaryEntry(w, m.Name, m.Name, "../" + this.ObjectFileName(cd));
                methodsWritten.Add(m.Name);
                count++;
            }
            w.WriteLine("</code><p>");

            foreach (object o in cd.BaseClasses)
            {
                ObjectOrClass bc = o as ObjectOrClass;
                if (bc != null)
                    count += this.WriteInheritedMethodSummary(w, bc, methodsWritten);
            }

            return count;
        }

        //=====================================================================
        /// <summary>
        /// Returns a list of the methods in the given class which are not
        /// in the given list of methods already written.
        /// </summary>
        private ArrayList GetUnwrittenMethods (ObjectOrClass cd, ArrayList methodsWritten)
        {
            ArrayList retList = new ArrayList();

            foreach (MethodDef m in cd.Methods)
            {
                if (! methodsWritten.Contains(m.Name))
                {
                    methodsWritten.Add(m);
                    retList.Add(m);
                }
            }

            return retList;
        }

        //=====================================================================
        /// <summary>
        /// Writes a summary of symbols.
        /// </summary>
        public void WriteSymbolSummary (StreamWriter w, ArrayList symbols, String fileName)
        {
            if (symbols.Count == 0)
                w.WriteLine("<i>(none)</i>");
            else
            {
                w.WriteLine("<code>");
                foreach (Symbol s in symbols)
                    this.WriteSummaryEntry(w, s.Name, s.Name, fileName);
                w.WriteLine("</code><p>");
            }
        }

        //=====================================================================
        /// <summary>
        /// Writes an entry in a summary.
        /// </summary>
        /// <param name="anchor">the html anchor (hyperlink) label name</param>
        /// <param name="name">the name of the symbol</param>
        /// <param name="fileName">the hyperlink file name</param>
        private void WriteSummaryEntry (StreamWriter w, String anchor, String name, String fileName)
        {
        //  w.Write("<a href=\"" + fileName);
        //  if (anchor != null)
        //      w.Write("#" + anchor);
        //  w.WriteLine("\">" + name + "</a>&nbsp;");
            w.WriteLine(Hyperlink(fileName, anchor, null, name) + "&nbsp; ");
        }

        //=====================================================================
        /// <summary>
        /// Writes a method description.
        /// </summary>
        public void WriteMethod (StreamWriter w, MethodDef md)
        {
            this.WriteThingWithDescription(
                w, md.Name, md.Parameters, null, md.Description, 
                md.Overridden, md.Source, md.Modifications,
                md.ModificationMods, md.IsIfcOnly);
        }

        //=====================================================================
        /// <summary>
        /// Writes a property description.
        /// </summary>
        public void WriteProperty (StreamWriter w, PropertyDef p)
        {
            this.WriteThingWithDescription(
                w, p.Name, null, null, p.Description,
                p.Overridden, p.Source,
                null, null, p.IsIfcOnly);
        }

        //=====================================================================
        /// <summary>
        /// Writes a global function description.
        /// </summary>
        public void WriteFunction (StreamWriter w, FunctionDef f)
        {
            this.WriteThingWithDescription(
                w, f.Name, f.Parameters, null, f.Description, 
                false, f.Source, null, null, false);
        }

        //=====================================================================
        /// <summary>
        /// Writes a macro description.
        /// </summary>
        public void WriteMacro (StreamWriter w, MacroDef m)
        {
            this.WriteThingWithDescription(
                w, m.Name, (m.Parameters.Count == 0) ? null : m.Parameters, 
                m.Body, m.Description, false, m.Source,
                null, null, false);
        }

        //=====================================================================
        /// <summary>
        /// Writes a template description.
        /// </summary>
        public void WriteTemplate (StreamWriter w, TemplateDef t)
        {
            this.WriteThingWithDescription(
                w, t.Name, null, t.Body, t.Description, 
                false, t.Source, null, null, false);
        }

        //=====================================================================
        /// <summary>
        /// Writes a description of a group of enums.
        /// </summary>
        public void WriteEnums (StreamWriter w, EnumGroup group)
        {
            foreach (EnumDef e in group.Enums)
                w.WriteLine("<a name=\"" + e.Name + "\"></a>");

            w.Write("<table class=decl><tr><td><code>");

            foreach (EnumDef e in group.Enums)
                w.Write(e.Name + " &nbsp; ");

            w.WriteLine("</code>");

            w.Write("<td align=right><code>" + this.FileLinks(group.Source)
                    + "</code></table>");

            w.Write("<div class=desc>");
            if (group.Description == "")
                w.Write("<i>no description available</i>");
            else
                w.Write(EncodeEntities(group.Description));
            w.WriteLine("</div>");
        }

        //=====================================================================
        /// <summary>
        /// Common code for writing descriptions of things.
        /// </summary>
        /// <param name="name">the name of the symbol</param>
        /// <param name="args">arguments for methods and macros, or null</param>
        /// <param name="body">the body of the macro or template, or null</param>
        /// <param name="descr">the description</param>
        /// <param name="overridden">does this symbol override a symbol in a base class?</param>
        /// <param name="src">where the symbol was defined</param>
        /// <param name="linkSource">whether to make the source file reference a hyperlink</param>
        public void WriteThingWithDescription (
            StreamWriter w, String name, ArrayList args, 
            String body, String descr, bool overridden,
            SourceLoc src, ArrayList otherSources,
            ArrayList modMethods, bool ifcOnly)
        {
            w.WriteLine("<a name=\"" + name + "\"></a>");
            w.Write("<table class=decl><tr><td>");
            w.Write("<code>" + (ifcOnly ? "// " : "") + name);
            if (args != null)
            {
                String s = "";
                foreach (String a in args)
                    s += ", " + a;
                if (s.Length == 0)
                    s = " ";
                else
                    s = s.Substring(2);
                w.Write(" (" + s + ")");
            }
            w.Write("</code>");

            if (overridden)
                w.Write("<span class=rem>OVERRIDDEN</span>");
            if (ifcOnly)
                w.Write("<span class=rem>Interface description only</span>");

            w.Write("<td align=right><code>" + this.FileLinks(src));

            if (otherSources != null)
                foreach (SourceLoc sl in otherSources)
                    w.Write(", " + this.FileLinks(sl));

            w.Write("</table>");

            w.Write("<div class=desc>");

            if (body != null)
                w.Write("<code>" + body + "</code><br>");

            if (descr == "")
                w.Write("<i>no description available</i>");
            else
                w.Write(EncodeEntities(descr));

            w.WriteLine("<p>");

            if (modMethods != null)
            {
                foreach (MethodDef m in modMethods)
                {
                    if (m.Description != "")
                    {
                        w.WriteLine("<p><i>Modified in "
                                    + FileLinks(m.Source) + ":</i><br>");
                        w.Write(EncodeEntities(m.Description));
                        w.WriteLine("<p>");
                    }
                }
            }

            w.WriteLine("</div>");
        }

        //=====================================================================
        /// <summary>
        /// Writes the tree of parent classes.
        /// </summary>
        private void WriteParentTree (StreamWriter w, ObjectOrClass cd)
        {
            this.WriteParentTree(w, cd, "");
        }

        private void WriteParentTree (StreamWriter w, ObjectOrClass cd, String indent)
        {
            w.Write("<code>" + indent);
            if (indent == "")
                w.WriteLine("<b>" + cd.Name + "</b></code><br>");
        //  else w.WriteLine("<a href=\"" + cd.Name + ".html\">" + cd.Name + "</a></code><br>");
            else w.WriteLine(Hyperlink("../" + this.ObjectFileName(cd), cd.Name)
                             + "</code><br>");
            indent += " &nbsp; &nbsp; &nbsp; &nbsp; ";
            foreach (object parent in cd.BaseClasses)
            {
                if (parent is ClassDef)
                    this.WriteParentTree(w, (ClassDef) parent, indent);
                else if (parent is ObjectDef)
                    this.WriteParentTree(w, (ObjectDef) parent, indent);
                else
                    w.WriteLine("<code>" + indent + parent.ToString() + "</code><br>");
            }
        }

        //=====================================================================
        /// <summary>
        /// Writes the tree of child classes.
        /// </summary>
        private void WriteChildTree (StreamWriter w, ClassDef cd)
        {
            if (cd.SubClasses.Count == 0)
                w.WriteLine("<i>(none)</i>");
            else this.WriteChildTree(w, cd, "");
        }

        private void WriteChildTree (StreamWriter w, ClassDef cd, String indent)
        {
            w.Write("<code>" + indent);
            if (indent == "")
                w.WriteLine("<b>" + cd.Name + "</b></code><br>");
        //  else w.WriteLine("<a href=\"" + cd.Name + ".html\">" + cd.Name + "</a></code><br>");
            else w.WriteLine(Hyperlink("../" + this.ObjectFileName(cd), cd.Name)
                             + "</code><br>");
            indent += " &nbsp; &nbsp; &nbsp; &nbsp; ";
            foreach (object parent in cd.SubClasses)
            {
                if (parent is ClassDef)
                    this.WriteChildTree(w, (ClassDef) parent, indent);
                else
                    w.WriteLine("<code>" + indent + parent.ToString()
                                + "</code><br>");
            }
        }

        //===================================================================== 
        /// <summary>
        /// Returns a list of all symbols of all types.
        /// </summary>
        public ArrayList CollectAllSymbols ()
        {
            ArrayList symbols = new ArrayList();

            foreach (ClassDef c in this.SymbolTable.Classes)
            {
                symbols.Add(c);
                foreach (MethodDef m in c.Methods)
                    symbols.Add(m);
                foreach (PropertyDef p in c.Properties)
                    symbols.Add(p);
            }
            foreach (ObjectDef o in this.SymbolTable.GlobalObjects)
                symbols.Add(o);
            foreach (FunctionDef f in this.SymbolTable.GlobalFunctions)
                symbols.Add(f);
            foreach (EnumDef c in this.SymbolTable.Enums)
                symbols.Add(c);
            foreach (MacroDef c in this.SymbolTable.Macros)
                symbols.Add(c);
            foreach (TemplateDef c in this.SymbolTable.Templates)
                symbols.Add(c);

            symbols.Sort();

            return symbols;
        }

//        //=====================================================================
//        /// <summary>
//        /// Writes the index of all symbols.
//        /// </summary>
//        public void WriteSymbolIndex (ArrayList symbols)
//        {
//            // write the file
//
//            System.Console.Out.WriteLine("writing index of symbols");
//
//            StreamWriter w = new StreamWriter(this.OutputDir + SymbolIndexFile);
//            w.WriteLine("<html>\r\n<head>"
//                        + "<link rel=stylesheet type=\"text/css\" "
//                        + "href=\"libref.css\">"
//                        + "<title>Index of Symbols</title>"
//                        + "<script type=\"text/javascript\" "
//                        + "src=\"../libref.js\"></script>"
//                        + "</head><body onload=\"frmLd()\">");
//            w.WriteLine("<h1>Index of Symbols</h1>");
//
//            char firstChar = ' ';
//
//            foreach (Symbol sym in symbols)
//            {
//                if (Char.ToUpper(sym.Name[0]) != firstChar)
//                {
//                    firstChar = Char.ToUpper(sym.Name[0]);
//                    w.Write("<p align=center><font size=+2><b>"
//                            + firstChar + "</b></font></p><br>");
//                }
//
//                this.WriteSymbolIndexEntry(w, sym, "");
//            }
//
//            this.WriteHtmlFooter(w);
//            w.Close();
//        }

        //=====================================================================
        /// <summary>
        /// Writes the index of all symbols.
        /// </summary>
        public void WriteSymbolIndexes (ArrayList symbols)
        {
            StreamWriter w;
            for (char letter = 'A'; letter <= 'Z'; letter++)
            {
                w = new StreamWriter(this.OutputDir + IndexDir + "/" + letter + ".html");
                w.WriteLine("<html>\r\n<head>"
                            + "<link rel=stylesheet type=\"text/css\" "
                            + "href=\"../libref.css\">"
                            + "<title>Index of Symbols</title>"
//                          + "<script type=\"text/javascript\" "
//                          + "src=\"../libref.js\"></script>"
//                          + "</head><body onload=\"frmLd()\">");
                            + "</head><body>");
                w.WriteLine("<h1>" + letter + "</h1>");

                foreach (Symbol sym in symbols)
                {
                    if (Char.ToUpper(sym.Name[0]) == letter)
                        this.WriteSymbolIndexEntry(w, sym, "../");
                }

                this.WriteHtmlFooter(w);
                w.Close();
            }

            w = new StreamWriter(this.OutputDir + IndexDir + "/" + "etc.html");
            w.WriteLine("<html>\r\n<head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"../libref.css\">"
                        + "<title>Index of Symbols</title>"
//                      + "<script type=\"text/javascript\" "
//                      + "src=\"../libref.js\"></script>"
//                      + "</head><body onload=\"frmLd()\">");
                        + "</head><body>");
            w.WriteLine("<h1>Etc.</h1>");

            foreach (Symbol sym in symbols)
            {
                if (! Char.IsLetter(sym.Name[0]))
                    this.WriteSymbolIndexEntry(w, sym, "../");
            }

            this.WriteHtmlFooter(w);
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes the table of contents for file the symbol index.
        /// </summary>
        public void WriteSymbolIndexTOC ()
        {
            StreamWriter w = new StreamWriter(this.OutputDir + IndexDir + "/" + SymbolTOCfile);
            w.WriteLine("<html>\r\n<head>"
                        + "<link rel=stylesheet type=\"text/css\" "
                        + "href=\"../libref.css\">"
//                      + "<script type=\"text/javascript\" "
//                      + "src=\"../libref.js\"></script>"
//                      + "</head><body onload=\"frmLd()\">"
                        + "</head><body>"
                        + "<h2>All&nbsp;Symbols</h2>");

            w.WriteLine("<table class=hdr>");

            for (Char letter = 'A'; letter <= 'M'; letter++)
            {
                char letter2 = (char) (letter + 13);
                w.WriteLine("<tr><td>" + 
                            Hyperlink(letter.ToString() + ".html",
                                      "_" + letter + "_", "main",
                                      letter.ToString())
                            + "<td>"
                            + Hyperlink(letter2.ToString() + ".html",
                                        "_" + letter2 + "_", "main",
                                        letter2.ToString()));
            }

            w.WriteLine("<tr><td colspan=2>" +
                        Hyperlink("etc.html", "main", "(etc)"));

            w.WriteLine("</table></body></html>");
            w.Close();
        }

        //=====================================================================
        /// <summary>
        /// Writes an entry in the symbol index file.
        /// </summary>
        /// <param name="prefix">file name prefix: "../" or ""</param>
        private void WriteSymbolIndexEntry (StreamWriter w, Symbol sym, String prefix)
        {
            if (sym is ClassDef)
                this.WriteSymbolIndexEntry(w, sym.Name, this.ObjectFileName((ClassDef) sym), 
                    null, (((ClassDef)sym).IsGrammar ? "grammar" : "class"),
                    null, sym.Source, prefix);
            else if (sym is ObjectDef)
                this.WriteSymbolIndexEntry(w, sym.Name, this.ObjectFileName((ObjectDef) sym), 
                    null, "object", null, sym.Source, prefix);
            else if (sym is MethodDef)
                this.WriteSymbolIndexEntry(w, sym.Name, this.ObjectFileName(((MethodDef) sym).ClassOrObject), 
                    sym.Name, "method", ((MethodDef) sym).ClassOrObject, sym.Source, prefix);
            else if (sym is PropertyDef)
                this.WriteSymbolIndexEntry(w, sym.Name, this.ObjectFileName(((PropertyDef) sym).ClassOrObject),
                    sym.Name, "property", ((PropertyDef) sym).ClassOrObject, sym.Source, prefix);
            else if (sym is EnumDef)
                this.WriteSymbolIndexEntry(w, sym.Name, this.FileFileName(sym.Source), 
                    sym.Name, "enum", null, sym.Source, prefix);
            else if (sym is MacroDef)
                this.WriteSymbolIndexEntry(w, sym.Name, this.FileFileName(sym.Source), 
                    sym.Name, "macro", null, sym.Source, prefix);
            else if (sym is TemplateDef)
                this.WriteSymbolIndexEntry(w, sym.Name, this.FileFileName(sym.Source), 
                    sym.Name, "template", null, sym.Source, prefix);
            else if (sym is FunctionDef)
                this.WriteSymbolIndexEntry(w, sym.Name, this.FileFileName(sym.Source), 
                    sym.Name, "global function", null, sym.Source, prefix);
        }

        //=====================================================================
        /// <summary>
        /// Writes a single entry in the symbol index file.
        /// </summary>
        /// <param name="symbolName">the name of the symbol</param>
        /// <param name="htmlFileName">link to the documentation file for this symbol</param>
        /// <param name="anchor">link to the documentation file for this symbol</param>
        /// <param name="type">the type of symbol (class, method, macro, enum, etc.)</param>
        /// <param name="container">the containing class, or null</param>
        /// <param name="sourceFileName">the source file in which the symbol was defined</param>
        /// <param name="prefix">file name prefix: "../" or ""</param>
        private void WriteSymbolIndexEntry (StreamWriter w, String symbolName,
                                String htmlFileName, String anchor, String type, 
                            Modification container, SourceLoc sourceFile, String prefix)
        {
            w.Write(Hyperlink(prefix + htmlFileName, anchor, null,
                              "<code><b>" + symbolName + "</b></code>")
                    + " - " + type);
            if (container != null)
                w.WriteLine(" of "
                            + Hyperlink(prefix + this.ObjectFileName(container),
                                        container.Name));
            w.WriteLine(" in " + FileLinks(sourceFile) + "<br>");
        //  Hyperlink(prefix + this.FileFileName(sourceFile), sourceFile.Name) + "<br>");
        }

        //=====================================================================
        /// <summary>
        /// Writes a major heading.
        /// </summary>
        private void WriteMajorHeading (StreamWriter w, String anchor, String majorText, String minorText)
        {
            w.WriteLine("<a name=\"" + anchor + "\"></a>"
                        + "<p><div class=mjhd>"
                        + "<span class=hdln>" + majorText
                        + "</span> &nbsp; " + minorText + "</div><p>");
        }

        //=====================================================================
        /// <summary>
        /// Copies the source files into the output directory, converting them
        /// to HTML and adding line number anchors.
        /// </summary>
        private void CopySourceFiles ()
        {
            foreach (SourceFile sf in this.SymbolTable.Files)
            {
                System.Console.Out.WriteLine("copying " + sf.FullPath);

                StreamReader r = new StreamReader(sf.FullPath);
                StreamWriter w = new StreamWriter(this.OutputDir + SourceDir + "/" + sf.ShortName + ".html");

                w.WriteLine("<html><head>"
                            + "<link rel=stylesheet type=\"text/css\" "
                            + "href=\"../libref.css\">"
                            + "<title>" + sf.ShortName + "</title>"
//                          + "<script type=\"text/javascript\" "
//                          + "src=\"../libref.js\"></script>"
//                          + "</head><body onload=\"frmLd()\">");
                            + "</head><body>");

                w.WriteLine("<table class=ban>"
                            + "<tr><td><h1>" + 
                            sf.ShortName + "</h1><td align=right>" + 
                            Hyperlink("../" + this.FileFileName(sf),
                                      "documentation")
                            + "</table><pre>");


                String line;
                for (int lineNumber = 1; null != (line = r.ReadLine()); lineNumber++)
                {
                    line = line.Replace("&", "&amp;");
                    line = line.Replace(">", "&gt;");
                    line = line.Replace("<", "&lt;");
                    w.WriteLine("<a name=\"" + lineNumber + "\"></a>" + line);
                }

                w.WriteLine("</pre>");
                this.WriteHtmlFooter(w);

                w.Close();
                r.Close();
            }
        }

        //=====================================================================
        /// <summary>
        /// Returns a hyperlink.
        /// </summary>
        /// <param name="targetFile">the file being linked to</param>
        /// <param name="targetLabel">the anchor label (the text right of the '#')</param>
        /// <param name="targetFrame">the frame in which to display the linked file</param>
        /// <param name="displayText">the visible text</param>
        private static String Hyperlink (String targetFile, 
            String targetLabel, String targetFrame, String displayText)
        {
            String s = "<a href=\"";
            if (targetFile != null)
                s += targetFile;
            if (targetLabel != null)
                s += "#" + targetLabel;
            s += "\"";
            if (targetFrame != null)
                s += " target=\"" + targetFrame + "\"";
            s += ">" + displayText + "</a>";
            return s;
        }

        //=====================================================================
        /// <summary>
        /// Returns a hyperlink.
        /// </summary>
        private static String Hyperlink (String targetFile, String targetFrame, String displayText)
        {
            return "<a href=\"" + targetFile + "\" target=\"" + targetFrame + "\">" + displayText + "</a>";
        }

        //=====================================================================
        /// <summary>
        /// Returns a hyperlink.
        /// </summary>
        private static String Hyperlink (String targetFile, String displayText)
        {
            return "<a href=\"" + targetFile + "\">" + displayText + "</a>";
        }

        //=====================================================================
        /// <summary>
        /// Returns a pair of hyperlinks for the file file and source file.
        /// </summary>
        private String FileLinks (SourceLoc s)
        {
            return Hyperlink("../" + this.FileFileName(s), s.Name) + "[" +
                Hyperlink("../" + this.SourceFileName(s), s.Line.ToString(), 
                          null, s.Line.ToString()) + "]";
        }

        //=====================================================================
        /// <summary>
        /// Writes an HTML file title.
        /// </summary>
        /// <param name="w"></param>
        /// <param name="type">"class", "object", "file", etc.</param>
        /// <param name="title">main title</param>
        /// <param name="file">file link for right-hand side</param>
        private void WriteFileTitle (StreamWriter w, String type, String title, 
                        String file, ArrayList otherFiles)
        {
            w.Write("<table class=ban><tr><td align=left>");
            w.Write("<span class=title>" + title + "</span>");
            if (type != null)
                w.Write("<span class=type>" + type + "</span>");
            if (file != null)
            {
                w.Write("<td align=right>" + file);
                if (otherFiles != null)
                    foreach (SourceLoc sl in otherFiles)
                        w.Write(", " + FileLinks(sl));
            }
            w.WriteLine("</table><p>");
        }

        //=====================================================================
        /// <summary>
        /// Encodes special characters that need to be represented as entities.
        /// </summary>
        private static String EncodeEntities (String s)
        {
            s = s.Replace("&", "&amp;");
            s = s.Replace("<", "&lt;");
            s = s.Replace(">", "&gt;");
            s = s.Replace("\r\n", "<p>");
            s = s.Replace("\n", "<p>");
            s = s.Replace("\r", "<p>");
            s = s.Replace("\u0001", "<br>");
//            s = s.Replace("{{mod}}", "<i>Modified in ");
//            s = s.Replace("{{/mod}}", ":</i><br>");
            return s;
        }

        //=====================================================================
        /// <summary>
        /// Writes the last bit of stuff in each HTML file.
        /// </summary>
        private void WriteHtmlFooter (StreamWriter w)
        {
            DateTime now = DateTime.Now;
            w.Write("<div class=ftr>Adv3Lite Library Reference Manual<br>"
                    + "Generated on " + now.ToShortDateString());
            if (TadsVersion != null)
                w.Write(" from adv3Lite version " + TadsVersion);

            w.Write("</div>\r\n</body>\r\n</html>\r\n");
        }

        //=====================================================================
    }
}
