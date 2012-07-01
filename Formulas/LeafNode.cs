using System;
using System.Collections.Generic;
using System.Text;

namespace Bocej.Info.Formulas
{
    /// <summary>
    /// list vypoctoveho stromu
    /// </summary>
    public class LeafNode
    {
        #region Initializers

        /// <summary>
        /// vytvori instanciu
        /// </summary>
        public LeafNode() { }

        #endregion

        #region Properties

        /// <summary>
        /// lava strana
        /// </summary>
        public LeafNode Left { get; set; }

        /// <summary>
        /// prava strana
        /// </summary>
        public LeafNode Right { get; set; }

        /// <summary>
        /// operator
        /// </summary>
        public object Data { get; set; }

        private double result = double.NaN; // cache vysledku
        private bool resultComputed = false; // indikacia cache vysledku
        /// <summary>
        /// vysledok
        /// </summary>
        public double Result 
        { 
            get 
            {
                if (!this.resultComputed)
                {
                    this.result = this.EvaluateNode();
                    this.resultComputed = true;
                }
                return this.result;
            }
        }

        /// <summary>
        /// typ listu; len Operator alebo Operand
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
        /// vrati data
        /// </summary>
        /// <returns>data string</returns>
        public override string ToString()
        {
            return this.Data.ToString();
        }

        #endregion

        #region Evaluation

        /// <summary>
        /// vypocita vysledok listu
        /// </summary>
        /// <returns>vysledok</returns>
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
