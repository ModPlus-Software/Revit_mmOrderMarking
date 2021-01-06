namespace mmOrderMarking.Models
{
    using System;
    using Autodesk.Revit.DB;
    using ModPlusAPI.Mvvm;

    /// <summary>
    /// Категория Revit
    /// </summary>
    [Serializable]
    public class RevitBuiltInCategory : VmBase
    {
        private bool _isSelected;
        private bool _isVisible = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="RevitBuiltInCategory"/> class.
        /// </summary>
        /// <param name="builtInCategory"><see cref="Autodesk.Revit.DB.BuiltInCategory"/></param>
        public RevitBuiltInCategory(BuiltInCategory builtInCategory)
        {
            BuiltInCategory = builtInCategory;
            BuiltInCategoryName = builtInCategory.ToString();
            try
            {
                DisplayName = GetDisplayName(builtInCategory);
            }
            catch
            {
                DisplayName = string.Empty;
            }
        }

        /// <summary>
        /// Выбрана ли категория в списке (для диалогового окна выбора категорий)
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected)
                    return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Видимость категории для реализации поиска
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value == _isVisible)
                    return;
                _isVisible = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// BuiltInCategory
        /// </summary>
        public BuiltInCategory BuiltInCategory { get; set; }

        /// <summary>
        /// BuiltInCategory.ToString()
        /// </summary>
        public string BuiltInCategoryName { get; set; }

        /// <summary>
        /// Отображаемое имя категории
        /// </summary>
        public string DisplayName { get; }

        private string GetDisplayName(BuiltInCategory builtInCategory)
        {
#if R2017 || R2018 || R2019
            return Category.GetCategory(Command.UiApplication.ActiveUIDocument.Document, builtInCategory).Name;
#else
            return LabelUtils.GetLabelFor(builtInCategory);
#endif
        }
    }
}
