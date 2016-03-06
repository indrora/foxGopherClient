using System.Collections.Generic;

namespace NetGopherClient.Gopher
{
    internal class ClientSettings
    {
        #region Fields and Properties

        public List<string> Bookmarks { get; set; }

        public bool ChangeLineEncodings { get; set; }
        public string textFileViewer { get; set; }

        public bool useAltDowloadForTextFiles { get; set; }

        #endregion
    }
}