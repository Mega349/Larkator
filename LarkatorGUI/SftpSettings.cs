using System;
using System.IO;

namespace LarkatorGUI
{
    /// <summary>
    /// SFTP Connection Settings
    /// </summary>
    public class SftpSettings
    {
        public string Host { get; set; }
        public int Port { get; set; } = 22;
        public string Username { get; set; }
        public string Password { get; set; }
        public string RemotePath { get; set; }
        public bool UseSftp { get; set; } = false;
        public bool UsePrivateKey { get; set; } = false;
        public string PrivateKeyPath { get; set; }
        public string PrivateKeyPassphrase { get; set; }

        public bool IsValid()
        {
            // Wenn UsePrivateKey aktiviert ist, muss ein PrivateKeyPath vorhanden sein
            bool hasValidPrivateKey = !UsePrivateKey || (UsePrivateKey && !string.IsNullOrEmpty(PrivateKeyPath));
            
            // Wenn UsePrivateKey deaktiviert ist, muss ein Passwort vorhanden sein
            bool hasValidPassword = !string.IsNullOrEmpty(Password);
            
            // Gültige Authentifizierung, wenn entweder privater Schlüssel oder Passwort vorhanden ist
            bool hasValidAuth = (hasValidPrivateKey && UsePrivateKey) || (!UsePrivateKey && hasValidPassword);
            
            // Debugging Information
            System.Diagnostics.Debug.WriteLine($"SFTP Configuration validation:");
            System.Diagnostics.Debug.WriteLine($"UseSftp: {UseSftp}");
            System.Diagnostics.Debug.WriteLine($"Host: {(string.IsNullOrEmpty(Host) ? "Missing" : "OK")}");
            System.Diagnostics.Debug.WriteLine($"Username: {(string.IsNullOrEmpty(Username) ? "Missing" : "OK")}");
            System.Diagnostics.Debug.WriteLine($"RemotePath: {(string.IsNullOrEmpty(RemotePath) ? "Missing" : "OK")}");
            System.Diagnostics.Debug.WriteLine($"UsePrivateKey: {UsePrivateKey}");
            System.Diagnostics.Debug.WriteLine($"HasValidPrivateKey: {hasValidPrivateKey}");
            System.Diagnostics.Debug.WriteLine($"HasValidPassword: {hasValidPassword}");
            System.Diagnostics.Debug.WriteLine($"HasValidAuth: {hasValidAuth}");
            System.Diagnostics.Debug.WriteLine($"PrivateKeyPath: {(string.IsNullOrEmpty(PrivateKeyPath) ? "Missing" : "OK")}");
            
            return UseSftp && 
                   !string.IsNullOrEmpty(Host) && 
                   !string.IsNullOrEmpty(Username) && 
                   !string.IsNullOrEmpty(RemotePath) && 
                   hasValidAuth;
        }
    }
} 