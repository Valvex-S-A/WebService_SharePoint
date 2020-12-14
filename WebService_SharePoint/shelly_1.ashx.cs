using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Http;

namespace WebService_SharePoint
{
    /// <summary>
    /// Summary description for shelly_1
    /// </summary>
    public class shelly_1 : IHttpHandler
    {

        public async void ProcessRequest(HttpContext context)
        {
             
             

            context.Response.ContentType = "text/plain";
            context.Response.Write("DONE");
            string ip = context.Request["ip"];
            string nazwa = context.Request["name"];
            DB2DataContext db = new
                DB2DataContext();

            

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri($"http://{ip}");
            HttpResponseMessage response = await client.GetAsync("/status");

            RootObject rt = new RootObject();

            rt = await response.Content.ReadAsAsync<RootObject>();
            string str = await response.Content.ReadAsStringAsync();

            shelly s = new shelly();
            s.datetime_ev = DateTime.Now;
            s.nazwa = nazwa;
            s.hum = (int)rt.hum.value;
            s.tC = (int)rt.tmp.tC;
            s.tF = (int)rt.tmp.tF;
            s.bat_lvl = (int)rt.bat.value;

            db.shellies.InsertOnSubmit(s);
            db.SubmitChanges();

        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }





        public class WifiSta
        {
            public bool connected { get; set; }
            public string ssid { get; set; }
            public string ip { get; set; }
            public int rssi { get; set; }
        }

        public class Cloud
        {
            public bool enabled { get; set; }
            public bool connected { get; set; }
        }

        public class Mqtt
        {
            public bool connected { get; set; }
        }

        public class Tmp
        {
            public double value { get; set; }
            public string units { get; set; }
            public double tC { get; set; }
            public double tF { get; set; }
            public bool is_valid { get; set; }
        }

        public class Hum
        {
            public double value { get; set; }
            public bool is_valid { get; set; }
        }

        public class Bat
        {
            public int value { get; set; }
            public double voltage { get; set; }
        }

        public class Update
        {
            public string status { get; set; }
            public bool has_update { get; set; }
            public string new_version { get; set; }
            public string old_version { get; set; }
        }

        public class RootObject
        {
            public WifiSta wifi_sta { get; set; }
            public Cloud cloud { get; set; }
            public Mqtt mqtt { get; set; }
            public string time { get; set; }
            public int serial { get; set; }
            public bool has_update { get; set; }
            public string mac { get; set; }
            public bool is_valid { get; set; }
            public Tmp tmp { get; set; }
            public Hum hum { get; set; }
            public Bat bat { get; set; }
            public List<string> act_reasons { get; set; }
            public int connect_retries { get; set; }
            public Update update { get; set; }
            public int ram_total { get; set; }
            public int ram_free { get; set; }
            public int fs_size { get; set; }
            public int fs_free { get; set; }
            public int uptime { get; set; }
        }
    }
}