using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace PB.Formulas
{
    /// <summary>
    /// Formula parsing exception
    /// </summary>
    public class FormulaException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaException" /> class.
        /// </summary>
        public FormulaException() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public FormulaException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaException" /> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="formulaText">Formula text</param>
        public FormulaException(string message, string formulaText)
            : this(message)
        {
            this.FormulaText = formulaText;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="formulaText">The formula text.</param>
        /// <param name="errStart">Start position in formula.</param>
        /// <param name="errLenght">Length of error text.</param>
        public FormulaException(string message, string formulaText, int errStart, int errLenght)
            : this(message)
        {
            this.FormulaText = formulaText;
            this.ErrorStart = errStart;
            this.ErrorLenght = errLenght;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaException" /> class for serialization.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected FormulaException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        /// <summary>
        /// Gets the formula text.
        /// </summary>
        public string FormulaText { get; private set; }

        /// <summary>
        /// Gets the error start.
        /// </summary>
        public int ErrorStart { get; private set; }

        /// <summary>
        /// Gets the error lenght.
        /// </summary>
        public int ErrorLenght { get; private set; }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="T:System.Runtime.Serialization.SerializationInfo"></see> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"></see> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"></see> that contains contextual information about the source or destination.</param>
        /// <PermissionSet><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Read="*AllFiles*" PathDiscovery="*AllFiles*" /><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="SerializationFormatter" /></PermissionSet>
        /// <exception cref="System.ArgumentNullException"></exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            info.AddValue("FormulaText", this.FormulaText);
            info.AddValue("ErrorStart", this.ErrorStart);
            info.AddValue("ErrorLenght", this.ErrorLenght);

            base.GetObjectData(info, context);
        }
    }
}
