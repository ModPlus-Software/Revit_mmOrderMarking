#pragma warning disable SA1600 // Elements should be documented
namespace mmOrderMarking
{
    using System;
    using System.Collections.Generic;
    using ModPlusAPI.Interfaces;

    public class ModPlusConnector : IModPlusFunctionInterface
    {
        public SupportedProduct SupportedProduct => SupportedProduct.Revit;

        public string Name => "mmOrderMarking";

#if R2015
        public string AvailProductExternalVersion => "2015";
#elif R2016
        public string AvailProductExternalVersion => "2016";
#elif R2017
        public string AvailProductExternalVersion => "2017";
#elif R2018
        public string AvailProductExternalVersion => "2018";
#elif R2019
        public string AvailProductExternalVersion => "2019";
#elif R2020
        public string AvailProductExternalVersion => "2020";
#elif R2021
        public string AvailProductExternalVersion => "2021";
#endif

        public string FullClassName => "mmOrderMarking.Command";

        public string AppFullClassName => string.Empty;

        public Guid AddInId => Guid.Empty;

        public string LName => "Нумерация";

        public string Description => "Добавление нумерации в указанный параметр элементов с возможностью нумерации в спецификации";

        public string Author => "Пекшев Александр aka Modis";

        public string Price => "0";

        public bool CanAddToRibbon => true;

        public string FullDescription => "При работе на виде, не являющимся спецификацией, можно указывать направление нумерации в зависимости от расположения элементов или добавлять нумерацию в порядке создания элементов. При работе в спецификации плагин добавляет нумерацию в порядке расположения элементов в спецификации (с учетом сортировки). Свойство спецификации «Для каждого экземпляра» влияет на скорость работы плагина. Имеется возможность задавать префикс и суффикс для номера";

        public string ToolTipHelpImage => string.Empty;

        public List<string> SubFunctionsNames => new List<string>();

        public List<string> SubFunctionsLames => new List<string>();

        public List<string> SubDescriptions => new List<string>();

        public List<string> SubFullDescriptions => new List<string>();

        public List<string> SubHelpImages => new List<string>();

        public List<string> SubClassNames => new List<string>();
    }
}
#pragma warning restore SA1600 // Elements should be documented