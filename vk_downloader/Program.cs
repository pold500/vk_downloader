using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Json;
using System.Web;
using System.Net;
using System.Text.RegularExpressions;

namespace vk_downloader
{
    class Program
    {
        static void Main(string[] args)
        {
            string mail = "paveld500@gmail.com", password = "12345";

            AuthorizeVkForUser(mail, password);
        }

        private static void AuthorizeVkForUser(string mail, string password)
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
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(authorizationURI);
            //myReq.Proxy = new WebProxy("proxy.kha.gameloft.org", 3128);
            webRequest.Proxy = WebRequest.DefaultWebProxy;
            webRequest.Credentials = System.Net.CredentialCache.DefaultCredentials; ;
            webRequest.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
            WebResponse response = webRequest.GetResponse();
            
        }
    }
}
