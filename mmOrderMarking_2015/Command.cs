namespace mmOrderMarking
{
    using System;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        private WinScheduleAutoNum _window;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            ModPlusAPI.Statistic.SendCommandStarting(new Interface());

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
