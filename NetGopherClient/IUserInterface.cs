using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetGopherClient
{
    public interface IUserInterface
    {
        void UpdateStatus(string text);

        void DisplayMessage(string text, string title = "Alert");

        bool RequestYesNo(string text, string title = "Alert");
    }
}
