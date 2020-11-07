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
        /// <inheritdoc />
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
#if !DEBUG
            Statistic.SendCommandStarting(new ModPlusConnector());
#endif

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

            var window = new WinScheduleAutoNum();
            var context = new MainViewModel(window, commandData.Application);
            window.DataContext = context;
            context.IsScheduleView = commandData.Application.ActiveUIDocument.Document.ActiveView is ViewSchedule;
            window.ContentRendered += (sender, args) => context.Init();
            window.ShowDialog();

            return Result.Succeeded;
        }
    }
}
