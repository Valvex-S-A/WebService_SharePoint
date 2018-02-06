using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebService_SharePoint
{
    /// <summary>
    /// Summary description for metka1
    /// </summary>
    public class metka1 : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            

            string IP = context.Request["IP"];
            string port = context.Request["port"];
            string kod = context.Request["kod"];
            string typ = context.Request["typ"];

            Service1 srv = new Service1();
            int port_ = 9100;
            int.TryParse(port, out port_);

            srv.JDE_Drukuj_metkę(IP, port_, kod, typ,1);

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
}