using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using NetGopherClient.Gopher;

namespace NetGopherClient.Desktop
{
    [Serializable]
    public class GopherNavState : CustomContentState
    {
        public String tLocation { get; set; }
        private String _qq;

        public override String JournalEntryName
        {
            get { return _qq; }
        }

        public GopherNavState(string Location)
        {
            tLocation = Location;
        }

        public override void Replay(NavigationService navigationService, NavigationMode mode)
        {
            _qq = tLocation;
        }
    }
}
