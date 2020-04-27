namespace mmOrderMarking
{
    public partial class WinScheduleAutoNum
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WinScheduleAutoNum"/> class.
        /// </summary>
        public WinScheduleAutoNum()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetFunctionLocalName(new ModPlusConnector());
        }
    }
}