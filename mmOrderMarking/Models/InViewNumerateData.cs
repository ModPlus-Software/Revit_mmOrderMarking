namespace mmOrderMarking.Models
{
    using Enums;

    /// <summary>
    /// Данные для нумерации на виде, не являющимся спецификацией
    /// </summary>
    public class InViewNumerateData : NumerateData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InViewNumerateData"/> class.
        /// </summary>
        /// <param name="parameter">Параметр</param>
        /// <param name="startValue">Начальное числовое значение</param>
        /// <param name="prefix">Префикс</param>
        /// <param name="suffix">Суффикс</param>
        /// <param name="locationOrder">Направление нумерации по положению элементов</param>
        /// <param name="orderDirection">Направление нумерации (по возрастанию или убыванию)</param>
        public InViewNumerateData(
            ExtParameter parameter,
            int startValue,
            string prefix,
            string suffix,
            LocationOrder locationOrder,
            OrderDirection orderDirection) 
            : base(parameter, startValue, prefix, suffix, orderDirection)
        {
            LocationOrder = locationOrder;
        }
        
        /// <summary>
        /// Направление нумерации по положению элементов
        /// </summary>
        public LocationOrder LocationOrder { get; }
    }
}
