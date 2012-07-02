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

        #region vypocet

        /// <summary>
        /// vypocet
        /// </summary>
        private void EvaluateFormula()
        {
            this._xmlLoaded = false;
            this._doc = null;
            try
            {
                // vytvorenie objektu vzorca a vypocet
                this._formula = new Formula(this.formulaTextBox.Text);
                // ikona ok
                this.formulaLabel.Image = Properties.Resources.Success;
                // syntax highlight
                this.infixTextBox.Text = string.Empty;
                for (int i = 0; i < this._formula.Infix.Length; i++)
                {
                    // nastavim farbu podla typu
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
                    // pridam do textu
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
                // ikona error
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
                // ikona error
                this.formulaLabel.Image = Properties.Resources.Error;
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (SystemException ex)
            {
                // ikona error
                this.formulaLabel.Image = Properties.Resources.Error;
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Eventy

        // klik na tlacitko vypoctu
        private void evalButton_Click(object sender, EventArgs e)
        {
            EvaluateFormula();
        }

        // fokus a oznacenie textboxu pre vzorec pri starte
        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (this.formulaTextBox.Text.Length > 0)
            {
                this.formulaTextBox.SelectionStart = 0;
                this.formulaTextBox.SelectionLength = this.formulaTextBox.Text.Length;
            }
            this.formulaTextBox.Focus();
        }

        // enter spusta vypocet
        private void formulaTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                this.EvaluateFormula();
        }

        #endregion

        #region Tree

        /// <summary>
        /// vykresli vypoctovy strom
        /// </summary>
        /// <param name="parent">vlastnik alebo null pre root</param>
        /// <param name="node">polozka na vykreslenie</param>
        private void DrawTree(TreeNode parent, LeafNode node)
        {
            // kontrola na null
            if (node != null)
            {
                // ak neexistuje vlastnik, vytvorim (je to root)
                if (parent == null)
                {
                    parent = this.treeView1.Nodes.Add(node.Result.ToString());
                    parent = this.treeView1.Nodes[0];
                }

                // vytvorenie treenode
                TreeNode p = new TreeNode(node.ToString());
                if (node.Type == NodeType.Operator) // operator vyfarbim a dam tooltip
                {
                    // farba
                    p.ForeColor = Color.Green;
                    // tooltip
                    if (node.Right.Result < 0) // zaporne cislo vpravo dam do zatvoriek
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
                this.DrawTree(p, node.Left);  // lavy
                this.DrawTree(p, node.Right); // pravy
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
        /// Loads evaluation tree xml to web browser
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
            this.wbXml.Select();
        }
    }
}
