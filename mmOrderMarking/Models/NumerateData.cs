namespace mmOrderMarking.Models
{
    using Autodesk.Revit.DB;
    using Enums;
    using JetBrains.Annotations;

    /// <summary>
    /// Данные для нумерации
    /// </summary>
    public abstract class NumerateData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumerateData"/> class.
        /// </summary>
        /// <param name="parameter">Параметр для нумерации</param>
        /// <param name="startValue">Начальное числовое значение</param>
        /// <param name="prefix">Префикс</param>
        /// <param name="suffix">Суффикс</param>
        /// <param name="orderDirection">Направление нумерации (по возрастанию или убыванию)</param>
        protected NumerateData(
            ExtParameter parameter,
            int startValue,
            string prefix,
            string suffix,
            OrderDirection orderDirection)
        {
            Parameter = parameter;
            StartValue = startValue;
            Prefix = prefix;
            Suffix = suffix;
            OrderDirection = orderDirection;
        }

        /// <summary>
        /// Имя параметра
        /// </summary>
        public ExtParameter Parameter { get; }

        /// <summary>
        /// Начальное числовое значение
        /// </summary>
        public int StartValue { get; }

        /// <summary>
        /// Префикс
        /// </summary>
        public string Prefix { get; }

        /// <summary>
        /// Суффикс
        /// </summary>
        public string Suffix { get; }
        
        /// <summary>
        /// Направление нумерации (по возрастанию или убыванию)
        /// </summary>
        public OrderDirection OrderDirection { get; }
        
        /// <summary>
        /// Взять параметр из элемента
        /// </summary>
        /// <param name="element">Элемент Revit</param>
        /// <param name="getFromType">Искать ли в типоразмере</param>
        /// <param name="isInstanceParameter">Является ли параметр параметром типа</param>
        [CanBeNull]
        public Parameter GetParameter(Element element, bool getFromType, out bool isInstanceParameter)
        {
            var parameter = element.LookupParameter(Parameter.Name);
            isInstanceParameter = true;
            
            if (parameter == null && getFromType)
            {
                var elementType = element.Document.GetElement(element.GetTypeId());
                if (elementType?.LookupParameter(Parameter.Name) is Parameter p)
                {
                    isInstanceParameter = false;
                    parameter = p;
                }
            }

            return parameter;
        }
    }
}
