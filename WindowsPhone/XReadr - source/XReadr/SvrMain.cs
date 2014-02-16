using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Text;
using System.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System.Xml;

//JSON related
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json;
using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace XReadr
{
    public class SvrMain    
    {
        public SvrMain(PhoneApplicationPage currentPage)
        {
            thisPage = currentPage;
            DBHandler tDBHandler;
            tDBHandler = new DBHandler(DBHandler.DBConnection);
            if (tDBHandler.DatabaseExists() == false)
            {
                tDBHandler.CreateDatabase();
            }            
        }

        public enum Transaction_Type
        {
            BEFORE_LOGIN = 0,
            LOGIN_USER = 1,
            BROWSE_LABEL = 2,
            GET_USER_INFO = 3,
            GET_LABEL_ATOM = 4,
            GET_LOGIN_TOKEN = 5,
            MARK_AS_READ = 6,
        }

        const int BUFFER_SIZE = 1024;
        //Login session information.
        public string UserEmail;
        public string UserPasswd;
        public string UserAuth;
        public string UserSid;
        public string LoginClient = "scroll";
        
        //For mark as read using
        static public string staUserID;
        static public string staNewsID;
        static public string staFeedID;
        static public string staLoginToken;
        static public string staLink;

        //Usere account information.
        public ServerUserInfo UserInfoClass;
        public PhoneApplicationPage thisPage;
        public Transaction_Type CurrentTransType = Transaction_Type.BEFORE_LOGIN;
        public class NewsItems : System.Collections.ObjectModel.ObservableCollection<NewsItem>
        {}

        public NewsItems myNews = new NewsItems(); //JSON

        public class ServerUnreadResult //JSON
        {
            public string id { get; set; }
            public string count { get; set; }
            public string newestItemTimestampUsec { get; set; }
        }

        public class ServerUserInfo //JSON
        {
            public string userId { get; set; }
            public string userName { get; set; }
            public string userProfileId { get; set; }
            public string userEmail { get; set; }
            public string isBloggerUser { get; set; }
            public string signupTimeSec { get; set; }
            public string publicUserName { get; set; }
            public string isMultiLoginEnabled { get; set; }
        }

        public class ServerLabelATOMResult //JSON
        {
            public string id { get; set; }
            public string title { get; set; }
        }

        public class ServerLabelATOMResult2 //JSON
        {
            public string href { get; set; }
            public string type { get; set; }
            //public string content { get; set; }
        }

        public class NewsItem
        {
            //For UI list control display
            public String DispTitle{ get; set; }
            public String DispDetail{ get; set; }
            
            //News detail items
            public String Label { get; set; }
            public String LabelUnreadCount{ get; set; }
            public String newestItemTimestampUsec{ get; set; }
            public String Link{ get; set; }

            public NewsItem(   String DispTitle,
                                String DispDetail,
                                String Label,
                                String LabelUnreadCount, 
                                String newestItemTimestampUsec,
                                String Link
                            )
            {   
                this.DispTitle = DispTitle;
                this.DispDetail = DispDetail;

                this.Label = Label;
                this.LabelUnreadCount = LabelUnreadCount;
                this.newestItemTimestampUsec = newestItemTimestampUsec;
                this.Link = "img/news.png";
            }
        }
#region Google API Request Functions
        public void LoginGoogleReader(string UserName, string Passwd)
        {
            CurrentTransType = Transaction_Type.LOGIN_USER;
            UserEmail = UserName; 
            UserPasswd = Passwd;
            string auth_params = string.Format("https://www.google.com/accounts/ClientLogin?accountType=HOSTED_OR_GOOGLE&Email=" + UserEmail + "&Passwd=" + UserPasswd + "&service=reader&source=J-MyReader-1.0");
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(auth_params);
            httpRequest.Method = "POST";
            httpRequest.BeginGetResponse(new AsyncCallback(ResponseCallback), httpRequest);
        }

        public void GetLoginToken(NewsItem articleInfo)
        {
            //
            //ex: https://www.google.com/reader/api/0/token?ck=212121212&client=scroll
            //
            CurrentTransType = Transaction_Type.GET_LOGIN_TOKEN;
            string auth_params = string.Format("https://www.google.com/reader/api/0/token?ck=" + DateTime.Now.Ticks.ToString() + "&client=scroll");
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(auth_params);
            httpRequest.Method = "GET";
            httpRequest.Headers["Authorization"] = "GoogleLogin auth=" + UserAuth;
            httpRequest.Headers["Cookie"] = "SID=" + UserSid;
            httpRequest.BeginGetResponse(new AsyncCallback(ResponseCallback), httpRequest);

            //handle static data
            staNewsID = articleInfo.Label;
            staFeedID = articleInfo.LabelUnreadCount;
            staLink = articleInfo.newestItemTimestampUsec;
        }

        public void GetUserInformation()
        {
            CurrentTransType = Transaction_Type.GET_USER_INFO;
            string auth_params = string.Format("https://www.google.com/reader/api/0/user-info?format=joson&ck=" + DateTime.Now.Ticks.ToString() + "&client=scroll");

            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(auth_params);
            httpRequest.Method = "GET";
            httpRequest.Headers["Authorization"] = "GoogleLogin auth=" + UserAuth;
            httpRequest.Headers["Cookie"] = "SID=" + UserSid;
            httpRequest.BeginGetResponse(new AsyncCallback(ResponseCallbackInner), httpRequest);
        }

        public void GetGoogleReaderUnReadCount()
        {
            CurrentTransType = Transaction_Type.BROWSE_LABEL;
            string auth_params = string.Format("https://www.google.com/reader/api/0/unread-count?allcomments=true&output=json&ck=" + DateTime.Now.Ticks.ToString() + "&client=scroll");

            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(auth_params);
            httpRequest.Method = "GET";
            httpRequest.Headers["Authorization"] = "GoogleLogin auth=" + UserAuth;
            httpRequest.Headers["Cookie"] = "SID=" + UserSid;
            httpRequest.BeginGetResponse(new AsyncCallback(ResponseCallbackInner), httpRequest);
        }

        public void GetATOMbyLabel(string sLabel, string nAtomUnreadCount)
        {
            CurrentTransType = Transaction_Type.GET_LABEL_ATOM;
            string auth_params = string.Format("http://www.google.com/reader/api/0/stream/contents/user/" + UserInfoClass.userId + "/label/"+sLabel+"?n="+ nAtomUnreadCount + "&ck=" + DateTime.Now.Ticks.ToString() + "&client=scroll&format=json&xt=user/" + UserInfoClass.userId + "/state/com.google/read");

            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(auth_params);
            httpRequest.Method = "GET";
            httpRequest.Headers["Authorization"] = "GoogleLogin auth=" + UserAuth;
            httpRequest.Headers["Cookie"] = "SID=" + UserSid;
            httpRequest.BeginGetResponse(new AsyncCallback(ResponseCallback), httpRequest);
        }

        public void MarkArticleAsRead()
        {
            CurrentTransType = Transaction_Type.MARK_AS_READ;
            string auth_params = string.Format("https://www.google.com/reader/api/0/edit-tag?client=scroll&format=joson&ck=" + DateTime.Now.Ticks.ToString());

            //string URI = "http://www.myurl.com/post.php";
            //string myParamters = "param1=value1&param2=value2";
            string postData = "";
            postData += "&i=" + staNewsID;
            postData += "&a=user/" + staUserID + "/state/com.google/read";
            postData += "&s=" + staFeedID;
            postData += "&T=" + staLoginToken;


            WebClient wc = new WebClient();
            wc.Headers["Content-type"] = "application/x-www-form-urlencoded";
            wc.Headers["Authorization"] = "GoogleLogin auth=" + UserAuth;
            wc.Headers["Cookie"] = "SID=" + UserSid;
            try
            {
                wc.UploadStringAsync(new Uri(auth_params), "POST", postData);
            }
            catch (WebException e)
            {
                //Handle Markit as resolve if failed.
            }

            //Open WebBrowser to see detail link.
            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.Uri = new Uri(staLink);
            webBrowserTask.Show();
        }       
#endregion

        private void ResponseCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest tRequest = (HttpWebRequest)asynchronousResult.AsyncState;
            HttpWebResponse tResponse = null;
            try
            {
                 tResponse = (HttpWebResponse)tRequest.EndGetResponse(asynchronousResult);
            }
            catch (WebException e)
            {
                //Go to UI thread.
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        switch (CurrentTransType)
                        {
                            case Transaction_Type.LOGIN_USER:
                                MessageBox.Show("Login error!");
                                thisPage.NavigationService.GoBack();
                                break;
                            default:
                                break;
                        }
                   });
                return;
            }

            using (StreamReader tResponseStream = new StreamReader(tResponse.GetResponseStream()))
            {
                string strResult = tResponseStream.ReadToEnd();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    switch (CurrentTransType)
                    {
                        case Transaction_Type.LOGIN_USER:
                            ParseLogin(strResult);
                            //Login sucucess!
                            DB_SaveUserInfo2DB();
                            GetUserInformation();
                            break;
                        case Transaction_Type.BROWSE_LABEL:
                            ParseUnreadList(strResult);
                            ((MainPage)thisPage).feedListBox.ItemsSource = myNews;
                            ((MainPage)thisPage).PageTitle.Text = "User Labels";
                            break;
                        case Transaction_Type.GET_LABEL_ATOM:
                            ParseLabelATOM(strResult);
                            ((MainPage)thisPage).feedListBox.ItemsSource = myNews;
                            ((MainPage)thisPage).PageTitle.Text = "User Labels";
                            break;
                        case Transaction_Type.GET_LOGIN_TOKEN:
                            ParseToken(strResult);
                            MarkArticleAsRead();
                            break;
                        default:
                            break;
                    }
                });
            }
        }

        private void ResponseCallbackInner(IAsyncResult asynchronousResult)
        {
            HttpWebRequest tRequest = (HttpWebRequest)asynchronousResult.AsyncState;
            HttpWebResponse tResponse = (HttpWebResponse)tRequest.EndGetResponse(asynchronousResult);
            using (StreamReader tResponseStream = new StreamReader(tResponse.GetResponseStream()))
            {
                string strResult = tResponseStream.ReadToEnd();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    switch (CurrentTransType)
                    {
                        case Transaction_Type.BROWSE_LABEL:
                            ParseUnreadList(strResult);
                            ((MainPage)thisPage).feedListBox.ItemsSource = myNews;
                            ((MainPage)thisPage).PageTitle.Text = "User Labels";
                            break;
                        case Transaction_Type.GET_USER_INFO:
                            ParseUserInfo(strResult);
                            GetGoogleReaderUnReadCount();
                            break;
                        default:
                            break;
                    }
                });
            }
        }
#region Response Parser Functions
        private void ParseLogin(string responseString)
        {
            string[] arrs = responseString.Split('\n');
            foreach (string arr in arrs)
            {
                //Such as follows, but we only need Auth and SID
                //SID=DQAAALcAAACH5nzd2xVxumGUJGjoTT9DblBfAuI1Sj-6Evtu_91q8Fkbis_S1qv66-vatdUe9HMqqELN3AijrD-CiQyzqJtr_XgTtJIDnfDeVmoGjtsSpkyIB4sES5slrOkTvRJd67dG0tlwTih8btejcPqAVYCJMD6f-a-x3PHa2JQ-Ehsh-rtjNzW6y7Riqosyawffvg4DPbtvUJFsIsum8d32EQ_XuxmUM7NFuhwW5OG8aw3lLO_jO0fLaznl7n5DfCC61KSA\n
                //LSID=DQAAALkAAAAAeAUiJHtt2vL41E3bfMAJwE_waG3qEtAgEsv-Il0xXznDCdm-Z_jQZ4Log9DbGqTMd1-t08udWBJWeQ9VDG0uOC4H5nB0zJ_WGv1E17v3EVeveemKvpu9eN2YBkQlr6hMtZlZWyAb5w0uwAx6kPdXnnuuYC4o0RHv2em0CrOAFzpNYZvLOhuB_veFZ9bsnPy6GP0_HHQGe2o3dJsoJK_DKyq85QteslDzcQySldfwNGUy46Q4HLKhZZPDrjnO_eU\n
                //Auth=DQAAALgAAAAAeAUiJHtt2vL41E3bfMAJta7kSZRtYIzGfm8uJU_jVFjmIFbYYL9WaLS7Xj3xqdwLOrzrBipqL8ItZks4Hf71NY2yTyZnAIG5ysrlA9kCcoZGDDqo3ib9avvgC4pPwXB2uQ3rBYt0gqYs28DkEX6fDD4S3j_NwBESynhOUhcTKqhN3pYX1VfH6uU4285yV7O3w7NKfF8kkTOEFl5toztOwnA4JWnbC5Rjb_gMXKmKnzayTMevgO_XfGWqqNa8x5M\n"	string
                string[] tmp = arr.Split('=');
                if (tmp[0] == "Auth")
                {
                    UserAuth = tmp[1];
                }
                else if (tmp[0] == "SID")
                {
                    UserSid = tmp[1];
                }
            }
        }

        private void ParseToken(string responseString)
        {
            // Content such as "//cZ25t8EO4Xk56b2LOYq9bg"
            //string[] arrs = responseString.Split('/');
            staLoginToken = responseString;
        }

        private void ParseUnreadList(string responseString)
        {
            //JSON Parse
            JObject googleSearch = JObject.Parse(responseString);

            // get JSON result objects into a list
            IList<JToken> results = googleSearch["unreadcounts"].Children().ToList();

            // serialize JSON results into .NET objects
            IList<ServerUnreadResult> searchResults = new List<ServerUnreadResult>();
            myNews.Clear();
            foreach (JToken result in results)
            {
                ServerUnreadResult searchResult = JsonConvert.DeserializeObject<ServerUnreadResult>(result.ToString());
                
                //Parse the label name
                // ex: user/06771113693638414260/label/Win8
                string LabelID = searchResult.id;
                string[] arrs = LabelID.Split('/');
                if (!arrs[0].Contains("user"))
                    continue;
                LabelID = arrs[3];           
                myNews.Add(new NewsItem(   LabelID + "(" + searchResult.count + ")",
                                            "(" + searchResult.count + ")",                                     
                                            LabelID,
                                            searchResult.count,
                                            searchResult.newestItemTimestampUsec,
                                            ""));
            }
        }

        private void ParseLabelATOM(string responseString)
        {
            //JSON Parse
            JObject googleSearch = JObject.Parse(responseString);

            // get JSON result objects into a list
            IList<JToken> results = googleSearch["items"].Children().ToList();

            // serialize JSON results into .NET objects
            IList<ServerLabelATOMResult> searchResults = new List<ServerLabelATOMResult>();
            myNews.Clear();
            foreach (JToken result in results)
            {
                ServerLabelATOMResult searchResult = JsonConvert.DeserializeObject<ServerLabelATOMResult>(result.ToString());
                string newsID = searchResult.id;
                string feedID = result["origin"]["streamId"].ToString();
                string feedTitle = result["origin"]["title"].ToString();
                                
                ///// Get the link of this news.
                string sLink = "";
                IList<JToken> results2 = result["alternate"].Children().ToList();
                foreach (JToken result2s in results2)
                    sLink = result2s["href"].ToString();

                myNews.Add(new NewsItem(searchResult.title,
                                            feedTitle,
                                            newsID,
                                            feedID,
                                            sLink,
                                            ""));
            }
        }

        private void ParseUserInfo(string responseString)
        {
            UserInfoClass = JsonConvert.DeserializeObject<ServerUserInfo>(responseString);
            staUserID = UserInfoClass.userId;
        }
#endregion

        //DATABASE relared
        public bool DB_HasUserExistDB()
        {
            DBHandler tDBHandler = new DBHandler(DBHandler.DBConnection);
            var tUser = from User in tDBHandler.UserInfo
                        select User;
            List<User> tAllUser = new ObservableCollection<User>(tUser).ToList();
            String Password = string.Empty;
            foreach (User tItem in tAllUser)
            {
                return true;
            }
            return false;
        }

        public string DB_GetUserPassword()
        {
            DBHandler tDBHandler = new DBHandler(DBHandler.DBConnection);
            var tUser = from User in tDBHandler.UserInfo
                        select User;
            List<User> tAllUser = new ObservableCollection<User>(tUser).ToList();
            String Password = string.Empty;
            foreach (User tItem in tAllUser)
            {
                return tItem.Password;
            }
            return "";
        }

        public string DB_GetUserName()
        {
            DBHandler tDBHandler = new DBHandler(DBHandler.DBConnection);
            var tUser = from User in tDBHandler.UserInfo
                        select User;
            List<User> tAllUser = new ObservableCollection<User>(tUser).ToList();
            String Password = string.Empty;
            foreach (User tItem in tAllUser)
            {
                return tItem.Email;;
            }
            return "";
        }

        public void DB_SaveUserInfo2DB()
        {
            if (!DB_HasUserExistDB())
            {
                //取得Local Database連線後的DataContext物件
                DBHandler tDBHandler = new DBHandler(DBHandler.DBConnection);
                //產生一個User
                User tNewUser = new User();
                tNewUser.Email = UserEmail;
                tNewUser.Password = UserPasswd;

                //將User變成一個IEnumerable<T>
                List<User> tAllConact = new List<User>();
                tAllConact.Add(tNewUser);

                //新增資料至Local Database
                tDBHandler.UserInfo.InsertAllOnSubmit(tAllConact);
                //發出更新資訊
                tDBHandler.SubmitChanges();                              
            }
        }
    }
}
