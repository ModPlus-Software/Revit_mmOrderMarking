namespace mmOrderMarking.Context
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Enums;
    using Models;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;
    using ModPlusAPI.Windows;
    using Services;

    /// <summary>
    /// Контекст работы в спецификации
    /// </summary>
    public class InScheduleContext : BaseContext
    {
        private readonly mmOrderMarking.View.InScheduleWindow _parentWindow;
        private readonly UIApplication _uiApplication;
        private readonly NumerateService _numerateService;
        
        public InScheduleContext(mmOrderMarking.View.InScheduleWindow parentWindow, UIApplication uiApplication)
        {
            _parentWindow = parentWindow;
            _uiApplication = uiApplication;
            _numerateService = new NumerateService(uiApplication);
        }

        /// <inheritdoc />
        public override ICommand NumerateCommand => new RelayCommandWithoutParameter(() =>
        {
            try
            {
                _parentWindow.Hide();

                _numerateService.NumerateInSchedule(new InScheduleNumerateData(
                    Parameter, StartValue, Prefix, Suffix, OrderDirection));
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            finally
            {
                _parentWindow.ShowDialog();
            }
        });

        /// <inheritdoc />
        public override ICommand ClearCommand => new RelayCommandWithoutParameter(() =>
        {
            try
            {
                _parentWindow.Hide();

                /*
                     * Внимание! Очистка параметра производится у всех элементов данной спецификации, независимо от
                     * настроек фильтрации. Это значит, что если, например, в спецификации представлено 10 колонн, а
                     * при настройках фильтрации отображается только 5 колонн, то очистка параметра произойдет у всех
                     * 10 колонн! Выполнить очистку параметра?
                     */
                if (MessageBox.ShowYesNo(Language.GetItem(LangItem, "h18")))
                    _numerateService.ClearInSchedule(Parameter);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            finally
            {
                _parentWindow.ShowDialog();
            }
        });

        /// <summary>
        /// Инициализация
        /// </summary>
        public void Initialize()
        {
            var instParamDescription = Language.GetItem(LangItem, "h12");
            var typeParamDescription = Language.GetItem(LangItem, "h13");
            var doc = _uiApplication.ActiveUIDocument.Document;

            if (doc.ActiveView is ViewSchedule viewSchedule)
            {
                var elements = new FilteredElementCollector(doc, doc.ActiveView.Id)
                    .WhereElementIsNotElementType().ToList();
                var element = elements.FirstOrDefault();
                if (element != null)
                {
                    var instanceParameters = element.Parameters.Cast<Parameter>().ToList();
                    var typeParameters = new List<Parameter>();

                    // Если снята галочка "Для каждого экземпляра", то добавляем параметры типа
                    if (!viewSchedule.Definition.IsItemized)
                    {
                        var t = doc.GetElement(element.GetTypeId());
                        if (t != null)
                        {
                            typeParameters.AddRange(t.Parameters.Cast<Parameter>());
                        }
                    }

                    foreach (var schedulableField in viewSchedule.Definition.GetSchedulableFields())
                    {
                        if (schedulableField.FieldType != ScheduleFieldType.Instance)
                            continue;

                        var parameter = instanceParameters.FirstOrDefault(p => p.Id == schedulableField.ParameterId);
                        if (ExtParameter.IsValid(parameter))
                        {
                            Parameters.Add(new ExtParameter(instParamDescription, parameter));
                        }

                        parameter = typeParameters.FirstOrDefault(p => p.Id == schedulableField.ParameterId);
                        if (ExtParameter.IsValid(parameter))
                        {
                            Parameters.Add(new ExtParameter(typeParamDescription, parameter));
                        }
                    }

                    var savedParameterName = UserConfigFile.GetValue(LangItem, nameof(Parameter));
                    var savedParameter = Parameters.FirstOrDefault(p => p.Name == savedParameterName);
                    if (savedParameter != null)
                    {
                        Parameter = savedParameter;
                    }
                    else
                    {
                        var markParameter = Parameters.FirstOrDefault(p =>
                            p.IsMatchBuiltInParameter(BuiltInParameter.ALL_MODEL_MARK));
                        Parameter = markParameter ?? Parameters.FirstOrDefault();
                    }
                }
            }
            else
            {
                throw new NotSupportedException("View is not Schedule View!");
            }

            Prefix = UserConfigFile.GetValue(LangItem, nameof(Prefix));
            Suffix = UserConfigFile.GetValue(LangItem, nameof(Suffix));

            StartValue = int.TryParse(UserConfigFile.GetValue(LangItem, nameof(StartValue)), out var i) ? i : 1;
            OrderDirection =
                Enum.TryParse(
                    UserConfigFile.GetValue(LangItem, nameof(OrderDirection)), out OrderDirection orderDirection)
                    ? orderDirection : OrderDirection.Ascending;
        }
    }
}
