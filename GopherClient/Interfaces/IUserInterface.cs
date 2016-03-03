using System;

namespace NetGopherClient.Gopher
{
    public interface IUserInterface
    {
        #region Public access

        void DisplayMessage(string text, string title = "Alert");

        void OnNavigationComplete();

        void RefreshInterface();

        bool RequestYesNo(string text, string title = "Alert");

        void ResetScroll();

        void UpdateNavigationUrl(Uri uri, bool trackBackUrl);
        void UpdateStatus(string text);

        #endregion
    }
}