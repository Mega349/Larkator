using System;
using System.Windows;
using Microsoft.Win32;

namespace LarkatorGUI
{
    public partial class ServerProfileDialog : Window
    {
        private readonly SftpServerProfile _profile;
        private readonly bool _isNewProfile;

        public ServerProfileDialog(SftpServerProfile profile = null)
        {
            InitializeComponent();

            _isNewProfile = profile == null;
            _profile = profile ?? new SftpServerProfile();
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
    }
} 