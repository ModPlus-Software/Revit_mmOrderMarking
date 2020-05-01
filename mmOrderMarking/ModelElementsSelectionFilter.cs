namespace mmOrderMarking
{
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI.Selection;

    /// <summary>
    /// Фильтр выбора элементов в модели
    /// </summary>
    public class ModelElementsSelectionFilter : ISelectionFilter
    {
        /// <summary>
        /// Является ли элемент валидным для обработки
        /// </summary>
        /// <param name="e">Элемент Revit</param>
        public static bool IsValid(Element e)
        {
            if (e is Group)
                return false;
            
            return true;
        }

        /// <inheritdoc/>
        public bool AllowElement(Element elem)
        {
            return IsValid(elem);
        }

        /// <inheritdoc/>
        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new System.NotImplementedException();
        }
    }
}
