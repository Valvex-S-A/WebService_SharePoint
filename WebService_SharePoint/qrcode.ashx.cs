using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;

namespace WebService_SharePoint
{
    /// <summary>
    /// Summary description for qrcode
    /// </summary>
    public class qrcode : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {

            string typ = context.Request["typ"];
            string nr = context.Request["nr"];

            

            
            Zen.Barcode.CodeQrBarcodeDraw qr = Zen.Barcode.BarcodeDrawFactory.CodeQr;
            Image im = qr.Draw("http://192.168.1.20:10001/potw.aspx?nr=" + nr ?? "", 3, 2);
            MemoryStream ms = new MemoryStream();



            im.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

            context.Response.ContentType = "image/png";
            context.Response.BinaryWrite(ms.GetBuffer());


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