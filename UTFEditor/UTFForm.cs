using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using UTFEditor.Components;

using static UTFEditor.VMeshData;

namespace UTFEditor
{
    /// <summary>
    /// A list of nodes that can be edited.
    /// </summary>
    public enum Editable
    {
        No,
        VMeshRef,
        Hardpoint,
        Fix,
        Rev,
        Sphere,
        Channel,
        Color,
        String,
        Int,
        IntHex,
        Float
    };

    /// <summary>
    /// A list of nodes that can be viewed (or played).
    /// </summary>
    public enum Viewable
    {
        No,
        VMeshData,
        VMeshRef,
        VWireData,
        Texture,            // node starts with MIP
        Wave,               // data starts with RIFF
    };

    public partial class UTFForm : Form
    {
        /// <summary>
        /// The parent window containing this form.
        /// </summary>
        UTFEditorMain parent;

        /// <summary>
        /// True if there are pending file changes that have not been saved.
        /// </summary>
        private bool fileChangesNotSaved = false;

        /// <summary>
        /// The UTFFile this form is displaying.
        /// </summary>
        UTFFile utfFile = new UTFFile();

        internal SurFile SurFile { get; private set; } = null;

        /// <summary>
        /// The name of the UTF file.
        /// </summary>
        public string fileName;

        public TreeView Tree => treeView1;

        /// <summary>
        /// Create an empty form.
        /// </summary>
        public UTFForm(UTFEditorMain parent, string name)
        {
            InitializeComponent();
            fileName = name;
            string path = Path.GetDirectoryName(name);
            this.Text = Path.GetFileName(name);
            if (path.Length != 0)
            {
                int data = path.IndexOf(@"\data\", StringComparison.OrdinalIgnoreCase);
                if (data != -1)
                    path = path.Substring(data + 6);
                this.Text += " - " + path;
            }
            this.parent = parent;
        }

        /// <summary>
        /// Try to load a UTF file. Throw an exception on failure.
        /// </summary>
        /// <param name="filePath">The file to load.</param>
        public void LoadUTFFile(string filePath)
        {
            // Add the real root to the treeview
            treeView1.Nodes.Clear();
            TreeNode root = utfFile.LoadUTFFile(filePath);

            // Add the hardpoints first, so we can sort them
            // and still preserve the root order.
            if (utfFile.Hardpoints.Nodes.Count > 0)
                treeView1.Nodes.Add(utfFile.Hardpoints);
            if (utfFile.Parts.Nodes.Count > 0)
                treeView1.Nodes.Add(utfFile.Parts);
            treeView1.TreeViewNodeSorter = new NodeSorter();
            treeView1.Sort();
            treeView1.TreeViewNodeSorter = null;
            treeView1.Sorted = false;
            treeView1.Nodes.Insert(0, root);
            if (utfFile.Hardpoints.Nodes.Count == 0 &&
                utfFile.Parts.Nodes.Count == 0)
                treeView1.Nodes[0].Expand();

            treeView1.Modified += (s, e) => { Modified(); };

            var surpath = Path.ChangeExtension(filePath, ".sur");
            if (File.Exists(surpath))
            {
                try
                {
                    SurFile = new SurFile(surpath);
                }
                catch(Exception)
                {
                    SurFile = null;
                    MessageBox.Show("Error while loading associated SUR file '" + surpath + "'.", "SUR Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <sumary>
        /// Sort the Hardpoints and Parts nodes.
        /// </sumary>
        public class NodeSorter : IComparer
        {
            private string sx, sy;

            private int compare(string hp)
            {
                bool a = sx.StartsWith(hp);
                bool b = sy.StartsWith(hp);
                if (a && !b)
                    return -1;
                if (!a && b)
                    return 1;
                if (a && b)
                    return String.CompareOrdinal(sx, sy);
                return 0;
            }
            
            public int Compare(object x, object y)
            {
                TreeNode tx = x as TreeNode;
                TreeNode ty = y as TreeNode;
                sx = tx.Text.ToLowerInvariant();
                sy = ty.Text.ToLowerInvariant();
                bool a, b;
                int rc;

                //if (tx.FullPath.StartsWith("Parts"))
                if (tx.Parent != null && tx.Parent.Text != "Hardpoints")
                    return String.CompareOrdinal(sx, sy);

                // Place hardpoints before damage points.
                a = (sx[0] == 'h');
                b = (sy[0] == 'h');
                if (a && !b)
                    return -1;
                if (!a && b)
                    return 1;
                if (a && b)
                {
                    // Match the inventory order.
                    // Place weapon hardpoints first.
                    rc = compare("hpweapon");
                    if (rc != 0)
                        return rc;

                    // Then turrets.
                    rc = compare("hpturret");
                    if (rc != 0)
                        return rc;

                    // Then torpedoes/disruptors.
                    rc = compare("hptorpedo");
                    if (rc != 0)
                        return rc;

                    // Then mines.
                    rc = compare("hpmine");
                    if (rc != 0)
                        return rc;

                    // Then countermeasures.
                    rc = compare("hpcm");
                    if (rc != 0)
                        return rc;

                    // Then shields.
                    rc = compare("hpshield");
                    if (rc != 0)
                        return rc;

                    // Finally thrusters.
                    rc = compare("hpthruster");
                    if (rc != 0)
                        return rc;
                }
                return string.CompareOrdinal(sx, sy);
            }
        }

        /// <summary>
        /// Save the data in the treeview displayed by this form back into
        /// the specified file.
        /// </summary>
        /// <param name="filename"></param>
        public void SaveUTFFile(string filePath)
        {
            utfFile.SaveUTFFile(treeView1.Nodes[0], filePath);
            fileName = filePath;
            fileChangesNotSaved = false;
            this.Text = Path.GetFileName(fileName) + " - " + Path.GetDirectoryName(fileName);
        }

        /// <summary>
        /// Save the data in the treeview displayed by this form back into
        /// the specified file.
        /// </summary>
        /// <param name="filename"></param>
        public void SaveUTFFile(KeyValuePair<string, UTFForm> view)
        {
            utfFile.SaveUTFFile(view.Value.Tree.Nodes[0], view.Key);
            fileName = view.Key;
            fileChangesNotSaved = false;
            this.Text = Path.GetFileName(fileName) + " - " + Path.GetDirectoryName(fileName);
        }

        bool doubleClicked = false;

        // The node in the double-click event might not actually be the node that was
        // double-clicked, due to scrolling caused by also expanding or collapsing it.
        // Use the node from the click event, instead.
        TreeNode clickedNode;

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            clickedNode = e.Node;
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // The event is also triggered on the plus/minus sign, so make sure
            // it's the selected node.
            if (clickedNode != treeView1.SelectedNode)
                return;

            // Can't do anything with the top-level nodes.
            if (treeView1.SelectedNode.Parent == null)
                return;

            // Double-clicking a list hardpoint will move to its definition.
            if (treeView1.SelectedNode.Parent.Text == "Hardpoints")
            {
                TreeNode node = treeView1.Nodes[0].Nodes.Find(treeView1.SelectedNode.Text, true)[0];
                if (node != treeView1.SelectedNode)
                    treeView1.SelectedNode = node;
            }
            // Edit or view, depending which is available.
            else if (toolStripMenuItemEdit.Enabled)
            {
                EditNode();
            }
            else if (toolStripMenuItemView.Enabled)
            {
                ViewNode();
            }
        }

        private void treeView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                if(e.KeyCode == Keys.Enter)
                {
                    if (toolStripMenuItemEdit.Enabled)
                        EditNode();
                    else if (toolStripMenuItemView.Enabled)
                        ViewNode();
                }

                if (e.Control)
                {
                    switch (e.KeyCode)
                    {
                        case Keys.C:
                            e.Handled = Copy();
                            break;
                        case Keys.X:
                            e.Handled = Cut();
                            break;
                        case Keys.V:
                            if (e.Shift)
                                e.Handled = PasteChild();
                            else
                                e.Handled = Paste();
                            break;
                    }

                    if (e.Handled) e.SuppressKeyPress = true;
                }

                if (e.KeyCode == Keys.Delete && (e.Handled = Delete())) e.SuppressKeyPress = true;
            }
        }

        private void toolStripMenuItemExpandAll_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
                treeView1.SelectedNode.ExpandAll();
        }

        private void toolStripMenuItemCollapseAll_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
                treeView1.SelectedNode.Collapse();
        }

        private void toolStripMenuItemRenameNode_Click(object sender, EventArgs e)
        {
            RenameNode();
        }

        private void toolStripMenuItemAddNode_Click(object sender, EventArgs e)
        {
            AddNode("New");
        }

        private void toolStripMenuItemDeleteNode_Click(object sender, EventArgs e)
        {
            DeleteSelectedNodes();
        }

        public void AddNode(string name)
        {
            TreeNode node = new TreeNode(name);
            node.Name = name;
            node.Tag = new byte[0];

            if (treeView1.SelectedNode == null)
            {
                treeView1.Nodes.Add(node);
            }
            else
            {
                treeView1.SelectedNode.Nodes.Add(node);
                treeView1.SelectedNode.Expand();
            }
            treeView1.SelectedNode = node;
            
            if (name != "\\")
            {
                Modified();
                RenameNode();
            }
        }

        public void DeleteSelectedNodes()
        {
            if (treeView1.SelectedNodes != null)
            {
                treeView1.Delete();
            }
        }

        public void RenameNode()
        {
            if (treeView1.SelectedNode != null)
                treeView1.SelectedNode.BeginEdit();
        }

        private void stringToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditString();
        }

        public void EditString()
        {
            if (treeView1.SelectedNode != null)
            {
                EditStringForm edit = new EditStringForm(treeView1.SelectedNode);
                if (edit.ShowDialog(this) == DialogResult.OK)
                {
                    parent.SetSelectedNode(treeView1.SelectedNode);
                    Modified();
                }
                edit.Dispose();
                treeView1.Select();
            }
        }

        private void intArrayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditIntArray(false);
        }

        public void EditIntArray(bool hex)
        {
            if (treeView1.SelectedNode != null &&
                ((treeView1.SelectedNode.Tag as byte[]).Length & 3) == 0)
            {
                EditIntArrayForm edit = new EditIntArrayForm(treeView1.SelectedNode, hex);
                if (edit.ShowDialog(this) == DialogResult.OK)
                {
                    parent.SetSelectedNode(treeView1.SelectedNode);
                    Modified();
                }
                edit.Dispose();
                treeView1.Select();
            }
        }

        private void floatArrayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditFloatArray();
        }

        public void EditFloatArray()
        {
            if (treeView1.SelectedNode != null &&
                ((treeView1.SelectedNode.Tag as byte[]).Length & 3) == 0)
            {
                EditFloatArrayForm edit = new EditFloatArrayForm(treeView1.SelectedNode);
                if (edit.ShowDialog() == DialogResult.OK)
                {
                    parent.SetSelectedNode(treeView1.SelectedNode);
                    Modified();
                }
                edit.Dispose();
                treeView1.Select();
            }
        }

        public void EditColor()
        {
            if (treeView1.SelectedNode != null)
            {
                EditColorForm edit = new EditColorForm(treeView1.SelectedNode);
                if (edit.ShowDialog(this) == DialogResult.OK)
                {
                    parent.SetSelectedNode(treeView1.SelectedNode);
                    Modified();
                }
                edit.Dispose();
                treeView1.Select();
            }
        }

        private void toolStripMenuItemImportData_Click(object sender, EventArgs e)
        {
            ImportData();
        }

        public void ImportData()
        {
            if (treeView1.SelectedNode == null || treeView1.SelectedNode.Nodes.Count > 0)
            {
                MessageBox.Show(this, "Cannot import data to non-leaf nodes or multiple nodes", "Error");
                return;
            }

            openFileDialog1.Filter = "All Types (*.*)|*.*";
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    byte[] data = File.ReadAllBytes(openFileDialog1.FileName);
                    treeView1.SelectedNode.Tag = data;
                    parent.SetSelectedNode(treeView1.SelectedNode);
                    Modified();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error " + ex.Message, "Error");
                }
            }
        }

        private void toolStripMenuItemExportData_Click(object sender, EventArgs e)
        {
            ExportData();
        }

        public void ExportData()
        {
            if (treeView1.SelectedNode == null || treeView1.SelectedNode.Nodes.Count > 0)
            {
                MessageBox.Show(this, "Cannot export data from non-leaf nodes or multiple nodes", "Error");
                return;
            }

            saveFileDialog1.Filter = "All Types (*.*)|*.*";
            if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    byte[] data = treeView1.SelectedNode.Tag as byte[];
                    File.WriteAllBytes(saveFileDialog1.FileName, data);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error " + ex.Message, "Error");
                }
            }
        }

        public void ExportAllTextures(string path, bool resetConfig = true)
        {
            if (UTFEditorMain.LoadedFilesThreeViews == null || UTFEditorMain.LoadedFilesThreeViews.Count == 0) return;

            var ExportTextureRoot = new ExportTexturesConfig();

            foreach (var view in UTFEditorMain.LoadedFilesThreeViews)
            {
                var item = new ExportItem();
                item.NameFile = Path.GetFileName(view.Key);
                item.ExportTextures = new List<ExportTexture>();

                var fulldirPathRoot = Path.Combine(path, item.NameFile);
                if(!Directory.Exists(fulldirPathRoot))
                    Directory.CreateDirectory(fulldirPathRoot);

                foreach (TreeNode n in view.Value.Tree.Nodes[0].Nodes)
                {
                    foreach (TreeNode p in n.Nodes)
                    {
                        foreach (TreeNode m in p.Nodes)
                        {
                            try
                            {
                                if (m.Text == "MIPS")
                                {
                                    var fileName = Path.ChangeExtension(p.Text, ".dds");
                                    byte[] data = m.Tag as byte[];
                                    File.WriteAllBytes(fulldirPathRoot + "\\" + fileName, data);

                                    var texture = new ExportTexture();
                                    texture.Name = fileName;
                                    texture.Format = ".dds";
                                    item.ExportTextures.Add(texture);
                                }
                                else if (m.Text.StartsWith("MIP"))
                                {
                                    byte[] data = m.Tag as byte[];
                                    var fileName = Path.ChangeExtension(p.Text, ".tga");
                                    var dir = Path.Combine(fulldirPathRoot, fileName);
                                    if (!Directory.Exists(dir))
                                        Directory.CreateDirectory(dir);
                                    File.WriteAllBytes(dir + "\\" + fileName, data);
                                    File.WriteAllBytes(dir + "\\" + m.Text + "_" + fileName, data);

                                    var texture = new ExportTexture();
                                    texture.Name = m.Text + "_" + fileName;
                                    texture.Format = ".tga";
                                    item.ExportTextures.Add(texture);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(this, "Error " + ex.Message, "Error");
                            }
                        }
                    }
                }

                ExportTextureRoot.ExportItems.Add(item);
            }

            var filename = Path.Combine(path, "manifest.json");

            if (!resetConfig && File.Exists(filename))
            {
                var data = File.ReadAllBytes(filename);
                var oldData = JsonSerializer.Deserialize<ExportTexturesConfig>(data);

                // Добавляем только новые ключи
                foreach (var kvp in oldData.ExportItems)
                { 
                    bool exist = ExportTextureRoot.ExportItems.Any(it => it.NameFile == kvp.NameFile);
                    if (!exist)
                    {
                        ExportTextureRoot.ExportItems.Add(kvp);
                    }
                    else
                    {
                        var error = 0;
                    }
                }

                var jsonString = JsonSerializer.Serialize(ExportTextureRoot);
                File.WriteAllText(filename, jsonString);
            }
            else
            {
                var jsonString = JsonSerializer.Serialize(ExportTextureRoot);
                File.WriteAllText(filename, jsonString);
            }
        }

        private void BuildImportedHardpoints(TreeNode parent, THNEditor.ThnParse thn)
        {
            foreach (THNEditor.ThnParse.Entity en in thn.entities)
            {
                HardpointData hp = new HardpointData(en.entity_name, false);

                hp.PosX = en.pos.x;
                hp.PosY = en.pos.y;
                hp.PosZ = en.pos.z;

                hp.RotMatXX = en.rot1.x;
                hp.RotMatXY = en.rot1.y;
                hp.RotMatXZ = en.rot1.z;
                hp.RotMatYX = en.rot2.x;
                hp.RotMatYY = en.rot2.y;
                hp.RotMatYZ = en.rot2.z;
                hp.RotMatZX = en.rot3.x;
                hp.RotMatZY = en.rot3.y;
                hp.RotMatZZ = en.rot3.z;
                hp.Write();

                parent.Nodes.Add(hp.Node);
            }
        }

        THNEditor.ThnParse thn;
            
        public void ImportHardpointsFromTHN(string path)
        {
            thn = new THNEditor.ThnParse();
            thn.Parse(File.ReadAllText(path));

            TreeNode thnnode = new TreeNode("THN");
            thnnode.Name = "THN";

            TreeNode fixnode = new TreeNode("Fixed");
            fixnode.Name = "Fixed";

            TreeNode hpnode = new TreeNode("Hardpoints");
            hpnode.Name = "Hardpoints";

            hpnode.Nodes.Add(fixnode);
            thnnode.Nodes.Add(hpnode);

            treeView1.Nodes[0].Nodes.Add(thnnode);
            BuildImportedHardpoints(fixnode, thn);

            if (treeView1.Nodes.Count <= 1 || treeView1.Nodes[1].Text != "Hardpoints")
            {
                treeView1.Nodes.Insert(1, "Hardpoints");
            }
            else
            {
                treeView1.Nodes[1].Nodes.Clear();
            }

            foreach (THNEditor.ThnParse.Entity en in thn.entities)
            {
                TreeNode hp = new TreeNode(en.entity_name);
                hp.Name = en.entity_name;
                treeView1.Nodes[1].Nodes.Add(hp);
            }
        }

        public void ExportHardpointsToTHN(string path)
        {
            List<THNEditor.ThnParse.Entity> entities = new List<THNEditor.ThnParse.Entity>();
            foreach (TreeNode n in treeView1.Nodes[0].Nodes["THN"].Nodes["Hardpoints"].Nodes["Fixed"].Nodes)
            {
                foreach (THNEditor.ThnParse.Entity e in thn.entities)
                {
                    if (n.Name == e.entity_name)
                    {
                        HardpointData d = new HardpointData(n);
                        e.pos.x = d.PosX;
                        e.pos.y = d.PosY;
                        e.pos.z = d.PosZ;

                        e.rot1.x = d.RotMatXX;
                        e.rot1.y = d.RotMatXY;
                        e.rot1.z = d.RotMatXZ;

                        e.rot2.z = d.RotMatYX;
                        e.rot2.y = d.RotMatYY;
                        e.rot2.z = d.RotMatYZ;

                        e.rot3.x = d.RotMatZX;
                        e.rot3.y = d.RotMatZY;
                        e.rot3.z = d.RotMatZZ;
                        entities.Add(e);
                        break;
                    }
                }
            }
            thn.entities = entities;
            if (!File.Exists(path))
                File.Create(path);
            File.WriteAllText(path, thn.Write());
        }

        /// <summary>
        /// Access hardpoint nodes and generate a file with all hardpoint names
        /// </summary>
        /// <param name="path">Path with filename.ext</param>
        public void ExportHardpointsToFile(string path)
        {
            string hardpointNames = "";
            TreeNode Hardpoints = treeView1.Nodes.Cast<TreeNode>().FirstOrDefault(tn => tn.Text == "Hardpoints");

            foreach (TreeNode hardpointNode in Hardpoints.Nodes)
            {
                if (hardpointNode.Name.StartsWith("Hp"))
                {
                    hardpointNames += hardpointNode.Name + "\n";
                }
            }                            

            File.WriteAllText(path, hardpointNames);
        }

        public void MakeAnimFrames()
        {
            TreeNode node = treeView1.SelectedNode;
            if (node != null)
            {
                // If we're not on Channel, assume Header or Frames.
                if (!Utilities.StrIEq(node.Text, "Channel"))
                    node = node.Parent;
                new EditAnimChannel(node).Show(this);
            }
        }

        public void ViewVMeshData()
        {
            if (treeView1.SelectedNode == null || treeView1.SelectedNode.Nodes.Count > 0)
            {
                MessageBox.Show(this, "Cannot export data from non-leaf nodes or multiple nodes", "Error");
                return;
            }

            try
            {
                byte[] data = treeView1.SelectedNode.Tag as byte[];

                VMeshData decoded = new VMeshData(data);

                StringBuilder st = new StringBuilder(data.Length);
                st.AppendLine("---- HEADER ----");
                st.AppendLine();
                st.AppendFormat("Mesh Type                 = {0}\n", decoded.MeshType);
                st.AppendFormat("Surface Type              = {0}\n", decoded.SurfaceType);
                st.AppendFormat("Number of Meshes          = {0}\n", decoded.NumMeshes);
                st.AppendFormat("Total referenced vertices = {0}\n", decoded.NumRefVertices);
                st.AppendFormat("Flexible Vertex Format    = 0x{0:X}\n", decoded.FlexibleVertexFormat);
                st.AppendFormat("Total number of vertices  = {0}\n", decoded.NumVertices);
                st.AppendLine();

                st.AppendLine("---- MESHES ----");
                st.AppendLine();
                st.AppendLine("Mesh Number  MaterialID  Start Vertex  End Vertex  Start Triangle  NumRefVertex  Padding");
                for (int count = 0; count < decoded.Meshes.Count; count++)
                {
                    st.AppendFormat("{0,11}  0x{1:X8}  {2,12}  {3,10}  {4,14}  {5,12}  0x{6:X2}\n",
                        count,
                        decoded.Meshes[count].MaterialId,
                        decoded.Meshes[count].StartVertex,
                        decoded.Meshes[count].EndVertex,
                        decoded.Meshes[count].TriangleStart/3,
                        decoded.Meshes[count].NumRefVertices,
                        decoded.Meshes[count].Padding);
                }
                st.AppendLine();

                st.AppendLine("---- Triangles ----");
                st.AppendLine();
                st.AppendLine("Triangle  Vertex 1  Vertex 2  Vertex 3");
                for (int count = 0; count < decoded.Triangles.Count; count++)
                {
                    st.AppendFormat("{0,8}  {1,8}  {2,8}  {3,8}\n",
                        count,
                        decoded.Triangles[count].Vertex1,
                        decoded.Triangles[count].Vertex2,
                        decoded.Triangles[count].Vertex3);
                }
                st.AppendLine();

                bool hasNormals = (decoded.FlexibleVertexFormat & VMeshData.D3DFVF_NORMAL) == VMeshData.D3DFVF_NORMAL;
                bool hasDiffuse = (decoded.FlexibleVertexFormat & VMeshData.D3DFVF_DIFFUSE) == VMeshData.D3DFVF_DIFFUSE;

                st.AppendLine("---- Vertices ----");
                st.AppendLine();
                st.Append("Vertex    ----X----,   ----Y----,   ----Z----");
                if (hasNormals)
                    st.Append(",    Normal X,    Normal Y,    Normal Z");
                if (hasDiffuse)
                    st.Append(", -Diffuse-");
                uint fvfTexCount = decoded.FlexibleVertexFormat & VMeshData.D3DFVF_TEXCOUNT_MASK;
                switch (fvfTexCount)
                {
                    case VMeshData.D3DFVF_TEX1:
                        st.Append(",   ----U----,   ----V----");
                        break;
                    case VMeshData.D3DFVF_TEX2:
                        st.Append(",  ----U1----,  ----V1----,  ----U2----,  ----V2----");
                        break;
                    case VMeshData.D3DFVF_TEX3:
                        st.Append(",  ----U1----,  ----V1----,   Tangent X,   Tangent Y,   Tangent Z,  Tangent W");
                        break;
                    case VMeshData.D3DFVF_TEX4:
                        st.Append(",  ----U1----,  ----V1----,  ----U2----,  ----V2----,   Tangent X,   Tangent Y,   Tangent Z,  Tangent W");
                        break;
                }
                st.AppendLine();
                for (int count = 0; count < decoded.Vertices.Count; count++)
                {
                    st.AppendFormat("{0,6} {1,12:F6},{2,12:F6},{3,12:F6}",
                        count,
                        decoded.Vertices[count].X,
                        decoded.Vertices[count].Y,
                        decoded.Vertices[count].Z);
                    if (hasNormals)
                        st.AppendFormat(",{0,12:F6},{1,12:F6},{2,12:F6}",
                            decoded.Vertices[count].NormalX,
                            decoded.Vertices[count].NormalY,
                            decoded.Vertices[count].NormalZ);
                    if (hasDiffuse)
                        st.AppendFormat(", 0x{0:X8}", decoded.Vertices[count].Diffuse);
                    switch (fvfTexCount)
                    {
                        case VMeshData.D3DFVF_TEX1:
                            st.AppendFormat(",{0,12:F6},{1,12:F6}",
                            decoded.Vertices[count].S,
                            decoded.Vertices[count].T);
                            break;
                        case VMeshData.D3DFVF_TEX2:
                            st.AppendFormat(",{0,12:F6},{1,12:F6},{2,12:F6},{3,12:F6}",
                            decoded.Vertices[count].S,
                            decoded.Vertices[count].T,
                            decoded.Vertices[count].U,
                            decoded.Vertices[count].V);
                            break;
                        case VMeshData.D3DFVF_TEX3:
                            st.AppendFormat(",{0,12:F6},{1,12:F6},{2,12:F6},{3,12:F6},{4,12:F6},{5,12:F6}",
                            decoded.Vertices[count].S,
                            decoded.Vertices[count].T,
                            decoded.Vertices[count].TangentX,
                            decoded.Vertices[count].TangentY,
                            decoded.Vertices[count].TangentZ,
                            decoded.Vertices[count].TangentW);
                            break;
                        case VMeshData.D3DFVF_TEX4:
                            st.AppendFormat(",{0,12:F6},{1,12:F6},{2,12:F6},{3,12:F6},{4,12:F6},{5,12:F6},{6,12:F6},{7,12:F6}",
                            decoded.Vertices[count].S,
                            decoded.Vertices[count].T,
                            decoded.Vertices[count].U,
                            decoded.Vertices[count].V,
                            decoded.Vertices[count].TangentX,
                            decoded.Vertices[count].TangentY,
                            decoded.Vertices[count].TangentZ,
                            decoded.Vertices[count].TangentW);
                            break;
                    }                   
                    st.AppendLine();
                }

                // Extract the relevant portion from the name.
                string name = treeView1.SelectedNode.Parent.Name;
                // .vms
                int pos = name.LastIndexOf('.');
                // .lod*
                if (pos != -1)
                    pos = name.LastIndexOf('.', pos - 1, pos);
                // .file*
                if (pos != -1)
                    pos = name.LastIndexOf('.', pos - 1, pos);
                new ViewTextForm(name.Substring(pos + 1), st.ToString()).Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error " + ex.Message, "Error");
            }
        }

        public void ViewVMeshRef()
        {
            if (treeView1.SelectedNode == null || treeView1.SelectedNode.Nodes.Count > 0)
            {
                MessageBox.Show(this, "Cannot export data from non-leaf nodes or multiple nodes", "Error");
                return;
            }

            try
            {
                byte[] data = treeView1.SelectedNode.Tag as byte[];

                VMeshRef dec = new VMeshRef(data);

                StringBuilder st = new StringBuilder(data.Length);
                st.AppendFormat("Size             = {0}\n", dec.HeaderSize);
                st.AppendFormat("VMeshLibID       = {0}\n", FindVMeshName(dec.VMeshLibId, true));
                st.AppendFormat("StartVert        = {0}\n", dec.StartVert);
                st.AppendFormat("VertQuantity     = {0}\n", dec.NumVert);
                st.AppendFormat("StartIndex       = {0}\n", dec.StartIndex);
                st.AppendFormat("IndexQuantity    = {0}\n", dec.NumIndex);
                st.AppendFormat("StartMesh        = {0}\n", dec.StartMesh);
                st.AppendFormat("MeshQuantity     = {0}\n", dec.NumMeshes);
                st.AppendFormat("BoundingBox.MaxX = {0:F6}\n", dec.BoundingBoxMaxX);
                st.AppendFormat("BoundingBox.MinX = {0:F6}\n", dec.BoundingBoxMinX);
                st.AppendFormat("BoundingBox.MaxY = {0:F6}\n", dec.BoundingBoxMaxY);
                st.AppendFormat("BoundingBox.MinY = {0:F6}\n", dec.BoundingBoxMinY);
                st.AppendFormat("BoundingBox.MaxZ = {0:F6}\n", dec.BoundingBoxMaxZ);
                st.AppendFormat("BoundingBox.MinZ = {0:F6}\n", dec.BoundingBoxMinZ);
                st.AppendFormat("CentreX          = {0:F6}\n", dec.CenterX);
                st.AppendFormat("CentreY          = {0:F6}\n", dec.CenterY);
                st.AppendFormat("CentreZ          = {0:F6}\n", dec.CenterZ);
                st.AppendFormat("Radius           = {0:F6}\n", dec.Radius);

                new ViewTextForm(GetVMeshRefName(treeView1.SelectedNode), st.ToString()).Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error " + ex.Message, "Error");
            }
        }

        /// <summary>
        /// Get the name associated with the selected VMeshRef node.
        /// </summary>
        /// <returns></returns>
        public string GetVMeshRefName(TreeNode node)
        {
            string name = node.Parent.Parent.Name;
            string level = "";
            if (name.StartsWith("Level", StringComparison.OrdinalIgnoreCase))
            {
                level = "-" + name;
                name = node.Parent.Parent.Parent.Parent.Name;
            }
            return GetName(name) + level + ".vmr";
        }

        public void ViewVWireData()
        {
            if (treeView1.SelectedNode == null || treeView1.SelectedNode.Nodes.Count > 0)
            {
                MessageBox.Show(this, "Cannot export data from non-leaf nodes or multiple nodes", "Error");
                return;
            }

            try
            {
                byte[] data = treeView1.SelectedNode.Tag as byte[];

                VWireData decoded = new VWireData(data);

                StringBuilder st = new StringBuilder(data.Length);
                st.AppendLine("---- HEADER ----");
                st.AppendLine();
                st.AppendFormat("Structure Size       = {0}\n", decoded.HeaderSize);
                st.AppendFormat("VMeshLibID           = {0}\n", FindVMeshName(decoded.VMeshLibId, true));
                st.AppendFormat("Vertex Base          = {0}\n", decoded.VertexOffset);
                st.AppendFormat("Vertex Quantity      = {0}\n", decoded.NoVertices);
                st.AppendFormat("Ref Vertex Quantity  = {0}\n", decoded.NoRefVertices);
                st.AppendFormat("Vertex Range         = {0}\n", decoded.MaxVertNoPlusOne);
                st.AppendLine();

                st.AppendLine("---- Line Vertex List -----");
                st.AppendLine();
                for (int count = 0; count < decoded.Lines.Count; count++)
                {
                    st.AppendFormat("Line {0,5} = {1,5} to {2,5}\n",
                        count,
                        "v" + decoded.Lines[count].Point1.ToString(),
                        "v" + decoded.Lines[count].Point2.ToString());
                }

                // Find an appropriate name.
                string name = GetName(treeView1.SelectedNode.Parent.Parent.Name);
                new ViewTextForm(name + ".vwd", st.ToString()).Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error " + ex.Message, "Error");
            }
        }

        private void ViewTexture()
        {
            try
            {
                string name = (treeView1.SelectedNode.Nodes.Count == 0)
                    ? treeView1.SelectedNode.Parent.Name : treeView1.SelectedNode.Name;
                new ViewTextureForm(treeView1.SelectedNode, GetName(name)).Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error " + ex.Message, "Error");
            }
        }

        /// <summary>
        /// Given the name of a node, strip the timestamp and extension.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetName(string name)
        {
            if (name[0] == '\\')
                return Path.GetFileNameWithoutExtension(fileName);
            
            // Check for an extension.
            int pos = name.LastIndexOf('.');
            
            // Let's assume if the extension is not last,
            // it's the timestamp that follows.
            if (pos != -1)
            {
                if (pos != name.Length - 4)
                    return name.Remove(pos);
                name = name.Remove(pos);
            }

            // If the last twelve characters are digits, assume a timestamp.
            if (name.Length > 12)
            {
                for (pos = 0; pos < 12; ++pos)
                    if (!Char.IsDigit(name[name.Length - 12 + pos]))
                        return name;
                name = name.Remove(name.Length - 12);
            }

            return name;
        }

        private void toolStripMenuItemEdit_Click(object sender, EventArgs e)
        {
            EditNode();
        }

        /// <summary>
        /// Determine if a node contains actual data.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool ContainsData(TreeNode node)
        {
            if (node != null && node.Nodes.Count == 0)
            {
                byte[] data = node.Tag as byte[];
                if (data != null && data.Length > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determine if the node is able to be edited.
        /// </summary>
        /// <returns></returns>
        public Editable IsEditable(TreeNode node)
        {
            if (node == null)
                return Editable.No;

            if (FindHardpoint(node) != null)
                return Editable.Hardpoint;

            switch (node.Name.ToLowerInvariant())
            {
                case "vmeshref":
                    return (ContainsData(node)) ? Editable.VMeshRef : Editable.No;

                case "fix":
                case "loose":
                    return Editable.Fix;

                case "rev":
                case "pris":
                    return Editable.Rev;

                case "sphere":
                    return Editable.Sphere;

                case "channel":
                case "header":
                case "frames":
                    return Editable.Channel;

                case "ac":
                case "dc":
                case "ec":
                    return Editable.Color;

                case "constant":
                {
                    // Constant can be either RGB or A.
                    byte[] data = node.Tag as byte[];
                    return (data == null || data.Length == 0 || data.Length == 12)
                           ? Editable.Color : Editable.Float;
                }

                case "bt_name":
                case "child name":
                case "dm0_name":
                case "dm1_name":
                case "dm_name":
                case "dt_name":
                case "et_name":
                case "exporter version":
                case "file name":
                case "m0":
                case "m1":
                case "m2":
                case "m3":
                case "m4":
                case "m5":
                case "m6":
                case "material_name":
                case "name":
                case "object name":
				case "ot_name":
                case "parent name":
                case "type":
                    return Editable.String;

                // single value
                case "count":
                case "edge count":
                case "face count":
                case "flip u":
                case "flip v":
                case "frame count":
                case "image x size":
                case "image y size":
                case "index":
                case "macount":
                case "material count":
                case "material identifier":
                case "material":
                case "normal count":
                case "object vertex count":
                case "sides":
                case "surface normal count":
                case "texture count":
                case "texture vertex count":
                case "uv_bone_id":
                case "uv_vertex_count":
                case "vertex batch count":
                case "vertex count":
                
                // variable array
                case "bone_id_chain":
                case "point_bone_count":
                case "point_indices":
                case "uv0_indices":
                case "uv1_indices":
                    return Editable.Int;

                case "bt_flags":
                case "dm0_flags":
                case "dm1_flags":
                case "dm_flags":
                case "dt_flags":
                case "et_flags":
                case "flags":
                case "maflags":
				case "ot_flags":
                    return Editable.IntHex;
                    
                // single value
                case "alpha":
                case "blend":
                case "bone_x_to_u_scale":
                case "bone_y_to_v_scale":
                case "fade":
                case "fovx":
                case "fovy":
                case "fps":
                case "half x":
                case "half y":
                case "half z":
                case "length":
                case "mass":
                case "max_du":
                case "max_dv":
                case "min_du":
                case "min_dv":
                case "oc":
                case "radius":
                case "root height":
                case "scale":
                case "tilerate":
                case "tilerate0":
                case "tilerate1":
                case "uv_plane_distance":
                case "zfar":
                case "znear":

                // single angular value
                case "max":
                case "min":

                // variable arrays
                case "fractions":
                case "switch2":
                case "bone_weight_chain":
                case "uv0":
                case "uv1":
                case "points":
                case "vertex_normals":
                case "makeys":
                case "edge_angles":
                case "madeltas":

                // matrix
                case "inertia tensor":
                case "orientation":

                // transform
                case "bone to root":
                case "transform":

                // vector
                case "axis":
                case "center of mass":
                case "center":
                case "centroid":
                case "position":
                    return Editable.Float;
            }

            return Editable.No;
        }

        public void EditNode()
        {
            switch (IsEditable(treeView1.SelectedNode))
            {
                case Editable.VMeshRef:  EditVMeshRef();      break;
                case Editable.Fix:       EditFixData(treeView1.SelectedNode.Text); break;
                case Editable.Rev:       EditRevData(treeView1.SelectedNode.Text); break;
                case Editable.Sphere:    EditSphereData();    break;
                case Editable.Channel:   MakeAnimFrames();    break;
                case Editable.Hardpoint: EditHardpoint();     break;
                case Editable.Color:     EditColor();         break;
                case Editable.String:    EditString();        break;
                case Editable.Int:       EditIntArray(false); break;
                case Editable.IntHex:    EditIntArray(true);  break;
                case Editable.Float:     EditFloatArray();    break;
            }
        }

        private void toolStripMenuItemView_Click(object sender, EventArgs e)
        {
            ViewNode();
        }

        /// <summary>
        /// Determine if the node is able to be viewed.
        /// </summary>
        public Viewable IsViewable(TreeNode node)
        {
            if (node.Parent != null && Utilities.StrIEq(node.Parent.Text, "texture library"))
            {
                // Some textures contain animation data, rather than an image.
                return (node.Nodes["MIPS"] == null && node.Nodes["MIP0"] == null)
                       ? Viewable.No : Viewable.Texture;
            }

            if (!ContainsData(node))
                return Viewable.No;

            string text = node.Text;
            
            if (Utilities.StrIEq(text, "VMeshData"))
                return Viewable.VMeshData;

            if (Utilities.StrIEq(text, "VMeshRef"))
                return Viewable.VMeshRef;

            if (Utilities.StrIEq(text, "VWireData"))
                return Viewable.VWireData;

            if (text.StartsWith("MIP", StringComparison.OrdinalIgnoreCase) ||
                Utilities.StrIEq(text, "CUBE"))
                return Viewable.Texture;

            byte[] data = node.Tag as byte[];
            if (data.Length > 16 && data[0] == 'R' && 
                                    data[1] == 'I' && 
                                    data[2] == 'F' && 
                                    data[3] == 'F')
                return Viewable.Wave;

            return Viewable.No;
        }

        public void ViewNode()
        {
            switch (IsViewable(treeView1.SelectedNode))
            {
                case Viewable.VMeshData: ViewVMeshData(); break;
                case Viewable.VMeshRef:  ViewVMeshRef();  break;
                case Viewable.VWireData: ViewVWireData(); break;
                case Viewable.Texture:   ViewTexture();   break;
                case Viewable.Wave:      PlaySound();     break;
            }
        }

        /// <summary>
        /// Play the sound in the selected node. The sound is played in the background.
        /// </summary>
        public void PlaySound()
        {
            if (treeView1.SelectedNode == null || treeView1.SelectedNode.Nodes.Count > 0)
            {
                MessageBox.Show(this, "Cannot play sound from non-leaf nodes or multiple nodes", "Error");
                return;
            }

            byte[] data = treeView1.SelectedNode.Tag as byte[];
            if (data.Length < 16 || !(data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F'))
            {
                MessageBox.Show(this, "Not a valid sound leaf", "Error");
                return;
            }

            System.Threading.Thread thread = new System.Threading.Thread(PlaySoundImpl);
            thread.IsBackground = true;
            thread.Start(data);
        }

        [DllImport("WinMM.dll")]
        public static extern bool PlaySound(byte[] wfname, IntPtr hmod, int fuSound);
        public int SND_SYNC = 0x0000; // play synchronously (default)
        public int SND_ASYNC = 0x0001; // play asynchronously
        public int SND_NODEFAULT = 0x0002; // silence (!default) if sound not found
        public int SND_MEMORY = 0x0004; // pszSound points to a memory file

        /// <summary>
        /// Background thread implementation for sound playing.
        /// </summary>
        /// <param name="arg">byte[] to play</param>
        void PlaySoundImpl(object arg)
        {
            byte[] data = arg as byte[];
            try
            {
                PlaySound(data, IntPtr.Zero, SND_SYNC | SND_NODEFAULT | SND_MEMORY);
            }
            catch { }
        }

        public void ExportAndFixBoundingBox(StringBuilder sb)
        {
            bool needs_saved = false;

            SharpDX.BoundingBox bb = new SharpDX.BoundingBox(new SharpDX.Vector3(float.MaxValue), new SharpDX.Vector3(float.MinValue));
            // find vmeshrefs
            foreach (TreeNode node in this.treeView1.Nodes.Find("VMeshRef", true))
            {
                VMeshRef refdata = new VMeshRef(node.Tag as byte[]);

                // Fix bounding boxes, if needed
                bool fixedbb = false;
                float tmp;
                if (refdata.BoundingBoxMinX > refdata.BoundingBoxMaxX)
                {
                    fixedbb = true;
                    tmp = refdata.BoundingBoxMinX;
                    refdata.BoundingBoxMinX = refdata.BoundingBoxMaxX;
                    refdata.BoundingBoxMaxX = tmp;
                }

                if (refdata.BoundingBoxMinY > refdata.BoundingBoxMaxY)
                {
                    fixedbb = true;
                    tmp = refdata.BoundingBoxMinY;
                    refdata.BoundingBoxMinY = refdata.BoundingBoxMaxY;
                    refdata.BoundingBoxMaxY = tmp;
                }

                if (refdata.BoundingBoxMinZ > refdata.BoundingBoxMaxZ)
                {
                    fixedbb = true;
                    tmp = refdata.BoundingBoxMinZ;
                    refdata.BoundingBoxMinZ = refdata.BoundingBoxMaxZ;
                    refdata.BoundingBoxMaxZ = tmp;
                }

                if (fixedbb)
                {
                    node.Tag = refdata.GetBytes();
                    needs_saved = true;
                }

                SharpDX.BoundingBox bb2 = new SharpDX.BoundingBox(
                    new SharpDX.Vector3(refdata.BoundingBoxMinX, refdata.BoundingBoxMinY, refdata.BoundingBoxMinZ),
                    new SharpDX.Vector3(refdata.BoundingBoxMaxX, refdata.BoundingBoxMaxY, refdata.BoundingBoxMaxZ));

                SharpDX.BoundingBox.Merge(ref bb, ref bb2, out bb);
            }

            if (needs_saved)
                SaveUTFFile(fileName);

            sb.AppendLine($"{fileName}:");
            sb.AppendLine($"min: {bb.Minimum.ToString()}");
            sb.AppendLine($"max: {bb.Maximum.ToString()}");
        }


        /// <summary>
        /// Find the fix or loose node and open an editor for it.
        /// </summary>
        public void EditFixData(string type)
        {
            try
            {
                TreeNode[] nodes = treeView1.Nodes.Find(type, true);
                if (nodes.Length == 0)
                    throw new Exception(type + " node not found");
                treeView1.SelectedNode = nodes[0];
                new EditCmpFixData(this, type, treeView1.SelectedNode).Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error '" + ex.Message + "'", "Error");
            }
        }

        /// <summary>
        /// Find the rev or pris node and open an editor for it.
        /// </summary>
        public void EditRevData(string type)
        {
            try
            {
                TreeNode[] nodes = treeView1.Nodes.Find(type, true);
                if (nodes.Length == 0)
                    throw new Exception(type + " node not found");
                treeView1.SelectedNode = nodes[0];
				new EditCmpRevData(this, type, treeView1.SelectedNode).Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error '" + ex.Message + "'", "Error");
            }
        }

        /// <summary>
        /// Find the sphere node and open an editor for it.
        /// </summary>
        public void EditSphereData()
        {
            try
            {
                TreeNode[] nodes = treeView1.Nodes.Find("Sphere", true);
                if (nodes.Length == 0)
                    throw new Exception("Sphere node not found");
                treeView1.SelectedNode = nodes[0];
                new EditCmpSphereData(this, treeView1.SelectedNode).Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error '" + ex.Message + "'", "Error");
            }
        }

        /// <summary>
        /// stuff for tangent calculation
        /// </summary>
        
        public class Vec3
        {
            public float x, y, z;

            public Vec3(float xx, float yy, float zz)
            {
                x = xx;
                y = yy;
                z = zz;
            }

            public static Vec3 operator +(Vec3 a, Vec3 b)
            {
                Vec3 res = new Vec3(
                a.x + b.x,
                a.y + b.y,
                a.z + b.z
                );

                return res;
            }

            public static Vec3 operator -(Vec3 a, Vec3 b)
            {
                Vec3 res = new Vec3(
                a.x - b.x,
                a.y - b.y,
                a.z - b.z
                );

                return res;
            }

            public static Vec3 operator *(Vec3 a, float b)
            {
                Vec3 res = new Vec3(
                a.x * b,
                a.y * b,
                a.z * b);

                return res;
            }

            public static Vec3 operator /(Vec3 a, float b)
            {
                Vec3 res = new Vec3(
                a.x / b,
                a.y / b,
                a.z / b);

                return res;
            }

            //dot product
            public static float operator *(Vec3 a, Vec3 b)
            {
                return a.x * b.x + a.y * b.y + a.z * b.z;
            }            

            //cross product
            public Vec3 Cross(Vec3 b)
            {
                Vec3 res = new Vec3(
                this.y * b.z - this.z * b.y,
                this.z * b.x - this.x * b.z,
                this.x * b.y - this.y * b.x);

                return res;
            }

            public Vec3 Clone()
            {
                return new Vec3(x, y, z);
            }

            public double GetCosAngle(Vec3 b)
            {
                Vec3 normA = this.Clone();
                Vec3 normB = b.Clone();
                normalize(ref normA, ref normA);
                normalize(ref normB, ref normB);

               //Prevent rounding errors
                return Math.Min(Math.Max(normA * normB,-1),1);
            }
        };

        public class Vec2
        {
            public float x, y;

            public Vec2(float xx, float yy)
            {
                x = xx;
                y = yy;
            }

            public static Vec2 operator -(Vec2 a, Vec2 b)
            {
                Vec2 res = new Vec2(
                 a.x - b.x,
                 a.y - b.y
                 );

                return res;
            }

        };

        /// <summary>
        /// Computes tangent and binormal vector using uv texture coordinates based on standard method in the literature.
        /// Modified by Schmackbolzen to be a bit more robust and use cross product for egde cases.
        /// It fails if at least two texture coordinates are identical, which happens unfortunately.
        /// </summary>
        private static bool ComputeTangentBasis(
            Vec3 P1, Vec3 P2, Vec3 P3,
            Vec2 UV1, Vec2 UV2, Vec2 UV3,
            ref Vec3 tangent, ref Vec3 binormal)
        {
            Vec3 Edge1 = P2 - P1;
            Vec3 Edge2 = P3 - P1;
            Vec2 Edge1uv = UV2 - UV1;
            Vec2 Edge2uv = UV3 - UV1;

            float cp = Edge1uv.x * Edge2uv.y - Edge1uv.y * Edge2uv.x;


            if (cp != 0.0f)
            {
                float mul = 1.0f / cp;
                tangent = (Edge1 * Edge2uv.y - Edge2 * Edge1uv.y) * mul;
                binormal = (Edge2 * Edge1uv.x - Edge1 * Edge2uv.x) * mul;
                normalize(ref tangent, ref tangent);
                normalize(ref binormal, ref binormal);

            }
            else if (Edge1uv.y == 0 && Edge2uv.y == 0 && (Edge1uv.x != 0 || Edge2uv.x != 0))
            {
                //Use the cross product to get the tangent vector
                Vec3 normal = ComputeNormal(P1, P2, P3);
                binormal = (Edge2 * Edge1uv.x - Edge1 * Edge2uv.x);
                normalize(ref binormal, ref binormal);
                tangent = normal.Cross(binormal);
                normalize(ref tangent, ref tangent);
            }
            else if (Edge1uv.x == 0 && Edge2uv.x == 0 && (Edge1uv.y != 0 || Edge2uv.y != 0))
            {
                //Use the cross product to get the binormal vector
                Vec3 normal = ComputeNormal(P1, P2, P3);
                tangent = (Edge1 * Edge2uv.y - Edge2 * Edge1uv.y);
                normalize(ref tangent, ref tangent);
                binormal = normal.Cross(tangent);
                normalize(ref binormal, ref binormal);
            }
            else
            {
                //Since all cases failed, this method for now can not calculate the tangent and binormal vectors 
                return false;
            }

            return true;

        }

       private static Vec3 ComputeNormal(Vec3 P1, Vec3 P2, Vec3 P3, bool normalize_ = true)
        {
            Vec3 Edge1 = P2 - P1;
            Vec3 Edge2 = P3 - P1;

            Vec3 normal = Edge1.Cross(Edge2);
            if(normalize_)
                normalize(ref normal, ref normal);
            return normal;
        }

        private static void normalize(ref Vec3 dest, ref Vec3 src)
        {
            double len = Math.Sqrt(src.x * src.x + src.y * src.y + src.z * src.z);

            if (len == 0)
            {
                dest = new Vec3(0,0,0);
                return;
            }

            dest = src / (float)len;
        }

        private static double DegreeToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        class VertexNeigbours
        {
            public VertexNeigbours(Vec3 vertexPosition)
            {
                position = vertexPosition;
                vertexIndex = new List<int>();
            }

            public Vec3 position;
            public List<int> vertexIndex;

        }

        const float EPSILON = 1E-3f;
        const float MAX_VERTICE_DIST = 1E-3f;

        bool IsVeryClose(float p1, float p2)
        {
            return Math.Abs(p1 - p2) < MAX_VERTICE_DIST;
        }

        bool IsVeryClose(Vec3 v1, Vec3 v2)
        {
            return IsVeryClose(v1.x, v2.x) && IsVeryClose(v1.y, v2.y) && IsVeryClose(v1.z, v2.z);
        }

        class NormalGroup
        {
            public List<int> group = new List<int>();
        }

        class VerticeEdges
        {
            public Vec3[] edges = new Vec3[2];

            public void SetEdges(Vec3 p1, Vec3 p2, Vec3 p3)
            {
                edges[0] = p2 - p1;
                edges[1] = p3 - p1;
                normalize(ref edges[0], ref edges[0]);
                normalize(ref edges[1], ref edges[1]);
            }

            public bool SharesEdges(VerticeEdges otherEdges)
            {
                foreach (Vec3 eOther in otherEdges.edges)
                    foreach (Vec3 eOwn in edges)
                    {
                        //Check whether edges are parallel
                        Vec3 cross = eOther.Cross(eOwn);
                        float sum = Math.Abs(cross.x) + Math.Abs(cross.y) + Math.Abs(cross.z);
                        if (sum < EPSILON)
                            return true;
                    }
                 return false;
            }
        }
        /// <summary>
        /// Normal vector calculation algorithm by Schmackbolzen
        /// It tries to find all neighbouring vertices depending on the maximum angle and also factors in whether the edges of the triangles are shared.
        /// After that all normals which are considered neighbours are smoothed using area + angle weighting.
        /// Finally all the tangent and binormal vectors are calculated. 
        /// To make the calculation easier, all vertices are duplicated so that each triangle has its own vertices.
        /// 
        /// Note that this algorithm currently does not know whether two triangles are neighbored if they don't share vertices. If this is the case the smoothing of the normal vectors can be wrong.
        /// 
        /// After calculating the new normal vectors it calls "DuplicateVerticesAndCalculateTangentsAndUpdateMeshEntries" for calculating the tangent und binormal vectors.
        /// 
        /// Note that the own VMeshRef entry has to be corrected by caller (makes more sense). Other VMeshRef entries and the VMeshData entries are corrected.
        /// Also it relies on the meshes in VMeshData all being sequential and that they do not share vertices.
        ///
        /// Author: Schmackbolzen
        /// Todo: More improvements to the algorithm (including optimizations). Also detect neighboured triangles if the are not connected by a vertice.
        /// </summary>
        /// <returns> New number of vertices.</returns>
        private int DuplicateVerticesAndCalcNormalsAndTangents(ref VMeshData.TMeshHeader mesh, ref VMeshData meshdata, VMeshRef refdata, TreeNode meshDataNode, TreeNode refDataNode, int iCurrentMesh, int iTriIndexOffset, int iTriangleCount, int iVerticeOffset, int iVerticeCount, double maxCosAngleInRadians)
        {
            int iNewVertexCount = 0;

            VMeshData.TVertex[] newVertices = new VMeshData.TVertex[iTriangleCount * 3];

            //Used for fast lookup
            Vec3[] newVerticesNormalsCache = new Vec3[iTriangleCount * 3];

            //Stores edges for each vertex
            VerticeEdges[] newVerticesEdges = new VerticeEdges[iTriangleCount * 3];


            // duplicate triangles and calculate triangle normal (not smoothed)
            for (int iTriIndex = iTriIndexOffset; iTriIndex < (iTriIndexOffset + iTriangleCount); iTriIndex++)
            {
                // first initialize array
                int arrindex = iTriIndex - iTriIndexOffset;

                VMeshData.TTriangle tri = meshdata.Triangles[iTriIndex];
                VMeshData.TVertex[] vertraw = new VMeshData.TVertex[3];
                vertraw[0] = meshdata.Vertices[iVerticeOffset + tri.Vertex1];
                vertraw[1] = meshdata.Vertices[iVerticeOffset + tri.Vertex2];
                vertraw[2] = meshdata.Vertices[iVerticeOffset + tri.Vertex3];

                // now get the triangle vertices and calc
                Vec3 vert1pos = new Vec3(vertraw[0].X, vertraw[0].Y, vertraw[0].Z);
                Vec3 vert2pos = new Vec3(vertraw[1].X, vertraw[1].Y, vertraw[1].Z);
                Vec3 vert3pos = new Vec3(vertraw[2].X, vertraw[2].Y, vertraw[2].Z);


                //Area (no normalization) + angle weighting
                //https://stackoverflow.com/a/45496726
                Vec3 normal = ComputeNormal(vert1pos, vert2pos, vert3pos, false);

                //Angle weights
                float[] weights = new float[3];               

                weights[0] = (float) Math.Abs(Math.Acos((vert2pos - vert1pos).GetCosAngle(vert3pos - vert1pos)));
                weights[1] = (float) Math.Abs(Math.Acos((vert3pos - vert2pos).GetCosAngle(vert1pos - vert2pos)));
                weights[2] = (float) Math.Abs(Math.Acos((vert1pos - vert3pos).GetCosAngle(vert2pos - vert3pos)));


                for (int i = 0; i < 3; i++)
                {
                    Vec3 weightedNormal = normal * weights[i];
                    newVerticesNormalsCache[iNewVertexCount + i] = weightedNormal;
                    vertraw[i].NormalX = weightedNormal.x;
                    vertraw[i].NormalY = weightedNormal.y;
                    vertraw[i].NormalZ = weightedNormal.z;
                }
         
                newVertices[iNewVertexCount] = vertraw[0];
                newVerticesEdges[iNewVertexCount] = new VerticeEdges();
                newVerticesEdges[iNewVertexCount].SetEdges(vert1pos, vert2pos, vert3pos);
                tri.Vertex1 = iNewVertexCount;                
                iNewVertexCount++;

                newVertices[iNewVertexCount] = vertraw[1];
                newVerticesEdges[iNewVertexCount] = new VerticeEdges();
                newVerticesEdges[iNewVertexCount].SetEdges(vert2pos, vert1pos, vert3pos);
                tri.Vertex2 = iNewVertexCount;                
                iNewVertexCount++;

                newVertices[iNewVertexCount] = vertraw[2];
                newVerticesEdges[iNewVertexCount] = new VerticeEdges();
                newVerticesEdges[iNewVertexCount].SetEdges(vert3pos, vert1pos, vert2pos);
                tri.Vertex3 = iNewVertexCount;                
                iNewVertexCount++;

                meshdata.Triangles[iTriIndex] = tri;
            }


            //Generate list of vertex neighbours
            List<VertexNeigbours> neighbouredVertices = new List<VertexNeigbours>();

            for (int i = 0; i < newVertices.Length; i++)
            {
                ref VMeshData.TVertex currentVertice = ref newVertices[i];
                Vec3 currentVerticePos = new Vec3(currentVertice.X, currentVertice.Y, currentVertice.Z);
                bool found = false;
                foreach (var currentListItem in neighbouredVertices)
                {
                    if (IsVeryClose(currentListItem.position, currentVerticePos))
                    {
                        currentListItem.vertexIndex.Add(i);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    VertexNeigbours newNeighbours = new VertexNeigbours(currentVerticePos);
                    newNeighbours.vertexIndex.Add(i);
                    neighbouredVertices.Add(newNeighbours);
                }
            }



            //Calculate new normals based on neighbour vertices            
            foreach (var currentNeighbours in neighbouredVertices)
            {             
                List <HashSet<int>> normalSets = new List<HashSet<int>>();
             
                for (int i = 0; i < currentNeighbours.vertexIndex.Count; i++)
                {
                    int currentVertexIndex = currentNeighbours.vertexIndex[i];
                        
                    Vec3 currentCachedNormal = newVerticesNormalsCache[currentVertexIndex];
                    HashSet<int> currentSet = new HashSet<int>();
                    currentSet.Add(currentVertexIndex);

                    for (int j = 0; j < currentNeighbours.vertexIndex.Count; j++)
                    {
                        int otherVertexIndex = currentNeighbours.vertexIndex[j];
                        Vec3 otherCachedNormal = newVerticesNormalsCache[otherVertexIndex];
                        //Check if angle is small enough
                        if (currentCachedNormal.GetCosAngle(otherCachedNormal) >= maxCosAngleInRadians - EPSILON)
                            //Only add if edge is shared
                            if (newVerticesEdges[currentVertexIndex].SharesEdges(newVerticesEdges[otherVertexIndex]))
                            {
                                currentSet.Add(otherVertexIndex);                    
                            }
                    }
                    if (currentSet.Count > 0)
                        normalSets.Add(currentSet);

                }
                //Now combine those sets which have at least one common element to normal groups;
                List<NormalGroup> normalGroups = new List<NormalGroup>();
                foreach (var currentSet in normalSets)
                {
                    if (currentSet.Count == 0)
                        continue;
                    HashSet<int> combinedSet = new HashSet<int>();
                    combinedSet.UnionWith(currentSet);
                    bool changed;
                    do
                    {
                        changed = false;
                        foreach (var otherSet in normalSets)
                        {
                            if (combinedSet.Overlaps(otherSet))
                            {
                                combinedSet.UnionWith(otherSet);
                                otherSet.Clear();
                                changed = true;
                            }
                        }
                    } while (changed);
                    NormalGroup newGroup = new NormalGroup();
                    foreach (int normalIndex in combinedSet)
                        newGroup.group.Add(normalIndex);
                    normalGroups.Add(newGroup);
                }                    

                //Average normal for each group and write it back
                foreach (NormalGroup currentNormalGroup in normalGroups)
                {
                    //Generate average
                    Vec3 averagedNormal = new Vec3(0, 0, 0);
                    foreach (int normalIndex in currentNormalGroup.group)
                    {
                        averagedNormal += newVerticesNormalsCache[normalIndex];
                    }
                    normalize(ref averagedNormal, ref averagedNormal);

                    //Write back
                    foreach (int vertexIndex in currentNormalGroup.group)
                    {
                        VMeshData.TVertex vertraw = newVertices[vertexIndex];
                        vertraw.NormalX = averagedNormal.x;
                        vertraw.NormalY = averagedNormal.y;
                        vertraw.NormalZ = averagedNormal.z;

                        newVertices[vertexIndex] = vertraw;
                    }
                }
            }

            //Calculate tangents and update mesh entries. The new vertices also are inserted into the mesh.
            //Also tell the function that the vertices are already duplicated, so it does not duplicate them again.
            return DuplicateVerticesAndCalculateTangentsAndUpdateMeshEntries(ref mesh, ref meshdata, ref newVertices, refdata, meshDataNode, refDataNode, iCurrentMesh, iTriIndexOffset, iTriangleCount, iVerticeOffset, iVerticeCount, true, iNewVertexCount);
        }

        /// <summary>
        /// Tangent and binormal vector calculation algorithm based on how it is usualy done in the literature.
        /// To make the calculation easier, all vertices are duplicated so that each triangle has its own vertices, unless the parameter verticesAreAlreadyDuplicated is set to true in which case it assumes they already are duplicated.
        /// 
        /// Note that the own VMeshRef entry has to be corrected by caller (makes more sense). Other VMeshRef entries and the VMeshData entries are corrected.
        /// Also it relies on the meshes in VMeshData all being sequential and that they do not share vertices.
        /// 
        /// Author: Schmackbolzen
        /// </summary>
        /// <returns>New number of vertices.</returns>
        private int DuplicateVerticesAndCalculateTangentsAndUpdateMeshEntries(ref VMeshData.TMeshHeader mesh, ref VMeshData meshdata, ref VMeshData.TVertex[] newVertices, VMeshRef refdata, TreeNode meshDataNode, TreeNode refDataNode, int iCurrentMesh, int iTriIndexOffset, int iTriangleCount, int iVerticeOffset, int iVerticeCount, bool verticesAreAlreadyDuplicated, int iNewVertexCount)
        {
         
            // iterate triangles
            for (int iTriIndex = iTriIndexOffset; iTriIndex < (iTriIndexOffset + iTriangleCount); iTriIndex++)
            {
                // first initialize array
                int arrindex = iTriIndex - iTriIndexOffset;

                VMeshData.TTriangle tri = meshdata.Triangles[iTriIndex];
                VMeshData.TVertex[] vertraw = new VMeshData.TVertex[3];
                if (!verticesAreAlreadyDuplicated)
                {
                    vertraw[0] = meshdata.Vertices[iVerticeOffset + tri.Vertex1];
                    vertraw[1] = meshdata.Vertices[iVerticeOffset + tri.Vertex2];
                    vertraw[2] = meshdata.Vertices[iVerticeOffset + tri.Vertex3];
                }
                else
                {
                    vertraw[0] = newVertices[tri.Vertex1];
                    vertraw[1] = newVertices[tri.Vertex2];
                    vertraw[2] = newVertices[tri.Vertex3];
                }

                // now get the triangle vertices and calc
                Vec3 vert1pos = new Vec3(vertraw[0].X, vertraw[0].Y, vertraw[0].Z);
                Vec2 vert1uv = new Vec2(vertraw[0].S, vertraw[0].T);

                Vec3 vert2pos = new Vec3(vertraw[1].X, vertraw[1].Y, vertraw[1].Z);
                Vec2 vert2uv = new Vec2(vertraw[1].S, vertraw[1].T);

                Vec3 vert3pos = new Vec3(vertraw[2].X, vertraw[2].Y, vertraw[2].Z);
                Vec2 vert3uv = new Vec2(vertraw[2].S, vertraw[2].T);

                Vec3 tangent = new Vec3(1, 0, 0);
                Vec3 biNormal = new Vec3(0, 1, 0);
                //For now ignore failed calculation of tangent and binormal vectors. In this case the above vectors will be used, which is not correct.
                //Maybe this algorithm can be replaced with something better later on.
                bool success=ComputeTangentBasis(vert1pos, vert2pos, vert3pos, vert1uv, vert2uv, vert3uv, ref tangent, ref biNormal);               
                for (int i = 0; i < 3; i++)
                {
                    //gram-schmidt
                    Vec3 normal = new Vec3(vertraw[i].NormalX, vertraw[i].NormalY, vertraw[i].NormalZ);
                    normalize(ref normal, ref normal);

                    Vec3 newTangent = tangent - normal * (normal * tangent);
                    normalize(ref newTangent, ref newTangent);                       

                    Vec3 newBiNormal = normal.Cross(newTangent);
                    float sgn = (newBiNormal) * biNormal;
                    if (sgn < 0)
                        sgn = -1;
                    else
                        sgn = 1;
                    newBiNormal = newBiNormal * sgn;
                    vertraw[i].BinormalX = newBiNormal.x;
                    vertraw[i].BinormalY = newBiNormal.y;
                    vertraw[i].BinormalZ = newBiNormal.z;
                    vertraw[i].TangentX = newTangent.x;
                    vertraw[i].TangentY = newTangent.y;
                    vertraw[i].TangentZ = newTangent.z;

                    //Get orientation for compact representation (tangent.W stores orientation of binormal vector)
                    float orient = normal * (newTangent.Cross(newBiNormal));
                    vertraw[i].TangentW = orient > 0 ? 1 : -1;
                }

                if (!verticesAreAlreadyDuplicated)
                {
                    newVertices[iNewVertexCount] = vertraw[0];
                    tri.Vertex1 = iNewVertexCount;
                    iNewVertexCount++;
                    newVertices[iNewVertexCount] = vertraw[1];
                    tri.Vertex2 = iNewVertexCount;
                    iNewVertexCount++;
                    newVertices[iNewVertexCount] = vertraw[2];
                    tri.Vertex3 = iNewVertexCount;
                    iNewVertexCount++;
                    meshdata.Triangles[iTriIndex] = tri;
                }
                else
                {
                    newVertices[tri.Vertex1] = vertraw[0];
                    newVertices[tri.Vertex2] = vertraw[1];
                    newVertices[tri.Vertex3] = vertraw[2];
                }
            }
            
            //Write new vertices to mesh
            meshdata.Vertices.RemoveRange(iVerticeOffset,iVerticeCount);
            meshdata.Vertices.InsertRange(iVerticeOffset, newVertices);

            //Update mesh entries for new vertex count
            int iVertexCorrection = iNewVertexCount - iVerticeCount;
            int iLastEndVertex = 0;

            //Update meshes within
            // for (int iMeshCorrect = refdata.StartMesh; iMeshCorrect < refdata.StartMesh + refdata.NumMeshes; iMeshCorrect++)
            for (int iMeshCorrect = iCurrentMesh+1; iMeshCorrect < refdata.StartMesh + refdata.NumMeshes; iMeshCorrect++)
            {
                //if (iMeshCorrect != iCurrentMesh)
                {
                    VMeshData.TMeshHeader meshCorrect = meshdata.Meshes[iMeshCorrect];
                   // if (meshCorrect.StartVertex > mesh.EndVertex)
                    {
                        iLastEndVertex = meshCorrect.EndVertex;
                        meshCorrect.StartVertex += iVertexCorrection;
                        meshCorrect.EndVertex += iVertexCorrection;
                        meshdata.Meshes[iMeshCorrect] = meshCorrect;
                    }                  

                }
            }
            iLastEndVertex += refdata.StartVert;


            //Update startvertex of other VMeshRef entries
            //Own VMeshRef entry will be corrected by caller
            foreach (TreeNode currentNode in this.treeView1.Nodes.Find("VMeshRef", true))
            {
                VMeshRef currentRefData = new VMeshRef(currentNode.Tag as byte[]);
                if (Utilities.FLModelCRC(meshDataNode.Parent.Name) == currentRefData.VMeshLibId)
                {                    
                    if (currentNode != refDataNode && currentRefData.StartMesh > iCurrentMesh)
                    {
                        currentRefData.StartVert += (ushort)iVertexCorrection;                        
                        string oldName2 = currentNode.Name;
                        object oldData2 = currentNode.Tag;
                        currentNode.Tag = currentRefData.GetBytes();
                        this.NodeChanged(currentNode, oldName2, oldData2);
                    }                    
                }
            }
            
            mesh.EndVertex += iVertexCorrection;           
            meshdata.Meshes[iCurrentMesh] = mesh;

            return iNewVertexCount;
        }

        /// <summary>
        /// Calls DuplicateVerticesAndCalculateTangentsAndUpdateMeshEntries with parameter verticesAreAlreadyDuplicated is set to true for duplicating vertices and calculation of tangent and binormal vectors.
        /// 
        /// Author: Schmackbolzen
        /// </summary>
        /// <returns>New number of vertices.</returns>
        private int  DuplicateVerticesAndCalcTangents(ref VMeshData.TMeshHeader mesh, ref VMeshData meshdata, VMeshRef refdata, TreeNode meshDataNode, TreeNode refDataNode, int iCurrentMesh, int iTriIndexOffset, int iTriangleCount, int iVerticeOffset, int iVerticeCount)
        {
            int iNewVertexCount = 0;

            VMeshData.TVertex[] newVertices = new VMeshData.TVertex[iTriangleCount*3];
            return DuplicateVerticesAndCalculateTangentsAndUpdateMeshEntries(ref mesh, ref meshdata, ref newVertices, refdata, meshDataNode, refDataNode, iCurrentMesh, iTriIndexOffset, iTriangleCount, iVerticeOffset, iVerticeCount, false, iNewVertexCount);
        }

        class SummedMeshlibEntries
        {
            public SummedMeshlibEntries(SummedMeshlibEntries src)
            {
                StartVertex = src.StartVertex;
                EndVertex = src.EndVertex;
                StartIndex = src.StartIndex;
            }

            public SummedMeshlibEntries()
            {
                StartVertex = 0;
                EndVertex = 0;
                StartIndex = 0;
            }

            public int StartVertex;
            public int EndVertex;
            public int StartIndex;
        }

        /// <summary>
        /// Makes sure that all vertices from meshes are sorted so that they are in sequential order (meaning one set of vertices and triangles of a mesh after another)
        /// and that only one mesh uses a set of vertices and triangles. This makes processing later on a lot easier.
        /// 
        /// Author: Schmackbolzen
        /// </summary>
        /// <param name="meshData"></param>
        /// <param name="meshDataNode"></param>
        /// <exception cref="Exception"></exception>
        /// <returns>True if meshdata changed, false otherwise</returns>

        public bool MakeSureMeshesAreSequential(ref VMeshData meshData, TreeNode meshDataNode)
        {
            //First build a list which contains the complete vertex start and end index. For that VMeshRef entries have to be added.
            SummedMeshlibEntries[] summedEntries = new SummedMeshlibEntries[meshData.Meshes.Count];
           
            bool foundVmeshRef=false;
            foreach (TreeNode currentNode in this.treeView1.Nodes.Find("VMeshRef", true))
            {
                VMeshRef currentMeshRef = new VMeshRef(currentNode.Tag as byte[]);
                if (Utilities.FLModelCRC(meshDataNode.Parent.Name) == currentMeshRef.VMeshLibId)
                {
                    foundVmeshRef = true;
                    for (int iMeshindex = currentMeshRef.StartMesh; iMeshindex < currentMeshRef.StartMesh + currentMeshRef.NumMeshes; iMeshindex++)
                    {                        
                        if(summedEntries[iMeshindex]!=null)
                            throw new Exception("Error: Meshes are used in multpiple VMeshRef entries instead of only one");

                        SummedMeshlibEntries meshlibCorrection = new SummedMeshlibEntries();
                        meshlibCorrection.StartVertex = meshData.Meshes[iMeshindex].StartVertex + currentMeshRef.StartVert;
                        meshlibCorrection.EndVertex = meshData.Meshes[iMeshindex].EndVertex + currentMeshRef.StartVert;

                        summedEntries[iMeshindex] =meshlibCorrection;
                    }                        
                }
            }

            //Check if meshdata is used for meshes (could be e.g. used for wire data)
            if (!foundVmeshRef)
                return false;

            //Sanity check
            Debug.Assert(summedEntries[0].StartVertex < summedEntries[0].EndVertex, "Error: End vertex of first mesh is not greater than start vertex");

            bool errorFound = false;
            bool sortedByVertices = true;            

            int lastStartVertex = summedEntries[0].StartVertex;
            //Check for vertex range overlaps
            //Also check whether the order of meshes is sorted by startvertex
            for (int i = 1; i < summedEntries.Length; i++)
            {
                if (summedEntries[i].StartVertex != summedEntries[i-1].EndVertex + 1)
                {
                    //Debug.Assert(summedEntries[i].StartVertex > summedEntries[i - 1].EndVertex, "Error: There is a vertex gap between meshes");
                    errorFound = true;     
                    //No break, because start vertex order has to be checked
                }      
                if(summedEntries[i].StartVertex < lastStartVertex)
                {
                    sortedByVertices = false;
                    errorFound = true;
                    break;
                }
                lastStartVertex = summedEntries[i].StartVertex;
            }
                        
            if (errorFound)
            {               
                //Create list for storing corrected entries
                List<SummedMeshlibEntries> correctedEntries = new List<SummedMeshlibEntries>(summedEntries.Length);

                //Deep copy
                foreach (SummedMeshlibEntries element in summedEntries)
                    correctedEntries.Add(new SummedMeshlibEntries(element));
                
                if (!sortedByVertices)
                {
                    //Since meshes are not sorted by start vertex, rebuild meshes including vertices and triangles completely via copying in correct order, which also results in no meshes with overlapping vertices (this is needed as well)

                    //First determine new order
                    int[] newOrder = new int[summedEntries.Length];

                    //Create array with a copy of the start vertices
                    int[] startVertices = new int[summedEntries.Length];                    
                    for (int i = 0; i < summedEntries.Length; i++)
                        startVertices[i] = summedEntries[i].StartVertex;

                    //Find new order by finding smallest start vertex for each slot.
                    //Complexity is O(n²) due to two loops, but iterating should be faster due to better cpu cache utilization (both arrays are a memory block) than having a list and removing elements once found                    
                    for (int i=0;i< summedEntries.Length;i++)
                    {
                        int smallestStartVertex = Int32.MaxValue;
                        int smallestStartVertexIndex = -1;
                        for (int k = 0; k < summedEntries.Length; k++)
                        {
                            //Use <= in case some start vertices were identical
                            if (startVertices[k]<= smallestStartVertex)
                            {
                                smallestStartVertex = startVertices[k];
                                smallestStartVertexIndex = k;
                            }
                        }
                        //Sanity check
                        Debug.Assert(smallestStartVertexIndex > -1);
                        //Store found index with smallest start vertex
                        newOrder[i] = smallestStartVertexIndex;
                        //Mark as found
                        startVertices[smallestStartVertexIndex] = Int32.MaxValue;                        
                    }

                    //Now copy all data
                    List<TVertex> verticesCorrected = new List<TVertex>(meshData.Vertices.Count);
                    List<TTriangle> trianglesCorrected = new List<TTriangle>(meshData.Triangles.Count);

                    int[] triangleStarts = new int[summedEntries.Length];

                    for (int i = 0; i < summedEntries.Length; i++)                    
                        triangleStarts[i]=meshData.Meshes[i].TriangleStart / 3;
                    
                    List<TMeshHeader> sortedMeshes = new List<TMeshHeader>(meshData.Meshes.Count);

                    for (int i = 0; i < summedEntries.Length; i++)
                    {
                        int newIndex = newOrder[i];
                        int triangleCount;

                        //Get triangle count
                        if (newIndex < summedEntries.Length - 1)
                            triangleCount = triangleStarts[newIndex + 1];
                        else
                            triangleCount = meshData.Triangles.Count;
                        triangleCount -= triangleStarts[newIndex];

                        int vertexCount = summedEntries[newIndex].EndVertex - summedEntries[newIndex].StartVertex + 1;

                        correctedEntries[i].StartVertex = verticesCorrected.Count;
                        correctedEntries[i].EndVertex = verticesCorrected.Count + vertexCount -1;
                        correctedEntries[i].StartIndex= trianglesCorrected.Count*3;                        
                        verticesCorrected.AddRange(meshData.Vertices.GetRange(summedEntries[newIndex].StartVertex, vertexCount));                        
                        trianglesCorrected.AddRange(meshData.Triangles.GetRange(triangleStarts[newIndex], triangleCount));
                        TMeshHeader sortedMeshHeader = meshData.Meshes[newIndex];
                        sortedMeshHeader.TriangleStart = correctedEntries[i].StartIndex;
                        sortedMeshes.Add(sortedMeshHeader);
                    }
                    meshData.Vertices = verticesCorrected;
                    meshData.Triangles = trianglesCorrected;
                    meshData.Meshes = sortedMeshes;
                }
                else
                {
                    //Since meshes are sorted by start vertex just copy vertices.
                    //This is done by completely copying all used vertices of a mesh. Triangles are not touched.

                    List<TVertex> verticesCorrected = new List<TVertex>(meshData.Vertices.Count);
                    verticesCorrected.AddRange(meshData.Vertices.GetRange(summedEntries[0].StartVertex, summedEntries[0].EndVertex - summedEntries[0].StartVertex + 1));
                    for (int i = 1; i < summedEntries.Length; i++)
                    {
                        if (summedEntries[i].StartVertex != summedEntries[i - 1].EndVertex + 1)
                        {
                            //Vertices are used by the mesh before. Copy used ones from this mesh and correct entries.
                            int start = verticesCorrected.Count;
                            int end = summedEntries[i].EndVertex - summedEntries[i].StartVertex + 1;
                            correctedEntries[i].StartVertex = start;
                            correctedEntries[i].EndVertex = start + end;

                            verticesCorrected.AddRange(meshData.Vertices.GetRange(summedEntries[i].StartVertex, end + 1));
                        }
                        else
                        {
                            //Everthing is fine, just copy original vertices without any change
                            correctedEntries[i] = summedEntries[i];
                            verticesCorrected.AddRange(meshData.Vertices.GetRange(summedEntries[i].StartVertex, summedEntries[i].EndVertex - summedEntries[i].StartVertex + 1));
                        }
                    }
                    meshData.Vertices = verticesCorrected;
                }

                //Correct VMeshRef and mesh header entries
                foreach (TreeNode currentNode in this.treeView1.Nodes.Find("VMeshRef", true))
                {
                    VMeshRef currentMeshRef = new VMeshRef(currentNode.Tag as byte[]);
                    if (Utilities.FLModelCRC(meshDataNode.Parent.Name) == currentMeshRef.VMeshLibId)
                    {
                        int startVertex = 0;
                        for (int iMeshindex = currentMeshRef.StartMesh; iMeshindex < currentMeshRef.StartMesh + currentMeshRef.NumMeshes; iMeshindex++)
                        {
                            TMeshHeader currentMeshHeader = meshData.Meshes[iMeshindex];

                            //The starting mesh needs different entries
                            if (iMeshindex == currentMeshRef.StartMesh)
                            {                                
                                currentMeshHeader.StartVertex = 0;
                                currentMeshHeader.EndVertex = correctedEntries[iMeshindex].EndVertex - correctedEntries[iMeshindex].StartVertex;
                                currentMeshRef.StartVert = (ushort)correctedEntries[iMeshindex].StartVertex;
                                if (!sortedByVertices)
                                {
                                    currentMeshRef.StartIndex = (ushort)correctedEntries[iMeshindex].StartIndex;
                                }
                            }
                            else
                            {
                                currentMeshHeader.StartVertex = startVertex;
                                currentMeshHeader.EndVertex = correctedEntries[iMeshindex].EndVertex - correctedEntries[iMeshindex].StartVertex + startVertex;

                            }                           
                            meshData.Meshes[iMeshindex] = currentMeshHeader;
                            startVertex += correctedEntries[iMeshindex].EndVertex - correctedEntries[iMeshindex].StartVertex + 1;
                        }
                    }
                    string oldName2 = currentNode.Name;
                    object oldData2 = currentNode.Tag;
                    currentNode.Tag = currentMeshRef.GetBytes();
                    this.NodeChanged(currentNode, oldName2, oldData2);
                }               
            }
            return true;
        }

        public class WireDataPoints
        {
            public struct LinePoints
            {
                public LinePoints(float X1, float Y1, float Z1, float X2, float Y2, float Z2)
                {
                    this.X1 = X1;
                    this.Y1 = Y1;
                    this.Z1 = Z1;
                    this.X2 = X2;
                    this.Y2 = Y2;
                    this.Z2 = Z2;
                }
                public float X1, Y1, Z1;
                public float X2, Y2, Z2;
            }

            public TreeNode wiredata_node;
            public TreeNode meshdata_node;
            public List<LinePoints> points = new List<LinePoints>();            
        }

        /// <summary>
        /// Calc binormal and tangent vectors for model (to use normal mapping)
        /// I decided to duplicate all vertices for each triangle as this makes it a lot easier and the resulting vectors are not smoothed (which introduces additional errors when rendering).
        /// Maybe this can be optimized later.
        /// 
        /// Todo: Add extra step at the end to find identical vertices and remove duplicates. Although with the added vectors that probability is reduced.
        /// Author: Schmackbolzen
        /// </summary>
        public void CalcTangents(bool calculateNormals, double maxAngleInDegrees, bool quiet = false)
        {
            List<WireDataPoints> storedWireDataPoints = new List<WireDataPoints>();
            //Store all VMeshWire points
            foreach (TreeNode wiredata_node in this.treeView1.Nodes.Find("VWireData", true))
            {
                VWireData wiredata = new VWireData(wiredata_node.Tag as byte[]);
                foreach (TreeNode meshdata_node in this.treeView1.Nodes.Find("VMeshData", true))
                {
                    if (Utilities.FLModelCRC(meshdata_node.Parent.Name) == wiredata.VMeshLibId)
                    {
                        VMeshData meshdata = new VMeshData(meshdata_node.Tag as byte[]);

                        WireDataPoints storedPoints=new WireDataPoints();
                        storedPoints.wiredata_node = wiredata_node;
                        storedPoints.meshdata_node = meshdata_node;
                        
                        storedPoints.points.Capacity = wiredata.Lines.Count;
                        foreach (VWireData.Line currentLine in wiredata.Lines)
                        {
                            TVertex from = meshdata.Vertices[currentLine.Point1 + wiredata.VertexOffset];
                            TVertex to = meshdata.Vertices[currentLine.Point2 + wiredata.VertexOffset];

                            storedPoints.points.Add(new WireDataPoints.LinePoints(from.X, from.Y, from.Z, to.X, to.Y, to.Z));
                        }
                        storedWireDataPoints.Add(storedPoints);
                        break;
                    }
                }
            }

            //Make sure all VMeshData meshes are sequential
            foreach (TreeNode meshdata_node in this.treeView1.Nodes.Find("VMeshData", true))
            {
                VMeshData meshdata = new VMeshData(meshdata_node.Tag as byte[]);
                if (MakeSureMeshesAreSequential(ref meshdata, meshdata_node))
                {
                    meshdata.NumVertices = (ushort)meshdata.Vertices.Count;
                    meshdata.NumMeshes = (ushort)meshdata.Meshes.Count;
                    string oldName = meshdata_node.Name;
                    object oldData = meshdata_node.Tag;

                    // save the VMeshData back to the UTF
                    meshdata_node.Tag = meshdata.GetRawData();

                    // communicate change
                    this.NodeChanged(meshdata_node, oldName, oldData);
                }
            }

            double maxCosAngleInRadians = Math.Cos(DegreeToRadians(maxAngleInDegrees));
            // find vmeshdata
            foreach (TreeNode meshDataNode in this.treeView1.Nodes.Find("VMeshData", true))
            {
                VMeshData meshData = new VMeshData(meshDataNode.Tag as byte[]);
                // find vmeshrefs
                foreach (TreeNode refDataNode in this.treeView1.Nodes.Find("VMeshRef", true))
                {
                    VMeshRef refData = new VMeshRef(refDataNode.Tag as byte[]);               
                    if (Utilities.FLModelCRC(meshDataNode.Parent.Name) == refData.VMeshLibId)
                    {
                        // found matching meshdata and refdata                      

                        // handle FVF formats

                        switch (meshData.FlexibleVertexFormat)
                        {
                            case 0x112:
                                meshData.FlexibleVertexFormat = 0x312;
                                break;
                            case 0x212:
                                meshData.FlexibleVertexFormat = 0x412;
                                break;
                            case 0x312:
                            case 0x412:    
                                break;
                            default:
                                continue;
                        }

                        int newVertexCount = 0;
                        int newTriangleCount = 0;

                        // iterate meshes
                        for (int iMesh = refData.StartMesh; iMesh < (refData.StartMesh + refData.NumMeshes); iMesh++)
                        {
                            // on every mesh, calculate tangent data

                            VMeshData.TMeshHeader mesh = meshData.Meshes[iMesh];

                            int iVerticeOffset = refData.StartVert + mesh.StartVertex;
                            int iTriIndexOffset = (mesh.TriangleStart / 3);

                            int iTriangleCount = (mesh.NumRefVertices / 3);
                            newTriangleCount += iTriangleCount;

                            int iMeshVerticeCount = mesh.EndVertex - mesh.StartVertex + 1;

                            if (calculateNormals)
                                newVertexCount+=DuplicateVerticesAndCalcNormalsAndTangents(ref mesh, ref meshData, refData, meshDataNode, refDataNode, iMesh, iTriIndexOffset, iTriangleCount, iVerticeOffset, iMeshVerticeCount, maxCosAngleInRadians);
                            else
                                newVertexCount+=DuplicateVerticesAndCalcTangents(ref mesh, ref meshData, refData, meshDataNode, refDataNode, iMesh, iTriIndexOffset, iTriangleCount, iVerticeOffset, iMeshVerticeCount);        
                        }

                        refData.NumVert = (UInt16) newVertexCount;
                        refData.NumIndex = (UInt16) (newTriangleCount * 3);

                        string oldName2 = refDataNode.Name;
                        object oldData2 = refDataNode.Tag;

                        // save the VRefData back to the UTF
                        refDataNode.Tag = refData.GetBytes();

                        // communicate change
                        this.NodeChanged(refDataNode, oldName2, oldData2);

                        meshData.NumVertices = (ushort)meshData.Vertices.Count; 
                    }

                }
                string oldName = meshDataNode.Name;
                object oldData = meshDataNode.Tag;

                // save the VMeshData back to the UTF
                meshDataNode.Tag = meshData.GetRawData();

                // communicate change
                this.NodeChanged(meshDataNode, oldName, oldData);
            }

            //Correct VWireData
            foreach (TreeNode meshdata_node in this.treeView1.Nodes.Find("VMeshData", true))
            {
                VMeshData meshdata = new VMeshData(meshdata_node.Tag as byte[]);
                foreach (WireDataPoints currentWireDataPoints in storedWireDataPoints)
                {
                    if (meshdata_node == currentWireDataPoints.meshdata_node)
                    {
                        //Search new vertices for stored points and get new index. 
                        VWireData wiredata = new VWireData(currentWireDataPoints.wiredata_node.Tag as byte[]);
                        List <(int,int)> lineIndicesList = new List<(int, int)>(wiredata.Lines.Count);

                        int minIndex = Int32.MaxValue;
                        int maxIndex = Int32.MinValue;
                        for (int i =0; i<wiredata.Lines.Count;i++)
                        {
                            int index1= -1, index2= -1;                           

                            WireDataPoints.LinePoints currentLinePoints = currentWireDataPoints.points[i];
                            for (int k = 0; k < meshdata.Vertices.Count; k++) 
                            {
                                TVertex currentVertex = meshdata.Vertices[k];
                                if (index1 == -1 && currentVertex.X == currentLinePoints.X1 && currentVertex.Y == currentLinePoints.Y1 && currentVertex.Z == currentLinePoints.Z1)
                                {
                                    index1 = k;
                                    minIndex = Math.Min(minIndex, index1);
                                    maxIndex = Math.Max(maxIndex, index1);
                                }
                                if (index2 == -1 && currentVertex.X == currentLinePoints.X2 && currentVertex.Y == currentLinePoints.Y2 && currentVertex.Z == currentLinePoints.Z2)
                                {
                                    index2 = k;
                                    minIndex = Math.Min(minIndex, index2);
                                    maxIndex = Math.Max(maxIndex, index2);
                                }
                                if (index1 != -1 && index2 != -1)
                                    break;
                               
                            }
                            if (index1 == -1 || index2 == -1)
                                throw new Exception("Error: Could not find VWireData point for correction");

                            lineIndicesList.Add((index1, index2));
                        }
                       
                        int newVertexOffset = minIndex;

                        //Correct remaining values
                        wiredata.VertexOffset = (UInt16)newVertexOffset;
                        wiredata.MaxVertNoPlusOne = (UInt16)(maxIndex - minIndex + 1);

                        //We can use the range as the number of indices, since the new base offset is minIndex (which is not guaranteed for original vanilla offsets)
                        bool[] usedVerticesIndices = new bool[wiredata.MaxVertNoPlusOne];                       

                        //Apply new vertex base and store new indices
                        for (int i = 0; i < wiredata.Lines.Count; i++)
                        {
                            (int index1,int index2)=lineIndicesList[i];
                            VWireData.Line newLinePoints = wiredata.Lines[i];
                            newLinePoints.Point1 = (UInt16)(index1 - newVertexOffset);
                            newLinePoints.Point2 = (UInt16)(index2 - newVertexOffset);
                            wiredata.Lines[i] = newLinePoints;
                            usedVerticesIndices[newLinePoints.Point1] = true;
                            usedVerticesIndices[newLinePoints.Point2] = true;
                        }

                        //Count number of used vertices
                        int count = 0;                        
                        foreach (bool used in usedVerticesIndices)
                        {
                            if (used)
                                count++;
                        }

                        wiredata.NoVertices = (UInt16)count;

                        string oldName = currentWireDataPoints.wiredata_node.Name;
                        object oldData = currentWireDataPoints.wiredata_node.Tag;

                        // save the VWireData back to the UTF
                        currentWireDataPoints.wiredata_node.Tag = wiredata.GetBytes();

                        // communicate change
                        this.NodeChanged(currentWireDataPoints.wiredata_node, oldName, oldData);
                    }
                }
            }

            if (!quiet)
                MessageBox.Show(this, "Tangent/binormal data successfully added!", "Success!");
   
        }

        /// <summary>
        /// Verifies that the model data is valid (including wireframe data).
        /// 
        /// Author: Schmackbolzen
        /// </summary>
        /// <returns>A list with error strings or an empty list if there are no errors.</returns>
        public List<string> VerifyModelData()
        {
            List<string> errorList = new List<string>();
            foreach (TreeNode meshDataNode in this.treeView1.Nodes.Find("VMeshData", true))
            {
                VMeshData meshdata = new VMeshData(meshDataNode.Tag as byte[]);
                string meshdataName = meshDataNode.Parent.Name; // Name of the VMeshData

                // find vmeshdata
                foreach (TreeNode refDataNode in this.treeView1.Nodes.Find("VMeshRef", true))
                {
                    VMeshRef refdata = new VMeshRef(refDataNode.Tag as byte[]);

                    //Search mesh part name of this refdata

                    //Model part this VMeshRef belongs to initialised with error string (if this is shown something went wrong and has to be fixed)
                    string modelPartName = "Error finding VMeshPart name";

                    //First check whether parent node is correct
                    TreeNode vmeshPartNode = refDataNode.Parent;
                    if (vmeshPartNode == null)
                    {
                        errorList.Add("VMeshRef node is in wrong tree location (root level). Skipping.");
                        continue;
                    }

                    if (vmeshPartNode.Name.ToLower()!= "vmeshpart")
                    {
                        errorList.Add("VMeshRef node is in wrong tree location. Skipping.");
                        continue;
                    }

                    if (vmeshPartNode.Parent==null)
                    {
                        errorList.Add("VMeshPart node is in wrong tree location (root level). Skipping.");
                        continue;
                    }


                    //Now check whether there is a levelx node
                    if (vmeshPartNode.Parent.Name.StartsWith("level",StringComparison.OrdinalIgnoreCase))
                    {
                        TreeNode multiLevelNode = vmeshPartNode.Parent.Parent;
                        if (multiLevelNode != null && multiLevelNode.Name.ToLower() == "multilevel")
                        {
                            TreeNode nameNode = vmeshPartNode.Parent.Parent.Parent;
                            if (nameNode != null)
                            {
                                //Add level to name
                                modelPartName = nameNode.Name + ", " + vmeshPartNode.Parent.Name;
                            }
                            else
                            {
                                errorList.Add("MultiLevel node is in wrong tree location. Skipping contained VMeshRef.");
                                continue;
                            }
                        }
                        else
                        {
                            errorList.Add(vmeshPartNode.Parent.Name + " node is in wrong tree location. Skipping contained VMeshRef.");
                            continue;
                        }
                    }
                    else
                    {
                        //There is no level node so either this is a single part or has a name
                        if (vmeshPartNode.Parent.Parent==null)
                        {
                            //single part
                            modelPartName = "Single VMeshPart";
                        }
                        else
                        {
                            if (vmeshPartNode.Parent.Parent.Parent == null)
                            {
                                //Right structure, name found
                                modelPartName = vmeshPartNode.Parent.Name;
                            }
                            else
                            {
                                //Wrong structure, should be null
                                errorList.Add("VMeshPart node is in wrong tree location. Skipping contained VMeshRef.");
                                continue;
                            }
                        }
                    }
                    

                    if (Utilities.FLModelCRC(meshDataNode.Parent.Name) == refdata.VMeshLibId)
                    {
                        //verify current refdata with matching meshdata

                        //Check whether mesh references are valid
                        if (refdata.StartMesh + refdata.NumMeshes > meshdata.Meshes.Count)
                        {
                            errorList.Add(modelPartName + ": References invalid meshes.");
                            continue;
                        }

                        //Not a criticial error, so do not skip remaining checks
                        if (meshdata.Meshes[refdata.StartMesh].TriangleStart != refdata.StartIndex)
                        {
                            errorList.Add(modelPartName + ": Start index is not indendical to first mesh start index.");                         
                        }

                        int numTriangles = 0;

                        bool[] usedVerticesIndices = new bool[meshdata.Vertices.Count];
                        bool errorFound = false;
                        // iterate meshes
                        for (int iMesh = refdata.StartMesh; iMesh < (refdata.StartMesh + refdata.NumMeshes); iMesh++)
                        {
                            VMeshData.TMeshHeader mesh = meshdata.Meshes[iMesh];

                            int iVerticeOffset = refdata.StartVert + mesh.StartVertex;
                            int iTriIndexOffset = (mesh.TriangleStart / 3); // refdata.StartIndex is unreliable???!!

                            int iTriangleCount = (mesh.NumRefVertices / 3);

                            int iMeshVerticeCount = mesh.EndVertex - mesh.StartVertex + 1;
                            int iEndVertex = iVerticeOffset + (mesh.EndVertex - mesh.StartVertex);
                            
                            numTriangles += iTriangleCount; 

                            if (iMeshVerticeCount + mesh.StartVertex > meshdata.Vertices.Count)
                            {
                                errorList.Add(meshdataName + ": Mesh vertex range is invalid. Mesh index: " + iMesh.ToString() + ". Affected model part: " + modelPartName);
                                errorFound = true;
                                break;
                            }

                            if (iTriangleCount + iTriIndexOffset > meshdata.Triangles.Count)
                            {
                                errorList.Add(meshdataName+ ": Referenced triangle count is larger than maximum triangles in meshdata. Mesh index: " + iMesh.ToString() + ". Affected model part: " + modelPartName);
                                errorFound = true;
                                break;
                            }

                            if (iEndVertex <= 0 || iEndVertex >= meshdata.Vertices.Count)
                            {
                                errorList.Add(modelPartName+": Vertex range is invalid. Mesh index: " + iMesh.ToString());
                                errorFound = true;
                                break;
                            }

                            //Verify triangles and mark used vertices
                            for (int iTriIndex = iTriIndexOffset; iTriIndex < (iTriIndexOffset + iTriangleCount); iTriIndex++)
                            {
                                VMeshData.TTriangle tri = meshdata.Triangles[iTriIndex];

                                int vertexIndex1 = iVerticeOffset + tri.Vertex1;
                                int vertexIndex2 = iVerticeOffset + tri.Vertex2;
                                int vertexIndex3 = iVerticeOffset + tri.Vertex3;

                                if (vertexIndex1 >= meshdata.Vertices.Count ||
                                    vertexIndex2 >= meshdata.Vertices.Count ||
                                    vertexIndex3 >= meshdata.Vertices.Count)
                                {
                                    errorList.Add(meshdataName + ": Triangle references wrong vertex index. Mesh index: " + iMesh.ToString()+ ". Affected model part: " + modelPartName);
                                    errorFound = true;
                                    break;
                                }

                                usedVerticesIndices[vertexIndex1] = true;
                                usedVerticesIndices[vertexIndex2] = true;
                                usedVerticesIndices[vertexIndex3] = true;
                            }
                           
                        }

                        //Skip to next VMeshRef as the tests below don't work when an error was found
                        if (errorFound)
                            continue;

                        if (numTriangles * 3 != refdata.NumIndex)
                            errorList.Add(modelPartName + ": Wrong indices count");

                        int count = 0;
                        //Count number of used vertices
                        foreach (bool used in usedVerticesIndices)
                        {
                            if (used)
                                count++;
                        }

                        if (count != refdata.NumVert)
                            errorList.Add(modelPartName + ": Wrong number of used vertices");
                    }
                }
                foreach (TreeNode wireDataNode in this.treeView1.Nodes.Find("VWireData", true))
                {

                    //Search mesh part name of this wiredata

                    //Model part this VMeshWire belongs to initialised with error string (if this is shown something went wrong and has to be fixed)
                    string modelPartName = "Error finding VMeshWire name";

                    //First check whether parent node is correct
                    TreeNode vmeshWireNode = wireDataNode.Parent;
                    if (vmeshWireNode == null)
                    {
                        errorList.Add("VWireData node is in wrong tree location (root level). Skipping.");
                        continue;
                    }

                    if (vmeshWireNode.Name.ToLower() != "vmeshwire")
                    {
                        errorList.Add("VWireData node is in wrong tree location. Skipping.");
                        continue;
                    }

                    if (vmeshWireNode.Parent == null)
                    {
                        errorList.Add("VMeshWire node is in wrong tree location (root level). Skipping.");
                        continue;
                    }

                    //VMeshWire is never in a level node so either this is a single part or has a name
                    if (vmeshWireNode.Parent.Parent == null)
                    {
                        //single part
                        modelPartName = "Single VMeshWire";
                    }
                    else
                    {
                        if (vmeshWireNode.Parent.Parent.Parent == null)
                        {
                            //Right structure, name found
                            modelPartName = vmeshWireNode.Parent.Name;
                        }
                        else
                        {
                            //Wrong structure, should be null
                            errorList.Add("VMeshWire node is in wrong tree location. Skipping contained VMeshRef.");
                            continue;
                        }
                    }                   

                    VWireData wiredata = new VWireData(wireDataNode.Tag as byte[]);
                    bool errorFound = false;
                    if (Utilities.FLModelCRC(meshDataNode.Parent.Name) == wiredata.VMeshLibId)
                    {                       
                        int maxIndex = Int32.MinValue;
                        int minIndex = Int32.MaxValue;
                        int maxVerticeIndex = meshdata.Vertices.Count - 1;

                        bool[] usedVerticesIndices = new bool[meshdata.Vertices.Count-wiredata.VertexOffset];

                        foreach (VWireData.Line currentLine in wiredata.Lines)
                        {
                            if (currentLine.Point1 + wiredata.VertexOffset > maxVerticeIndex || currentLine.Point2 + wiredata.VertexOffset > maxVerticeIndex)
                            {
                                errorList.Add(modelPartName + ": VWireData vertex index is invalid (too large). Skipping follow up checks.");
                                errorFound = true;
                                break;
                            }

                            usedVerticesIndices[currentLine.Point1] = true;
                            usedVerticesIndices[currentLine.Point2] = true;

                            maxIndex = Math.Max(maxIndex, Math.Max(currentLine.Point1, currentLine.Point2));
                            minIndex = Math.Min(minIndex, Math.Min(currentLine.Point1, currentLine.Point2));
                        }

                        //Skip to next VWireData as the tests below don't work when an error was found
                        if (errorFound)
                            continue;

                        if (maxIndex-minIndex +1!= wiredata.MaxVertNoPlusOne)
                            errorList.Add(modelPartName + ": VWireData vertex range entry has wrong value");

                        int count = 0;
                        //Count number of used vertices
                        foreach (bool used in usedVerticesIndices)
                        {
                            if (used)
                                count++;
                        }

                        if (count != wiredata.NoVertices)
                            errorList.Add(modelPartName + ": VWireData Number of used vertices entry is wrong");
                    }
                }
            }
            return errorList;
        }
                        

        public void EditVMeshRef()
        {
            try
            {
                new EditVMeshRef(this, treeView1.SelectedNode).Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error '" + ex.Message + "'", "Error");
            }
        }

        public void EditHardpoint()
        {
            try
            {
                new EditHardpointData(this, FindHardpoint(treeView1.SelectedNode)).Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error '" + ex.Message + "'", "Error");
            }
        }

        /// <summary>
        /// Find the hardpoint associated with a node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public TreeNode FindHardpoint(TreeNode node)
        {
            try
            {
                if (treeView1.Nodes.Count == 1)
                    return null;
                // In the list, find the actual node.
                if (node.Parent == treeView1.Nodes[1])
                    return treeView1.Nodes[0].Nodes.Find(node.Name, true)[0];
                // On the hardpoint.
                if (Utilities.StrIEq(node.Parent?.Parent?.Name, "Hardpoints"))
                    return node;
                // In the hardpoint.
                if (Utilities.StrIEq(node.Parent?.Parent?.Parent?.Name, "Hardpoints"))
                    return node.Parent;
            }
            catch { }
            return null;
        }
        
        public TreeNode GetSelectedNode()
        {
			return treeView1.SelectedNode;
        }


        /// <summary>
        /// Enable display items in the popup menu depending on the node that
        /// is selected.
        /// </summary>
        /// <param name="node"></param>
        private void UpdateContextMenu(TreeNode node)
        {
            bool is_node = (node != null);
            bool has_data = ContainsData(node);

            // Can only Add nodes to branch nodes.
            toolStripMenuItemAddNode.Enabled = !has_data;

            // Can Rename and Delete any node.
            toolStripMenuItemRenameNode.Enabled =
            toolStripMenuItemDeleteNode.Enabled = is_node;

            // Can Import and Edit As any leaf node.
            toolStripMenuItemImportData.Enabled = 
            stringToolStripMenuItem.Enabled     =
            intArrayToolStripMenuItem.Enabled   =
            floatArrayToolStripMenuItem.Enabled = (is_node && node.Nodes.Count == 0);

            // Can Export any node containing data.
            toolStripMenuItemExportData.Enabled = has_data;

            toolStripMenuItemEdit.Enabled = (IsEditable(node) != Editable.No);

            Viewable view = IsViewable(node);
            toolStripMenuItemView.Enabled = (view != Viewable.No);
            toolStripMenuItemView.Text = (view == Viewable.Wave) ? "Play" : "View";
        }

        bool removeHighlight = false;

        /// <summary>
        /// When a node in the tree is selected update the info box on the parent window.
        /// </summary>
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // Notify the parent to update the node summary area.
            parent.SetSelectedNode(e.Node);
            UpdateContextMenu(e.Node);

            // Redraw the model with the new highlight.
            if (e.Node.FullPath.StartsWith("Parts") || 
                e.Node.FullPath.Contains("Hardpoints"))
            {
                RedrawModel();
                removeHighlight = true;
            }
            else if (removeHighlight)
            {
                RedrawModel();
                removeHighlight = false;
            }
        }

        public void RedrawModel()
        {
            foreach (UTFFormObserver ob in observers)
            {
                (ob as ModelViewForm).Invalidate();
            }
        }

        public bool Cut()
        {
            if (treeView1.SelectedNode != null && !treeView1.SelectedNode.IsEditing)
            {
                treeView1.Cut();

                return true;
            }

            return false;
        }

        public bool Copy()
        {
            if (treeView1.SelectedNode != null && !treeView1.SelectedNode.IsEditing)
            {
                treeView1.Copy();

                return true;
            }

            return false;
        }

        public bool Paste()
        {
            if (!treeView1.SelectedNode.IsEditing)
            {
                treeView1.Paste();

                return true;
            }

            return false;
        }

        public bool PasteChild()
        {
            if (!treeView1.SelectedNode.IsEditing)
            {
                treeView1.PasteChild();

                return true;
            }

            return false;
        }

        public bool Delete()
        {
            if (treeView1.SelectedNode != null && !treeView1.SelectedNode.IsEditing)
            {
                treeView1.Delete();

                return true;
            }

            return false;
        }

        public void ShowModel()
        {
            try
            {
				ModelViewForm modelView = new ModelViewForm(this, treeView1, fileName);
				modelView.Show(this);
				modelView.HardpointMoved += new EventHandler(modelView_HardpointMoved);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error '" + ex.Message + "'", "Error");
            }
        }

		void modelView_HardpointMoved(object sender, EventArgs e)
		{
            if(treeView1.SelectedNode != null)
			    parent.SetSelectedNode(treeView1.SelectedNode);
			Modified();
		}

        private void treeView1_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Label == null)
                return;

            string oldName = e.Node.Name;
            object oldData = e.Node.Tag;
            e.Node.Name = e.Label;
            NodeChanged(e.Node, oldName, oldData);
        }

        private void treeView1_ModifiedNode(object sender, TreeViewEventArgs e)
        {
            NodeChanged(e.Node, null, null);
        }

        /// <summary>
        /// List of data observers.
        /// </summary>
        List<UTFFormObserver> observers = new List<UTFFormObserver>();

        /// <summary>
        /// Register for notifications when node data changes.
        /// </summary>
        /// <param name="ob"></param>
        public void AddObserver(UTFFormObserver ob)
        {
            observers.Add(ob);
        }

        /// <summary>
        /// Remove observing object for node data changes.
        /// </summary>
        /// <param name="ob"></param>
        public void DelObserver(UTFFormObserver ob)
        {
            observers.Remove(ob);
        }

        /// <summary>
        /// Call this function to notify observers when node data changes.
        /// </summary>
        /// <param name="node"></param>
        public void NodeChanged(TreeNode node, string oldName, object oldData)
        {
            if (node?.Parent == null) return;
            bool isHardpoint = Utilities.StrIEq(node?.Parent.Text, "Fixed", "Revolute", "Hardpoints") || Utilities.StrIEq(node.Text, "Fixed", "Revolute", "Hardpoints");

            foreach (UTFFormObserver ob in observers)
                ob.DataChanged(isHardpoint ? DataChangedType.Hardpoints : DataChangedType.Mesh);

            if (node.Parent != null)
            {
                if (Utilities.StrIEq(node.Parent.Name, "VMeshLibrary"))
                {
                    uint oldCRC = Utilities.FLModelCRC(oldName);
                    uint newCRC = Utilities.FLModelCRC(node.Name);

                    TreeNode[] refNodes = FindVMeshRefs(oldCRC);
                    if (refNodes.Length > 0)
                    {
                        if (MessageBox.Show("Automatically update matching VMeshRefLibIDs?", "Question", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            UpdateVMeshRefs(refNodes, newCRC);
                        }
                    }
                }

                // If this is a hardpoint, rename the other one, too.
                if (Utilities.StrIEq(node.Parent.Text, "Fixed", "Revolute", "Hardpoints"))
                {
                    TreeNode[] onode = treeView1.Nodes.Find(oldName, true);
                    if (onode.Length > 0)
                        onode[0].Text = onode[0].Name = node.Name;
                }
            }
            Modified(node);
        }

        /// <summary>
        /// Find all vmeshref nodes with the matching flmodelcrc.
        /// </summary>
        /// <param name="crc"></param>
        /// <returns></returns>
        public TreeNode[] FindVMeshRefs(uint vMeshLibId)
        {
            List<TreeNode> nodes = new List<TreeNode>();

            try
            {
                TreeNode rootNode = treeView1.Nodes[0];
                foreach (TreeNode node in rootNode.Nodes.Find("VMeshRef", true))
                {
                    try
                    {
                        VMeshRef data = new VMeshRef(node.Tag as byte[]);
                        if (data.VMeshLibId == vMeshLibId)
                            nodes.Add(node);
                    }
                    catch { }
                }
            }
            catch { }
            return nodes.ToArray();
        }

        /// <summary>
        /// Update all VMeshRef nodes with newVMeshLibId.
        /// </summary>
        /// <param name="crc"></param>
        /// <returns></returns>
        public void UpdateVMeshRefs(TreeNode[] nodes, uint newVMeshLibId)
        {
            foreach (TreeNode node in nodes)
            {
                VMeshRef data = new VMeshRef(node.Tag as byte[]);
                data.VMeshLibId = newVMeshLibId;
                node.Tag = data.GetBytes();
            }

            MessageBox.Show(String.Format("{0} {1} updated.", nodes.Length, (nodes.Length == 1) ? "node" : "nodes"));
        }

        /// <summary>
        /// Search the VMeshData names for a matching crc.
        /// </summary>
        /// <param name="crc"></param>
        /// <returns></returns>
        public string FindVMeshName(uint crc, bool code)
        {
            string name = null;
            try
            {
                TreeNode vmesh = treeView1.Nodes[0].Nodes["VMeshLibrary"];
                foreach (TreeNode node in vmesh.Nodes)
                {
                    if (Utilities.FLModelCRC(node.Name) == crc)
                    {
                        name = node.Name;
                        break;
                    }
                }
            }
            catch { }
            if (name == null)
            {
                if (crc == 0xE296602F)
                    name = "interface.generic-2.vms";
                else if (crc == 0x1351B6D4)
                    name = "interface.generic-102.vms";
            }
            if (name == null)
                return String.Format("0x{0:X8}", crc);
            if (code)
                return String.Format("{0} (0x{1:X8})", name, crc);
            return name;
        }


        /// <summary>
        /// Indicate the tree has been modified.
        /// </summary>
        public void Modified()
        {
            if (!fileChangesNotSaved)
            {
                this.Text = this.Text.Insert(0, "*");
                fileChangesNotSaved = true;
            }

			foreach (UTFFormObserver ob in observers)
			{
				ob.Invalidate();
			}
        }

        public void Modified(TreeNode node)
        {
            Modified();
            if (this == parent.ActiveMdiChild &&
                (node == treeView1.SelectedNode || node == treeView1.SelectedNode.Parent))
                parent.SetSelectedNode(treeView1.SelectedNode);
        }
        
        public void SetSelectedNode(TreeNode node)
        {
			treeView1.SelectedNode = node;
        }

        private void UTFForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (fileChangesNotSaved)
            {
				DialogResult r = MessageBox.Show("Changes not saved. Save now?", "Save Changes for '" + Path.GetFileName(fileName) + "'", MessageBoxButtons.YesNoCancel);
                if (r == DialogResult.Yes)
                {
                    SaveUTFFile(fileName);
                }
                else if (r == DialogResult.Cancel)
                {
					e.Cancel = true;
					return;
				}
            }

            UTFEditorMain.LoadedFilesThreeViews.Remove(fileName);
            List<UTFFormObserver> observerscopy = new List<UTFFormObserver>(observers);
            foreach (UTFFormObserver ob in observerscopy)
			{
				ob.Close();
			}
        }

        private void UTFForm_Activated(object sender, EventArgs e)
        {
            parent.SetSelectedNode(treeView1.SelectedNode);
        }

        // Can't find a word that combines expand/collapse, so I'll make one up,
        // based on inflate/deflate.
        private void treeView1_BeforeFlate(object sender, TreeViewCancelEventArgs e)
        {
            if (doubleClicked)
            {
                if (e.Node == treeView1.SelectedNode &&
                    (IsEditable(e.Node) != Editable.No ||
                     IsViewable(e.Node) != Viewable.No))
                {
                    e.Cancel = true;
                }
                doubleClicked = false;
            }
        }

        private void treeView1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                e.IsInputKey = true;
                parent.SelectGrid();
            }
        }

        internal void ImportTextures(string[] textures)
        {
            bool appendTga = false, overwrite = false;
            switch (MessageBox.Show("Do you want to include a .tga extension?", "Naming Scheme", MessageBoxButtons.YesNoCancel))
            {
                case DialogResult.Yes:
                    appendTga = true;
                    break;
                case DialogResult.No:
                    appendTga = false;
                    break;
                case DialogResult.Cancel:
                    return;
            }

            TreeNode rootNode = treeView1.Nodes[0];
            TreeNode[] tlibs = rootNode.Nodes.Find("Texture library", false);

            TreeNode textureLibrary;

            if (tlibs.Length > 0)
            {
                textureLibrary = tlibs[0];

                foreach (string t in textures)
                {
                    string n = Path.GetFileNameWithoutExtension(t);
                    if (appendTga) n += ".tga";

                    TreeNode[] tx = textureLibrary.Nodes.Find(n, false);
                    if (tx.Length > 0)
                    {
                        if (!overwrite)
                        {
                            if (MessageBox.Show("WARNING: Some textures already exist. Overwrite?", "Naming Scheme", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                overwrite = true;
                            else
                                return;
                        }

                        tx[0].Remove();
                        NodeChanged(tx[0], "", null);
                    }
                }
            }
            else
                textureLibrary = rootNode.Nodes.Add("Texture library");

            foreach (string t in textures)
            {
                string n = Path.GetFileNameWithoutExtension(t);
                if (appendTga) n += ".tga";

                string ext = Path.GetExtension(t).ToLower();

                Byte[] contents = File.ReadAllBytes(t);

                TreeNode node = textureLibrary.Nodes.Add(n);
                node.Name = n;
                node.Tag = new byte[0];
                TreeNode texnode = node.Nodes.Add(ext == ".dds" ? "MIPS" : "MIP0");
                texnode.Name = texnode.Text;
                texnode.Tag = contents;
            }
            NodeChanged(textureLibrary, "", null);
        }

        internal void ImportTexturesAllDds(List<ExportTexture> exportTextures, string pathTextures)
        {
            List<string> textures = new List<string>();
            foreach (var tex in exportTextures)
            {
                var name = GetDdsFileName(tex.Name);
                var pathTex = Path.Combine(pathTextures, name);
                if (!textures.Contains(pathTex))
                    textures.Add(pathTex);
            }

            TreeNode rootNode = treeView1.Nodes[0];
            TreeNode[] tlibs = rootNode.Nodes.Find("Texture library", false);

            TreeNode textureLibrary = tlibs.Length > 0
                ? tlibs[0]
                : rootNode.Nodes.Add("Texture library");

            foreach (string t in textures)
            {
                string normalizedName = NormalizeNodeName(t);
                string ext = Path.GetExtension(t).ToLower();
                byte[] contents = File.ReadAllBytes(t);

                // Найти ноду по нормализованному имени
                TreeNode existingNode = textureLibrary.Nodes
                    .Cast<TreeNode>()
                    .FirstOrDefault(n => NormalizeNodeName(n.Name) == normalizedName);

                if (existingNode != null)
                {
                    // Заменить содержимое
                    existingNode.Nodes.Clear(); // Удалим старые мипы
                    TreeNode mipNode = existingNode.Nodes.Add(ext == ".dds" ? "MIPS" : "MIP0");
                    mipNode.Name = mipNode.Text;
                    mipNode.Tag = contents;
                }
                else
                {
                    // Создать новую ноду, если нет
                    string originalName = Path.GetFileNameWithoutExtension(t);
                    if (ext == ".tga")
                        originalName += ".tga";

                    TreeNode newNode = textureLibrary.Nodes.Add(originalName);
                    newNode.Name = originalName;
                    newNode.Tag = new byte[0];

                    TreeNode mipNode = newNode.Nodes.Add(ext == ".dds" ? "MIPS" : "MIP0");
                    mipNode.Name = mipNode.Text;
                    mipNode.Tag = contents;
                }
            }

            NodeChanged(textureLibrary, "", null);

            //bool resetTgaLibrary = false, overwrite = false;

            //List<string> textures = new List<string>();
            //foreach (var tex in exportTextures)
            //{
            //    // перманентное dds
            //    var name = GetDdsFileName(tex.Name);
            //    var pathTex = Path.Combine(pathTextures, name);
            //    if(!textures.Contains(pathTex))
            //        textures.Add(pathTex);
            //}

            //TreeNode rootNode = treeView1.Nodes[0];
            //TreeNode[] tlibs = rootNode.Nodes.Find("Texture library", false);

            //TreeNode textureLibrary;

            //if (tlibs.Length > 0)
            //{
            //    textureLibrary = tlibs[0];

            //    foreach (string t in textures)
            //    {
            //        string normalizedName = NormalizeNodeName(t);

            //        // Удалим старую, если есть
            //        TreeNode[] existingNodes = textureLibrary.Nodes
            //            .Cast<TreeNode>()
            //            .Where(n => NormalizeNodeName(n.Name) == normalizedName)
            //            .ToArray();

            //        foreach (var node in existingNodes)
            //        {
            //            node.Remove();
            //            NodeChanged(node, "", null);
            //        }

            //        //string n = Path.GetFileNameWithoutExtension(t);

            //        //if (resetTgaLibrary) n += ".tga";

            //        //TreeNode[] tx = textureLibrary.Nodes.Find(n, false);
            //        //if (tx.Length > 0)
            //        //{
            //        //    overwrite = true;
            //        //    tx[0].Remove();
            //        //    NodeChanged(tx[0], "", null);
            //        //}
            //    }
            //}
            //else // все текстуры добавит заново и создаст Texture library
            //    textureLibrary = rootNode.Nodes.Add("Texture library");

            //foreach (string t in textures)
            //{
            //    //var extCheck = Path.GetExtension(t).ToLower();
            //    //if (extCheck == ".tga")
            //    //    resetTgaLibrary = true;
            //    //else resetTgaLibrary = false;

            //    //string n = Path.GetFileNameWithoutExtension(t);
            //    //if (resetTgaLibrary) n += ".tga";

            //    //string ext = Path.GetExtension(t).ToLower();

            //    //Byte[] contents = File.ReadAllBytes(t);

            //    //TreeNode node = textureLibrary.Nodes.Add(n);
            //    //node.Name = n;
            //    //node.Tag = new byte[0];
            //    //TreeNode texnode = node.Nodes.Add(ext == ".dds" ? "MIPS" : "MIP0");
            //    //texnode.Name = texnode.Text;
            //    //texnode.Tag = contents;

            //    string normalizedName = NormalizeNodeName(t);

            //    // Добавим новую
            //    string ext = Path.GetExtension(t).ToLower();
            //    Byte[] contents = File.ReadAllBytes(t);

            //    TreeNode newNode = textureLibrary.Nodes.Add(normalizedName);
            //    newNode.Name = normalizedName;
            //    newNode.Tag = new byte[0];

            //    TreeNode mipNode = newNode.Nodes.Add(ext == ".dds" ? "MIPS" : "MIP0");
            //    mipNode.Name = mipNode.Text;
            //    mipNode.Tag = contents;
            //}
            //NodeChanged(textureLibrary, "", null);
        }

        internal void ImportTexturesDdsAndTga(List<ExportTexture> exportTextures, string pathTextures)
        {
            var textures = new List<ItemPointTgaDds>();

            foreach (var tex in exportTextures)
            {
                var mipName = ExtractMipLevelName(tex.Name); // Получить MIP0, MIP1 и т.п.
                var pathTex = Path.Combine(pathTextures, tex.Name); // Имя базово dds лежат в корне без своей папки
                var cleanName = ExtractClearName(tex.Name);
                // tga лежат в своей папочке поэтому получаем точный адрес с учётом папочки
                if (Path.GetExtension(tex.Name) == ".tga")
                {
                    var nameFolderTga = ExtractFolderTgaName(tex.Name);
                    pathTex = Path.Combine(pathTextures, nameFolderTga, tex.Name);
                }

                if (!textures.Any(t => t.FileName == pathTex))
                {
                    textures.Add(new ItemPointTgaDds
                    {
                        ClearName = cleanName,
                        FileName = pathTex,
                        MipName = mipName
                    });
                }
            }

            TreeNode rootNode = treeView1.Nodes[0];
            TreeNode[] tlibs = rootNode.Nodes.Find("Texture library", false);

            TreeNode textureLibrary = tlibs.Length > 0
                ? tlibs[0]
                : rootNode.Nodes.Add("Texture library");

            // Группируем текстуры по базовому имени (без MIP-префикса и расширения)
            var groupedTextures = textures
                .GroupBy(t => NormalizeNodeName(t.ClearName))
                .ToDictionary(
                    g => g.Key, // ключ словаря — нормализованное имя
                    g => g
                        .OrderBy(t => GetMipLevel(t.MipName)) // сортируем по номеру MIP (MIP0 < MIP1 < ...)
                        .ToList()
                );

            //foreach (var group in groupedTextures)
            //{
            //    foreach (var mip in group.Value)
            //    {
            //        var first = mip;
            //        var normalizedName = NormalizeNodeName(first.FileName);

            //        byte[] dummy = File.ReadAllBytes(first.FileName); // Для проверки существования
            //        string baseName = Path.GetFileNameWithoutExtension(mipPrefixRegex.Replace(Path.GetFileName(first.FileName), ""));

            //        TreeNode existingNode = textureLibrary.Nodes
            //            .Cast<TreeNode>()
            //            .FirstOrDefault(n => NormalizeNodeName(n.Name) == baseName.ToLower());
            //        //if(existingNode.Name == "CoalitionStar1")
            //        //{
            //        //    int aaa = 0;
            //        //}    
            //        if (existingNode == null)
            //        {
            //            existingNode = textureLibrary.Nodes.Add(baseName);
            //            existingNode.Name = baseName;
            //        }

            //        existingNode.Nodes.Clear(); // Заменим все MIP’ы заново

            //        foreach (var item in group.Value.OrderBy(i => i.MipName))
            //        {
            //            if (!File.Exists(item.FileName)) continue;

            //            byte[] contents = File.ReadAllBytes(item.FileName);
            //            TreeNode mipNode = existingNode.Nodes.Add(item.MipName);
            //            mipNode.Name = item.MipName;
            //            mipNode.Tag = contents;
            //        }
            //        break;
            //    }
            //}

            foreach (var group in groupedTextures)
            {
                foreach (var mip in group.Value)
                {
                    var first = mip;
                    string baseName = Path.GetFileNameWithoutExtension(
                        mipPrefixRegex.Replace(Path.GetFileName(first.FileName), "")
                    );

                    // Ищем уже существующий узел по имени
                    TreeNode existingNode = textureLibrary.Nodes
                        .Cast<TreeNode>()
                        .FirstOrDefault(n => NormalizeNodeName(n.Name) == baseName.ToLower());

                    if (existingNode == null)
                    {
                        // Не создаём новых, просто пропускаем
                        continue;
                    }

                    // Обходим все mip'ы внутри группы
                    foreach (var item in group.Value)
                    {
                        if (!File.Exists(item.FileName)) continue;

                        // Пытаемся найти соответствующий mip внутри существующего узла
                        TreeNode existingMip = existingNode.Nodes
                            .Cast<TreeNode>()
                            .FirstOrDefault(n => n.Name == item.MipName);

                        if (existingMip != null)
                        {
                            // Обновляем содержимое только если узел существует
                            existingMip.Tag = File.ReadAllBytes(item.FileName);
                        }

                        // ❌ Не создаём ничего нового, если MIP не найден
                    }

                    break; // только первую текстуру из группы
                }
            }

            NodeChanged(textureLibrary, "", null);
        }

        private int GetMipLevel(string mipName)
        {
            // Извлекает цифру из MIP0, MIP1 и т.п.
            if (mipName != null && mipName.StartsWith("MIP", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(mipName.Substring(3), out int result))
                    return result;
            }
            return int.MaxValue; // если нет цифры — кидаем в конец
        }

        private string NormalizeNodeName(string filename)
        {
            var name = Path.GetFileName(filename).ToLower();

            // Убираем все лишние расширения типа .tga.dds
            while (Path.HasExtension(name))
            {
                name = Path.GetFileNameWithoutExtension(name);
            }

            return name;
        }

        private static readonly Regex mipPrefixRegex = new Regex(@"^MIP\d+_", RegexOptions.IgnoreCase);

        public string GetDdsFileName(string inputFileName)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFileName);
            string cleanedName = mipPrefixRegex.Replace(fileNameWithoutExtension, "");
            return cleanedName + ".dds";
        }

        private string ExtractMipLevelName(string filename)
        {
            Regex mipRegex = new Regex(@"^(MIP\d+)_", RegexOptions.IgnoreCase);
            var name = Path.GetFileNameWithoutExtension(filename);
            var match = mipRegex.Match(name);
            return match.Success ? match.Groups[1].Value.ToUpperInvariant() : "MIPS";
        }

        private string ExtractFolderTgaName(string filename)
        {
            Regex mipRegex = new Regex(@"^MIP\d+_", RegexOptions.IgnoreCase);
            return mipRegex.Replace(filename, "");
        }

        private string ExtractClearName(string filename)
        {
            Regex mipRegex = new Regex(@"^MIP\d+_", RegexOptions.IgnoreCase);
            var regexRes = mipRegex.Replace(filename, "");
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(regexRes);
            string cleanedName = mipPrefixRegex.Replace(fileNameWithoutExtension, "");
            return cleanedName;
        }

        internal void ReplaceAll(string find, string replace, bool whole, bool content, bool name)
        {
            ReplaceNodeAndChildren(treeView1.Nodes[0], find, replace, whole, content, name);

            Modified();
        }

        void ReplaceNodeAndChildren(TreeNode n, string find, string replace, bool whole, bool content, bool name)
        {
            if (name)
            {
                if (whole && n.Name == find)
                    n.Text = n.Name = replace;
                else if (!whole)
                    n.Text = n.Name = n.Name.Replace(find, replace);
            }

            if (content && IsEditable(n) == Editable.String)
            {
                byte[] data = n.Tag as byte[];
                string txt = Encoding.ASCII.GetString(data);

                if (whole && txt == find)
                    txt = replace;
                else if(!whole)
                    txt = txt.Replace(find, replace);

                n.Tag = Encoding.ASCII.GetBytes(txt + "\u0000");
            }

            foreach (TreeNode node in n.Nodes)
                ReplaceNodeAndChildren(node, find, replace, whole, content, name);
        }

        HashSet<string> rescaledHPs = new HashSet<string>();
        internal void RescaleModel(float scaling)
        {
            rescaledHPs.Clear();

            foreach (TreeNode n in treeView1.Nodes)
            {
                if (n.Name == "\\")
                {
                    TraverseRescaleModel(n, scaling);
                    Modified(n);
                }
            }

            rescaledHPs.Clear();

            foreach (UTFFormObserver ob in observers)
                ob.DataChanged(DataChangedType.All);
        }

        void TraverseRescaleModel(TreeNode p, float scaling)
        {
            foreach (TreeNode n in p.Nodes)
            {
                switch (IsEditable(n))
                {
                    case Editable.VMeshRef: RescaleVMeshRef(n, scaling); break;
                    case Editable.Fix: RescaleFixData(n, scaling); break;
                    case Editable.Rev: RescaleRevData(n, scaling); break;
                    case Editable.Hardpoint: RescaleHardpoint(n, scaling); break;
                }

                if (n.Name.ToLowerInvariant() == "vmeshdata")
                    RescaleVMeshData(n, scaling);

                TraverseRescaleModel(n, scaling);
            }
        }

        private void RescaleVMeshData(TreeNode n, float scaling)
        {
            byte[] data = n.Tag as byte[];

            VMeshData s = new VMeshData(data);

            s.Vertices = s.Vertices.Select(v =>
            {
                v.X *= scaling;
                v.Y *= scaling;
                v.Z *= scaling;

                return v;
            }).ToList();

            n.Tag = s.GetRawData();
        }

        private void RescaleHardpoint(TreeNode n, float scaling)
        {
            var hp = FindHardpoint(n);
            if (hp == null || rescaledHPs.Contains(hp.Name))
                return;

            HardpointData s = new HardpointData(hp);

            s.PosX *= scaling;
            s.PosY *= scaling;
            s.PosZ *= scaling;

            s.Write();

            rescaledHPs.Add(s.Name);
        }

        private void RescaleRevData(TreeNode n, float scaling)
        {
            byte[] data = n.Tag as byte[];

            CmpRevData s = new CmpRevData(data);

            foreach(var p in s.Parts)
            {
                p.OffsetX *= scaling;
                p.OffsetY *= scaling;
                p.OffsetZ *= scaling;

                p.OriginX *= scaling;
                p.OriginY *= scaling;
                p.OriginZ *= scaling;
            }

            n.Tag = s.GetBytes();
        }

        private void RescaleFixData(TreeNode n, float scaling)
        {
            byte[] data = n.Tag as byte[];

            CmpFixData s = new CmpFixData(data);

            foreach (var p in s.Parts)
            {
                p.OriginX *= scaling;
                p.OriginY *= scaling;
                p.OriginZ *= scaling;
            }

            n.Tag = s.GetBytes();
        }

        private void RescaleVMeshRef(TreeNode n, float scaling)
        {
            byte[] data = n.Tag as byte[];

            VMeshRef s = new VMeshRef(data);

            s.BoundingBoxMaxX *= scaling;
            s.BoundingBoxMaxY *= scaling;
            s.BoundingBoxMaxZ *= scaling;
            s.BoundingBoxMinX *= scaling;
            s.BoundingBoxMinY *= scaling;
            s.BoundingBoxMinZ *= scaling;

            s.CenterX *= scaling;
            s.CenterY *= scaling;
            s.CenterZ *= scaling;

            s.Radius *= scaling;

            n.Tag = s.GetBytes();
        }
    }
}
