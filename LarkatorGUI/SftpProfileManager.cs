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
                    System.Diagnostics.Debug.WriteLine($"LoadProfiles - JSON aus Settings geladen, Länge: {profilesJson?.Length ?? 0}");
                } catch (Exception ex) {
                    // Error handling falls Settings nicht gefunden wird
                    System.Diagnostics.Debug.WriteLine($"LoadProfiles - Fehler beim Laden aus Settings: {ex.Message}");
                }
                
                if (!string.IsNullOrWhiteSpace(profilesJson))
                {
                    // Spezielle Serialisierungseinstellungen
                    var settings = new JsonSerializerSettings 
                    { 
                        TypeNameHandling = TypeNameHandling.None
                    };
                    
                    var loadedProfiles = JsonConvert.DeserializeObject<List<SftpServerProfile>>(profilesJson, settings);
                    
                    System.Diagnostics.Debug.WriteLine($"LoadProfiles - {loadedProfiles.Count} Profile geladen");
                    
                    Profiles.Clear();
                    foreach (var profile in loadedProfiles)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - Profil: {profile.Name}, Port: {profile.Port}");
                        Profiles.Add(profile);
                    }

                    // Select the previously selected profile if it exists
                    string selectedProfileName = "";
                    
                    try {
                        selectedProfileName = Properties.Settings.Default.SelectedServerProfile;
                    } catch (Exception ex) {
                        // Error handling falls Settings nicht gefunden wird
                        System.Diagnostics.Debug.WriteLine($"LoadProfiles - Fehler beim Laden des ausgewählten Profils: {ex.Message}");
                    }
                    
                    if (!string.IsNullOrWhiteSpace(selectedProfileName))
                    {
                        var profileToSelect = Profiles.FirstOrDefault(p => p.Name == selectedProfileName);
                        if (profileToSelect != null)
                        {
                            // Set selected profile directly to avoid triggering the reload on startup
                            _selectedProfile = profileToSelect;
                            OnPropertyChanged(nameof(SelectedProfile));
                            
                            System.Diagnostics.Debug.WriteLine($"LoadProfiles - Profil ausgewählt: {profileToSelect.Name}, Port: {profileToSelect.Port}");
                            
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
                // Debug-Ausgabe vor dem Speichern
                System.Diagnostics.Debug.WriteLine($"SaveProfiles - Speichere {Profiles.Count} Profile");
                foreach (var profile in Profiles)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Profil: {profile.Name}, Port: {profile.Port}");
                }
                
                // Spezielle Serialisierungseinstellungen
                var settings = new JsonSerializerSettings 
                { 
                    TypeNameHandling = TypeNameHandling.None,
                    Formatting = Formatting.Indented
                };
                
                var profilesJson = JsonConvert.SerializeObject(Profiles.ToList(), settings);
                
                // Debug-Ausgabe nach der Serialisierung
                System.Diagnostics.Debug.WriteLine($"SaveProfiles - JSON erstellt, Länge: {profilesJson.Length}");
                
                try {
                    Properties.Settings.Default.SftpServerProfiles = profilesJson;
                    Properties.Settings.Default.Save();
                    
                    // Debug-Ausgabe nach dem Speichern
                    System.Diagnostics.Debug.WriteLine("SaveProfiles - Einstellungen erfolgreich gespeichert");
                } catch (Exception ex) {
                    // Error handling falls Settings nicht gefunden wird
                    System.Diagnostics.Debug.WriteLine($"SaveProfiles - Fehler beim Speichern in Settings: {ex.Message}");
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

            // Debug-Ausgabe vor dem Übernehmen der Einstellungen
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
            
            // Debug-Ausgabe vor dem Speichern
            System.Diagnostics.Debug.WriteLine($"ApplyProfileSettings - Nach Zuweisungen, Settings Port: {Properties.Settings.Default.SftpPort}");
            
            Properties.Settings.Default.Save();
            
            // Debug-Ausgabe nach dem Speichern
            System.Diagnostics.Debug.WriteLine($"ApplyProfileSettings - Nach Speichern, Settings Port: {Properties.Settings.Default.SftpPort}");
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
                // Debug-Ausgabe für Port
                System.Diagnostics.Debug.WriteLine($"UpdateProfile - Gefundenes Profil mit Port: {existingProfile.Port}");
                System.Diagnostics.Debug.WriteLine($"UpdateProfile - Zu aktualisieren auf Port: {profile.Port}");
                
                // Kopiere alle Eigenschaften
                existingProfile.Host = profile.Host;
                existingProfile.Port = profile.Port;  // Stelle sicher, dass der Port kopiert wird
                existingProfile.Username = profile.Username;
                existingProfile.Password = profile.Password;
                existingProfile.RemotePath = profile.RemotePath;
                existingProfile.UsePrivateKey = profile.UsePrivateKey;
                existingProfile.PrivateKeyPath = profile.PrivateKeyPath;
                existingProfile.PrivateKeyPassphrase = profile.PrivateKeyPassphrase;
                
                // Debug-Ausgabe nach Update
                System.Diagnostics.Debug.WriteLine($"UpdateProfile - Nach Update, Port: {existingProfile.Port}");
                
                // Aktualisiere die UI
                var index = Profiles.IndexOf(existingProfile);
                Profiles[index] = existingProfile;
                SaveProfiles();
                
                // Wenn dies das ausgewählte Profil ist, aktualisiere die Einstellungen
                if (SelectedProfile == existingProfile)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateProfile - Aktualisiere Einstellungen für selektiertes Profil");
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