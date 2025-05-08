using System.Windows;

namespace LarkatorGUI
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private SettingsWindowModel Model { get { return Resources["Model"] as SettingsWindowModel; } }

        public SettingsWindow()
        {
            InitializeComponent();
            
            // Initialize the password box with the saved password if available
            if (!string.IsNullOrEmpty(Properties.Settings.Default.SftpPassword))
            {
                SftpPasswordBox.Password = Properties.Settings.Default.SftpPassword;
            }
            
            // Initialize the private key passphrase box if available
            if (!string.IsNullOrEmpty(Properties.Settings.Default.PrivateKeyPassphrase))
            {
                PrivateKeyPassphraseBox.Password = Properties.Settings.Default.PrivateKeyPassphrase;
            }
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
        
        private void SftpPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Update the password in settings
            Model.Settings.SftpPassword = SftpPasswordBox.Password;
        }
        
        private void PrivateKeyPassphraseBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Update the private key passphrase in settings
            Model.Settings.PrivateKeyPassphrase = PrivateKeyPassphraseBox.Password;
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
