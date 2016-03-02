using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetGopherClient
{
    public class GopherClient
    {
        public string GopherURI { get; set; }
        public string GopherServer { get; set; }
        public int GopherPort { get; set; }

        private ObservableCollection<gopherLine> _menu;

        private IUserInterface _userInterface;

        public GopherClient(ref ObservableCollection<gopherLine> menu, IUserInterface userInterface)
        {
            if (userInterface == null)
            {
                throw new ArgumentNullException("userInterface is null.");
            }

            if (menu == null)
            {
                throw new ArgumentNullException("menu is null");
            }

            _menu = menu;
            _userInterface = userInterface;
        }


    }
}
