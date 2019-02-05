// Copyright (c) 2013 Pavel Samokha
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace VSChangeTargetFrameworkExtension
{
    public partial class ProjectsUpdateList : Form
    {
        public event Action UpdateFired;
        public event Action ReloadFired;

        public ProjectsUpdateList()
        {
            InitializeComponent();
            dataGridView1.AutoGenerateColumns = false;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        public List<FrameworkModel> Frameworks
        {
            set
            {
                comboBox1.DataSource = value;
            }
        }
        public List<ProjectModel> Projects
        {
            set
            {
                var wrapperBindingList = new SortableBindingList<ProjectModel>(value);
                try
                {
                    dataGridView1.DataSource = wrapperBindingList;
                    dataGridView1.Refresh();
                }
                catch (InvalidOperationException)
                {
                    Invoke(new EventHandler(delegate
                    {
                        dataGridView1.DataSource = wrapperBindingList;
                        dataGridView1.Refresh();
                    }));
                }
            }
            get
            {
                SortableBindingList<ProjectModel> wrapperBindingList = null;
                try
                {
                    wrapperBindingList = (SortableBindingList<ProjectModel>)dataGridView1.DataSource;
                }
                catch (InvalidOperationException)
                {
                    Invoke(new EventHandler(delegate
                    {
                        wrapperBindingList = (SortableBindingList<ProjectModel>)dataGridView1.DataSource;
                    }));
                }
                return wrapperBindingList.WrappedList;
            }
        }

        public FrameworkModel SelectedFramework
        {
            get
            {
                FrameworkModel model = null;
                Invoke(new EventHandler(delegate
                {
                    model = (FrameworkModel)comboBox1.SelectedItem;
                }));
                return model;
            }
        }

        public string State
        {
            set
            {
                try
                {
                    label1.Text = value;
                }
                catch (InvalidOperationException)
                {
                    Invoke(new EventHandler(delegate
                    {
                        label1.Text = value;
                    }));
                }
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            var onUpdate = UpdateFired;
            if (onUpdate != null)
                await Task.Run(() =>
                {
                    onUpdate.Invoke();
                });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (var projectModel in Projects)
            {
                projectModel.IsSelected = true;
            }
            dataGridView1.Refresh();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (var projectModel in Projects)
            {
                projectModel.IsSelected = false;
            }
            dataGridView1.Refresh();
        }

        private void reloadButton_Click(object sender, EventArgs e)
        {
            var onReloadFired = ReloadFired;
            if (onReloadFired != null)
                onReloadFired.Invoke();
        }

        private void AddDotNetVersion(string version)
        {
            var digits = version.Split('.').ToList();
            if (digits.Count == 3)
            {
                var tmp = digits[1];
                digits[1] = digits[2];
                digits[2] = tmp;
            }
            else
            {
                digits.Insert(1, "");
            }

            var hexValue = string.Empty;
            foreach (var digit in digits)
            {
                hexValue += digit.PadLeft(2, '0');
            }
            int versionId = int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
            UpdateXML(versionId, version);
        }

        private void UpdateXML(int versionId, string version)
        {
            UpdateXML(versionId.ToString(), version);
        }

        private void UpdateXML(string versionId, string version)
        {
            var filePath = GetFrameworksXmlFilePath();
            var doc = XDocument.Load(filePath);
            var frameworkNodes = doc.Root.Elements("Framework");
            bool exists = false;
            foreach (var node in frameworkNodes)
            {
                if (node.Attribute("Id").Value == versionId)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                var elem = new XElement("Framework", new XAttribute("Id", versionId), new XAttribute("Name", $".NETFramework,Version=v{version}"));
                doc.Descendants("Frameworks").FirstOrDefault().Add(elem);
                doc.Save(filePath);
            }
        }

        private string GetFrameworksXmlFilePath()
        {
            var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(folderPath, "Frameworks.xml");
        }

        private bool ValidateVersion(string version)
        {
            var result = true;
            if (!string.IsNullOrWhiteSpace(version.Trim()))
            {
                result = false;
                var digits = version.Split('.');
                if (digits.Length <= 3 && digits.Length >= 2)
                {
                    foreach (var digit in digits)
                    {
                        result = byte.TryParse(digit, out byte value);
                        if (!result)
                        {
                            break;
                        }
                    }
                }
            }
            return result;
        }

        private void btnAddDotNetVersion_Click(object sender, EventArgs e)
        {
            AddDotNetVersion(tbAddDotNetVersion.Text);
        }

        private void tbAddDotNetVersion_Leave(object sender, EventArgs e)
        {
            var tb = sender as TextBox;
            if (!ValidateVersion(tb.Text))
            {
                toolTip1.ToolTipTitle = "Input Rejected";
                toolTip1.Show("You can only add numeric characters (0-9) into this field.", tb, 0, -20, 5000);
            }
        }
    }
}
