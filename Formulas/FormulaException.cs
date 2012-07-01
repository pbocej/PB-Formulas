using System;
using System.Collections.Generic;
using System.Text;

namespace Bocej.Info.Formulas
{
    /// <summary>
    /// chyba pri parsovani vzorca
    /// </summary>
    public class FormulaException : ApplicationException
    {
        /// <summary>
        /// vytvori prazdnu chybu
        /// </summary>
        public FormulaException() { }
        /// <summary>
        /// vytvori chybu so spravou
        /// </summary>
        /// <param name="message">sprava</param>
        public FormulaException(string message) : base(message) { }
        /// <summary>
        /// vytvori chybu so spravou a vzorcom
        /// </summary>
        /// <param name="message">sprava</param>
        /// <param name="formulaText">vzorec</param>
        public FormulaException(string message, string formulaText)
            : this(message)
        {
            this.FormulaText = formulaText;
        }
        /// <summary>
        /// vytvori chybu so spravou, vzorcom a poziciou chyby pre oznacenie
        /// </summary>
        /// <param name="message">sprava</param>
        /// <param name="formulaText">vzorec</param>
        /// <param name="errStart">pozicia chyby vo vzorci</param>
        /// <param name="errLenght">dlzka chyby</param>
        public FormulaException(string message, string formulaText, int errStart, int errLenght)
            : this(message)
        {
            this.FormulaText = formulaText;
            this.ErrorStart = errStart;
            this.ErrorLenght = errLenght;
        }

        /// <summary>
        /// text vzorca
        /// </summary>
        public string FormulaText { get; private set; }

        /// <summary>
        /// zaciatok chyby
        /// </summary>
        public int ErrorStart { get; private set; }

        /// <summary>
        /// dlzka chyby
        /// </summary>
        public int ErrorLenght { get; private set; }
    }
}
