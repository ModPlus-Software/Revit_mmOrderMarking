namespace mmOrderMarking
{
    using Autodesk.Revit.DB;

    /// <summary>
    /// Параметр для отображения в ComboBox
    /// </summary>
    public class CbParameter
    {
        public CbParameter(string description, Parameter parameter)
        {
            Name = parameter.Definition.Name;
            Description = description;
            Parameter = parameter;
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
        /// Параметр Revit
        /// </summary>
        public Parameter Parameter { get; }
    }
}
