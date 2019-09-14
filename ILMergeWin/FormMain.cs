using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.Core;

namespace ILMergeWin
{
    public partial class FormMain : Form
    {
        // 程序集列表
        List<AssemblyInfo> _assemblies = new List<AssemblyInfo>();

        //
        public FormMain()
        {
            InitializeComponent();
            this.tbOutput.Text = @"
This tool can merge assemblies into one assembly. It's a windows form wrapper for ILMerge. 
- [x] Select assemblies (Add, Delete, Clear)
- [x] Merge assemblies
- [x] Show console output

Notes: The tool can't run in network folder, because cmd.exe don't support UNC path.
";
        }

        /// <summary>About</summary>
        private void BtnAbout_Click(object sender, EventArgs e)
        {
            new FormAbout().ShowDialog();
        }


        //------------------------------------------------
        // Utils
        //------------------------------------------------
        /// <summary>清除日志</summary>
        void LogClear()
        {
            this.tbOutput.Clear();
        }
        /// <summary>添加日志</summary>
        void Log(string format, params object[] args)
        {
            this.tbOutput.AppendText(string.Format(format + "\r\n", args));
        }

        /// <summary>显示状态</summary>
        private void ShowStatus(string status)
        {
            this.lblStatus.Text = status;
        }


        //------------------------------------------------
        // 增删改查
        //------------------------------------------------
        #region Assemblies CRUD
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Exe,Dll|*.exe;*.dll|All Files|*.*";
            dlg.Multiselect = true;
            if (DialogResult.OK == dlg.ShowDialog())
            {
                foreach (var file in dlg.FileNames)
                {
                    var ass = _assemblies.FirstOrDefault(t => t.File == file);
                    if (ass == null)
                        _assemblies.Add(new AssemblyInfo(file));
                }
                ShowAssemblies();
            }
        }

        // 显示程序集列表
        void ShowAssemblies()
        {
            this.lbAssembly.DataSource = _assemblies.OrderBy(t => t.File).ToList();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lbAssembly.SelectedItems.Count; i++)
            {
                var item = lbAssembly.SelectedItems[i] as AssemblyInfo;
                _assemblies.Remove(item);
            }
            lbAssembly.SelectedIndex = -1;
            ShowAssemblies();
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            _assemblies.Clear();
            ShowAssemblies();
        }

        // 显示列表
        private void LbAssembly_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1)
                return;

            //
            var assembly = lbAssembly.Items[e.Index] as AssemblyInfo;
            Rectangle b = e.Bounds;

            // 背景
            Brush brush = Brushes.Black;
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                brush = new SolidBrush(Color.FromArgb(240, 240, 240));     // 150, 200, 250
            else
                brush = new SolidBrush(Color.White);
            e.Graphics.FillRectangle(brush, b);
            //e.DrawFocusRectangle();

            // 绘制图标
            var ext = Path.GetExtension(assembly.File).ToLower();
            Image img = (ext == ".exe") ? Properties.Resources.Application : Properties.Resources.Brick;
            Rectangle imgRect = new Rectangle(b.X, b.Y + 1, b.Height - 2, b.Height - 2);
            e.Graphics.DrawImage(img, imgRect, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel);

            // 绘制文字
            var info = new FileInfo(assembly.File);
            var text = string.Format("{0}, {1}", info.FullName, assembly.Version);
            Rectangle txtRect = new Rectangle(
                imgRect.Right + 10,
                b.Y + 4,
                b.Width - imgRect.Right - 100,
                b.Height);
            StringFormat format1 = new StringFormat() { Alignment = StringAlignment.Near };
            e.Graphics.DrawString(text, e.Font, new SolidBrush(Color.Black), txtRect, format1);

            // 绘制尺寸
            Rectangle sizeRect = new Rectangle(
                b.Right - 80,
                b.Y + 4,
                80,
                b.Height);
            StringFormat format2 = new StringFormat() { Alignment = StringAlignment.Near };
            e.Graphics.DrawString(info.Length.ToSizeText(), e.Font, new SolidBrush(Color.Black), sizeRect, format2);
        }
        #endregion

        //------------------------------------------------
        // 处理
        //------------------------------------------------
        private void BtnMerge_Click(object sender, EventArgs e)
        {
            if (_assemblies.Count < 2)
            {
                MessageBox.Show(this, "Please add assemblies.");
                return;
            }

            // User choose target assembly.
            SaveFileDialog dlg = new SaveFileDialog();
            var targetType = GetTargetType();
            if (targetType == "winexe")
                dlg.Filter = "exe|*.exe";
            else if (targetType == "library")
                dlg.Filter = "dll|*.dll";
            dlg.AddExtension = true;
            if (DialogResult.OK == dlg.ShowDialog())
            {
                var filePath = dlg.FileName;
                var platform = cmbVersion.Text;
                var args = BuildArgs(filePath, platform, targetType, _assemblies);
                RunCommand(filePath, args);
            }
        }

        /// <summary>获取合并类型</summary>
        string GetTargetType()
        {
            foreach (AssemblyInfo assembly in lbAssembly.Items)
            {
                if (Path.GetExtension(assembly.File).ToLower() == ".exe")
                    return "winexe";
            }
            return "library";
        }

        /// <summary>构建命令参数</summary>
        private string BuildArgs(string filePath, string platform, string targetType, List<AssemblyInfo> items)
        {
            var cmd = "  /ndebug  /allowDup  /targetplatform:{0}  /target:{1}  /out:{2}  ^\n";
            cmd = string.Format(cmd, platform, targetType, filePath);
            foreach(AssemblyInfo assembly in items)
                cmd += string.Format("{0} ^\n", assembly.File);
            return cmd.TrimEnd('\n', '\r', '^');
        }

        /// <summary>运行命令</summary>
        private void RunCommand(string targetFile, string arg)
        {
            //var mergeFile = new FileInfo("ILMerge.exe").FullName;
            var mergeFile = "ILMerge.exe";
            var cmd = $"{mergeFile} {arg}";
            var cmdFile = String.Format("{0}.bat", Guid.NewGuid().ToString("N"));
            File.WriteAllText(cmdFile, cmd);
            LogClear();
            //Log(cmd + "\r\n");
            Log("[{0:yyyy-MM-dd HH:mm:ss}] Start", DateTime.Now);
            ShowStatus("Running");

            // Go
            var runner = new CommandRunner(this);
            runner.Command = cmdFile;
            runner.OnReadLine += (txt) => Log(txt);
            runner.OnEnd += (code, txt) =>
            {
                File.Delete(cmdFile);
                Log("[{0:yyyy-MM-dd HH:mm:ss}] Exit {1} {2}", DateTime.Now, code, txt);
                if (code == 0)
                    Log("Create assembly: {0}", targetFile);
                ShowStatus((code == 0) ? "Success" : "Failure");
            };
            runner.Run();
        }

    }
}
