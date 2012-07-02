using System;
using System.Collections.Generic;
using System.Text;

namespace PB.Formulas
{
    /// <summary>
    /// Expression tree node
    /// </summary>
    public class LeafNode
    {
        #region Initializers

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the left side.
        /// </summary>
        public LeafNode Left { get; set; }

        /// <summary>
        /// Gets or sets the right side.
        /// </summary>
        public LeafNode Right { get; set; }

        /// <summary>
        /// operator
        /// </summary>
        public object Data { get; set; }

        private double result = double.NaN; // result cache
        /// <summary>
        /// Expression result
        /// </summary>
        public double Result 
        { 
            get 
            {
                if (double.IsNaN(this.result))
                    this.result = this.EvaluateNode();
                return this.result;
            }
        }

        /// <summary>
        /// Node type (only operator or operand)
        /// </summary>
        public NodeType Type
        {
            get
            {
                if (this.Data is char)
                    return NodeType.Operator;
                else
                    return NodeType.Operand;
            }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Data.ToString();
        }

        #endregion

        #region Evaluation

        /// <summary>
        /// Evaluates the node.
        /// </summary>
        /// <returns>double</returns>
        /// <exception cref="System.DivideByZeroException">When divide by zero detected</exception>
        /// <exception cref="System.FormatException">Wraps other errors, see InnerException</exception>
        internal double EvaluateNode()
        {
            if (this.Data is double) // cislo
                return (double)this.Data;
            else // operator
            {
                // vyhodnotenie podla operatora
                switch (this.Data.ToString())
                {
                    case "+":
                        return this.Left.Result + this.Right.Result;
                    case "-":
                        return this.Left.Result - this.Right.Result;
                    case "*":
                        return this.Left.Result * this.Right.Result;
                    case "/":
                        // pre double system nespusta DivideByZeroException ale vracia double.Infinity,
                        // spustim ju manualne
                        if (this.Right.Result == 0)
                            throw new DivideByZeroException();
                        return this.Left.Result / this.Right.Result;
                    default: // neznamy operator, tu by sa nemal nikdy dostat, ak ano tak je chyba v Formula.ToInfix()
                        throw new FormatException(string.Format("Unknown operator '{0}'.", this.Data));
                }
            }
        }

        #endregion
    }
}
