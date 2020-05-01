namespace mmOrderMarking.Models
{
    using System.Collections.Generic;
    using Autodesk.Revit.DB;

    /// <summary>
    /// Объект, содержащий информацию о том, какие элементе в какой строке находятся
    /// </summary>
    public class ElementsInRow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ElementsInRow"/> class.
        /// </summary>
        /// <param name="rowNumber">Row number</param>
        public ElementsInRow(int rowNumber)
        {
            RowNumber = rowNumber;
        }

        /// <summary>
        /// Row number
        /// </summary>
        public int RowNumber { get; }

        /// <summary>
        /// Elements in row
        /// </summary>
        public List<Element> Elements { get; } = new List<Element>();
    }
}