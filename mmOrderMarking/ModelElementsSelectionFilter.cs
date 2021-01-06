namespace mmOrderMarking
{
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI.Selection;
    using Models;

    /// <summary>
    /// Фильтр выбора элементов в модели
    /// </summary>
    public class ModelElementsSelectionFilter : ISelectionFilter
    {
        private readonly IEnumerable<RevitBuiltInCategory> _categories;

        public ModelElementsSelectionFilter(IEnumerable<RevitBuiltInCategory> categories)
        {
            _categories = categories;
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

        private bool IsValid(Element e)
        {
            if (e is Group)
                return false;

            if (_categories.Any() && e.Category != null)
            {
                return _categories.FirstOrDefault(c => (int)c.BuiltInCategory == e.Category.Id.IntegerValue) != null;
            }
            
            return true;
        }
    }
}
