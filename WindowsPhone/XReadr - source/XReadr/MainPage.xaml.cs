using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using System.IO;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Text;
using System.Threading;
using Microsoft.Phone.Tasks;

namespace XReadr
{
    public partial class MainPage : PhoneApplicationPage
    {

        SvrMain uSvr;
       
        // Constructor
        public MainPage()
        {            
            InitializeComponent();
        }  
     
        private void Refresh_Click(object sender, EventArgs e)
        {
            uSvr.GetGoogleReaderUnReadCount();
        }

        // This method determines whether the user has navigated to the application after the application was tombstoned.
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            //Get parameter
            string myAccount = NavigationContext.QueryString["name"];
            string myPaswd = NavigationContext.QueryString["password"];
            string myAutoLogin = NavigationContext.QueryString["autologin"];

            uSvr = new SvrMain(this);
            uSvr.LoginGoogleReader(myAccount, myPaswd);
            PageTitle.Text = "XReadr";            
        }

        // The SelectionChanged handler for the feed items 
        private void feedListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = sender as ListBox;

            if (listBox != null && listBox.SelectedItem != null)
            {
                SvrMain.NewsItem sItem = (SvrMain.NewsItem)feedListBox.SelectedItem;
                switch (uSvr.CurrentTransType)
                {
                    case SvrMain.Transaction_Type.BROWSE_LABEL:
                        PageTitle.Text = "Label:" + sItem.Label;
                        uSvr.GetATOMbyLabel(sItem.Label, sItem.LabelUnreadCount);
                        break;
                    case SvrMain.Transaction_Type.GET_LABEL_ATOM:
                        uSvr.GetLoginToken(sItem);                        
                        break;
                    default:
                        break;
                }               
            }
        }
    }
}
