using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml.Linq;

namespace ChangeReference
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> excludes = new List<string>();
        private List<string> projFiles = new List<string>();
        private string extension = string.Empty;
        private string from = string.Empty;
        private string to = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ok_Button_Click(object sender, RoutedEventArgs e)
        {
            this.excludes.Clear();
            this.projFiles.Clear();
            this.messages_TextBox.Clear();

            var exclude = this.exclude_TextBox.Text.Split(';');
            this.excludes.AddRange(exclude);
            this.extension = this.extension_TextBox.Text;
            this.from = this.from_TextBox.Text;
            this.to = this.to_TextBox.Text;
            var path = this.project_TextBox.Text;

            if (string.IsNullOrEmpty(path) ||
                string.IsNullOrEmpty(this.extension) ||
                string.IsNullOrEmpty(this.from) ||
                string.IsNullOrEmpty(this.to))
            {
                MessageBox.Show("项目路径,项目扩展名,from,to不允许为空");
            }
            else if (!Directory.Exists(path))
            {
                MessageBox.Show("该项目路径不存在");
            }
            else
            {
                this.GetDir(path);
                this.Change();
                MessageBox.Show("成功!");
            }
        }

        private void Change()
        {
            foreach (var path in projFiles)
            {
                XDocument document = XDocument.Load(path);
                XElement root = document.Root;
                if (root == null) continue;

                // Microsoft.CodeDom.Providers.DotNetCompilerPlatform
                // Microsoft.Net.Compilers
                foreach (var import in root.Elements().Where(e => e.Name.LocalName == "Import"))
                {
                    var project = import.Attribute("Project");
                    if (project != null)
                        project.Value = project.Value.Replace(this.from, this.to);

                    var condition = import.Attribute("Condition");
                    if (condition != null)
                        condition.Value = condition.Value.Replace(this.from, this.to);
                }

                // 此项目引用这台计算机上缺少的 NuGet 程序包。
                foreach (var target in root.Elements().Where(e => e.Name.LocalName == "Target"))
                {
                    var nameAttribute = target.Attribute("Name");
                    if (nameAttribute == null || nameAttribute.Value != "EnsureNuGetPackageBuildImports") continue;

                    foreach (var error in target.Elements().Where(e => e.Name.LocalName == "Error"))
                    {
                        var condition = error.Attribute("Condition");
                        if (condition != null)
                            condition.Value = condition.Value.Replace(this.from, this.to);

                        var text = error.Attribute("Text");
                        if (text != null)
                            text.Value = text.Value.Replace(this.from, this.to);
                    }
                }

                // nuget引用程序集  // ItemGroup==>Reference==>HintPath
                foreach (XElement itemGroup in root.Elements().Where(e => e.Name.LocalName == "ItemGroup"))
                {
                    foreach (var reference in itemGroup.Elements().Where(e => e.Name.LocalName == "Reference"))
                    {
                        foreach (var hintPath in reference.Elements().Where(e => e.Name.LocalName == "HintPath"))
                        {
                            hintPath.Value = hintPath.Value.Replace(this.from, this.to);
                        }
                    }
                }
                document.Save(path);
                this.messages_TextBox.AppendText(path);
                this.messages_TextBox.AppendText("\n\n");
            }
        }

        private void GetDir(string path)
        {
            var files = Directory.GetFiles(path);

            foreach (var item in files)
            {
                var filename = Path.GetExtension(item.ToLower());
                if (filename != this.extension) continue;
                this.projFiles.Add(item);
            }

            var directories = Directory.GetDirectories(path);
            foreach (var item in directories)
            {
                var filename = Path.GetFileName(item.ToLower());
                if (this.excludes.Contains(filename)) continue;
                this.GetDir(item);
            }
        }
    }
}
