using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace WebService_SharePoint
{
    /// <summary>
    /// Opis podsumowujący dla Szlif
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // Aby zezwalać na wywoływanie tej usługi sieci Web ze skryptu za pomocą kodu ASP.NET AJAX, usuń znaczniki komentarza z następującego wiersza. 
    // [System.Web.Script.Services.ScriptService]
    public class Szlif : System.Web.Services.WebService
    {

        [WebMethod]
        public decimal Get_My_Productivity(int _user_id)
        {
            decimal wyk_proc = 0;
            baza_metekDataContext db = new baza_metekDataContext();
            try
            {

                var min_norma = (from c in db.Wykonanie_szlifiernia_normas

                                 where c.ID_Usera == _user_id.ToString() &&
                        c.DATA_.Value.Date == DateTime.Now.Date
                                 group c by new { c.ID_Usera } into cgroup
                                 select new
                                 {
                                     minuty = cgroup.Sum(g => g.Minuty_wg_normy)
                                 }).Single();

                var min_wyk = (from c in db.Wykonanie_szlifiernia_normas

                               where c.ID_Usera == _user_id.ToString() &&
                      c.DATA_.Value.Date == DateTime.Now.Date
                               group c by new { c.ID_Usera } into cgroup
                               select new
                               {
                                   minuty = cgroup.Sum(g => g.czas_operacji_min)
                               }).Single();
                

                if (min_wyk.minuty != 0)
                    wyk_proc = ((decimal)min_norma.minuty / (decimal)min_wyk.minuty) * 100;




                
            }
            catch { }
            return wyk_proc;
        }



        [WebMethod]
        public string Insert_Document(int Id_pracownika, string nr_zlecenia, string nr_zlec_nr_partii, string kod_detalu, string Id_operacji, string Nazwa_operacji, int ilosc_ok, int ilosc_izolator)
        {
            baza_metekDataContext db = new baza_metekDataContext();
            Szlifiernia_operacje_skan skan = new Szlifiernia_operacje_skan();
            skan.Czas_stop = DateTime.Now;
            skan.Do_poprawy = "NIE";
            skan.ID_Operacji = Id_operacji;
            skan.ID_Usera = Id_pracownika.ToString();
            skan.Ilosc_izolator = ilosc_izolator;
            skan.Ilosc_ok = ilosc_ok;
            skan.Kod_detalu = kod_detalu;
            skan.Nazwa_operacji = Nazwa_operacji;
            skan.Nr_zlecenia = nr_zlecenia;
            skan.Nr_zlecenia_nr_partii = nr_zlec_nr_partii;
            db.Szlifiernia_operacje_skan.InsertOnSubmit(skan);

            db.SubmitChanges();





            return "OK";
        }


        [WebMethod]
        public string GetUserName(int id)
        {
            DBSZlifDataContext db = new DBSZlifDataContext();
            string osoba = "ERROR"; //domyślnie błąd

            var naz = from c in db.Pracownicy
                      where c.Id_Pracownika == id
                      select c.Nazwisko_prac;

            if (naz.Count() == 1) osoba = naz.First();


            return osoba;
        }


        [WebMethod]
        public string Get_order_name(string ord_no)
        {
            string nazwa = "BŁĄD!!!";
            baza_metekDataContext db = new baza_metekDataContext();
            var zlec = (from b in db.Metki_bazas
                       where b.Nr_zlecenia == ord_no
                       select b).Take(1);

            DBDataContext db1 = new DBDataContext();

            if (zlec.Count() == 1)
            {
                string kod = zlec.First().Nr_kodu.Trim();
                string nzw = db1.SLOWNIK_1s.Where(x => x.IMLITM.Trim() == zlec.First().Nr_kodu.Trim()).First().NAZWA;

                nazwa = kod + " - " + nzw;
            }
            return nazwa;
        }

        [WebMethod]
        public string[] Get_operations(string ord_no)
        {
            List<string> operacje = new List<string>();
            baza_metekDataContext db = new baza_metekDataContext();

            try
            {
                var zlec = (from b in db.Metki_bazas
                            where b.Nr_zlecenia == ord_no
                            select b).Take(1);

                DBDataContext db1 = new DBDataContext();

                if (zlec.Count() == 1)
                {
                    string nr_kodu = zlec.First().Nr_kodu;
                    var oper = from c in db.Marszruty_szlif
                               where c.Id_wyrobu == nr_kodu
                               orderby c.Id_operacji ascending
                               select new { OPER = c.Id_operacji.ToString(), NAZWA = c.Nazwa_operacji, ZATW = c.NormaZatwierdzona };
                    foreach (var o in oper)
                    {

                        operacje.Add(o.OPER + "_" + o.NAZWA + o.ZATW);

                    }



                }

            }
            catch { }
            return operacje.ToArray();
        }



    }

    

}
