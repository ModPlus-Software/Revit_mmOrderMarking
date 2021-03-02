namespace mmOrderMarking
{
    using System.Linq;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Context;
    using ModPlus_Revit;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    /// <inheritdoc />
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        /// <summary>
        /// Revit event
        /// </summary>
        public static RevitEvent RevitEvent { get; private set; }
        
        /// <summary>
        /// Current <see cref="UIApplication"/>
        /// </summary>
        public static UIApplication UiApplication { get; private set; }
        
        /// <inheritdoc />
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
#if !DEBUG
            Statistic.SendCommandStarting(new ModPlusConnector());
#endif
            RevitEvent = new RevitEvent();
            UiApplication = commandData.Application;
            
            if (commandData.View is ViewSchedule)
            {
                var el = new FilteredElementCollector(commandData.View.Document, commandData.View.Id)
                    .WhereElementIsNotElementType();
                if (!el.Any())
                {
                    MessageBox.Show(Language.GetItem("m2"));
                    return Result.Cancelled;
                }

                var win = new View.InScheduleWindow();
                var context = new InScheduleContext(win, commandData.Application);
                win.DataContext = context;
                win.ContentRendered += (sender, args) => context.Initialize();
                win.ShowDialog();
            }
            else
            {
                var selection = commandData.Application.ActiveUIDocument.Selection;
                var doc = commandData.Application.ActiveUIDocument.Document;
                var preSelectedElements = selection.GetElementIds().Select(id => doc.GetElement(id)).ToList();
                var win = new View.InViewWindow();
                var context = new InViewContext(win, commandData.Application);
                win.DataContext = context;
                win.ContentRendered += (sender, args) => context.Initialize(preSelectedElements);
                win.Show();
            }

            return Result.Succeeded;
        }
    }
}
