using System;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Xml;
using System.IO;

namespace PB.Formulas
{
    /// <summary>
    /// Formula engine
    /// </summary>
    public class Formula
    {
        private readonly string[] signs = { "-", "+" };

        #region Initializers & Creators

        /// <summary>
        /// Initializes a new instance of the <see cref="Formula" /> class.
        /// </summary>
        /// <param name="formula">The expression string.</param>
        public Formula(string formula)
        {
            this.Evaluate(formula);
            this.Expression = formula;
        }

        #endregion

        #region Evaluation

        /// <summary>
        /// Evaluates the specified formula.
        /// </summary>
        /// <param name="formula">The formula.</param>
        /// <exception cref="System.ArgumentNullException">When formula is empty</exception>
        private void Evaluate(string formula)
        {
            // empty formula
            if (formula == null || string.IsNullOrEmpty(formula.Trim()))
                throw new ArgumentNullException();

            // extract to elements - infix notation
            this.Infix = this.ToInfix(formula);

            // change to postfix layout
            this.Postfix = this.ToPostfix(this.Infix);

            // evaluation tree
            this.EvaluationTreeRoot = this.ToEvaluationTree(this.Postfix);

        }

        #region Infix

        /// <summary>
        /// extract to elements - infix notation
        /// </summary>
        /// <param name="formula">expression</param>
        /// <returns>array of nodes</returns>
        private Node[] ToInfix(string formula)
        {
            // thousents and decimal separators
            char[] otherNumerisc = new char[] { 
                CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0], 
                CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator[0],
                '+', '-' };
            // output array
            ArrayList nodes = new ArrayList();
            // number of open brackets
            int openBrackets = 0;
            // next allowed element
            NodeType nextAllowed = NodeType.Sign | NodeType.Operand | NodeType.OpeningBracket;
            // type of element
            NodeType nodeType = NodeType.None;
            // start position of element in formula
            int opStart = 0;

            for (int i = 0; i < formula.Length; i++)
            {
                char c = formula[i];
                // empty
                if (char.IsWhiteSpace(c))
                    continue;

                // save start position
                opStart = i;

                switch (c)
                {
                    case '(':
                        nodeType = NodeType.OpeningBracket;
                        if ((nextAllowed & nodeType) == 0)
                            throw new FormulaException("Opening bracket is not allowed here.", formula, i, 1);
                        nextAllowed = NodeType.Sign | NodeType.OpeningBracket | NodeType.Operand;
                        openBrackets++;
                        break;
                    case ')':
                        nodeType = NodeType.ClosingBracket;
                        if ((nextAllowed & nodeType) == 0 || openBrackets == 0)
                            throw new FormulaException("Closing bracket is not allowed here.", formula, i, 1);
                        nextAllowed = NodeType.Sign | NodeType.Operator | NodeType.ClosingBracket;
                        openBrackets--;
                        break;
                    case '+':
                    case '-':
                        // begin of subexpression, bracket or +- sign: operator
                        if (nodeType == NodeType.None ||
                            nodeType == NodeType.OpeningBracket ||
                            (nodes.Count > 0 && Array.IndexOf(this.signs, ((Node)nodes[nodes.Count - 1]).Value.ToString()) >= 0))
                            goto ProcessOperand;

                        nodeType = NodeType.Sign;
                        if ((nextAllowed & nodeType) == 0)
                            throw new FormulaException("Operator is not allowed here.", formula, i, 1);
                        nextAllowed = NodeType.OpeningBracket | NodeType.Operand | NodeType.Sign;
                        break;
                    case '*':
                    case '/':
                        nodeType = NodeType.Operator;
                        if ((nextAllowed & nodeType) == 0)
                            throw new FormulaException("Operator is not allowed here.", formula, i, 1);
                        nextAllowed = NodeType.OpeningBracket | NodeType.Operand;
                        break;
                    default:
                    ProcessOperand:
                        nodeType = NodeType.Operand;
                    if ((nextAllowed & nodeType) == 0)
                        throw new FormulaException("Invalid character.", formula, i, 1);
                    // load whole operand
                    StringBuilder operand = new StringBuilder();
                    do
                    {
                        if (!char.IsWhiteSpace(c))
                            operand.Append(c);
                        i++;
                        if (i == formula.Length)
                            break;
                        c = formula[i];
                    } while (i < formula.Length && (char.IsDigit(c) || Array.BinarySearch(otherNumerisc, c) >= 0 || char.IsWhiteSpace(c)));
                    // previous position
                    i--;
                    // text to number, initialize node
                    double val = 0;
                    if (double.TryParse(operand.ToString(), out val))
                        nodes.Add(new Node(val));
                    else
                        throw new FormulaException("Invalid operand.", formula, opStart, i - opStart);
                    nextAllowed = NodeType.Operator | NodeType.Sign | NodeType.ClosingBracket;
                    break;
                }
                // sign to operator
                if (nodeType == NodeType.Sign)
                    nodeType = NodeType.Operator;
                // add operator and bracket (operand was detected in switch section)
                if (nodeType != NodeType.Operand)
                    nodes.Add(new Node(c, nodeType));
            }

            // chechk close all brackets
            if (openBrackets != 0)
                throw new FormulaException("Brackets are not closed.");

            // check last node, must be closing bracket or operand
            Node last = (Node)nodes[nodes.Count - 1];
            if (!(last.Type == NodeType.ClosingBracket || last.Type == NodeType.Operand))
                throw new FormulaException("Invalid characters.", formula, opStart, formula.Length - opStart);

            // return array
            return (Node[])nodes.ToArray(typeof(Node));
        }



        #endregion

        #region Postfix

        /// <summary>
        /// change to postfix layout
        /// </summary>
        /// <param name="infixNodes">infix array</param>
        /// <returns>postfix array</returns>
        private Node[] ToPostfix(Node[] infixNodes)
        {
            // temporary stack
            Stack stack = new Stack();
            // output array
            ArrayList output = new ArrayList();
            foreach (Node node in infixNodes)
            {
                switch (node.Type)
                {
                    case NodeType.Operand: // add now
                        output.Add(node);
                        break;
                    case NodeType.OpeningBracket: // add last from stack
                        stack.Push(node);
                        break;
                    case NodeType.Operator:
                        this.ProcessOperator(node, stack, output);
                        break;
                    case NodeType.ClosingBracket:
                        this.StackToOutput(stack, output, true);
                        break;
                }
            }
            // flush stack to output
            this.StackToOutput(stack, output, false);

            return (Node[])output.ToArray(typeof(Node));
        }

        /// <summary>
        /// process operator by type and priority
        /// </summary>
        /// <param name="node">operator</param>
        /// <param name="stack">stack of nodes</param>
        /// <param name="output">output arraylist</param>
        private void ProcessOperator(Node node, Stack stack, ArrayList output)
        {
            if (stack.Count == 0) // empty stack
            {
                // operator to stack
                stack.Push(node);
            }
            else
            {
                if (((Node)stack.Peek()).Type == NodeType.OpeningBracket) // there is opening bracked on top of stack
                {
                    // operator to stack
                    stack.Push(node);
                }
                else
                {
                    if (node.Priority > ((Node)stack.Peek()).Priority) // operator has higher priority then last in stack
                        // operator to stack
                        stack.Push(node);
                    else  // operator has lower priority then last in stack
                    {
                        // last from stack to output
                        output.Add(stack.Pop());
                        // recursive repeat
                        this.ProcessOperator(node, stack, output);
                    }
                }
            }
        }

        /// <summary>
        /// pushs stack to output
        /// </summary>
        /// <param name="stack">stack</param>
        /// <param name="output">output array</param>
        private void StackToOutput(Stack stack, ArrayList output, bool onlyToBracket)
        {
            while (stack.Count > 0)
            {
                Node node = (Node)stack.Pop();
                if (node.Type != NodeType.OpeningBracket)
                {
                    output.Add(node);
                }
                else
                {
                    if (onlyToBracket)
                        break;
                }
            }
        }

        #endregion

        #region Evaluation tree

        /// <summary>
        /// reates evaluation tree from postfix
        /// </summary>
        /// <param name="postfixNodes">postfix array</param>
        /// <returns>tree root node</returns>
        private LeafNode ToEvaluationTree(Node[] postfixNodes)
        {
            Stack stack = new Stack();
            foreach (Node node in postfixNodes)
            {
                LeafNode ln;
                switch (node.Type)
                {
                    case NodeType.Operand: // operand - only push to stack
                        ln = new LeafNode();
                        ln.Data = node.Value;
                        stack.Push(ln);
                        break;
                    case NodeType.Operator: // operator: setup left and right side and push to stack
                        ln = new LeafNode();
                        ln.Data = node.Value;
                        ln.Right = (LeafNode)stack.Pop();
                        ln.Left = (LeafNode)stack.Pop();
                        stack.Push(ln);
                        break;
                }
            }

            if (stack.Count > 0)
                return (LeafNode)stack.Pop();
            else
                return null;
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// infix notation
        /// </summary>
        public Node[] Infix { get; private set; }

        /// <summary>
        /// postfix notation
        /// </summary>
        public Node[] Postfix { get; private set; }

        /// <summary>
        /// evaluation tree root node
        /// </summary>
        public LeafNode EvaluationTreeRoot { get; private set; }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        public string Expression { get; private set; }

        /// <summary>
        /// formula result
        /// </summary>
        public double Result
        {
            get
            {
                if (this.EvaluationTreeRoot != null)
                    return this.EvaluationTreeRoot.Result;
                else
                    return 0;
            }
        }

        #endregion

        #region Xml
        /// <summary>
        /// To the XML stream.
        /// </summary>
        /// <returns>xml stream</returns>
        public Stream ToXmlStream()
        {
            try
            {
                var doc = this.ToXmlDocument();
                var buffer = Encoding.UTF8.GetBytes(doc.ToString());
                var mem = new MemoryStream(buffer);
                mem.Position = 0;
                return mem;
            }
            catch (FormulaException) { throw; }
            catch (SystemException e)
            {
                throw new FormatException("Error creating xml stream.", e);
            }
        }
        /// <summary>
        /// To the XML document.
        /// </summary>
        /// <returns>xml document tree</returns>
        public XmlDocument ToXmlDocument()
        {
            if (this.EvaluationTreeRoot == null)
                throw new FormulaException("Nothing was evauated yet.");
            try
            {
                var doc = new XmlDocument();
                var el = doc.CreateElement("formula");
                var attr = doc.CreateAttribute("expression");
                attr.Value = this.Expression;
                el.Attributes.Append(attr);
                attr = doc.CreateAttribute("result");
                attr.Value = this.Result.ToString();
                el.Attributes.Append(attr);
                doc.AppendChild(el);
                this.MakeXmlTree(doc.DocumentElement, this.EvaluationTreeRoot);
                return doc;
            }
            catch (SystemException e)
            {
                throw new FormatException("Error creating xml tree.", e);
            }
        }

        private void MakeXmlTree(XmlElement parent, LeafNode node)
        {
            if (node != null)
            {
                // creating element
                XmlElement el = parent.OwnerDocument.CreateElement(node.Type.ToString());
                XmlAttribute attr = parent.OwnerDocument.CreateAttribute("data");
                attr.Value=node.ToString();
                el.Attributes.Append(attr);
                if (node.Type == NodeType.Operator) // operator - expression and result attributes
                {
                    XmlAttribute a = el.Attributes.Append(parent.OwnerDocument.CreateAttribute("expression"));
                    if (node.Right.Result < 0) // negative number to bracket
                        a.Value = string.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            "{0}{1}({2})",
                            node.Left.Result, node.ToString(), node.Right.Result);
                    else
                        a.Value = string.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            "{0}{1}{2}",
                            node.Left.Result, node.ToString(), node.Right.Result);
                    el.Attributes.Append(a);
                    a = el.Attributes.Append(parent.OwnerDocument.CreateAttribute("result"));
                    a.Value = node.Result.ToString();
                    el.Attributes.Append(a);
                }
                parent.AppendChild(el);
                this.MakeXmlTree(el, node.Left);  // left
                this.MakeXmlTree(el, node.Right); // right
            }
        }
        #endregion
    }
}
