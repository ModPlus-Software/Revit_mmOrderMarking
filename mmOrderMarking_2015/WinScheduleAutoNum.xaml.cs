namespace mmOrderMarking
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using Autodesk.Revit.UI;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    public partial class WinScheduleAutoNum
    {
        private const string LangItem = "mmOrderMarking";

        private readonly ExternalCommandData _commandData;

        public WinScheduleAutoNum(ExternalCommandData commandData)
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetFunctionLocalName(LangItem, new Interface().LName);
            _commandData = commandData;
        }

        private void Do_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide();
                VoidsClass.ScheduleAutoNum(_commandData, TbPrefix.Text, TbSuffix.Text);
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
                VoidsClass.ScheduleAutoDel(_commandData);
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
            TbPrefix.Text = UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, LangItem, "Prefix");
            TbSuffix.Text = UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, LangItem, "Suffix");
        }

        private void WinScheduleAutoNum_OnClosing(object sender, CancelEventArgs e)
        {
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, LangItem, "Prefix", TbPrefix.Text, true);
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, LangItem, "Suffix", TbSuffix.Text, true);
        }
    }
}