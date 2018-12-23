namespace mmOrderMarking
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Enums;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    public static class VoidsClass
    {
        private const string LangItem = "mmAutoMarking";

        public static void ScheduleAutoNum(
            ExternalCommandData commandData,
            string prefix,
            string suffix,
            int startValue,
            OrderDirection orderDirection,
            string parameterName,
            LocationOrder locationOrder)
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
                            var elements = new FilteredElementCollector(doc, viewSchedule.Id)
                                .Where(e => e.LookupParameter(parameterName) != null)
                                .ToList();

                            sortElements = GetSortedElementsFromSchedule(viewSchedule, elements);
                        }
                    }
                    else
                    {
                        List<Element> pickedElements = uiDoc.Selection.
                            PickElementsByRectangle(Language.GetItem(LangItem, "m1")).ToList();
                        sortElements = locationOrder == LocationOrder.Creation
                            ? pickedElements.ToList()
                            : GetSortedElementsFromSelection(doc, pickedElements, locationOrder);
                    }

                    for (var i = 0; i < sortElements.Count; i++)
                    {
                        var element = sortElements[i];
                        if (element.LookupParameter(parameterName) is Parameter parameter)
                        {
                            var markValue = orderDirection == OrderDirection.Ascending
                                ? startValue + i
                                : sortElements.Count + startValue - i - 1;

                            parameter.Set(prefix + markValue + suffix);
                        }
                    }
                }
                if (TransactionStatus.Committed != transaction.Commit())
                {
                    transaction.RollBack();
                }
            }
        }

        public static void ScheduleAutoDel(
            ExternalCommandData commandData,
            string parameterName)
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
                                .Where(e => e.LookupParameter(parameterName) != null)
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
                        if (pickedElement.LookupParameter(parameterName) is Parameter parameter)
                        {
                            parameter.Set(string.Empty);
                        }
                    }
                }
                if (TransactionStatus.Committed != transaction.Commit())
                {
                    transaction.RollBack();
                }
            }
        }

        private static List<Element> GetSortedElementsFromSchedule(ViewSchedule viewSchedule, List<Element> elements)
        {
            List<Element> sortedElements = new List<Element>();

            using (SubTransaction transaction = new SubTransaction(viewSchedule.Document))
            {
                transaction.Start();

                // Разделитель
                var separator = "$ElementId=";

                // Записываем Id всех элементов в параметр "Комментарий"
                elements.ForEach(e =>
                {
                    var parameter = e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                    parameter.Set(parameter.AsString() + separator + e.Id.IntegerValue);
                });

                // К спецификации добавляем поле. Код из справки, за исключением try {} catch{}
                IList<SchedulableField> schedulableFields = viewSchedule.Definition.GetSchedulableFields();

                foreach (SchedulableField sf in schedulableFields)
                {
                    if (sf.FieldType != ScheduleFieldType.Instance)
                        continue;
                    if (sf.ParameterId.IntegerValue != (int)BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)
                        continue;

                    bool fieldAlreadyAdded = false;
                    //Get all schedule field ids
                    IList<ScheduleFieldId> ids = viewSchedule.Definition.GetFieldOrder();
                    foreach (ScheduleFieldId id in ids)
                    {
                        try
                        {
                            if (viewSchedule.Definition.GetField(id).GetSchedulableField() == sf)
                            {
                                fieldAlreadyAdded = true;
                                break;
                            }
                        }
                        catch
                        {
                            // Тут бывают какие-то ошибки, но мне они не важны, поэтому проще их "проглатить"
                        }
                    }

                    if (fieldAlreadyAdded == false)
                    {
                        viewSchedule.Definition.AddField(sf);
                    }
                }

                // Ну и сама магия - просто читаем получившуюся спецификацию по ячейкам и получаем
                // элементы уже в том порядке, в котором мы их видим в спецификации
                TableSectionData sectionData = viewSchedule.GetTableData().GetSectionData(SectionType.Body);
                for (int r = sectionData.FirstRowNumber; r <= sectionData.LastRowNumber; r++)
                {
                    for (int c = sectionData.FirstColumnNumber; c <= sectionData.LastColumnNumber; c++)
                    {
                        var cellValue = viewSchedule.GetCellText(SectionType.Body, r, c);
                        if (cellValue.Contains(separator))
                        {
                            var idStr = cellValue.Split(separator.ToCharArray()).Last();
                            if (!string.IsNullOrEmpty(idStr))
                            {
                                // Делаем устойчивым к ошибкам - при наличии ошибок все равно код завершится, 
                                // а я буду знать о возможных проблемах. На мой взгляд лучше, чем полное прерывание метода
                                try
                                {
                                    sortedElements.Add(viewSchedule.Document.GetElement(new ElementId(Convert.ToInt32(idStr))));
                                }
                                catch (Exception exception)
                                {
                                    ExceptionBox.Show(exception);
                                }
                            }
                        }
                    }
                }

                // Откатываем транзакцию
                transaction.RollBack();
            }

            return sortedElements;
        }

        private static List<Element> GetSortedElementsFromSelection(Document doc, List<Element> elements, LocationOrder locationOrder)
        {
            List<Element> sortedElements = new List<Element>();

            Dictionary<Element, XYZ> points = new Dictionary<Element, XYZ>();

            // get points
            foreach (Element element in elements)
            {
                var location = element.Location;
                if (location is LocationPoint locationPoint)
                    points.Add(element, locationPoint.Point);
                else if (location is LocationCurve locationCurve)
                {
                    points.Add(
                        element,
                        (locationCurve.Curve.GetEndPoint(0) + locationCurve.Curve.GetEndPoint(1)) / 2);
                }
            }

            List<Dictionary<Element, XYZ>> rows = new List<Dictionary<Element, XYZ>>();

            foreach (KeyValuePair<Element, XYZ> keyValuePair in points)
            {
                if (rows.Count == 0)
                {
                    var row = new Dictionary<Element, XYZ> { { keyValuePair.Key, keyValuePair.Value } };
                    rows.Add(row);
                }
                else
                {
                    var hasAllowableRow = false;
                    foreach (Dictionary<Element, XYZ> dictionary in rows)
                    {
                        if (hasAllowableRow) break;
                        foreach (XYZ xyz in dictionary.Values)
                        {
                            if (Math.Abs(xyz.Y - keyValuePair.Value.Y) < 0.0001)
                            {
                                dictionary.Add(keyValuePair.Key, keyValuePair.Value);
                                hasAllowableRow = true;
                                break;
                            }
                        }
                    }

                    if (!hasAllowableRow)
                    {
                        var row = new Dictionary<Element, XYZ> { { keyValuePair.Key, keyValuePair.Value } };
                        rows.Add(row);
                    }
                }
            }

            if (rows.Any())
            {
                rows.Sort((r1, r2) => r1.Values.First().Y.CompareTo(r2.Values.First().Y));
                if (locationOrder == LocationOrder.LeftToRightUpToDown ||
                    locationOrder == LocationOrder.RightToLeftUpToDown)
                    rows.Reverse();
                foreach (Dictionary<Element, XYZ> row in rows)
                {
                    if (locationOrder == LocationOrder.LeftToRightDownToUp ||
                        locationOrder == LocationOrder.LeftToRightUpToDown)
                        foreach (KeyValuePair<Element, XYZ> keyValuePair in row.OrderBy(r => r.Value.X))
                        {
                            sortedElements.Add(keyValuePair.Key);
                        }
                    else if (locationOrder == LocationOrder.RightToLeftDownToUp ||
                             locationOrder == LocationOrder.RightToLeftUpToDown)
                        foreach (KeyValuePair<Element, XYZ> keyValuePair in row.OrderByDescending(r => r.Value.X))
                        {
                            sortedElements.Add(keyValuePair.Key);
                        }
                }
            }

            return sortedElements;
        }
    }
}
