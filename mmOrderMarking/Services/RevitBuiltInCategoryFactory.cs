namespace mmOrderMarking.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Models;

    /// <summary>
    /// Фабрика категорий Revit
    /// </summary>
    public class RevitBuiltInCategoryFactory
    {
        private readonly UIApplication _uiApplication;
        private List<RevitBuiltInCategory> _revitBuiltInCategories;

        public RevitBuiltInCategoryFactory(UIApplication uiApplication)
        {
            _uiApplication = uiApplication;
        }
        
        public List<RevitBuiltInCategory> GetRevitBuiltInCategories()
        {
            if (_revitBuiltInCategories != null)
                return _revitBuiltInCategories;

            _revitBuiltInCategories = new List<RevitBuiltInCategory>();
            var builtInCategories = 
                ConvertToBuiltIn(GetCategoriesIdsIEnumerable(_uiApplication.ActiveUIDocument.Document, true)).ToList();
            foreach (var builtInCategory in builtInCategories)
            {
                var revitBuiltInCategory = new RevitBuiltInCategory(builtInCategory);
                if (string.IsNullOrEmpty(revitBuiltInCategory.DisplayName))
                    continue;
                _revitBuiltInCategories.Add(revitBuiltInCategory);
            }

            return _revitBuiltInCategories;
        }

        /// <summary>
        /// Получение списка идентификаторов категорий, имеющихся в проекте
        /// </summary>
        /// <param name="doc">Документ</param>
        /// <param name="onlyAllowsBoundParameters">Только категории, к которым можно привязать общие параметры</param>
        /// <param name="includeSubCategories">Включая подкатегории</param>
        /// <returns></returns>
        private IEnumerable<int> GetCategoriesIdsIEnumerable(
            Document doc,
            bool onlyAllowsBoundParameters = false,
            bool includeSubCategories = true)
        {
            foreach (Category category in doc.Settings.Categories)
            {
                if (onlyAllowsBoundParameters)
                {
                    if (category.AllowsBoundParameters)
                        yield return category.Id.IntegerValue;
                }
                else
                {
                    yield return category.Id.IntegerValue;
                }

                if (!includeSubCategories)
                    continue;

                foreach (Category subCategory in category.SubCategories)
                {
                    if (onlyAllowsBoundParameters)
                    {
                        if (subCategory.AllowsBoundParameters)
                            yield return subCategory.Id.IntegerValue;
                    }
                    else
                    {
                        yield return subCategory.Id.IntegerValue;
                    }
                }
            }
        }

        /// <summary>
        /// Конвертировать список исключений из конфигурации в BuiltInCategory
        /// </summary>
        /// <param name="categories">Список идентификаторов категорий (int)</param>
        /// <returns></returns>
        private IEnumerable<BuiltInCategory> ConvertToBuiltIn(IEnumerable<int> categories)
        {
            return categories.Select(excludeCategoriesId => (BuiltInCategory)excludeCategoriesId);
        }
    }
}
