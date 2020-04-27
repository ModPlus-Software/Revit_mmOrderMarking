namespace mmOrderMarking
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
            IsDouble = parameter.StorageType == StorageType.Double;
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
        public bool IsDouble { get; }

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
    }
}
