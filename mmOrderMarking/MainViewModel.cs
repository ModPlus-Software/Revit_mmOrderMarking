namespace mmOrderMarking
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Input;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Enums;
    using Models;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Главный контекст плагина
    /// </summary>
    public class MainViewModel : VmBase
    {
        private const string LangItem = "mmOrderMarking";
        private readonly WinScheduleAutoNum _parentWindow;
        private readonly UIApplication _uiApplication;
        private readonly NumerateService _numerateService;
        private string _prefix;
        private string _suffix;
        private bool _isEnabledPrefixAndSuffix = true;
        private int _startValue;
        private OrderDirection _orderDirection = OrderDirection.Ascending;
        private LocationOrder _locationOrder = LocationOrder.Creation;
        private ExtParameter _scheduleParameter;
        private string _parameterName;
        private bool _isScheduleView;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="parentWindow">Parent window</param>
        /// <param name="uiApplication">Instance of <see cref="UIApplication"/></param>
        public MainViewModel(WinScheduleAutoNum parentWindow, UIApplication uiApplication)
        {
            _parentWindow = parentWindow;
            _uiApplication = uiApplication;
            _numerateService = new NumerateService(uiApplication);
            ScheduleParameters = new ObservableCollection<ExtParameter>();
        }

        /// <summary>
        /// Является ли текущий вид спецификацией
        /// </summary>
        public bool IsScheduleView
        {
            get => _isScheduleView;
            set
            {
                if (_isScheduleView == value)
                    return;
                _isScheduleView = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Префикс марки
        /// </summary>
        public string Prefix
        {
            get => _prefix;
            set
            {
                if (_prefix == value)
                    return;
                _prefix = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(Prefix), value, true);
            }
        }

        /// <summary>
        /// Суффикс марки
        /// </summary>
        public string Suffix
        {
            get => _suffix;
            set
            {
                if (_suffix == value)
                    return;
                _suffix = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(Suffix), value, true);
            }
        }

        /// <summary>
        /// Доступность указания префикса и суффикса
        /// </summary>
        public bool IsEnabledPrefixAndSuffix
        {
            get => _isEnabledPrefixAndSuffix;
            set
            {
                if (_isEnabledPrefixAndSuffix == value)
                    return;
                _isEnabledPrefixAndSuffix = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Начальное значение нумерации
        /// </summary>
        public int StartValue
        {
            get => _startValue;
            set
            {
                if (_startValue == value)
                    return;
                _startValue = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(StartValue), value.ToString(), true);
            }
        }

        /// <summary>
        /// Направление нумерации 
        /// </summary>
        public OrderDirection OrderDirection
        {
            get => _orderDirection;
            set
            {
                if (_orderDirection == value)
                    return;
                _orderDirection = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(OrderDirection), value.ToString(), false);
            }
        }

        /// <summary>
        /// Вариант направления нумерации по положению элемента
        /// </summary>
        public LocationOrder LocationOrder
        {
            get => _locationOrder;
            set
            {
                if (_locationOrder == value)
                    return;
                _locationOrder = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(LocationOrder), value.ToString(), true);
            }
        }

        /// <summary>
        /// Параметры для спецификации
        /// </summary>
        public ObservableCollection<ExtParameter> ScheduleParameters { get; }

        /// <summary>
        /// Параметр для нумерации в спецификации
        /// </summary>
        public ExtParameter ScheduleParameter
        {
            get => _scheduleParameter;
            set
            {
                if (_scheduleParameter == value)
                    return;
                _scheduleParameter = value;
                IsEnabledPrefixAndSuffix = !value.IsNumeric;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(ScheduleParameter), value.Name, true);
            }
        }

        /// <summary>
        /// Имя параметра для нумерации элементов по положению
        /// </summary>
        public string ParameterName
        {
            get => _parameterName;
            set
            {
                if (_parameterName == value)
                    return;
                _parameterName = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(ParameterName), value, true);
            }
        }

        /// <summary>
        /// Команда "Выполнить нумерацию"
        /// </summary>
        public ICommand NumerateCommand => new RelayCommandWithoutParameter(() =>
        {
            try
            {
                _parentWindow.Hide();

                if (IsScheduleView)
                {
                    _numerateService.NumerateInSchedule(new NumerateData(
                        ScheduleParameter.Name, StartValue, Prefix, Suffix, 
                        OrderDirection, LocationOrder, ScheduleParameter.IsNumeric));
                }
                else
                {
                    _numerateService.NumerateInView(new NumerateData(
                        ParameterName, StartValue, Prefix, Suffix, OrderDirection.Ascending, LocationOrder, false));
                }
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
        /// Команда "Очистить"
        /// </summary>
        public ICommand ClearCommand => new RelayCommandWithoutParameter(() =>
        {
            try
            {
                _parentWindow.Hide();

                if (IsScheduleView)
                {
                    /*
                     * Внимание! Очистка параметра производится у всех элементов данной спецификации, независимо от
                     * настроек фильтрации. Это значит, что если, например, в спецификации представлено 10 колонн, а
                     * при настройках фильтрации отображается только 5 колонн, то очистка параметра произойдет у всех
                     * 10 колонн! Выполнить очистку параметра?
                     */
                    if (MessageBox.ShowYesNo(Language.GetItem(LangItem, "h18")))
                        _numerateService.ClearInSchedule(ScheduleParameter);
                }
                else
                {
                    _numerateService.ClearInView(ParameterName);
                }
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
        /// Инициализация данных для работы
        /// </summary>
        public void Init()
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
                            ScheduleParameters.Add(new ExtParameter(instParamDescription, parameter));
                        }

                        parameter = typeParameters.FirstOrDefault(p => p.Id == schedulableField.ParameterId);
                        if (ExtParameter.IsValid(parameter))
                        {
                            ScheduleParameters.Add(new ExtParameter(typeParamDescription, parameter));
                        }
                    }

                    var savedParameterName = UserConfigFile.GetValue(LangItem, nameof(ScheduleParameter));
                    var savedParameter = ScheduleParameters.FirstOrDefault(p => p.Name == savedParameterName);
                    if (savedParameter != null)
                    {
                        ScheduleParameter = savedParameter;
                    }
                    else
                    {
                        var markParameter = ScheduleParameters.FirstOrDefault(p =>
                            p.IsMatchBuiltInParameter(BuiltInParameter.ALL_MODEL_MARK));
                        ScheduleParameter = markParameter ?? ScheduleParameters.FirstOrDefault();
                    }
                }
            }
            else
            {
                var savedParameterName = UserConfigFile.GetValue(LangItem, nameof(ParameterName));
                ParameterName = !string.IsNullOrWhiteSpace(savedParameterName)
                    ? savedParameterName : LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_MARK);
            }

            Prefix = UserConfigFile.GetValue(LangItem, nameof(Prefix));
            Suffix = UserConfigFile.GetValue(LangItem, nameof(Suffix));

            StartValue = int.TryParse(UserConfigFile.GetValue(LangItem, nameof(StartValue)), out var i) ? i : 1;
            OrderDirection =
                Enum.TryParse(
                    UserConfigFile.GetValue(LangItem, nameof(OrderDirection)), out OrderDirection orderDirection)
                    ? orderDirection : OrderDirection.Ascending;
            LocationOrder =
                Enum.TryParse(UserConfigFile.GetValue(LangItem, nameof(LocationOrder)), out LocationOrder locationOrder)
                    ? locationOrder : LocationOrder.Creation;
        }
    }
}
