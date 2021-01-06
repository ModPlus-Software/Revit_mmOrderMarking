namespace mmOrderMarking.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Events;
    using Autodesk.Revit.UI;
    using Enums;
    using JetBrains.Annotations;
    using Models;
    using ModPlusAPI;
    using ModPlusAPI.Enums;
    using ModPlusAPI.Services;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Сервис нумерации
    /// </summary>
    public class NumerateService
    {
        private const string LangItem = "mmOrderMarking";
        private readonly UIApplication _uiApplication;
        private readonly Document _doc;
        private readonly List<BuiltInParameter> _allowableBuiltInParameter = new List<BuiltInParameter>
        {
            BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS,
            BuiltInParameter.SHEET_NAME
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="NumerateService"/> class.
        /// </summary>
        /// <param name="uiApplication">Instance of <see cref="UIApplication"/></param>
        public NumerateService(UIApplication uiApplication)
        {
            _uiApplication = uiApplication;
            _doc = _uiApplication.ActiveUIDocument.Document;
        }

        /// <summary>
        /// Нумерация элементов в спецификации
        /// </summary>
        /// <param name="numerateData">Данные для нумерации</param>
        public void NumerateInSchedule(InScheduleNumerateData numerateData)
        {
            if (_doc.ActiveView is ViewSchedule viewSchedule)
            {
                var newNumbers = CollectElementsInSchedule(numerateData, viewSchedule, out var canByTypeParameter);
                if (newNumbers.Any())
                    ProceedNumeration(numerateData, newNumbers, canByTypeParameter);
            }
        }

        /// <summary>
        /// Нумерация элементов в модели
        /// </summary>
        /// <param name="selectionType">Тип выбора элементов, влияющий на порядок нумерации</param>
        /// <param name="numerateData">Данные для нумерации</param>
        /// <param name="elements">Элементы для нумерации</param>
        public void NumerateInView(
            ElementsSelectionType selectionType, InViewNumerateData numerateData, List<Element> elements)
        {
            var newNumbers = new Dictionary<Element, int>();
            List<Element> sortElements;
            OrderDirection orderDirection;
            
            if (selectionType == ElementsSelectionType.ByRectangle)
            {
                
                orderDirection = OrderDirection.Ascending;
                sortElements = numerateData.LocationOrder == LocationOrder.Creation
                    ? elements
                    : GetElementsSortedByLocation(elements, numerateData);
            }
            else
            {
                orderDirection = numerateData.OrderDirection;
                sortElements = elements;
            }
            
            for (var i = 0; i < sortElements.Count; i++)
            {
                var e = sortElements[i];
                var markValue = orderDirection == OrderDirection.Ascending
                    ? numerateData.StartValue + i
                    : sortElements.Count + numerateData.StartValue - i - 1;
                newNumbers.Add(e, markValue);
            }

            if (newNumbers.Any())
                ProceedNumeration(numerateData, newNumbers, true);
        }

        /// <summary>
        /// Удаление значения текстового параметра на виде, являющимся видом спецификации
        /// </summary>
        /// <param name="extParameter">Текстовый параметр</param>
        public void ClearInSchedule(ExtParameter extParameter)
        {
            if (extParameter.IsNumeric)
                return;

            var doc = _uiApplication.ActiveUIDocument.Document;
            if (doc.ActiveView is ViewSchedule viewSchedule)
            {
                var listElements = new FilteredElementCollector(doc, viewSchedule.Id)
                    .Where(e => e.LookupParameter(extParameter.Name) != null);

                ClearStringParameter(listElements, extParameter.Name);
            }
        }

        /// <summary>
        /// Удаление значения текстового параметра у элементов на виде, не являющимся спецификацией
        /// </summary>
        /// <param name="extParameter">Имя текстового параметра</param>
        /// <param name="elements">Элементы</param>
        public void ClearInView(ExtParameter extParameter, List<Element> elements)
        {
            ClearStringParameter(elements, extParameter.Name);
        }

        private void ClearStringParameter(IEnumerable<Element> listElements, string parameterName)
        {
            var uiDoc = _uiApplication.ActiveUIDocument;
            var doc = uiDoc.Document;
            var transactionName = Language.GetFunctionLocalName(new ModPlusConnector());
            if (string.IsNullOrEmpty(transactionName))
                transactionName = LangItem;
            using (var transaction = new Transaction(doc))
            {
                if (transaction.Start(transactionName) == TransactionStatus.Started)
                {
                    foreach (var pickedElement in listElements)
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

        private void ProceedNumeration(
            NumerateData numerateData, IDictionary<Element, int> elementsWithNewMarkNumber, bool canBeTypeParameter)
        {
            var uiDoc = _uiApplication.ActiveUIDocument;
            var doc = uiDoc.Document;

            try
            {
                _uiApplication.Application.FailuresProcessing += ApplicationOnFailuresProcessing;

                var resultService = new ResultService();

                var transactionName = Language.GetFunctionLocalName(new ModPlusConnector());
                if (string.IsNullOrEmpty(transactionName))
                    transactionName = LangItem;
                using (var transaction = new Transaction(doc))
                {
                    transaction.Start(transactionName);

                    var typesToSkip = new List<ElementId>();
                    foreach (var pair in elementsWithNewMarkNumber)
                    {
                        if (typesToSkip.Contains(pair.Key.GetTypeId()))
                            continue;
                        try
                        {
                            if (numerateData.GetParameter(pair.Key, canBeTypeParameter, out var isInstanceParameter) is Parameter parameter)
                            {
                                if (!isInstanceParameter)
                                    typesToSkip.Add(pair.Key.GetTypeId());

                                if (parameter.IsReadOnly)
                                {
                                    // Параметр "..." имеет свойство "Только для чтения" у следующих элементов
                                    resultService.Add(
                                        $"{Language.GetItem("h14")} \"{numerateData.Parameter.Name}\" {Language.GetItem("h15")}:",
                                        pair.Key.Id.IntegerValue.ToString(),
                                        ResultItemType.Info);
                                }
                                else
                                {
                                    SetParameterValue(pair.Key, parameter, numerateData, pair.Value, isInstanceParameter);
                                }
                            }
                        }
                        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                        {
                            // ignore
                        }
                        catch (Exception exception)
                        {
                            // При изменении значения параметров перечисленных элементов произошли ошибки
                            resultService.Add(
                                $"{Language.GetItem("h17")}: {exception.Message}",
                                pair.Key.Id.IntegerValue.ToString(),
                                ResultItemType.Error);
                        }
                    }

                    transaction.Commit();
                }
                
                resultService.ShowByType();

                _uiApplication.Application.FailuresProcessing -= ApplicationOnFailuresProcessing;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private IDictionary<Element, int> CollectElementsInSchedule(
            InScheduleNumerateData numerateData, ViewSchedule viewSchedule, out bool canBeTypeParameter)
        {
            var newNumbers = new Dictionary<Element, int>();

            // Если стоит галочка "Для каждого экземпляра", то получаем сортированный список и нумерация
            // происходит далее. Иначе получаем сортированный список по строкам и сразу нумеруем
            if (viewSchedule.Definition.IsItemized)
            {
                canBeTypeParameter = false;
                var elements = new FilteredElementCollector(_doc, viewSchedule.Id)
                    .Where(e => numerateData.GetParameter(e, false, out _) != null)
                    .ToList();

                List<Element> sortElements;
                using (var transaction = new Transaction(_doc))
                {
                    transaction.Start("Find in itemized table");
                    sortElements = GetSortedElementsFromItemizedSchedule(viewSchedule, elements);
                    transaction.RollBack();
                }

                for (var i = 0; i < sortElements.Count; i++)
                {
                    var e = sortElements[i];
                    var markValue = numerateData.OrderDirection == OrderDirection.Ascending
                        ? numerateData.StartValue + i
                        : sortElements.Count + numerateData.StartValue - i - 2;
                    newNumbers.Add(e, markValue);
                }
            }
            else
            {
                canBeTypeParameter = true;
                var elements = new FilteredElementCollector(_doc, viewSchedule.Id)
                    .Where(e => numerateData.GetParameter(e, true, out _) != null)
                    .ToList();

                List<ElementsInRow> sortedElementsByRows;
                using (var transaction = new Transaction(_doc))
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
                        foreach (var e in sortedElementsByRows[i].Elements)
                        {
                            var markValue = numerateData.OrderDirection == OrderDirection.Ascending
                                ? numerateData.StartValue + i
                                : sortedElementsByRows.Count + numerateData.StartValue - i - 1;
                            newNumbers.Add(e, markValue);
                        }
                    }
                }
            }

            return newNumbers;
        }

        private static void SetParameterValue(
            Element element, Parameter parameter, NumerateData numerateData, int markNumber, bool isInstanceParameter)
        {
            if (element.GroupId != ElementId.InvalidElementId &&
                parameter.Definition is InternalDefinition internalDefinition &&
                isInstanceParameter)
            {
                // Параметр в группе меняем без разгруппировки, если:
                // 1 - это стандартный параметр "Марка"
                // 2 - это общий параметр проекта с включенным свойством "Значения могут меняться по экземплярам групп"
                if (!internalDefinition.VariesAcrossGroups &&
                    internalDefinition.BuiltInParameter != BuiltInParameter.ALL_MODEL_MARK)
                {
                    // Невозможно изменить параметр экземпляра у элемента, расположенного в группе, если у параметра не
                    // включено свойство "Значения могут меняться по экземплярам групп" или это не системный параметр "Марка"
                    throw new ArgumentException(Language.GetItem(LangItem, "h16"));
                }
            }

            if (numerateData.Parameter.IsNumeric)
            {
                if (parameter.StorageType == StorageType.Integer)
                {
                    parameter.Set(markNumber);
                }
                else if (parameter.StorageType == StorageType.Double)
                {
#if R2017 || R2018 || R2019 || R2020
                    parameter.Set(UnitUtils.ConvertToInternalUnits(markNumber, parameter.DisplayUnitType));
#else
                    parameter.Set(UnitUtils.ConvertToInternalUnits(markNumber, parameter.GetUnitTypeId()));
#endif
                }
            }
            else
            {
                parameter.Set($"{numerateData.Prefix}{markNumber}{numerateData.Suffix}");
            }
        }

        private static void ApplicationOnFailuresProcessing(object sender, FailuresProcessingEventArgs e)
        {
            var failList = e.GetFailuresAccessor().GetFailureMessages();
            if (failList.Any())
            {
                foreach (var failureMessageAccessor in failList)
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

        /// <summary>
        /// Получение сортированных элементов из спецификации с установленной галочкой "Для каждого экземпляра"
        /// </summary>
        /// <param name="viewSchedule">Вид спецификации</param>
        /// <param name="elements">Элементы, полученные с этого вида</param>
        private List<Element> GetSortedElementsFromItemizedSchedule(ViewSchedule viewSchedule, List<Element> elements)
        {
            var sortedElements = new List<Element>();

            var builtInParameter = GetBuiltInParameterForElements(elements);
            if (builtInParameter == null)
                return sortedElements;

            using (var transaction = new SubTransaction(viewSchedule.Document))
            {
                transaction.Start();

                // Разделитель
                const string separator = "$ElementId=";

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
                var sectionData = viewSchedule.GetTableData().GetSectionData(SectionType.Body);
                for (var r = sectionData.FirstRowNumber; r <= sectionData.LastRowNumber; r++)
                {
                    for (var c = sectionData.FirstColumnNumber; c <= sectionData.LastColumnNumber; c++)
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
        private IEnumerable<ElementsInRow> GetSortedElementsFromNotItemizedSchedule(ViewSchedule viewSchedule, List<Element> elements)
        {
            var sortedElements = new List<ElementsInRow>();

            var builtInParameter = GetBuiltInParameterForElements(elements);
            if (builtInParameter == null)
                return sortedElements;

            // запоминаю начальные значения в параметре
            var cachedParameterValues = new Dictionary<Element, string>();
            elements.ForEach(e =>
            {
                var parameter = e.get_Parameter(builtInParameter.Value);
                cachedParameterValues.Add(e, parameter.AsString());
            });

            const string signalValue = "$Filled$";
            var columnNumber = -1;
            var startRowNumber = -1;

            bool fieldAlreadyAdded;

            using (var transaction = new SubTransaction(viewSchedule.Document))
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
                var sectionData = viewSchedule.GetTableData().GetSectionData(SectionType.Body);
                var rowNumber = 0;
                for (var r = sectionData.FirstRowNumber; r <= sectionData.LastRowNumber; r++)
                {
                    for (var c = sectionData.FirstColumnNumber; c <= sectionData.LastColumnNumber; c++)
                    {
                        var cellValue = viewSchedule.GetCellText(SectionType.Body, r, c);
                        if (cellValue.Contains(signalValue))
                        {
                            rowNumber++;
                            sortedElements.Add(new ElementsInRow(rowNumber));

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
                var element = elements[index];
                using (var transaction = new SubTransaction(viewSchedule.Document))
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
                var sectionData = viewSchedule.GetTableData().GetSectionData(SectionType.Body);
                var rowNumber = 0;
                for (var r = startRowNumber; r <= sectionData.LastRowNumber; r++)
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
            using (var transaction = new SubTransaction(viewSchedule.Document))
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
        private BuiltInParameter? GetBuiltInParameterForElements(IReadOnlyCollection<Element> elements)
        {
            BuiltInParameter? returnedBuiltInParameter = null;

            foreach (var builtInParameter in _allowableBuiltInParameter)
            {
                foreach (var element in elements)
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

        private static void AddFieldToSchedule(ViewSchedule viewSchedule, BuiltInParameter builtInParameter, out bool fieldAlreadyAdded)
        {
            var schedulableFields = viewSchedule.Definition.GetSchedulableFields();

            fieldAlreadyAdded = false;

            foreach (var sf in schedulableFields)
            {
                if (sf.FieldType != ScheduleFieldType.Instance)
                    continue;
                if (sf.ParameterId.IntegerValue != (int)builtInParameter)
                    continue;

                // Get all schedule field ids
                var ids = viewSchedule.Definition.GetFieldOrder();
                foreach (var id in ids)
                {
                    try
                    {
                        var scheduleField = viewSchedule.Definition.GetField(id);
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
            var schedulableFields = viewSchedule.Definition.GetSchedulableFields();

            foreach (var sf in schedulableFields)
            {
                if (sf.FieldType != ScheduleFieldType.Instance)
                    continue;
                if (sf.ParameterId.IntegerValue != (int)builtInParameter)
                    continue;

                // Get all schedule field ids
                var ids = viewSchedule.Definition.GetFieldOrder();
                foreach (var id in ids)
                {
                    try
                    {
                        var scheduleField = viewSchedule.Definition.GetField(id);
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

        private static List<Element> GetElementsSortedByLocation(
            IEnumerable<Element> elements, InViewNumerateData numerateData)
        {
            var sortedElements = new List<Element>();

            var points = new Dictionary<Element, XYZ>();

            // get points
            foreach (var element in elements)
            {
                var location = element.Location;
                if (location is LocationPoint locationPoint)
                {
                    points.Add(element, locationPoint.Point);
                }
                else if (location is LocationCurve locationCurve)
                {
                    points.Add(element, locationCurve.Curve.Evaluate(0.5, true));
                }
            }

            var rows = new List<Dictionary<Element, XYZ>>();

            foreach (var keyValuePair in points)
            {
                if (rows.Count == 0)
                {
                    var row = new Dictionary<Element, XYZ> { { keyValuePair.Key, keyValuePair.Value } };
                    rows.Add(row);
                }
                else
                {
                    var hasAllowableRow = false;
                    foreach (var dictionary in rows)
                    {
                        if (hasAllowableRow)
                            break;
                        foreach (var xyz in dictionary.Values)
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

                if (numerateData.LocationOrder == LocationOrder.LeftToRightUpToDown ||
                    numerateData.LocationOrder == LocationOrder.RightToLeftUpToDown)
                    rows.Reverse();
                foreach (var row in rows)
                {
                    if (numerateData.LocationOrder == LocationOrder.LeftToRightDownToUp ||
                        numerateData.LocationOrder == LocationOrder.LeftToRightUpToDown)
                    {
                        sortedElements.AddRange(row.OrderBy(r => r.Value.X).Select(keyValuePair => keyValuePair.Key));
                    }
                    else if (numerateData.LocationOrder == LocationOrder.RightToLeftDownToUp ||
                             numerateData.LocationOrder == LocationOrder.RightToLeftUpToDown)
                    {
                        sortedElements.AddRange(row.OrderByDescending(r => r.Value.X).Select(keyValuePair => keyValuePair.Key));
                    }
                }
            }

            return sortedElements;
        }
    }
}
