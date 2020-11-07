namespace mmOrderMarking
{
    using System;
    using System.Collections.Generic;
    using ModPlusAPI.Abstractions;
    using ModPlusAPI.Enums;

    /// <inheritdoc/>
    public class ModPlusConnector : IModPlusPlugin
    {
        /// <inheritdoc/>
        public SupportedProduct SupportedProduct => SupportedProduct.Revit;

        /// <inheritdoc/>
        public string Name => "mmOrderMarking";

#if R2017
        /// <inheritdoc/>
        public string AvailProductExternalVersion => "2017";
#elif R2018
        /// <inheritdoc/>
        public string AvailProductExternalVersion => "2018";
#elif R2019
        /// <inheritdoc/>
        public string AvailProductExternalVersion => "2019";
#elif R2020
        /// <inheritdoc/>
        public string AvailProductExternalVersion => "2020";
#elif R2021
        /// <inheritdoc/>
        public string AvailProductExternalVersion => "2021";
#endif

        /// <inheritdoc/>
        public string FullClassName => "mmOrderMarking.Command";

        /// <inheritdoc/>
        public string AppFullClassName => string.Empty;

        /// <inheritdoc/>
        public Guid AddInId => Guid.Empty;

        /// <inheritdoc/>
        public string LName => "Нумерация";

        /// <inheritdoc/>
        public string Description => "Добавление нумерации в указанный параметр элементов с возможностью нумерации в спецификации";

        /// <inheritdoc/>
        public string Author => "Пекшев Александр aka Modis";

        /// <inheritdoc/>
        public string Price => "0";

        /// <inheritdoc/>
        public bool CanAddToRibbon => true;

        /// <inheritdoc/>
        public string FullDescription => "При работе на виде, не являющимся спецификацией, можно указывать направление нумерации в зависимости от расположения элементов или добавлять нумерацию в порядке создания элементов. При работе в спецификации плагин добавляет нумерацию в порядке расположения элементов в спецификации (с учетом сортировки). Свойство спецификации «Для каждого экземпляра» влияет на скорость работы плагина. Имеется возможность задавать префикс и суффикс для номера";

        /// <inheritdoc/>
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc/>
        public List<string> SubPluginsNames => new List<string>();

        /// <inheritdoc/>
        public List<string> SubPluginsLNames => new List<string>();

        /// <inheritdoc/>
        public List<string> SubDescriptions => new List<string>();

        /// <inheritdoc/>
        public List<string> SubFullDescriptions => new List<string>();

        /// <inheritdoc/>
        public List<string> SubHelpImages => new List<string>();

        /// <inheritdoc/>
        public List<string> SubClassNames => new List<string>();
    }
}