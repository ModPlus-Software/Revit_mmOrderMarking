﻿namespace mmOrderMarking
{
    using System;
    using System.Collections.Generic;
    using ModPlusAPI.Interfaces;

    public class Interface : IModPlusFunctionInterface
    {
        public SupportedProduct SupportedProduct => SupportedProduct.Revit;

        public string Name => "mmOrderMarking";

        public string AvailProductExternalVersion => "2018";

        public string FullClassName => "mmOrderMarking.Command";

        public string AppFullClassName => string.Empty;

        public Guid AddInId => Guid.Empty;

        public string LName => "Маркировка по порядку";

        public string Description => "Добавление нумерации в марку элементов с возможностью маркировки в спецификации";

        public string Author => "Пекшев Александр aka Modis";

        public string Price => "0";

        public bool CanAddToRibbon => true;

        public string FullDescription => string.Empty;

        public string ToolTipHelpImage => string.Empty;

        public List<string> SubFunctionsNames => new List<string>();

        public List<string> SubFunctionsLames => new List<string>();

        public List<string> SubDescriptions => new List<string>();

        public List<string> SubFullDescriptions => new List<string>();

        public List<string> SubHelpImages => new List<string>();

        public List<string> SubClassNames => new List<string>();
    }
}