using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetGopherClient.Gopher
{
    public class GopherLine
    {
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

        /// <summary>
        /// DO NOT USE. USED ONLY FOR DESIGN TIME ITEMS.
        /// </summary>
        public GopherLine()
        {
        }

        public GopherLineType LineType { get; set; }
        public string LineText { get; set; }
        public string TargetServer { get; set; }
        public string TargetUri { get; set; }
        public Int16 TargetPort { get; set; }
        char TypeChar;

        public Uri HyperLinkURI
        {
            get
            {
                Uri u =
                    new Uri("gopher://" + TargetServer + ":" + TargetPort +
                            (TargetUri.StartsWith("/") ? TargetUri : "/" + TargetUri));
                return u;
            }
        }

        public GopherLine(string Line)
        {
            // this isnt reccomended practice, but its what we have to do.
            if (Line == "." || Line == "")
            {
                return;
            }

            string[] blocks = Line.Split(new char[] { '\t' });
            // we know we're going to have 4 items: Text (with 1 item before it), a location, a server and a port.

            if (blocks[0] == "")
            {
                return;
            }

            char type = blocks[0][0];
            TypeChar = type;
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
                    TargetPort = Int16.Parse(blocks[3]);
                }
                else
                {
                    this.LineType = GopherLineType.Error;
                    TargetUri = "";
                    TargetServer = "";
                    TargetPort = 0;
                }
            }
        }


        public override string ToString()
        {
            return "<" + TypeChar + "> " + LineText + " [ " + LineType + " =>" + TargetUri + " ]";
        }
    }
}
