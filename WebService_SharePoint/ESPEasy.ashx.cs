using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Http;
using Newtonsoft.Json;

namespace WebService_SharePoint
{
    /// <summary>
    /// Summary description for ESPEasy
    /// </summary>
    public class ESPEasy : IHttpHandler
    {

        public async void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            context.Response.Write("Hello World");
            string ip = context.Request["ip"];
            string nazwa = context.Request["name"];

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri($"http://{ip}");
            string get_string = await client.GetStringAsync("/json");
            get_string = get_string.Replace("Free RAM", "FreeRAM").Replace("nan", "0").Replace(".00", ""); ;
             

            RootObject ro = JsonConvert.DeserializeObject<RootObject>(get_string);
            ip = "";


            DB2DataContext db = new
               DB2DataContext();

            shelly s = new shelly();
            s.datetime_ev = DateTime.Now;
            s.nazwa = nazwa;
            s.hum = (int)ro.Sensors[0].Humidity;
            s.tC = (int)ro.Sensors[0].Temperature;
            s.tF = 0;
            s.bat_lvl = 0;

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

        public class System
        {
            public int Build { get; set; }
            public int Unit { get; set; }
            public int Uptime { get; set; }
            public int FreeRAM { get; set; }
    }

    public class Sensor
    {
        public string TaskName { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
    }

    public class RootObject
    {
        public System System { get; set; }
        public List<Sensor> Sensors { get; set; }
    }

}
}