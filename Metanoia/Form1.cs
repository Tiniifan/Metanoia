﻿using System;
using System.Windows.Forms;
using Metanoia.GUI;
using System.Collections.Generic;
using System.Linq;
using Metanoia.Rendering;
using Metanoia.Formats;

namespace Metanoia
{
    public partial class MainForm : Form
    {
        public ModelViewer ModelViewer;

        public MainForm()
        {
            InitializeComponent();

            ShowDirectory(AppDomain.CurrentDomain.BaseDirectory);

            ModelViewer = new ModelViewer();
            ModelViewer.Dock = DockStyle.Fill;
            viewerBox.Controls.Add(ModelViewer);
        }

        private Queue<string> ExpandQueue = new Queue<string>();

        public void ShowDirectory(string path)
        {
            string[] Folders = path.Split('\\');

            FolderNode Node = new FolderNode(Folders[0]);
            folderTree.Nodes.Add(Node);
            Node.Expand();

            for (int i = 1; i < Folders.Length; i++)
                ExpandQueue.Enqueue(Folders[i]);
        }

        private void folderTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            ((FolderNode)e.Node).BeforeExpand();
        }

        private void folderTree_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (ExpandQueue.Count == 0) return;
            ((FolderNode)e.Node).ExpandNode(ExpandQueue.Dequeue());
        }

        private void folderTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if(e.Node != null)
            {
                fileList.Items.Clear();
                fileList.Items.AddRange(((FolderNode)e.Node).GetFiles().ToArray());
            }
        }

        private void fileList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (fileList.SelectedItem == null)
                return;

            if (fileList.SelectedItem is FileItem File)
            {
                IFileFormat Node = GetFileFormatFromExtension(File.Extension);
                
                if (Node != null)
                {
                    if(Node is IModelFormat ModelFormat)
                    {
                        Node.Open(File.GetFileBinary());
                        ModelViewer.GenericRenderer.SetGenericModel(ModelFormat.ToGenericModel());
                        ModelViewer.Invalidate();
                    }
                }
            }
        }

        private IFileFormat GetFileFormatFromExtension(string Extension)
        {
            var Types = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                         from assemblyType in domainAssembly.GetTypes()
                         where typeof(IFileFormat).IsAssignableFrom(assemblyType)
                         select assemblyType).ToArray();

            IFileFormat Node = null;

            foreach (Type type in Types)
            {
                if (type.GetCustomAttributes(typeof(FormatAttribute), true).FirstOrDefault() is FormatAttribute attr)
                {
                    if (attr.Extension.Equals(Extension))
                    {
                        Node = (IFileFormat)Activator.CreateInstance(type);
                    }
                }
            }

            return Node;
        }

        private void exportedSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(fileList.SelectedItem is FileItem File)
            {
                IFileFormat Node = GetFileFormatFromExtension(File.Extension);

                if (Node != null)
                    Node.Open(File.GetFileBinary());

                if (Node is IModelFormat Model)
                {
                    using (SaveFileDialog d = new SaveFileDialog())
                    {
                        d.Filter = "Source Model (*.smd)|*.smd";

                        if (d.ShowDialog() == DialogResult.OK)
                        {
                            Metanoia.Exporting.ExportSMD.Save(d.FileName, Model.ToGenericModel());
                        }
                    }
                }
            }
        }
    }
}