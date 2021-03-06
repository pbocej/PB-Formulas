﻿using System;

namespace PB.Formulas
{
    /// <summary>
    /// Type of the expression node
    /// </summary>
    [Flags]
    public enum NodeType : int
    {
        Operator = 1,
        Operand = 2,
        OpeningBracket = 4,
        ClosingBracket = 8,
        Sign = 16,
        None = Int32.MaxValue
    }
}
