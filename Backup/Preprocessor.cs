using System;
using System.Collections;
using System.IO;

namespace DocGen
{
    /// <summary>
    /// Summary description for Preprocessor.
    /// </summary>
    public class Preprocessor
    {
        private ArrayList Files = new ArrayList();
        public ArrayList SortedFiles = new ArrayList();

        //=====================================================================
        
        public Preprocessor()
        {
        }
        
        //=====================================================================


        public void TakeFile (String name)
        {
            this.Files.Add(new SourceFile(name));
        }

        //=====================================================================

        public void Preprocess()
        {
            foreach (SourceFile f in this.Files)
                this.Preprocess(f);
            this.SortFiles();
        }

        //=====================================================================

        private void Preprocess (SourceFile file)
        {
            System.Console.Out.WriteLine("preprocessing " + file.FullPath);

            StreamReader sr = new StreamReader(file.FullPath);
            String line;

            while (null != (line = sr.ReadLine()))
            {
                file.NumLines++;
                line = line.Trim();
                if (! line.StartsWith("#include"))
                    continue;
                line = line.Substring(8).Trim();
                if (line == "")
                    continue;

                String includeFileName;
                if (line[0] == '\"')
                {
                    int n = line.IndexOf('\"', 1);
                    if (n < 0)
                        return;
                    includeFileName = line.Substring(1, n - 1);
                }
                else if (line[0] == '<')
                {
                    int n = line.IndexOf('>', 1);
                    if (n < 0)
                        return;
                    includeFileName = line.Substring(1, n - 1);
                }
                else continue;
                
                SourceFile includeFile = this.FindFile(includeFileName);
                if (includeFile == null)
                    throw new Exception("unknown include file: " + includeFileName);
                file.IncludeFiles.Add(includeFile);
            }

            sr.Close();
        }

        //=====================================================================

        private void SortFiles ()
        {
            while (this.SortingPass())
            { }

            foreach (SourceFile f in Files)
            {
                if (!f.SortFlag)
                    SortedFiles.Add(f);
            }

            if (this.Files.Count != this.SortedFiles.Count)
                throw new Exception("uh-oh");
        }

        //=====================================================================

        private bool SortingPass ()
        {
            bool didSomething = false;
            foreach (SourceFile f in this.Files)
            {
                if (! (f.SortFlag || f.HasUnsortedDependencies()))
                {
                    f.SortFlag = true;
                    this.SortedFiles.Add(f);
                    didSomething = true;
                }
            }
            return didSomething;
        }

        //=====================================================================

        private SourceFile FindFile (String name)
        {
            foreach (SourceFile f in this.Files)
                if (f.ShortName == name)
                    return f;
            return null;
        }
    }

}
