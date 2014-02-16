using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace XReadr
{
    public partial class LoginPage : PhoneApplicationPage
    {
        SvrMain uSvr;
        public LoginPage()
        {
            InitializeComponent();
        }

        // This method determines whether the user has navigated to the application after the application was tombstoned.
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
#if DEBUG
            //Account.Text = "PUT YOUR ACCOUNT HERE";
            //passwordBox1.Password = "PUT YOUR PASSWORD HERE";
#endif

            uSvr = new SvrMain(this);
            if (uSvr.DB_HasUserExistDB())
            {
                Account.Text = uSvr.DB_GetUserName();
                passwordBox1.Password = uSvr.DB_GetUserPassword();
                if ((bool)AutoLogin.IsChecked)
                {
                    GoToMainPage();
                }
            }
            else
            {
            }
        }

        private void GoToMainPage()
        {
            string mylogin = "/MainPage.xaml";
            mylogin += "?name=" + Account.Text;
            mylogin += "&password=" + passwordBox1.Password;
            mylogin += "&autologin=" + ( (bool)AutoLogin.IsChecked ? "true" : "false" );

            if (!String.IsNullOrWhiteSpace(mylogin))
            {
                this.NavigationService.Navigate(new Uri(mylogin, UriKind.Relative));
            }
        }
        private void loginbtn_Click(object sender, RoutedEventArgs e)
        {
            GoToMainPage();
        }
    }
}