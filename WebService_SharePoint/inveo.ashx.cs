using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Xml.Serialization;

namespace WebService_SharePoint
{
    /// <summary>
    /// Handler do odczytu temperatury z Inveo na serwerowni
    /// </summary>
    public class inveo : IHttpHandler
    {

        public async void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";

            try
            {
                string ip = context.Request["ip"];
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri($"http://{ip}");
                HttpResponseMessage response = await client.GetAsync("/status.xml");
                string str = await response.Content.ReadAsStringAsync();
                var reader = new StringReader(str.Replace("iso-8859-1", "utf-8"));

                XmlSerializer serializer = new XmlSerializer(typeof(Response));
                Response resp = (Response)serializer.Deserialize(reader);
                DB2DataContext db = new DB2DataContext();
                shelly temp = new shelly();
                temp.bat_lvl = 100;
                temp.tC = (int)resp.Temp1;
                temp.tF = 0;
                if (ip == "192.168.1.2") temp.nazwa = "Wysyłane - kotłownia"; else temp.nazwa = "?";
                temp.datetime_ev = DateTime.Now;
                temp.hum = -1;
                db.shellies.InsertOnSubmit(temp);
                db.SubmitChanges();
            }
            catch 
            {

              
            }

            context.Response.Write("OK");
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }

    [XmlRoot(ElementName = "response")]
    public class Response
    {
        [XmlElement(ElementName = "prod_name")]
        public string Prod_name { get; set; }
        [XmlElement(ElementName = "sv")]
        public string Sv { get; set; }
        [XmlElement(ElementName = "mac")]
        public string Mac { get; set; }
        [XmlElement(ElementName = "out")]
        public string Out { get; set; }
        [XmlElement(ElementName = "on")]
        public string On { get; set; }
        [XmlElement(ElementName = "in")]
        public string In { get; set; }
        [XmlElement(ElementName = "counter1")]
        public string Counter1 { get; set; }
        [XmlElement(ElementName = "temp1")]
        public double Temp1 { get; set; }
    }



}