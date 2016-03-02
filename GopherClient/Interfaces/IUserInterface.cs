using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetGopherClient.Gopher
{
    public interface IUserInterface
    {
        void UpdateStatus(string text);

        void DisplayMessage(string text, string title = "Alert");

        bool RequestYesNo(string text, string title = "Alert");

        void RefreshInterface();

        void UpdateNavigationUrl(Uri uri, bool trackBackUrl);

        void ResetScroll();

        void OnNavigationComplete();
    }
}
