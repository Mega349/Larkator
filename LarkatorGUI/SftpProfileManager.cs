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
                        
                        // Save profile name directly in settings
                        try {
                            Properties.Settings.Default.SelectedServerProfile = _selectedProfile.Name;
                            Properties.Settings.Default.Save();
                        } catch {
                            // Error handling if settings not found
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
                    System.Diagnostics.Debug.WriteLine($"LoadProfiles - JSON loaded from settings, length: {profilesJson?.Length ?? 0}");
                } catch (Exception ex) {
                    // Error handling if settings not found
                    System.Diagnostics.Debug.WriteLine($"LoadProfiles - Error loading from settings: {ex.Message}");
                }
                
                if (!string.IsNullOrWhiteSpace(profilesJson))
                {
                    // Special serialization settings
                    var settings = new JsonSerializerSettings 
                    { 
                        TypeNameHandling = TypeNameHandling.None
                    };
                    
                    var loadedProfiles = JsonConvert.DeserializeObject<List<SftpServerProfile>>(profilesJson, settings);
                    
                    System.Diagnostics.Debug.WriteLine($"LoadProfiles - {loadedProfiles.Count} profiles loaded");
                    
                    Profiles.Clear();
                    foreach (var profile in loadedProfiles)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - Profile: {profile.Name}, Port: {profile.Port}");
                        Profiles.Add(profile);
                    }

                    // Select the previously selected profile if it exists
                    string selectedProfileName = "";
                    
                    try {
                        selectedProfileName = Properties.Settings.Default.SelectedServerProfile;
                    } catch (Exception ex) {
                        // Error handling if settings not found
                        System.Diagnostics.Debug.WriteLine($"LoadProfiles - Error loading selected profile: {ex.Message}");
                    }
                    
                    if (!string.IsNullOrWhiteSpace(selectedProfileName))
                    {
                        var profileToSelect = Profiles.FirstOrDefault(p => p.Name == selectedProfileName);
                        if (profileToSelect != null)
                        {
                            // Set selected profile directly to avoid triggering the reload on startup
                            _selectedProfile = profileToSelect;
                            OnPropertyChanged(nameof(SelectedProfile));
                            
                            System.Diagnostics.Debug.WriteLine($"LoadProfiles - Profile selected: {profileToSelect.Name}, Port: {profileToSelect.Port}");
                            
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
                // Debug output before saving
                System.Diagnostics.Debug.WriteLine($"SaveProfiles - Saving {Profiles.Count} profiles");
                foreach (var profile in Profiles)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Profile: {profile.Name}, Port: {profile.Port}");
                }
                
                // Special serialization settings
                var settings = new JsonSerializerSettings 
                { 
                    TypeNameHandling = TypeNameHandling.None,
                    Formatting = Formatting.Indented
                };
                
                var profilesJson = JsonConvert.SerializeObject(Profiles.ToList(), settings);
                
                // Debug output after serialization
                System.Diagnostics.Debug.WriteLine($"SaveProfiles - JSON created, length: {profilesJson.Length}");
                
                try {
                    Properties.Settings.Default.SftpServerProfiles = profilesJson;
                    Properties.Settings.Default.Save();
                    
                    // Debug output after saving
                    System.Diagnostics.Debug.WriteLine("SaveProfiles - Settings saved successfully");
                } catch (Exception ex) {
                    // Error handling if settings not found
                    System.Diagnostics.Debug.WriteLine($"SaveProfiles - Error saving to settings: {ex.Message}");
                    throw;
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

            // Debug output before applying settings
            System.Diagnostics.Debug.WriteLine($"ApplyProfileSettings - Profile: {profile.Name}, Profile Port: {profile.Port}");
            System.Diagnostics.Debug.WriteLine($"ApplyProfileSettings - Current Settings Port: {Properties.Settings.Default.SftpPort}");

            Properties.Settings.Default.UseSftp = true;
            Properties.Settings.Default.SftpHost = profile.Host;
            Properties.Settings.Default.SftpPort = profile.Port;
            Properties.Settings.Default.SftpUsername = profile.Username;
            Properties.Settings.Default.SftpPassword = profile.Password;
            Properties.Settings.Default.SftpRemotePath = profile.RemotePath;
            Properties.Settings.Default.UsePrivateKey = profile.UsePrivateKey;
            Properties.Settings.Default.PrivateKeyPath = profile.PrivateKeyPath;
            Properties.Settings.Default.PrivateKeyPassphrase = profile.PrivateKeyPassphrase;
            
            // Debug output before saving
            System.Diagnostics.Debug.WriteLine($"ApplyProfileSettings - After assignments, Settings Port: {Properties.Settings.Default.SftpPort}");
            
            Properties.Settings.Default.Save();
            
            // Debug output after saving
            System.Diagnostics.Debug.WriteLine($"ApplyProfileSettings - After saving, Settings Port: {Properties.Settings.Default.SftpPort}");
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
            if (profile == null) return;
            
            // Debug-Ausgabe für Port
            System.Diagnostics.Debug.WriteLine($"UpdateProfile - Start mit Port: {profile.Port}");

            // Find the profile in the collection and update it
            var existingProfile = Profiles.FirstOrDefault(p => p.Name == profile.Name);
            if (existingProfile != null)
            {
                // Debug output for port
                System.Diagnostics.Debug.WriteLine($"UpdateProfile - Found profile with port: {existingProfile.Port}");
                System.Diagnostics.Debug.WriteLine($"UpdateProfile - Updating to port: {profile.Port}");
                
                // Copy all properties
                existingProfile.Host = profile.Host;
                existingProfile.Port = profile.Port;  // Ensure port is copied
                existingProfile.Username = profile.Username;
                existingProfile.Password = profile.Password;
                existingProfile.RemotePath = profile.RemotePath;
                existingProfile.UsePrivateKey = profile.UsePrivateKey;
                existingProfile.PrivateKeyPath = profile.PrivateKeyPath;
                existingProfile.PrivateKeyPassphrase = profile.PrivateKeyPassphrase;
                
                // Debug output after update
                System.Diagnostics.Debug.WriteLine($"UpdateProfile - After update, port: {existingProfile.Port}");
                
                // Update UI
                var index = Profiles.IndexOf(existingProfile);
                Profiles[index] = existingProfile;
                SaveProfiles();
                
                // If this is the selected profile, update settings
                if (SelectedProfile == existingProfile)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateProfile - Updating settings for selected profile");
                    ApplyProfileSettings(existingProfile);
                }
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
                        // Error handling if settings not found
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