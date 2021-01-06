namespace mmOrderMarking.Models
{
    using Enums;

    /// <summary>
    /// Данные для нумерации на виде, являющимся спецификацией
    /// </summary>
    public class InScheduleNumerateData : NumerateData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InScheduleNumerateData"/> class.
        /// </summary>
        /// <param name="parameter">Параметр</param>
        /// <param name="startValue">Начальное числовое значение</param>
        /// <param name="prefix">Префикс</param>
        /// <param name="suffix">Суффикс</param>
        /// <param name="orderDirection">Направление нумерации (по возрастанию или убыванию)</param>
        public InScheduleNumerateData(
            ExtParameter parameter,
            int startValue,
            string prefix,
            string suffix,
            OrderDirection orderDirection)
            : base(parameter, startValue, prefix, suffix, orderDirection)
        {
        }
    }
}
