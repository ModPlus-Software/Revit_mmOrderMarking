namespace mmOrderMarking.Context
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Input;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;
    using Enums;
    using Models;
    using ModPlus_Revit.Utils;
    using ModPlusAPI;
    using ModPlusAPI.IO;
    using ModPlusAPI.Mvvm;
    using ModPlusAPI.Windows;
    using Services;
    using View;

    /// <summary>
    /// Контекст работы на виде, не являющимся спецификацией
    /// </summary>
    public class InViewContext : BaseContext
    {
        private readonly InViewWindow _parentWindow;
        private readonly UIApplication _uiApplication;
        private readonly NumerateService _numerateService;
        private LocationOrder _locationOrder;
        private ElementsSelectionType _selectionType;

        public InViewContext(InViewWindow parentWindow, UIApplication uiApplication)
        {
            _parentWindow = parentWindow;
            _uiApplication = uiApplication;
            _numerateService = new NumerateService(uiApplication);
            Categories = new ObservableCollection<RevitBuiltInCategory>();
            Categories.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(DisplayCategories));

            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(Parameter))
                    OnPropertyChanged(nameof(CanNumerate));
            };
        }

        /// <summary>
        /// Вариант направления нумерации по положению элемента
        /// </summary>
        public LocationOrder LocationOrder
        {
            get => _locationOrder;
            set
            {
                if (_locationOrder == value)
                    return;
                _locationOrder = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(LocationOrder), value.ToString(), true);
            }
        }

        /// <summary>
        /// Видимость варианта направления по положению элемента
        /// </summary>
        public bool IsVisibleLocationOrder => SelectionType == ElementsSelectionType.ByRectangle;

        /// <summary>
        /// Вариант выбора элементов на виде
        /// </summary>
        public ElementsSelectionType SelectionType
        {
            get => _selectionType;
            set
            {
                if (_selectionType == value)
                    return;
                _selectionType = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(SelectionType), value.ToString(), true);
                Elements?.Clear();
                OnPropertyChanged(nameof(IsVisibleLocationOrder));
            }
        }

        /// <summary>
        /// Элементы для нумерации
        /// </summary>
        public List<Element> Elements { get; private set; }

        /// <summary>
        /// Категории для фильтрации элементов
        /// </summary>
        public ObservableCollection<RevitBuiltInCategory> Categories { get; set; }

        /// <summary>
        /// Строка, отображающая категории для фильтрации
        /// </summary>
        public string DisplayCategories => string.Join(", ", Categories.Select(c => c.DisplayName));

        /// <summary>
        /// Можно ли выполнить нумерацию или очистку
        /// </summary>
        public bool CanNumerate => Elements != null && Elements.Any();

        /// <summary>
        /// Редактировать список категорий
        /// </summary>
        public ICommand EditCategoriesCommand => new RelayCommandWithoutParameter(() =>
        {
            try
            {
                _parentWindow.Topmost = false;
                var factory = new RevitBuiltInCategoryFactory(_uiApplication);
                var context = new CategoriesContext();
                var win = new CategoriesWindow
                {
                    DataContext = context
                };

                win.ContentRendered += (sender, args) => context.Initialize(factory, Categories);

                if (win.ShowDialog() == true)
                {
                    Categories.Clear();
                    foreach (var s in context.Categories.Where(c => c.IsSelected))
                    {
                        Categories.Add(s);
                    }

                    if (Elements != null && Elements.Any())
                    {
                        // Выполнить фильтрацию выбранных элементов по текущему списку категорий?
                        if (MessageBox.ShowYesNo(Language.GetItem("m3")))
                            FilterSelectedElementsByCategories();
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            finally
            {
                _parentWindow.Topmost = true;
            }
        });

        /// <summary>
        /// Выбор элементов
        /// </summary>
        public ICommand SelectElementsCommand => new RelayCommandWithoutParameter(() =>
        {
            Command.RevitEvent.Run(
                () =>
                {
                    try
                    {
                        _parentWindow.Hide();

                        var uiDoc = _uiApplication.ActiveUIDocument;
                        var doc = uiDoc.Document;

                        if (SelectionType == ElementsSelectionType.ByRectangle)
                        {
                            Elements = uiDoc.Selection
                                .PickObjects(
                                    ObjectType.Element,
                                    new ModelElementsSelectionFilter(Categories),
                                    Language.GetItem(LangItem, "m1"))
                                .Select(r => doc.GetElement(r))
                                .ToList();
                        }
                        else if (SelectionType == ElementsSelectionType.ByOrderPick)
                        {
                            Elements = OrderPick();
                        }
                        else if (SelectionType == ElementsSelectionType.ByCurve)
                        {
                            var curve = PickCurve();
                            if (curve != null)
                                Elements = GetElementsByCurve(curve).ToList();
                        }

                        OnPropertyChanged(nameof(Elements));
                        OnPropertyChanged(nameof(CanNumerate));

                        CollectParameters(Parameter?.Name);
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        // ignore
                    }
                    catch (Exception exception)
                    {
                        ExceptionBox.Show(exception);
                    }
                    finally
                    {
                        _parentWindow.Show();
                    }
                },
                false);
        });

        /// <inheritdoc />
        public override ICommand NumerateCommand => new RelayCommandWithoutParameter(() =>
        {
            Command.RevitEvent.Run(
                () =>
                {
                    try
                    {
                        _parentWindow.Hide();

                        _numerateService.NumerateInView(
                            SelectionType,
                            new InViewNumerateData(Parameter, StartValue, Prefix, Suffix, LocationOrder, OrderDirection),
                            Elements);
                    }
                    catch (Exception exception)
                    {
                        ExceptionBox.Show(exception);
                    }
                    finally
                    {
                        _parentWindow.Show();
                    }
                },
                false);
        });

        /// <inheritdoc />
        public override ICommand ClearCommand => new RelayCommandWithoutParameter(() =>
        {
            Command.RevitEvent.Run(
                () =>
                {
                    try
                    {
                        _parentWindow.Hide();

                        _numerateService.ClearInView(Parameter, Elements);
                    }
                    catch (Exception exception)
                    {
                        ExceptionBox.Show(exception);
                    }
                    finally
                    {
                        _parentWindow.Show();
                    }
                },
                false);
        });

        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="preSelectedElements">Предварительно выбранные элементы</param>
        public void Initialize(List<Element> preSelectedElements)
        {
            if (preSelectedElements != null && preSelectedElements.Any())
            {
                _selectionType = ElementsSelectionType.ByRectangle;
                OnPropertyChanged(nameof(SelectionType));
                Elements = preSelectedElements;
                OnPropertyChanged(nameof(Elements));
                OnPropertyChanged(nameof(CanNumerate));
            }
            else
            {
                SelectionType =
                    Enum.TryParse(UserConfigFile.GetValue(LangItem, nameof(SelectionType)), out ElementsSelectionType selectionType)
                        ? selectionType : ElementsSelectionType.ByRectangle;
            }

            Prefix = UserConfigFile.GetValue(LangItem, nameof(Prefix));
            Suffix = UserConfigFile.GetValue(LangItem, nameof(Suffix));

            StartValue = int.TryParse(UserConfigFile.GetValue(LangItem, nameof(StartValue)), out var i) ? i : 1;
            LocationOrder =
                Enum.TryParse(UserConfigFile.GetValue(LangItem, nameof(LocationOrder)), out LocationOrder locationOrder)
                    ? locationOrder : LocationOrder.Creation;

            CollectParameters(UserConfigFile.GetValue(LangItem, nameof(Parameter)));

            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(SelectionType))
                {
                    Elements?.Clear();
                    Parameters?.Clear();
                    OnPropertyChanged(nameof(Elements));
                    OnPropertyChanged(nameof(CanNumerate));
                }
            };
        }

        private void CollectParameters(string previousSelectedParameter)
        {
            var instParamDescription = Language.GetItem(LangItem, "h12");
            var typeParamDescription = Language.GetItem(LangItem, "h13");

            Parameters.Clear();

            var parameters = new List<ExtParameter>();

            if (Elements != null)
            {
                var parameterLists = new List<List<ExtParameter>>();

                foreach (var element in Elements)
                {
                    var parametersOfElement = new List<ExtParameter>();

                    foreach (var parameter in element.Parameters.Cast<Parameter>())
                    {
                        parametersOfElement.Add(new ExtParameter(instParamDescription, parameter));
                    }

                    var t = element.Document.GetElement(element.GetTypeId());
                    if (t != null)
                    {
                        foreach (var parameter in t.Parameters.Cast<Parameter>())
                        {
                            parametersOfElement.Add(new ExtParameter(typeParamDescription, parameter));
                        }
                    }

                    parameterLists.Add(parametersOfElement);
                }
                
                for (var i = 0; i < parameterLists.Count; i++)
                {
                    var extParameters = parameterLists[i];
                    foreach (var parameter in extParameters.Where(p => p != null))
                    {
                        var hasInAll = true;

                        for (var j = 0; j < parameterLists.Count; j++)
                        {
                            if (i == j)
                                continue;
                            if (parameterLists[j].FirstOrDefault(p => p.Name == parameter.Name) == null)
                            {
                                hasInAll = false;
                                break;
                            }
                        }

                        if (hasInAll)
                        {
                            if (parameters.FirstOrDefault(p =>
                                p.Name == parameter.Name && p.Description == parameter.Description) == null)
                            {
                                parameters.Add(parameter);
                            }
                        }
                    }
                }
            }
            
            foreach (var parameter in parameters.OrderBy(p => p.Name, new OrdinalStringComparer()))
            {
                Parameters.Add(parameter);
            }

            if (!string.IsNullOrEmpty(previousSelectedParameter) &&
                Parameters.FirstOrDefault(p => p.Name == previousSelectedParameter) is ExtParameter extParameter)
            {
                Parameter = extParameter;
            }
            else
            {
                var markParameter = Parameters.FirstOrDefault(p =>
                    p.IsMatchBuiltInParameter(BuiltInParameter.ALL_MODEL_MARK));
                Parameter = markParameter ?? Parameters.FirstOrDefault();
            }
        }

        private List<Element> OrderPick()
        {
            var elements = new List<Element>();
            while (true)
            {
                try
                {
                    elements.Add(_uiApplication.ActiveUIDocument.Document.GetElement(
                        _uiApplication.ActiveUIDocument.Selection.PickObject(
                            ObjectType.Element, new ModelElementsSelectionFilter(Categories))));
                }
                catch
                {
                    break;
                }
            }

            return elements;
        }

        private Tuple<Curve, ElementId> PickCurve()
        {
            var uiDoc = _uiApplication.ActiveUIDocument;
            var doc = uiDoc.Document;

            var reference = uiDoc.Selection.PickObject(ObjectType.Element, new CurveSelectionFilter(), "Pick curve");
            var element = doc.GetElement(reference);
            if (element is DetailCurve detailCurve)
            {
                return new Tuple<Curve, ElementId>(detailCurve.GeometryCurve, detailCurve.Id);
            }

            if (element is ModelCurve modelCurve)
            {
                return new Tuple<Curve, ElementId>(modelCurve.GeometryCurve, modelCurve.Id);
            }

            return null;
        }

        private IEnumerable<Element> GetElementsByCurve(Tuple<Curve, ElementId> curve)
        {
            var doc = _uiApplication.ActiveUIDocument.Document;
            var elements = new FilteredElementCollector(doc, doc.ActiveView.Id)
                .WhereElementIsNotElementType()
                .Where(e => !(e is Group) && e.Id.IntegerValue != curve.Item2.IntegerValue)
                .ToList();

            if (Categories.Any())
            {
                for (var i = elements.Count - 1; i >= 0; i--)
                {
                    if (elements[i].Category == null ||
                        Categories.FirstOrDefault(c => (int)c.BuiltInCategory == elements[i].Category.Id.IntegerValue) == null)
                        elements.RemoveAt(i);
                }
            }

            if (!elements.Any())
                yield break;

            var viewDirection = doc.ActiveView.ViewDirection;
            var plane = Plane.CreateByNormalAndOrigin(viewDirection, XYZ.Zero);
            var projectedCurve = GetProjectedCurve(curve.Item1, plane);
            var elementsWithCurves = elements.ToDictionary(e => e, e => GetProjectedCurves(e, plane).ToList());

            var intersectionDataItems = new List<IntersectionDataItem>();
            foreach (var pair in elementsWithCurves)
            {
                foreach (var c in pair.Value.Where(c => c != null))
                {
                    if (projectedCurve.Intersect(c, out var intersectionResultArray) == SetComparisonResult.Overlap &&
                        intersectionResultArray != null &&
                        !intersectionResultArray.IsEmpty)
                    {
                        foreach (var intersectionResult in intersectionResultArray.OfType<IntersectionResult>())
                        {
                            intersectionDataItems.Add(new IntersectionDataItem(projectedCurve, intersectionResult, pair.Key));
                        }
                    }
                }
            }

            var processedIds = new List<int>();
            foreach (var intersectionDataItem in intersectionDataItems.OrderBy(i => i.Parameter))
            {
                if (processedIds.Contains(intersectionDataItem.IntersectedElementId))
                    continue;

                yield return intersectionDataItem.IntersectedElement;
                processedIds.Add(intersectionDataItem.IntersectedElementId);
            }
        }

        private IEnumerable<Curve> GetProjectedCurves(Element element, Plane plane)
        {
            var geometry = element.get_Geometry(new Options
            {
                View = _uiApplication.ActiveUIDocument.Document.ActiveView,
                ComputeReferences = false,
                IncludeNonVisibleObjects = false
            });

            if (geometry != null)
            {
                foreach (var geometryObject in geometry.GetTransformed(Transform.Identity))
                {
                    if (geometryObject is Curve curve &&
                        GetProjectedCurve(curve, plane) is Curve projectedCurve)
                    {
                        yield return projectedCurve;
                    }
                    
                    if (geometryObject is Solid solid)
                    {
                        foreach (Edge edge in solid.Edges)
                        {
                            yield return GetProjectedCurve(edge.AsCurve(), plane);
                        }
                    }
                }
            }
        }

        private Curve GetProjectedCurve(Curve curve, Plane plane)
        {
            try
            {
                if (curve is Line line)
                {
                    return Line.CreateBound(
                        plane.ProjectOnto(line.GetEndPoint(0)),
                        plane.ProjectOnto(line.GetEndPoint(1)));
                }

                if (curve is Arc arc)
                {
                    return Arc.Create(
                        plane.ProjectOnto(arc.GetEndPoint(0)),
                        plane.ProjectOnto(arc.GetEndPoint(1)),
                        plane.ProjectOnto(arc.Evaluate(0.5, true)));
                }

                if (curve is NurbSpline nurbSpline)
                {
                    return NurbSpline.CreateCurve(
                        nurbSpline.CtrlPoints.Select(plane.ProjectOnto).ToList(),
                        nurbSpline.Weights.Cast<double>().ToList());
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private void FilterSelectedElementsByCategories()
        {
            for (var i = Elements.Count - 1; i >= 0; i--)
            {
                if (Categories.FirstOrDefault(c => (int)c.BuiltInCategory == Elements[i].Category.Id.IntegerValue) == null)
                    Elements.RemoveAt(i);
            }

            OnPropertyChanged(nameof(Elements));
            OnPropertyChanged(nameof(CanNumerate));
            
            CollectParameters(null);
        }
    }
}
