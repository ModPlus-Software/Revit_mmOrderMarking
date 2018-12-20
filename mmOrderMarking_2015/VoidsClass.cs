namespace mmOrderMarking
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using ModPlusAPI;

    public static class VoidsClass
    {
        private const string LangItem = "mmAutoMarking";

        public static void ScheduleAutoNum(
            ExternalCommandData commandData,
            string prefix,
            string suffix)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            using (Transaction transaction = new Transaction(doc))
            {
                if (transaction.Start("mmOrderMarking") == TransactionStatus.Started)
                {
                    View currentView = uiDoc.ActiveView;
                    List<Element> sortElements = new List<Element>();
                    if (currentView.ViewType == ViewType.Schedule)
                    {
                        if (commandData.View is ViewSchedule viewSchedule)
                        {
                            ScheduleDefinition definition = viewSchedule.Definition;
                            IList<ScheduleSortGroupField> sortFieldsList =
                                definition.GetSortGroupFields();
                            var sortFields = new List<ScheduleField>();
                            var sortOrders = new List<ScheduleSortOrder>();

                            foreach (var sortField in sortFieldsList)
                            {
                                sortOrders.Add(sortField.SortOrder);
                                sortFields.Add(definition.GetField(sortField.FieldId));
                            }
                            var elements = new FilteredElementCollector(doc, viewSchedule.Id)
                                .Where(e => e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK) != null)
                                .ToList();

                            sortElements = elements;

                            if (sortFields.Count != 0)
                            {
                                var reverseSortFields = sortFields.AsEnumerable().Reverse().ToList();

                                foreach (var sortField in reverseSortFields)
                                {
                                    var index = sortFields.IndexOf(sortField);

                                    sortElements =
                                        sortOrders[index] == ScheduleSortOrder.Ascending
                                            ? sortElements.OrderBy(e => GetParameters(e)
                                                .First(t => t.Definition.Name == sortField.ColumnHeading).AsDouble()).ToList()
                                            : sortElements.OrderByDescending(e => GetParameters(e)
                                                .First(t => t.Definition.Name == sortField.ColumnHeading).AsDouble()).ToList();
                                }
                            }
                        }
                    }
                    else
                    {
                        IList<Element> pickedElements = uiDoc.Selection.
                            PickElementsByRectangle(Language.GetItem(LangItem, "m1"));
                        sortElements = pickedElements.ToList();
                    }

                    for (var i = 0; i < sortElements.Count; i++)
                    {
                        var element = sortElements[i];
                        foreach (Parameter parameter in element.Parameters)
                        {
                            if (parameter.Id == new ElementId(BuiltInParameter.ALL_MODEL_MARK))
                            {
                                parameter.Set(prefix + Convert.ToString(i + 1) + suffix);
                            }
                        }
                    }
                }
                if (TransactionStatus.Committed != transaction.Commit())
                {
                    transaction.RollBack();
                }
            }
        }

        public static void ScheduleAutoDel(ExternalCommandData commandData)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;
            using (Transaction transaction = new Transaction(doc))
            {
                if (transaction.Start("mmOrderMarking") == TransactionStatus.Started)
                {
                    View currentView = uiDoc.ActiveView;
                    List<Element> listElements = new List<Element>();
                    if (currentView.ViewType == ViewType.Schedule)
                    {
                        if (commandData.View is ViewSchedule viewSchedule)
                        {
                            listElements = new FilteredElementCollector(doc, viewSchedule.Id)
                                .Where(e => e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK) != null)
                                .ToList();
                        }
                    }
                    else
                    {
                        IList<Element> selectedElements =
                            uiDoc.Selection.PickElementsByRectangle
                            (Language.GetItem(LangItem, "m1"));
                        listElements = selectedElements.ToList();
                    }

                    foreach (Element pickedElement in listElements)
                    {
                        foreach (Parameter parameter in pickedElement.Parameters)
                        {
                            if (parameter.Id == new ElementId(BuiltInParameter.ALL_MODEL_MARK))
                            {
                                parameter.Set(string.Empty);
                            }
                        }
                    }
                }
                if (TransactionStatus.Committed != transaction.Commit())
                {
                    transaction.RollBack();
                }
            }
        }

        private static List<Parameter> GetParameters(Element e)
        {
            // Getting all parameters of category
            var parameters = e.Parameters;
            List<Parameter> allParams = new List<Parameter>();
            foreach (Parameter p in parameters)
            {
                allParams.Add(p);
            }

            return allParams;
        }
    }
}
