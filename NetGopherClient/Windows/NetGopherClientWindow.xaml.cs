using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Win32;
using NetGopherClient.Gopher;

namespace NetGopherClient.Desktop
{
    /// <summary>
    ///     Interaction logic for NetGopherClient.xaml
    /// </summary>
    public partial class NetGopherClientWindow : NavigationWindow, IUserInterface
    {
        #region Fields and Properties

        private List<string> bookmarks = new List<string>();

        #endregion

        #region Fields and Properties

        internal string Status
        {
            get { return StatusLabel.Content.ToString(); }
            set { Dispatcher.Invoke(() => { StatusLabel.Content = value; }); }
        }

        private GopherClient Gopher { get; }

        private IUserInterface UserInterface { get; }

        #endregion

        #region Constructors

        public NetGopherClientWindow()
        {
            InitializeComponent();

            NavigationService.Navigating += NavigationService_Navigating;

            UserInterface = this;
            Gopher = new GopherClient(UserInterface);

            ResultsList.DataContext = Gopher.ReceivedLines;
        }

        #endregion

        #region Public access

        //Used as a refresher. 
        public static void Refresh(DependencyObject obj)
        {
            obj.Dispatcher.Invoke(DispatcherPriority.Input,
                                  (NoArgDelegate) delegate { });
        }

        public void UpdateMenu(IEnumerable<GopherLine> items)
        {
            Gopher.ReceivedLines.Clear();
            foreach (var item in items)
            {
                Gopher.ReceivedLines.Add(item);
            }
        }

        #endregion

        #region Interface implementations

        public void DisplayMessage(string text, string title = "Alert")
        {
            UserInterface.DisplayMessage(text, title);
        }

        public void OnNavigationComplete()
        {
            ResultsList.SelectedIndex = 0;

            //this.NavigationService.Content = ResultsList;
        }

        public void RefreshInterface()
        {
            new Thread(() => Refresh(this)).Start();
        }

        public bool RequestYesNo(string text, string title = "Alert")
        {
            var result = MessageBox.Show(text, title, MessageBoxButton.YesNo);

            return result == MessageBoxResult.Yes || result == MessageBoxResult.OK;
        }

        public void ResetScroll()
        {
            ResultsList.ScrollIntoView(ResultsList.Items[0]);
        }

        public void UpdateNavigationUrl(Uri uri, bool trackBackUrl)
        {
            if (uri == null)
            {
                return;
            }
            if (uri.ToString() != "" && trackBackUrl)
            {
                NavigationService.AddBackEntry(new GopherNavState(uri.ToString()));
            }

            NavigationURL.Text = uri.GetLeftPart(UriPartial.Query);

            Title = ".NET Gopher Client: " + uri.GetLeftPart(UriPartial.Query);
        }

        public void UpdateStatus(string text)
        {
            Status = text;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Move from the current selector to another.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeDirectory(object sender, RoutedEventArgs e)
        {
            var gopherLine = GetGopherLine(e);
            if (gopherLine == null)
            {
                return;
            }

            // Target URIs must be absolute given the server. I've read the GOPHER spec a few times over and still havent found anything for "relative" selectors.
            if (!gopherLine.TargetUri.StartsWith("/"))
            {
                gopherLine.TargetUri = "/" + gopherLine.TargetUri;
            }

            Gopher.Navigate(
                $"gopher://{(gopherLine.TargetPort != 70 ? gopherLine.TargetServer + ":" + gopherLine.TargetPort : gopherLine.TargetServer)}{gopherLine.TargetUri}",
                true);
        }

        private static void ClickButton(Button button)
        {
            // Invoke button automation for NavigateToBrowserLocation
            var peer = new ButtonAutomationPeer(button);
            var invokeProvider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            invokeProvider?.Invoke();
        }

        /// <summary>
        ///     Download a file
        /// </summary>
        /// <param name="sender">User interface sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void GetFile(object sender, RoutedEventArgs e)
        {
            var gopherLine = GetGopherLine(e);
            if (gopherLine == null)
            {
                return;
            }

            var fName = gopherLine.TargetUri.Substring(gopherLine.TargetUri.LastIndexOf('/') + 1);
            var extension = fName.Substring(fName.LastIndexOf('.'));

            var sfd = new SaveFileDialog
                      {
                          Filter = extension + " files (*" + extension + ")|*" + extension,
                          FileName = fName
                      };
            if (sfd.ShowDialog(this) == true)
            {
                new Thread(() => Gopher.DownloadFile(gopherLine, false, fName, extension)).Start();
            }
        }

        /// <summary>
        ///     Get a <see cref="GopherLine" /> from user interface arguments
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static GopherLine GetGopherLine(RoutedEventArgs e)
        {
            var element = (FrameworkElement) e.OriginalSource;
            var line = element?.DataContext;
            return line as GopherLine;
        }

        private void GoButtonClick(object sender, RoutedEventArgs e)
        {
            Title = ".NET Gopher Client";
            Gopher.Navigate(NavigationURL.Text, true);
        }

        /// <summary>
        ///     Called for index search submissions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IndexSubmit(object sender, RoutedEventArgs e)
        {
            // we need the information from a sibling.

            var b = VisualTreeHelper.GetParent((DependencyObject) sender) as StackPanel;
            var q = (b?.Children[0] as TextBox)?.Text;

            //MessageBox.Show(q);

            var frameworkElement = e.OriginalSource as FrameworkElement;

            if (frameworkElement == null)
            {
                return;
            }

            var gl = frameworkElement.DataContext as GopherLine;

            //navigate(gl.TargetServer, gl.TargetPort, gl.TargetUri + "?" + q);
            // This is really a bit of a hack.
            Gopher.Navigate(
                $"gopher://{(gl.TargetPort != 70 ? gl.TargetServer + ":" + gl.TargetPort : gl.TargetServer)}{gl.TargetUri}?{q}",
                true);
        }

        /// <summary>
        ///     Open a URL (either an HTTP url or an HTML page)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavigateToUrl(object sender, RoutedEventArgs e)
        {
            var gopherLine = GetGopherLine(e);
            if (gopherLine == null)
            {
                return;
            }

            // we need to find the proper means and way of doing things.
            // We want to make sure that there's some kind of HTTP:// in the uri

            if (gopherLine.TargetUri.ToLower().StartsWith("url:") || gopherLine.TargetUri.ToLower().StartsWith("/url:"))
                /* the second is because some gopher+ servers dont handle url: selectors normally */
            {
                // we just need to figure out where the HTTP is, substr that out and go
                var targetUrl =
                    gopherLine.TargetUri.Substring(gopherLine.TargetUri.IndexOf("http", StringComparison.Ordinal));
                Process.Start(targetUrl);
            }
            else
            {
                var filename = Gopher.DownloadFile(gopherLine, true, null, "html");
                Process.Start(filename);
            }
        }

        private void NavigationService_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            try
            {
                /* if (e.NavigationMode ==  NavigationMode.Back ) { */
                e.ContentStateToSave = new GopherNavState(Gopher.Location); // }
                if (e.NavigationMode != NavigationMode.Refresh)
                {
                    if (e.TargetContentState != null)
                    {
                        Gopher.Navigate(((GopherNavState) e.TargetContentState).tLocation, false);
                    }
                }
                else if (e.NavigationMode == NavigationMode.Refresh)
                {
                    Gopher.Navigate(Gopher.Location, false);
                }
            }
            catch
            {
                // Something bad happened. Ignore it for the moment.
            }
        }

        private void NavigationURL_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ClickButton(NavigateToBrowserLocation);
            }
        }

        private void OnPrintButtonClick(object sender, RoutedEventArgs e)
        {
            UserInterface.DisplayMessage("TODO: Implement printing");
        }

        /// <summary>
        ///     Open a text file.
        /// </summary>
        /// <param name="sender">User interface sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OpenFile(object sender, RoutedEventArgs e)
        {
            var gopherLine = GetGopherLine(e);
            if (gopherLine == null)
            {
                return;
            }

            //string tf = GetTempFileName("txt");
            var filename = Gopher.DownloadFile(gopherLine, true, null, "txt");

            if (filename != null)
            {
                Process.Start(filename);
            }
        }

        private void WindowLoad(object sender, RoutedEventArgs e)
        {
            var homeUrl = ConfigurationManager.AppSettings["HomeUrl"];

            if (string.IsNullOrWhiteSpace(homeUrl) || !homeUrl.ToLower().StartsWith("gopher://"))
            {
                return;
            }

            NavigationURL.Text = homeUrl;

            ClickButton(NavigateToBrowserLocation);
        }

        #endregion

        // Used to refresh
        private delegate void NoArgDelegate();
    }
}