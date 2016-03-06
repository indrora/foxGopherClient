using System;

namespace NetGopherClient.Gopher
{
    public class GopherLine
    {
        #region Fields and Properties

        private readonly char _typeChar;

        #endregion

        #region Fields and Properties

        public Uri HyperLinkUri
        {
            get
            {
                var u =
                    new Uri("gopher://" + TargetServer + ":" + TargetPort +
                            (TargetUri.StartsWith("/") ? TargetUri : "/" + TargetUri));
                return u;
            }
        }

        public string LineText { get; set; }

        public GopherLineType LineType { get; set; }
        public short TargetPort { get; set; }
        public string TargetServer { get; set; }
        public string TargetUri { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        ///     DO NOT USE. USED ONLY FOR DESIGN TIME ITEMS.
        /// </summary>
        public GopherLine() { }

        public GopherLine(string line)
        {
            // this isnt reccomended practice, but its what we have to do.
            if (line == "." || line == "")
            {
                return;
            }

            var blocks = line.Split('\t');

            // we know we're going to have 4 items: Text (with 1 item before it), a location, a server and a port.

            if (blocks[0] == "")
            {
                return;
            }

            var type = blocks[0][0];
            _typeChar = type;
            switch (type)
            {
                case 'i':
                    LineType = GopherLineType.Info;
                    break;
                case '0':
                    LineType = GopherLineType.File;
                    break;
                case '1':
                    LineType = GopherLineType.Directory;
                    break;
                case '2':
                    LineType = GopherLineType.PhoneBook;
                    break;
                case '3':
                    LineType = GopherLineType.Error;
                    break;
                case '4':
                    LineType = GopherLineType.BinHex;
                    break;
                case '5':
                    LineType = GopherLineType.DosBinary;
                    break;
                case '6':
                    LineType = GopherLineType.UUEncoded;
                    break;
                case '7':
                    LineType = GopherLineType.IndexSearch;
                    break;
                case 's': // HACK: We're treating SOUNDs as BINARIEs.
                case '9':
                    LineType = GopherLineType.Binary;
                    break;
                case '+':
                    LineType = GopherLineType.RendundantServer;
                    break;
                case 'T':
                    LineType = GopherLineType.SessionPointer;
                    break;
                case 'g': // GIF Image
                case 'p': // PNG Image
                case 'j': // JPG Image
                case 'I':
                    LineType = GopherLineType.GenericImage;
                    break;
                case 'h':
                    LineType = GopherLineType.WebLink;
                    break;
                default:
                    LineType = GopherLineType.UnknownType;
                    break;
            }

            LineText = blocks[0].Substring(1);

            if (LineType != GopherLineType.Info)
            {
                if (blocks.Length > 1) // Info lines do NOT have to conform.
                {
                    TargetUri = blocks[1];
                    TargetServer = blocks[2];
                    TargetPort = short.Parse(blocks[3]);
                }
                else
                {
                    LineType = GopherLineType.Error;
                    TargetUri = "";
                    TargetServer = "";
                    TargetPort = 0;
                }
            }
        }

        #endregion

        #region Public access

        public enum GopherLineType
        {
            Info,
            File,
            Directory,
            PhoneBook,
            Error,
            BinHex,
            DosBinary,
            UUEncoded,
            IndexSearch,
            Telnet,
            Binary,
            RendundantServer,
            SessionPointer,
            GIF,
            GenericImage,
            WebLink,
            UnknownType
        }

        #endregion

        #region Public access

        public override string ToString()
        {
            return "<" + _typeChar + "> " + LineText + " [ " + LineType + " =>" + TargetUri + " ]";
        }

        #endregion
    }
}