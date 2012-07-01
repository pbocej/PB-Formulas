using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Bocej.Info.Formulas;
using System.Drawing.Drawing2D;

namespace Bocej.Info.FEval
{
    public partial class MainForm : Form
    {
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
            try
            {
                // vytvorenie objektu vzorca a vypocet
                Formula f = new Formula(this.formulaTextBox.Text);
                // ikona ok
                this.formulaLabel.Image = Properties.Resources.Success;
                // syntax highlight
                this.infixTextBox.Text = string.Empty;
                for (int i = 0; i < f.Infix.Length; i++)
                {
                    // nastavim farbu podla typu
                    switch (f.Infix[i].Type)
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
                    this.infixTextBox.AppendText(f.Infix[i].Value.ToString());
                }
                // result
                this.resultTextBox.Text = f.Result.ToString();
                // evaluation tree
                this.treeView1.Nodes.Clear();
                this.DrawTree(null, f.EvaluationTreeRoot);
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

        #endregion


    }
}
