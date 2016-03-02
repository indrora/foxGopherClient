using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using NetGopherClient.Gopher;

namespace NetGopherClient.Desktop
{
    /// <summary>
    /// Interaction logic for NetGopherClient.xaml
    /// </summary>
    public partial class NetGopherClientWindow : NavigationWindow, IUserInterface
    {
        #region "Private Properties"

        

        List<String> bookmarks = new List<string>();

        private GopherClient Gopher { get; }

        private IUserInterface UserInterface { get; }

        internal string Status
        {
            get { return StatusLabel.Content.ToString(); }
            set { Dispatcher.Invoke(new Action(() => { StatusLabel.Content = value; })); }
        }

        #endregion

        #region Init Stuff

        public NetGopherClientWindow()
        {
            InitializeComponent();

            this.NavigationService.Navigating += NavigationService_Navigating;

            UserInterface = this;
            Gopher = new GopherClient(UserInterface);

            ResultsList.DataContext = Gopher.ReceivedLines;
        }

        private void WindowLoad(object Sender, RoutedEventArgs e)
        {
            string homeUrl = ConfigurationManager.AppSettings["HomeUrl"];

            if (!String.IsNullOrWhiteSpace(homeUrl) && homeUrl.ToLower().StartsWith("gopher://"))
            {
                NavigationURL.Text = homeUrl;

                ClickButton(NavigateToBrowserLocation);
            }
        }

        #endregion

        #region Refresh the UI code

        // Used to refresh
        private delegate void NoArgDelegate();

        //Used as a refresher. 
        public static void Refresh(DependencyObject obj)
        {
            obj.Dispatcher.Invoke(DispatcherPriority.Input,
                (NoArgDelegate)delegate { });
        }

        public void RefreshInterface()
        {
            (new Thread(() => Refresh(this))).Start();
        }

        #endregion

        private void ClickButton(Button button)
        {
            // Invoke button automation for NavigateToBrowserLocation
            ButtonAutomationPeer peer = new ButtonAutomationPeer(button);
            IInvokeProvider invokeProvider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            invokeProvider.Invoke();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            UserInterface.DisplayMessage("TODO: Implement printing");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Title = ".NET Gopher Client";
            Gopher.Navigate(NavigationURL.Text, true);

            //GopherLine gl = new GopherLine(@"1Initial server request: " + browserLocation.Text);
        }

        public void UpdateStatus(string text)
        {
            //(new Thread(() => SetStatusLabel(text))).Start();
            Status = text;
        }

        public void DisplayMessage(string text, string title = "Alert")
        {
            UserInterface.DisplayMessage(text, title);
        }

        public bool RequestYesNo(string text, string title = "Alert")
        {
            var result = MessageBox.Show(text, title, MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes || result == MessageBoxResult.OK)
            {
                return true;
            }

            return false;
        }

        public void UpdateMenu(IEnumerable<GopherLine> items)
        {
            Gopher.ReceivedLines.Clear();
            foreach (GopherLine item in items)
            {
                Gopher.ReceivedLines.Add(item);
            }
        }

        public void UpdateNavigationUrl(Uri uri, bool trackBackUrl)
        {
            if (uri != null && uri.ToString() != "" && trackBackUrl)
            {
                this.NavigationService.AddBackEntry(new GopherNavState(uri.ToString()));
            }
            NavigationURL.Text = uri.GetLeftPart(UriPartial.Query);

            this.Title = ".NET Gopher Client: " + uri.GetLeftPart(UriPartial.Query);
        }

        public void ResetScroll()
        {
            ResultsList.ScrollIntoView(ResultsList.Items[0]);
        }

        public void OnNavigationComplete()
        {
            ResultsList.SelectedIndex = 0;
            //this.NavigationService.Content = ResultsList;
        }

        #region Navigation Service commands

        void NavigationService_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            try
            {
                /* if (e.NavigationMode ==  NavigationMode.Back ) { */
                e.ContentStateToSave = new GopherNavState(Gopher.Location); // }
                if (e.NavigationMode != NavigationMode.Refresh)
                {
                    Gopher.Navigate((e.TargetContentState as GopherNavState).tLocation, false);
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

        #endregion

        #region item type handlers

        /// <summary>
        /// Called for index search submissions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IndexSubmit(object sender, RoutedEventArgs e)
        {
            // we need the information from a sibling.

            StackPanel b = VisualTreeHelper.GetParent(sender as DependencyObject) as StackPanel;
            string q = ((b.Children[0] as TextBox).Text);
            //MessageBox.Show(q);

            GopherLine gl = (e.OriginalSource as FrameworkElement).DataContext as GopherLine;
            //navigate(gl.TargetServer, gl.TargetPort, gl.TargetUri + "?" + q);
            // This is really a bit of a hack.
            Gopher.Navigate(
                String.Format("gopher://{0}{1}?{2}",
                    (gl.TargetPort != 70 ? gl.TargetServer + ":" + gl.TargetPort : gl.TargetServer), gl.TargetUri, q),
                true);
        }

        /// <summary>
        /// Open a text file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFile(object sender, RoutedEventArgs e)
        {
            // OPEN A TEXT FILE.

            object q = (e.OriginalSource as FrameworkElement).DataContext;
            if (q is GopherLine)
            {
                GopherLine g = q as GopherLine;

                //string tf = GetTempFileName("txt");
                string filename = Gopher.DownloadFile(g, true, null, "txt");

                if (filename != null)
                {
                    System.Diagnostics.Process.Start(filename);
                }
            }
        }

        /// <summary>
        /// Download a file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetFile(object sender, RoutedEventArgs e)
        {
            object q = (e.OriginalSource as FrameworkElement).DataContext;
            if (q is GopherLine)
            {
                GopherLine g = q as GopherLine;

                string fName = g.TargetUri.Substring(g.TargetUri.LastIndexOf('/') + 1);
                string extension = fName.Substring(fName.LastIndexOf('.'));

                Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
                sfd.Filter = extension + " files (*" + extension + ")|*" + extension;
                sfd.FileName = fName;
                if (sfd.ShowDialog(this) == true)
                {
                    new Thread(() => Gopher.DownloadFile(g, false, fName, extension)).Start();
                }
            }
        }

        /// <summary>
        /// Open a URL (either an HTTP url or an HTML page)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavigateToURL(object sender, RoutedEventArgs e)
        {
            object q = (e.OriginalSource as FrameworkElement).DataContext;
            if (q is GopherLine)
            {
                GopherLine g = q as GopherLine;
                // we need to find the proper means and way of doing things.
                // We want to make sure that there's some kind of HTTP:// in the uri

                if (g.TargetUri.ToLower().StartsWith("url:") || g.TargetUri.ToLower().StartsWith("/url:"))
                    /* the second is because some gopher+ servers dont handle url: selectors normally */
                {
                    // we just need to figure out where the HTTP is, substr that out and go
                    string tURL = g.TargetUri.Substring(g.TargetUri.IndexOf("http"));
                    System.Diagnostics.Process.Start(tURL);
                }
                else
                {
                    string filename = Gopher.DownloadFile(g, true, null, "html");
                    System.Diagnostics.Process.Start(filename);
                }
            }
        }

        /// <summary>
        /// Move from the current selector to another.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeDirectory(object sender, RoutedEventArgs e)
        {
            object q = (e.OriginalSource as FrameworkElement).DataContext;
            if (q is GopherLine)
            {
                GopherLine g = q as GopherLine;

                if (!g.TargetUri.StartsWith("/"))
                    // Target URIs must be absolute given the server. I've read the GOPHER spec a few times over and still havent found anything for "relative" selectors.
                {
                    g.TargetUri = "/" + g.TargetUri;
                }

                Gopher.Navigate($"gopher://{(g.TargetPort != 70 ? g.TargetServer + ":" + g.TargetPort : g.TargetServer)}{g.TargetUri}", true);
            }
        }

        #endregion

        private void NavigationURL_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ClickButton(NavigateToBrowserLocation);
            }
        }
    }
}