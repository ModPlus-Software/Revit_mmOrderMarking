namespace mmOrderMarking
{
    using System;
    using System.Linq;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        private WinScheduleAutoNum _window;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            ModPlusAPI.Statistic.SendCommandStarting(new Interface());

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
                _window.Closed += Window_Closed;
            }
            if (_window.IsLoaded)
                _window.Activate();
            else _window.ShowDialog();

            return Result.Succeeded;
        }

        // Closing window WPF
        private void Window_Closed(object sender, EventArgs e)
        {
            _window = null;
        }
    }
}
