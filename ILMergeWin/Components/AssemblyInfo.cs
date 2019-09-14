using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ILMergeWin
{
    /// <summary>
    /// 程序集信息
    /// </summary>
    public class AssemblyInfo
    {
        // properties
        public string File { get; set; }

        // get
        public FileInfo Info => new FileInfo(File);
        public Assembly Assembly => Assembly.LoadFrom(File);
        public Version Version => Assembly.GetName().Version;

        //
        public AssemblyInfo(string file) { this.File = file; }
        public override string ToString()
        {
            return string.Format("{0}, {1}", File, Version);
        }
    }

}
