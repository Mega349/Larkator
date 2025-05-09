using System.Windows;

namespace LarkatorGUI
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private SettingsWindowModel Model { get { return Resources["Model"] as SettingsWindowModel; } }
        
        // Property to display the current profile name
        public string CurrentProfileName 
        {
            get 
            {
                var profileManager = SftpProfileManager.Instance;
                if (profileManager.SelectedProfile != null)
                {
                    return profileManager.SelectedProfile.Name;
                }
                return "None";
            }
        }

        public SettingsWindow()
        {
            InitializeComponent();
            
            // Set window to update when activated
            this.Activated += SettingsWindow_Activated;
        }
        
        private void SettingsWindow_Activated(object sender, System.EventArgs e)
        {
            // Update the current profile display
            UpdateCurrentProfileDisplay();
        }
        
        private void UpdateCurrentProfileDisplay()
        {
            // Force a refresh of the current profile display
            CurrentProfileTextBlock.GetBindingExpression(System.Windows.Controls.TextBlock.TextProperty)?.UpdateTarget();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            Model.Settings.Save();
            Properties.Settings.Default.Reload();

            Close();
        }

        private void Restore_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MessageBox.Show("Are you want to override all settings with their defaults? All required paths must be set again before the program will function.", "Are you sure?",
                MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                Properties.Settings.Default.Reset();
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();
            }
        }

        private void ResetTmp_Click(object sender, RoutedEventArgs e)
        {
            Model.Settings.OutputDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Properties.Resources.ProgramName);
        }

        private void ManageServerProfiles_Click(object sender, RoutedEventArgs e)
        {
            var window = new ServerProfilesWindow();
            window.Owner = this;
            window.ShowDialog();
            
            // Update current profile display after profile manager closes
            UpdateCurrentProfileDisplay();
        }
    }

    public class SettingsWindowModel : DependencyObject
    {
        public Properties.Settings Settings { get; }

        public SettingsWindowModel()
        {
            Settings = new Properties.Settings();
        }
    }
}
