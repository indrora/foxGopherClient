using System;
using System.Windows.Navigation;

namespace NetGopherClient.Desktop
{
    [Serializable]
    public class GopherNavState : CustomContentState
    {
        #region Fields and Properties

        private string _qq;

        #endregion

        #region Fields and Properties

        public override string JournalEntryName => _qq;

        public string tLocation { get; set; }

        #endregion

        #region Constructors

        public GopherNavState(string Location)
        {
            tLocation = Location;
        }

        #endregion

        #region Public access

        public override void Replay(NavigationService navigationService, NavigationMode mode)
        {
            _qq = tLocation;
        }

        #endregion
    }
}