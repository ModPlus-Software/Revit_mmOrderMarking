namespace mmOrderMarking.View
{
    /// <summary>
    /// Логика взаимодействия для InScheduleWindow.xaml
    /// </summary>
    public partial class InScheduleWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InScheduleWindow"/> class.
        /// </summary>
        public InScheduleWindow()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetFunctionLocalName(new ModPlusConnector());
        }
    }
}
