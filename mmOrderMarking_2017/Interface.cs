namespace mmOrderMarking
{
    using System;
    using System.Collections.Generic;
    using ModPlusAPI.Interfaces;

    public class Interface : IModPlusFunctionInterface
    {
        public SupportedProduct SupportedProduct => SupportedProduct.Revit;

        public string Name => "mmOrderMarking";

        public string AvailProductExternalVersion => "2017";

        public string FullClassName => "mmOrderMarking.Command";

        public string AppFullClassName => string.Empty;

        public Guid AddInId => Guid.Empty;

        public string LName => "Маркировка по порядку";

        public string Description => "Добавление нумерации в марку элементов с возможностью маркировки в спецификации";

        public string Author => "Маркевич Максим";

        public string Price => "0";

        public bool CanAddToRibbon => true;

        public string FullDescription => "При работе на виде, не являющимся спецификацией, Функция добавляет нумерованную марку в порядке создания элементов. При работе в спецификации Функция добавляет нумерованную марку в порядке расположения элементов в спецификации (с учетом сортировки). Имеется возможность задавать префикс и суффикс для марки";

        public string ToolTipHelpImage => string.Empty;

        public List<string> SubFunctionsNames => new List<string>();

        public List<string> SubFunctionsLames => new List<string>();

        public List<string> SubDescriptions => new List<string>();

        public List<string> SubFullDescriptions => new List<string>();

        public List<string> SubHelpImages => new List<string>();

        public List<string> SubClassNames => new List<string>();
    }
}
