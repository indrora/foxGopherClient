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

namespace NetGopherClient
{
    /// <summary>
    /// Interaction logic for NetGopherClient.xaml
    /// </summary>
    public partial class NetGopherClientWindow : NavigationWindow
    {
        //#region DWM Stuff

        //[StructLayout(LayoutKind.Sequential)]
        //public struct MARGINS
        //{
        //    public int cxLeftWidth; // width of left border that retains its size
        //    public int cxRightWidth; // width of right border that retains its size
        //    public int cyTopHeight; // height of top border that retains its size
        //    public int cyBottomHeight; // height of bottom border that retains its size
        //};


        //[DllImport("DwmApi.dll")]
        //public static extern int DwmExtendFrameIntoClientArea(
        //    IntPtr hwnd,
        //    ref MARGINS pMarInset);

        //#endregion

        System.Collections.ObjectModel.ObservableCollection<gopherLine> myLines =
            new System.Collections.ObjectModel.ObservableCollection<gopherLine>();

        string cLocation = "";

        List<String> bookmarks = new List<string>();

        #region Init Stuff

        public NetGopherClientWindow()
        {
            InitializeComponent();

            this.NavigationService.Navigating += new NavigatingCancelEventHandler(NavigationService_Navigating);


            ResultsList.DataContext = myLines;
        }

        private void WindowLoad(object Sender, RoutedEventArgs e)
        {
            string homeUrl = ConfigurationManager.AppSettings["HomeUrl"];

            if (!String.IsNullOrWhiteSpace(homeUrl) && homeUrl.Contains("gopher://"))
            {
                browserLocation.Text = homeUrl;

                // Invoke button automation for NavigateToBrowserLocation
                ButtonAutomationPeer peer = new ButtonAutomationPeer(NavigateToBrowserLocation);
                IInvokeProvider invokeProvider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProvider.Invoke();
            }


            //try
            //{
            //    // handle loading the DWM Stuff.
            //    IntPtr mainWindowPtr = new WindowInteropHelper(this).Handle;
            //    HwndSource mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);
            //    mainWindowSrc.CompositionTarget.BackgroundColor = Color.FromArgb(0, 0, 0, 0);
            //    System.Drawing.Graphics desktop = System.Drawing.Graphics.FromHwnd(mainWindowPtr);
            //    float DesktopDpiX = desktop.DpiX;
            //    float DesktopDpiY = desktop.DpiY;

            //    MARGINS margins = new MARGINS();

            //    margins.cxLeftWidth = Convert.ToInt32(0*(DesktopDpiX/96));
            //    margins.cxRightWidth = Convert.ToInt32(0*(DesktopDpiX/96));
            //    margins.cyTopHeight =
            //        Convert.ToInt32(((int) navBar.ActualHeight + navBar.Margin.Bottom + 1)*(DesktopDpiX/96));
            //    margins.cyBottomHeight = Convert.ToInt32(sBar.ActualHeight*(DesktopDpiX/96));

            //    int hr = DwmExtendFrameIntoClientArea(mainWindowSrc.Handle, ref margins);
            //    //
            //    if (hr < 0)
            //    {
            //        //DwmExtendFrameIntoClientArea Failed
            //        MessageBox.Show("I was unable to make DwmExtendFrameIntoClientArea work!");
            //    }
            //    else
            //    {
            //        this.Background = Brushes.Transparent;
            //        navBar.Background = Brushes.Transparent;
            //        sBar.Background = Brushes.Transparent;
            //        //MessageBox.Show("DWMExtendFrameINtoClientArea Succeeded");
            //    }
            //}
            //catch
            //{
            //    // We should ignore DWM stuff...
            //}
        }

        #endregion

        #region Navigation Service commands

        void NavigationService_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            try
            {
                /* if (e.NavigationMode ==  NavigationMode.Back ) { */
                e.ContentStateToSave = new GopherNavState(cLocation); // }
                if (e.NavigationMode != NavigationMode.Refresh)
                {
                    navigate((e.TargetContentState as GopherNavState).tLocation, false);
                }
                else if (e.NavigationMode == NavigationMode.Refresh)
                {
                    navigate(cLocation, false);
                }
            }
            catch
            {
                // Something bad happened. Ignore it for the moment.
            }
        }

        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Title = "NetGopherClient";
            navigate(browserLocation.Text, true);

            //gopherLine gl = new gopherLine(@"1Initial server request: " + browserLocation.Text);
        }

        #region Internal navigation function

        private void navigate(string Location, bool make_back_url)
        {
            if (Location == "")
            {
                return;
            }

            StatusLabel.Content = "";
            // we're going to make sure the beginning has gopher:// before it. 
            if (!Location.ToLower().StartsWith("gopher://"))
            {
                // Look for a malformed protocol
                if (Location.ToLower().IndexOf("://") > 0)
                {
                    // Someone screwed up their URI requestor. We're going to have to fix it. 
                    //Location = Location.Substring(Location.IndexOf("://") + 3);
                    MessageBox.Show("Invalid URI Scheme. Try Gopher:// instead");
                }
                Location = "gopher://" + Location;
            }

            // use the Uri object to parse it out. 

            Uri ur;
            try
            {
                ur = new Uri(Location);
            }
            catch
            {
                MessageBox.Show("Invalid location!");
                return;
            }

            //if (ur.Scheme != Uri.UriSchemeGopher) { MessageBox.Show("I dont know how to process that URL. Try a Gopher URI."); return; } 

            if (cLocation != "" && make_back_url)
            {
                this.NavigationService.AddBackEntry(new GopherNavState(cLocation));
            }
            browserLocation.Text = ur.GetLeftPart(UriPartial.Query);
            this.Title = ".NET Gopher Client: " + ur.GetLeftPart(UriPartial.Query);

            System.Net.Sockets.TcpClient tcpC = null;
            myLines.Clear();
            Refresh(this);


            Refresh(this);
            System.Net.IPAddress[] addresses;
            try
            {
                addresses = System.Net.Dns.GetHostAddresses(ur.DnsSafeHost);

                if (addresses.Length == 0)
                {
                    // myLines.Add(new gopherLine("3Could not look up host...\terror\terror.host\t0"));
                    StatusLabel.Content = "Host not found";
                    return;
                }
            }
            catch
            {
                StatusLabel.Content = "Host not found";
                return;
            }


            //myLines.Add(new gopherLine("iEstablishing connection to host...\terror\terror.host\t0")); Refresh(this);
            StatusLabel.Content = "Connecting to " + ur.DnsSafeHost;
            try
            {
                tcpC = new System.Net.Sockets.TcpClient(ur.DnsSafeHost, ur.Port);
            }
            catch
            {
                //myLines.Add(new gopherLine("3Could not connect to host...\terror\terror.host\t0")); Refresh(this);
                StatusLabel.Content = "Could not connect to " + ur.DnsSafeHost;
                return;
            }
            //myLines.Add(new gopherLine("iGetting the stream...\terror\terror.host\t0")); Refresh(this);
            System.Net.Sockets.NetworkStream ConnStream = tcpC.GetStream();
            if (ConnStream.CanWrite == false)
            {
                // myLines.Add(new gopherLine("3Unable to use connection stream... network error?\terror\terror.host\t0")); Refresh(this);
                StatusLabel.Content = "Unable to connect";
                return;
            }
            // We need to write our resource locator, then keep going. 
            byte[] EncodedResource;
            EncodedResource = System.Text.Encoding.ASCII.GetBytes(ur.PathAndQuery + "\n");
            ConnStream.Write(EncodedResource, 0, EncodedResource.Length);


            System.IO.TextReader inStream = new System.IO.StreamReader(ConnStream);

            System.Collections.Generic.List<string> tLines = new List<string>();

            string tString = inStream.ReadLine();
            if (tString == ".")
            {
                //myLines.Add(new gopherLine("3Host sent bad EOF. Contact the server maintainer.\terror\terror.host\t0")); Refresh();
                StatusLabel.Content = "Error while reciving data";
                return;
            }
            myLines.Clear();
            Refresh(this);
            do
            {
                try
                {
                    if (tString != null)
                    {
                        myLines.Add(new gopherLine(tString));

                        StatusLabel.Content = "Received " + myLines.Count + " Selectors";
                        Refresh(this);
                    }
                }
                catch
                {
                    myLines.Clear();
                    myLines.Add(new gopherLine("3Recived bad selector.\txxx\txxx\t0"));
                    myLines.Add(new gopherLine("3Errenous line: " + tString.Replace('\t', '^') + "\txx\txx\t0"));
                    StatusLabel.Content = "Error while reciving data";
                    ResultsList.ScrollIntoView(ResultsList.Items[0]);
                    return;
                }
                tString = inStream.ReadLine();
            } while (tString != "." && tString != null);
            StatusLabel.Content = "Done with " + myLines.Count + " selectors";
            //string[] lines = tLines.ToArray();

            ResultsList.SelectedIndex = 0;
            //this.NavigationService.Content = ResultsList;


            cLocation = Location; // = ur.GetLeftPart(UriPartial.Query);


            //myLines.Clear();
            //foreach (var line in lines)
            //{
            //    myLines.Add(new gopherLine(line));
            //}
            if (myLines.Count > 0)
            {
                ResultsList.ScrollIntoView(ResultsList.Items[0]);
            }
        }

        #endregion

        #region File Download Functions

        /// <summary>
        /// Gets a temporary filename. 
        /// </summary>
        /// <param name="extention">the extention of the temp file (eg txt, png, etc)</param>
        /// <returns></returns>
        private string getTempFileName(string extention)
        {
            string k = System.IO.Path.GetTempFileName();
            string kn = k.Substring(0, k.LastIndexOf(".") + 1) + extention;
            System.IO.File.Move(k, kn);
            return kn;
        }

        /// <summary>
        /// Gets the human-readable format (KB, MB, etc)
        /// </summary>
        /// <param name="Len"></param>
        /// <returns></returns>
        private string getHumanReadableSize(float Len)
        {
            string[] suffixes = {"B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"};
            int s = 0;
            while (Len >= 1024 && s < suffixes.Length - 1)
            {
                s++;
                Len = Len/1024.0f;
            }

            return String.Format("{0} {1}", Len, suffixes[s]);
        }

        private void DownloadFile(gopherLine g, string tFileName)
        {
            ResultsList.IsEnabled = false;


            StatusLabel.Content = "Connecting...";

            System.Net.Sockets.TcpClient tc = new System.Net.Sockets.TcpClient();
            try
            {
                tc.Connect(g.TargetServer, g.TargetPort);
            }
            catch
            {
                StatusLabel.Content = "Ready...";
                MessageBox.Show("Cannot connect to host " + g.TargetServer + " on port " + g.TargetPort);
                return;
            }

            System.Net.Sockets.NetworkStream ts = tc.GetStream();
            try
            {
                byte[] b = System.Text.Encoding.ASCII.GetBytes(g.TargetUri + "\n");
                ts.Write(b, 0, b.Length);
            }
            catch
            {
                MessageBox.Show("I couldnt talk to the host; Network error?");
            }
            string fout = tFileName;
            System.IO.FileStream fs = System.IO.File.Open(fout, System.IO.FileMode.Create);
            int num = 0;
            do
            {
                Refresh(this);
                byte[] buf = new byte[2 ^ 8];
                Refresh(this);
                num = ts.Read(buf, 0, buf.Length);
                Refresh(this);
                if (num == 0)
                {
                    break;
                }
                Refresh(this);
                Array.Resize(ref buf, num);
                Refresh(this);
                fs.Write(buf, 0, num);
                Refresh(this);
                StatusLabel.Content = "Downloading... " + getHumanReadableSize(fs.Position);
                Refresh(this);
            } while (num > 0);
            StatusLabel.Content = "Downloaded " + getHumanReadableSize(fs.Length);

            fs.Close();
            ResultsList.IsEnabled = true;
        }

        #endregion

        #region item type handlers

        /// <summary>
        /// Called for index search submissions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void indexSubmit(object sender, RoutedEventArgs e)
        {
            // we need the information from a sibling.

            StackPanel b = VisualTreeHelper.GetParent(sender as DependencyObject) as StackPanel;
            string q = ((b.Children[0] as TextBox).Text);
            //MessageBox.Show(q);

            gopherLine gl = (e.OriginalSource as FrameworkElement).DataContext as gopherLine;
            //navigate(gl.TargetServer, gl.TargetPort, gl.TargetUri + "?" + q);
            // This is really a bit of a hack.
            navigate(
                String.Format("gopher://{0}{1}?{2}",
                    (gl.TargetPort != 70 ? gl.TargetServer + ":" + gl.TargetPort : gl.TargetServer), gl.TargetUri, q),
                true);
        }

        /// <summary>
        /// Open a text file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openFile(object sender, RoutedEventArgs e)
        {
// OPEN A TEXT FILE.

            object q = (e.OriginalSource as FrameworkElement).DataContext;
            if (q is gopherLine)
            {
                gopherLine g = q as gopherLine;

                string tf = getTempFileName("txt");
                DownloadFile(g, tf);
                System.Diagnostics.Process.Start(tf);
            }
        }

        /// <summary>
        /// Download a file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void getFile(object sender, RoutedEventArgs e)
        {
            object q = (e.OriginalSource as FrameworkElement).DataContext;
            if (q is gopherLine)
            {
                gopherLine g = q as gopherLine;

                string fName = g.TargetUri.Substring(g.TargetUri.LastIndexOf('/') + 1);
                string extension = fName.Substring(fName.LastIndexOf('.'));

                Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
                sfd.Filter = extension + " files (*" + extension + ")|*" + extension;
                sfd.FileName = fName;
                if (sfd.ShowDialog(this) == true)
                {
                    DownloadFile(g, fName);
                }
            }
        }

        /// <summary>
        /// Open a URL (either an HTTP url or an HTML page)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void goURL(object sender, RoutedEventArgs e)
        {
            object q = (e.OriginalSource as FrameworkElement).DataContext;
            if (q is gopherLine)
            {
                gopherLine g = q as gopherLine;
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
                    string fout = getTempFileName("html");
                    DownloadFile(g, fout);
                    System.Diagnostics.Process.Start(fout);
                }
            }
        }

        /// <summary>
        /// Move from the current selector to another.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void changeDirectory(object sender, RoutedEventArgs e)
        {
            object q = (e.OriginalSource as FrameworkElement).DataContext;
            if (q is gopherLine)
            {
                gopherLine g = q as gopherLine;

                if (!g.TargetUri.StartsWith("/"))
                    // Target URIs must be absolute given the server. I've read the GOPHER spec a few times over and still havent found anything for "relative" selectors.
                {
                    g.TargetUri = "/" + g.TargetUri;
                }

                navigate(
                    String.Format("gopher://{0}{1}",
                        (g.TargetPort != 70 ? g.TargetServer + ":" + g.TargetPort : g.TargetServer), g.TargetUri), true);
            }
        }

        #endregion

        #region Refresh the UI code

        // Used to refresh
        private delegate void NoArgDelegate();

        //Used as a refresher. 
        public static void Refresh(DependencyObject obj)
        {
            obj.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Input,
                (NoArgDelegate) delegate { });
        }

        #endregion

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TODO: Implement printing");
        }
    }

    public struct download_item
    {
        public gopherLine from_source;
        public string target;
    }

    public class GopherClient
    {
        public string GopherURI { get; set; }
        public string GopherServer { get; set; }
        public int GopherPort { get; set; }
    }
}