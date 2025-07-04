using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

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
        private bool _useRcon;
        private string _rconHost;
        private int _rconPort = 27020;
        private string _rconPassword;

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
                    
                    // Automatisch auch den RCON-Host aktualisieren
                    RconHost = value;
                }
            }
        }

        [JsonProperty(TypeNameHandling = TypeNameHandling.None)]
        public int Port
        {
            get { return _port; }
            set
            {
                // Debug-Ausgabe für Port
                System.Diagnostics.Debug.WriteLine($"SftpServerProfile - Setting port to: {value}");
                
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

        public bool UseRcon
        {
            get { return _useRcon; }
            set
            {
                if (_useRcon != value)
                {
                    _useRcon = value;
                    OnPropertyChanged();
                }
            }
        }

        public string RconHost
        {
            get { return _rconHost; }
            set
            {
                if (_rconHost != value)
                {
                    _rconHost = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty(TypeNameHandling = TypeNameHandling.None)]
        public int RconPort
        {
            get { return _rconPort; }
            set
            {
                if (_rconPort != value)
                {
                    _rconPort = value;
                    OnPropertyChanged();
                }
            }
        }

        public string RconPassword
        {
            get { return _rconPassword; }
            set
            {
                if (_rconPassword != value)
                {
                    _rconPassword = value;
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