using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Web;
using System.Web.Services;

namespace WebService_SharePoint
{
    /// <summary>
    /// Opis podsumowujący dla Service2
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // Aby zezwalać na wywoływanie tej usługi sieci Web ze skryptu za pomocą kodu ASP.NET AJAX, usuń znaczniki komentarza z następującego wiersza. 
    // [System.Web.Script.Services.ScriptService]
    public class Service2 : System.Web.Services.WebService
    {

        [WebMethod]
        public byte[] GAL_Pobierz_zdjecia(int id_zawieszki, int nr_zd)
        {

             
            dbKartyProdukcjiDataContext db2 = new dbKartyProdukcjiDataContext();


            var zdj = (from d in db2.GAL_ZDJECIAs
                       where d.ID_zawieszki == id_zawieszki
                       orderby d.ID
                       select d).Skip(nr_zd).First();

            
                MemoryStream ms = new MemoryStream(zdj.Zdjecie.ToArray());
            //Image returnImage = Image.FromStream(ms);








            return ms.ToArray();
        }

        [WebMethod]
        public  List<IPO_ZLECENIA> GAL_dane_zlecenia(int id_zlec)
        {
            DB2DataContext db = new DB2DataContext();
           

            return db.IPO_ZLECENIA.Where(x => x.ipo_nr_zlec == id_zlec).ToList();



        }

        


        [WebMethod]
        public List<GAL_ZAWIESZKI>  GAL_zawieszki(int item_id)
        {

            List<GAL_ZAWIESZKI> list = new List<GAL_ZAWIESZKI>();

            DBDataContext db = new DBDataContext();
            var item = from c in db.SLOWNIK_1s where c.IMITM == item_id select c;
            if (item.Count() == 1)
            {

                dbKartyProdukcjiDataContext db1 = new dbKartyProdukcjiDataContext();

                var dane = from c in db1.GAL_ZAWIESZKIs
                           where c.Nr_rysunku.Trim().ToLower() == item.First().NR_RYS.Trim().ToLower()
                           select c;
                foreach (var d in dane)
                {
                    list.Add(d);

                }


            }

            return list;


        }


        [WebMethod]
        public void Wyczysc_zlecenie_z_telewizora(int nr_zlecenia)
        {
            try
            {
                string test = SendToTCPListener("192.168.7.23", 10005, nr_zlecenia.ToString());
                test = SendToTCPListener("192.168.7.24", 10005, nr_zlecenia.ToString());



                test = SendToTCPListener("192.168.7.25", 10005, nr_zlecenia.ToString());

                DB2DataContext db = new DB2DataContext();
                var zl_do_akt = from c in db.IPO_ZDAWKA_PW where c.Nr_zlecenia_IPO == nr_zlecenia select c;
                foreach (var z in zl_do_akt)
                {
                    z.do_kontroli = -1;
                    db.SubmitChanges();
                }

               
                 


                Service1 srv = new Service1();
                //srv.SendAlert("andrzej.pawlowski@valvex.com", "TELEWIZOR - zlec IPO " + nr_zlecenia.ToString(), "skasowano zlecenie z telewizora");

            }
            catch
            {

            }
            
            
              
        }


        [WebMethod]
        public List<string> GAL_Powody_brakow()
        {
            var db = new dbKartyProdukcjiDataContext();
            var powody = from c in db.GAL_powody_brakow
                         select c.Opis;

            return powody.ToList();
        }


        [WebMethod]
        public double Zaloz_transferM4(string mag_z, string lok_z, string litm, string mag_do, int qty,string JM, string utworzyl, string komentarz)
        {
            //pobierz trukid
            DBDataContext db = new DBDataContext();

            var itm = (from c in db.F4101s
                       where c.IMLITM.Trim() == litm.Trim()
                       select new { c.IMITM, c.IMLITM, c.IMAITM }).FirstOrDefault();



            double currentid = 0;
            var trukid = (from c in db.F00022
                          where c.UKOBNM.Trim() == "F5V00010"
                          select c).FirstOrDefault();

            currentid = (double)trukid.UKUKID++;
            db.SubmitChanges();
            F5V00010 new_M4 = new F5V00010();
            new_M4.TRUKID = currentid;
            new_M4.TRLNID = 1000;
            new_M4.TROGNO = 0;
            new_M4.TR_5XMTRTY = '1';
            new_M4.TR_5XMTRST = '1';
            new_M4.TRPRIO = '0';
            new_M4.TRCO = "00001";
            new_M4.TRMCUF = mag_z;
            new_M4.TRFLOC = lok_z;
            new_M4.TRMCU = "";
            new_M4.TRLOCN = "";
            new_M4.TRTOMCU = mag_do;
            new_M4.TRTLOC = "";
            new_M4.TRDOCO = 0;
            new_M4.TRDCTO = "SC";
            new_M4.TRKIT = 0;
            new_M4.TRKITL = "";
            new_M4.TRKITA = "";
            new_M4.TRUORG = 0;
            new_M4.TRUOM = "";
            new_M4.TRITM = itm.IMITM;
            new_M4.TRLITM = itm.IMLITM;
            new_M4.TRAITM = itm.IMAITM;
            new_M4.TRLOTN = "";
            new_M4.TRTRQT = qty * 10000;
            new_M4.TRUM = JM;
            new_M4.TRDCT = "M4";
            new_M4.TRTRDJ = this.Date2JT(DateTime.Now);
            new_M4.TRDRQJ = this.Date2JT(DateTime.Now);
            new_M4.TRADDJ = 0;
            new_M4.TRDSC1 = komentarz;
            //new_M4.TR_5XNOAV = '';
            new_M4.TRTORG = utworzyl;
            new_M4.TRURCD = "";
            new_M4.TRURDT = 0;
            new_M4.TRURAT = 0;
            new_M4.TRURAB = 0;
            new_M4.TRURRF = "";
            new_M4.TRUSER = utworzyl;
            new_M4.TRPID = "VTRANS";
            new_M4.TRJOBN = "Skaner";
            new_M4.TRUPMJ = this.Date2JT(DateTime.Now);
            new_M4.TRUPMT = this.Time2JT(DateTime.Now);
            db.F5V00010.InsertOnSubmit(new_M4);
            db.SubmitChanges();


            return currentid;
        }   
        [WebMethod]
        public int Date2JT(DateTime dt)
        {
            int dti = 100000 + ((dt.Year - 2000) * 1000) + dt.DayOfYear;

            return dti;
        }
        [WebMethod]
        public int Time2JT(DateTime dt)
        {
            int dti = (dt.Hour * 10000) + (dt.Minute * 100) + (dt.Second);

            return dti;

        }


        [WebMethod]
        public void Wyczysc_zlecenia_na_telewizorze()
        {
            DB2DataContext db = new DB2DataContext();

            var lista = (from c in db.IPO_ZDAWKA_PW
                         where c.HALA_PROD == "[GA]" & c.RW_PW == "RW" && c.do_kontroli != -1
                         select c.Nr_zlecenia_IPO).Distinct().ToList();

            foreach (var zl in lista)
            {
                Wyczysc_zlecenie_z_telewizora(zl.Value);
            }
        }


        [WebMethod]
        public string SendToTCPListener(string Server, int port,string message)
        {

            
            TcpClient client = new TcpClient(Server, port);
            byte[] data = System.Text.Encoding.Unicode.GetBytes(message);
            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);

            


            data = new Byte[256];

             
            string responseData = string.Empty;

             
            Int32 bytes = stream.Read(data, 0, data.Length);
            responseData = System.Text.Encoding.Unicode.GetString(data, 0, bytes);
            

            client.Close();
            stream.Close();

            return responseData;
        }




        [WebMethod]
        public string Aktualizuj_dni()
        {
          
            string siteUrl = "http://SP2013/jakosc";

            ClientContext clientContext = new ClientContext(siteUrl);


            CredentialCache cc = new CredentialCache();
            cc.Add(new Uri(siteUrl), "NTLM", new NetworkCredential("apawlowski", "cbv3.560671bf", "valvex"));
            clientContext.Credentials = cc;
            clientContext.AuthenticationMode = ClientAuthenticationMode.Default;


            //{CA5FAC64-ADA7-4BB4-8837-66F28A93A4ED} - Ukraina
            //{58BEEDDE-F116-49E0-AA33-8D3A51E33A05} - Chorwacja
            //{58BEEDDE-F116-49E0-AA33-8D3A51E33A05} - INIG
            //{58BEEDDE-F116-49E0-AA33-8D3A51E33A05} KOT
            //{48E0857F-DB38-48A3-838F-AE07EB68D977} - Znaki towarowe
            //{48E0857F-DB38-48A3-838F-AE07EB68D977} Atesty higieniczne



            Microsoft.SharePoint.Client.List oList = clientContext.Web.Lists.GetById(new Guid("CDA86D2C-3C3C-4EC8-848D-E2AF67AC0906"));

            Microsoft.SharePoint.Client.ListItemCollection col = oList.GetItems(CamlQuery.CreateAllItemsQuery());
            clientContext.Load(col);

            clientContext.ExecuteQuery();
             

            foreach (Microsoft.SharePoint.Client.ListItem l in col)
            {

                DateTime term_op = (DateTime)(l["Termin_op_x0142_aty"] ?? DateTime.Now)  ;
                l["Liczba_dni_do_zakonczenia_waznos"] = (term_op - DateTime.Now).TotalDays;
                l.Update();
                
                clientContext.ExecuteQuery();
            }

            siteUrl = "http://wew.valvex.com/jakosc/certyfikacja/";

            clientContext = new ClientContext(siteUrl);


            cc = new CredentialCache();
            cc.Add(new Uri(siteUrl), "NTLM", new NetworkCredential("apawlowski", "cbv3.560671bf", "valvex"));
            clientContext.Credentials = cc;
            clientContext.AuthenticationMode = ClientAuthenticationMode.Default;

            // UKRAINA
            oList = clientContext.Web.Lists.GetById(new Guid("CA5FAC64-ADA7-4BB4-8837-66F28A93A4ED"));

              col = oList.GetItems(CamlQuery.CreateAllItemsQuery());
              clientContext.Load(col);

              clientContext.ExecuteQuery();


            foreach (Microsoft.SharePoint.Client.ListItem l in col)
            {
            
                DateTime term_op = (DateTime)(l["Data_x0020_wa_x017c_no_x015b_ci"] ?? DateTime.Now);
                double il_dni_do_konca = (term_op - DateTime.Now).TotalDays;
                if (il_dni_do_konca < 180 && (string)l["_x006f_tj5"] != "TAK")
                {         
                    l["_x006f_tj5"] = "TAK";
                    l.Update();
                    Service1 srv = new Service1();
                    srv.SendAlert("alert_certyfikaty@valvex.com", "UKRAINA Certyfikaty - termin poniżej 180", $"Sprawdź listę certyfikatów - nr dok: {(string)l["Title"]}");
                }
              

                clientContext.ExecuteQuery();
            }








            return "";
        }
    }
}
