using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Services;

namespace WebService_SharePoint
{
    /// <summary>
    /// Summary description for OVS
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class OVS : System.Web.Services.WebService
    {

        [WebMethod]
        public string SendItemsToOVS()
        {

            DB2DataContext db = new DB2DataContext();
            var oitem = (from c in db.STAN_OVs select c).ToList();
            List<OVS.item> list = new List<item>();

            foreach (var i in oitem)
            {
                OVS.item it = new item();
                it.litm = i.PJLITM.Trim();
                it.qty = (int)i.ILOSC025;
                list.Add(it);

            }

            var t = SendData(list, DateTime.Now);

            return oitem.Count().ToString() + ";" + t.Result.ToString();
        }


        private HttpClient GetHttpClient()
        {

            var auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
           System.Text.Encoding.ASCII.GetBytes(
               $"valvexws:jgrval120")));
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = auth;
            return client;

        }


        public  async Task<HttpResponseMessage> SendData(List<OVS.item> its, DateTime dt )
        {
            HttpClient cl = GetHttpClient();
            StringBuilder content = new StringBuilder();
            string _date = dt.Year.ToString() + dt.Month.ToString("D2") + dt.Day.ToString("D2");
            string _time = dt.Hour.ToString("D2") + dt.Minute.ToString("D2") + dt.Second.ToString("D2");


            string header = @"<CACHEDOCUMENTInput><PID>5210</PID><LDATA_LENGTH>2</LDATA_LENGTH><LDATA><![CDATA[<?xml version=""1.0"" encoding=""UTF-8""?><OVS_RealAvailability>";

            foreach (var i in its)
            {
                string content_tmp = @"<RealAvailability><LIFNR>0000021240</LIFNR><IDNLF>@litm</IDNLF><MEINS>szt</MEINS><COM_TIME>@time</COM_TIME><COM_DATE>@date</COM_DATE><Availability>@qty</Availability></RealAvailability>";
                content_tmp = content_tmp.Replace("@litm", i.litm).Replace("@qty", i.qty.ToString()).Replace("@date", _date).Replace("@time", _time);
                content.Append(content_tmp);
            }

           string footer= @"</OVS_RealAvailability>]]></LDATA><NAME>VALVEX_VENDOR_REQUEST</NAME></CACHEDOCUMENTInput>";



            string test = header + content.ToString() + footer;

            
            


            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Put, "http://212.160.102.26:10043/web/services/cacheDocument");
            req.Content = new StringContent(test, Encoding.UTF8, "text/xml");
            HttpResponseMessage httpResponseMessage = cl.SendAsync(req).Result;
            httpResponseMessage.EnsureSuccessStatusCode();
            HttpContent httpContent = httpResponseMessage.Content;
            string responseString = await httpContent.ReadAsStringAsync();
            cl.Dispose();
            return httpResponseMessage;
        }
        public class item
        {
            public string litm { get; set; }
            public int qty { get; set; }

        }

    }
}
