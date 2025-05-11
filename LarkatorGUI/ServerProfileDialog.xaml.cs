using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace LarkatorGUI
{
    public partial class ServerProfileDialog : Window
    {
        private readonly SftpServerProfile _profile;
        private readonly bool _isNewProfile;
        private static readonly Regex _numericRegex = new Regex("[^0-9]+");

        public ServerProfileDialog(SftpServerProfile profile = null)
        {
            InitializeComponent();

            _isNewProfile = profile == null;
            _profile = profile ?? new SftpServerProfile();
            
            // Debug-Ausgabe für Port vor der Dialogdarstellung
            System.Diagnostics.Debug.WriteLine($"ServerProfileDialog - Initialisierung mit Port: {_profile.Port}");
            
            DataContext = _profile;

            // Set password box values
            if (!string.IsNullOrEmpty(_profile.Password))
            {
                SftpPasswordBox.Password = _profile.Password;
            }

            if (!string.IsNullOrEmpty(_profile.PrivateKeyPassphrase))
            {
                PrivateKeyPassphraseBox.Password = _profile.PrivateKeyPassphrase;
            }

            // Set title based on edit or new profile
            Title = _isNewProfile ? "Add Server Profile" : "Edit Server Profile";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Stellen Sie sicher, dass der Port aktualisiert wird
            UpdatePortFromTextBox();
            
            // Apply the changes
            ApplyButton_Click(sender, e);
            
            // If we're still here (no validation errors), close the dialog
            if (SftpProfileManager.Instance.Profiles.Contains(_profile))
            {
                DialogResult = true;
                Close();
            }
        }
        
        private void BrowsePrivateKey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "",
                DereferenceLinks = true,
                Filter = "Private Key Files|*.*",
                Multiselect = false,
                Title = "Select private key file"
            };
            
            if (!string.IsNullOrEmpty(_profile.PrivateKeyPath))
            {
                dialog.InitialDirectory = System.IO.Path.GetDirectoryName(_profile.PrivateKeyPath);
                dialog.FileName = System.IO.Path.GetFileName(_profile.PrivateKeyPath);
            }

            var result = dialog.ShowDialog();
            if (result == true)
            {
                _profile.PrivateKeyPath = dialog.FileName;
                System.Diagnostics.Debug.WriteLine($"Private Key Pfad gesetzt: {_profile.PrivateKeyPath}");
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            // Stellen Sie sicher, dass der Port aktualisiert wird
            UpdatePortFromTextBox();
            
            // Debug-Ausgabe für Port
            System.Diagnostics.Debug.WriteLine($"ApplyButton_Click - Port: {_profile.Port}");
            
            // Validate required fields
            if (string.IsNullOrWhiteSpace(_profile.Name))
            {
                MessageBox.Show("Please enter a profile name.", "Missing Field", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_profile.Host))
            {
                MessageBox.Show("Please enter a host name or IP address.", "Missing Field", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Validiere Port
            if (_profile.Port <= 0 || _profile.Port > 65535)
            {
                MessageBox.Show("Please enter a valid port number (1-65535).", "Invalid Port", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_profile.Username))
            {
                MessageBox.Show("Please enter a username.", "Missing Field", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Wenn SSH Key aktiviert ist, muss ein Pfad ausgewählt sein
            if (_profile.UsePrivateKey && string.IsNullOrWhiteSpace(_profile.PrivateKeyPath))
            {
                MessageBox.Show("When using SSH Private Key authentication, you must select a key file.", 
                    "Missing SSH Key", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Wenn SSH Key deaktiviert ist, muss ein Passwort eingegeben sein
            if (!_profile.UsePrivateKey && string.IsNullOrWhiteSpace(_profile.Password))
            {
                MessageBox.Show("When not using SSH Private Key authentication, you must enter a password.", 
                    "Missing Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(_profile.RemotePath))
            {
                MessageBox.Show("Please enter a remote path.", "Missing Field", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Debug-Ausgabe für Port vor dem Speichern
            System.Diagnostics.Debug.WriteLine($"SaveProfile - Port: {_profile.Port}");

            // Check for name conflict when adding a new profile
            if (_isNewProfile)
            {
                foreach (var existingProfile in SftpProfileManager.Instance.Profiles)
                {
                    if (existingProfile.Name.Equals(_profile.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show($"A profile with the name '{_profile.Name}' already exists. Please choose a different name.", 
                            "Name Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                
                // Add the new profile to the manager
                SftpProfileManager.Instance.AddProfile(_profile);
            }
            else
            {
                // Update the existing profile
                SftpProfileManager.Instance.UpdateProfile(_profile);
            }
            
            // Save the selected profile without triggering reload
            if (SftpProfileManager.Instance.SelectedProfile != _profile)
            {
                SftpProfileManager.Instance.SetSelectedProfileWithoutReload(_profile);
            }
        }

        private void SftpPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_profile != null)
            {
                _profile.Password = SftpPasswordBox.Password;
            }
        }

        private void PrivateKeyPassphraseBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_profile != null)
            {
                _profile.PrivateKeyPassphrase = PrivateKeyPassphraseBox.Password;
            }
        }
        
        private void PortTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Nur Zahlen erlauben
            e.Handled = _numericRegex.IsMatch(e.Text);
        }
        
        private void PortTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePortFromTextBox();
        }
        
        private void UpdatePortFromTextBox()
        {
            if (PortTextBox != null && _profile != null)
            {
                if (int.TryParse(PortTextBox.Text, out int portValue))
                {
                    // Stelle sicher, dass der Port im gültigen Bereich liegt
                    if (portValue < 1) portValue = 1;
                    if (portValue > 65535) portValue = 65535;
                    
                    // Direktes Setzen am Profil
                    _profile.Port = portValue;
                    
                    // Debug-Ausgabe
                    System.Diagnostics.Debug.WriteLine($"UpdatePortFromTextBox - Port gesetzt auf: {portValue}");
                }
                else
                {
                    // Bei ungültigem Wert Standard-Port setzen
                    _profile.Port = 22;
                    PortTextBox.Text = "22";
                    System.Diagnostics.Debug.WriteLine("UpdatePortFromTextBox - Ungültiger Port-Wert, auf 22 zurückgesetzt");
                }
            }
        }
    }
} 