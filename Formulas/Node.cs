using System;

namespace PB.Formulas
{
    /// <summary>
    /// Expression node
    /// </summary>
    public class Node
    {
        private static char[] signs = new char[] { '+', '-' };

        /// <summary>
        /// Initializes a new instance of the <see cref="Node" /> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="type">The node type.</param>
        public Node(object value, NodeType type)
        {
            this.Value = value;
            this.Type = type;
            // setup priority by type
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
        /// Initializes a new instance of the <see cref="Node" /> class with number.
        /// </summary>
        /// <param name="value">The number.</param>
        public Node(double value) : this(value, NodeType.Operand) { }

        /// <summary>
        /// Gets the type.
        /// </summary>
        public NodeType Type { get; private set; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        public byte Priority { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Value.ToString();
        }
    }
}
