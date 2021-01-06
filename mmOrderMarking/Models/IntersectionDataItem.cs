namespace mmOrderMarking.Models
{
    using Autodesk.Revit.DB;

    /// <summary>
    /// Объект данных о пересечении
    /// </summary>
    public class IntersectionDataItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntersectionDataItem"/> class.
        /// </summary>
        /// <param name="curve">Исходная кривая (сплайн) вдоль которой ищутся пересечения</param>
        /// <param name="intersectionResult"><see cref="IntersectionResult"/></param>
        /// <param name="element">Intersected element</param>
        public IntersectionDataItem(
            Curve curve,
            IntersectionResult intersectionResult,
            Element element)
        {
            /*
             * XYZPoint is the evaluated intersection point
             * UVPoint.U is the unnormalized parameter on this curve (use ComputeNormalizedParameter to compute the normalized value).
             * UVPoint.V is the unnormalized parameter on the specified curve (use ComputeNormalizedParameter to compute the normalized value).
             */
            IntersectedElement = element;
            IntersectedElementId = element.Id.IntegerValue;
            Point = intersectionResult.XYZPoint;
            Parameter = curve.ComputeNormalizedParameter(intersectionResult.UVPoint.U);
        }

        /// <summary>
        /// Intersected element
        /// </summary>
        public Element IntersectedElement { get; }

        /// <summary>
        /// Id of <see cref="IntersectedElement"/>
        /// </summary>
        public int IntersectedElementId { get; }

        /// <summary>
        /// Point of intersection in local 3D coordinates
        /// </summary>
        public XYZ Point { get; }

        /// <summary>
        /// 1d parameter of the point of intersection
        /// </summary>
        public double Parameter { get; }
    }
}
