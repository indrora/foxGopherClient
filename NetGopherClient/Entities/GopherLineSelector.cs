using System.Windows;
using System.Windows.Controls;
using NetGopherClient.Gopher;

namespace NetGopherClient.Desktop
{
    public class GopherLineSelector : DataTemplateSelector
    {
        #region Fields and Properties

        public DataTemplate BinaryTemplate { get; set; }
        public DataTemplate BinHexTemplate { get; set; }
        public DataTemplate DirectoryTemplate { get; set; }
        public DataTemplate DosBinTemplate { get; set; }
        public DataTemplate DupeServerTemplate { get; set; }
        public DataTemplate ErrorTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate IndexSvrTemplate { get; set; }
        public DataTemplate InfoTemplate { get; set; }
        public DataTemplate MenuTemplate { get; set; }
        public DataTemplate PhoneTemplate { get; set; }
        public DataTemplate SessionTemplate { get; set; }
        public DataTemplate TelnetTemplate { get; set; }
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate UnknownTemplate { get; set; }
        public DataTemplate UrlTemplate { get; set; }
        public DataTemplate UUETemplate { get; set; }

        #endregion

        #region Public access

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is GopherLine)
            {
                try
                {
                    switch ((item as GopherLine).LineType)
                    {
                        case GopherLine.GopherLineType.Info:
                            return InfoTemplate;
                        case GopherLine.GopherLineType.File:
                            return TextTemplate;
                        case GopherLine.GopherLineType.Directory:
                            return DirectoryTemplate;
                        case GopherLine.GopherLineType.PhoneBook:
                            return PhoneTemplate;
                        case GopherLine.GopherLineType.Error:
                            return ErrorTemplate;
                        case GopherLine.GopherLineType.BinHex:
                            return BinHexTemplate;
                        case GopherLine.GopherLineType.DosBinary:
                            return DosBinTemplate;
                        case GopherLine.GopherLineType.UUEncoded:
                            return UUETemplate;
                        case GopherLine.GopherLineType.IndexSearch:
                            return IndexSvrTemplate;
                        case GopherLine.GopherLineType.Telnet:
                            return TelnetTemplate;
                        case GopherLine.GopherLineType.Binary:
                            return BinaryTemplate;
                        case GopherLine.GopherLineType.RendundantServer:
                            return DupeServerTemplate;
                        case GopherLine.GopherLineType.SessionPointer:
                            return SessionTemplate;
                        case GopherLine.GopherLineType.GIF:
                        case GopherLine.GopherLineType.GenericImage:
                            return ImageTemplate;
                        case GopherLine.GopherLineType.UnknownType:
                            return UnknownTemplate;
                        case GopherLine.GopherLineType.WebLink:
                            return UrlTemplate;
                    }
                }
                catch
                {
                    return base.SelectTemplate(item, container);
                }
            }
            return base.SelectTemplate(item, container);
        }

        #endregion
    }
}