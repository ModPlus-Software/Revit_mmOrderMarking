namespace mmOrderMarking.View
{
    /// <summary>
    /// Логика взаимодействия для InViewWindow.xaml
    /// </summary>
    public partial class InViewWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InViewWindow"/> class.
        /// </summary>
        public InViewWindow()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetFunctionLocalName(new ModPlusConnector());
        }
    }
}
