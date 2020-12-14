using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;

namespace WebService_SharePoint
{
    /// <summary>
    /// Summary description for barcode128
    /// </summary>
    public class barcode128 : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {

            string typ = context.Request["kod"];
            Zen.Barcode.Code128BarcodeDraw code128 = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;
            Image im = code128.Draw(typ, 20, 3);
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