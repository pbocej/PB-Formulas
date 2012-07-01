using System;

namespace Bocej.Info.Formulas
{
    /// <summary>
    /// polozka vzorca
    /// </summary>
    public class Node
    {
        // znamienka
        private static char[] signs = new char[] { '+', '-' };

        /// <summary>
        /// inicializuje polozku podla hodnoty a typu
        /// </summary>
        /// <param name="value">operator or bracket</param>
        /// <param name="type">node type</param>
        public Node(object value, NodeType type)
        {
            this.Value = value;
            this.Type = type;
            // podla typu nastavim prioritu
            switch (type)
            {
                case NodeType.Operator:         // +,-,*,/
                    if (Array.BinarySearch(signs, (char)value) >= 0)
                        this.Priority = 1;
                    else
                        this.Priority = 2;
                    break;
                case NodeType.Operand:          // number
                    this.Priority = 0;
                    break;
                case NodeType.OpeningBracket:   // (
                    this.Priority = 3;
                    break;
                case NodeType.ClosingBracket:   // )
                    this.Priority = 3;
                    break;
            }
        }

        /// <summary>
        /// inicializuje polozku cislom
        /// </summary>
        /// <param name="value">number</param>
        public Node(double value) : this(value, NodeType.Operand) { }

        /// <summary>
        /// typ polozky
        /// </summary>
        public NodeType Type { get; private set; }

        /// <summary>
        /// hodnota polozky
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// priorita
        /// </summary>
        public byte Priority { get; private set; }

        /// <summary>
        /// vrati string z Value
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Value.ToString();
        }
    }
}
