using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using PB.Formulas;
using System.Drawing.Drawing2D;
using System.Xml;
using System.IO;

namespace PB.FEval
{
    public partial class MainForm : Form
    {
        private XmlDocument _doc;
        private bool _xmlLoaded = false;
        private Formula _formula;

        public MainForm()
        {
            InitializeComponent();
        }

        #region evaluation

        /// <summary>
        /// evaluation
        /// </summary>
        private void EvaluateFormula()
        {
            this._xmlLoaded = false;
            this._doc = null;
            try
            {
                // evaluate formula, if fails then throws exception
                this._formula = new Formula(this.formulaTextBox.Text);
                // icon ok
                this.formulaLabel.Image = Properties.Resources.Success;
                // syntax highlight
                this.infixTextBox.Text = string.Empty;
                for (int i = 0; i < this._formula.Infix.Length; i++)
                {
                    // color by type
                    switch (this._formula.Infix[i].Type)
                    {
                        case NodeType.Operator:
                            this.infixTextBox.SelectionColor = Color.Green;
                            break;
                        case NodeType.Operand:
                            this.infixTextBox.SelectionColor = Color.Black;
                            break;
                        case NodeType.OpeningBracket:
                        case NodeType.ClosingBracket:
                            this.infixTextBox.SelectionColor = Color.Blue;
                            break;
                    }
                    // add to richtextbox
                    this.infixTextBox.AppendText(this._formula.Infix[i].Value.ToString());
                }
                // result
                this.resultTextBox.Text = this._formula.Result.ToString();
                // evaluation tree
                this.treeView1.Nodes.Clear();
                this.DrawTree(null, this._formula.EvaluationTreeRoot);
                this.treeView1.ExpandAll();
                this.DrawTreeHelp();
            }
            catch (FormulaException ex)
            {
                // icon error
                this.formulaLabel.Image = Properties.Resources.Error;
                if (ex.ErrorStart > -1)
                {
                    this.formulaTextBox.SelectionStart = ex.ErrorStart;
                    this.formulaTextBox.SelectionLength = ex.ErrorLenght;
                    this.formulaTextBox.Focus();
                }
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (DivideByZeroException ex)
            {
                // icon error
                this.formulaLabel.Image = Properties.Resources.Error;
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (SystemException ex)
            {
                // icon error
                this.formulaLabel.Image = Properties.Resources.Error;
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region form and controls events

        private void evalButton_Click(object sender, EventArgs e)
        {
            EvaluateFormula();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (this.formulaTextBox.Text.Length > 0)
            {
                this.formulaTextBox.SelectionStart = 0;
                this.formulaTextBox.SelectionLength = this.formulaTextBox.Text.Length;
            }
            this.formulaTextBox.Focus();
        }

        private void formulaTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                this.EvaluateFormula();
        }

        private void wbXml_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            this.Cursor = Cursors.Default;
            this.wbXml.Document.Body.MouseEnter += new HtmlElementEventHandler(Body_MouseEnter);
            this.wbXml.Document.Body.MouseLeave += new HtmlElementEventHandler(Body_MouseLeave);
        }

        void Body_MouseLeave(object sender, HtmlElementEventArgs e)
        {
            this.btnSaveXml.Visible = false;
        }

        void Body_MouseEnter(object sender, HtmlElementEventArgs e)
        {
            this.btnSaveXml.Visible = true;
        }

        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPage.Name == this.tabPage2.Name)
            {
                if (this._formula != null && !this._xmlLoaded)
                {
                    this.Cursor = Cursors.WaitCursor;
                    Application.DoEvents();
                    this.LoadXmlTree();
                    this._xmlLoaded = true;
                }
            }
        }

        private void btnSaveXml_Click(object sender, EventArgs e)
        {
            this.SaveXml();
            this.wbXml.Select();
        }

        #endregion

        #region Tree

        /// <summary>
        /// draw evaluation tree to treeview
        /// </summary>
        /// <param name="parent">parent or null for root</param>
        /// <param name="node">node to draw</param>
        private void DrawTree(TreeNode parent, LeafNode node)
        {
            if (node != null)
            {
                // null parent is root
                if (parent == null)
                {
                    parent = this.treeView1.Nodes.Add(node.Result.ToString());
                    parent = this.treeView1.Nodes[0];
                }

                // create tree node
                TreeNode p = new TreeNode(node.ToString());
                if (node.Type == NodeType.Operator) // operator - colorize and set tooltip
                {
                    // farba
                    p.ForeColor = Color.Green;
                    // tooltip
                    if (node.Right.Result < 0) // negative number to brackets
                        p.ToolTipText = string.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            "{0}{1}({2})={3}",
                            node.Left.Result, node.ToString(), node.Right.Result, node.Result);
                    else
                        p.ToolTipText = string.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            "{0}{1}{2}={3}",
                            node.Left.Result, node.ToString(), node.Right.Result, node.Result);
                }
                parent.Nodes.Add(p);
                this.DrawTree(p, node.Left);  // left
                this.DrawTree(p, node.Right); // right
            }
        }

        private void DrawTreeHelp()
        {
            string text = "See tool tips to view subresults";
            Graphics g = this.treeView1.CreateGraphics();
            Bitmap b = new Bitmap((int)g.MeasureString(text, treeView1.Font).Width + 2, (int)g.MeasureString(text, treeView1.Font).Height + 2);
            g.DrawString(text, this.treeView1.Font, new SolidBrush(Color.Gray), 1, 1);
        }

        /// <summary>
        /// Loads evaluation tree xml to web browser control
        /// </summary>
        private void LoadXmlTree()
        {
            var tmp = Path.GetTempFileName();
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write))
            using (var sw = new StreamWriter(fs, Encoding.UTF8))
            {
                this._doc = this._formula.ToXmlDocument();
                this._doc.Save(sw);
            }
            this.wbXml.Navigate(tmp);
        }
        #endregion

        #region Xml
        private void SaveXml()
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "Save xml as...";
                sfd.DefaultExt = "xml";
                sfd.Filter = "XML Files|*.xml|All Files|*.*";
                sfd.FilterIndex = 0;
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    using (var fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
                    using (var sw = new StreamWriter(fs, Encoding.UTF8))
                        this._doc.Save(sw);
                    MessageBox.Show(this, "Document saved", "info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        #endregion

    }
}
