namespace mmOrderMarking.Enums
{
    /// <summary>
    /// Вариант сортировки (порядка) по значению положения элемента
    /// </summary>
    public enum LocationOrder
    {
        /// <summary>
        /// В порядке создания элементов
        /// </summary>
        Creation = 0,

        /// <summary>
        /// Слева направо и сверху вниз
        /// </summary>
        LeftToRightUpToDown = 1,

        /// <summary>
        /// Слева направо и снизу вверх
        /// </summary>
        LeftToRightDownToUp = 2,

        /// <summary>
        /// Справа налево и сверху вниз
        /// </summary>
        RightToLeftUpToDown = 3,

        /// <summary>
        /// Справа налево и снизу вверх
        /// </summary>
        RightToLeftDownToUp = 4
    }
}
