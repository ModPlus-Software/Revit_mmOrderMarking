namespace mmOrderMarking
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Events;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;
    using Enums;
    using ModPlusAPI;
    using ModPlusAPI.Annotations;
    using ModPlusAPI.Windows;

    public class NumerateService
    {
        private readonly ExternalCommandData _commandData;
        private readonly string _prefix;
        private readonly string _suffix;
        private readonly int _startValue;
        private readonly OrderDirection _orderDirection;
        private readonly string _parameterName;
        private readonly LocationOrder _locationOrder;
        private readonly bool _numberingInGroups;
        private const string LangItem = "mmAutoMarking";

        public NumerateService(
            ExternalCommandData commandData,
            string prefix,
            string suffix,
            int startValue,
            OrderDirection orderDirection,
            string parameterName,
            LocationOrder locationOrder,
            bool numberingInGroups)
        {
            _commandData = commandData;
            _prefix = prefix;
            _suffix = suffix;
            _startValue = startValue;
            _orderDirection = orderDirection;
            _parameterName = parameterName;
            _locationOrder = locationOrder;
            _numberingInGroups = numberingInGroups;
        }

        public void ProceedNumeration()
        {
            UIApplication uiApp = _commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                List<ElNewMark> elNewMarks = new List<ElNewMark>();
                List<ElInGroup> elementsInGroups = new List<ElInGroup>();

                uiApp.Application.FailuresProcessing += ApplicationOnFailuresProcessing;

                if (_commandData.View is ViewSchedule viewSchedule)
                {
                    CollectElementsInSchedule(viewSchedule, elNewMarks, elementsInGroups);
                }
                else
                {
                    CollectElementsNotInSchedule(elNewMarks, elementsInGroups);
                }

                if (elNewMarks.Any())
                {
                    var transactionName = Language.GetFunctionLocalName(LangItem, new ModPlusConnector().LName);
                    if (string.IsNullOrEmpty(transactionName))
                        transactionName = LangItem;
                    using (Transaction transaction = new Transaction(doc))
                    {
                        transaction.Start(transactionName);
                        foreach (var e in elNewMarks)
                        {
                            if (e.Element.LookupParameter(_parameterName) is Parameter parameter)
                            {
                                parameter.Set(e.Mark);
                            }
                        }

                        transaction.Commit();
                    }
                }

                if (elementsInGroups.Any() && _numberingInGroups)
                    NumerateInGroups(elementsInGroups, doc);

                uiApp.Application.FailuresProcessing -= ApplicationOnFailuresProcessing;

            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        public static void ScheduleAutoDel(
            ExternalCommandData commandData,
            string parameterName)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;
            var transactionName = Language.GetFunctionLocalName(LangItem, new ModPlusConnector().LName);
            if (string.IsNullOrEmpty(transactionName))
                transactionName = LangItem;
            using (Transaction transaction = new Transaction(doc))
            {
                if (transaction.Start(transactionName) == TransactionStatus.Started)
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

        private void CollectElementsInSchedule(
            ViewSchedule viewSchedule,
            List<ElNewMark> elNewMarks,
            List<ElInGroup> elementsInGroups)
        {
            var doc = _commandData.Application.ActiveUIDocument.Document;

            var elements = new FilteredElementCollector(doc, viewSchedule.Id)
                .Where(e => e.LookupParameter(_parameterName) != null)
                .ToList();

            // Если стоит галочка "Для каждого экземпляра", то получаем сортированный список и нумерация
            // происходит далее. Иначе получаем сортированный список по строкам и сразу нумеруем
            if (viewSchedule.Definition.IsItemized)
            {
                List<Element> sortElements;
                using (Transaction transaction = new Transaction(doc))
                {
                    transaction.Start("Find in itemized table");
                    sortElements = GetSortedElementsFromItemizedSchedule(viewSchedule, elements);
                    transaction.RollBack();
                }

                for (var i = 0; i < sortElements.Count; i++)
                {
                    var e = sortElements[i];

                    ProceedElement(i, sortElements.Count, e, elNewMarks, elementsInGroups, doc);
                }
            }
            else
            {
                List<ElInRow> sortedElementsByRows;
                using (Transaction transaction = new Transaction(doc))
                {
                    transaction.Start("Find in rows");
                    sortedElementsByRows = GetSortedElementsFromNotItemizedSchedule(viewSchedule, elements)
                        .Where(e => e.Elements.Count > 0)
                        .ToList();
                    transaction.RollBack();
                }

                if (sortedElementsByRows.Any())
                {
                    for (var i = 0; i < sortedElementsByRows.Count; i++)
                    {
                        sortedElementsByRows[i].Elements.ForEach(e =>
                        {
                            ProceedElement(i, sortedElementsByRows.Count, e, elNewMarks, elementsInGroups, doc);
                        });
                    }
                }
            }
        }

        private void CollectElementsNotInSchedule(
            List<ElNewMark> elNewMarks,
            List<ElInGroup> elementsInGroups)
        {
            UIApplication uiApp = _commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;
            List<Element> sortElements = new List<Element>();
            var elementIds = uiDoc.Selection.GetElementIds();
            if (elementIds.Any())
            {
                List<Element> selectedElements = new List<Element>();
                foreach (ElementId elementId in elementIds)
                {
                    selectedElements.Add(doc.GetElement(elementId));
                }

                sortElements = _locationOrder == LocationOrder.Creation
                    ? selectedElements.ToList()
                    : GetSortedElementsFromSelection(selectedElements);
            }
            else
            {
                try
                {
                    var pickedElements =
                        uiDoc.Selection.PickObjects(ObjectType.Element, Language.GetItem(LangItem, "m1"))
                            .Select(r => doc.GetElement(r));
                    sortElements = _locationOrder == LocationOrder.Creation
                        ? pickedElements.ToList()
                        : GetSortedElementsFromSelection(pickedElements);
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    // ignore
                }
            }

            for (var i = 0; i < sortElements.Count; i++)
            {
                var e = sortElements[i];

                ProceedElement(i, sortElements.Count, e, elNewMarks, elementsInGroups, doc);
            }
        }

        private void ProceedElement(
            int i,
            int elementsCount,
            Element e,
            List<ElNewMark> elNewMarks,
            List<ElInGroup> elementsInGroups,
            Document doc)
        {
            var markValue = _orderDirection == OrderDirection.Ascending
                ? _startValue + i
                : elementsCount + _startValue - i;
            var newMark = _prefix + markValue + _suffix;

            if (e.GroupId != ElementId.InvalidElementId)
            {
                var parameter = e.LookupParameter(_parameterName);
                if (parameter != null)
                {
                    var map = doc.ParameterBindings;
                    var binding = map.get_Item(parameter.Definition);
                    InternalDefinition internalDefinition = (InternalDefinition)parameter.Definition;

                    // Параметр меняем без разгруппировки, если:
                    // 1 - это стандартный параметр "Марка"
                    // 2 - это общий параметр проекта с включенным свойством "Значения могут меняться по экземплярам групп"
                    if ((binding != null && internalDefinition.VariesAcrossGroups) ||
                        internalDefinition.BuiltInParameter == BuiltInParameter.ALL_MODEL_MARK)
                    {
                        elNewMarks.Add(new ElNewMark(e, newMark));
                    }
                    else
                    {
                        var elInGroup = elementsInGroups.FirstOrDefault(g => g.GroupId == e.GroupId.IntegerValue);
                        if (elInGroup == null)
                        {
                            var group = doc.GetElement(e.GroupId) as Group;
                            elInGroup = new ElInGroup(group);
                            elementsInGroups.Add(elInGroup);
                        }

                        elInGroup.Elements.Add(e, newMark);
                    }
                }
            }
            else
            {
                elNewMarks.Add(new ElNewMark(e, newMark));
            }
        }

        private void ApplicationOnFailuresProcessing(object sender, FailuresProcessingEventArgs e)
        {
            var failList = e.GetFailuresAccessor().GetFailureMessages();
            if (failList.Any())
            {
                foreach (FailureMessageAccessor failureMessageAccessor in failList)
                {
                    var failureDefinitionId = failureMessageAccessor.GetFailureDefinitionId();

                    // Пропускаю сообщения о дублированных значениях (Одинаковая марка)
                    if (failureDefinitionId == BuiltInFailures.GeneralFailures.DuplicateValue)
                    {
                        e.GetFailuresAccessor().DeleteWarning(failureMessageAccessor);
                    }

                    // Пропускаю сообщения про группы
                    ////if (failureDefinitionId == BuiltInFailures.GroupFailures.AtomViolationWhenOnePlaceInstance)
                    ////{
                    ////    e.GetFailuresAccessor().DeleteWarning(failureMessageAccessor);
                    ////}

                    ////if (failureDefinitionId == BuiltInFailures.GroupFailures.AtomViolationWhenMultiPlacedInstances)
                    ////{
                    ////    e.GetFailuresAccessor().DeleteWarning(failureMessageAccessor);
                    ////}
                }
            }
        }

        private void ApplicationOnFailuresProcessing_SkipAll(object sender, FailuresProcessingEventArgs e)
        {
            var failList = e.GetFailuresAccessor().GetFailureMessages();
            if (failList.Any())
            {
                e.GetFailuresAccessor().DeleteAllWarnings();
                e.SetProcessingResult(FailureProcessingResult.Continue);
            }
        }

        private void NumerateInGroups(List<ElInGroup> elementsInGroups, Document doc)
        {
            var transactionName = Language.GetFunctionLocalName(LangItem, new ModPlusConnector().LName);
            if (string.IsNullOrEmpty(transactionName))
                transactionName = LangItem;
            using (TransactionGroup transactionGroup = new TransactionGroup(doc))
            {
                transactionGroup.Start($"{transactionName}: Groups");

                foreach (IGrouping<string, ElInGroup> grouping in elementsInGroups.GroupBy(e => e.GroupName))
                {
                    List<List<ElementId>> elements = new List<List<ElementId>>();

                    // ungroup and delete
                    using (Transaction transaction = new Transaction(doc))
                    {
                        transaction.Start("Ungroup");

                        foreach (ElInGroup elInGroup in grouping)
                        {
                            var group = (Group) doc.GetElement(new ElementId(elInGroup.GroupId));
                            var grpElements = group.UngroupMembers().ToList();
                            elements.Add(grpElements);
                        }

                        transaction.Commit();

                        transaction.Start("Numerate");

                        foreach (ElInGroup elInGroup in grouping)
                        {
                            foreach (var pair in elInGroup.Elements)
                            {
                                if (pair.Key.LookupParameter(_parameterName) is Parameter parameter)
                                {
                                    parameter.Set(pair.Value);
                                }
                            }
                        }

                        transaction.Commit();

                        transaction.Start("Create new group");
                        var idsToDelete = new List<ElementId>();
                        GroupType createdGroup = null;
                        foreach (List<ElementId> elementIds in elements)
                        {
                            Group newGroup = doc.Create.NewGroup(elementIds);
                            if (createdGroup == null)
                            {
                                createdGroup = new FilteredElementCollector(doc)
                                    .OfClass(typeof(GroupType))
                                    .Cast<GroupType>()
                                    .FirstOrDefault(g => g.Name == grouping.Key);
                                if (createdGroup != null)
                                {
                                    idsToDelete.Add(newGroup.GroupType.Id);
                                    newGroup.GroupType = createdGroup;
                                }
                                else
                                {
                                    newGroup.GroupType.Name = grouping.Key;
                                }
                            }
                            else
                            {
                                idsToDelete.Add(newGroup.GroupType.Id);
                                newGroup.GroupType = createdGroup;
                            }
                        }

                        doc.Delete(idsToDelete);

                        transaction.Commit();
                    }
                }

                transactionGroup.Assimilate();
            }
        }

        /// <summary>
        /// Получение сортированных элементов из спецификации с установленной галочкой "Для каждого экземпляра"
        /// </summary>
        /// <param name="viewSchedule">Вид спецификации</param>
        /// <param name="elements">Элементы, полученные с этого вида</param>
        /// <returns></returns>
        private List<Element> GetSortedElementsFromItemizedSchedule(ViewSchedule viewSchedule, List<Element> elements)
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
                    if (e.GroupId != ElementId.InvalidElementId)
                        (e.Document.GetElement(e.GroupId) as Group)?.UngroupMembers();

                    var parameter = e.get_Parameter(builtInParameter.Value);
                    parameter.Set(parameter.AsString() + separator + e.Id.IntegerValue);
                });

                // К спецификации добавляем поле. Код из справки, за исключением try {} catch{}
                AddFieldToSchedule(viewSchedule, builtInParameter.Value, out _);

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

        /// <summary>
        /// Получение сортированных элементов из спецификации с не установленной галочкой "Для каждого экземпляра"
        /// </summary>
        /// <param name="viewSchedule">Вид спецификации</param>
        /// <param name="elements">Элементы, полученные с этого вида</param>
        /// <returns></returns>
        private List<ElInRow> GetSortedElementsFromNotItemizedSchedule(ViewSchedule viewSchedule, List<Element> elements)
        {
            List<ElInRow> sortedElements = new List<ElInRow>();

            var builtInParameter = GetBuiltInParameterForElements(elements);
            if (builtInParameter == null)
                return sortedElements;

            // запоминаю начальные значения в параметре
            Dictionary<Element, string> cachedParameterValues = new Dictionary<Element, string>();
            elements.ForEach(e =>
            {
                var parameter = e.get_Parameter(builtInParameter.Value);
                cachedParameterValues.Add(e, parameter.AsString());
            });

            var signalValue = "$Filled$";
            var columnNumber = -1;
            var startRowNumber = -1;

            bool fieldAlreadyAdded;

            using (SubTransaction transaction = new SubTransaction(viewSchedule.Document))
            {
                transaction.Start();

                // всем элементам записываем в комментарий сигнальное значение в виде двух специальных символов
                elements.ForEach(e =>
                {
                    if (e.GroupId != ElementId.InvalidElementId)
                        (e.Document.GetElement(e.GroupId) as Group)?.UngroupMembers();

                    var parameter = e.get_Parameter(builtInParameter.Value);
                    parameter.Set(signalValue);
                });

                // К спецификации добавляем поле. Код из справки, за исключением try {} catch{}
                AddFieldToSchedule(viewSchedule, builtInParameter.Value, out fieldAlreadyAdded);

                // в зависимости от количества строк в таблице сразу заполняю коллекцию "болванками" и
                // нахожу номер нужной колонки и первой строки
                TableSectionData sectionData = viewSchedule.GetTableData().GetSectionData(SectionType.Body);
                int rowNumber = 0;
                for (int r = sectionData.FirstRowNumber; r <= sectionData.LastRowNumber; r++)
                {
                    for (int c = sectionData.FirstColumnNumber; c <= sectionData.LastColumnNumber; c++)
                    {
                        var cellValue = viewSchedule.GetCellText(SectionType.Body, r, c);
                        if (cellValue.Contains(signalValue))
                        {
                            rowNumber++;
                            sortedElements.Add(new ElInRow(rowNumber));

                            if (startRowNumber == -1)
                                startRowNumber = r;
                            if (columnNumber == -1)
                                columnNumber = c;

                            break;
                        }
                    }
                }

                transaction.Commit();
            }

            // теперь выполняю итерацию по всем элементам
            for (var index = 0; index < elements.Count; index++)
            {
                Element element = elements[index];
                using (SubTransaction transaction = new SubTransaction(viewSchedule.Document))
                {
                    transaction.Start();

                    if (index != 0)
                    {
                        var parameter = elements[index - 1].get_Parameter(builtInParameter.Value);
                        // возвращаю элементу значение в параметр
                        parameter.Set(signalValue);
                    }

                    {
                        // элементу стираю второй символ. Первый символ нужен, чтобы идентифицировать ячейку
                        var parameter = element.get_Parameter(builtInParameter.Value);
                        parameter.Set(string.Empty);
                    }

                    // регенерирую таблицу, чтобы обновить представление
                    viewSchedule.RefreshData();
                    viewSchedule.Document.Regenerate();

                    transaction.Commit();
                }



                // теперь смотрю какая ячейка погасла
                TableSectionData sectionData = viewSchedule.GetTableData().GetSectionData(SectionType.Body);
                var rowNumber = 0;
                for (int r = startRowNumber; r <= sectionData.LastRowNumber; r++)
                {
                    rowNumber++;
                    var cellValue = viewSchedule.GetCellText(SectionType.Body, r, columnNumber);
                    if (string.IsNullOrEmpty(cellValue))
                    {
                        var elInRow = sortedElements.FirstOrDefault(e => e.RowNumber == rowNumber);
                        elInRow?.Elements.Add(element);

                        break;
                    }
                }
            }

            // восстанавливаю начальные значения параметров
            using (SubTransaction transaction = new SubTransaction(viewSchedule.Document))
            {
                transaction.Start();

                foreach (var pair in cachedParameterValues)
                {
                    var parameter = pair.Key.get_Parameter(builtInParameter.Value);
                    parameter.Set(pair.Value);
                }

                // если поле не было добавлено изначально, то нужно его удалить
                if (!fieldAlreadyAdded)
                    RemoveFieldFromSchedule(viewSchedule, builtInParameter.Value);

                transaction.Commit();
            }

            return sortedElements;
        }

        [CanBeNull]
        private BuiltInParameter? GetBuiltInParameterForElements(List<Element> elements)
        {
            BuiltInParameter? returnedBuiltInParameter = null;

            foreach (BuiltInParameter builtInParameter in _allowableBuiltInParameter)
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

                if (returnedBuiltInParameter != null)
                    break;
            }

            return returnedBuiltInParameter;
        }

        private readonly List<BuiltInParameter> _allowableBuiltInParameter = new List<BuiltInParameter>
        {
            BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS,
            BuiltInParameter.SHEET_NAME
        };

        private static void AddFieldToSchedule(ViewSchedule viewSchedule, BuiltInParameter builtInParameter, out bool fieldAlreadyAdded)
        {
            IList<SchedulableField> schedulableFields = viewSchedule.Definition.GetSchedulableFields();

            fieldAlreadyAdded = false;

            foreach (SchedulableField sf in schedulableFields)
            {
                if (sf.FieldType != ScheduleFieldType.Instance)
                    continue;
                if (sf.ParameterId.IntegerValue != (int)builtInParameter)
                    continue;

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
                        // Тут бывают какие-то ошибки, но мне они не важны, поэтому проще их "проглотить"
                    }
                }

                if (fieldAlreadyAdded == false)
                {
                    viewSchedule.Definition.AddField(sf);
                }
            }

            viewSchedule.RefreshData();
            viewSchedule.Document.Regenerate();
        }

        private static void RemoveFieldFromSchedule(ViewSchedule viewSchedule, BuiltInParameter builtInParameter)
        {
            IList<SchedulableField> schedulableFields = viewSchedule.Definition.GetSchedulableFields();

            foreach (SchedulableField sf in schedulableFields)
            {
                if (sf.FieldType != ScheduleFieldType.Instance)
                    continue;
                if (sf.ParameterId.IntegerValue != (int)builtInParameter)
                    continue;

                //Get all schedule field ids
                IList<ScheduleFieldId> ids = viewSchedule.Definition.GetFieldOrder();
                foreach (ScheduleFieldId id in ids)
                {
                    try
                    {
                        ScheduleField scheduleField = viewSchedule.Definition.GetField(id);
                        if (scheduleField.GetSchedulableField() == sf)
                        {
                            viewSchedule.Definition.RemoveField(scheduleField.FieldId);
                            break;
                        }
                    }
                    catch
                    {
                        // Тут бывают какие-то ошибки, но мне они не важны, поэтому проще их "проглотить"
                    }
                }
            }

            viewSchedule.RefreshData();
            viewSchedule.Document.Regenerate();
        }

        private List<Element> GetSortedElementsFromSelection(IEnumerable<Element> elements)
        {
            var sortedElements = new List<Element>();

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
                if (_locationOrder == LocationOrder.LeftToRightUpToDown ||
                    _locationOrder == LocationOrder.RightToLeftUpToDown)
                    rows.Reverse();
                foreach (Dictionary<Element, XYZ> row in rows)
                {
                    if (_locationOrder == LocationOrder.LeftToRightDownToUp ||
                        _locationOrder == LocationOrder.LeftToRightUpToDown)
                        foreach (KeyValuePair<Element, XYZ> keyValuePair in row.OrderBy(r => r.Value.X))
                        {
                            sortedElements.Add(keyValuePair.Key);
                        }
                    else if (_locationOrder == LocationOrder.RightToLeftDownToUp ||
                             _locationOrder == LocationOrder.RightToLeftUpToDown)
                        foreach (KeyValuePair<Element, XYZ> keyValuePair in row.OrderByDescending(r => r.Value.X))
                        {
                            sortedElements.Add(keyValuePair.Key);
                        }
                }
            }

            return sortedElements;
        }

        /// <summary>
        /// Объект, содержащий информацию о том, какие элементе в какой строке находятся
        /// </summary>
        internal class ElInRow
        {
            public ElInRow(int rowNumber)
            {
                RowNumber = rowNumber;
            }

            public int RowNumber { get; }

            public List<Element> Elements { get; } = new List<Element>();
        }

        internal class ElInGroup
        {
            public ElInGroup(Group group)
            {
                GroupId = group.Id.IntegerValue;
                GroupName = group.Name;
            }

            public int GroupId { get; }

            public string GroupName { get; set; }

            public Dictionary<Element, string> Elements { get; } = new Dictionary<Element, string>();
        }

        internal class ElNewMark
        {
            public ElNewMark(Element element, string mark)
            {
                Element = element;
                Mark = mark;
            }

            public Element Element { get; }

            public string Mark { get; }
        }
    }
}
