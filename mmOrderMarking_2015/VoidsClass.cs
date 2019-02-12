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
    using ModPlusAPI.Annotations;
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

                            ////if (viewSchedule.Definition.IsItemized)
                                sortElements = GetSortedElementsFromItemizedSchedule(viewSchedule, elements);
                            ////else
                            ////    sortElements = GetSortedElementsFromNotItemizedSchedule(viewSchedule, elements);
                        }
                    }
                    else
                    {
                        var elementIds = uiDoc.Selection.GetElementIds();
                        if (elementIds.Any())
                        {
                            List<Element> selectedElements = new List<Element>();
                            foreach (ElementId elementId in elementIds)
                            {
                                selectedElements.Add(doc.GetElement(elementId));
                            }
                            sortElements = locationOrder == LocationOrder.Creation
                                ? selectedElements.ToList()
                                : GetSortedElementsFromSelection(doc, selectedElements, locationOrder);
                        }
                        else
                        {
                            try
                            {
                                List<Element> pickedElements =
                                    uiDoc.Selection.PickElementsByRectangle(Language.GetItem(LangItem, "m1")).ToList();
                                sortElements = locationOrder == LocationOrder.Creation
                                    ? pickedElements.ToList()
                                    : GetSortedElementsFromSelection(doc, pickedElements, locationOrder);
                            }
                            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                            {
                                // ignore
                            }
                        }
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

                transaction.Commit();
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
                        var elementIds = uiDoc.Selection.GetElementIds();
                        if (elementIds.Any())
                        {
                            foreach (ElementId elementId in elementIds)
                            {
                                listElements.Add(doc.GetElement(elementId));
                            }
                        }
                        else
                        {
                            try
                            {
                                IList<Element> selectedElements =
                                    uiDoc.Selection.PickElementsByRectangle(Language.GetItem(LangItem, "m1"));
                                listElements = selectedElements.ToList();
                            }
                            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                            {
                                // ignore
                            }
                        }
                    }

                    foreach (Element pickedElement in listElements)
                    {
                        if (pickedElement.LookupParameter(parameterName) is Parameter parameter)
                        {
                            parameter.Set(string.Empty);
                        }
                    }
                }

                transaction.Commit();
            }
        }

        /// <summary>
        /// Получение сортированных элементов из спецификации с установленной галочкой "Для каждого экземпляра"
        /// </summary>
        /// <param name="viewSchedule">Вид спецификации</param>
        /// <param name="elements">Элементы, полученные с этого вида</param>
        /// <returns></returns>
        private static List<Element> GetSortedElementsFromItemizedSchedule(ViewSchedule viewSchedule, List<Element> elements)
        {
            List<Element> sortedElements = new List<Element>();

            var builtInParameter = GetBuiltInParameterForElements(elements);
            if (builtInParameter == null)
                return sortedElements;

            using (SubTransaction transaction = new SubTransaction(viewSchedule.Document))
            {
                transaction.Start();

                // Разделитель
                var separator = "$ElementId=";

                // Записываем Id всех элементов в параметр "Комментарий"
                elements.ForEach(e =>
                {
                    var parameter = e.get_Parameter(builtInParameter.Value);
                    parameter.Set(parameter.AsString() + separator + e.Id.IntegerValue);
                });

                // К спецификации добавляем поле. Код из справки, за исключением try {} catch{}
                IList<SchedulableField> schedulableFields = viewSchedule.Definition.GetSchedulableFields();

                foreach (SchedulableField sf in schedulableFields)
                {
                    if (sf.FieldType != ScheduleFieldType.Instance)
                        continue;
                    if (sf.ParameterId.IntegerValue != (int)builtInParameter.Value)
                        continue;

                    bool fieldAlreadyAdded = false;
                    //Get all schedule field ids
                    IList<ScheduleFieldId> ids = viewSchedule.Definition.GetFieldOrder();
                    foreach (ScheduleFieldId id in ids)
                    {
                        try
                        {
                            ScheduleField scheduleField = viewSchedule.Definition.GetField(id);
                            if (scheduleField.GetSchedulableField() == sf)
                            {
                                fieldAlreadyAdded = true;
                                scheduleField.IsHidden = false;
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

                viewSchedule.RefreshData();
                viewSchedule.Document.Regenerate();

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

        [CanBeNull]
        private static BuiltInParameter? GetBuiltInParameterForElements(List<Element> elements)
        {
            BuiltInParameter? returnedBuiltInParameter = null;

            foreach (BuiltInParameter builtInParameter in AllowableBuiltInParameter)
            {
                foreach (Element element in elements)
                {
                    if (element.get_Parameter(builtInParameter) == null)
                    {
                        returnedBuiltInParameter = null;
                        break;
                    }

                    returnedBuiltInParameter = builtInParameter;
                }
            }

            return returnedBuiltInParameter;
        }

        private static List<BuiltInParameter> AllowableBuiltInParameter = new List<BuiltInParameter>
        {
            BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS,
            BuiltInParameter.SHEET_NAME
        };

        /// <summary>
        /// Получение сортированных элементов из спецификации с не установленной галочкой "Для каждого экземпляра"
        /// </summary>
        /// <param name="viewSchedule">Вид спецификации</param>
        /// <param name="elements">Элементы, полученные с этого вида</param>
        /// <returns></returns>
        private static List<Element> GetSortedElementsFromNotItemizedSchedule(ViewSchedule viewSchedule, List<Element> elements)
        {
            List<Element> sortedElements = new List<Element>();

            using (SubTransaction transaction = new SubTransaction(viewSchedule.Document))
            {
                transaction.Start();

                // Разделитель
                var separator = "$HelpIntegerValue=";

                // Сначала мне нужно добавить (или проверить) поле "Комментарии" и запомнить заголовок
                // К спецификации добавляем поле. Код из справки, за исключением try {} catch{}
                IList<SchedulableField> schedulableFields = viewSchedule.Definition.GetSchedulableFields();

                var columnHeader = string.Empty;

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
                            ScheduleField scheduleField = viewSchedule.Definition.GetField(id);
                            if (scheduleField.GetSchedulableField() == sf)
                            {
                                fieldAlreadyAdded = true;
                                scheduleField.IsHidden = false;
                                if (string.IsNullOrEmpty(scheduleField.ColumnHeading))
                                    scheduleField.ColumnHeading = "CommentsColumn";
                                columnHeader = scheduleField.ColumnHeading;
                                break;
                            }
                        }
                        catch
                        {
                            // Тут бывают какие-то ошибки, но мне они не важны, поэтому проще их "проглотить"
                        }
                    }

                    if (fieldAlreadyAdded == false)
                    {
                        var addedField = viewSchedule.Definition.AddField(sf);
                        columnHeader = addedField.ColumnHeading;
                    }
                }

                viewSchedule.RefreshData();
                viewSchedule.Document.Regenerate();


                if (!string.IsNullOrEmpty(columnHeader))
                {
                    TableSectionData sectionData = viewSchedule.GetTableData().GetSectionData(SectionType.Body);
                    var column = -1;
                    var helpInteger = 1;
                    for (int r = sectionData.FirstRowNumber; r <= sectionData.LastRowNumber; r++)
                    {
                        if (column == -1)
                        {
                            for (int c = sectionData.FirstColumnNumber; c <= sectionData.LastColumnNumber; c++)
                            {
                                var cellValue = viewSchedule.GetCellText(SectionType.Body, r, c);
                                if (cellValue == columnHeader)
                                {
                                    column = c;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            var cellValue = viewSchedule.GetCellText(SectionType.Body, r, column);
                            Debug.Print("Cell value: " + cellValue);
                            TableCellCalculatedValueData calculatedValue = sectionData.GetCellCalculatedValue(r, column);
                            Debug.Print("Calculated name: " + calculatedValue?.GetName()); 
                            
                            
                            //sectionData.SetCellText(r, column, cellValue + separator + helpInteger);
                            helpInteger++;
                        }
                    }
                }

                // Откатываю транзакцию
                transaction.Commit();
                //transaction.RollBack();
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
