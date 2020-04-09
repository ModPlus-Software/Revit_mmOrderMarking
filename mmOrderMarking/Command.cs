namespace mmOrderMarking
{
    using System.Linq;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    /// <inheritdoc />
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        private WinScheduleAutoNum _window;

        /// <inheritdoc />
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Statistic.SendCommandStarting(new ModPlusConnector());

            if (commandData.View is ViewSchedule)
            {
                var el = new FilteredElementCollector(commandData.View.Document, commandData.View.Id)
                    .WhereElementIsNotElementType();
                if (!el.Any())
                {
                    MessageBox.Show(Language.GetItem("mmOrderMarking", "m2"));
                    return Result.Cancelled;
                }
            }

            // Working with window WPF
            if (_window == null)
            {
                _window = new WinScheduleAutoNum(commandData);
                _window.Closed += (sender, args) => _window = null;
            }

            if (_window.IsLoaded)
                _window.Activate();
            else 
                _window.ShowDialog();

            return Result.Succeeded;
        }
    }
}
