namespace mmOrderMarking
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Enums;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Visibility = System.Windows.Visibility;

    public partial class WinScheduleAutoNum
    {
        private const string LangItem = "mmOrderMarking";

        private readonly ExternalCommandData _commandData;

        public WinScheduleAutoNum(ExternalCommandData commandData)
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetFunctionLocalName(LangItem, new ModPlusConnector().LName);
            _commandData = commandData;
            if (commandData.View is ViewSchedule viewSchedule)
            {
                CbParameter.Visibility = Visibility.Visible;
                TbParameter.Visibility = Visibility.Collapsed;
                CbDirection.Visibility = Visibility.Collapsed;
                CbOrderBy.Visibility = Visibility.Visible;
                var elements = new FilteredElementCollector(commandData.View.Document, commandData.View.Id)
                    .WhereElementIsNotElementType().ToList();
                foreach (SchedulableField schedulableField in viewSchedule.Definition.GetSchedulableFields())
                {
                    if (schedulableField.FieldType == ScheduleFieldType.Instance)
                    {
                        var parameter = elements.First().get_Parameter((BuiltInParameter)schedulableField.ParameterId.IntegerValue);
                        if (parameter != null && parameter.StorageType == StorageType.String &&
                            !parameter.IsReadOnly)
                            CbParameter.Items.Add(schedulableField.GetName(viewSchedule.Document));
                    }
                }

                if (elements.First().get_Parameter(BuiltInParameter.ALL_MODEL_MARK) != null)
                    CbParameter.SelectedItem = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_MARK);
                else CbParameter.SelectedIndex = 0;
            }
            else
            {
                CbParameter.Visibility = Visibility.Collapsed;
                TbParameter.Visibility = Visibility.Visible;
                CbOrderBy.Visibility = Visibility.Collapsed;
                CbDirection.Visibility = Visibility.Visible;
                TbParameter.Text = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_MARK);
            }
        }

        private void Do_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide();
                var parameterName = TbParameter.Visibility == Visibility.Visible
                    ? TbParameter.Text
                    : CbParameter.SelectedItem.ToString();

                NumerateService numerateService = new NumerateService(
                    _commandData,
                    TbPrefix.Text,
                    TbSuffix.Text,
                    // ReSharper disable once PossibleInvalidOperationException
                    (int)TbStartValue.Value.Value,
                    CbOrderBy.SelectedIndex == 1 ? OrderDirection.Descending : OrderDirection.Ascending, parameterName,
                    (LocationOrder)CbDirection.SelectedIndex,
                    ChkNumberingInGroups.IsChecked != null && ChkNumberingInGroups.IsChecked.Value);
                numerateService.ProceedNumeration();
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            finally
            {
                ShowDialog();
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide();
                var parameterName = TbParameter.Visibility == Visibility.Visible
                    ? TbParameter.Text
                    : CbParameter.SelectedItem.ToString();
                NumerateService.ScheduleAutoDel(_commandData, parameterName);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            finally
            {
                ShowDialog();
            }
        }

        private void WinScheduleAutoNum_OnLoaded(object sender, RoutedEventArgs e)
        {
            TbPrefix.Text = UserConfigFile.GetValue(LangItem, "Prefix");
            TbSuffix.Text = UserConfigFile.GetValue(LangItem, "Suffix");
            TbStartValue.Value = int.TryParse(UserConfigFile.GetValue(LangItem, "StartValue"), out var i) ? i : 1;
            CbOrderBy.SelectedIndex = int.TryParse(UserConfigFile.GetValue(LangItem, "OrderBy"), out i) ? i : 1;
            CbDirection.SelectedIndex = int.TryParse(UserConfigFile.GetValue(LangItem, "Direction"), out i) ? i : 0;
            ChkNumberingInGroups.IsChecked = bool.TryParse(UserConfigFile.GetValue(LangItem, "NumberingInGroups"), out var b) && b;
            var selectedParameter = UserConfigFile.GetValue(LangItem, "SelectedParameter");
            if (CbParameter.Items.Contains(selectedParameter))
                CbParameter.SelectedItem = selectedParameter;
        }

        private void WinScheduleAutoNum_OnClosing(object sender, CancelEventArgs e)
        {
            UserConfigFile.SetValue(LangItem, "Prefix", TbPrefix.Text, false);
            UserConfigFile.SetValue(LangItem, "Suffix", TbSuffix.Text, false);
            UserConfigFile.SetValue(LangItem, "StartValue", TbStartValue.Value.ToString(), false);
            UserConfigFile.SetValue(LangItem, "OrderBy", CbOrderBy.SelectedIndex.ToString(), false);
            UserConfigFile.SetValue(LangItem, "Direction", CbDirection.SelectedIndex.ToString(), false);
            UserConfigFile.SetValue(LangItem, "NumberingInGroups", (ChkNumberingInGroups.IsChecked != null && ChkNumberingInGroups.IsChecked.Value).ToString(), false);
            if (CbParameter.SelectedItem != null) 
                UserConfigFile.SetValue(LangItem, "SelectedParameter", CbParameter.SelectedItem.ToString(), false);
            UserConfigFile.SaveConfigFile();
        }
    }
}