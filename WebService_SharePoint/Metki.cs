using Microsoft.Win32.SafeHandles;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Web;

namespace WebService_SharePoint
{

    public struct metka
    {
        public int nr_zlec_szlif;
        public int nr_zlec_galw;
        public string nazwa;
        public string kod_zlecenia;
        public string kod_materialu_szlif;
        public string kod_materialu_galw;
        
        public string nr_rysunku;
        public double[] ilosc_szt;
        public System.Drawing.Image rysunek;
         
        public string kolor;
        public string userid;
        public string komentarz;
        public int il_stron;



    }

    public class Metki
    {




    }

    public static class GenPDF
    {

        public static byte[] GenPDFFile_old(metka m_)
        {
            PdfDocument document = new PdfDocument();
            Zen.Barcode.Code128BarcodeDraw bc = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;
            Zen.Barcode.Code128BarcodeDraw bc1 = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;
            DB2DataContext db2 = new DB2DataContext();

            bool tylko_poler_wyk = false;// m_.komentarz.Contains("!!");


            int ilosc = 0;
            for (int i = 0; i < m_.il_stron; i++)
            {
                PdfPage page = document.AddPage();
                page.Size = PdfSharp.PageSize.A5;
                // Get an XGraphics object for drawing
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont font = new XFont("Verdana", 15, XFontStyle.Bold);
                XFont font1 = new XFont("Verdana", 10, XFontStyle.Regular);
                XFont font2 = new XFont("Verdana", 8, XFontStyle.Regular);
                XFont font3 = new XFont("Verdana", 8, XFontStyle.Italic);

                // kod kreskowy
                Image im = bc.Draw(m_.nr_zlec_szlif + "_" + (i + 1).ToString(), 35, 2);
                XImage xim = XImage.FromGdiPlusImage(im);
                gfx.DrawImage(xim, new Point(10, 10));
                gfx.DrawString("Utw.: " + DateTime.Now.ToString(), font1, XBrushes.Black,
                new XRect(210, 10, 300, 22),
               XStringFormats.TopLeft);



                gfx.DrawString(m_.nazwa, font, XBrushes.Black,
                   new XRect(5, 40, 400, 22),
                  XStringFormats.Center);



                gfx.DrawString("Kod części: " + m_.kod_zlecenia + " (partia nr " + (i + 1).ToString() + " z " + m_.il_stron.ToString() + ")", font1, XBrushes.Black,
                   new XRect(5, 65, 300, 22),
                  XStringFormats.TopLeft);
                gfx.DrawString("Ilość szt: " + m_.ilosc_szt[i].ToString(), font1, XBrushes.Black,
                new XRect(240, 65, 300, 22),
               XStringFormats.TopLeft);
                ilosc =  (int)m_.ilosc_szt[0];
                gfx.DrawString("Nr rysunku: " + m_.nr_rysunku, font1, XBrushes.Black,
                  new XRect(5, 80, 300, 22),
                 XStringFormats.TopLeft);
               
                
                
                
                
                //gfx.DrawString("Kod po wykonczeniu: " + m_.kod_po_wykonczeniu, font1, XBrushes.Black,
                //  new XRect(5, 95, 300, 22),
                // XStringFormats.TopLeft);
                gfx.DrawString("Kolor: " + m_.kolor, font1, XBrushes.Black,
               new XRect(240, 75, 300, 22),
              XStringFormats.TopLeft);
                gfx.DrawString("Nr_zlec: " + m_.nr_zlec_szlif, font1, XBrushes.Black,
               new XRect(240, 95, 300, 22),
              XStringFormats.TopLeft);

                var rw = from c in db2.IPO_ZDAWKA_PW
                         where c.RW_PW == "RW" && c.Nr_zlecenia_IPO == m_.nr_zlec_szlif
                         group c by new {c.Magazyn_IPO,c.Nr_indeksu} into fgr
                        
                         select new { Magazyn_IPO=fgr.Key.Magazyn_IPO, Nr_indeksu=fgr.Key.Nr_indeksu, Ilosc=fgr.Sum(g =>g.Ilosc) };
                int poz = 105;
                foreach (var d in rw.Where(c => c.Ilosc != 0))
                {

                    gfx.DrawString(d.Magazyn_IPO + " " + d.Nr_indeksu + " :" + d.Ilosc.ToString(), font1, XBrushes.Black,
              new XRect(240, poz, 300, 22),
             XStringFormats.TopLeft);



                    poz = poz + 10;
                }
                szlif_operacjeDataContext db1 = new szlif_operacjeDataContext();
                DBDataContext db2008 = new DBDataContext();

                string kod_szlif = m_.kod_zlecenia;

                try
                {
                    var idx = (from c in db2008.SLOWNIK_1s where c.IMLITM.Trim() == kod_szlif select c).First();


                    if (idx.KOD_PLAN == "GALWANIKA")
                    {
                        var rozpis_zlecenia = (from c in db2008.IPO_Rozpis_mats
                                               where c.wyrob_l.Trim() == m_.kod_zlecenia.Trim()
                                               select c).First();
                        kod_szlif = rozpis_zlecenia.skladnik_l.Trim();
                    }

                }
                catch { }
                double st = 170;

                var oper = (from c in db1.Marszruty_szlifiernia_s
                            where c.Id_wyrobu == kod_szlif
                            orderby c.Nr_kol_operacji ascending
                            select new { c.Id_operacji, OPERACJA = c.Id_operacji + " " + c.Nazwa_operacji, c.IloscSztZm, c.NormaZatwierdzona, c.Nazwa_operacji, c.Nr_kol_operacji }); ;

                if (tylko_poler_wyk)
                {
                    oper = oper.Where(x => x.Nazwa_operacji == "Polerowanie wykańczające");
                }
                else
                {
                    oper = oper.Where(x => x.Nazwa_operacji != "Polerowanie wykańczające");
                }

                var noper = oper.ToList();


                foreach (var o in noper)
                {
                    string wst = "";

                    if (o.NormaZatwierdzona.Contains("*")) wst = "*";
                    if (!o.Nr_kol_operacji.Value.ToString().EndsWith("0")) wst = "A" + wst ;  //operacja nie kończy się na zero - to alternatywa!!!

                    //jeżeli wst zawiera A to drukuj pochyłą czcionką
                    if (wst.Contains("A"))
                    {
                        gfx.DrawString(wst + o.OPERACJA, font3, XBrushes.Black,
                   new XRect(240, st, 300, 22),
                  XStringFormats.TopLeft);
                    }
                    //lub normalną jeżeli nie alternatywna
                    else {

                        gfx.DrawString(wst + o.OPERACJA, font2, XBrushes.Black,
                   new XRect(240, st, 300, 22),
                  XStringFormats.TopLeft);
                    }


                    im = bc1.Draw("OPER_" + o.Id_operacji.ToString(), 15, 2);
                    xim = XImage.FromGdiPlusImage(im);
                    gfx.DrawImage(xim, new Point(230, (int)(st + 15)));
                    gfx.DrawString(((decimal)((decimal)m_.ilosc_szt[i] * 480m) / (decimal)o.IloscSztZm).ToString("####.#") + "/" + o.IloscSztZm.ToString(), font1, XBrushes.Black,
               new XRect(240, st + 26, 300, 22),
              XStringFormats.TopLeft);
                    st = st + 55;
                }


                MemoryStream ms = new MemoryStream();
                m_.rysunek.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                Image n_rys = System.Drawing.Image.FromStream(ms);
                double r_width = 150;
                double ratio = r_width / n_rys.Width;
                double r_height = (double)n_rys.Height * ratio;
                XImage rys = XImage.FromGdiPlusImage(n_rys);

                gfx.DrawImage(rys, 10, 130, r_width, r_height);
                Image im1 = bc.Draw(m_.kod_zlecenia, 20, 3);
                XImage xim1 = XImage.FromGdiPlusImage(im1);
                gfx.DrawImage(xim1, new Point(15, 150 + (int)r_height));
            }







            // Save the document...
            //string filename = "HelloWorld.pdf";
            MemoryStream str = new MemoryStream();
            document.Save(str, true);
            Metki_PDF m_pdf = new Metki_PDF();
            m_pdf.Nr_zlecenia = m_.nr_zlec_szlif.ToString();
            m_pdf.Data_utw = DateTime.Now;
            m_pdf.PDF = str.ToArray();
            m_pdf.Ilosc = ilosc;
            baza_metekDataContext db = new baza_metekDataContext();
            db.Metki_PDFs.InsertOnSubmit(m_pdf);
            db.SubmitChanges();


            return str.ToArray();

        }








        public static byte[] GenMetka_GAL_SZL(metka m_)
        {
            PdfDocument document = new PdfDocument();
            Zen.Barcode.Code128BarcodeDraw bc = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;
            Zen.Barcode.Code128BarcodeDraw bc1 = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;
            Zen.Barcode.Code128BarcodeDraw bc2 = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;
            double ilosc = 0;
            for (int i = 0; i < m_.il_stron; i++)
            {
                PdfPage page = document.AddPage();
                page.Size = PdfSharp.PageSize.A5;
                // Get an XGraphics object for drawing
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont font = new XFont("Verdana", 15, XFontStyle.BoldItalic);
                XFont font1 = new XFont("Verdana", 10, XFontStyle.Regular);
                XFont font2 = new XFont("Verdana", 15, XFontStyle.Regular);
                XFont font3 = new XFont("Verdana", 8, XFontStyle.Regular);
                XFont font4 = new XFont("Verdana", 8, XFontStyle.Italic);
                // kod kreskowy
                Image im = bc.Draw(m_.nr_zlec_galw.ToString(), 20, 2);
                XImage xim = XImage.FromGdiPlusImage(im);
                gfx.DrawImage(xim, new Point(20, 10));
                im = bc.Draw(m_.kod_zlecenia, 20, 2);





                 gfx.DrawString("Utw.: " + DateTime.Now.ToString(), font1, XBrushes.Black,
                 new XRect(210, 10, 300, 22),
                 XStringFormats.TopLeft);
                 gfx.DrawString("Przez: " + m_.userid, font3, XBrushes.Black,
                  new XRect(210, 22, 300, 22),
                 XStringFormats.TopLeft);







                gfx.DrawString(m_.nazwa, font, XBrushes.Black,
                   new XRect(20, 40, 400, 22),
                  XStringFormats.TopLeft);


                gfx.DrawString(m_.kod_zlecenia + " (Kod zlecenia)", font1, XBrushes.Black,
                   new XRect(20, 65, 300, 22),
                  XStringFormats.TopLeft);
                gfx.DrawString(m_.kod_materialu_galw + " (Kod materialu)", font1, XBrushes.Black,
                 new XRect(20, 80, 300, 22),
                XStringFormats.TopLeft);
                gfx.DrawString(m_.kolor, font2, XBrushes.Black,
               new XRect(20, 95, 300, 22),
              XStringFormats.TopLeft);







                gfx.DrawString("Ilość szt: " + m_.ilosc_szt[i].ToString(), font1, XBrushes.Black,
                new XRect(255, 65, 300, 22),
               XStringFormats.TopLeft);

                gfx.DrawString("Nr rysunku: " + m_.nr_rysunku, font1, XBrushes.Black,
                 new XRect(255, 80, 300, 22),
                XStringFormats.TopLeft);
                gfx.DrawString("Zlec_IPO: " + m_.nr_zlec_galw, font1, XBrushes.Black,
            new XRect(255, 95, 300, 22),
           XStringFormats.TopLeft);


                baza_metekDataContext db1 = new baza_metekDataContext();
                var pow = from c in db1.GAL_POWIERZCHNIEs
                          where c.NR_RYS == m_.nr_rysunku && (bool)c.STATUS
                          select c;
                if (pow.Count() == 1)
                {
                    var pows = pow.Single();

                    gfx.DrawString("Pow: " + Math.Round((double)pows.POW, 3).ToString() + "(" + Math.Round(((double)pows.POW * m_.ilosc_szt[i]), 3).ToString() + ") dm2", font1, XBrushes.Black,
             new XRect(255, 110, 300, 22),
            XStringFormats.TopLeft);

                }

                gfx.DrawString("" + m_.komentarz, font4, XBrushes.Black,
                 new XRect(200, 125, 300, 22),
                XStringFormats.TopLeft);

                ilosc = ilosc + m_.ilosc_szt[i];








                MemoryStream ms = new MemoryStream();
                m_.rysunek.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                Image n_rys = System.Drawing.Image.FromStream(ms);
                double r_width = 150;
                double ratio = r_width / n_rys.Width;
                double r_height = (double)n_rys.Height * ratio;
                XImage rys = XImage.FromGdiPlusImage(n_rys);

                gfx.DrawImage(rys, 15, 150, r_width, r_height);
                Image im1 = bc.Draw("" + m_.kod_zlecenia, 10, 2);
                XImage xim1 = XImage.FromGdiPlusImage(im1);
                gfx.DrawImage(xim1, new Point(15, 130));

                Image im2 = bc.Draw("" + m_.kod_materialu_galw, 10, 2);
                XImage xim2 = XImage.FromGdiPlusImage(im2);
                gfx.DrawImage(xim2, new Point(215, 300));


                gfx.DrawString("  Belka   ,  il.szt", font4, XBrushes.Black,
                 new XRect(255, 140, 300, 22),
                XStringFormats.TopLeft);


                XPen pen = new XPen(XColors.Black, 1);
                XPoint[] points = new XPoint[] { new XPoint(255, 150), new XPoint(355, 150), new XPoint(355, 190), new XPoint(255, 190), new XPoint(255, 150) };
                gfx.DrawLines(pen, points);

                points = new XPoint[] { new XPoint(355, 190), new XPoint(355, 230), new XPoint(255, 230), new XPoint(255, 190) };
                gfx.DrawLines(pen, points);
                points = new XPoint[] { new XPoint(355, 230), new XPoint(355, 270), new XPoint(255, 270), new XPoint(255, 230) };
                gfx.DrawLines(pen, points);

            }







            // Save the document...
            //string filename = "HelloWorld.pdf";
            MemoryStream str = new MemoryStream();
            document.Save(str, true);
            Metki_PDF m_pdf = new Metki_PDF();
            m_pdf.Nr_zlecenia = m_.nr_zlec_galw.ToString();
            m_pdf.Data_utw = DateTime.Now;
            m_pdf.PDF = str.ToArray();
            m_pdf.Ilosc = (int)ilosc;
            baza_metekDataContext db = new baza_metekDataContext();
            db.Metki_PDFs.InsertOnSubmit(m_pdf);
            db.SubmitChanges();

            Metki_baza m = new Metki_baza();
            m.Data_utw = DateTime.Now;
            m.Ilosc = (int)m_.ilosc_szt[0];
            m.Nr_kodu = m_.kod_zlecenia;
            m.Nr_zlecenia = m_.nr_zlec_galw.ToString();
            m.User_id = "galwanika";
            db.Metki_bazas.InsertOnSubmit(m);
            db.SubmitChanges();

            return str.ToArray();


        }



        public static byte[] GenPDFFile(metka m_)
        {
            PdfDocument document = new PdfDocument();
            Zen.Barcode.Code128BarcodeDraw bc = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;
            Zen.Barcode.Code128BarcodeDraw bc1 = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;
            double ilosc = 0;
            for (int i = 0; i < m_.il_stron; i++)
            {
                PdfPage page = document.AddPage();
                page.Size = PdfSharp.PageSize.A5;
                // Get an XGraphics object for drawing
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont font = new XFont("Verdana", 15, XFontStyle.BoldItalic);
                XFont font1 = new XFont("Verdana", 10, XFontStyle.Regular);
                XFont font2 = new XFont("Verdana", 15, XFontStyle.Regular);
                XFont font3 = new XFont("Verdana", 8, XFontStyle.Regular);
                XFont font4 = new XFont("Verdana", 8, XFontStyle.Italic);
                // kod kreskowy
                Image im = bc.Draw(m_.nr_zlec_galw.ToString(), 20, 2);
                XImage xim = XImage.FromGdiPlusImage(im);
                gfx.DrawImage(xim, new Point(20, 10));
                gfx.DrawString("Utw.: " + DateTime.Now.ToString(), font1, XBrushes.Black,
                new XRect(210, 10, 300, 22),
               XStringFormats.TopLeft);
                gfx.DrawString("Przez: " + m_.userid, font3, XBrushes.Black,
                new XRect(210, 22, 300, 22),
               XStringFormats.TopLeft);





                gfx.DrawString(m_.nazwa, font, XBrushes.Black,
                   new XRect(20, 40, 400, 22),
                  XStringFormats.TopLeft);


                gfx.DrawString(m_.kod_zlecenia + " (Kod zlecenia)", font1, XBrushes.Black,
                   new XRect(20, 65, 300, 22),
                  XStringFormats.TopLeft);
                gfx.DrawString(m_.kod_materialu_galw + " (Kod materialu)", font1, XBrushes.Black,
                 new XRect(20, 80, 300, 22),
                XStringFormats.TopLeft);
                gfx.DrawString(m_.kolor, font2, XBrushes.Black,
               new XRect(20, 95, 300, 22),
              XStringFormats.TopLeft);







                gfx.DrawString("Ilość szt: " + m_.ilosc_szt[i].ToString(), font1, XBrushes.Black,
                new XRect(255, 65, 300, 22),
               XStringFormats.TopLeft);

                gfx.DrawString("Nr rysunku: " + m_.nr_rysunku, font1, XBrushes.Black,
                 new XRect(255, 80, 300, 22),
                XStringFormats.TopLeft);
                gfx.DrawString("Zlec_IPO: " + m_.nr_zlec_galw, font1, XBrushes.Black,
            new XRect(255, 95, 300, 22),
           XStringFormats.TopLeft);


                baza_metekDataContext db1 = new baza_metekDataContext();
                var pow = from c in db1.GAL_POWIERZCHNIEs
                          where c.NR_RYS == m_.nr_rysunku && (bool)c.STATUS
                          select c;
                if (pow.Count() == 1)
                {
                    var pows = pow.Single();

                    gfx.DrawString("Pow: " + Math.Round((double)pows.POW, 3).ToString() + "(" + Math.Round(((double)pows.POW * m_.ilosc_szt[i]), 3).ToString() + ") dm2", font1, XBrushes.Black,
             new XRect(255, 110, 300, 22),
            XStringFormats.TopLeft);

                }

                gfx.DrawString("" + m_.komentarz, font4, XBrushes.Black,
                 new XRect(200, 125, 300, 22),
                XStringFormats.TopLeft);

                ilosc = ilosc + m_.ilosc_szt[i];








                MemoryStream ms = new MemoryStream();
                m_.rysunek.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                Image n_rys = System.Drawing.Image.FromStream(ms);
                double r_width = 150;
                double ratio = r_width / n_rys.Width;
                double r_height = (double)n_rys.Height * ratio;
                XImage rys = XImage.FromGdiPlusImage(n_rys);

                gfx.DrawImage(rys, 15, 150, r_width, r_height);
                Image im1 = bc.Draw("" + m_.nr_zlec_galw, 10, 2);
                XImage xim1 = XImage.FromGdiPlusImage(im1);
                gfx.DrawImage(xim1, new Point(15, 130));

                gfx.DrawString("  Belka   ,  il.szt", font4, XBrushes.Black,
                 new XRect(255, 140, 300, 22),
                XStringFormats.TopLeft);


                XPen pen = new XPen(XColors.Black, 1);
                XPoint[] points = new XPoint[] { new XPoint(255, 150), new XPoint(355, 150), new XPoint(355, 190), new XPoint(255, 190), new XPoint(255, 150) };
                gfx.DrawLines(pen, points);

                points = new XPoint[] { new XPoint(355, 190), new XPoint(355, 230), new XPoint(255, 230), new XPoint(255, 190) };
                gfx.DrawLines(pen, points);
                points = new XPoint[] { new XPoint(355, 230), new XPoint(355, 270), new XPoint(255, 270), new XPoint(255, 230) };
                gfx.DrawLines(pen, points);

            }







            // Save the document...
            //string filename = "HelloWorld.pdf";
            MemoryStream str = new MemoryStream();
            document.Save(str, true);
            Metki_PDF m_pdf = new Metki_PDF();
            m_pdf.Nr_zlecenia = m_.nr_zlec_galw.ToString();
            m_pdf.Data_utw = DateTime.Now;
            m_pdf.PDF = str.ToArray();
            m_pdf.Ilosc = (int)ilosc;
            baza_metekDataContext db = new baza_metekDataContext();
            db.Metki_PDFs.InsertOnSubmit(m_pdf);
            db.SubmitChanges();

            Metki_baza m = new Metki_baza();
            m.Data_utw = DateTime.Now;
            m.Ilosc = (int)m_.ilosc_szt[0];
            m.Nr_kodu = m_.kod_zlecenia;
            m.Nr_zlecenia = m_.nr_zlec_galw.ToString();
            m.User_id = "galwanika";
            db.Metki_bazas.InsertOnSubmit(m);
            db.SubmitChanges();

            return str.ToArray();

        }



        public static byte[] GenPDFFile_A4_new(metka m_)
        {
            PdfDocument document = new PdfDocument();
            Zen.Barcode.Code128BarcodeDraw bc = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;
            Zen.Barcode.Code128BarcodeDraw bc1 = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;
            DB2DataContext db2 = new DB2DataContext();

            bool tylko_poler_wyk = false;// m_.komentarz.Contains("!!");


            int ilosc = 0;
            for (int i = 0; i < m_.il_stron; i++)
            {
                PdfPage page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                // Get an XGraphics object for drawing
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont font = new XFont("Verdana", 15, XFontStyle.Bold);
                XFont font1 = new XFont("Verdana", 10, XFontStyle.Regular);
                XFont font2 = new XFont("Verdana", 8, XFontStyle.Regular);
                XFont font3 = new XFont("Verdana", 8, XFontStyle.Italic);

                // kod kreskowy
                Image im = bc.Draw(m_.nr_zlec_szlif + "_" + (i + 1).ToString(), 35, 2);
                XImage xim = XImage.FromGdiPlusImage(im);
                gfx.DrawImage(xim, new Point(10, 10));
                gfx.DrawString("Utw.: " + DateTime.Now.ToString(), font1, XBrushes.Black,
                new XRect(210, 10, 300, 22),
               XStringFormats.TopLeft);



                gfx.DrawString(m_.nazwa, font, XBrushes.Black,
                   new XRect(5, 40, 400, 22),
                  XStringFormats.Center);



                gfx.DrawString("Kod części: " + m_.kod_zlecenia + " (partia nr " + (i + 1).ToString() + " z " + m_.il_stron.ToString() + ")", font1, XBrushes.Black,
                   new XRect(5, 65, 300, 22),
                  XStringFormats.TopLeft);
                gfx.DrawString("Ilość szt: " + m_.ilosc_szt[i].ToString(), font1, XBrushes.Black,
                new XRect(240, 65, 300, 22),
               XStringFormats.TopLeft);
                ilosc = (int)m_.ilosc_szt[0];
                gfx.DrawString("Nr rysunku: " + m_.nr_rysunku, font1, XBrushes.Black,
                  new XRect(5, 80, 300, 22),
                 XStringFormats.TopLeft);





                //gfx.DrawString("Kod po wykonczeniu: " + m_.kod_po_wykonczeniu, font1, XBrushes.Black,
                //  new XRect(5, 95, 300, 22),
                // XStringFormats.TopLeft);
                gfx.DrawString("Kolor: " + m_.kolor, font1, XBrushes.Black,
               new XRect(240, 75, 300, 22),
              XStringFormats.TopLeft);
                gfx.DrawString("Nr_zlec: " + m_.nr_zlec_szlif, font1, XBrushes.Black,
               new XRect(240, 95, 300, 22),
              XStringFormats.TopLeft);

                var rw = from c in db2.IPO_ZDAWKA_PW
                         where c.RW_PW == "RW" && c.Nr_zlecenia_IPO == m_.nr_zlec_szlif
                         group c by new { c.Magazyn_IPO, c.Nr_indeksu } into fgr

                         select new { Magazyn_IPO = fgr.Key.Magazyn_IPO, Nr_indeksu = fgr.Key.Nr_indeksu, Ilosc = fgr.Sum(g => g.Ilosc) };
                int poz = 105;
                foreach (var d in rw.Where(c => c.Ilosc != 0))
                {

                    gfx.DrawString(d.Magazyn_IPO + " " + d.Nr_indeksu + " :" + d.Ilosc.ToString(), font1, XBrushes.Black,
              new XRect(240, poz, 300, 22),
             XStringFormats.TopLeft);



                    poz = poz + 10;
                }



                double st = 170;
                szlif_operacjeDataContext db1 = new szlif_operacjeDataContext();
                var oper = from c in db1.Marszruty_szlifiernia_s
                           where c.Id_wyrobu == m_.kod_zlecenia
                           orderby c.Nr_kol_operacji ascending
                           select new { c.Id_operacji, OPERACJA = c.Id_operacji + " " + c.Nazwa_operacji, c.IloscSztZm, c.NormaZatwierdzona, c.Nazwa_operacji, c.Nr_kol_operacji };

                if (tylko_poler_wyk)
                {
                    oper = oper.Where(x => x.Nazwa_operacji == "Polerowanie wykańczające");
                }
                else
                {
                    oper = oper.Where(x => x.Nazwa_operacji != "Polerowanie wykańczające");
                }

                foreach (var o in oper)
                {
                    string wst = "";

                    if (o.NormaZatwierdzona.Contains("*")) wst = "*";
                    if (!o.Nr_kol_operacji.Value.ToString().EndsWith("0")) wst = "A" + wst;  //operacja nie kończy się na zero - to alternatywa!!!

                    //jeżeli wst zawiera A to drukuj pochyłą czcionką
                    if (wst.Contains("A"))
                    {
                        gfx.DrawString(wst + o.OPERACJA, font3, XBrushes.Black,
                   new XRect(240, st, 300, 22),
                  XStringFormats.TopLeft);
                    }
                    //lub normalną jeżeli nie alternatywna
                    else
                    {

                        gfx.DrawString(wst + o.OPERACJA, font2, XBrushes.Black,
                   new XRect(240, st, 300, 22),
                  XStringFormats.TopLeft);
                    }


                    im = bc1.Draw("OPER_" + o.Id_operacji.ToString(), 15, 2);
                    xim = XImage.FromGdiPlusImage(im);
                    gfx.DrawImage(xim, new Point(230, (int)(st + 15)));
                    gfx.DrawString(((decimal)((decimal)m_.ilosc_szt[i] * 480m) / (decimal)o.IloscSztZm).ToString("####.#") + "/" + o.IloscSztZm.ToString(), font1, XBrushes.Black,
               new XRect(240, st + 26, 300, 22),
              XStringFormats.TopLeft);
                    st = st + 55;
                }


                MemoryStream ms = new MemoryStream();
                m_.rysunek.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                Image n_rys = System.Drawing.Image.FromStream(ms);
                double r_width = 150;
                double ratio = r_width / n_rys.Width;
                double r_height = (double)n_rys.Height * ratio;
                XImage rys = XImage.FromGdiPlusImage(n_rys);

                gfx.DrawImage(rys, 10, 130, r_width, r_height);
                Image im1 = bc.Draw(m_.kod_zlecenia, 20, 3);
                XImage xim1 = XImage.FromGdiPlusImage(im1);
                gfx.DrawImage(xim1, new Point(15, 150 + (int)r_height));
            }







            // Save the document...
            //string filename = "HelloWorld.pdf";
            MemoryStream str = new MemoryStream();
            document.Save(str, true);
            Metki_PDF m_pdf = new Metki_PDF();
            m_pdf.Nr_zlecenia = m_.nr_zlec_szlif.ToString();
            m_pdf.Data_utw = DateTime.Now;
            m_pdf.PDF = str.ToArray();
            m_pdf.Ilosc = ilosc;
            baza_metekDataContext db = new baza_metekDataContext();
            db.Metki_PDFs.InsertOnSubmit(m_pdf);
            db.SubmitChanges();


            return str.ToArray();

        }






        public static System.Drawing.Image GetImage(string nr_rys)
        {

            string nazwa_pliku = "pusty";
            System.Drawing.Bitmap b = new System.Drawing.Bitmap(50, 50);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(b);
            g.Clear(System.Drawing.Color.White);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.DrawString(nr_rys, new System.Drawing.Font("Microsoft Sans Serif", 8), System.Drawing.Brushes.Black,4, 4);
            System.Drawing.Image dd = (System.Drawing.Image)b;
            BAZA_RYSDataContext db = new BAZA_RYSDataContext();
            var nazwa_rys = from c in db.Kartoteka_rysunków_konstrukcyjnyches
                            where c.NrRysunku == nr_rys
                            select c.Rysunek;
            if (nazwa_rys.Count() == 1)
            {
                var n = nazwa_rys.Single(); nazwa_pliku = n;
                if (string.IsNullOrEmpty(nazwa_pliku)) nazwa_pliku = "pusty";
            }

            
            const int LOGON32_PROVIDER_DEFAULT = 0;
            //This parameter causes LogonUser to create a primary token.
            const int LOGON32_LOGON_INTERACTIVE = 2;
            SafeTokenHandle safeTokenHandle;
            //Call LogonUser to obtain a handle to an access token.
            bool returnValue = LogonUser("metki", "valvex.in", "metki",
               LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT,
                out safeTokenHandle);

            WindowsIdentity newId = new WindowsIdentity(safeTokenHandle.DangerousGetHandle());
            WindowsImpersonationContext impersonatedUser = newId.Impersonate();


            string metki = @"\\192.168.1.110\bazy_access\Label View - katalogi\PCX\";
            string plik = Path.Combine(metki, nazwa_pliku);
            //string temp_file = Path.GetTempPath() + @"\" + Guid.NewGuid() + ".txt";

            if (File.Exists(plik))
            {
                //File.Copy(plik, temp_file);
                    
                Image imf = Image.FromFile(plik, false);         //     }


               // File.Delete(temp_file);
                return (Image)imf.Clone();
                 

                
            }
             else
            {

                Service1 srv = new Service1();
                //srv.SendAlert("krzysztof.misiewicz@valvex.com,dariusz.niemiec@valvex.com", "Błąd rysunku: " + nr_rys, "Nie znaleziono rysunku: " + plik   );
            }
             
            return dd;
        }


        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,
       int dwLogonType, int dwLogonProvider, out SafeTokenHandle phToken);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public extern static bool CloseHandle(IntPtr handle);

        public sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeTokenHandle()
                : base(true)
            {
            }

            [DllImport("kernel32.dll")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [SuppressUnmanagedCodeSecurity]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CloseHandle(IntPtr handle);

            protected override bool ReleaseHandle()
            {
                return CloseHandle(handle);
            }
        }



    }

}