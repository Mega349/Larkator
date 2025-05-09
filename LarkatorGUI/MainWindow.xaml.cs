using FastMember;

using GongSolutions.Wpf.DragDrop;

using Larkator.Common;

using Newtonsoft.Json;

using Renci.SshNet;

using SavegameToolkitAdditions;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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
            return UseSftp && !string.IsNullOrEmpty(Host) && 
                  !string.IsNullOrEmpty(Username) && 
                  !string.IsNullOrEmpty(RemotePath) && 
                  ((!UsePrivateKey && !string.IsNullOrEmpty(Password)) || 
                   (UsePrivateKey && !string.IsNullOrEmpty(PrivateKeyPath)));
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDropTarget, INotifyPropertyChanged
    {
        private const string DEV_STRING = "DEVELOPMENT";

        public ObservableCollection<SearchCriteria> ListSearches { get; } = new ObservableCollection<SearchCriteria>();
        public Collection<DinoViewModel> ListResults { get; } = new Collection<DinoViewModel>();
        public List<string> AllSpecies { get { return arkReader.AllSpecies; } }

        public string ApplicationVersion
        {
            get
            {
                return appVersion;
            }
        }

        public string WindowTitle { get { return $"{Properties.Resources.ProgramName} {ApplicationVersion}"; } }
        public MapCalibration MapCalibration { get; private set; }
        public ImageSource MapImage { get; private set; }

        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        public string StatusText
        {
            get { return (string)GetValue(StatusTextProperty); }
            set { SetValue(StatusTextProperty, value); }
        }

        public string StatusDetailText
        {
            get { return (string)GetValue(StatusDetailTextProperty); }
            set { SetValue(StatusDetailTextProperty, value); }
        }

        public SearchCriteria NewSearch
        {
            get { return (SearchCriteria)GetValue(NewSearchProperty); }
            set { SetValue(NewSearchProperty, value); }
        }

        public bool CreateSearchAvailable
        {
            get { return (bool)GetValue(CreateSearchAvailableProperty); }
            set { SetValue(CreateSearchAvailableProperty, value); }
        }

        public bool NewSearchActive
        {
            get { return (bool)GetValue(NewSearchActiveProperty); }
            set { SetValue(NewSearchActiveProperty, value); }
        }

        public bool ShowHunt
        {
            get { return (bool)GetValue(ShowHuntProperty); }
            set { SetValue(ShowHuntProperty, value); }
        }

        public bool ShowTames
        {
            get { return (bool)GetValue(ShowTamesProperty); }
            set { SetValue(ShowTamesProperty, value); }
        }

        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        public int ResultMatchingCount
        {
            get { return (int)GetValue(ResultMatchingCountProperty); }
            set { SetValue(ResultMatchingCountProperty, value); }
        }

        public int ResultTotalCount
        {
            get { return (int)GetValue(ResultTotalCountProperty); }
            set { SetValue(ResultTotalCountProperty, value); }
        }

        public bool ShowCounts
        {
            get { return (bool)GetValue(ShowCountsProperty); }
            set { SetValue(ShowCountsProperty, value); }
        }

        public bool IsDevMode
        {
            get { return (bool)GetValue(IsDevModeProperty); }
            set { SetValue(IsDevModeProperty, value); }
        }

        public bool AutoReload
        {
            get { return (bool)GetValue(AutoReloadProperty); }
            set { SetValue(AutoReloadProperty, value); }
        }

        public static readonly DependencyProperty IsDevModeProperty =
            DependencyProperty.Register("IsDevMode", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public static readonly DependencyProperty ShowCountsProperty =
            DependencyProperty.Register("ShowCounts", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public static readonly DependencyProperty ResultTotalCountProperty =
            DependencyProperty.Register("ResultTotalCount", typeof(int), typeof(MainWindow), new PropertyMetadata(0));

        public static readonly DependencyProperty ResultMatchingCountProperty =
            DependencyProperty.Register("ResultMatchingCount", typeof(int), typeof(MainWindow), new PropertyMetadata(0));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register("SearchText", typeof(string), typeof(MainWindow), new PropertyMetadata(""));

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(MainWindow), new PropertyMetadata(null));

        public static readonly DependencyProperty ShowTamesProperty =
            DependencyProperty.Register("ShowTames", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public static readonly DependencyProperty ShowHuntProperty =
            DependencyProperty.Register("ShowHunt", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

        public static readonly DependencyProperty NewSearchActiveProperty =
            DependencyProperty.Register("NewSearchActive", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public static readonly DependencyProperty CreateSearchAvailableProperty =
            DependencyProperty.Register("CreateSearchAvailable", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

        public static readonly DependencyProperty NewSearchProperty =
            DependencyProperty.Register("NewSearch", typeof(SearchCriteria), typeof(MainWindow), new PropertyMetadata(null));

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public static readonly DependencyProperty StatusTextProperty =
            DependencyProperty.Register("StatusText", typeof(string), typeof(MainWindow), new PropertyMetadata(""));

        public static readonly DependencyProperty StatusDetailTextProperty =
            DependencyProperty.Register("StatusDetailText", typeof(string), typeof(MainWindow), new PropertyMetadata(""));

        public static readonly DependencyProperty AutoReloadProperty =
            DependencyProperty.Register("AutoReload", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        ArkReader arkReader;
        FileSystemWatcher fileWatcher;
        DispatcherTimer reloadTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        private List<MapCalibration> mapCalibrations;
        private readonly string appVersion;
        readonly List<bool?> nullableBoolValues = new List<bool?> { null, false, true };
        DateTime nameSearchTime;
        string nameSearchArg;
        bool nameSearchRunning;
        private string lastArk;
        private DebounceDispatcher refreshSearchesTimer = new DebounceDispatcher();
        private DebounceDispatcher settingsSaveTimer = new DebounceDispatcher();

        public SftpSettings SftpConfig { get; private set; } = new SftpSettings();
        
        public SftpProfileManager SftpProfileManager => SftpProfileManager.Instance;

        public MainWindow()
        {
            ValidateWindowPositionAndSize();

            arkReader = new ArkReader();

            appVersion = CalculateApplicationVersion();
            
            // Set initial dev mode state (restore old behavior)
            IsDevMode = (appVersion == DEV_STRING);

            LoadCalibrations();
            DiscoverCalibration();

            // Load SFTP settings
            LoadSftpSettings();
            
            // Set auto reload from settings
            AutoReload = Properties.Settings.Default.AutoReload;

            DataContext = this;
            this.MouseDown += new MouseButtonEventHandler(window_MouseDown);

            InitializeComponent();

            Dispatcher.Invoke(async () => {
                await Task.Yield();
                Properties.Settings.Default.MainWindowWidth = CalculateWidthFromHeight((int)Math.Round(Properties.Settings.Default.MainWindowHeight));
            }, DispatcherPriority.Loaded);

            LoadSavedSearches();
            SetupFileWatcher();

            // Add keyboard shortcuts
            var cmdThrowExceptionAndExit = new RoutedCommand();
            cmdThrowExceptionAndExit.InputGestures.Add(new KeyGesture(Key.F2, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(cmdThrowExceptionAndExit, (o, e) => Dev_GenerateException_Click(null, null)));
            
            // Add shortcut to toggle dev mode with Ctrl+D
            var cmdToggleDevMode = new RoutedCommand();
            cmdToggleDevMode.InputGestures.Add(new KeyGesture(Key.D, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(cmdToggleDevMode, (o, e) => ToggleDevMode()));

            DependencyPropertyDescriptor.FromProperty(SearchTextProperty, typeof(MainWindow)).AddValueChanged(DataContext, (s, e) => TriggerNameSearch());
        }

        private static string CalculateApplicationVersion()
        {
            try
            {
                return ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            catch (InvalidDeploymentException)
            {
#if DEBUG
                return DEV_STRING;
#else
                // Version aus der Assembly lesen für Release-Builds
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version.ToString();
#endif
            }
        }

        private void SetupFileWatcher()
        {
            if (fileWatcher != null)
            {
                fileWatcher.EnableRaisingEvents = false;
                fileWatcher.Dispose();
            }

            if (!AutoReload)
                return;

            if (SftpConfig.UseSftp)
                return; // No file watcher for SFTP

            fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(Properties.Settings.Default.SaveFile));
            fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime;
            fileWatcher.Renamed += FileWatcher_Changed;
            fileWatcher.Changed += FileWatcher_Changed;
            fileWatcher.Created += FileWatcher_Changed;
            fileWatcher.EnableRaisingEvents = true;
            reloadTimer.Interval = TimeSpan.FromMilliseconds(Properties.Settings.Default.ConvertDelay);
            reloadTimer.Tick += ReloadTimer_Tick;
        }

        private void LoadCalibrations()
        {
            mapCalibrations = JsonConvert.DeserializeObject<List<MapCalibration>>(Properties.Resources.calibrationsJson);
        }

        private void DiscoverCalibration()
        {
            // Bestimme den zu verwendenden Dateinamen basierend auf Lokal- oder Remote-Pfad
            string filename;
            
            if (SftpConfig.UseSftp && !string.IsNullOrEmpty(SftpConfig.RemotePath))
            {
                // Bei SFTP den Remote-Pfad verwenden
                filename = Path.GetFileNameWithoutExtension(SftpConfig.RemotePath);
            }
            else
            {
                // Bei lokaler Datei den Standard-Pfad verwenden
                filename = Path.GetFileNameWithoutExtension(Properties.Settings.Default.SaveFile);
            }
            
            var best = mapCalibrations.FirstOrDefault(cal => filename.StartsWith(cal.Filename));
            if (best == null)
            {
                StatusText = "Warning: Unable to determine map from filename - defaulting to The Island";
                MapCalibration = mapCalibrations.Single(cal => cal.Filename == "TheIsland");
            }
            else
            {
                MapCalibration = best;
            }

            var imgFilename = $"pack://application:,,,/imgs/map_{MapCalibration.Filename}.jpg";
            MapImage = (new ImageSourceConverter()).ConvertFromString(imgFilename) as ImageSource;
            if (image != null)
                image.Source = MapImage;

            arkReader.MapCalibration = MapCalibration;
        }

        private void ValidateWindowPositionAndSize()
        {
            var settings = Properties.Settings.Default;

            if (settings.MainWindowLeft <= -10000 || settings.MainWindowTop <= -10000)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            if (settings.MainWindowWidth < 0 || settings.MainWindowHeight < 0)
            {
                settings.MainWindowWidth = (double)settings.Properties["MainWindowWidth"].DefaultValue;
                settings.MainWindowHeight = (double)settings.Properties["MainWindowHeight"].DefaultValue;
                settings.Save();
            }
        }

        private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath?.ToLowerInvariant() != Properties.Settings.Default.SaveFile?.ToLowerInvariant())
                return;

            Dispatcher.Invoke(() => {
                StatusText = "Detected change to saved ARK...";
                StatusDetailText = "...waiting";
            });

            // Cancel any existing timer to ensure we're not called multiple times
            if (reloadTimer.IsEnabled)
                reloadTimer.Stop();

            reloadTimer.Start();
        }

        private async void ReloadTimer_Tick(object sender, EventArgs e)
        {
            reloadTimer.Stop();
            await Dispatcher.InvokeAsync(() => ReReadArk(), DispatcherPriority.Background);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // First load species data
            await UpdateArkToolsData();
            
            // Load profiles before first reading of the ARK file
            SftpProfileManager.Instance.LoadProfiles();
            
            // Now read the ARK file (only once)
            await ReReadArk();
        }

        private async Task UpdateArkToolsData()
        {
            StatusText = "Fetching latest species data...";

            try
            {
                var targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Properties.Resources.ProgramName);
                var targetFile = Path.Combine(targetFolder, "ark-data.json");
                if (!Directory.Exists(targetFolder))
                    Directory.CreateDirectory(targetFolder);

                var fetchOkay = await FetchArkData(targetFile);
                var loadOkay = await LoadArkData(targetFile);

                if (!loadOkay)
                    throw new ApplicationException("No species data available");
                if (fetchOkay)
                    StatusText = "Species data loaded";
                else
                    StatusText = "Using old species data - offline?";
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to fetch species data - Larkator cannot function");
                Environment.Exit(3);
            }
        }

        private async Task<bool> FetchArkData(string targetFile)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    if (File.Exists(targetFile))
                        client.DefaultRequestHeaders.IfModifiedSince = new FileInfo(targetFile).LastWriteTimeUtc;

                    using (var response = await client.GetAsync(Properties.Resources.ArkDataURL))
                    {
                        // Throw exception on failure
                        response.EnsureSuccessStatusCode();

                        // Don't do anything if the file hasn't changed
                        if (response.StatusCode == HttpStatusCode.NotModified)
                            return true;

                        // Write the data to file
                        using (var fileWriter = File.OpenWrite(targetFile))
                        {
                            await response.Content.CopyToAsync(fileWriter);
                        }
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> LoadArkData(string targetFile)
        {
            return await Task.Run<bool>(() => {
                try
                {
                    arkReader.SetArkData(ArkDataReader.ReadFromFile(targetFile));
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }

        private void Searches_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCurrentSearch();
        }

        private void RemoveSearch_Click(object sender, RoutedEventArgs e)
        {
            if (ShowTames)
                return;

            var button = sender as Button;
            if (button?.DataContext is SearchCriteria search)
                ListSearches.Remove(search);
            UpdateCurrentSearch();

            MarkSearchesChanged();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchText = "";

            TriggerNameSearch(true);
        }

        private void Search_Click(object sender, MouseButtonEventArgs e)
        {
            TriggerNameSearch(true);
        }

        private void SearchText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                TriggerNameSearch(true);
        }

        private void MapPin_Click(object sender, MouseButtonEventArgs ev)
        {
            if (sender is FrameworkElement e && e.DataContext is DinoViewModel dvm)
            {
                dvm.Highlight = !dvm.Highlight;
                if (dvm.Highlight)
                    resultsList.ScrollIntoView(dvm);
            }
        }

        private void CopyTeleport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Parent is ContextMenu cm && cm.PlacementTarget is DataGridRow dgr && dgr.DataContext is DinoViewModel dvm)
            {
                var z = dvm.Dino.Location.Z + Properties.Settings.Default.TeleportHeightOffset;
                var clipboard = "cheat SetPlayerPos ";
                clipboard += System.FormattableString.Invariant($"{dvm.Dino.Location.X:0.00} {dvm.Dino.Location.Y:0.00} {z:0.00}");
                if (Properties.Settings.Default.TeleportFly)
                {
                    clipboard += " | cheat fly";
                    if (Properties.Settings.Default.TeleportGhost)
                        clipboard += " | cheat ghost";
                }

                try
                {
                    Clipboard.SetText(clipboard, TextDataFormat.Text);
                }
                catch (Exception)
                {
                    MessageBox.Show("Failed to set clipboard - is Larkator sandboxed or run as a different user?", "Error setting clipboard", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    StatusText = "Failed to set clipboard!";
                    return;
                }

                StatusText = "Copied: " + clipboard;
            }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            // Handler to maintain window aspect ratio
            WindowAspectRatio.Register((Window)sender, CalculateWidthFromHeight);
        }

        private int CalculateWidthFromHeight(int height)
        {
            return (int)Math.Round(height
                - statusPanel.ActualHeight
                - 2 * SystemParameters.ResizeFrameHorizontalBorderHeight
                - SystemParameters.WindowCaptionHeight
                + leftPanel.ActualWidth + rightPanel.ActualWidth
                + 2 * SystemParameters.ResizeFrameVerticalBorderWidth);
        }

        private void CreateSearch_Click(object sender, RoutedEventArgs e)
        {
            NewSearch = new SearchCriteria();
            NewSearchActive = true;
            CreateSearchAvailable = false;

            speciesCombo.ItemsSource = arkReader.AllSpecies;
            groupsCombo.ItemsSource = ListSearches.Select(sc => sc.Group).Distinct().OrderBy(g => g).ToArray();
            groupsCombo.SelectedItem = Properties.Settings.Default.LastGroup;
            NewSearch.GroupSearch = Properties.Settings.Default.GroupSearch;
        }

        private void Dev_Calibration_Click(object sender, MouseButtonEventArgs e)
        {
            var win = new CalibrationWindow(new Calibration { Bounds = new Bounds(), Filename = MapCalibration.Filename });
            win.ShowDialog();
        }

        private void Dev_GenerateException_Click(object sender, MouseButtonEventArgs e)
        {
            throw new ApplicationException("Dummy unhandled exception");
        }

        private void Dev_RemoveSettings_Click(object sender, MouseButtonEventArgs e)
        {
            var result = MessageBox.Show(this, "Are you sure you wish to reset your options and force the application to exit?", "Unrecoverable action", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
            if (result != MessageBoxResult.OK)
                return;

            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();
            Environment.Exit(0);
        }

        private void Dev_DummyData_Click(object sender, MouseButtonEventArgs e)
        {
            var dummyData = new Dino[] {
                new Dino { Location=new Position{ Lat=10,Lon=10 }, Type="Testificate", Name="10,10" },
                new Dino { Location=new Position{ Lat=90,Lon=10 }, Type="Testificate", Name="90,10" },
                new Dino { Location=new Position{ Lat=10,Lon=90 }, Type="Testificate", Name="10,90" },
                new Dino { Location=new Position{ Lat=90,Lon=90 }, Type="Testificate", Name="90,90" },
                new Dino { Location=new Position{ Lat=50,Lon=50 }, Type="Testificate", Name="50,50" },
            };

            var rnd = new Random();
            foreach (var result in dummyData)
            {
                result.Id = (ulong)rnd.Next();
                DinoViewModel vm = new DinoViewModel(result) { Color = Colors.Green };
                ListResults.Add(vm);
            }

            ((CollectionViewSource)Resources["OrderedResults"]).View.Refresh();
        }

        private void Dev_CalibrationData_Click(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.Invoke(GenerateCalibrationPoints);
        }

        private async Task GenerateCalibrationPoints()
        {
            IsLoading = true;
            try
            {
                StatusDetailText = "...converting";
                StatusText = "Processing saved ARK (for calibration)";

                var boxes = await arkReader.PerformCalibrationRead(Properties.Settings.Default.SaveFile);

                if (boxes.Count == 0)
                {
                    MessageBox.Show(@"Map calibration requires storage boxes named 'Calibration: XX.X, YY.Y', " +
                        "where XX.X and YY.Y are read from the GPS when standing on top of the box. " +
                        "At least 4 are required for a calculation but 16+ are recommended!",
                        "Calibration Boxes", MessageBoxButton.OK, MessageBoxImage.Information);

                    return;
                }

                var rnd = new Random();
                foreach (var (pos, name) in boxes)
                {
                    var dino = new Dino { Location = pos, Type = "Calibration", Name = name, Id = (ulong)rnd.Next() };
                    var vm = new DinoViewModel(dino) { Color = Colors.Blue };
                    ListResults.Add(vm);
                }

                ((CollectionViewSource)Resources["OrderedResults"]).View.Refresh();

                StatusText = "ARK processing completed";
                StatusDetailText = $"{boxes.Count} valid calibration boxes located";

                if (boxes.Count >= 4)
                {
                    var ((xO, xD, xC), (yO, yD, yC)) = CalculateCalibration(boxes.Select(p => p.pos).ToArray());

                    var warning = (xC < 0.99 || yC < 0.99) ? "\nWARNING: Correlation is poor - add more boxes!\n" : "";
                    var result = MessageBox.Show("UE->LatLon conversion...\n" +
                                            "\n" +
                                            $"X: {xO:F2} + x / {xD:F3}  (correlation {xC:F5})\n" +
                                            $"Y: {yO:F2} + y / {yD:F3}  (correlation {yC:F5})\n" +
                                            warning +
                                            "\nOpen Calibration window with these presets?", "Calibration Box Results", MessageBoxButton.YesNo);

                    if (MessageBoxResult.Yes == result)
                    {
                        var win = new CalibrationWindow(new Calibration
                        {
                            Bounds = new Bounds(),
                            Filename = MapCalibration.Filename,
                            LonOffset = xO,
                            LonDivisor = xD,
                            LatOffset = yO,
                            LatDivisor = yD,
                        });
                        Dispatcher.Invoke(() => win.ShowDialog());
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = "ARK processing failed";
                StatusDetailText = "";
                MessageBox.Show(ex.Message, "Savegame Read Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public static ((double xOffset, double xDiv, double xCorr), (double yOffset, double yDiv, double yCorr)) CalculateCalibration(Position[] inputs)
        {
            // Perform linear regression on the values for best fit, separately for X and Y
            double[] xValues = inputs.Select(i => i.X).ToArray();
            double[] yValues = inputs.Select(i => i.Y).ToArray();
            double[] lonValues = inputs.Select(i => i.Lon).ToArray();
            double[] latValues = inputs.Select(i => i.Lat).ToArray();
            var (xOffset, xMult) = LinearRegression.Fit(xValues, lonValues);
            var (yOffset, yMult) = LinearRegression.Fit(yValues, latValues);
            var xCorr = LinearRegression.RSquared(xValues.Select(x => xOffset + xMult * x).ToArray(), lonValues);
            var yCorr = LinearRegression.RSquared(yValues.Select(y => yOffset + yMult * y).ToArray(), latValues);

            return ((xOffset, 1 / xMult, xCorr), (yOffset, 1 / yMult, yCorr));
        }

        private void LoadSavedSearches()
        {
            if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.SavedSearches))
            {
                Collection<SearchCriteria> searches;
                try
                {
                    searches = JsonConvert.DeserializeObject<Collection<SearchCriteria>>(Properties.Settings.Default.SavedSearches);
                    
                    // Zeilenumbrüche aus Speziesnamen entfernen
                    foreach (var search in searches)
                    {
                        if (!string.IsNullOrEmpty(search.Species))
                        {
                            // Entferne alle Arten von Zeilenumbrüchen und trimme Leerzeichen
                            search.Species = search.Species.Replace("\r\n", " ")
                                                .Replace("\n", " ")
                                                .Replace("\r", " ")
                                                .Replace("\t", " "); // Tabs ersetzen
                            
                            // Mehrfache Leerzeichen durch ein einzelnes ersetzen (wiederhole bis keine Änderungen mehr)
                            string previous;
                            do {
                                previous = search.Species;
                                search.Species = search.Species.Replace("  ", " ");
                            } while (previous != search.Species);
                            
                            search.Species = search.Species.Trim();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception reading saved searches: " + e.ToString());
                    return;
                }

                ListSearches.Clear();
                foreach (var search in searches)
                    ListSearches.Add(search);
            }
        }

        private void SaveSearch_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(NewSearch.Species))
                return;

            // Reinigen des Artennamens von Zeilenumbrüchen
            NewSearch.Species = NewSearch.Species.Replace("\r\n", " ")
                                .Replace("\n", " ")
                                .Replace("\r", " ")
                                .Replace("\t", " "); // Tabs ersetzen
            
            // Mehrfache Leerzeichen durch ein einzelnes ersetzen (wiederhole bis keine Änderungen mehr)
            string previous;
            do {
                previous = NewSearch.Species;
                NewSearch.Species = NewSearch.Species.Replace("  ", " ");
            } while (previous != NewSearch.Species);
            
            NewSearch.Species = NewSearch.Species.Trim();

            List<String> NewSearchList = new List<String>(AllSpecies.Where(species => species.Contains(NewSearch.Species)));
            SearchCriteria tempSearch;
            int order = 100;

            //If we lose our selection default back to Shopping List
            try
            {
                Properties.Settings.Default.LastGroup = groupsCombo.Text;
            }
            catch
            {
                Properties.Settings.Default.LastGroup = "Shopping List";
            }
            //Set and save property
            Properties.Settings.Default.GroupSearch = NewSearch.GroupSearch;
            Properties.Settings.Default.Save();

            if (NewSearchList.Count == 0 && !NewSearch.GroupSearch) // No matches
            { //Trigger default values so the user knows we did search to match
                NewSearch = null;
                tempSearch = null;
                NewSearchActive = false;
                CreateSearchAvailable = true;
                return;
            }
            ObservableCollection<SearchCriteria> tempListSearch = new ObservableCollection<SearchCriteria>(ListSearches.Where(sc => sc.Group == (String)groupsCombo.SelectedValue));
            if (tempListSearch.Count > 0)
            {
                order = (int)ListSearches.Where(sc => sc.Group == NewSearch.Group).Max(sc => sc.Order) + 100;
            }
            //Check for group based search
            if (NewSearch.GroupSearch)
            {
                tempSearch = new SearchCriteria(NewSearch);
                tempSearch.Species = NewSearch.Species;
                tempSearch.Order = order; //Sort order
                tempSearch.GroupSearch = NewSearch.GroupSearch;
                ListSearches.Add(tempSearch);
            }
            else
            {
                try
                {
                    foreach (String newDino in NewSearchList)
                    {
                        if (tempListSearch.Count == 0 || tempListSearch.Where(dino => dino.Species == newDino).Count() == 0)
                        {
                            tempSearch = new SearchCriteria(NewSearch);
                            // Reinigen des gefundenen Artennamens
                            string cleanedSpecies = newDino;
                            if (!string.IsNullOrEmpty(cleanedSpecies))
                            {
                                cleanedSpecies = cleanedSpecies.Replace("\r\n", " ")
                                            .Replace("\n", " ")
                                            .Replace("\r", " ")
                                            .Replace("\t", " "); // Tabs ersetzen
                                
                                // Mehrfache Leerzeichen durch ein einzelnes ersetzen (wiederhole bis keine Änderungen mehr)
                                string previous2;
                                do {
                                    previous2 = cleanedSpecies;
                                    cleanedSpecies = cleanedSpecies.Replace("  ", " ");
                                } while (previous2 != cleanedSpecies);
                                
                                cleanedSpecies = cleanedSpecies.Trim();
                            }
                            tempSearch.Species = cleanedSpecies;
                            tempSearch.Order = order;
                            tempSearch.GroupSearch = NewSearch.GroupSearch;
                            ListSearches.Add(tempSearch);
                            order += 100;
                        }
                    }
                }
                catch (InvalidOperationException) // no entries for .Max - ignore
                { }
            }


            NewSearch = null;
            tempSearch = null;
            NewSearchActive = false;
            CreateSearchAvailable = true;

            MarkSearchesChanged();
        }

        private void CloseNewSearch_Click(object sender, RoutedEventArgs e)
        {
            NewSearchActive = false;
            CreateSearchAvailable = true;
        }

        private void ShowTames_Click(object sender, MouseButtonEventArgs e)
        {
            ShowTames = true;
            ShowHunt = false;
            NewSearchActive = false;
            CreateSearchAvailable = false;

            ShowTameSearches();
        }

        private void ShowTheHunt_Click(object sender, MouseButtonEventArgs e)
        {
            ShowTames = false;
            ShowHunt = true;
            NewSearchActive = false;
            CreateSearchAvailable = true;

            ShowWildSearches();
        }

        private void Settings_Click(object sender, MouseButtonEventArgs e)
        {
            var settings = new SettingsWindow();
            settings.ShowDialog();

            OnSettingsChanged();
        }

        private async void Refresh_Click(object sender, MouseButtonEventArgs e)
        {
            // Update the map image first
            DiscoverCalibration();
            
            // Then reload the ARK data
            await ReReadArk();
        }

        private void AdjustableInteger_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var tb = (TextBlock)sender;
            var diff = Math.Sign(e.Delta) * Properties.Settings.Default.LevelStep;
            var bexpr = tb.GetBindingExpression(TextBlock.TextProperty);
            var accessor = TypeAccessor.Create(typeof(SearchCriteria));
            var value = (int?)accessor[bexpr.ResolvedSource, bexpr.ResolvedSourcePropertyName];
            if (value.HasValue)
            {
                value = value + diff;
                if (value < 0 || value > Properties.Settings.Default.MaxLevel)
                    value = null;
            }
            else
            {
                value = (diff > 0) ? 0 : Properties.Settings.Default.MaxLevel;
            }

            accessor[bexpr.ResolvedSource, bexpr.ResolvedSourcePropertyName] = value;
            bexpr.UpdateTarget();

            if (null != searchesList.SelectedItem)
                UpdateCurrentSearch();

            MarkSearchesChanged();

            e.Handled = true;
        }

        private void AdjustableGender_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var im = (Image)sender;
            var diff = Math.Sign(e.Delta);

            CycleAdjustableGender(im, diff);

            e.Handled = true;
        }

        private void AdjustableGender_Click(object sender, MouseButtonEventArgs e)
        {
            var im = (Image)sender;

            var diff = e.ChangedButton switch
            {
                MouseButton.Left => 1,
                MouseButton.Right => -1,
                _ => 0,
            };

            if (diff != 0)
            {
                CycleAdjustableGender(im, diff);
                e.Handled = true;
            }
        }

        private void CycleAdjustableGender(Image im, int diff)
        {
            var nOptions = nullableBoolValues.Count;
            var bexpr = im.GetBindingExpression(Image.SourceProperty);
            var accessor = TypeAccessor.Create(typeof(SearchCriteria));
            var value = (bool?)accessor[bexpr.ResolvedSource, bexpr.ResolvedSourcePropertyName];
            var index = nullableBoolValues.IndexOf(value);
            index = (index + diff + nOptions) % nOptions;
            value = nullableBoolValues[index];
            accessor[bexpr.ResolvedSource, bexpr.ResolvedSourcePropertyName] = value;
            bexpr.UpdateTarget();

            if (null != searchesList.SelectedItem)
                UpdateCurrentSearch();

            MarkSearchesChanged();
        }

        private void Result_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!(sender is FrameworkElement fe))
                return;
            if (!(fe.DataContext is DinoViewModel dino))
                return;
            //dino.Highlight = true;
        }

        private void Result_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!(sender is FrameworkElement fe))
                return;
            if (!(fe.DataContext is DinoViewModel dino))
                return;
            //dino.ClearValue(DinoViewModel.HighlightProperty);
        }

        private void ResultList_Sorting(object sender, DataGridSortingEventArgs e)
        {
            // No sorting for position
            if (e.Column.DisplayIndex == 2)
            {
                e.Handled = true;
                return;
            }

            if (e.Column.SortDirection == null)
                e.Column.SortDirection = ListSortDirection.Ascending;

            e.Handled = false;
        }

        private void MarkSearchesChanged()
        {
            settingsSaveTimer.Debounce(1000, o => SaveSearches());
        }

        private void SaveSearches()
        {
            if (!ShowTames)
            {
                Properties.Settings.Default.SavedSearches = JsonConvert.SerializeObject(ListSearches);
                Properties.Settings.Default.Save();
            }
        }

        private async Task ReReadArk()
        {
            if (IsLoading)
                return;

            // Always update the map image first to ensure it's current
            DiscoverCalibration();
            
            lastArk = Properties.Settings.Default.SaveFile;
            await PerformConversion();

            var currentSearch = searchesList.SelectedItems.Cast<SearchCriteria>().ToList();
            UpdateSearchResults(currentSearch);
        }

        private void UpdateSearchResults(IList<SearchCriteria> searches)
        {
            if (searches == null || searches.Count == 0)
            {
                ListResults.Clear();
            }
            else
            {
                // Find dinos that match the given searches
                var found = new List<Dino>();
                var sourceDinos = ShowTames ? arkReader.TamedDinos : arkReader.WildDinos;
                var total = 0;
                foreach (var search in searches)
                {
                    found.AddRange(search.Matches(sourceDinos, out int num_matched_on_species_only));
                    total += num_matched_on_species_only;
                    continue;
                }

                ListResults.Clear();
                foreach (var result in found)
                    if (!Properties.Settings.Default.HideUntameable || (result.IsTameable))
                        ListResults.Add(result);

                ShowCounts = true;
                ResultTotalCount = ShowTames ? sourceDinos.Sum(species => species.Value.Count()) : total;
                ResultMatchingCount = ListResults.Count;

            }

            ((CollectionViewSource)Resources["OrderedResults"]).View.Refresh();

            TriggerNameSearch(true);
        }

        private async Task PerformConversion()
        {
            string arkDirName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.SaveFile);

            IsLoading = true;
            try
            {
                // Stelle sicher, dass die SFTP-Konfiguration aktuell ist
                LoadSftpSettings();
                
                // Prüfe SFTP Status und Validität
                bool useSftp = SftpConfig.UseSftp;
                bool sftpValid = SftpConfig.IsValid();
                
                System.Diagnostics.Debug.WriteLine($"SFTP Status: UseSftp={useSftp}, IsValid={sftpValid}");
                
                if (useSftp)
                {
                    StatusText = "Processing saved ARK via SFTP";
                    StatusDetailText = "...connecting to SFTP server";
                    
                    // Debugging-Ausgabe
                    System.Diagnostics.Debug.WriteLine($"Using SFTP with server: {SftpConfig.Host}, UsePrivateKey={SftpConfig.UsePrivateKey}");
                    
                    // Erzwinge UI-Update vor SFTP-Verbindung
                    await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                    await Task.Delay(200); // Längeres Delay für zuverlässigeres UI-Update
                    
                    try 
                    {
                        await PerformSftpConversion();
                    }
                    catch (Exception ex)
                    {
                        // Bei SFTP-Fehler zeigen wir die Meldung an und brechen ab
                        StatusText = "SFTP connection failed";
                        StatusDetailText = ex.Message;
                        MessageBox.Show($"SFTP Error: {ex.Message}\n\nPlease check your connection settings.", 
                            "SFTP Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        IsLoading = false;
                        return;
                    }
                }
                else
                {
                    StatusDetailText = "...loading savegame";
                    StatusText = "Processing saved ARK";
                    await arkReader.PerformConversion(Properties.Settings.Default.SaveFile);
                }
                
                StatusText = "ARK processing completed";
                StatusDetailText = $"{arkReader.NumberOfWildSpecies} wild and {arkReader.NumberOfTamedSpecies} tame species located";
            }
            catch (Exception ex)
            {
                StatusText = "ARK processing failed";
                StatusDetailText = "";
                MessageBox.Show(ex.Message, "ARK Tools Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadSftpSettings()
        {
            SftpConfig.Host = Properties.Settings.Default.SftpHost;
            SftpConfig.Port = Properties.Settings.Default.SftpPort;
            SftpConfig.Username = Properties.Settings.Default.SftpUsername;
            SftpConfig.Password = Properties.Settings.Default.SftpPassword;
            SftpConfig.RemotePath = Properties.Settings.Default.SftpRemotePath;
            SftpConfig.UseSftp = Properties.Settings.Default.UseSftp;
            SftpConfig.UsePrivateKey = Properties.Settings.Default.UsePrivateKey;
            SftpConfig.PrivateKeyPath = Properties.Settings.Default.PrivateKeyPath;
            SftpConfig.PrivateKeyPassphrase = Properties.Settings.Default.PrivateKeyPassphrase;
            
            // Force property notification to update UI
            OnPropertyChanged(nameof(SftpConfig));
            
            // Debugging-Ausgabe für SFTP-Einstellungen
            System.Diagnostics.Debug.WriteLine($"SFTP Settings loaded: UseSftp={SftpConfig.UseSftp}, UsePrivateKey={SftpConfig.UsePrivateKey}, PrivateKeyPath={SftpConfig.PrivateKeyPath}");

            // Subscribe to profile changes
            SftpProfileManager.ProfileChanged -= SftpProfileManager_ProfileChanged; // Vermeide doppelte Registrierung
            SftpProfileManager.ProfileChanged += SftpProfileManager_ProfileChanged;
        }

        private async Task PerformSftpConversion()
        {
            try
            {
                // Die "connecting" Meldung wird jetzt bereits vorher gesetzt
                // Wir brauchen hier keinen erneuten Status
                
                // Setup SFTP client
                SftpClient client;
                
                System.Diagnostics.Debug.WriteLine($"Preparing SFTP client: Host={SftpConfig.Host}, UsePrivateKey={SftpConfig.UsePrivateKey}, PrivateKeyPath={SftpConfig.PrivateKeyPath}");
                
                if (SftpConfig.UsePrivateKey && !string.IsNullOrWhiteSpace(SftpConfig.PrivateKeyPath))
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("Attempting private key authentication");
                        // Use private key authentication
                        var privateKeyFile = string.IsNullOrEmpty(SftpConfig.PrivateKeyPassphrase)
                            ? new PrivateKeyFile(SftpConfig.PrivateKeyPath)
                            : new PrivateKeyFile(SftpConfig.PrivateKeyPath, SftpConfig.PrivateKeyPassphrase);
                            
                        var keyFiles = new[] { privateKeyFile };
                        client = new SftpClient(SftpConfig.Host, SftpConfig.Port, SftpConfig.Username, keyFiles);
                        System.Diagnostics.Debug.WriteLine("Private key authentication setup complete");
                    }
                    catch (Exception ex)
                    {
                        // Log the error and fallback to password authentication
                        System.Diagnostics.Debug.WriteLine($"Private key authentication failed: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine("Falling back to password authentication");
                        client = new SftpClient(SftpConfig.Host, SftpConfig.Port, SftpConfig.Username, SftpConfig.Password);
                    }
                }
                else
                {
                    // Use password authentication
                    System.Diagnostics.Debug.WriteLine("Using password authentication");
                    client = new SftpClient(SftpConfig.Host, SftpConfig.Port, SftpConfig.Username, SftpConfig.Password);
                }
                
                using (client)
                {
                    System.Diagnostics.Debug.WriteLine("Connecting to SFTP server...");
                    client.Connect();
                    
                    if (!client.IsConnected)
                    {
                        throw new Exception("Failed to connect to SFTP server");
                    }
                    
                    System.Diagnostics.Debug.WriteLine("Connected to SFTP server successfully");
                    
                    StatusDetailText = "...downloading savegame";
                    // UI aktualisieren vor dem Download
                    await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                    await Task.Delay(100); // Kurzes Delay für UI-Update
                    
                    // Create temporary file
                    string tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(SftpConfig.RemotePath));
                    
                    System.Diagnostics.Debug.WriteLine($"Downloading {SftpConfig.RemotePath} to {tempFile}");
                    
                    // Download the file
                    using (var fileStream = File.Create(tempFile))
                    {
                        client.DownloadFile(SftpConfig.RemotePath, fileStream);
                    }
                    
                    System.Diagnostics.Debug.WriteLine("Download complete");
                    
                    // Process the file
                    StatusDetailText = "...processing savegame";
                    // UI aktualisieren vor der Verarbeitung
                    await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                    await Task.Delay(100); // Kurzes Delay für UI-Update
                    
                    System.Diagnostics.Debug.WriteLine("Processing downloaded file");
                    await arkReader.PerformConversion(tempFile);
                    System.Diagnostics.Debug.WriteLine("Processing complete");
                    
                    // Cleanup
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch (Exception ex)
                    {
                        // Log but ignore cleanup errors
                        System.Diagnostics.Debug.WriteLine($"Cleanup error (non-critical): {ex.Message}");
                    }
                    
                    client.Disconnect();
                    System.Diagnostics.Debug.WriteLine("Disconnected from SFTP server");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SFTP Error: {ex.Message}");
                throw new Exception($"SFTP Error: {ex.Message}", ex);
            }
        }

        private void UpdateCurrentSearch()
        {
            void updateSearch(object o)
            {
                var search = (SearchCriteria)searchesList.SelectedItem;
                var searches = new List<SearchCriteria>();
                if (search != null)
                    searches.Add(search);
                UpdateSearchResults(searches);
            }

            refreshSearchesTimer.Debounce(250, updateSearch);
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.TargetItem is SearchCriteria targetItem && dropInfo.Data is SearchCriteria sourceItem)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            var sourceItem = (SearchCriteria)dropInfo.Data;
            var targetItem = (SearchCriteria)dropInfo.TargetItem;

            var ii = dropInfo.InsertIndex;
            var ip = dropInfo.InsertPosition;

            // Change source item's group
            sourceItem.Group = targetItem.Group;

            // Try to figure out the other item to insert between, or pick a boundary
            var options = ListSearches
                .Where(sc => sc != sourceItem)
                .Where(sc => sc.Group == targetItem.Group)
                .OrderBy(sc => sc.Order)
                .ToArray();

            // Make no changes if it was dropped on itself
            if (options.Length == 0)
                return;

            var above = options.Where(sc => sc.Order < targetItem.Order).OrderByDescending(sc => sc.Order).FirstOrDefault();
            var below = options.Where(sc => sc.Order > targetItem.Order).OrderBy(sc => sc.Order).FirstOrDefault();

            var aboveOrder = (above == null) ? options.Min(sc => sc.Order) - 1 : above.Order;
            var belowOrder = (below == null) ? options.Max(sc => sc.Order) + 1 : below.Order;

            // Update the order to be mid-way between either above or below, based on drag insert position
            sourceItem.Order = (targetItem.Order + (ip.HasFlag(RelativeInsertPosition.AfterTargetItem) ? belowOrder : aboveOrder)) / 2;

            // Renumber the results
            var orderedSearches = ListSearches
                .OrderBy(sc => sc.Group)
                .ThenBy(sc => sc.Order)
                .ToArray();

            for (var i = 0; i < orderedSearches.Length; i++)
            {
                orderedSearches[i].Order = i;
            }

            // Force binding update
            CollectionViewSource.GetDefaultView(searchesList.ItemsSource).Refresh();

            // Save list
            MarkSearchesChanged();
        }

        private void ShowTameSearches()
        {
            SetupTamedSearches();
        }

        private void SetupTamedSearches()
        {
            var wildcard = new string[] { null };
            var speciesList = wildcard.Concat(arkReader.TamedSpecies).ToList();
            var orderList = Enumerable.Range(0, speciesList.Count);
            var searches = speciesList.Zip(orderList, (species, order) => new SearchCriteria { Species = species, Order = order });

            ListSearches.Clear();
            foreach (var search in searches)
                ListSearches.Add(search);

            TriggerNameSearch(true);
        }

        private void ShowWildSearches()
        {
            LoadSavedSearches();
        }

        private void OnSettingsChanged()
        {
            DiscoverCalibration();
            
            // Update SFTP settings
            LoadSftpSettings();
            
            // Update auto reload setting
            AutoReload = Properties.Settings.Default.AutoReload;
            
            CheckIfArkChanged();
            UpdateCurrentSearch();

            ForceFontSizeUpdate();
            reloadTimer.Interval = TimeSpan.FromMilliseconds(Properties.Settings.Default.ConvertDelay);
            
            // Update file watcher based on AutoReload setting
            SetupFileWatcher();
        }

        private async void CheckIfArkChanged()
        {
            if (!EqualityComparer<string>.Default.Equals(lastArk, Properties.Settings.Default.SaveFile))
            {
                await ReReadArk();
            }
        }

        private void ForceFontSizeUpdate()
        {
            Dispatcher.Invoke(() => RefreshDataGridColumnWidths("GroupedSearchCriteria", searchesList), DispatcherPriority.ContextIdle);
            Dispatcher.Invoke(() => RefreshDataGridColumnWidths("OrderedResults", resultsList), DispatcherPriority.ContextIdle);
        }

        private void RefreshDataGridColumnWidths(string resourceName, DataGrid dataGrid)
        {
            var widths = dataGrid.Columns.Select(col => col.Width).ToArray();
            foreach (var col in dataGrid.Columns)
                col.Width = 0;

            ((CollectionViewSource)Resources[resourceName]).View.Refresh();
            dataGrid.UpdateLayout();

            foreach (var o in dataGrid.Columns.Zip(widths, (col, width) => new { col, width }))
                o.col.Width = o.width;
        }

        private void TriggerNameSearch(bool immediate = false)
        {
            nameSearchTime = DateTime.Now + TimeSpan.FromSeconds(immediate ? 0.01 : 0.5);
            nameSearchArg = SearchText;

            if (!nameSearchRunning)
            {
                nameSearchRunning = true;
                Dispatcher.Invoke(WaitForNameSearch, DispatcherPriority.Background);
            }
        }

        private async Task WaitForNameSearch()
        {
            while (nameSearchTime > DateTime.Now)
            {
                await Task.Delay(100);
            }

            nameSearchRunning = false;

            var searchText = nameSearchArg.Trim();

            if (String.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
            {
                foreach (var dvm in ListResults)
                    dvm.Highlight = false;
            }
            else
            {
                foreach (var dvm in ListResults)
                    dvm.Highlight = (dvm.Dino.Name != null) && dvm.Dino.Name.Contains(searchText);
            }
        }

        private void ComboOpen(object sender, EventArgs e)
        {
            //Open the box when we first click into the text
            speciesCombo.IsDropDownOpen = true;
        }

        private void window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

        private async void SftpProfileManager_ProfileChanged(object sender, EventArgs e)
        {
            // Reload settings from the newly selected profile
            LoadSftpSettings();
            
            // Update the map image based on the new profile
            DiscoverCalibration();
            
            // If SFTP is enabled, refresh the data
            if (SftpConfig.UseSftp && SftpConfig.IsValid())
            {
                await ReReadArk();
            }
        }

        private void ServerProfiles_Click(object sender, MouseButtonEventArgs e)
        {
            var window = new ServerProfilesWindow();
            window.Owner = this;
            window.ShowDialog();
        }

        // Add the OnPropertyChanged method to notify bindings
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        // Add PropertyChanged event
        public event PropertyChangedEventHandler PropertyChanged;

        private void ToggleDevMode()
        {
            // Toggle developer mode on/off
            IsDevMode = !IsDevMode;
            StatusText = IsDevMode ? "Developer mode enabled" : "Developer mode disabled";
        }
    }
}
