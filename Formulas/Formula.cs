using System;
using System.Text;
using System.Collections;
using System.Globalization;

namespace Bocej.Info.Formulas
{
    /// <summary>
    /// vzorec
    /// </summary>
    public class Formula
    {

        private readonly string[] signs = { "-", "+" };

        #region Initializers & Creators

        /// <summary>
        /// vytvori objekt vzorca a vypocita ho
        /// </summary>
        /// <param name="formula">formula</param>
        public Formula(string formula)
        {
            this.Evaluate(formula);
        }

        #endregion

        #region Evaluation

        /// <summary>
        /// vypocet
        /// </summary>
        /// <param name="formula">vstupny vzorec</param>
        private void Evaluate(string formula)
        {
            // prazdny vzorec
            if (formula == null || string.IsNullOrEmpty(formula.Trim()))
                throw new ArgumentNullException();

            // rozlozenie na jednotlive elementy, infix notacia
            this.Infix = this.ToInfix(formula);

            // usporiadanie podla postfix notacie
            this.Postfix = this.ToPostfix(this.Infix);

            // vypoctovy strom
            this.EvaluationTreeRoot = this.ToEvaluationTree(this.Postfix);

        }

        #region Infix

        /// <summary>
        /// rozlozenie na jednotlive elementy, infix notacia
        /// </summary>
        /// <param name="formula">vzorec</param>
        /// <returns>pole prvkov</returns>
        private Node[] ToInfix(string formula)
        {
            // oddelovace tisicov a des. miest
            char[] otherNumerisc = new char[] { 
                CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0], 
                CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator[0],
                '+', '-' };
            // vystupne pole
            ArrayList nodes = new ArrayList();
            // otvorene zatvorky
            int openBrackets = 0;
            // nasledujuci povoleny element
            NodeType nextAllowed = NodeType.Sign | NodeType.Operand | NodeType.OpeningBracket;
            // type elementu
            NodeType nodeType = NodeType.None;
            // zaciatok elementu v stringu vzorca
            int opStart = 0;
            // cyklus po znakoch vo vzorci
            for (int i = 0; i < formula.Length; i++)
            {
                char c = formula[i];
                // prazdne miesto
                if (char.IsWhiteSpace(c))
                    continue;

                // zapisem zaciatok elementu
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
                        // ak som na zaciatku vzorca, alebo zatvoriek, alebo bolo predchadzajuce tiez +-, znamienko tvori operator
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
                    // nacitam cely operator
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
                    // vratim i na predoslu hodnotu
                    i--;
                    // zmenim text na cislo a ulozim
                    double val = 0;
                    if (double.TryParse(operand.ToString(), out val))
                        nodes.Add(new Node(val));
                    else
                        throw new FormulaException("Invalid operand.", formula, opStart, i - opStart);
                    nextAllowed = NodeType.Operator | NodeType.Sign | NodeType.ClosingBracket;
                    break;
                }
                // zmenim typ znamienko na operator
                if (nodeType == NodeType.Sign)
                    nodeType = NodeType.Operator;
                // pridam operator a zatvorku (operand sa zapisal vo svojej sekcii switch)
                if (nodeType != NodeType.Operand)
                    nodes.Add(new Node(c, nodeType));
            }

            // kontrola zatvorenia zatvoriek
            if (openBrackets != 0)
                throw new FormulaException("Brackets are not closed.");

            // kontrola posledneho prvku, musi to byt ) alebo operand
            Node last = (Node)nodes[nodes.Count - 1];
            if (!(last.Type == NodeType.ClosingBracket || last.Type == NodeType.Operand))
                throw new FormulaException("Invalid characters.", formula, opStart, formula.Length - opStart);

            // vratim pole
            return (Node[])nodes.ToArray(typeof(Node));
        }



        #endregion

        #region Postfix

        /// <summary>
        /// usporiadanie podla postfix notacie
        /// </summary>
        /// <param name="infixNodes">infix pole</param>
        /// <returns>postfix pole</returns>
        private Node[] ToPostfix(Node[] infixNodes)
        {
            // pomocna halda
            Stack stack = new Stack();
            // vystupne pole
            ArrayList output = new ArrayList();
            foreach (Node node in infixNodes)
            {
                switch (node.Type)
                {
                    case NodeType.Operand: // pridam ihned
                        output.Add(node);
                        break;
                    case NodeType.OpeningBracket: // vlozim posledny z haldy
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
            // zbytok haldy do vystupu
            this.StackToOutput(stack, output, false);

            return (Node[])output.ToArray(typeof(Node));
        }

        /// <summary>
        /// spracovanie operatora podla typu a priority
        /// </summary>
        /// <param name="node">operator</param>
        /// <param name="stack">halda</param>
        /// <param name="output">vystup</param>
        private void ProcessOperator(Node node, Stack stack, ArrayList output)
        {
            if (stack.Count == 0) // prazdna halda
            {
                // vlozim operator do haldy
                stack.Push(node);
            }
            else
            {
                if (((Node)stack.Peek()).Type == NodeType.OpeningBracket) // na vrchu haldy je otvorena zatvorka
                {
                    // vlozim operator do haldy
                    stack.Push(node);
                }
                else
                {
                    if (node.Priority > ((Node)stack.Peek()).Priority) // operator ma vacsiu prioritu ako posledny v halde
                        // vlozim operator do haldy
                        stack.Push(node);
                    else  // operator ma mensiu prioritu ako posledny v halde
                    {
                        // vlozim posledny z haldy do vystupu
                        output.Add(stack.Pop());
                        // opakujem proceduru
                        this.ProcessOperator(node, stack, output);
                    }
                }
            }
        }

        /// <summary>
        /// vysype haldu do vystupu
        /// </summary>
        /// <param name="stack">halda</param>
        /// <param name="output">vystup</param>
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
        /// vytvori vypoctovy strom z postfix pola
        /// </summary>
        /// <param name="postfixNodes">postfix pole</param>
        /// <returns>kmen stromu</returns>
        private LeafNode ToEvaluationTree(Node[] postfixNodes)
        {
            // halda
            Stack stack = new Stack();
            foreach (Node node in postfixNodes)
            {
                LeafNode ln;
                switch (node.Type)
                {
                    case NodeType.Operand: // operand len vlozim do haldy
                        ln = new LeafNode();
                        ln.Data = node.Value;
                        stack.Push(ln);
                        break;
                    case NodeType.Operator: // operator: nastavim lavu a pravu stranu a vlozim do haldy
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
        /// infix notacia
        /// </summary>
        public Node[] Infix { get; private set; }

        /// <summary>
        /// postfix notacia
        /// </summary>
        public Node[] Postfix { get; private set; }

        /// <summary>
        /// kmen vypoctoveho stromu
        /// </summary>
        public LeafNode EvaluationTreeRoot { get; private set; }

        /// <summary>
        /// vysledok
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
    }
}
