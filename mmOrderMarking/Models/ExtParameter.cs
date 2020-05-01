namespace mmOrderMarking.Models
{
    using Autodesk.Revit.DB;

    /// <summary>
    /// Параметр
    /// </summary>
    public class ExtParameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtParameter"/> class.
        /// </summary>
        /// <param name="description">Описание (параметр экземпляра или типа)</param>
        /// <param name="parameter">Параметр Revit</param>
        public ExtParameter(string description, Parameter parameter)
        {
            Name = parameter.Definition.Name;
            Description = description;
            Parameter = parameter;
            IsNumeric = parameter.StorageType != StorageType.String;
        }

        /// <summary>
        /// Имя параметра
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Описание (параметр экземпляра или типа)
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Параметр содержит числовое значение
        /// </summary>
        public bool IsNumeric { get; }

        /// <summary>
        /// Параметр Revit
        /// </summary>
        public Parameter Parameter { get; }

        /// <summary>
        /// Is current parameter match to specified <see cref="BuiltInParameter"/>
        /// </summary>
        /// <param name="builtInParameter"><see cref="BuiltInParameter"/></param>
        /// <returns></returns>
        public bool IsMatchBuiltInParameter(BuiltInParameter builtInParameter)
        {
            if (Parameter.Definition is InternalDefinition internalDefinition &&
                internalDefinition.BuiltInParameter == builtInParameter)
                return true;
            return false;
        }

        /// <summary>
        /// Является ли параметр валидным для обработки
        /// </summary>
        /// <param name="parameter">Параметр Revit</param>
        public static bool IsValid(Parameter parameter)
        {
            if (parameter == null)
                return false;
            if (parameter.IsReadOnly)
                return false;
            if (parameter.StorageType == StorageType.String)
                return true;

            if (parameter.StorageType == StorageType.Integer &&
                parameter.Definition.ParameterType != ParameterType.YesNo)
                return true;

            if (parameter.StorageType == StorageType.Double)
                return true;

            return false;
        }
    }
}
