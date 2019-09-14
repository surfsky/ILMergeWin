using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ILMergeWin
{
    /// <summary>
    /// 命令运行器
    /// （1）截取输出，不显示窗口
    /// （2）在线程中运行
    /// （3）实现UI线程Invoke处理逻辑
    /// </summary>
    /// <example>
    /// var runner = new CommandRunner(this);
    /// runner.Command = "merge.bat"
    /// runner.Arguments = "";
    /// runner.OnReadLine += (str) => AddLog(str);
    /// runner.OnEnd += (n, str) => AddLog("\r\n[{0}] Exit {1} {2}", DateTime.Now, n, str);
    /// AddLog("[{0}] Start", DateTime.Now);
    /// runner.Run();
    /// </example>
    public class CommandRunner
    {
        private System.Windows.Forms.Control Caller { get; set; }
        public string Command { get; set; }
        public string Arguments { get; set; }
        public event Action<string> OnReadLine;
        public event Action<int, string> OnEnd;

        /// <summary>构造函数</summary>
        /// <param name="caller">UI线程控件</param>
        public CommandRunner(System.Windows.Forms.Control caller)
        {
            this.Caller = caller;
        }

        public void Run()
        {
            Thread thread = new Thread(new ThreadStart(() => RunInternal()));
            thread.Start();
        }

        void RunInternal()
        {
            try
            {
                ProcessStartInfo pi = new ProcessStartInfo();
                pi.FileName = this.Command;
                pi.Arguments = this.Arguments;
                pi.RedirectStandardInput = true;
                pi.RedirectStandardOutput = true;
                pi.RedirectStandardError = true;
                pi.UseShellExecute = false;
                pi.CreateNoWindow = true;

                Process p = new Process();
                p.StartInfo = pi;
                p.EnableRaisingEvents = true;
                p.OutputDataReceived += process_OutputDataReceived;
                p.ErrorDataReceived += process_ErrorDataReceived;
                p.Exited += process_Exited;
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                ProcessEnd(-1, ex.Message);
            }
        }

        /// <summary>输出重定向</summary>
        private void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            ProcessLine(e.Data);
        }

        /// <summary>错误重定向</summary>
        private void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            ProcessLine(e.Data);
        }

        /// <summary>进程结束</summary>
        private void process_Exited(object sender, EventArgs e)
        {
            ProcessEnd((sender as Process).ExitCode);
        }

        // 读取一行数据后的处理
        private void ProcessLine(string line)
        {
            if (OnReadLine != null)
            {
                if (Caller != null)
                    Caller.Invoke(OnReadLine, line);
                else
                    OnReadLine(line);
            }
        }


        // 结束后的处理
        private void ProcessEnd(int exitCode, string info = "")
        {
            if (OnEnd != null)
            {
                if (Caller != null)
                    Caller.Invoke(OnEnd, exitCode, info);
                else
                    OnEnd(exitCode, info);
            }
        }
    }
}
