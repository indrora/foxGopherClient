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

        private IUserInterface _userInterface;

        public ObservableCollection<GopherLine> ReceivedLines { get; } 
        public GopherClient(IUserInterface userInterface)
        {
            if (userInterface == null)
            {
                throw new ArgumentNullException("userInterface is null.");
            }

            ReceivedLines = new ObservableCollection<GopherLine>();
            
            _userInterface = userInterface;
        }


    }
}
