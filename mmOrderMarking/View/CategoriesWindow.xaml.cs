namespace mmOrderMarking.View
{
    using System.Windows;

    /// <summary>
    /// Логика взаимодействия для CategoriesWindow.xaml
    /// </summary>
    public partial class CategoriesWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CategoriesWindow"/> class.
        /// </summary>
        public CategoriesWindow()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem("h19");
        }

        private void BtAccept_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
