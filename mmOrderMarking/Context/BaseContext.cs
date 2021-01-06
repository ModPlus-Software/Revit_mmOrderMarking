namespace mmOrderMarking.Context
{
    using System.Collections.ObjectModel;
    using System.Windows.Input;
    using Enums;
    using Models;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;

    /// <summary>
    /// Базовый контекст
    /// </summary>
    public abstract class BaseContext : VmBase
    {
        private string _prefix;
        private string _suffix;
        private bool _isEnabledPrefixAndSuffix = true;
        private int _startValue;
        private OrderDirection _orderDirection = OrderDirection.Ascending;
        private ExtParameter _parameter;
        
        /// <summary>
        /// Ключ локализации (имя плагина)
        /// </summary>
        public const string LangItem = "mmOrderMarking";

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseContext"/> class.
        /// </summary>
        protected BaseContext()
        {
            Parameters = new ObservableCollection<ExtParameter>();
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
        /// Параметры
        /// </summary>
        public ObservableCollection<ExtParameter> Parameters { get; }

        /// <summary>
        /// Параметр для нумерации
        /// </summary>
        public ExtParameter Parameter
        {
            get => _parameter;
            set
            {
                if (_parameter == value)
                    return;
                _parameter = value;
                IsEnabledPrefixAndSuffix = !value.IsNumeric;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(Parameter), value.Name, true);
            }
        }
        
        /// <summary>
        /// Выполнить нумерацию
        /// </summary>
        public abstract ICommand NumerateCommand { get; }
        
        /// <summary>
        /// Очистить нумерацию
        /// </summary>
        public abstract ICommand ClearCommand { get; }
    }
}
