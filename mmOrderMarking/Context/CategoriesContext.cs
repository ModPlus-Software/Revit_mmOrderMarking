namespace mmOrderMarking.Context
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Input;
    using Models;
    using ModPlusAPI.Mvvm;
    using Services;

    /// <summary>
    /// Контекст окна выбора категорий
    /// </summary>
    public class CategoriesContext : VmBase
    {
        private string _searchText;
        
        /// <summary>
        /// Categories
        /// </summary>
        public ObservableCollection<RevitBuiltInCategory> Categories { get; set; }

        /// <summary>
        /// Количество выбранных категорий
        /// </summary>
        public int SelectedCategoriesCount => Categories.Count(c => c.IsSelected);

        /// <summary>
        /// Текст поиска
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (value == _searchText)
                    return;
                _searchText = value;
                OnPropertyChanged();
                SearchInvoke();
            }
        }

        /// <summary>
        /// Снять выбор со всех категорий
        /// </summary>
        public ICommand UncheckAllCommand => new RelayCommandWithoutParameter(() =>
        {
            foreach (var category in Categories)
            {
                category.IsSelected = false;
            }
        });

        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="revitBuiltInCategoryFactory">Фабрика категорий</param>
        /// <param name="selectedCategories">Выбранные категории</param>
        public void Initialize(
            RevitBuiltInCategoryFactory revitBuiltInCategoryFactory,
            IEnumerable<RevitBuiltInCategory> selectedCategories)
        {
            Categories = new ObservableCollection<RevitBuiltInCategory>(revitBuiltInCategoryFactory.GetRevitBuiltInCategories());

            foreach (var c in Categories)
            {
                c.IsVisible = true;
                c.IsSelected = false;
                c.PropertyChanged += CategoryOnPropertyChanged;
            }

            foreach (var c in selectedCategories)
            {
                var bc = Categories.FirstOrDefault(i => i.DisplayName == c.DisplayName);
                if (bc != null)
                    bc.IsSelected = true;
            }
            
            OnPropertyChanged(nameof(Categories));
        }

        private void CategoryOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected")
                OnPropertyChanged(nameof(SelectedCategoriesCount));
        }

        private void SearchInvoke()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var revitBuiltInCategory in Categories)
                {
                    revitBuiltInCategory.IsVisible = true;
                }
            }
            else
            {
                var searchStringUpper = SearchText.ToUpper();
                if (searchStringUpper == "*")
                {
                    foreach (var revitBuiltInCategory in Categories)
                    {
                        revitBuiltInCategory.IsVisible = revitBuiltInCategory.IsSelected;
                    }
                }
                else
                {
                    foreach (var revitBuiltInCategory in Categories)
                    {
                        revitBuiltInCategory.IsVisible =
                            revitBuiltInCategory.DisplayName.ToUpper().Contains(searchStringUpper) ||
                            revitBuiltInCategory.BuiltInCategoryName.ToUpper().Contains(searchStringUpper);
                    }
                }
            }
        }
    }
}
