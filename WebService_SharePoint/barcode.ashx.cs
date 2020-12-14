using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;

namespace WebService_SharePoint
{
    /// <summary>
    /// Summary description for barcode
    /// </summary>
    public class barcode : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            MemoryStream ms = new MemoryStream();
            Image im;
            string type = context.Request["type"];
            string data = context.Request["data"];
            string scale = context.Request["size"];
            string height = context.Request["height"];


            switch (type)
            {
                case "code128":
                    {
                        Zen.Barcode.Code128BarcodeDraw code128 = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;
                        im = code128.Draw(data, int.Parse(height), int.Parse(scale));
                        im.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        break;
                    }
                case "qrcode":
                    {
                        Zen.Barcode.CodeQrBarcodeDraw code128 = Zen.Barcode.BarcodeDrawFactory.CodeQr;
                        im = code128.Draw(data, int.Parse(height), int.Parse(scale));
                        im.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        break;
                    }

                case "ean13":
                    {
                        Zen.Barcode.CodeEan13BarcodeDraw code128 = Zen.Barcode.BarcodeDrawFactory.CodeEan13WithChecksum;
                        im = code128.Draw(data, int.Parse(height), int.Parse(scale));
                        im.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        break;
                    }

            }

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