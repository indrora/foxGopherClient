using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetGopherClient.Gopher
{
    public class GopherClient
    {
        public string GopherURI { get; set; }
        public string GopherServer { get; set; }
        public int GopherPort { get; set; }

        private IUserInterface _userInterface;

        public ObservableCollection<GopherLine> ReceivedLines { get; }

        string _location = "";

        public string Location
        {
            get { return _location; }
            set { _location = value; }
        }

        public GopherClient(IUserInterface userInterface)
        {
            if (userInterface == null)
            {
                throw new ArgumentNullException("userInterface is null.");
            }

            ReceivedLines = new ObservableCollection<GopherLine>();
            
            _userInterface = userInterface;
        }

        #region Internal navigation function

        public void Navigate(string location, bool trackBackUrl)
        {
            if (location == "")
            {
                return;
            }

            _userInterface.UpdateStatus("");

            // we're going to make sure the beginning has gopher:// before it. 
            if (!location.ToLower().StartsWith("gopher://"))
            {
                // Look for a malformed protocol
                if (location.ToLower().IndexOf("://") > 0)
                {
                    // Someone screwed up their URI requestor. We're going to have to fix it. 
                    //Location = Location.Substring(Location.IndexOf("://") + 3);
                    _userInterface.DisplayMessage("Invalid URI Scheme. Try Gopher:// instead");
                }
                location = "gopher://" + location;
            }

            // use the Uri object to parse it out. 

            Uri uri;
            try
            {
                uri = new Uri(location);
            }
            catch
            {
                _userInterface.DisplayMessage("Invalid location!");
                return;
            }

            _userInterface.UpdateNavigationUrl(uri, trackBackUrl);
            //if (ur.Scheme != Uri.UriSchemeGopher) { MessageBox.Show("I dont know how to process that URL. Try a Gopher URI."); return; } 

            System.Net.Sockets.TcpClient tcpC = null;
            ReceivedLines.Clear();
            _userInterface.RefreshInterface();

            // TODO: Why duplicate refreshes here?

            _userInterface.RefreshInterface();
            System.Net.IPAddress[] addresses;
            try
            {
                addresses = System.Net.Dns.GetHostAddresses(uri.DnsSafeHost);

                if (addresses.Length == 0)
                {
                    // myLines.Add(new GopherLine("3Could not look up host...\terror\terror.host\t0"));
                    _userInterface.UpdateStatus("Host not found");
                    return;
                }
            }
            catch
            {
                _userInterface.UpdateStatus("Host not found");
                return;
            }


            //myLines.Add(new GopherLine("iEstablishing connection to host...\terror\terror.host\t0")); Refresh(this);
            _userInterface.UpdateStatus("Connecting to " + uri.DnsSafeHost);
            try
            {
                tcpC = new System.Net.Sockets.TcpClient(uri.DnsSafeHost, uri.Port);
            }
            catch
            {
                //myLines.Add(new GopherLine("3Could not connect to host...\terror\terror.host\t0")); Refresh(this);
                _userInterface.UpdateStatus("Could not connect to " + uri.DnsSafeHost);
                return;
            }
            //myLines.Add(new GopherLine("iGetting the stream...\terror\terror.host\t0")); Refresh(this);
            System.Net.Sockets.NetworkStream ConnStream = tcpC.GetStream();
            if (ConnStream.CanWrite == false)
            {
                // myLines.Add(new GopherLine("3Unable to use connection stream... network error?\terror\terror.host\t0")); Refresh(this);
                _userInterface.UpdateStatus("Unable to connect");
                return;
            }
            // We need to write our resource locator, then keep going. 
            byte[] EncodedResource;
            EncodedResource = System.Text.Encoding.ASCII.GetBytes(uri.PathAndQuery + "\n");
            ConnStream.Write(EncodedResource, 0, EncodedResource.Length);


            System.IO.TextReader inStream = new System.IO.StreamReader(ConnStream);

            System.Collections.Generic.List<string> tLines = new List<string>();

            string tString = inStream.ReadLine();
            if (tString == ".")
            {
                //myLines.Add(new GopherLine("3Host sent bad EOF. Contact the server maintainer.\terror\terror.host\t0")); Refresh();
                _userInterface.UpdateStatus("Error while reciving data");
                return;
            }
            ReceivedLines.Clear();
            _userInterface.RefreshInterface();
            do
            {
                try
                {
                    if (tString != null)
                    {
                        ReceivedLines.Add(new GopherLine(tString));

                        _userInterface.UpdateStatus("Received " + ReceivedLines.Count + " Selectors");
                        _userInterface.RefreshInterface();
                    }
                }
                catch
                {
                    ReceivedLines.Clear();
                    ReceivedLines.Add(new GopherLine("3Recived bad selector.\txxx\txxx\t0"));
                    ReceivedLines.Add(new GopherLine("3Errenous line: " + tString.Replace('\t', '^') + "\txx\txx\t0"));

                    _userInterface.UpdateStatus("Error while reciving data");
                    _userInterface.ResetScroll();

                    return;
                }
                tString = inStream.ReadLine();
            } while (tString != "." && tString != null);
            _userInterface.UpdateStatus("Done with " + ReceivedLines.Count + " selectors");
            //string[] lines = tLines.ToArray();


            _userInterface.OnNavigationComplete();
            
            Location = location; // = ur.GetLeftPart(UriPartial.Query);


            //myLines.Clear();
            //foreach (var line in lines)
            //{
            //    myLines.Add(new GopherLine(line));
            //}
            if (ReceivedLines.Count > 0)
            {
                _userInterface.ResetScroll();
            }
        }

        #endregion

        #region File Download Functions

        /// <summary>
        /// Gets a temporary filename. 
        /// </summary>
        /// <param name="extention">the extention of the temp file (eg txt, png, etc)</param>
        /// <returns></returns>
        private string GetTempFileName(string extention)
        {
            string k = System.IO.Path.GetTempFileName();
            string kn = k.Substring(0, k.LastIndexOf(".") + 1) + extention;
            System.IO.File.Move(k, kn);
            return kn;
        }

        public string DownloadFile(GopherLine g, bool randomFilename, string filename, string extension)
        {
            string tFileName = filename;

            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentNullException("extension is null");
            }

            if (randomFilename || string.IsNullOrWhiteSpace(filename))
            {
                tFileName = GetTempFileName(extension);
            }

            _userInterface.UpdateStatus("Connecting...");

            System.Net.Sockets.TcpClient tc = new System.Net.Sockets.TcpClient();
            try
            {
                tc.Connect(g.TargetServer, g.TargetPort);
            }
            catch
            {
                _userInterface.UpdateStatus("Ready...");
                _userInterface.DisplayMessage("Cannot connect to host " + g.TargetServer + " on port " + g.TargetPort);
                return null;
            }

            System.Net.Sockets.NetworkStream ts = tc.GetStream();
            try
            {
                byte[] b = System.Text.Encoding.ASCII.GetBytes(g.TargetUri + "\n");
                ts.Write(b, 0, b.Length);
            }
            catch
            {
                _userInterface.DisplayMessage("I couldnt talk to the host; Network error?");
            }

            StartTrackingDownload();

            System.IO.FileStream fs = System.IO.File.Open(tFileName, System.IO.FileMode.Create);
            int num = 0;
            do
            {
                byte[] buf = new byte[2 ^ 8];

                num = ts.Read(buf, 0, buf.Length);

                if (num == 0)
                {
                    break;
                }

                Array.Resize(ref buf, num);

                fs.Write(buf, 0, num);

                UpdateDownloadTracking(fs.Position);
                _userInterface.UpdateStatus("Downloading... " + GetDownloadProgress());
            } while (num > 0);
            fs.Close();

            StopTrackingDownload();
            _userInterface.UpdateStatus("Downloaded " + GetDownloadProgress());
            
            return tFileName;
        }

        private long positionStart, positionEnd, positionEpsilon;
        private DateTime timeAbsoluteStart, timeAbsoluteEnd;
        private DateTime timeStart, timeEnd;
        private TimeSpan timeEpsilon;
        private void StartTrackingDownload()
        {
            positionEnd = 0;
            timeAbsoluteStart = DateTime.Now;
            timeStart = timeAbsoluteStart;
        }

        private void UpdateDownloadTracking(long position)
        {
            positionEnd = position;

            positionEpsilon = positionEnd - positionStart;

            positionStart = positionEnd;

            timeEnd = DateTime.Now;

            timeEpsilon = timeEnd - timeStart;

            timeStart = timeEnd;

        }

        private void StopTrackingDownload()
        {
            timeAbsoluteEnd = DateTime.Now;
            timeEnd = timeAbsoluteEnd;
        }

        private string GetDownloadProgress()
        {
            return GetHumanReadableSize(positionEnd) + "(" + GetDownloadSpeed() + ")";
        }
        // TODO: moving average
        private string GetDownloadSpeed()
        {
            if ((timeEnd - timeAbsoluteStart).TotalSeconds > 0)
            {
                var rate = positionEnd / (decimal) (timeEnd - timeAbsoluteStart).TotalSeconds;

                return GetHumanReadableSize((float) rate) + "/s";
            }

            return "NaN";
            //if (timeEpsilon.TotalMilliseconds > 0)
            //{
            //    var rate = positionEpsilon/(decimal) timeEpsilon.TotalMilliseconds;

            //    return Decimal.Round(rate/1000).ToString("F") + " " +
            //           GetHumanReadableSize(positionEpsilon).Split(' ')[1] + "/s";
            //}

            //return "NaN";
        }

        /// <summary>
        /// Gets the human-readable format (KB, MB, etc)
        /// </summary>
        /// <param name="Len"></param>
        /// <returns></returns>
        private string GetHumanReadableSize(float Len)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            int s = 0;
            while (Len >= 1024 && s < suffixes.Length - 1)
            {
                s++;
                Len = Len / 1024.0f;
            }

            return String.Format("{0} {1}", Len.ToString("F"), suffixes[s]);
        }

        #endregion
    }
}
