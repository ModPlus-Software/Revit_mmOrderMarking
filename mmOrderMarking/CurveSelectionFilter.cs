namespace mmOrderMarking
{
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI.Selection;

    /// <summary>
    /// Curve selection filter
    /// </summary>
    public class CurveSelectionFilter : ISelectionFilter
    {
        /// <inheritdoc />
        public bool AllowElement(Element elem)
        {
            return elem is ModelCurve || elem is DetailCurve;
        }

        /// <inheritdoc />
        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new System.NotImplementedException();
        }
    }
}
