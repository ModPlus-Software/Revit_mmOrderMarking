namespace mmOrderMarking.Models
{
    using Autodesk.Revit.DB;
    using Enums;
    using JetBrains.Annotations;

    /// <summary>
    /// Данные для нумерации
    /// </summary>
    public class NumerateData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumerateData"/> class.
        /// </summary>
        /// <param name="parameterName">Имя параметра</param>
        /// <param name="startValue">Начальное числовое значение</param>
        /// <param name="prefix">Префикс</param>
        /// <param name="suffix">Суффикс</param>
        /// <param name="orderDirection">Направление нумерации (по возрастанию или убыванию)</param>
        /// <param name="locationOrder">Направление нумерации по положению элементов</param>
        /// <param name="isNumeric">Является ли параметр числовым</param>
        public NumerateData(
            string parameterName,
            int startValue,
            string prefix,
            string suffix,
            OrderDirection orderDirection,
            LocationOrder locationOrder,
            bool isNumeric)
        {
            ParameterName = parameterName;
            StartValue = startValue;
            Prefix = prefix;
            Suffix = suffix;
            OrderDirection = orderDirection;
            LocationOrder = locationOrder;
            IsNumeric = isNumeric;
        }

        /// <summary>
        /// Имя параметра
        /// </summary>
        public string ParameterName { get; }

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
        /// Является ли параметр числовым
        /// </summary>
        public bool IsNumeric { get; }

        /// <summary>
        /// Направление нумерации (по возрастанию или убыванию)
        /// </summary>
        public OrderDirection OrderDirection { get; }

        /// <summary>
        /// Направление нумерации по положению элементов
        /// </summary>
        public LocationOrder LocationOrder { get; }

        /// <summary>
        /// Взять параметр из элемента
        /// </summary>
        /// <param name="element">Элемент Revit</param>
        /// <param name="getFromType">Искать ли в типоразмере</param>
        /// <param name="isInstanceParameter">Является ли параметр параметром типа</param>
        [CanBeNull]
        public Parameter GetParameter(Element element, bool getFromType, out bool isInstanceParameter)
        {
            var parameter = element.LookupParameter(ParameterName);
            isInstanceParameter = true;
            
            if (parameter == null && getFromType)
            {
                var elementType = element.Document.GetElement(element.GetTypeId());
                if (elementType != null &&
                    elementType.LookupParameter(ParameterName) is Parameter p)
                {
                    isInstanceParameter = false;
                    parameter = p;
                }
            }

            return parameter;
        }
    }
}
