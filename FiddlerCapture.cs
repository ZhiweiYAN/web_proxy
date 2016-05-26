using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Fiddler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using HtmlAgilityPack;
//using System.Net.Http;
//using System.Collections.Specialized;

namespace WebSurge
{
    public partial class FiddlerCapture : Form
    {
        private const string Separator = "------------------------------------------------------------------";
        private UrlCaptureConfiguration CaptureConfiguration { get; set; }

        //bool isFirstSslRequest;
        //Read config parameters

        public static string short_sleep = ConfigurationManager.AppSettings["short_sleep"];
        public static string refresh_delay = ConfigurationManager.AppSettings["refresh_delay"];

        public static string posturl_article = ConfigurationManager.AppSettings["posturl_article"];
        public static string posturl_task = ConfigurationManager.AppSettings["posturl_task"];

        public static string posturl_register = ConfigurationManager.AppSettings["posturl_register"];
        public static string redirect_srv_url = ConfigurationManager.AppSettings["redirect_srv_url"];

        public static string intercept_webpage = ConfigurationManager.AppSettings["intercept_webpage"];
        public static string intercept_readingnum_json = ConfigurationManager.AppSettings["intercept_readingnum_json"];
        public static string intercept_jsreport = ConfigurationManager.AppSettings["intercept_jsreport"];
        public static string intercept_appmsg_comment = ConfigurationManager.AppSettings["intercept_appmsg_comment"];


        public string HttpPost(string url, string postData)
        {

            //return "200 OK";
            //using (var client = new WebClient())
            //{
            //    var values = new NameValueCollection();
            //    values["key"] = "register";
            //    values["value"] = postData;

            //    var response = client.UploadValues(url, values);

            //    var responseString = Encoding.Default.GetString(response);

            //    return responseString;
            //}

            byte[] postBytes = Encoding.GetEncoding("utf-8").GetBytes(postData);

            var httpRequest = (HttpWebRequest)WebRequest.Create(url);

            httpRequest.Timeout = 5000;
            httpRequest.Method = "POST";
            httpRequest.ContentType = "application/x-www-form-urlencoded";
            httpRequest.ContentLength = postBytes.Length;
            httpRequest.UserAgent = "IE6.0";
            httpRequest.KeepAlive = true;

            //HttpWebResponse response;
            //StreamReader reader;

            try
            {
                Stream writer = httpRequest.GetRequestStream();
                writer.Write(postBytes, 0, postBytes.Length);
                writer.Close();

                HttpWebResponse response = (HttpWebResponse)httpRequest.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("UTF-8"));
                string retString = reader.ReadToEnd();
                reader.Close();
                response.Close();
                Console.WriteLine(retString);
                return retString;
            }
            catch (Exception e)
            {
                Console.WriteLine("Connection failed. " + e.Message);
                return "Error";
            }

        }

        public string HttpGet(string url)
        {
            //return "HTTPGET";

            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
                httpRequest.Timeout = 1000;
                httpRequest.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)httpRequest.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("UTF-8"));
                string retString = reader.ReadToEnd();
                response.Close();
                reader.Close();
                return retString;
            }
            catch (Exception e)
            {
                Console.WriteLine("Connection failed. " + e.Message);
                return "Error";
            }
        }

        public FiddlerCapture()
        {
            InitializeComponent();

            CaptureConfiguration = new UrlCaptureConfiguration();  // this usually comes from configuration settings

            Start();

        }

        private void FiddlerCapture_Load(object sender, EventArgs e)
        {
            tbIgnoreResources.Checked = CaptureConfiguration.IgnoreResources;
            txtCaptureDomain.Text = CaptureConfiguration.CaptureDomain;

            UpdateButtonStatus();

            try
            {
                var processes = Process.GetProcesses().OrderBy(p => p.ProcessName);
                foreach (var process in processes)
                {
                    txtProcessId.Items.Add(process.ProcessName + "  - " + process.Id);
                }
            }
            catch { }

        }

        //if we fail to connect to the server.
        //private void FiddlerApplication_BeforeReturningError(Session sess)
        //{
        //    if (sess.uriContains(redirect_srv_url))
        //    {
        //        string sErrMsg = sess.GetResponseBodyAsString();
        //        System.Threading.Thread.Sleep(Int32.Parse(short_sleep));
        //        string server_ip_url = redirect_srv_url;
        //        string delay = refresh_delay;
        //        string webpage = "<!DOCTYPE html> <head> "
        //            + "<meta http-equiv=\"refresh\" "
        //            + " content=\"" + delay
        //            + " ;url= " + server_ip_url + " \" > </head> "
        //            + "<title> F-return Error: Re-HTTP-Get: " + server_ip_url + " </title>"
        //            + "<div style=\"width:300px; height:180px; background-color:red;\">"
        //            + "<pre> F-return Error:" + server_ip_url + "</pre>"
        //            + "<pre> F-return Error: " + sErrMsg + "</pre>"
        //            + "<p> " + sess.url + "</p>"
        //            + "</div> </html>";

        //        sess.utilSetResponseBody(webpage);
        //    }

        //}

        //We receive the request of client.
        private void FiddlerApplication_BeforeRequest(Session sess)
        {
            bool ret = false;

            if (sess == null || sess.oRequest == null || sess.oRequest.headers == null)
                return;

            //https
            if (sess.RequestMethod == "CONNECT")
                return;

            //block the other website(server IP) requests. White List are:
            ret = sess.uriContains("120.25.204.250")
                 || sess.uriContains("120.25.205.241")
                 || sess.uriContains("10.116.104.2")
                 || sess.uriContains("10.116.68.154")
                 || sess.uriContains("120.25.145.20")
                 || sess.uriContains("10.116.145.207");
            if (ret)
            {
                return;
            }

            string webpage = "<!DOCTYPE html> <head> "
                      + " </head> "
                      + "<title>Request-BLOCK</title>"
                      + "<div style=\"width:300px; height:180px; background-color:green;\">"
                      + "</div> </html>";


            ret = (sess.uriContains("mp.weixin.qq.com")
                && sess.uriContains("&key=")
                && sess.uriContains("uin"));
            if (ret)
            {
                sess.bBufferResponse = true;
                HttpPost(posturl_register, sess.url + '\n' + sess.RequestHeaders.ToString());
            }
            else
            {
                sess.oRequest.FailSession(404, "Blocked", webpage);
                return;
            }

            //block images.
            if (sess.uriContains(".css") && sess.uriContains(".jpg")
                && sess.uriContains(".gif") && sess.uriContains(".png"))
            {
                sess.oRequest.FailSession(404, "Blocked", webpage);
                return;
            }

        }


        private void FiddlerApplication_BeforeResponse(Session sess)
        {
            bool ret = false;

            if (sess == null || sess.oRequest == null || sess.oRequest.headers == null)
                return;

            if (sess.RequestMethod == "CONNECT")
                return;

            //passby the servers.
            ret = sess.uriContains("120.25.204.250") || sess.uriContains("120.25.205.241")
                 || sess.uriContains("10.116.104.2") || sess.uriContains("10.116.68.154")
                 || sess.uriContains("120.25.145.20") || sess.uriContains("10.116.145.207");
            if (ret)
            {
                return;
            }

            //filter other websites other than qq.com
            ret = sess.uriContains("qq.com");
            if (!ret)
            {
                return;
            }


            //if you wanted to capture the response
            string respHeaders = sess.oResponse.headers.ToString();
            string reqHeaders = sess.oRequest.headers.ToString();

            var respBody = sess.GetResponseBodyAsString();

            //added by yzw
            if (null == respBody)
            {
                return;
            }

            //modify article response html
            if (sess.uriContains("mp.weixin.qq.com") && sess.uriContains("&key=") && sess.uriContains("uin")
                && !sess.uriContains("getmasssendmsg"))
            {

                string server_ip_url = redirect_srv_url;
                string delay = refresh_delay;
                string webpage = "<!DOCTYPE html> <head> "
                    + "<meta http-equiv=\"refresh\" "
                    + " content=\"" + delay
                    + " ;url= " + server_ip_url + " \" > </head> "
                    + "<title> F-BeforeResPonse: " + server_ip_url + " </title>"
                    + "<div style=\"width:300px; height:180px; background-color:yellow;\">"
                    + "<pre> F-BeforeResPonse:" + server_ip_url + "</pre>"
                    + "<p> " + sess.url + "</p>"
                    + "</div> </html>";
                sess.utilSetResponseBody(webpage);
                return;
            }

            //post read num json
            if (sess.uriContains(intercept_readingnum_json))
            {

                /*  The packet is json format, such as
                 * { "advertisement_num":0,
                 *   "advertisement_info":[],
                 *   "appmsgstat":
                 *        {"show":true,
                 *         "is_login":true,
                 *         "liked":false,
                 *         "read_num":94193,
                 *         "like_num":5596,
                 *         "ret":0,
                 *         "real_read_num":94193
                 *        },
                 *   "comment_enabled":1,
                 *   "reward_head_imgs":[],
                 *   "base_resp":
                 *        {"wxtoken":1547698538}
                 *  }
                 */

                //remove AD text
                var index_appmsgstat = respBody.IndexOf("appmsgstat");
                string respBody_noAD = respBody.Substring(index_appmsgstat, respBody.Length - index_appmsgstat);
                respBody_noAD = "{\"advertisement_num\":0,\"advertisement_info\":[],\"" + respBody_noAD;

                //send the packet to a weixin_article server.
                HttpPost(posturl_article + "intercept_readingnum_json", respBody_noAD);

                //return the packet to WeiXin client(APP).
                sess.utilSetResponseBody(respBody_noAD);
                return;
            }


            if (sess.uriContains(intercept_jsreport) || sess.uriContains(intercept_appmsg_comment))
            {
                sess.oResponse.headers.HTTPResponseStatus = "404 OK";
                sess.oResponse["Content-Type"] = "text/html; charset=UTF-8";
                sess.oResponse["Cache-Control"] = "private, max-age=0";
                sess.utilSetResponseBody("Fiddler Block the request.\n\r");
                return;
            }

            int index_imagebegin, index_imageend;
            var indexnum = respBody.IndexOf("msgList ");

            //if webpage is a message list.
            if (indexnum != -1)
            {
                //parsing message list from the html web page.
                index_imagebegin = respBody.IndexOf("'", indexnum);
                index_imageend = respBody.IndexOf("'", index_imagebegin + 1);
                string respBodydata = respBody.Substring(index_imagebegin + 1, index_imageend - index_imagebegin - 1);
                respBodydata = respBodydata.Replace("&quot;", "\"").Replace("&nbsp;", " ");

                JObject list = JObject.Parse(respBodydata);
                int count = list["list"].Count();

                //Fetch the message, at most 5
                int max_msg_number = 2;
                int fetch_article_num = 0;
                if (count > max_msg_number)
                {
                    fetch_article_num = max_msg_number;
                }
                else
                {
                    fetch_article_num = count;
                }

                string htmlbody = "<!DOCTYPE html><html lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\"><head><meta charset=\"utf-8\" /><title></title></head><body><font size=\"20px\">";

                int article_num = 0;
                string postdata = "";
                string datatime = "";
                bool fist_time = true;
                for (int i = 0; i < fetch_article_num; i++)
                {
                    string prefix_url = "http://mp.weixin.qq.com/s";
                    int prefix_url_len = prefix_url.Length;
                    if (list["list"][i]["app_msg_ext_info"] != null)
                    {
                        //if (countnum >= 10) { break;}
                        //string title = list["list"][i]["app_msg_ext_info"]["title"].ToString();
                        string marker = "&nbsp&nbsp&nbsp0";
                        string url = list["list"][i]["app_msg_ext_info"]["content_url"].ToString().Replace("\\", "").Replace("amp;", "");

                        if (url.Length > prefix_url.Length && url.Substring(0, prefix_url_len) == prefix_url)
                        {
                            if (fist_time && url != "")
                            {
                                HttpPost(posturl_register, url);
                                fist_time = false;

                            }
                            if (list["list"][i]["comm_msg_info"]["datetime"] != null)
                            {
                                datatime = list["list"][i]["comm_msg_info"]["datetime"].ToString();
                            }
                            else
                            {
                                datatime = "";
                            }
                            postdata = postdata + url + ";" + datatime + "|";
                            htmlbody = htmlbody + "<a style=\"Background:red\" href=\"" + url + "\">" + marker + "</a>&nbsp&nbsp";
                            article_num++;
                        }

                        if (list["list"][i]["app_msg_ext_info"]["multi_app_msg_item_list"] != null)
                        {
                            int msg_item_list_count = list["list"][i]["app_msg_ext_info"]["multi_app_msg_item_list"].Count();
                            for (int j = 0; j < msg_item_list_count; j++)
                            {
                                //if (countnum >= 10) { break; }
                                url = list["list"][i]["app_msg_ext_info"]["multi_app_msg_item_list"][j]["content_url"].ToString().Replace("\\", "").Replace("amp;", "");

                                //htmlbody = htmlbody + "<a href=\"" + url + "&time=" + list["list"][i]["comm_msg_info"]["datetime"] + "\">" + title + "</a>&nbsp&nbsp";
                                if (url.Length > prefix_url.Length && url.Substring(0, prefix_url_len) == prefix_url)
                                {

                                    htmlbody = htmlbody + "<a style=\"Background:red\" href=\"" + url + "\">" + marker + "</a>&nbsp&nbsp";
                                    postdata = postdata + url + ";" + datatime + "|";
                                    article_num++;
                                }
                            }
                            if (article_num > 0)
                            {
                                htmlbody = htmlbody + "<br>";
                                article_num = 0;
                            }

                        }
                    }
                }
                //Send the message urls to webpy server <weixin_task.py>
                // postdata = postdata + list["list"][i]["comm_msg_info"]["datetime"];
                HttpPost(posturl_task + "article_url", postdata);
                htmlbody = htmlbody + "<a style=\"Background:black\">EN </a></h1></body></html>";
                sess.utilSetResponseBody(htmlbody);
            }

            //if the webpage is an article
            if (sess.uriContains("mp.weixin.qq.com/s?__"))
            {

                // remove the unsessary contents in the webpage
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(respBody);
                HtmlAgilityPack.HtmlNode node;

                var nodesToRemove = htmlDoc.DocumentNode.SelectNodes("//img");
                if (nodesToRemove != null)
                {
                    foreach (var n in nodesToRemove.ToList())
                        n.Remove();
                }
                nodesToRemove = htmlDoc.DocumentNode.SelectNodes("//iframe");
                if (nodesToRemove != null)
                {
                    foreach (var n in nodesToRemove.ToList())
                        n.Remove();
                }

                /* 
                node = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='js_content']");
                if (node != null)
                {
                    node.ParentNode.RemoveChild(node);
                }
               
                node = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='rich_media_area_extra']");
                if (node != null)
                {
                    node.ParentNode.RemoveChild(node);
                }
                */

                node = htmlDoc.DocumentNode.SelectSingleNode("//script[@id='voice_tpl']");
                if (node != null)
                {
                    node.ParentNode.RemoveChild(node);
                }
                node = htmlDoc.DocumentNode.SelectSingleNode("//script[@id='qqmusic_tpl']");
                if (node != null)
                {
                    node.ParentNode.RemoveChild(node);
                }
                node = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='js_cmt_mine']");
                if (node != null)
                {
                    node.ParentNode.RemoveChild(node);
                }

                node = htmlDoc.DocumentNode.SelectSingleNode("//a[@id='js_view_source']");
                if (node != null)
                {
                    node.ParentNode.RemoveChild(node);
                }

                nodesToRemove = htmlDoc.DocumentNode.SelectNodes("//p");
                if (nodesToRemove != null)
                {
                    foreach (var n in nodesToRemove.ToList())
                        n.Remove();
                }
                nodesToRemove = htmlDoc.DocumentNode.SelectNodes("//section");
                if (nodesToRemove != null)
                {
                    foreach (var n in nodesToRemove.ToList())
                        n.Remove();
                }
                nodesToRemove = htmlDoc.DocumentNode.SelectNodes("//link");
                if (nodesToRemove != null)
                {
                    foreach (var n in nodesToRemove.ToList())
                        n.Remove();
                }

                //var TitleNode = htmlDoc.DocumentNode.SelectSingleNode("//h2[@id='activity-name']");
                //if (null != TitleNode)
                //{
                //    htmlDoc.DocumentNode.AppendChild(TitleNode);
                //    String inner_url = HttpGet("http://120.25.204.250:9537/request_url?client=c&last_url=OK");

                //    if (inner_uriContains("none") || inner_uriContains("error"))
                //    {
                //        TitleNode.InnerHtml = "<button "
                //                             + "style=\"width:100%;height:200px;background:maroon\" "
                //                             + "onclick=\"nextpage()\"> NO PAGE </button>\n"
                //                             + "<script>function nextpage(){"
                //                             + "location.href = \"" + inner_url + "\""
                //                             + ";}</script>"
                //                             + "<p>" + TitleNode.InnerText.Trim() + "</p>";
                //    }
                //    else
                //    {
                //        //TitleNode.InnerHtml = "<a href='" + inner_url + "'>" + "NEXT PAGE" + "</a>"
                //        //                      + "<p > " + inner_url + "</p>"
                //        //                      + "<p>" + TitleNode.InnerText.Trim() + "</p>";

                //        TitleNode.InnerHtml = "<button "
                //                             + "style=\"width:100%;height:200px;background:green\" "
                //                             + "onclick=\"nextpage()\"> NEXT PAGE </button>\n"
                //                             + "<script>function nextpage(){"
                //                             + "location.href = \"" + inner_url + "\""
                //                             + ";}</script>"
                //                             + "<p>" + TitleNode.InnerText.Trim() + "</p>"
                //                             + "<p>" + inner_url + "</p>";

                //    }
                //}

                sess.utilSetResponseBody(htmlDoc.DocumentNode.OuterHtml);
                if (sess.uriContains(intercept_webpage))
                {
                    HttpPost(posturl_article + "intercept_webpage", "<abc>" + sess.url + "</abc>" + respBody);
                }
            }

        }

        private void FiddlerApplication_AfterSessionComplete(Session sess)
        {
            if (sess == null || sess.oRequest == null || sess.oRequest.headers == null)
                return;

            // Ignore HTTPS connect requests
            if (sess.RequestMethod == "CONNECT")
                return;

            string headers = sess.oRequest.headers.ToString();

            // replace the HTTP line to inject full URL
            string firstLine = sess.RequestMethod
                                    + " " + sess.fullUrl
                                    + " " + sess.oRequest.headers.HTTPVersion;
            int at = headers.IndexOf("\r\n");
            if (at < 0)
                return;
            headers = firstLine + "\r\n" + headers.Substring(at + 1);

            string output = "\r\nHEADER:\r\n" + headers + "\r\n";

            BeginInvoke(new Action<string>((text) =>
            {
                txtCapture.Text = string.Empty;
                txtCapture.AppendText(text);
                UpdateButtonStatus();
            }), output);
        }

        void Start()
        {
            if (tbIgnoreResources.Checked)
                CaptureConfiguration.IgnoreResources = true;
            else
                CaptureConfiguration.IgnoreResources = false;

            string strProcId = txtProcessId.Text;
            if (strProcId.Contains('-'))
                strProcId = strProcId.Substring(strProcId.IndexOf('-') + 1).Trim();

            strProcId = strProcId.Trim();

            int procId = 0;
            if (!string.IsNullOrEmpty(strProcId))
            {
                if (!int.TryParse(strProcId, out procId))
                    procId = 0;
            }
            CaptureConfiguration.ProcessId = procId;
            CaptureConfiguration.CaptureDomain = txtCaptureDomain.Text;


            FiddlerApplication.AfterSessionComplete += FiddlerApplication_AfterSessionComplete;
            FiddlerApplication.BeforeResponse += FiddlerApplication_BeforeResponse;
            //FiddlerApplication.BeforeReturningError += FiddlerApplication_BeforeReturningError;
            FiddlerApplication.BeforeRequest += FiddlerApplication_BeforeRequest;

            const FiddlerCoreStartupFlags flags =
                FiddlerCoreStartupFlags.AllowRemoteClients |
                FiddlerCoreStartupFlags.CaptureLocalhostTraffic |
                FiddlerCoreStartupFlags.DecryptSSL |
                FiddlerCoreStartupFlags.MonitorAllConnections |
                FiddlerCoreStartupFlags.RegisterAsSystemProxy;

            Fiddler.CONFIG.IgnoreServerCertErrors = true;

            FiddlerApplication.Startup(8888, flags);
        }


        void Stop()
        {
            FiddlerApplication.AfterSessionComplete += FiddlerApplication_AfterSessionComplete;
            FiddlerApplication.BeforeResponse -= FiddlerApplication_BeforeResponse;
            //FiddlerApplication.BeforeReturningError -= FiddlerApplication_BeforeReturningError;
            FiddlerApplication.BeforeRequest -= FiddlerApplication_BeforeRequest;
            if (FiddlerApplication.IsStarted())
                FiddlerApplication.Shutdown();
        }



        public static bool InstallCertificate()
        {
            if (!CertMaker.rootCertExists())
            {
                if (!CertMaker.createRootCert())
                    return false;

                if (!CertMaker.trustRootCert())
                    return false;
            }

            return true;
        }

        public static bool UninstallCertificate()
        {
            if (CertMaker.rootCertExists())
            {
                if (!CertMaker.removeFiddlerGeneratedCerts(true))
                    return false;
            }
            return true;
        }

        private void ButtonHandler(object sender, EventArgs e)
        {
            if (sender == tbCapture)
                Start();
            else if (sender == tbStop)
                Stop();
            else if (sender == tbSave)
            {
                var diag = new SaveFileDialog()
                {
                    AutoUpgradeEnabled = true,
                    CheckPathExists = true,
                    DefaultExt = "txt",
                    Filter = "Text files (*.txt)|*.txt|All Files (*.*)|*.*",
                    OverwritePrompt = false,
                    Title = "Save Fiddler Capture File",
                    RestoreDirectory = true
                };
                var res = diag.ShowDialog();

                if (res == DialogResult.OK)
                {
                    if (File.Exists(diag.FileName))
                        File.Delete(diag.FileName);

                    File.WriteAllText(diag.FileName, txtCapture.Text);


                }
            }
            else if (sender == tbClear)
            {
                txtCapture.Text = string.Empty;
            }
            else if (sender == btnInstallSslCert)
            {
                Cursor = Cursors.WaitCursor;
                InstallCertificate();
                Cursor = Cursors.Default;
            }
            else if (sender == btnUninstallSslCert)
                UninstallCertificate();

            UpdateButtonStatus();
        }



        private void FiddlerCapture_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop();
            System.Environment.Exit(0);

        }


        public void UpdateButtonStatus()
        {
            tbCapture.Enabled = !FiddlerApplication.IsStarted();
            tbStop.Enabled = !tbCapture.Enabled;
            tbSave.Enabled = txtCapture.Text.Length > 0;
            tbClear.Enabled = tbSave.Enabled;

            btnInstallSslCert.Enabled = !CertMaker.rootCertExists();
            btnUninstallSslCert.Enabled = !btnInstallSslCert.Enabled;

            CaptureConfiguration.IgnoreResources = tbIgnoreResources.Checked;
        }

    }


}
