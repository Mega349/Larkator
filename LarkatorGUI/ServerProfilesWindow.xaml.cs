using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace LarkatorGUI
{
    public partial class ServerProfilesWindow : Window, INotifyPropertyChanged
    {
        private bool _isProfileSelected;

        public SftpProfileManager ProfileManager => SftpProfileManager.Instance;

        public bool IsProfileSelected
        {
            get { return _isProfileSelected; }
            set
            {
                if (_isProfileSelected != value)
                {
                    _isProfileSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public ServerProfilesWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void ProfilesListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            IsProfileSelected = ProfilesListView.SelectedItem != null;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ServerProfileDialog();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                // Profile is added in the dialog
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesListView.SelectedItem is SftpServerProfile selectedProfile)
            {
                var dialog = new ServerProfileDialog(selectedProfile);
                dialog.Owner = this;
                if (dialog.ShowDialog() == true)
                {
                    // Profile is updated in the dialog
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesListView.SelectedItem is SftpServerProfile selectedProfile)
            {
                ProfileManager.RemoveProfile(selectedProfile);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // If a profile is selected, load it and trigger reload when closing
            if (ProfilesListView.SelectedItem is SftpServerProfile selectedProfile)
            {
                // Set the selected profile using the normal property to trigger reload
                SftpProfileManager.Instance.SelectedProfile = selectedProfile;
            }
            
            DialogResult = true;
            Close();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 