using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetGopherClient.Gopher
{
    public class GopherClient
    {
        #region Fields and Properties

        private readonly IUserInterface _userInterface;

        private long positionStart, positionEnd, positionEpsilon;
        private DateTime timeAbsoluteStart, timeAbsoluteEnd;
        private TimeSpan timeEpsilon;
        private DateTime timeStart, timeEnd;

        #endregion

        #region Fields and Properties

        public int GopherPort { get; set; }
        public string GopherServer { get; set; }

        public string GopherUri { get; set; }

        public string Location { get; set; } = "";

        public ObservableCollection<GopherLine> ReceivedLines { get; }

        #endregion

        #region Constructors

        public GopherClient(IUserInterface userInterface)
        {
            if (userInterface == null)
            {
                throw new ArgumentNullException(nameof(userInterface));
            }

            ReceivedLines = new ObservableCollection<GopherLine>();

            _userInterface = userInterface;
        }

        #endregion

        #region Public access

        public string DownloadFile(GopherLine g, bool randomFilename, string filename, string extension)
        {
            var tFileName = filename;

            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentNullException("extension is null");
            }

            if (randomFilename || string.IsNullOrWhiteSpace(filename))
            {
                tFileName = GetTempFileName(extension);
            }

            _userInterface.UpdateStatus("Connecting...");

            var tc = new TcpClient();
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

            var ts = tc.GetStream();
            try
            {
                var b = Encoding.ASCII.GetBytes(g.TargetUri + "\n");
                ts.Write(b, 0, b.Length);
            }
            catch
            {
                _userInterface.DisplayMessage("I couldnt talk to the host; Network error?");
            }

            StartTrackingDownload();

            var fs = File.Open(tFileName, FileMode.Create);
            var num = 0;
            do
            {
                var buf = new byte[2 ^ 8];

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
                if (location.ToLower().IndexOf("://", StringComparison.Ordinal) > 0)
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
            ReceivedLines.Clear();

            // Prepare for TCP connection to Gopher server
            TcpClient tcpClient;

            try
            {
                var addresses = Dns.GetHostAddresses(uri.DnsSafeHost);

                if (addresses.Length == 0)
                {
                    _userInterface.UpdateStatus("Host not found");
                    return;
                }
            }
            catch
            {
                _userInterface.UpdateStatus("Host not found");
                return;
            }

            _userInterface.UpdateStatus("Connecting to " + uri.DnsSafeHost);

            try
            {
                // Start the TCP connection to the Gopher server
                tcpClient = new TcpClient(uri.DnsSafeHost, uri.Port);
            }
            catch
            {
                _userInterface.UpdateStatus("Could not connect to " + uri.DnsSafeHost);
                return;
            }

            using (var connStream = tcpClient.GetStream())
            {
                if (connStream.CanWrite == false)
                {
                    _userInterface.UpdateStatus("Unable to connect");
                    return;
                }

                // We need to write our resource locator, then keep going. 
                var encodedResource = Encoding.ASCII.GetBytes(uri.PathAndQuery + "\n");
                connStream.Write(encodedResource, 0, encodedResource.Length);

                var inStream = new StreamReader(connStream);
                var line = inStream.ReadLine();
                if (line != null && line == ".")
                {
                    _userInterface.UpdateStatus("Error while receiving data");
                    return;
                }

                // TODO: Investigate redundant call to clear received lines
                //ReceivedLines.Clear();

                do
                {
                    try
                    {
                        if (line != null)
                        {
                            ReceivedLines.Add(new GopherLine(line));

                            _userInterface.UpdateStatus("Received " + ReceivedLines.Count + " Selectors");
                        }
                    }
                    catch
                    {
                        ReceivedLines.Clear();
                        ReceivedLines.Add(new GopherLine("3Recived bad selector.\txxx\txxx\t0"));

                        ReceivedLines.Add(new GopherLine("3Errenous line: " + line?.Replace('\t', '^') + "\txx\txx\t0"));

                        _userInterface.UpdateStatus("Error while reciving data");
                        _userInterface.ResetScroll();

                        return;
                    }

                    line = inStream.ReadLine();
                } while (line != "." && line != null);
            }

            _userInterface.UpdateStatus("Done with " + ReceivedLines.Count + " selectors");

            _userInterface.OnNavigationComplete();

            Location = location;

            if (ReceivedLines.Count > 0)
            {
                _userInterface.ResetScroll();
            }
        }

        #endregion

        #region Private Methods

        private string GetDownloadProgress()
        {
            return GetHumanReadableSize(positionEnd) + " (" + GetDownloadSpeed() + ")";
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
        ///     Gets the human-readable format (KB, MB, etc)
        /// </summary>
        /// <param name="Len"></param>
        /// <returns></returns>
        private string GetHumanReadableSize(float Len)
        {
            string[] suffixes = {"B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"};
            var s = 0;
            while (Len >= 1024 && s < suffixes.Length - 1)
            {
                s++;
                Len = Len / 1024.0f;
            }

            return string.Format("{0} {1}", Len.ToString("F"), suffixes[s]);
        }

        /// <summary>
        ///     Gets a temporary filename.
        /// </summary>
        /// <param name="extention">the extention of the temp file (eg txt, png, etc)</param>
        /// <returns></returns>
        private string GetTempFileName(string extention)
        {
            var k = Path.GetTempFileName();
            var kn = k.Substring(0, k.LastIndexOf(".") + 1) + extention;
            File.Move(k, kn);
            return kn;
        }

        private void StartTrackingDownload()
        {
            positionEnd = 0;
            timeAbsoluteStart = DateTime.Now;
            timeStart = timeAbsoluteStart;
        }

        private void StopTrackingDownload()
        {
            timeAbsoluteEnd = DateTime.Now;
            timeEnd = timeAbsoluteEnd;
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

        #endregion
    }
}