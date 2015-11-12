using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web;
using System.IO;
using System.Xml;
using System.Collections.Specialized;
using Newtonsoft.Json;

namespace VkformsLoader
{
    public partial class Form1 : Form
    {
        public string access_token { get; set; }
        public BackgroundWorker worker { get; private set; }
        public string userDirectory { get; private set; }

        public Form1()
        {
            InitializeComponent();
        }

        private void getTokenButtonClick(object sender, EventArgs e)
        {
            this.runJobButton.Enabled = true;
            webBrowser.Navigate(AuthorizeVkForUser());
        }

        private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            if (textBox1.Text != e.Url.ToString())
            {
                textBox1.Text = e.Url.ToString();
                string uri  = e.Url.AbsoluteUri;
                int index = uri.IndexOf('#');
                uri = uri.Remove(0, index + 1);
                access_token = HttpUtility.ParseQueryString(uri).Get("access_token");
                if(access_token == string.Empty || access_token == null)
                {
                    MessageBox.Show("No Token! Cann't proceed!");
                    this.runJobButton.Enabled = false;
                }
            }
        }

        private string ToQueryString(NameValueCollection nvc)
        {
            var array = (from key in nvc.AllKeys
                         from value in nvc.GetValues(key)
                         select string.Format("{0}={1}", /*HttpUtility.UrlEncode*/(key), /*HttpUtility.UrlEncode*/(value)))
                .ToArray();
            return string.Join("&", array);
        }

        

        private void processtoken_Click(object sender, EventArgs e)
        {
            runJobButton.Enabled = false;
            cancelJobButton.Enabled = true;
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;

            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowNewFolderButton = true;
            fbd.Description = @"Choose a folder to save photos";
            if(fbd.ShowDialog(this) == DialogResult.OK)
            {
                userDirectory = fbd.SelectedPath;
            }
            else
            {
                return;
            }

            bw.DoWork += Bw_DoWork;
            bw.ProgressChanged += Bw_ProgressChanged;
            bw.RunWorkerCompleted += Bw_RunWorkerCompleted;
            if (this.userDirectory != String.Empty)
            {
                bw.RunWorkerAsync();
            } 
            else
            {
                MessageBox.Show("Please select a dir where to save pictures!");
            }
            this.worker = bw;
        }

        private void Fbd_Disposed(object sender, EventArgs e)
        {
            userDirectory = (sender as FolderBrowserDialog).SelectedPath;
        }

        private void Bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Cancelled)
            {
                MessageBox.Show("Job was cancelled");
            }
            else
            {
                MessageBox.Show("Done");
            }
               
            runJobButton.Enabled = true;
            progressBar1.Value = 0;
            cancelJobButton.Enabled = false;
    
        }

        private void Bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void Bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            if ((worker.CancellationPending == true))
            {
                e.Cancel = true;
            }
            else
            {
                // Perform a time consuming operation and report progress.
                //this.worker = worker;
                doWork(worker);

            }
        }

        private void doWork(BackgroundWorker worker)
        {
            if (this.access_token == String.Empty)
            {
                this.worker.CancelAsync();
            }
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("user_id", "20925567");
            nvc.Add("fields", "name, secondname, sex, photo_200_orig, photo_big, relation");
            var queryString = ToQueryString(nvc);
            var jsonString = callVkMethod("friends.get", queryString);
            var users = processJson(jsonString);
            downloadPictures(users, worker);
        }

        private void downloadPictures(dynamic users, BackgroundWorker worker)
        {
            int elementsCount  = (users).Count;
            UInt32 i = 0;
            foreach (var user in users)
            {
                var dict = user.ToObject<Dictionary<string, string>>();
                if (dict["photo_big"]!=null)
                {
                    string localFilename = userDirectory + @"\" + dict["first_name"]+" "+ dict["last_name"]+".jpg";
                    using (WebClient client = new WebClient())
                    {
                        retry:
                        try
                        {
                              client.DownloadFile(dict["photo_200_orig"], localFilename);
                        }
                        catch (Exception)
                        {
                            goto retry;
                        }
  
                        int percent = (int)((float)i / ((float)elementsCount/100));
                        i++;
                        worker.ReportProgress(percent);
                        if (this.worker.CancellationPending)
                            break;
                    }
                }
            }
        }

        enum RelationStatus
        {
            NotMarried = 1,
            InActiveSearch = 6
        };

        private dynamic processJson(string jsonString)
        {
            var values = JsonConvert.DeserializeObject<Dictionary<string, IList<Object>>>(jsonString);
            var object1 = values["response"];
            var users = object1.Cast<object>().ToList();

            var interestUserList = new List<Newtonsoft.Json.Linq.JObject>();
            
            foreach(Newtonsoft.Json.Linq.JObject entry in users)
            {
                try
                {
                    var dict = entry.ToObject<Dictionary<string, object>>();
                    bool haveInterest = dict.ContainsKey("relation") ? 
                        (dict["relation"].ToString() == RelationStatus.NotMarried.ToString() ||
                         dict["relation"].ToString() == RelationStatus.InActiveSearch.ToString()) : true;
                    if (dict["sex"].ToString() == "1" && haveInterest)
                    {
                        interestUserList.Add(entry);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }

                if (this.worker.CancellationPending)
                {
                    break;
                }
            }
            return interestUserList;
        }

        private string callVkMethod(string methodName, string parameters)
        {
            string url = string.Format("https://api.vk.com/method/{0}?{1}&access_token={2}", methodName, parameters, this.access_token);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            //myReq.Proxy = new WebProxy("proxy.kha.gameloft.org", 3128);
            webRequest.Proxy = WebRequest.DefaultWebProxy;
            webRequest.Credentials = System.Net.CredentialCache.DefaultCredentials; ;
            webRequest.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
            WebResponse response = webRequest.GetResponse();

            Stream stream = response.GetResponseStream();
            string json = null;
            
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                json = reader.ReadToEnd();
            }
            //richTextBox1.Text = json;
            return json;
    }

    private static string AuthorizeVkForUser()
        {
            //String s = String.Format("The current price is {0} per ounce.",
            //pricePerOunce);
            string appID = "5131032";
            string scope = "friends";
            string redirectURI = "http://api.vkontakte.ru/blank.html";
            string token = null;
            string authorizationURI = String.Format(@"http://api.vkontakte.ru/oauth/authorize?
                                         client_id = {0} &
                                         scope = {1} &
                                         redirect_uri = {2} &
                                         response_type = token", appID, scope, redirectURI, token);
            Regex rgx = new Regex(@"[\n\r\s]+");
            authorizationURI = rgx.Replace(authorizationURI, "");
            return authorizationURI;
        }

        private void cancelJob_Click(object sender, EventArgs e)
        {
            this.worker.CancelAsync();
            cancelJobButton.Enabled = false;
        }
    }
}
