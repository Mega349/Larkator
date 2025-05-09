using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using Newtonsoft.Json;
using LarkatorGUI.Properties;

namespace LarkatorGUI
{
    public class SftpProfileManager : INotifyPropertyChanged
    {
        private ObservableCollection<SftpServerProfile> _profiles = new ObservableCollection<SftpServerProfile>();
        private SftpServerProfile _selectedProfile;
        private static SftpProfileManager _instance;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler ProfileChanged;

        public static SftpProfileManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SftpProfileManager();
                }
                return _instance;
            }
        }

        public ObservableCollection<SftpServerProfile> Profiles
        {
            get { return _profiles; }
            set
            {
                if (_profiles != value)
                {
                    _profiles = value;
                    OnPropertyChanged();
                }
            }
        }

        public SftpServerProfile SelectedProfile
        {
            get { return _selectedProfile; }
            set
            {
                if (_selectedProfile != value)
                {
                    _selectedProfile = value;
                    OnPropertyChanged();
                    
                    // If a profile is selected, apply its settings
                    if (_selectedProfile != null)
                    {
                        ApplyProfileSettings(_selectedProfile);
                        
                        // Speichere den Profilnamen direkt in den Settings
                        try {
                            Properties.Settings.Default.SelectedServerProfile = _selectedProfile.Name;
                            Properties.Settings.Default.Save();
                        } catch {
                            // Error handling falls Settings nicht gefunden wird
                        }
                        
                        // Notify listeners about the profile change
                        ProfileChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        private SftpProfileManager()
        {
            LoadProfiles();
        }

        public void LoadProfiles()
        {
            try
            {
                string profilesJson = "[]";
                
                try {
                    profilesJson = Properties.Settings.Default.SftpServerProfiles;
                } catch {
                    // Error handling falls Settings nicht gefunden wird
                }
                
                if (!string.IsNullOrWhiteSpace(profilesJson))
                {
                    var loadedProfiles = JsonConvert.DeserializeObject<List<SftpServerProfile>>(profilesJson);
                    Profiles.Clear();
                    foreach (var profile in loadedProfiles)
                    {
                        Profiles.Add(profile);
                    }

                    // Select the previously selected profile if it exists
                    string selectedProfileName = "";
                    
                    try {
                        selectedProfileName = Properties.Settings.Default.SelectedServerProfile;
                    } catch {
                        // Error handling falls Settings nicht gefunden wird
                    }
                    
                    if (!string.IsNullOrWhiteSpace(selectedProfileName))
                    {
                        var profileToSelect = Profiles.FirstOrDefault(p => p.Name == selectedProfileName);
                        if (profileToSelect != null)
                        {
                            // Set selected profile directly to avoid triggering the reload on startup
                            _selectedProfile = profileToSelect;
                            OnPropertyChanged(nameof(SelectedProfile));
                            // Don't apply settings here, they will be loaded elsewhere on startup
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading server profiles: {ex.Message}", "Profile Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveProfiles()
        {
            try
            {
                var profilesJson = JsonConvert.SerializeObject(Profiles.ToList());
                
                try {
                    Properties.Settings.Default.SftpServerProfiles = profilesJson;
                    Properties.Settings.Default.Save();
                } catch {
                    // Error handling falls Settings nicht gefunden wird
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving server profiles: {ex.Message}", "Profile Saving Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public SftpServerProfile CreateProfileFromCurrentSettings()
        {
            var profile = new SftpServerProfile
            {
                Host = Properties.Settings.Default.SftpHost,
                Port = Properties.Settings.Default.SftpPort,
                Username = Properties.Settings.Default.SftpUsername,
                Password = Properties.Settings.Default.SftpPassword,
                RemotePath = Properties.Settings.Default.SftpRemotePath,
                UsePrivateKey = Properties.Settings.Default.UsePrivateKey,
                PrivateKeyPath = Properties.Settings.Default.PrivateKeyPath,
                PrivateKeyPassphrase = Properties.Settings.Default.PrivateKeyPassphrase
            };
            return profile;
        }

        public void ApplyProfileSettings(SftpServerProfile profile)
        {
            if (profile == null) return;

            Properties.Settings.Default.UseSftp = true;
            Properties.Settings.Default.SftpHost = profile.Host;
            Properties.Settings.Default.SftpPort = profile.Port;
            Properties.Settings.Default.SftpUsername = profile.Username;
            Properties.Settings.Default.SftpPassword = profile.Password;
            Properties.Settings.Default.SftpRemotePath = profile.RemotePath;
            Properties.Settings.Default.UsePrivateKey = profile.UsePrivateKey;
            Properties.Settings.Default.PrivateKeyPath = profile.PrivateKeyPath;
            Properties.Settings.Default.PrivateKeyPassphrase = profile.PrivateKeyPassphrase;
            Properties.Settings.Default.Save();
        }

        public void AddProfile(SftpServerProfile profile)
        {
            Profiles.Add(profile);
            SaveProfiles();
            SelectedProfile = profile;
        }

        public void RemoveProfile(SftpServerProfile profile)
        {
            if (profile == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete the profile '{profile.Name}'?", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                Profiles.Remove(profile);
                SaveProfiles();
                
                if (SelectedProfile == profile)
                {
                    SelectedProfile = Profiles.FirstOrDefault();
                }
            }
        }

        public void UpdateProfile(SftpServerProfile profile)
        {
            // Find the profile in the collection and update it
            var existingProfile = Profiles.FirstOrDefault(p => p.Name == profile.Name);
            if (existingProfile != null)
            {
                var index = Profiles.IndexOf(existingProfile);
                Profiles[index] = profile;
                SaveProfiles();
            }
        }

        public void SetSelectedProfileWithoutReload(SftpServerProfile profile)
        {
            if (_selectedProfile != profile)
            {
                _selectedProfile = profile;
                OnPropertyChanged(nameof(SelectedProfile));
                
                // Apply settings but don't trigger map reload
                if (_selectedProfile != null)
                {
                    ApplyProfileSettings(_selectedProfile);
                    
                    try {
                        Properties.Settings.Default.SelectedServerProfile = _selectedProfile.Name;
                        Properties.Settings.Default.Save();
                    } catch {
                        // Error handling falls Settings nicht gefunden wird
                    }
                    
                    // Note: We intentionally don't fire ProfileChanged event here
                    // to avoid map reload when setting profile from dialog
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 