using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LarkatorGUI
{
    [Serializable]
    public class SftpServerProfile : INotifyPropertyChanged
    {
        private string _name;
        private string _host;
        private int _port = 22;
        private string _username;
        private string _password;
        private string _remotePath;
        private bool _usePrivateKey;
        private string _privateKeyPath;
        private string _privateKeyPassphrase;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Host
        {
            get { return _host; }
            set
            {
                if (_host != value)
                {
                    _host = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Port
        {
            get { return _port; }
            set
            {
                if (_port != value)
                {
                    _port = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Username
        {
            get { return _username; }
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged();
                }
            }
        }

        public string RemotePath
        {
            get { return _remotePath; }
            set
            {
                if (_remotePath != value)
                {
                    _remotePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool UsePrivateKey
        {
            get { return _usePrivateKey; }
            set
            {
                if (_usePrivateKey != value)
                {
                    _usePrivateKey = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PrivateKeyPath
        {
            get { return _privateKeyPath; }
            set
            {
                if (_privateKeyPath != value)
                {
                    _privateKeyPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PrivateKeyPassphrase
        {
            get { return _privateKeyPassphrase; }
            set
            {
                if (_privateKeyPassphrase != value)
                {
                    _privateKeyPassphrase = value;
                    OnPropertyChanged();
                }
            }
        }

        // Override ToString for display in UI
        public override string ToString()
        {
            return Name ?? Host;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 