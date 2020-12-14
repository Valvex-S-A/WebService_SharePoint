using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Services;
using System.Reflection;
using System.Runtime.Serialization;
using NLog;
using System.Xml.Serialization;
using System.IO;
using System.Drawing;
using System.Transactions;
using System.Security.Principal;
using SelectPdf;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace WebService_SharePoint
{



    /// <summary>
    /// Opis podsumowujący dla trans
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]


    // Aby zezwalać na wywoływanie tej usługi sieci Web ze skryptu za pomocą kodu ASP.NET AJAX, usuń znaczniki komentarza z następującego wiersza. 
    // [System.Web.Script.Services.ScriptService]
    public class trans : System.Web.Services.WebService
    {



        private static Logger logger = LogManager.GetCurrentClassLogger();


        public enum task_type
        {
            Transfer_prosty = 0,
            Potwierdzenie_transferu = 1,
            Kompletacja_przewodnika = 2


        }
        [WebMethod]
        public void ledger_stop_operation(int id_usera, int ilosc_szt, int ilosc_zd)
        {
            dbtrans1DataContext db = new dbtrans1DataContext();
            var zlec = from c in db.Dziennik_operacjis
                       where (int)c.user_id == id_usera && c.status == "w_toku"
                       select c;

            foreach (var z in zlec)
            {
                z.status = "zakończone";
                z.data_stop = DateTime.Now;
                z.ilosc_sztuk = ilosc_szt;
                z.ilosc_zdarzen = ilosc_zd;

            }
            db.SubmitChanges();
        }


        [WebMethod]
        public void ledger_start_operation(int id_usera, task_type tsk)
        {
            //sprawdz czy ten user juz nie ma  operacji w toku i je zamknij.
            //Jeżeli coś zostało to oznacza że był jakiś błąd - albo user nie wylogował się prawidłowo.
            dbtrans1DataContext db = new dbtrans1DataContext();
            this.ledger_stop_operation(id_usera, 0, 0);

            string task_name = "";
            switch ((int)tsk)
            {
                case 0: task_name = "Transfer_prosty"; break;
                case 1: task_name = "Potwierdzenie_transferu"; break;
                case 2: task_name = "Kompletacja_przewodnika"; break;

                default: break;
            }
            Dziennik_operacji dz = new Dziennik_operacji();

            dz.data_start = DateTime.Now;
            dz.status = "w_toku";
            dz.ilosc_sztuk = 0;
            dz.ilosc_zdarzen = 0;
            dz.komentarz = "";
            dz.typ_operacji = task_name;
            dz.user_id = id_usera;

            db.Dziennik_operacjis.InsertOnSubmit(dz);
            db.SubmitChanges();





        }



        [WebMethod]
        public string Nowy_uzytkownik(string login, string haslo, string imie, string nazwisko, string nr_tel, string email, string komentarz, bool aktywny)
        {
            dbtrans1DataContext db = new dbtrans1DataContext();

            string salt = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                uzytkownicy user = new uzytkownicy();
                user.Aktywny = true;
                user.Data_utworzenia = DateTime.Now;
                user.email = email;
                user.Imie = imie;
                user.Komentarz = komentarz;
                user.Konto_aktywne_do = null;
                user.Konto_aktywne_od = null;
                user.login = login;
                user.Nazwisko = nazwisko;
                user.Ostatnie_logowanie = null;
                user.password = GetSHA1Hash(salt + haslo);
                user.salt = salt;
                user.tel_kom = nr_tel;

                db.uzytkownicies.InsertOnSubmit(user);
                db.SubmitChanges();

                Service1 srv = new Service1();
                if (nr_tel.Length == 9) srv.SendSMS(nr_tel, "Utworzono uzytkownika " + login + ". Twoje haslo to " + haslo);

            }
            catch { return "BLAD!!!"; }



            return "OK";
        }
        [WebMethod]
        public zlecenia_naglowek_view Pobierz_naglowek_zlecenia(int id_zlecenia)
        {
            dbtrans1DataContext tr = new dbtrans1DataContext();

            var linia = from c in tr.zlecenia_naglowek_views
                        where c.Nr_zlecenia == id_zlecenia
                        select c;

            zlecenia_naglowek_view z = new zlecenia_naglowek_view();

            if (linia.Count() == 1) z = linia.First();

            return z;

        }


        [WebMethod]
        public void Potwierdz_caly_transfer(int nr_zlec, string UserName)
        {
            dbtrans1DataContext tr = new dbtrans1DataContext();

            var linie = from c in tr.zlecenia_szczegoly
                        where c.id_zlecenia == nr_zlec && c.status_linii.Trim().ToLower() == "spakowane" && c.Status_ksiegowania != "P" && c.Status_ksiegowania != "K"
                        select c;

            foreach (var linia in linie)
            {

                this.Zaksieguj_Linie_transferu(linia.id.ToString(), UserName);
            }


        }


        [WebMethod]
        public List<PALETY62> PALETY_Pobierz_palete(string id_palety)
        {
            dbtrans1DataContext tr = new dbtrans1DataContext();
            var lista = (from c in tr.PALETY62s
                         where c.ANULOWANY == false && c.Nr_palety == id_palety
                         orderby c.ID descending
                         select c).Take(50);
            return lista.ToList();

        }


        [WebMethod]
        public List<PALETY62> PALETY_Pobierz_liste_palet()
        {
            dbtrans1DataContext tr = new dbtrans1DataContext();
            var lista = (from c in tr.PALETY62s
                         where c.ANULOWANY == false
                         orderby c.ID descending
                         select c).Take(50);
            return lista.ToList();

        }


         



        [WebMethod]
        public void PALETY_Dodaj_palete(string id_palety, string lok, string UserName)
        {
            dbtrans1DataContext db = new dbtrans1DataContext();
            //anuluj pozostałe
            var do_anul = from c in db.PALETY62s
                          where c.Nr_palety == id_palety
                          select c;

            foreach (var a in do_anul)
            {

                a.ANULOWANY = true;
                db.SubmitChanges();
            }



            PALETY62 p = new PALETY62();

            p.ANULOWANY = false;
            p.Data_utw = DateTime.Now;
            p.lok = lok;

            p.Nr_palety = id_palety;
            p.UserName = UserName;
            db.PALETY62s.InsertOnSubmit(p);
            db.SubmitChanges();
            db.Dispose();
        }

        [WebMethod]
        public void PALETY_Dodaj_palete_indeks(string id_palety, string lok, string Indeks, string UserName)
        {
            dbtrans1DataContext db = new dbtrans1DataContext();
            //anuluj pozostałe
            var do_anul = from c in db.PALETY62s
                          where c.Nr_palety == id_palety
                          select c;

            foreach (var a in do_anul)
            {

                a.ANULOWANY = true;
                db.SubmitChanges();
            }



            PALETY62 p = new PALETY62();

            p.ANULOWANY = false;
            p.Data_utw = DateTime.Now;
            p.lok = lok;
            p.Nr_palety = id_palety;
            p.UserName = UserName;
            p.Indeks = Indeks;
            db.PALETY62s.InsertOnSubmit(p);
            db.SubmitChanges();
            db.Dispose();
        }
        [WebMethod]
        public void PALETY_Anuluj_palete(int id, string UserName)
        {
            dbtrans1DataContext db = new dbtrans1DataContext();

            var an = from c in db.PALETY62s
                     where c.ID == id
                     select c;

            if (an.Count() == 1)
            {
                var a = an.First();
                a.ANULOWANY = true;
                a.UserName = UserName;
                a.Data_utw = DateTime.Now;
                db.SubmitChanges();
            }
            db.Dispose();
        }
        [WebMethod]
        public List<IPO_magazyny_IPO2JDE> Pobierz_lokalizacje_KOMPLETACJA_Z(string Userlogin)
        {
            List<IPO_magazyny_IPO2JDE> lista_mag = new List<IPO_magazyny_IPO2JDE>();
            var db2 = new DBDataContext();
            var wszystkie_mag = (from c in db2.IPO_magazyny_IPO2JDE
                                 select c).ToArray();


            var db = new dbtrans1DataContext();
            var uprawnienia = from c in db.Aktualne_grupy_views
                              where c.login == Userlogin && (bool)c.KOMPLETACJA_Z
                              select c.Grupa;



            foreach (var gr in uprawnienia)
            {
                var mag = from c in wszystkie_mag
                          where c.mag_ipo.StartsWith(gr)
                          select c;

                lista_mag.AddRange(mag);



            }
            lista_mag = lista_mag.Distinct().ToList();



            return lista_mag;
        }

        [WebMethod]
        public string ODC_Dodaj_PU_brak(int nr_zlec, int qty)
        {
            Service1 srv = new Service1();

            var zl = srv.IPO_GET_ORDER(nr_zlec);
            //if (zl.quantity - zl.quantity_out - qty < 0) return "BLAD";
            DB2DataContext db2 = new DB2DataContext();
            DBDataContext db = new DBDataContext();
            var item = (from c in db.SLOWNIK_1s
                        where c.IMITM == int.Parse(zl.item_id)
                        select c).FirstOrDefault();


            IPO_ZDAWKA_PW pu = new IPO_ZDAWKA_PW();
            pu.Magazyn_IPO = "PROD_P5GALWNIZERNIA";
            pu.Nr_zlecenia_IPO = nr_zlec;
            pu.Czy_korygowany = false;
            pu.Data_utworzenia_poz = DateTime.Now;
            pu.do_kontroli = 0;
            pu.HALA_PROD = "[GV]";
            pu.Ilosc = qty;
            pu.IPO_ID_POZYCJI = -1;
            pu.ITM = zl.item_id;
            pu.JM = "SZ";
            pu.Kod_zlecenia_klienta = "";
            pu.Koszt_IPO = 0;
            pu.Koszt_mat_IPO = 0;
            pu.Nazwa_pozycji = item.NAZWA;
            pu.Nr_indeksu = item.IMLITM;
            pu.Nr_seryjny = "x";
            pu.Nr_zam_klienta = item.IMLITM;
            pu.Powod_korekty = "DO ODCIAGANIA";
            pu.RW_PW = "PU";
            pu.typ = 1;
            pu.Zaksiegowany_JDE = false;
            db2.IPO_ZDAWKA_PW.InsertOnSubmit(pu);
            db2.SubmitChanges();

            GAL_Utwórz_transfer_na_GAL_ODC(item.IMLITM.Trim(), "PROD", "P -5 GALWNIZERNIA", "SZ", qty, "DO_ODCIĄGANIA_GALW", 2);


            return pu.Nr_indeksu.Trim();
        }

        [WebMethod]
        public string ODC_Odbierz_detal(int id, int ilosc, int user_id, string komentarz)
        {
            var db = new dbtrans1DataContext();

            var odc = (from c in db.Odciaganie_galwanizernia
                       where c.ID == id && c.status != "ZAMKNIĘTE"
                       select c).FirstOrDefault();
            odc.Modyfikowal = user_id;
            odc.Data_modyfikacji = DateTime.Now;
            if (odc.Ilosc_odciagnieta + ilosc < 0) return "BŁĄD";
            if (odc.Ilosc_odciagnieta + ilosc > odc.Ilosc_zawieszona) return "BŁĄD";
            if (odc.Ilosc_odciagnieta + ilosc == odc.Ilosc_zawieszona)
            {
                odc.Ilosc_odciagnieta = odc.Ilosc_zawieszona;
                odc.Data_Stop = DateTime.Now;

                odc.status = "ZAMKNIĘTE";
            }
            else
            {
                odc.Ilosc_odciagnieta += ilosc;


            }
            if (odc.Komentarz == null) odc.Komentarz = "";

            odc.Komentarz += komentarz; db.SubmitChanges();

            return "OK";
        }

        [WebMethod]
        public List<Odciaganie_galwanizernia> ODC_Pobierz_odciagane()
        {
            var db = new dbtrans1DataContext();
            var list = from c in db.Odciaganie_galwanizernia
                       where c.status == "ZAWIESZONE"
                       select c;

            return list.ToList();


        }


        [WebMethod]
        public void ODC_Zawies_detal_do_odciagania(string litm, int ilosc, int user_id)
        {

            try
            {
                var db = new dbtrans1DataContext();
                var db2 = new DBDataContext();
                Odciaganie_galwanizernia odc = new Odciaganie_galwanizernia();

                var item = (from c in db2.SLOWNIK_1s
                            where c.IMLITM.ToLower().Trim() == litm.ToLower().Trim()
                            select c).FirstOrDefault();
                odc.Data_Start = DateTime.Now;
                odc.Data_modyfikacji = DateTime.Now;
                odc.Data_utworzena = DateTime.Now;
                odc.Ilosc_odciagnieta = 0;
                odc.Ilosc_zawieszona = ilosc;
                odc.Indeks = litm;
                odc.Nazwa = item.NAZWA.Trim();
                odc.status = "ZAWIESZONE";
                odc.Utworzyl = user_id;
                odc.Modyfikowal = user_id;
                db.Odciaganie_galwanizernia.InsertOnSubmit(odc);
                db.SubmitChanges();


            }
            catch { }

        }


        [WebMethod]
        public List<zlecenia_szczegoly> Pobierz_linie_do_potwierdzenia(int nr_zlec)
        {
            var tr = new dbtrans1DataContext();
            var linie = from c in tr.zlecenia_szczegoly
                        where c.id_zlecenia == nr_zlec && c.status_linii.Trim().ToLower() == "spakowane" && (c.Status_ksiegowania ?? "x") != "K"
                         && (c.Status_ksiegowania ?? "x") != "P"
                        select c;
            return linie.ToList();
        }

        [WebMethod]
        public int Odrzuc_Linie_transferu(string ID, string user_name)
        {
            Service1 srv1 = new Service1();
            dbtrans1DataContext db = new dbtrans1DataContext();
            var linie = from c in db.zlecenia_szczegoly
                        where c.id == int.Parse(ID)
                        select c;

            if (linie.Count() == 1)
            {
                var linia = linie.First();
                if ((linia.Status_ksiegowania ?? "x") == "P" || (linia.Status_ksiegowania ?? "x") == "K" || (linia.Status_ksiegowania ?? "x") == "Z") return -1;


                linia.status_linii = "odrzucone                     ";
                linia.autor_ost_oper = this.Userid(user_name);
                linia.data_ost_oper = DateTime.Now;

                db.SubmitChanges();
                //sprawdz czy są jeszcze jakieś linie nie zatwierdzone
                Ustaw_status_zlecenia(linia.id_zlecenia, this.WezDaneUsera(user_name).Id_usera);



                var user = WezDaneUseraId(linia.autor_zlecenia);

                Logger(user_name, linia.litm, linia.ilosc_otwarta.ToString() + " " + linia.JM, linia.id_zlecenia, "ODRZUCONO LINIE TRANSFERU", linia.magazyn_z.Trim() + "=>" + linia.magazyn_do.Trim(),
                    linia.lokalizacja_z.Trim() + "=>" + linia.lokalizacja_do.Trim(), linia.nr_linii);
                srv1.SendAlert(user.email, "Transfer " + linia.id_zlecenia.ToString(),
                    ". Odrzucono indeks: " + linia.litm + ", ilość: " + linia.ilosc_zamowiona.ToString() + " przez " + user.Nazwisko + " " + user.Imię

                    );



                return 0;


            }



            return -1;
        }
        [WebMethod]
        public int Zaksieguj_Linie_transferu_czesciowo(string iD, string nlok, string user_name, double nqty)
        {
            dbtrans1DataContext db = new dbtrans1DataContext();
            var linie = from c in db.zlecenia_szczegoly
                        where c.id == int.Parse(iD)
                        select c;
            if (linie.Count() == 1)
            {
                var linia = linie.First(); //JEST TYLE SAMO
                if (linia.ilosc_zamowiona == nqty)
                {
                    linia.lokalizacja_do = nlok;
                    db.SubmitChanges();
                    return Zaksieguj_Linie_transferu(iD, user_name);
                }
                if (linia.ilosc_zamowiona > nqty) //JEST MNIEJ
                {
                    linia.lokalizacja_do = nlok;

                    //3402140
                    var nlinia = Clone(linia);

                    nlinia.ilosc_zamowiona = nlinia.ilosc_zamowiona - nqty;
                    nlinia.ilosc_zrealizowana = 0;
                    nlinia.ilosc_otwarta = nlinia.ilosc_zamowiona;

                    nlinia.nr_linii++;
                    db.zlecenia_szczegoly.InsertOnSubmit(nlinia);


                    //linia.lokalizacja_do = nlok;
                    linia.ilosc_zamowiona = nqty;
                    linia.ilosc_zrealizowana = nqty;
                    linia.ilosc_otwarta = 0;
                    db.SubmitChanges();

                    return Zaksieguj_Linie_transferu(iD, user_name); ;
                }
                if (linia.ilosc_zamowiona < nqty) //JEST WIĘCEJ
                {
                    linia.lokalizacja_do = nlok;
                    double? old_ilosc = linia.ilosc_zamowiona;
                    linia.ilosc_zamowiona = nqty;
                    linia.ilosc_otwarta = nqty;
                    db.SubmitChanges();
                    try
                    {
                        var utw = WezDaneUseraId(linia.autor_zlecenia);

                        Service1 s = new Service1();
                        s.SendAlert(utw.email, $"Transfer {linia.id_zlecenia}: Zwiększono ilość dla: {linia.litm.Trim()} ",
                            $"Podczas potwierdzania indeksu {linia.litm.Trim() } - {linia.opis} zwiększono ilość z {old_ilosc.ToString()} do " + nqty +
                            ". Jeżeli nie zgadzasz się z ilością, to skontaktuj się z potwiedzającym. UWAGA! TO MOŻE OZNACZAĆ ŻE POWSTAŁ UJEMNY STAN NA TWOIM MAGAZYNIE!!!");


                    }
                    catch { }

                    return Zaksieguj_Linie_transferu(iD, user_name); ;
                }


            }

            return -1;
        }


        [WebMethod]
        public zlecenia_szczegoly Pobierz_linie_transferu(int id)
        {
            dbtrans1DataContext db = new dbtrans1DataContext();
            return (from c in db.zlecenia_szczegoly
                    where c.id == id
                    select c).FirstOrDefault();


        }


        [WebMethod]
        public string Weryfikuj_timeout_VTRANS_id(int id)
        {
            dbtrans1DataContext db = new dbtrans1DataContext();
            DBDataContext dbj = new DBDataContext();
            dbj.CommandTimeout = 1000000;
            var linie = (from c in db.zlecenia_szczegoly
                         where c.id == id
                         select c);
            int x = linie.Count();
            int z = 0;
            foreach (var linia in linie)
            {
                var liniaCardex = (from c in dbj.IPO_CARDEX
                                   where c.krotki_nr == linia.itm && c.ILTREX == ("Transfer_" + linia.id_zlecenia.ToString() + "_" + linia.Weryfikacja)
                                   select c.ILOSC).ToArray();
                if (liniaCardex.Count() == 0)
                {
                    z++;
                    linia.Opis_statusu = "BRAK W CARDEX!!!";
                    db.SubmitChanges();
                }
                else
                {
                    linia.Opis_statusu = linia.Opis_statusu.Replace("JD Edwards error Timeout Error (16)", "OK");
                    db.SubmitChanges();
                }

            }
            //Transfer_57342_9544ebaf       
            //POTWIERDZONE 2019-11-28 08:42:18 przez zmalecka,status: JD Edwards error Timeout Error (16)


            return $"Total: {x} Bez Cardex:{z}";
        }



        /// <summary>
        /// Metoda do weryfikacji linii z timeoutem - jeżeli brak zapisu w Cardex, to zaksięguj ponownie...
        /// </summary>
        /// <param name="id_linii"></param>
        /// <returns>0 - poprawnie, -1 - to nie timeout, -2 - do ponownego zaksięgowania</returns>
        [WebMethod]
        public string Weryfikuj_timeout_VTRANS(int rok, int mies, int dzien)
        {
            dbtrans1DataContext db = new dbtrans1DataContext();
            DBDataContext dbj = new DBDataContext();
            dbj.CommandTimeout = 1000000;
            var linie = (from c in db.zlecenia_szczegoly
                         where c.Status_ksiegowania == "Z" && c.Opis_statusu.Contains("Timeout") &&
                         c.data_ost_oper.Year == rok &&
                         c.data_ost_oper.Month == mies &&
                         c.data_ost_oper.Day == dzien
                         select c);
            int x = linie.Count();
            int z = 0;
            foreach (var linia in linie)
            {
                var liniaCardex = (from c in dbj.IPO_CARDEX
                                   where c.krotki_nr == linia.itm && c.ILTREX == ("Transfer_" + linia.id_zlecenia.ToString() + "_" + linia.Weryfikacja)
                                   select c.ILOSC).ToArray();
                if (liniaCardex.Count() == 0)
                {
                    z++;
                    linia.Opis_statusu = "BRAK W CARDEX!!!";
                    db.SubmitChanges();
                }
                else
                {
                    linia.Opis_statusu = linia.Opis_statusu.Replace("JD Edwards error Timeout Error (16)", "OK");
                    db.SubmitChanges();
                }

            }
            //Transfer_57342_9544ebaf       
            //POTWIERDZONE 2019-11-28 08:42:18 przez zmalecka,status: JD Edwards error Timeout Error (16)


            return $"Total: {x} Bez Cardex:{z}";
        }



        [WebMethod]
        public int Zaksieguj_Linie_transferu(string iD, string user_name)
        {

            string status_temp;
            string opis_statusu_temp;

            //dbtrans1DataContext db_trans = new dbtrans1DataContext();
            dbtrans1DataContext db = new dbtrans1DataContext();
            //db_trans.Connection.Open();
            //db_trans.Transaction = db_trans.Connection.BeginTransaction();
            System.Threading.Thread.Sleep(1000);
            var linia = (from c in db.zlecenia_szczegoly
                         where c.id == int.Parse(iD)
                         select c).FirstOrDefault();
            status_temp = (linia.Status_ksiegowania ?? "");
            opis_statusu_temp = (linia.Opis_statusu ?? "");


            if ((linia.Status_ksiegowania ?? "") == "K" || (linia.Status_ksiegowania ?? "") == "P" || (linia.Weryfikacja ?? "").Length == 8)
            {
                //  db_trans.Transaction.Commit();
                //  db_trans.Connection.Close();
                return -3;
            };

            var nagl = (from c in db.zlecenia_naglowkis where c.Nr_zlecenia == linia.id_zlecenia select c).First();
            linia.Opis_statusu = "W trakcie księgowania";
            linia.Status_ksiegowania = "K";

            if (nagl.Typ.Trim() == "Transfer" && linia.ilosc_zrealizowana == 0)
            {
                linia.ilosc_zrealizowana = linia.ilosc_zamowiona;
                linia.ilosc_otwarta = 0;
            }
            db.SubmitChanges();
            try
            {
                var db1 = new DBDataContext();
                var lok_do = (from c in db1.IPO_magazyny_IPO2JDE
                              where c.LILOCN.Trim().ToLower() == linia.lokalizacja_do.Trim().ToLower()
                              select c).FirstOrDefault();
                linia.lokalizacja_do = lok_do.LILOCN;
                db.SubmitChanges();

            }
            catch { }


            var upr = this.Pobierz_lokalizacje_POTWIERDZANIE_DO(user_name);
            var check = (from c in upr
                         where c.LIMCU.Trim() == linia.magazyn_do.Trim() &&
                         c.LILOCN.Trim() == linia.lokalizacja_do.Trim()
                         select c).ToList();

            if (check.Count() == 0)
            {

                var _errlinia = (from c in db.zlecenia_szczegoly
                                 where c.id == int.Parse(iD)
                                 select c).FirstOrDefault();
                _errlinia.Status_ksiegowania = status_temp;
                _errlinia.Opis_statusu = "BRAK UPRAWNIEN!!!";
                db.SubmitChanges();
                return -1; //brak uprawnien - zakończ z błędem


            }







            // spróbuj zaktualizować lokalizacje - mogą być wpisane z palca ze złymi znakami...
            try
            {
                var db1 = new DBDataContext();
                var lok_do = (from c in db1.IPO_magazyny_IPO2JDE
                              where c.LILOCN.Trim().ToLower() == linia.lokalizacja_do.Trim().ToLower()
                              select c).FirstOrDefault();
                linia.lokalizacja_do = lok_do.LILOCN;
                db.SubmitChanges();

            }
            catch { }






            try
            {
                if (linia.status_linii.Trim().ToLower() == "spakowane" || linia.status_linii.Trim().ToLower() == "oczekuje" || linia.status_linii.Trim().ToLower() == "błąd api"
                    || linia.status_linii.Trim().ToLower() == "zakończone")
                {
                    linia.autor_ost_oper = this.Userid(user_name);
                    linia.data_ost_oper = DateTime.Now;

                    var nagl1 = (from c in db.zlecenia_naglowkis where c.Nr_zlecenia == linia.id_zlecenia select c).First();

                    nagl1.Data_ost_mod = DateTime.Now;
                    nagl1.Autor_ost_mod = this.Userid(user_name);

                    db.SubmitChanges();
                    //db_trans.Transaction.Commit();

                    //db.Connection.Close();
                    Ustaw_status_zlecenia(linia.id_zlecenia, this.WezDaneUsera(user_name).Id_usera);

                    return 0;
                }
                else
                {
                    linia.Opis_statusu = "proba zatwierdzenia linii ze złym statusem...";
                    linia.Status_ksiegowania = status_temp;

                    db.SubmitChanges();
                    // db.Transaction.Commit();
                    // db.Connection.Close();
                    return -1;
                }
            }
            catch (Exception x)
            {

                linia.Opis_statusu = $"BŁĄD!!! {x.Message}";
                db.SubmitChanges();
                db.Transaction.Commit();
                db.Connection.Close();
            }




            return -1; //brak takiego Id - błąd...
        }



        [WebMethod]
        public void Zaksieguj_jeden_dokument_z_kolejki()
        {



            Service1 srv = new Service1();
            srv.SendAlert("andrzej.pawlowski@valvex.com", $"Błąd id ", $"Ktoś wywołał funkcję ZJDZK() ");

            return;
            int id = 0;
            dbtrans1DataContext db = new dbtrans1DataContext();
            try
            {



                var linia = (from c in db.zlecenia_szczegoly
                             where c.Status_ksiegowania == "K" && (c.Weryfikacja ?? "x") == "x"
                             select c).FirstOrDefault();
                linia.Status_ksiegowania = "P";
                linia.Weryfikacja = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8);
                id = linia.id;

                db.SubmitChanges();




                this.Zaksieguj_linie_JDE(id);




            } catch (Exception e) {


                srv.SendAlert("andrzej.pawlowski@valvex.com", $"Błąd id ", $"Nie udało się zaksięgować {id.ToString()}. Błąd to {e.Message}");


            }

        }

        [WebMethod]
        public void Zaksieguj_linie_JDE(int id)
        {


            dbtrans1DataContext db = new dbtrans1DataContext();
            var linie = from c in db.zlecenia_szczegoly
                        where c.id == id
                        select c;



            if (linie.Count() == 1)
            {



                var linia = linie.FirstOrDefault();

                var user = (from c in db.uzytkownicies
                            where c.id == linia.autor_ost_oper
                            select c.login).FirstOrDefault();




                string test = this.JDE_Zaksięguj_przesuniecie("PD810", "cdaa",
                   linia.litm, linia.JM,
                   linia.magazyn_z, linia.lokalizacja_z,
                   linia.magazyn_do, linia.lokalizacja_do,
                   "MT", (double)linia.ilosc_zrealizowana, "Transfer_" + linia.id_zlecenia.ToString() + "_" + linia.Weryfikacja);

                Logger(user, linia.litm.Trim(), linia.ilosc_zrealizowana + " " + linia.JM, linia.id_zlecenia, "2JDE:" + test, linia.magazyn_z.Trim() + "=>" + linia.magazyn_do.Trim(), linia.lokalizacja_z.Trim() + "=>" + linia.lokalizacja_do.Trim(), linia.nr_linii);
                Ustaw_status_zlecenia(linia.id_zlecenia, this.WezDaneUsera(user).Id_usera);


                linia.Opis_statusu = "POTWIERDZONE " + DateTime.Now.ToString() + " przez " + user + ",status: " + test;

                if (test.Contains("OK") || test.Contains("Timeout"))
                { linia.Status_ksiegowania = "Z"; linia.status_linii = "zakończone                    "; }
                else
                {
                    linia.Status_ksiegowania = "?";
                    linia.status_linii = "Błąd API";
                    linia.Opis_statusu = test;
                }



                db.SubmitChanges();
            }



        }


        public enum Status_zlecenia
        {

            anulowane,
            błąd_API,
            oczekuje,
            odrzucone,
            spakowane,
            test,
            w_trakcie_kompletacji,
            wstrzymane,
            zablokowane,
            zakończone,
            zamknięte,
            kompletny,
            sciana,
            do_zwrotu
        }



        [WebMethod]
        public void Ustaw_status_zlecenia(int id_zlecenia, int user_id)
        {

            //status Nr_statusu
            //anulowane   0
            //błąd API    9
            //oczekuje    1
            //odrzucone NULL
            //spakowane   2
            //test    7
            //w trakcie kompletacji   3
            //wstrzymane  4
            //zablokowane 5
            //zakończone  8
            //zamknięte   6
            //kompletny
            //sciana
            //do_zwrotu

            var db = new dbtrans1DataContext();
            // zamknij zerowe
            var zero = from c in db.zlecenia_szczegoly
                       where c.id_zlecenia == id_zlecenia && c.ilosc_otwarta == 0 && c.ilosc_zamowiona == 0 && c.ilosc_otwarta == 0
                       select c;

            foreach (var z in zero)
            {
                z.status_linii = "zamknięte";
                // Logger(this.WezDaneUseraId(user_id).login, "", "", id_zlecenia, "STATUS:" + z.status_linii,"", "", z.nr_linii);

            }
            db.SubmitChanges();

            var zlec = (from c in db.zlecenia_naglowkis
                        where c.Nr_zlecenia == id_zlecenia
                        select c).First();


            if (zlec.Status == "kompletny" || zlec.Status == "do_zwrotu" || zlec.Status == "sciana") return;




            var statusy =
                            (from c in db.zlecenia_szczegoly
                             where c.id_zlecenia == id_zlecenia
                             select c.status_linii.Trim()).Distinct();


            var nagl = (from c in db.zlecenia_naglowkis where c.Nr_zlecenia == id_zlecenia select c).First();


            nagl.Autor_ost_mod = user_id;

            List<string> zamkniete = new List<string> { "zakończone", "anulowane", "odrzucone", "zamknięte", "Fantom" };

            var test = from c in db.zlecenia_szczegoly
                       where !zamkniete.Contains(c.status_linii.Trim())
                       && c.id_zlecenia == id_zlecenia
                       select c;
            if (test.Count() == 0) //zamknij zlecenie
            {

                zlec.Status = "zamknięte";
                nagl.Status = zlec.Status;

                db.SubmitChanges();

                //Logger(this.WezDaneUseraId(user_id).login, "", "", id_zlecenia, "STATUS:" + zlec.Status,"","",0);

                return;
            }


            List<string> spakowane = new List<string> { "zakończone", "anulowane", "odrzucone", "spakowane", "Fantom", "zamknięte" };

            var test2 = from c in db.zlecenia_szczegoly
                        where !spakowane.Contains(c.status_linii)

                        && c.id_zlecenia == id_zlecenia
                        select c;
            if (test2.Count() == 0) //zamknij zlecenie
            {

                zlec.Status = "spakowane";
                nagl.Status = zlec.Status;
                db.SubmitChanges();

                //Logger(this.WezDaneUseraId(user_id).login, "", "", id_zlecenia, "STATUS:" + zlec.Status, "", "", 0);

                return;
            }
            var zlec1 = (from c in db.zlecenia_naglowkis
                         where c.Nr_zlecenia == id_zlecenia
                         select c).First();
            zlec1.Status = "oczekuje";
            nagl.Status = zlec1.Status;
            //Logger(this.WezDaneUseraId(user_id).login, "", "", id_zlecenia, "STATUS:" + zlec1.Status, "", "", 0);
            db.SubmitChanges();


        }


        [WebMethod]
        public void Weryfikuj_linie_lista(string dt)
        {
            dbtrans1DataContext dbt = new dbtrans1DataContext();
            var linie = dt.Split(',');

            foreach (var l in linie)
            {

                this.Weryfikuj_linie(int.Parse(l));

            }


        }

        [WebMethod]
        public void Weryfikuj_linie_data(DateTime dt)
        {
            dbtrans1DataContext dbt = new dbtrans1DataContext();
            var linie = (from c in dbt.zlecenia_szczegoly
                         where c.data_ost_oper.Date == dt.Date && c.Weryfikacja != null && !((new List<string> { "fantom", "anulowany", "anulowane", "odrzucone", "oczekuje" }).Contains(c.status_linii.Trim().ToLower()))
                         select c).ToList();

            foreach (var l in linie)
            {

                this.Weryfikuj_linie(l.id);

            }


        }


        [WebMethod]
        public string Weryfikuj_linie(int id_linii)
        {

            dbtrans1DataContext dbt = new dbtrans1DataContext();
            DBDataContext db = new DBDataContext();
            db.CommandTimeout = 1000000;
            var linie = (from c in dbt.zlecenia_szczegoly
                         where c.id == id_linii && !((new List<string> { "fantom", "anulowany", "anulowane", "odrzucone", "oczekuje" }).Contains(c.status_linii.Trim().ToLower()))
                         select c).ToList();
            string result = "";
            foreach (var l in linie)
            {
                var spr = (from c in db.IPO_CARDEX
                           where
                           l.itm == c.krotki_nr && c.ILFRTO == 'F' &&

                           c.ILTREX.Contains(l.Weryfikacja ?? "xxx")
                           select c).ToArray();
                if (spr.Count() != 0)
                {
                    double ilosc_total = -(spr.Sum(x => x.ILOSC)) ?? 0;
                    if (l.ilosc_zrealizowana == ilosc_total) l.Status_ksiegowania = "W0";
                    if (l.ilosc_zrealizowana > ilosc_total) l.Status_ksiegowania = "W>";
                    if (l.ilosc_zrealizowana < ilosc_total) l.Status_ksiegowania = "W<";


                }
                else l.Status_ksiegowania = "W!";


                result = l.Weryfikacja;
                dbt.SubmitChanges();

            }

            return result;
        }

        [WebMethod]
        public DataTable GetCARDEX(string litm)
        {
            DataTable dt;

            DBDataContext db = new DBDataContext();
            var crx = (from c in db.IPO_CARDEX
                      where c.nr_indeksu.Trim().ToLower() == litm.Trim().ToLower()
                      orderby c.DataKG descending
                      select new { data = c.data_transakcji.Value.ToShortDateString()
                      , typ = c.typ_dok, c.ILOSC,
                          c.lok,
                          DOC =c.nr_dok_DOC,  c.mag, c.ILTREX }).Take(50).ToList();

            dt = LINQToDataTable(crx);
            dt.TableName = "CARDEX";
            return dt;



        }

        [WebMethod]
        public void Weryfikuj_transfer(int id_transferu)
        {
            dbtrans1DataContext dbt = new dbtrans1DataContext();
            DBDataContext db = new DBDataContext();
            db.CommandTimeout = 1000000;
            var linie = (from c in dbt.zlecenia_szczegoly
                         where c.id_zlecenia == id_transferu && !((new List<string> { "fantom", "anulowany", "anulowane", "odrzucone", "oczekuje" }).Contains(c.status_linii.Trim().ToLower()))
                         select c).ToList();
            foreach (var l in linie)
            {
                var spr = from c in db.IPO_CARDEX
                          where
                          l.itm == c.krotki_nr &&
                          c.mag.Trim().ToLower() == l.magazyn_z.Trim().ToLower() &&
                          c.lok.Trim().ToLower() == l.lokalizacja_z.Trim().ToLower() &&
                          c.ILTREX.Contains(l.id_zlecenia.ToString())
                          select c;
                if (spr.Count() != 0) l.Weryfikacja = "OK"; else l.Weryfikacja = "!";
                dbt.SubmitChanges();

            }
        }




        public static Image ResizeImage(Image imgToResize, Size size)
        {
            return (Image)(new Bitmap(imgToResize, size));
        }
        /// <summary>
        /// Wynajduje wszystkie transakcje z błędem 2JDE:Item Balance on Lot Hold (0959) i ksieguje je ponownie
        /// </summary>
        /// <param name="_od"></param>
        /// <param name="_do"></param>
        /// <returns></returns>
        [WebMethod]
        public int Zaksięguj_ponownie_linie_kompletacji_wg_id(int id)
        {

            dbtrans1DataContext db = new dbtrans1DataContext();

            var linie = from c in db.Log_historias
                        where c.id == id
                        select c;

            foreach (var l in linie)
            {
                string mag_z = l.mag.Trim().Replace("=>", "%").Split('%')[0];
                string mag_do = l.mag.Trim().Replace("=>", "%").Split('%')[1];
                string lok_z = l.lok.Trim().Replace("=>", "%").Split('%')[0];
                string lok_do = l.lok.Trim().Replace("=>", "%").Split('%')[1];

                string ilosc = l.ilosc.Trim().Replace(" ", "%").Split('%')[0];
                double.TryParse(ilosc, out double _ilosc);

                string JM = l.ilosc.Trim().Replace(" ", "%").Split('%')[1];
                var test = this.JDE_Zaksięguj_przesuniecie("PD810", "cdaa", l.LITM, JM, mag_z, lok_z,
                    mag_do, lok_do, "", _ilosc, "doks: " + l.Nr_kompletacji + ";" + l.nr_linii_kompletacji);
                l.Komentarz = "2JDE:" + test;
                db.SubmitChanges();

            }





            return 0;
        }


        [WebMethod]
        public byte[] GetImage(string indeks)
        {


            MemoryStream stream = new MemoryStream();

            // Save image to stream.
            //    image.Save(stream, ImageFormat.Bmp);


            string nr_rys = "";
            try
            {

                var db = new DBDataContext();
                var item = from c in db.SLOWNIK_1s
                           where c.IMLITM.Trim().ToLower() == indeks.Trim().ToLower()
                           select c;

                nr_rys = item.First().NR_RYS.Trim();
                Image im = GenPDF.GetImage(nr_rys);
                im = ResizeImage(im, new Size(231, 264));
                im.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            catch { }
            return stream.ToArray();

        }

        [WebMethod]
        public byte[] GetImageByDrawingNo(string nr_rys)
        {


            MemoryStream stream = new MemoryStream();

            // Save image to stream.
            //    image.Save(stream, ImageFormat.Bmp);



            try
            {



                Image im = GenPDF.GetImage(nr_rys);
                im = ResizeImage(im, new Size(231, 264));
                im.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            catch { }
            return stream.ToArray();

        }

        [WebMethod]
        public void BlokujLinie(bool status, int nrlinii, int nr_zlec, int UserId)
        {

            //Logger(this.WezDaneUseraId(UserId).login, "", "", nr_zlec, "Blokada linii", "", "", nrlinii);

            var db = new dbtrans1DataContext();
            var linia = from c in db.zlecenia_szczegoly
                        where c.id_zlecenia == nr_zlec && c.nr_linii == nrlinii
                        select c;
            if (status)
            {
                if (linia.Count() == 1)
                {
                    linia.Single().status_linii = "Zablokowane";
                    linia.Single().autor_ost_oper = UserId;
                    linia.Single().data_ost_oper = DateTime.Now;

                    //   db.SubmitChanges();
                }
            }
            else
            {
                if (linia.Count() == 1)
                {
                    linia.Single().status_linii = "Oczekuje";
                    linia.Single().autor_ost_oper = UserId;
                    linia.Single().data_ost_oper = DateTime.Now;

                    // db.SubmitChanges();
                }

            }
        }


        [WebMethod]
        public Zlecenia_szczegoly_SL Pobierz_linie_do_kompletacji_SL(int nr_linii, int nr_zlec)
        {



            var db = new dbtrans1DataContext();

            var linie = (from c in db.Zlecenia_szczegoly_SLs
                         where c.id_zlecenia == nr_zlec && c.ilosc_otwarta != 0 && c.status_linii.Trim() != "Fantom" &&
                         c.nr_linii == nr_linii
                         select c).FirstOrDefault();

            return linie;
        }

        [WebMethod]
        public zlecenia_szczegoly Pobierz_linie_do_kompletacji(int nr_linii, int nr_zlec)
        {
            var db = new dbtrans1DataContext();

            var linie = (from c in db.zlecenia_szczegoly
                         where c.id_zlecenia == nr_zlec && c.ilosc_otwarta != 0 &&
                         c.nr_linii == nr_linii
                         select c).First();

            return linie;
        }

        [WebMethod]
        public static int Logger(string UserName, string ItemId, string qty, int id_zlecenia, string comment, string mag, string lok, int nr_linii)
        {

            var db = new dbtrans1DataContext();
            try
            {
                Log_historia log = new Log_historia();
                log.UserName = UserName;
                log.lok = lok;
                log.mag = mag;
                log.Komentarz = comment;
                log.Nr_kompletacji = id_zlecenia;
                log.nr_linii_kompletacji = nr_linii;
                log.LITM = ItemId;
                log.Data_zdarzenia = DateTime.Now;
                log.ilosc = qty;

                db.Log_historias.InsertOnSubmit(log);
                db.SubmitChanges();
            }
            catch (Exception ex) {

                Service1 s = new Service1();
                s.SendAlert("andrzej.pawlowski@valvex.com", "Bład logowania", ex.Message);


            }






            return 0;
        }


        [WebMethod]
        public string Drukuj_metke_MG_pobranieZG(string unc_drukarki, int nr_zlec_IPO)
        {

            var db = new DB2DataContext();
            var zlec = (from c in db.IPO_ZLECENIA where c.ipo_nr_zlec == nr_zlec_IPO select c).FirstOrDefault();
            int print_lenght = 300;
            int curr_y = 240;

            string Header = $"^XA^LL%DL_METKI%^PA1,1,1,1^FS^FO110,10^CI28^AZN,40,40^F11^FD{zlec.Indeks_zlecenia.Trim()}" +
                $"^FS^FS^FB600,2,0,C,0^FO5,45^CI28^AZN,30,30^F11^FD{zlec.NAZWA.Trim()}^FS" +
                $"^BY4,2,30^FO20,100^BC^FD{zlec.ipo_nr_zlec}^FS^CFA,30^FO5,180^FDLISTA POBRANIA:^FS^FO5,210^GB700,1,3^FS";
            var linie = from c in db.IPO_ZDAWKA_PW where c.Nr_zlecenia_IPO == nr_zlec_IPO && c.RW_PW == "RW" select c;
            StringBuilder strb = new StringBuilder();
            
            
            foreach (var lln in linie) 
            {
                strb.Append($"^CI28^AZN,25,25^FO5,{curr_y}^FD{lln.Nazwa_pozycji.Trim()}^FS");
                curr_y += 30; print_lenght += 30;
                strb.Append($"^A0N,30,35^BCN,30,Y,Y,Y,N^FO5,{curr_y}^BC^FD{lln.Nr_indeksu.Trim()}^FS");
                curr_y += 60; print_lenght += 60;
                strb.Append($"^CI28^AZN,30,30^FO5,{curr_y}^FD{lln.Magazyn_IPO.Trim()} - {lln.Ilosc} {lln.JM.Trim()}^FS");
                curr_y += 30; print_lenght += 30;
                strb.Append($"^FO5,{curr_y}^GB700,1,3^FS");
                curr_y += 10; print_lenght += 10;

            }
            print_lenght += 50;
            Header = Header.Replace("%DL_METKI%", print_lenght.ToString());
            string txt = Header + strb.ToString() + "^XZ";
            string file_name = System.IO.Path.GetTempPath() + @"\\" + Guid.NewGuid().ToString() + @".txt";
            System.IO.File.WriteAllText(file_name, txt);
            System.IO.File.Copy(file_name, unc_drukarki);

            System.IO.File.Delete(file_name);
            return "OK";

        }






        [WebMethod]
        public void Drukuj_metke_szlif_ZEBRA(string unc_drukarki, string litm, int nr_zlec, string[] Lines)
        {

            var db2 = new DB2DataContext();
            double itm = 0;
           
            //sprawdz czy jest taka metka
            



            Service1 srv = new Service1();
            //string[] Lines = new string[1] { "100" }; //do testów
            metka m = new metka();
            API.IPO_Order zlecenie = srv.IPO_GET_ORDER(nr_zlec);
            if (zlecenie.ipo_order_id != 0)
            {


                itm = double.Parse(zlecenie.item_id);

            }
            else { return; }

            szlif_operacjeDataContext db1 = new szlif_operacjeDataContext();
            DBDataContext db2008 = new DBDataContext();


            var idx = (from c in db2008.SLOWNIK_1s
                       where c.IMITM == itm
                       select c).Single();

            string w_logged_user = WindowsIdentity.GetCurrent().Name;

            List<API.IPO_BOM_Line> bom = srv.IPO_BOM_ITEM(idx.IMLITM);
            var dane = db2008.Dane_do_metki(idx.IMLITM).Take(1).Single();

            m.kod_zlecenia = idx.IMLITM;
            m.ilosc_szt = new double[1] { zlecenie.quantity };
            m.il_stron = 1;
            if (Lines.Count() != 0)
            {
                double[] qtys = new double[Lines.Count()];
                int n = 0;
                foreach (string l in Lines)
                {
                    qtys[n] = double.Parse(l);
                    n++;
                }
                m.ilosc_szt = qtys;
                m.il_stron = qtys.Count();
            }

            m.komentarz = zlecenie.order_no_cust + " " + w_logged_user;

            string kod_materialu = "BRAK";
            if (bom.Count() > 0)
            {
                var idxm = (from c in db2008.SLOWNIK_1s
                            where c.IMITM == bom[0].item_id
                            select c).Single();
                kod_materialu = idxm.IMLITM;
            }

            m.kod_materialu_galw = kod_materialu;
            m.kolor = dane.KOLOR;
            m.nazwa = zlecenie.name;
            m.nr_rysunku = zlecenie.drawing_no;
            m.nr_zlec_galw = int.Parse(zlecenie.ipo_order_no.ToString());
            m.rysunek = GenPDF.GetImage(zlecenie.drawing_no);
            m.userid = w_logged_user;
            m.nr_zlec_szlif = nr_zlec;

            bool tylko_poler_wyk = false;// m_.komentarz.Contains("!!");
            string kod_szlif = m.kod_zlecenia;

            baza_metekDataContext dbm = new baza_metekDataContext();
            try
            {
                if (idx.KOD_PLAN.Trim() == "GALWANIKA")
                {
                    var rozpis_zlecenia = (from c in db2008.IPO_Rozpis_mats
                                           where c.wyrob_l.Trim() == m.kod_zlecenia.Trim()
                                           select c).First();
                    kod_szlif = rozpis_zlecenia.skladnik_l.Trim();
                }

            }
            catch { }

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
            var rw = from c in db2.IPO_ZDAWKA_PW
                     where c.RW_PW == "RW" && c.Nr_zlecenia_IPO == m.nr_zlec_szlif
                     group c by new { c.Magazyn_IPO, c.Nr_indeksu } into fgr
                     select new { Magazyn_IPO = fgr.Key.Magazyn_IPO, Nr_indeksu = fgr.Key.Nr_indeksu, Ilosc = fgr.Sum(g => g.Ilosc) };
            var zlec = (from c in db2.IPO_ZLECENIA where c.ipo_nr_zlec == m.nr_zlec_szlif select c.cecha_A).FirstOrDefault();

            var db3 = new baza_metekDataContext();
            var metka = from c in db3.Metki_s where c.Nr_zlecenia.Trim() == nr_zlec.ToString() select c;
            if (metka.Count() == 0)
            {
                try
                {

                    Metki_ m1 = new Metki_();
                    m1.Ilosc = (int)zlecenie.quantity;
                    m1.Nr_kodu = m.kod_zlecenia.Trim();
                    m1.Nr_zlecenia = nr_zlec.ToString();
                    m1.User_id = "test_sk";
                    m1.Data_utw = DateTime.Now;
                    db3.Metki_s.InsertOnSubmit(m1);
                    db3.SubmitChanges();

                }
                catch { }

            }






            for (int i = 0; i < m.il_stron; i++)
            {

                StringBuilder txt = new StringBuilder();
                int dl_metki = 1200;
                int curr_y = 210;
                txt.Append($"^XA^CWZ,E:ARI000.FNT^FS^XZ^XA^LL%DL_METKI%^PA1,1,1,1^FS^FO110,10^CI28^AZN,40,40^F11^FDSZLIFIERNIA - DO {idx.KOLOR.Trim()}^FS^FS^FB600,2,0,C,0^FO5,45^CI28^AZN,30,30^F11^FD{idx.NAZWA + " - " + m.ilosc_szt[i] + " szt"}^FS" +
                $"^BY4,2,30^FO20,100^BC^FD{m.nr_zlec_szlif}_{(i + 1).ToString()}^FS^FO5,170^GB700,1,3^FS^CFA,30^FO5,180^FDMARSZRUTA:({zlec ?? ""})^FS");

                foreach (var o in noper)
                {
                    string wst = "";

                    if (o.NormaZatwierdzona.Contains("*")) wst = "*";
                    if (!o.Nr_kol_operacji.Value.ToString().EndsWith("0")) wst = "A" + wst;
                    txt.Append($"^FO5,{curr_y}^FD{wst + o.OPERACJA + " " + ((decimal)((decimal)m.ilosc_szt[i] * 480m) / (decimal)o.IloscSztZm).ToString("####.#") + "/" + o.IloscSztZm.ToString()}^FS^A0N,1,1^BCN,30,Y,Y,Y,N^FO15,{curr_y + 30}^BC^FD{"OPER_" + o.Id_operacji.ToString()}^FS");
                    curr_y = curr_y + 80;
                    dl_metki = dl_metki + 80;

                }



                foreach (var d in rw.Where(c => c.Ilosc != 0))
                {
                    txt.Append($"^FS^CFA,30^FO5,{curr_y}^FD{d.Magazyn_IPO + " " + d.Nr_indeksu + " :" + d.Ilosc.ToString()}^FS");
                    curr_y = curr_y + 30;
                    dl_metki = dl_metki + 30;
                }

                string grafika = srv.RETURN_GFA_ZPL(dane.NR_RYSUNKU.Trim());

                txt.Append($"^FO5,{curr_y + 90}^GB700,1,3^FS^CFB,60^FO10,{curr_y + 90 + 30}^FD{m.kod_zlecenia.Trim()} - {idx.KOLOR}^FS^A0N,1,1^BCN,50,$Y,Y,Y,N^FO5,{curr_y + 90 + 30 + 60}^BC^FD{m.kod_zlecenia.Trim()}^FS^FB600,2,0,C,0^FO15,{curr_y + 90 + 30 + 60 + 80}^CI28^AZN,30,30^F11^FD{m.nazwa}^FS^FS^FO5,{curr_y + 90 + 30 + 60 + 80 + 90}{grafika}^PQ1^XZ");

                int nwidth = 450;
                int nheight = (m.rysunek.Height * nwidth) / m.rysunek.Width;

                dl_metki = dl_metki + nheight - 450;

                string ntext = txt.ToString();
                ntext = ntext.Replace("%DL_METKI%", dl_metki.ToString());
                string file_name = System.IO.Path.GetTempPath() + @"\\" + Guid.NewGuid().ToString() + @".txt";
                System.IO.File.WriteAllText(file_name, ntext);
                System.IO.File.Copy(file_name, unc_drukarki);

                System.IO.File.Delete(file_name);
                Metki_PDF m_pdf = new Metki_PDF();
                m_pdf.Nr_zlecenia = m.nr_zlec_szlif.ToString();
                m_pdf.Data_utw = DateTime.Now;
                m_pdf.PDF = null;
                m_pdf.Ilosc = (int)m.ilosc_szt[i];

                dbm.Metki_PDFs.InsertOnSubmit(m_pdf);

            }

        }


        [WebMethod]
        public bool SZL_Sprawdz_zlecenie(int nr_zlec)
        {

            bool ok = true;
            var db = new DB2DataContext();
            var test = from c in db.IPO_ZDAWKA_PW where c.Nr_zlecenia_IPO == nr_zlec select c;
            if (test.Count() == 0) ok = false;
            foreach (var i in test)
            {
                if (string.IsNullOrEmpty(i.Nr_seryjny.Trim())) ok = false;


            }    



            return ok;
        }

            [WebMethod]
        public void SZL_Stop_zlec(int nr_zlec)
        {
            try
            {
                _ = Task.Run(new Action(() =>
                {
                    var db = new DB2DataContext();
                    Prace_zewn_API pr = new Prace_zewn_API();
                    var id = from c in db.API_prace_zewns where c.zlecenie == nr_zlec && c.Stan_pracy == "W_toku" select c.id;
                    foreach (var i in id)
                        pr.EndTask(i);



                }));


            }
            catch { }
        }

        [WebMethod]
        public string SZL_Start_zlec(int nr_zlec)
        {

            ///znadz id pracy
            ///
            string t = "";
            try
            {
                

                    var db = new DB2DataContext();
                    Prace_zewn_API pr = new Prace_zewn_API();
                    var id = from c in db.API_prace_zewns where c.zlecenie == nr_zlec  && c.Stan_pracy == "W planie" select c.id;
                    foreach (var i in id)
                     t =   pr.StartTask(i, DateTime.Now.AddMinutes(30));



              
               
            }
            catch { }
            return t;
        }

        [WebMethod]
        public void SZL_Metka(int nr_zlec)
        {

        }



        [WebMethod]
        public DataTable SZL_Refresh_prace_szlif(string f_nazwa, string fkolor, string findeks, string fstan, string fnrzlec, string fzamowienie, string f_cechaA, string f_cechaB, string f_cechaP)
        {
            DataTable dt = new DataTable();
            var db = new DB2DataContext();


            if (!string.IsNullOrEmpty(fkolor))
            {
                var items = (from c in db.API_prace_zewns
                             where (c.okprac == "Szlif_galw_" || c.okprac == "Szlifiernia") && c.KOLOR.Trim().ToLower() == fkolor.ToLower() && c.NAZWA.Contains(f_nazwa) &&
                             c.cecha_A.ToLower().Trim().Contains(f_cechaA.ToLower()) &&
                             c.cecha_B.ToLower().Trim().Contains(f_cechaB.ToLower()) &&
                             c.cecha_P.ToLower().Trim().Contains(f_cechaP.ToLower()) &&

                             c.Indeks_zlecenia.ToLower().Trim().Contains(findeks.ToLower()) &&
                             c.Stan_pracy.ToLower().Trim().Contains(fstan.ToLower()) &&
                             c.zlecenie.ToString().Contains(fnrzlec) && c.nr_zam_klienta.Trim().ToLower().Contains(fzamowienie)
                             orderby c.wsp descending
                             select new
                             {
                                 c.zlecenie,
                                 c.Indeks_zlecenia,
                                 c.NAZWA,
                                 c.ilosc_zam,
                                 c.iloscWe,
                                 c.KOLOR,
                                 c.nr_zam_klienta,
                                 c.boost,
                                 c.cecha_A,
                                 c.cecha_B,
                                 c.cecha_P,
                                 c.cecha_Q,
                                 c.dataGraniczna,
                                 c.dataUtworzenia,
                                 c.GALW_TECHN
                             }).Take(50).ToList();
                dt = LINQToDataTable(items);
            }
            else
            {
                var items = (from c in db.API_prace_zewns
                             where (c.okprac == "Szlif_galw_" || c.okprac == "Szlifiernia") && c.NAZWA.Contains(f_nazwa) &&
                             c.cecha_A.ToLower().Trim().Contains(f_cechaA.ToLower()) &&
                             c.cecha_B.ToLower().Trim().Contains(f_cechaB.ToLower()) &&
                             c.cecha_P.ToLower().Trim().Contains(f_cechaP.ToLower()) &&

                             c.Indeks_zlecenia.ToLower().Trim().Contains(findeks.ToLower()) &&
                             c.Stan_pracy.ToLower().Trim().Contains(fstan.ToLower()) &&
                             c.zlecenie.ToString().Contains(fnrzlec) && c.nr_zam_klienta.Trim().ToLower().Contains(fzamowienie)
                             orderby c.wsp descending
                             select new
                             {
                                 c.zlecenie,
                                 c.Indeks_zlecenia,
                                 c.NAZWA,
                                 c.ilosc_zam,
                                 c.iloscWe,
                                 c.KOLOR,
                                 c.nr_zam_klienta,
                                 c.boost,
                                 c.cecha_A,
                                 c.cecha_B,
                                 c.cecha_P,
                                 c.cecha_Q,
                                 c.dataGraniczna,
                                 c.dataUtworzenia,
                                 c.GALW_TECHN
                             }).Take(50).ToList();
                dt = LINQToDataTable(items);

            }
           

            dt.TableName = "TB";
            return dt;

        }



        [WebMethod]
        public string Drukuj_metkę(string ip, int port, string user, string nr_metki, string linie, bool ber)
        {
            string text_metki = @"^XA^LL1350^CI28^FO15,50^GFA,3760,3760,47,jO03F04,jQ084,,:::jQ02,iR01,7IF8Q01IFE0MFE01IFL03IFCR0SFC1IFEN07IF,7IF8Q03IFC0NF01FFEL01IFER0SFE0JFN0IFE,3IFCQ03IFC1NF81FFEL01IFEQ01TF03IF8L01IFC,1IFEQ07IF81NFC1FFEM0JFQ03TF81IFCL03IF8,1IFEQ07IF83NFC1FFEM0JFQ03TFC0IFEL07IF,0JFQ0JF03NFC1FFEM07IF8P07TFE07IFK01IFE,0JFP01IFE07NFC1FFEM03IFCP07UF03IF8J03IFC,07IF8O01IFE0OFC1FFEM03IFCP0VF81IFCJ07IF8,07IF8O03IFC0OFC1FFEM01IFEO01VFC0JFJ0JF,03IFCO03IFC1OFC1FFEM01IFEO01VFE07IF8001IFC,01IFEO07IF8M03FFC1FFEN0JFO03IFCT03IFC003IF8,01IFEO0JF8M03FFC1FFEN0JF8N03IFCT01IFE007IF,00JFO0JFN03FFC1FFEN07IF8N07IF8U0JF00IFE,00JFN01IFEN03FFC1FFEN03IFCN07IF8U07IF81IFC,007IF8M01IFEN03FFC1FFEN03IFCN0JFV01IFC3IF8,007IFCM03IFCN03FFC1FFEN01IFEM01IFEW0IFE7IF,003IFCM03IFCN03FFC1FFEN01IFEM01IFEW07LFE,001IFEM07IF8N03FFC1FFEO0JFM03IFCW03LFC,001IFEM0JFO03FFC1FFEO07IF8L03IFCW01LF8,I0JFM0JF03PFC1FFEO07IF8L07IF81UFC0KFE,I0JF8K01IFE07PFC1FFEO03IFCL0JF83UFE07JFC,I07IF8K01IFE0QFC1FFEO03IFCL0JF03VF03JF8,I03IFCK03IFC0QFC1FFEO01IFEK01IFE07VFC1JF,I03IFCK07IFC1QFC1FFEO01JFK01IFE0WFC0JF,I01IFEK07IF81QFC1FFEP0JFK03IFC0WF81JF8,I01IFEK0JF03QFC1FFEP07IF8J03IFC1WF03JFC,J0JFK0JF07QFC1FFEP07IF8J07IF81VFE0KFE,J0JF8I01IFE07QFC1FFEP03IFCJ0JF83VFC1LF,J07IF8I01IFE0RFC1FFEP03IFCJ0JF03VF83LF8,J03IFCI03IFC0IFL03FFC1FFEP01IFEI01IFEY07LFC,J03IFCI07IF81FFEL03FFC1FFEP01JFI01IFEY0MFE,J01IFEI07IF83FFEL03FFC1FFEQ0JFI03IFCX01IFE7IF,J01JFI0JF03FFCL03FFC1FFEQ07IF8007IFCX03IFC3IF8,K0JFI0JF07FFCL03FFC1FFEQ07IF8007IF8X07IF81IFC,K0JF801IFE07FF8L03FFC1FFEQ03IFC00JFY0JF00IFE,K07IF803IFE0IF8L03FFC1FFEQ03IFE00JFX01IFE007IF,K03IFC03IFC0IFM03FFC1FFEQ01IFE01IFEX03IFC003IF8,K03IFC07IF81FFEM03FFC1FFER0JF01IFEX07IF8001IFE,K01IFE07IF83FFEM03FFC1IFR0JF03IFCX0JFJ0JF,K01JF0JF03FFCM03FFC1SFE07IF87IFC1UF81IFEJ07IF8,L0JF1JF07FFCM03FFC1TF07IFC7IF81UF03IFCJ03IFC,L07IF9IFE07FF8M03FFC1TF03IFCJF03TFE07IF8J01IFE,L07IFBIFE0IFN03FFC1TF83IFEJF07TFC0IFEL0JF,L03MFC0IFN03FFC1TF81MFE07TF81IFCL07IF8,L03MF81FFEN03FFC0TFC0MFE0UF03IF8L03IFC,L01MF83FFEN03FFC0TFE0MFC0TFE07IFM01IFE,L01MF03FFCN03FFC07SFE07LFC1TFC0IFEN0JF,M0MF07FFCN03FFC03TF07LF83TF81IFCN07IF8,M07KFE07FF8N03FFC007SF03LF03TF03IF8N03IFC,M07KFC0IFgP03LF,M03KFC1IFgP01KFE,M03KF81FFEgQ0KFE,M01KF83FFEgQ0KFC,N0KF03FFCgQ07JF8,N0JFE07FF8gQ03JF807FCU060077L024,N07IFC07FFgR01JF00402U0801808K0244,N01IF80FFEgS0IFC00403U0801004K0204,O0FFE01FFCgS03FF800401U0802006K0204,O01F001FFgU07CI04011F10608F84C03E1E020024007C24F404,hV0403209060904700410802002408822I408,hV040240484090260080880400240880244208,hV07FC40481120240080880400240802244208,hV04004048912024008048040024080624421,hV0400404091200400804802002408C224411,hV040040448A200400800802002409002441,hV040040450A202400808803024409022440A,hV040060C30A10240040880101C409022440A,hV0400318304184400610800C1C2188724204,i0EI04078I01EJ03E21C07100104,jO04,jO08,jN01,^FS^FX ^CF0,40" +
@"^FO400,70^FDValvex S.A.^FS^CF0,30^FO400,100^FDul.Nad Skawą 2^FS^FO400,135^FD34-240 Jordanow" +
@"^FS^FO400,170^FDPolska^FS^FO10,250^GB620,1,3^FS^FX^CF0,40" +
@"^FS^FO30,130^CF0,60^FD%BER%^FS^FS^FO30,190^CF0,70^FD%NR%^FS^FS^FO350,210^CF0,40^FD%USER%^FS^FO170,230^FO600,30^BQN,2,6^FDmetka=%NR%^FS^FS^CF0,40^FO25,310^FB520,100,,^FD%TEXT%^FS^XZ";

            if (ber) text_metki = text_metki.Replace("%BER%", "BER!");
            else text_metki = text_metki.Replace("%BER%", "");
            text_metki = text_metki.Replace("%NR%", nr_metki);
            text_metki = text_metki.Replace("%TEXT%", linie);
            text_metki = text_metki.Replace("%USER%", user);

            Service1 srv = new Service1();
            srv.SendToZebra(ip, port, text_metki);



            //string file_name = System.IO.Path.GetTempPath() + @"\\" + Guid.NewGuid().ToString() + @".txt";
            //System.IO.File.WriteAllText(file_name, text_metki);
            //System.IO.File.Copy(file_name, UNCdrukarki);




            //System.IO.File.Delete(file_name);
            return "OK";
        }

        /// <summary>
        /// drukuje metkę do transferu
        /// </summary>
        /// <param name="ip">adres drukarki</param>
        /// <param name="port">port drukarki</param>
        /// <param name="user">Nazwa użytkownika - jest drukowana na etykiecie</param>
        /// <param name="nr_metki">nr transferu, kompletacji itp.</param>
        /// <param name="linie">tekst na betce</param>
        /// <param name="ber">Czy pilne?</param>
        /// <param name="item_id">indeks wyrobu</param>
        /// <returns>Tekst OK jeżeli nie ma błędu, ERROR jeżeli błąd</returns>
        [WebMethod]
        public string Drukuj_metkę_transfer(string ip, int port, string user, string nr_metki, string linie, bool ber, string item_id)
        {
            string text_metki = @"^XA^LL1350^CI28^FO15,50^GFA,3760,3760,47,jO03F04,jQ084,,:::jQ02,iR01,7IF8Q01IFE0MFE01IFL03IFCR0SFC1IFEN07IF,7IF8Q03IFC0NF01FFEL01IFER0SFE0JFN0IFE,3IFCQ03IFC1NF81FFEL01IFEQ01TF03IF8L01IFC,1IFEQ07IF81NFC1FFEM0JFQ03TF81IFCL03IF8,1IFEQ07IF83NFC1FFEM0JFQ03TFC0IFEL07IF,0JFQ0JF03NFC1FFEM07IF8P07TFE07IFK01IFE,0JFP01IFE07NFC1FFEM03IFCP07UF03IF8J03IFC,07IF8O01IFE0OFC1FFEM03IFCP0VF81IFCJ07IF8,07IF8O03IFC0OFC1FFEM01IFEO01VFC0JFJ0JF,03IFCO03IFC1OFC1FFEM01IFEO01VFE07IF8001IFC,01IFEO07IF8M03FFC1FFEN0JFO03IFCT03IFC003IF8,01IFEO0JF8M03FFC1FFEN0JF8N03IFCT01IFE007IF,00JFO0JFN03FFC1FFEN07IF8N07IF8U0JF00IFE,00JFN01IFEN03FFC1FFEN03IFCN07IF8U07IF81IFC,007IF8M01IFEN03FFC1FFEN03IFCN0JFV01IFC3IF8,007IFCM03IFCN03FFC1FFEN01IFEM01IFEW0IFE7IF,003IFCM03IFCN03FFC1FFEN01IFEM01IFEW07LFE,001IFEM07IF8N03FFC1FFEO0JFM03IFCW03LFC,001IFEM0JFO03FFC1FFEO07IF8L03IFCW01LF8,I0JFM0JF03PFC1FFEO07IF8L07IF81UFC0KFE,I0JF8K01IFE07PFC1FFEO03IFCL0JF83UFE07JFC,I07IF8K01IFE0QFC1FFEO03IFCL0JF03VF03JF8,I03IFCK03IFC0QFC1FFEO01IFEK01IFE07VFC1JF,I03IFCK07IFC1QFC1FFEO01JFK01IFE0WFC0JF,I01IFEK07IF81QFC1FFEP0JFK03IFC0WF81JF8,I01IFEK0JF03QFC1FFEP07IF8J03IFC1WF03JFC,J0JFK0JF07QFC1FFEP07IF8J07IF81VFE0KFE,J0JF8I01IFE07QFC1FFEP03IFCJ0JF83VFC1LF,J07IF8I01IFE0RFC1FFEP03IFCJ0JF03VF83LF8,J03IFCI03IFC0IFL03FFC1FFEP01IFEI01IFEY07LFC,J03IFCI07IF81FFEL03FFC1FFEP01JFI01IFEY0MFE,J01IFEI07IF83FFEL03FFC1FFEQ0JFI03IFCX01IFE7IF,J01JFI0JF03FFCL03FFC1FFEQ07IF8007IFCX03IFC3IF8,K0JFI0JF07FFCL03FFC1FFEQ07IF8007IF8X07IF81IFC,K0JF801IFE07FF8L03FFC1FFEQ03IFC00JFY0JF00IFE,K07IF803IFE0IF8L03FFC1FFEQ03IFE00JFX01IFE007IF,K03IFC03IFC0IFM03FFC1FFEQ01IFE01IFEX03IFC003IF8,K03IFC07IF81FFEM03FFC1FFER0JF01IFEX07IF8001IFE,K01IFE07IF83FFEM03FFC1IFR0JF03IFCX0JFJ0JF,K01JF0JF03FFCM03FFC1SFE07IF87IFC1UF81IFEJ07IF8,L0JF1JF07FFCM03FFC1TF07IFC7IF81UF03IFCJ03IFC,L07IF9IFE07FF8M03FFC1TF03IFCJF03TFE07IF8J01IFE,L07IFBIFE0IFN03FFC1TF83IFEJF07TFC0IFEL0JF,L03MFC0IFN03FFC1TF81MFE07TF81IFCL07IF8,L03MF81FFEN03FFC0TFC0MFE0UF03IF8L03IFC,L01MF83FFEN03FFC0TFE0MFC0TFE07IFM01IFE,L01MF03FFCN03FFC07SFE07LFC1TFC0IFEN0JF,M0MF07FFCN03FFC03TF07LF83TF81IFCN07IF8,M07KFE07FF8N03FFC007SF03LF03TF03IF8N03IFC,M07KFC0IFgP03LF,M03KFC1IFgP01KFE,M03KF81FFEgQ0KFE,M01KF83FFEgQ0KFC,N0KF03FFCgQ07JF8,N0JFE07FF8gQ03JF807FCU060077L024,N07IFC07FFgR01JF00402U0801808K0244,N01IF80FFEgS0IFC00403U0801004K0204,O0FFE01FFCgS03FF800401U0802006K0204,O01F001FFgU07CI04011F10608F84C03E1E020024007C24F404,hV0403209060904700410802002408822I408,hV040240484090260080880400240880244208,hV07FC40481120240080880400240802244208,hV04004048912024008048040024080624421,hV0400404091200400804802002408C224411,hV040040448A200400800802002409002441,hV040040450A202400808803024409022440A,hV040060C30A10240040880101C409022440A,hV0400318304184400610800C1C2188724204,i0EI04078I01EJ03E21C07100104,jO04,jO08,jN01,^FS^FX ^CF0,40
^FO400,70^FDValvex S.A.^FS^CF0,30^FO400,100^FDul.Nad Skawą 2^FS^FO400,135^FD34-240 Jordanow
^FS^FO400,170^FDPolska^FS^FO10,250^GB620,1,3^FS^FX^CF0,40
^FS^FO30,130^CF0,60^FD%BER%^FS^FS^FO30,190^CF0,70^FD%NR%^FS^FS^FO350,210^CF0,40^FD%USER%^FS^FO170,230^FO600,30^BQN,2,6^FDmetka=%NR%^FS^FS^FO480,190^BCN,15,N^FD%NR%^FS^FO40,270^BCN,30,N^FD%ITEMID%^FS^CF0,40^FO25,310^FB620,100,,^FD%TEXT%^FS^XZ";

            if (ber) text_metki = text_metki.Replace("%BER%", "BER!");
            else text_metki = text_metki.Replace("%BER%", "");
            text_metki = text_metki.Replace("%NR%", nr_metki);
            text_metki = text_metki.Replace("%TEXT%", linie);
            text_metki = text_metki.Replace("%USER%", user);
            text_metki = text_metki.Replace("%ITEMID%", item_id);
            Service1 srv = new Service1();
            srv.SendToZebra(ip, port, text_metki);



            //string file_name = System.IO.Path.GetTempPath() + @"\\" + Guid.NewGuid().ToString() + @".txt";
            //System.IO.File.WriteAllText(file_name, text_metki);
            //System.IO.File.Copy(file_name, UNCdrukarki);




            //System.IO.File.Delete(file_name);
            return "OK";
        }




        [WebMethod]
        public string Drukuj_metk_KJ(string UNCdrukarki, string user, string nr_metki, string linie)
        {
            string msg = "OK";
            try
            {
                string text_metki = @" ^XA^CWZ,E:ARI000.FNT^FS^XZ^XA" +

         "^PA1,1,1,1^FS ^FX Enables Advanced Text ^FS " +
       "^FO10,50^CI28^AZN,50,50^F16^FDZebra Technologies^FS " +
       "^FO10,150^CI28^AZN,50,100^F16^FDUNICODE^FS " +
       "^FO020,260^CI28^AZN,50,40^F16^FDzażółć gęślą jaźń^FS " +
       "^PQ1^XZ";






                text_metki = text_metki.Replace(" % NR%", nr_metki);
                text_metki = text_metki.Replace("%TEXT%", linie);
                text_metki = text_metki.Replace("%USER%", user);

                //Service1 srv = new Service1();
                //srv.SendToZebra(ip, port, text_metki);



                string file_name = System.IO.Path.GetTempPath() + @"\\" + Guid.NewGuid().ToString() + @".txt";
                System.IO.File.WriteAllText(file_name, text_metki);
                System.IO.File.Copy(file_name, UNCdrukarki);




                System.IO.File.Delete(file_name);
            }
            catch (Exception Ex) { msg = Ex.Message; }
            return msg;
        }

        [WebMethod]
        public List<IPO_STANY_MAG> StanMagazynowy_JDE(List<string> litms)
        {
            var db = new DBDataContext();
            var stan = (from c in db.IPO_STANY_MAGs
                        where litms.Contains(c.LITM.Trim()) && c.LOK != "S1B1000"
                        select c);
            return stan.ToList();
        }

        [WebMethod]
        public List<IPO_STANY_MAG> StanMagazynowy_lista(List<string> litms, string Userlogin)
        {
            var db = new DBDataContext();
            var stan = (from c in db.IPO_STANY_MAGs
                        where litms.Contains(c.LITM.Trim()) && c.LOK != "S1B1000"
                        select c);

            return stan.ToList();

        }



        [WebMethod]
        public List<IPO_STANY> StanMagazynowy(string litm, string Userlogin)
        {
            var db = new DBDataContext();
            var dbt = new dbtrans1DataContext();
            // var uprawnienia = from c in dbt.Aktualne_grupy_views
            //                   where c.login == Userlogin && (bool)c.KOMPLETACJA_Z
            //                  select c.Grupa;



            var stan = (from c in db.IPO_STANies
                        where c.LITM.Trim() == litm.Trim() && c.MAG_ZAK == 0 && c.QTY > 0 && c.LOK != "S1B1000"
                        select c).ToList();






            //return list;
            var spakowane = from c in dbt.zlecenia_szczegoly
                            where c.status_linii.Trim().ToLower() == "spakowane" && c.litm.Trim() == litm.Trim()
                            select c;


            if (spakowane.Count() == 0) return stan.ToList();

            foreach (var st in stan)
            {
                foreach (var spk in spakowane)
                {
                    if (st.LITM.Trim() == spk.litm.Trim() &&
                        st.MAG.Trim() == spk.magazyn_z.Trim() &&
                        st.LOK.Trim() == spk.lokalizacja_z.Trim())
                    {
                        st.QTY = (st.QTY - spk.ilosc_zrealizowana);

                    }

                }


            }


            return stan;

        }

        [WebMethod]
        public bool Sprawdz_upr_POTWIERDZANIE_DO(string Userlogin, string lok, string mag)
        {

            var lista_lok = this.Pobierz_lokalizacje_POTWIERDZANIE_DO(Userlogin).ToList();


            var check = from c in lista_lok
                        where c.LILOCN.Trim().ToLower() == lok.Trim().ToLower() && c.LIMCU.Trim().ToLower() == mag.Trim().ToLower()
                        select c;


            if (check.Count() == 0) return false;
            else return true;



        }

        [WebMethod]
        public long TestStanow(int Nr_przewodnika)
        {

            var db = new dbtrans1DataContext();

            var lista_indeksow = (from c in db.zlecenia_szczegoly
                                  where c.id_zlecenia == Nr_przewodnika
                                  select c.litm.Trim()).ToList();
            var watch = System.Diagnostics.Stopwatch.StartNew();



            var mwatch = System.Diagnostics.Stopwatch.StartNew();
            //  var st = Stan(lista_indeksow, "brde");

            var test = mwatch.ElapsedMilliseconds;


            watch.Stop();
            return watch.ElapsedMilliseconds;




        }
        [WebMethod]
        public List<string> Pobierz_MAG_POTWIERDZANIE_DO(string Userlogin)
        {

            var lista_lok = this.Pobierz_lokalizacje_POTWIERDZANIE_DO(Userlogin);

            var mag = (from c in lista_lok
                       select new { mag = c.LIMCU.Trim() }).Distinct().ToArray();
            List<string> nlist = new List<string>();
            foreach (var m in mag)
            {
                nlist.Add(m.mag.Trim());
            }



            return nlist;
        }
        [WebMethod]
        public List<string> Pobierz_LOK_POTWIERDZANIE_DO(string Userlogin)
        {

            var lista_lok = this.Pobierz_lokalizacje_POTWIERDZANIE_DO(Userlogin);

            var mag = (from c in lista_lok
                       select new { lok = c.LILOCN.Trim() }).Distinct();
            List<string> nlist = new List<string>();
            foreach (var m in mag)
            {
                nlist.Add(m.lok.Trim());
            }



            return nlist;
        }

        [WebMethod]
        public bool UPR_Czy_nalezy_do_grupy(string grupa, int UserId)
        {
            bool test = false;
            var db = new dbtrans1DataContext();
            var wynik = from c in db.Aktualne_grupy_views where c.id_uzytkownika == UserId && c.Grupa.Trim() == grupa select c;
            foreach (var w in wynik)
            {
                if (w.Doz_wyk_komplet == true) test = true;

            }



            return test;
        }



        //Cała lista_usera
        [WebMethod]
        public List<Grupy> UPR_Pobierz_grupy()
        {

            var db = new dbtrans1DataContext();

            var gr = from c in db.Grupies
                     select c;

            return gr.ToList();



        }



        [WebMethod]
        public List<grupy_uzytkownicy> UPR_Pobierz_grupy_usera(int UserId)
        {
            var db = new dbtrans1DataContext();
            var grupy = from c in db.grupy_uzytkownicies
                        where c.id_uzytkownika == UserId
                        select c;
            return grupy.ToList();
        }








        [WebMethod]
        public int UPR_Usun_usera_z_grupy(int UserId, int id_grupy)
        {
            try
            {
                var db = new dbtrans1DataContext();
                var gr = from c in db.grupy_uzytkownicies
                         where c.id_uzytkownika == UserId && c.id_grupy == id_grupy
                         select c;

                foreach (var g in gr)
                { g.Data_do = DateTime.Now.AddDays(-1); }


                db.SubmitChanges();
                return 0;
            }
            catch { return -1; }
        }


        [WebMethod]
        public int UPR_Dodaj_usera_do_grupy(int UserId, int id_grupy, DateTime _od, DateTime _do)
        {
            try
            {
                var db = new dbtrans1DataContext();
                var gr = new grupy_uzytkownicy();
                gr.Data_do = _do;
                gr.Data_od = _od;
                gr.id_grupy = id_grupy;
                gr.id_uzytkownika = UserId;
                db.grupy_uzytkownicies.InsertOnSubmit(gr);
                db.SubmitChanges();
                return 0;
            }
            catch { return -1; }
        }
        [WebMethod]
        public List<IPO_magazyny_IPO2JDE> Pobierz_lokalizacje_POTWIERDZANIE_DO(string Userlogin)
        {
            List<IPO_magazyny_IPO2JDE> lista_mag = new List<IPO_magazyny_IPO2JDE>();
            var db2 = new DBDataContext();
            var wszystkie_mag = (from c in db2.IPO_magazyny_IPO2JDE
                                 select c).ToList();


            var db = new dbtrans1DataContext();
            var uprawnienia = (from c in db.Aktualne_grupy_views
                               where c.login == Userlogin && (bool)c.POTWIERDZANIE_DO
                               select c.Grupa).ToList();



            foreach (var gr in uprawnienia)
            {
                var mag = from c in wszystkie_mag
                          where c.mag_ipo.StartsWith(gr)
                          select c;

                lista_mag.AddRange(mag);



            }
            lista_mag = lista_mag.Distinct().ToList();



            return lista_mag;
        }
        [WebMethod]
        public List<IPO_magazyny_IPO2JDE> Pobierz_lokalizacje_KOMPLETACJA_DO(string Userlogin)
        {
            List<IPO_magazyny_IPO2JDE> lista_mag = new List<IPO_magazyny_IPO2JDE>();
            var db2 = new DBDataContext();
            var wszystkie_mag = (from c in db2.IPO_magazyny_IPO2JDE
                                 select c).ToList();


            var db = new dbtrans1DataContext();
            var uprawnienia = (from c in db.Aktualne_grupy_views
                               where c.login == Userlogin && (bool)c.KOMPLETACJA_DO
                               select c.Grupa).ToList();



            foreach (var gr in uprawnienia)
            {
                var mag = from c in wszystkie_mag
                          where c.mag_ipo.StartsWith(gr)
                          select c;

                lista_mag.AddRange(mag);



            }
            lista_mag = lista_mag.Distinct().ToList();



            return lista_mag;
        }

        [WebMethod]
        public int Userid(string user_name)
        {
            var db = new dbtrans1DataContext();
            var id = from c in db.uzytkownicies where c.login == user_name select c.id;


            return id.First();
        }

        [WebMethod]
        public int Odrzuc_kompletacje(int Nr_kompletacji, int userId, string powod)
        {

            var db = new dbtrans1DataContext();

            var nag = from c in db.zlecenia_naglowkis
                      where c.Nr_zlecenia == Nr_kompletacji
                      select c;
            try
            {

                foreach (var n in nag)
                {
                    n.Status = "odrzucone";
                    n.Autor_ost_mod = userId;
                    n.Data_ost_mod = DateTime.Now;
                    db.SubmitChanges();
                }

                var linie = from c in db.zlecenia_szczegoly
                            where c.id_zlecenia == Nr_kompletacji &&
                            c.ilosc_otwarta != 0 && (c.Status_ksiegowania ?? "x") !="P"
                            select c;

                foreach (var l in linie)
                {
                    l.ilosc_otwarta = 0;
                    l.status_linii = "odrzucone";
                    l.autor_ost_oper = userId;
                    l.data_ost_oper = DateTime.Now;
                    l.Opis_statusu = powod.Trim();
                    db.SubmitChanges();

                }
            }
            catch { }
            try
            {
                var dane_odrzucil = WezDaneUseraId(userId);
                var dane_autor = WezDaneUseraId(nag.First().Utworzony_przez);
                Service1 srv = new Service1();
                srv.SendAlert(dane_autor.email, "Odrzucono przewodnik " + nag.First().Nr_zlecenia + " wyrób:" + nag.First().LITM,
                    "Przewodnik odrzucono przez " + dane_odrzucil.Nazwisko + " " + dane_odrzucil.Imię + ",POWÓD: " + powod

                    );


            }
            catch { }

            return 0;
        }

        //do podziału linii - zwraca KONIEC - jeżeli kompletacja zakończona lub PODZIAŁ - jeżeli 
        [WebMethod]
        public int Odrzuc_linie_kompletacji_id(int id_rec, string user_name, string powod)
        {

            var db = new dbtrans1DataContext();

            var linia_total = (from c in db.zlecenia_szczegoly
                               where c.id == id_rec select c);

            if (linia_total.Count() != 1) return 0;

            var linia = linia_total.FirstOrDefault();

            if (linia.Status_ksiegowania == "Z" || linia.Status_ksiegowania == "K" || linia.Status_ksiegowania == "P") return 0;


            var u = WezDaneUsera(user_name);

            linia.ilosc_otwarta = 0;
            linia.ilosc_zrealizowana = 0;
            linia.status_linii = "odrzucone                     ";
            linia.data_ost_oper = DateTime.Now;
            linia.autor_ost_oper = u.Id_usera;
            linia.Opis_statusu = powod;
            db.SubmitChanges();
            var cr = WezDaneUseraId(linia.autor_zlecenia);
            if (powod != "Domknięcie zlecenia")
            {
                Service1 srv = new Service1();
                srv.SendAlert(cr.email, linia.id_zlecenia + ": Odrzucono linię kompletacji", "Indeks " + linia.litm + " " + linia.opis + ", Ilosc: " + linia.ilosc_zamowiona + ". Przez: " + u.login + " ,powód:" + powod);
            }
            Logger(user_name, linia.litm, linia.ilosc_zrealizowana.ToString() + " " + linia.JM,
                   linia.id_zlecenia, "ODRZUCONO LINIĘ:" + powod, linia.magazyn_z.Trim() + "=>" + linia.magazyn_do.Trim(), linia.lokalizacja_z.Trim() + "=>" + linia.lokalizacja_do.Trim(), linia.nr_linii);

            Ustaw_status_zlecenia(linia.id_zlecenia, this.WezDaneUsera(user_name).Id_usera);
            return 0;
        }




        //do podziału linii - zwraca KONIEC - jeżeli kompletacja zakończona lub PODZIAŁ - jeżeli 
        [WebMethod]
        public int Odrzuc_linie_kompletacji(int id_kompletacji, int nr_linii, string user_name, string powod)
        {

            var db = new dbtrans1DataContext();

            var linia_total = (from c in db.zlecenia_szczegoly
                               where c.nr_linii == nr_linii && c.id_zlecenia == id_kompletacji
                               select c);

            if (linia_total.Count() != 1) return 0;

            var linia = linia_total.FirstOrDefault();

            if (linia.Status_ksiegowania == "Z" || linia.Status_ksiegowania == "K" || linia.Status_ksiegowania == "P") return 0;


            var u = WezDaneUsera(user_name);

            linia.ilosc_otwarta = 0;
            linia.ilosc_zrealizowana = 0;
            linia.status_linii = "odrzucone                     ";
            linia.data_ost_oper = DateTime.Now;
            linia.autor_ost_oper = u.Id_usera;
            linia.Opis_statusu = powod;
            db.SubmitChanges();
            var cr = WezDaneUseraId(linia.autor_zlecenia);
            if (powod != "Domknięcie zlecenia")
            {
                Service1 srv = new Service1();
                srv.SendAlert(cr.email, linia.id_zlecenia + ": Odrzucono linię kompletacji", "Indeks " + linia.litm + " " + linia.opis + ", Ilosc: " + linia.ilosc_zamowiona + ". Przez: " + u.login + " ,powód:" + powod);
            }
            Logger(user_name, linia.litm, linia.ilosc_zrealizowana.ToString() + " " + linia.JM,
                   linia.id_zlecenia, "ODRZUCONO LINIĘ:" + powod, linia.magazyn_z.Trim() + "=>" + linia.magazyn_do.Trim(), linia.lokalizacja_z.Trim() + "=>" + linia.lokalizacja_do.Trim(), linia.nr_linii);

            Ustaw_status_zlecenia(id_kompletacji, this.WezDaneUsera(user_name).Id_usera);
            return 0;
        }

        [WebMethod]
        public List<Analiza> Analiza_zlecenia(int nr_zlec)
        {
            List<Analiza> an = new List<Analiza>();

            //var linie = Linie_zamowienia(nr_zlec).Where(x => x.status_linii.Trim().ToLower() == "oczekuje" && x.ilosc_otwarta != 0);
            var linie = Linie_zamowienia_szczegoly(nr_zlec).Where(x => x.status_linii.Trim().ToLower() == "oczekuje" && x.ilosc_otwarta != 0).ToList();
            //var lok_docelowa = (from c in linie select c.lokalizacja_do).First();


            var lista = from c in linie
                        group c by new { c.litm, c.opis, c.PRP4, c.SRP3, c.lokalizacja_do, c.IMSTKT } into g
                        select new { g.Key.litm, g.Key.opis, ILOSC = g.Sum(s => s.ilosc_otwarta), g.Key.PRP4, g.Key.SRP3, g.Key.lokalizacja_do, g.Key.IMSTKT };
            var lista_indeksow = (from c in lista select c.litm).Distinct().ToList();

            var stany_mag = StanMagazynowy_JDE(lista_indeksow);


            foreach (var l in lista)
            {
                Analiza a = new Analiza();
                var stan = stany_mag.Where(c => c.LOK.Trim().ToLower() != l.lokalizacja_do.Trim().ToLower() &&
                !c.LOK.Contains("-32 ") && !c.LOK.Contains("USŁUGI") && !c.LOK.Contains("ODCI") && !c.MAG.Contains("PVD") && !c.LOK.Contains("-42") && !c.LOK.Contains("-41") && !c.LOK.Contains("P-2") && !c.LOK.Contains("P42-4") &&
                !c.MAG.Contains("IZ") && !c.LOK.Contains("POPRAW") && c.LITM.Trim().ToLower() == l.litm.Trim().ToLower()).Sum(x => x.QTY);
                if (stan >= l.ILOSC) a.STATUS = "OK";
                else { a.STATUS = "BRAK"; }

                a.litm = l.litm;
                a.ilosc_otwarta = Math.Round((double)l.ILOSC, 2);
                a.nazwa = l.opis;
                a.PRP4 = l.PRP4;
                a.SRP3 = l.SRP3;
                a.IMSTKT = l.IMSTKT;

                a.stan_mag = Math.Round((double)stan, 2);
                an.Add(a);

            }




            return an;
        }
        /// <summary>
        /// Tworzy nową wiadomość - dla konkretnego odbiorcy lub osoby kompletującej konkretną linię. 
        /// Jeżeli treść dotyczy zlecenia i linii, to pojawi sie dopiero po wejściu na tę linię.
        /// Metoda zwraca 0 przy powodzeniu i -1 jeżeli błąd...
        /// </summary>
        /// <param name="autor">Id tworzącego komunikat</param>
        /// <param name="odbiorca">Id odbiorcy - jeżeli = 0 to powinne byc uzupełnione nr_zlec i nr linii</param>
        /// <param name="nr_zlec">Nr zlecenia, którego dotyczy komunikat. Jeżeli = 0, to wiadomość zostanie pokazana po 60 sekundach odbiocy jeżeli ma zasieg sieci</param>
        /// <param name="nr_linii">Nr linii, której dotyczy komunikat</param>
        /// <param name="tresc">Treść komunikatu</param>
        /// <returns></returns>
        [WebMethod]
        public int KOM_dodaj_komunikat(int autor, int odbiorca, int nr_zlec, int nr_linii, string tresc)
        {
            Komunikaty kom = new Komunikaty();
            kom.Autor = autor;
            kom.Data_utworzenia = DateTime.Now;
            kom.Nr_linii = nr_linii;
            kom.Nr_zlecenia = nr_zlec;
            kom.Odbiorca = odbiorca;
            kom.Odczytano = false;
            dbtrans1DataContext db = new dbtrans1DataContext();
            db.Komunikaties.InsertOnSubmit(kom);


            return 0;
        }
        /// <summary>
        /// Zwraca listę komunikatów - dla konkretnego usera i/lub nr_zlecenia i linii
        /// </summary>
        /// <param name="odbiorca"></param>
        /// <param name="nr_zlec"></param>
        /// <param name="nr_linii"></param>
        /// <returns></returns>
        public List<Komunikaty> KOM_pobierz_komunikaty(int odbiorca, int nr_zlec, int nr_linii)
        {
            return new List<Komunikaty>();


        }
        public void KOM_potwierdz_komunikat(int odbiorca, int id_komunikatu)
        {

        }


        [WebMethod]
        public int Kompletuj_linię(int id_kompletacji, int nr_linii, string user_name, double ilosc, string mag, string lok)
        {
            var db = new dbtrans1DataContext();
            if (ilosc == 0) return -1;


            var linia = (from c in db.zlecenia_szczegoly
                         where c.nr_linii == nr_linii && c.id_zlecenia == id_kompletacji select c).First();
            if (linia.Status_ksiegowania == "Z" || linia.Status_ksiegowania == "K" || linia.Status_ksiegowania == "P" || linia.status_linii == "spakowane") return -3;

            if (linia.ilosc_otwarta <= ilosc) //normalna linia 
            {
                linia.ilosc_otwarta = 0;
                linia.magazyn_z = mag;
                linia.lokalizacja_z = lok;
                linia.ilosc_zrealizowana = ilosc;
                linia.status_linii = "spakowane";
                linia.data_ost_oper = DateTime.Now;
                linia.autor_ost_oper = Userid(user_name);
                db.SubmitChanges();
                Logger(user_name, linia.litm, linia.ilosc_zrealizowana.ToString() + " " + linia.JM,
                    linia.id_zlecenia, "SPAKOWANO LINIĘ", linia.magazyn_z.Trim() + "=>" + linia.magazyn_do.Trim(), linia.lokalizacja_z.Trim() + "=>" + linia.lokalizacja_do.Trim(), linia.nr_linii);
                return 0;
            }
            else //rozdziel
            {

                var nlinia = Helper.Clone(linia);

                nlinia.nr_linii++;
                nlinia.ilosc_zamowiona = linia.ilosc_zamowiona - ilosc;
                nlinia.ilosc_otwarta = nlinia.ilosc_zamowiona;
                nlinia.lokalizacja_z = "";

                db.zlecenia_szczegoly.InsertOnSubmit(nlinia);
                Logger(user_name, nlinia.litm, nlinia.ilosc_zamowiona.ToString() + " " + nlinia.JM,
                    linia.id_zlecenia, "ROZDZIAŁ-NOWA LINIA", nlinia.magazyn_z.Trim() + "=>" + nlinia.magazyn_do.Trim(), nlinia.lokalizacja_do.Trim(), nlinia.nr_linii);


                linia.magazyn_z = mag;
                linia.lokalizacja_z = lok;
                linia.ilosc_otwarta = 0;
                linia.ilosc_zamowiona = ilosc;
                linia.ilosc_zrealizowana = ilosc;
                linia.status_linii = "spakowane                     ";
                linia.data_ost_oper = DateTime.Now;
                linia.autor_ost_oper = Userid(user_name);
                db.SubmitChanges();

                Logger(user_name, linia.litm, linia.ilosc_zrealizowana.ToString() + " " + linia.JM,
                    linia.id_zlecenia, "SPAKOWANO LINIĘ", linia.magazyn_z.Trim() + "=>" + linia.magazyn_do.Trim(), linia.lokalizacja_z.Trim() + "=>" + linia.lokalizacja_do.Trim(), linia.nr_linii);


                return nlinia.nr_linii;
            }

        }



        [WebMethod]
        public string Zdejmij_blokade_kompletacji(int id_kompletacji, string user_name)
        {
            var db = new dbtrans1DataContext();
            var blok = from c in db.transfer_blokada_nagls
                       where c.id_zlecenia_transf == id_kompletacji && c.uzytkownik == user_name
                       select c;

            db.transfer_blokada_nagls.DeleteAllOnSubmit(blok);
            db.SubmitChanges();
            //sprawdz czy są jeszcze jakies blokady
            var eblok = from c in db.transfer_blokada_nagls
                        where c.id_zlecenia_transf == id_kompletacji
                        select c;

            if (eblok.Count() == 0) //jezeli brak blokad, to zmień status
            {



                db.SubmitChanges();

            }
            Ustaw_status_zlecenia(id_kompletacji, this.WezDaneUsera(user_name).Id_usera);
            return "OK";
        }
        public static T Clone<T>(T source)
        {
            var dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(T));
            using (var ms = new System.IO.MemoryStream())
            {
                dcs.WriteObject(ms, source);
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                return (T)dcs.ReadObject(ms);
            }
        }
        [WebMethod]
        public string Dekompletuj(int nr_zlec, int id_usera)
        {

            var db = new dbtrans1DataContext();
            var do_dekompletacji = from c in db.zlecenia_szczegoly
                                   where c.ilosc_zrealizowana != 0 && c.id_zlecenia == nr_zlec && (c.Status_ksiegowania ?? "x") != "P" 
                                   select c;


            foreach (var rec in do_dekompletacji.Where(x => !x.Opis_statusu.Contains("dekompletacja")))
            {

                var nrec = Clone(rec);

                if (nrec.status_linii.Trim().ToLower() == "zamknięte" || nrec.status_linii.Trim().ToLower() == "zakończone")
                {
                    nrec.Opis_statusu = ": OK:DEKOMPLETACJA: " + rec.status_linii.Trim();
                    nrec.status_linii = "spakowane                    ";
                    nrec.magazyn_do = rec.magazyn_z; // zamień
                    nrec.magazyn_z = rec.magazyn_do;
                    nrec.lokalizacja_do = rec.lokalizacja_z;
                    nrec.lokalizacja_z = rec.lokalizacja_do;
                    nrec.nr_linii++;
                    nrec.autor_ost_oper = id_usera;
                    nrec.autor_zlecenia = id_usera;
                    nrec.data_utworzenia = DateTime.Now;
                    nrec.Weryfikacja = "";
                    nrec.Status_ksiegowania = "";

                    rec.Opis_statusu = ": OK:dekompletacja " + DateTime.Now + " przez: " + id_usera;

                    db.zlecenia_szczegoly.InsertOnSubmit(nrec);
                    db.SubmitChanges();

                }
            }


            return "0";
        }


        [WebMethod]
        public int PoprawStatusyZbiorczo(int okres)
        {
            var db = new dbtrans1DataContext();
            var id = (db.zlecenia_naglowkis.Where(x => x.Data_utworzenia.Year * 100 + x.Data_utworzenia.Month == okres)).ToList();


            foreach (var nr in id)
            {

                Ustaw_status_zlecenia(nr.Nr_zlecenia, nr.Autor_ost_mod);

            }


            return id.Count;
        }

        [WebMethod]
        public string Rozpocznij_kompletacje(int id_kompletacji, string user_name)
        {
            var db = new dbtrans1DataContext();

            transfer_blokada_nagl blok = new transfer_blokada_nagl();
            blok.id_zlecenia_transf = id_kompletacji;
            blok.poczatek = DateTime.Now;
            blok.uzytkownik = user_name;
            db.transfer_blokada_nagls.InsertOnSubmit(blok);

            var zlec = (from c in db.zlecenia_naglowkis
                        where c.Nr_zlecenia == id_kompletacji
                        select c).First();

            zlec.Status = "w trakcie kompletacji         ";


            db.SubmitChanges();


            return "OK";
        }

        [WebMethod]
        public List<string> Lokalizace_docelowe_oczekujace()
        {

            dbtrans1DataContext db = new dbtrans1DataContext();
            var trans = (from c in db.zlecenia_naglowek_views
                         where c.Typ == "kompletacja" && c.Status.ToLower() == "oczekuje"
                         select c.lok_docelowa).Distinct();

            return trans.ToList();


        }


        [WebMethod]
        public List<string> Aktualne_zlecenia_transferowe_kto_utworzyl()
        {
            List<string> zlecenia_aktywne = new List<string>() { "Oczekuje", "oczekuje", "w trakcie kompletacji", "zablokowane" };
            dbtrans1DataContext db = new dbtrans1DataContext();
            var trans = (from c in db.zlecenia_naglowek_views
                        where zlecenia_aktywne.Contains(c.Status.Trim().ToLower()) && c.Typ == "Kompletacja                   "
                        


                        select new { UTWORZYŁ = c.login.ToString() }).Distinct().ToList();
            List<string> lista = new List<string>();
            foreach (var l in trans)
            {
                lista.Add(l.UTWORZYŁ);

            }

            return lista;

        }

        [WebMethod]
        public DataTable Aktualne_zlecenia_transferowe(string user_name)
        {
            //TODO: sprawdzanie uprawnień i stanów - jeżeli nie ma, to nie pokazuje...

            List<string> zlecenia_aktywne = new List<string>() { "Oczekuje", "oczekuje", "w trakcie kompletacji", "zablokowane" };
            DataTable dt = new DataTable();
            dbtrans1DataContext db = new dbtrans1DataContext();
            var trans = from c in db.zlecenia_naglowek_views
                        where zlecenia_aktywne.Contains(c.Status.Trim().ToLower()) && c.Typ == "Kompletacja                   "
                        orderby c.BER descending, c.data_wymagana descending


                        select new { BER = (bool)c.BER ? "TAK" : "NIE", c.Nr_zlecenia, c.Status, c.Opis, UTWORZYŁ = c.login.ToString(), c.LITM, c.data_wymagana, c.Data_utworzenia, CEL = c.lok_docelowa };

            dt = LINQToDataTable(trans);
            dt.TableName = "zlecenia_transferowe";

            return dt;
        }
        /// <summary>
        /// POBIERA LINIE ZAMÓWIENIA DO SKOMPLETOWANIA
        /// </summary>
        /// <param name="nr_zlecenia">NR transferu/kompletacji</param>
        /// <returns></returns>
        [WebMethod]
        public List<zlecenia_szczegoly> Linie_zamowienia(int nr_zlecenia)
        {
            List<zlecenia_szczegoly> lst = new List<zlecenia_szczegoly>();

            var db = new dbtrans1DataContext();
            var linie = from c in db.zlecenia_szczegoly
                        where c.id_zlecenia == nr_zlecenia &&  (c.Status_ksiegowania ?? "") != "K" && (c.Status_ksiegowania ?? "") != "P"
                        select c;
            lst = linie.ToList<zlecenia_szczegoly>();

            return lst;
        }
        [WebMethod]
        public List<Zlecenia_szczegoly_SL> Linie_zamowienia_szczegoly(int nr_zlecenia)
        {
            List<Zlecenia_szczegoly_SL> lst = new List<Zlecenia_szczegoly_SL>();

            var db = new dbtrans1DataContext();
            var linie = from c in db.Zlecenia_szczegoly_SLs
                        where c.id_zlecenia == nr_zlecenia && (c.Status_ksiegowania ?? "") != "K" && (c.Status_ksiegowania ?? "") != "P"
                        select c;
            lst = linie.ToList<Zlecenia_szczegoly_SL>();

            return lst;
        }

        [WebMethod]
        public List<string> Ostatnia_wersja()
        {
            List<string> v = new List<string>();
            dbtrans1DataContext db = new dbtrans1DataContext();
            var wersja = (from c in db.Wersja_aplikacjis
                          orderby c.id descending
                          select c).First();

            v.Add(wersja.Wersja);
            v.Add(wersja.Opis);

            return v;
        }


        [WebMethod]
        public List<Zlecenia_szczegoly_SL> Linie_zamowienia_szczegoly_filtr(int nr_zlecenia, string UserName, string filtr)
        {
            List<Zlecenia_szczegoly_SL> lst = new List<Zlecenia_szczegoly_SL>();



            var db = new dbtrans1DataContext();
            var linie = (from c in db.Zlecenia_szczegoly_SLs
                         where c.id_zlecenia == nr_zlecenia && c.ilosc_otwarta != 0 && (c.Status_ksiegowania ?? "" )!= "K" && (c.Status_ksiegowania ?? "" ) != "P"
                         select c).ToList();

            //przefiltruj linie
            if (!string.IsNullOrEmpty(filtr))
            {
                List<string> indeksy = new List<string>();
                indeksy = (from c in linie select c.litm.Trim()).Distinct().ToList();
                var stany_mag = this.StanMagazynowy_JDE(indeksy);



                List<string> usun_kod = new List<string>();


                foreach (var l in linie)
                {
                    bool usun = true;
                    var stany = stany_mag.Where(x => x.LITM.Trim().ToLower() == l.litm.Trim().ToLower());
                    foreach (var stan in stany)
                    {
                        if (stan.LOK.ToLower().Contains(filtr.ToLower())) usun = false;
                    }
                    if (usun) usun_kod.Add(l.litm.Trim());

                }
                linie = (from c in linie
                         where !usun_kod.Contains(c.litm.Trim())
                         select c).ToList();

            }

            lst = linie.ToList<Zlecenia_szczegoly_SL>();
            return lst;
        }


        [WebMethod]
        public string Kto_kompletuje(int nr_zlecenia)
        {
            var db = new dbtrans1DataContext();
            var lista = (from c in db.transfer_blokada_nagls
                         where c.id_zlecenia_transf == nr_zlecenia
                         select c.uzytkownik).Distinct();
            StringBuilder str = new StringBuilder();

            foreach (var l in lista)
            {
                str.AppendLine(l);

            }

            return str.ToString();
        }

        [WebMethod]
        public List<uzytkownicy> Pobierz_liste_uzytkownikow(bool wszyscy)
        {
            var tr = new dbtrans1DataContext();


            if (wszyscy)
            {
                var lista = from c in tr.uzytkownicies
                            select c;

                return lista.ToList();
            }

            else
            {
                var lista = from c in tr.uzytkownicies where c.Aktywny == true
                            select c;

                return lista.ToList();
            }


        }

        [WebMethod]
        public string JDE_Zaksięguj_przesuniecie(string srodowisko, string token, string indeks_litm, string JM,
            string magazyn_z, string lokalizacja_z,
            string magazyn_do, string lokalizacja_do,
            string wersja_dok, double ilosc, string opis_transakcji)
        {

            if (opis_transakcji.Length > 30) opis_transakcji = opis_transakcji.Substring(0, 29);
            if (magazyn_z.Trim() == "PROD" && magazyn_do.Trim() == "PROD") wersja_dok = "MT";
            if (magazyn_z.Trim() == "62" && magazyn_do.Trim() == "PROD") wersja_dok = "MW";
            if (magazyn_z.Trim() == "61" && magazyn_do.Trim() == "PROD") wersja_dok = "MW";
            if (magazyn_z.Trim() == "62" && magazyn_do.Trim() == "62") wersja_dok = "MI";
            if (magazyn_z.Trim() == "61" && magazyn_do.Trim() == "61") wersja_dok = "MI";
            if (magazyn_z.Trim() == "PROD" && magazyn_do.Trim() == "62") wersja_dok = "ME";
            if (magazyn_z.Trim() == "PROD" && magazyn_do.Trim() == "61") wersja_dok = "ME";
            if (magazyn_z.Trim() == "MWG" && magazyn_do.Trim() == "PROD") wersja_dok = "MA";
            if (magazyn_z.Trim() == "PROD" && magazyn_do.Trim() == "MWG") wersja_dok = "M4";
            if (magazyn_z.Trim() == "MWG" && magazyn_do.Trim() == "MWG") wersja_dok = "MU";

            if (magazyn_z.Trim() == "PROD" && magazyn_do.Trim().StartsWith("IZ")) wersja_dok = "HZ";
            if (magazyn_z.Trim().StartsWith("IZ") && magazyn_do.Trim() == "PROD") wersja_dok = "HE";
            if (magazyn_z.Trim().StartsWith("6") && magazyn_do.Trim().StartsWith("IZ")) wersja_dok = "błąd";
            if (magazyn_z.Trim().StartsWith("IZ") && magazyn_do.Trim().StartsWith("6")) wersja_dok = "błąd";
            if (magazyn_z.Trim().StartsWith("IZ") && magazyn_do.Trim().StartsWith("IZ")) wersja_dok = "HI";


            //popraw lokalizację jezeli zawiera spacje i duze/male litery

            try
            {
                var db = new DBDataContext();
                var lok_z = (from c in db.IPO_magazyny_IPO2JDE
                             where c.LILOCN.Trim().ToLower() == lokalizacja_z.Trim().ToLower()
                             select c).First();
                lokalizacja_z = lok_z.LILOCN;

                var nlokdo = lokalizacja_do.Trim().ToUpper();

                var lok_do = from c in db.IPO_magazyny_IPO2JDE
                             where c.LILOCN.Trim().ToUpper() == nlokdo
                             select c;
                lokalizacja_do = lok_do.First().LILOCN;

                var _litm = (from c in db.SLOWNIK_1s
                             where c.IMLITM.Trim().ToLower() == indeks_litm.Trim().ToLower() select c).FirstOrDefault();
                indeks_litm = _litm.IMLITM;

            }
            catch (Exception ex) { var test = ex.Message; }


            JDE_REF.Logic2Client client = new JDE_REF.Logic2Client("BasicHttpBinding_ILogic2", "http://192.168.1.108:8731/XELCODE.Server.Valvex/Logic");

            JDE_REF.ROSessionData sd = new JDE_REF.ROSessionData();
            sd.environment = srodowisko;
            sd.userId = "PSFT";
            sd.password = "valvex01";
            sd.warehouse = "PROD";

            JDE_REF.ROInventoryTransferVI tr = new JDE_REF.ROInventoryTransferVI();
            tr.itemNumber2 = indeks_litm;
            tr.locationFrom = lokalizacja_z;
            tr.locationTo = lokalizacja_do;
            tr.quantity = ilosc;
            tr.warehouseFrom = magazyn_z;
            tr.warehouseTo = magazyn_do;
            tr.um = JM;
            tr.reasonCode = "";
            tr.transactionDate = DateTime.Now;
            tr.transactionExplanation = opis_transakcji;


            List<JDE_REF.ROInventoryTransferVI> trl = new List<JDE_REF.ROInventoryTransferVI>();
            trl.Add(tr);

            try
            {

                logger.Info("1Przes " + wersja_dok + ": " + indeks_litm);
                logger.Info("2Przes F: mag: '" + tr.warehouseFrom + "', lok: '" + tr.locationFrom + "'");
                logger.Info("3Przes T: mag: '" + tr.warehouseTo + "', lok: '" + tr.locationTo + "'");
                logger.Info("4Przes Ilosc: '" + tr.quantity.ToString());
                logger.Info("5Przes kom: '" + tr.transactionExplanation);
                logger.Info("6Przes token: '" + token + ", srod: " + srodowisko);
                if (token == "cdaa") client.RO_VI_InventoryTransfer(sd, wersja_dok, trl.ToArray());
                else throw new Exception("Zły token!");

            }
            catch (Exception ex)
            {

                logger.Info("7Bład: " + ex.Message);
                return ex.Message;


            }

            var db2008 = new DBDataContext();
            var item = (from c in db2008.SLOWNIK_1s
                        where c.IMLITM.Trim().ToUpper() == indeks_litm.Trim().ToUpper()
                        select c).FirstOrDefault();



            try

            {
                //kolejka IPO do aktualizacji


                //IPO_to_update ipo = new IPO_to_update();
                //ipo.GUID = Guid.NewGuid();
                //ipo.ITM = (int)item.IMITM;
                //ipo.LITM = indeks_litm;
                //ipo.LOCN = "PRZESUNIECIE";
                //ipo.MCU = "";
                //ipo.QTY = 0;
                //db2008.IPO_to_update.InsertOnSubmit(ipo);
                //db2008.SubmitChanges();
            }
            catch { }


            return "OK";



        }

        [WebMethod]
        public void GAL_dodaj_transfery()
        {
            var db2 = new DBDataContext();
            var db = new dbKartyProdukcjiDataContext();
            var lista = (from c in db.KJ_szlifiernia_potwierdzones
                         where c.Przesuniecie == "NA GAL: NIE_PRZESUNIĘTO"
                         select c).ToList();

            foreach (var l in lista)
            {
                var item = (from c in db2.SLOWNIK_1s
                            where c.IMLITM.Trim() == l.Kod_detalu.Trim()
                            select c).FirstOrDefault();

                GAL_Utwórz_transfer_ze_szlif(l.Kod_detalu.Trim(), (int)item.IMITM, item.IMUOM1, item.NAZWA, (double)l.Ilosc_ok, "PRZESUNIECIE");



            }


        }
        [WebMethod]
        public DataTable GAL_lista_indeksow_wg_rys(int itm)
        {
            DataTable dt = new DataTable();
            dt.TableName = "lst";
            try
            {
                DBDataContext db = new DBDataContext();
                var rys = (from c in db.SLOWNIK_1s where c.IMITM == itm select c.NR_RYS).FirstOrDefault();

                var lista = from c in db.SLOWNIK_1s where c.NR_RYS == rys && (c.KOD_PLAN == "BRAK" || c.KOD_PLAN == "OBRÓBKA") select new {STAT=c.IMSHCN,Indeks=c.IMLITM, c.NAZWA, c.KOD_PLAN };

                dt = LINQToDataTable(lista);
                dt.TableName = "lst";
                



            }
            catch { }
            
            return dt;



        
        }

        //pobiera transfery na GAL-ODCIAGANIE, które są jeszcze nie potwierdzone
        [WebMethod]
        public DataTable GAL_lista_od_potwierdzenia_id(int user_id)
        {
            dbtrans1DataContext db = new dbtrans1DataContext();
            var db2 = new DBDataContext();
            var lista = from c in db.Zlecenia_szczegoly_SLs
                        where c.lokalizacja_do == "GAL-ODCIAGANIE      " && c.status_linii == "spakowane" && (c.Status_ksiegowania ?? "") == "" && c.autor_zlecenia == user_id
                        orderby c.data_utworzenia descending
                        select new { c.litm, c.ilosc_otwarta, c.opis, lokalizacja_z = (c.magazyn_z.Trim() + '_' + c.lokalizacja_z.Trim()), c.data_utworzenia, c.autor_zlecenia, c.id };

            DataTable dt = new DataTable();

            dt = LINQToDataTable(lista);
            dt.TableName = "DO_ODC";
            return dt;
        }



        //pobiera transfery na GAL-ODCIAGANIE, które są jeszcze nie potwierdzone
        [WebMethod]
        public DataTable GAL_lista_od_potwierdzenia()
        {
            dbtrans1DataContext db = new dbtrans1DataContext();
            var db2 = new DBDataContext();
            var lista = from c in db.Zlecenia_szczegoly_SLs
                        where c.lokalizacja_do == "GAL-ODCIAGANIE      " && c.status_linii == "spakowane" && (c.Status_ksiegowania ?? "") == ""
                        orderby c.data_utworzenia descending
                        select new { c.litm, c.ilosc_otwarta, c.opis, lokalizacja_z =( c.magazyn_z.Trim() + '_' + c.lokalizacja_z.Trim()), c.data_utworzenia, c.autor_zlecenia,c.id };

            DataTable dt = new DataTable();
           
            dt = LINQToDataTable(lista);
            dt.TableName = "DO_ODC";
            return dt;
        }

        [WebMethod]
        public string GAL_Utwórz_transfer_na_GAL_ODC(string LITM,  string mag_z, string loc_z , string JM,   int Ilosc, string komentarz, int id_usera)
        {


            int ITM; string Nazwa;

            bool nowy_transfer = false;
            dbtrans1DataContext db = new dbtrans1DataContext();
            var db2 = new DBDataContext();
             
                var item = (from c in db2.SLOWNIK_1s
                            where c.IMLITM.Trim() == LITM.Trim()
                            select c).FirstOrDefault();
                ITM = (int)item.IMITM;
                Nazwa = item.NAZWA;

            

            //sprawdz czy dzisiaj transfer jest juz założony
            var tr = from c in db.zlecenia_naglowkis
                     where c.Data_utworzenia.Date == DateTime.Now.Date && c.Opis.StartsWith("3GAL/")
                     select c;

            if (tr.Count() == 0)
            {

                nowy_transfer = true;
                //nie ma zlecenia na dzisiaj - załóż je
                zlecenia_naglowki nzlec = new zlecenia_naglowki();

                nzlec.Autor_ost_mod = 0;
                nzlec.BER = false;
                nzlec.Data_ost_mod = DateTime.Now;
                nzlec.Data_utworzenia = DateTime.Now;
                nzlec.data_wymagana = DateTime.Now;
                nzlec.LITM = "3GAL_ODC";
                nzlec.odpowiedzialny = 0;
                nzlec.Opis = "3GAL/Transfer na odciaganie " + DateTime.Now.Date.ToString();
                nzlec.Typ = "Transfer";
                nzlec.Utworzony_przez = id_usera;
                nzlec.Status = "oczekuje";
                db.zlecenia_naglowkis.InsertOnSubmit(nzlec);
                db.SubmitChanges();

                tr = from c in db.zlecenia_naglowkis
                     where c.Data_utworzenia.Date == DateTime.Now.Date && c.Opis.StartsWith("3GAL/")
                     select c;


            }

            zlecenia_szczegoly nl = new zlecenia_szczegoly();
            nl.autor_ost_oper = id_usera;
            nl.autor_zlecenia = id_usera;
            nl.data_ost_oper = DateTime.Now;
            nl.data_utworzenia = DateTime.Now;
            nl.id_zlecenia = tr.First().Nr_zlecenia;
            nl.ilosc_otwarta = Ilosc;
            nl.ilosc_zamowiona = Ilosc;
            nl.ilosc_zrealizowana = 0;
            nl.itm = ITM;
            nl.JM = JM;
            nl.litm = LITM;
            nl.lokalizacja_do = "GAL-ODCIAGANIE";
            nl.lokalizacja_z = loc_z;
            nl.magazyn_do = "PROD";
            nl.magazyn_z = mag_z;
            nl.opis = Nazwa;
            nl.Opis_statusu = komentarz;
            nl.status_linii = "spakowane";

            if (nowy_transfer) { nl.nr_linii = 100; }
            else
            {
                try
                {
                    var nr = (from c in db.zlecenia_szczegoly
                              where c.id_zlecenia == nl.id_zlecenia
                              select c.nr_linii).Max();

                    nl.nr_linii = nr + 100;
                }
                //pierwsza linia sie nie załozyła - wywala bład i wtedy przypisujemy ręcznie....
                catch { nl.nr_linii = 100; }
            }
            db.zlecenia_szczegoly.InsertOnSubmit(nl);
            db.SubmitChanges();
            Ustaw_status_zlecenia(nl.id_zlecenia, 2);
            return nl.id_zlecenia.ToString();



             
        }

        [WebMethod]
        public string GAL_Utwórz_transfer_ze_szlif_na_buf(string LITM, int ITM, string JM, string Nazwa, double Ilosc, string komentarz)

        {
            bool nowy_transfer = false;
            dbtrans1DataContext db = new dbtrans1DataContext();
            //sprawdz czy dzisiaj transfer jest juz założony
            var tr = from c in db.zlecenia_naglowkis
                     where c.Data_utworzenia.Date == DateTime.Now.Date && c.Opis.StartsWith("2GAL/")
                     select c;

            if (tr.Count() == 0)
            {

                nowy_transfer = true;
                //nie ma zlecenia na dzisiaj - załóż je
                zlecenia_naglowki nzlec = new zlecenia_naglowki();

                nzlec.Autor_ost_mod = 0;
                nzlec.BER = false;
                nzlec.Data_ost_mod = DateTime.Now;
                nzlec.Data_utworzenia = DateTime.Now;
                nzlec.data_wymagana = DateTime.Now;
                nzlec.LITM = "SZLIF2GAL";
                nzlec.odpowiedzialny = 0;
                nzlec.Opis = "2GAL/Transfer na galwanizernię " + DateTime.Now.Date.ToString();
                nzlec.Typ = "Transfer";
                nzlec.Utworzony_przez = 2;
                nzlec.Status = "oczekuje";
                db.zlecenia_naglowkis.InsertOnSubmit(nzlec);
                db.SubmitChanges();

                tr = from c in db.zlecenia_naglowkis
                     where c.Data_utworzenia.Date == DateTime.Now.Date && c.Opis.StartsWith("2GAL/")
                     select c;


            }

            zlecenia_szczegoly nl = new zlecenia_szczegoly();
            nl.autor_ost_oper = 2;
            nl.autor_zlecenia = 2;
            nl.data_ost_oper = DateTime.Now;
            nl.data_utworzenia = DateTime.Now;
            nl.id_zlecenia = tr.First().Nr_zlecenia;
            nl.ilosc_otwarta = Ilosc;
            nl.ilosc_zamowiona = Ilosc;
            nl.ilosc_zrealizowana = 0;
            nl.itm = ITM;
            nl.JM = JM;
            nl.litm = LITM;
            nl.lokalizacja_do = "P -5 GALWNIZERNIA   ";
            nl.lokalizacja_z = "GALW-BUFOR          ";
            nl.magazyn_do = "PROD";
            nl.magazyn_z = "PROD";
            nl.opis = Nazwa;
            nl.Opis_statusu = komentarz;
            nl.status_linii = "spakowane";

            if (nowy_transfer) { nl.nr_linii = 100; }
            else
            {
                try
                {
                    var nr = (from c in db.zlecenia_szczegoly
                              where c.id_zlecenia == nl.id_zlecenia
                              select c.nr_linii).Max();

                    nl.nr_linii = nr + 100;
                }
                //pierwsza linia sie nie załozyła - wywala bład i wtedy przypisujemy ręcznie....
                catch { nl.nr_linii = 100; }
            }
            db.zlecenia_szczegoly.InsertOnSubmit(nl);
            db.SubmitChanges();
            Ustaw_status_zlecenia(nl.id_zlecenia, 2);
            return nl.id_zlecenia.ToString();

        }

        [WebMethod]
        public string MWG_Utwórz_transfer_ze_H7_na_MWG(string LITM, string JM, double Ilosc, string komentarz)
        {

            var dbs = new DBDataContext();
            var item = dbs.SLOWNIK_1s.Where(x => x.IMLITM.Trim().ToLower() == LITM.Trim().ToLower()).FirstOrDefault();


            bool nowy_transfer = false;
            dbtrans1DataContext db = new dbtrans1DataContext();
            //sprawdz czy dzisiaj transfer jest juz założony
            var tr = from c in db.zlecenia_naglowkis
                     where c.Data_utworzenia.Date == DateTime.Now.Date && c.Opis.StartsWith("2MWG/")
                     select c;

            if (tr.Count() == 0)
            {

                nowy_transfer = true;
                //nie ma zlecenia na dzisiaj - załóż je
                zlecenia_naglowki nzlec = new zlecenia_naglowki();

                nzlec.Autor_ost_mod = 0;
                nzlec.BER = false;
                nzlec.Data_ost_mod = DateTime.Now;
                nzlec.Data_utworzenia = DateTime.Now;
                nzlec.data_wymagana = DateTime.Now;
                nzlec.LITM = "H7->MWG";
                nzlec.odpowiedzialny = 35;
                nzlec.Opis = "2MWG/Transfer na MWG " + DateTime.Now.Date.ToString();
                nzlec.Typ = "Transfer";
                nzlec.Utworzony_przez = 2;
                nzlec.Status = "oczekuje";
                db.zlecenia_naglowkis.InsertOnSubmit(nzlec);
                db.SubmitChanges();

                tr = from c in db.zlecenia_naglowkis
                     where c.Data_utworzenia.Date == DateTime.Now.Date && c.Opis.StartsWith("2MWG/")
                     select c;


            }

            zlecenia_szczegoly nl = new zlecenia_szczegoly();
            nl.autor_ost_oper = 2;
            nl.autor_zlecenia = 6;
            nl.data_ost_oper = DateTime.Now;
            nl.data_utworzenia = DateTime.Now;
            nl.id_zlecenia = tr.First().Nr_zlecenia;
            nl.ilosc_otwarta = Ilosc;
            nl.ilosc_zamowiona = Ilosc;
            nl.ilosc_zrealizowana = 0;
            nl.itm = (int)item.IMITM;
            nl.JM = JM;
            nl.litm = LITM;
            nl.lokalizacja_do = "0";
            nl.lokalizacja_z = "H7PROD              ";
            nl.magazyn_do = "MWG";
            nl.magazyn_z = "PROD";
            nl.opis = item.NAZWA;
            nl.Opis_statusu = komentarz;
            nl.status_linii = "spakowane";

            if (nowy_transfer) { nl.nr_linii = 100; }
            else
            {
                try
                {
                    var nr = (from c in db.zlecenia_szczegoly
                              where c.id_zlecenia == nl.id_zlecenia
                              select c.nr_linii).Max();

                    nl.nr_linii = nr + 100;
                }
                //pierwsza linia sie nie załozyła - wywala bład i wtedy przypisujemy ręcznie....
                catch { nl.nr_linii = 100; }
            }
            db.zlecenia_szczegoly.InsertOnSubmit(nl);
            db.SubmitChanges();
            Ustaw_status_zlecenia(nl.id_zlecenia, 2);
            return nl.id_zlecenia.ToString();

        }




        [WebMethod]
        public string GAL_Utwórz_transfer_ze_szlif(string LITM, int ITM, string JM, string Nazwa, double Ilosc, string komentarz)
        {
            bool nowy_transfer = false;
            dbtrans1DataContext db = new dbtrans1DataContext();
            //sprawdz czy dzisiaj transfer jest juz założony
            var tr = from c in db.zlecenia_naglowkis
                     where c.Data_utworzenia.Date == DateTime.Now.Date && c.Opis.StartsWith("2GAL/")
                     select c;

            if (tr.Count() == 0)
            {

                nowy_transfer = true;
                //nie ma zlecenia na dzisiaj - załóż je
                zlecenia_naglowki nzlec = new zlecenia_naglowki();

                nzlec.Autor_ost_mod = 0;
                nzlec.BER = false;
                nzlec.Data_ost_mod = DateTime.Now;
                nzlec.Data_utworzenia = DateTime.Now;
                nzlec.data_wymagana = DateTime.Now;
                nzlec.LITM = "SZLIF2GAL";
                nzlec.odpowiedzialny = 0;
                nzlec.Opis = "2GAL/Transfer na galwanizernię " + DateTime.Now.Date.ToString();
                nzlec.Typ = "Transfer";
                nzlec.Utworzony_przez = 2;
                nzlec.Status = "oczekuje";
                db.zlecenia_naglowkis.InsertOnSubmit(nzlec);
                db.SubmitChanges();

                tr = from c in db.zlecenia_naglowkis
                     where c.Data_utworzenia.Date == DateTime.Now.Date && c.Opis.StartsWith("2GAL/")
                     select c;


            }

            zlecenia_szczegoly nl = new zlecenia_szczegoly();
            nl.autor_ost_oper = 2;
            nl.autor_zlecenia = 2;
            nl.data_ost_oper = DateTime.Now;
            nl.data_utworzenia = DateTime.Now;
            nl.id_zlecenia = tr.First().Nr_zlecenia;
            nl.ilosc_otwarta = Ilosc;
            nl.ilosc_zamowiona = Ilosc;
            nl.ilosc_zrealizowana = 0;
            nl.itm = ITM;
            nl.JM = JM;
            nl.litm = LITM;
            nl.lokalizacja_do = "P -5 GALWNIZERNIA   ";
            nl.lokalizacja_z = "P -31 SZLIF - POL";
            nl.magazyn_do = "PROD";
            nl.magazyn_z = "PROD";
            nl.opis = Nazwa;
            nl.Opis_statusu = komentarz;
            nl.status_linii = "spakowane";

            if (nowy_transfer) { nl.nr_linii = 100; } else
            {
                try
                {
                    var nr = (from c in db.zlecenia_szczegoly
                              where c.id_zlecenia == nl.id_zlecenia
                              select c.nr_linii).Max();

                    nl.nr_linii = nr + 100;
                }
                //pierwsza linia sie nie załozyła - wywala bład i wtedy przypisujemy ręcznie....
                catch { nl.nr_linii = 100; }
            }
            db.zlecenia_szczegoly.InsertOnSubmit(nl);
            db.SubmitChanges();
            Ustaw_status_zlecenia(nl.id_zlecenia, 2);
            return nl.id_zlecenia.ToString();

        }



        [WebMethod]
        public string SUP_Utwórz_transfer_z_GALW(string LITM, int ITM, string JM, string Nazwa, double Ilosc, string komentarz)
        {
            bool nowy_transfer = false;
            dbtrans1DataContext db = new dbtrans1DataContext();
            //sprawdz czy dzisiaj transfer jest juz założony
            var tr = from c in db.zlecenia_naglowkis
                     where c.Data_utworzenia.Date == DateTime.Now.Date && c.Opis.StartsWith("GAL2SM")
                     select c;

            if (tr.Count() == 0)
            {

                nowy_transfer = true;
                //nie ma zlecenia na dzisiaj - załóż je
                zlecenia_naglowki nzlec = new zlecenia_naglowki();

                nzlec.Autor_ost_mod = 0;
                nzlec.BER = false;
                nzlec.Data_ost_mod = DateTime.Now;
                nzlec.Data_utworzenia = DateTime.Now;
                nzlec.data_wymagana = DateTime.Now;
                nzlec.LITM = "GAL2SM";
                nzlec.odpowiedzialny = 0;
                nzlec.Opis = "GAL2SM/Zdawka Galwanizernia " + DateTime.Now.Date.ToString();
                nzlec.Typ = "Transfer";
                nzlec.Utworzony_przez = 2;
                nzlec.Status = "oczekuje";
                db.zlecenia_naglowkis.InsertOnSubmit(nzlec);
                db.SubmitChanges();

                tr = from c in db.zlecenia_naglowkis
                     where c.Data_utworzenia.Date == DateTime.Now.Date && c.Opis.StartsWith("GAL2SM")
                     select c;


            }

            zlecenia_szczegoly nl = new zlecenia_szczegoly();
            nl.autor_ost_oper = 2;
            nl.autor_zlecenia = 2;
            nl.data_ost_oper = DateTime.Now;
            nl.data_utworzenia = DateTime.Now;
            nl.id_zlecenia = tr.First().Nr_zlecenia;
            nl.ilosc_otwarta = Ilosc;
            nl.ilosc_zamowiona = Ilosc;
            nl.ilosc_zrealizowana = 0;
            nl.itm = ITM;
            nl.JM = JM;
            nl.litm = LITM;
            nl.lokalizacja_do = "SUPERMARKET3";
            nl.lokalizacja_z = "P -5 GALWNIZERNIA   ";
            nl.magazyn_do = "PROD";
            nl.magazyn_z = "PROD";
            nl.opis = Nazwa;
            nl.Opis_statusu = komentarz;
            nl.status_linii = "spakowane";

            if (nowy_transfer) { nl.nr_linii = 100; }
            else
            {
                try
                {
                    var nr = (from c in db.zlecenia_szczegoly
                              where c.id_zlecenia == nl.id_zlecenia
                              select c.nr_linii).Max();

                    nl.nr_linii = nr + 100;
                }
                //pierwsza linia sie nie załozyła - wywala bład i wtedy przypisujemy ręcznie....
                catch { nl.nr_linii = 100; }
            }
            db.zlecenia_szczegoly.InsertOnSubmit(nl);
            db.SubmitChanges();
            Ustaw_status_zlecenia(nl.id_zlecenia, 2);
            return nl.id_zlecenia.ToString();

        }


        [WebMethod]
        public string SUP_Utwórz_transfer_z_P31(string LITM, int ITM, string JM, string Nazwa, double Ilosc, string komentarz)
        {
            bool nowy_transfer = false;
            dbtrans1DataContext db = new dbtrans1DataContext();
            //sprawdz czy dzisiaj transfer jest juz założony
            var tr = from c in db.zlecenia_naglowkis
                     where c.Data_utworzenia.Date == DateTime.Now.Date && c.Opis.StartsWith("GAL2SM")
                     select c;

            if (tr.Count() == 0)
            {

                nowy_transfer = true;
                //nie ma zlecenia na dzisiaj - załóż je
                zlecenia_naglowki nzlec = new zlecenia_naglowki();

                nzlec.Autor_ost_mod = 0;
                nzlec.BER = false;
                nzlec.Data_ost_mod = DateTime.Now;
                nzlec.Data_utworzenia = DateTime.Now;
                nzlec.data_wymagana = DateTime.Now;
                nzlec.LITM = "GAL2SM";
                nzlec.odpowiedzialny = 0;
                nzlec.Opis = "GAL2SM/Zdawka Galwanizernia " + DateTime.Now.Date.ToString();
                nzlec.Typ = "Transfer";
                nzlec.Utworzony_przez = 2;
                nzlec.Status = "oczekuje";
                db.zlecenia_naglowkis.InsertOnSubmit(nzlec);
                db.SubmitChanges();

                tr = from c in db.zlecenia_naglowkis
                     where c.Data_utworzenia.Date == DateTime.Now.Date && c.Opis.StartsWith("GAL2SM")
                     select c;


            }

            zlecenia_szczegoly nl = new zlecenia_szczegoly();
            nl.autor_ost_oper = 2;
            nl.autor_zlecenia = 2;
            nl.data_ost_oper = DateTime.Now;
            nl.data_utworzenia = DateTime.Now;
            nl.id_zlecenia = tr.First().Nr_zlecenia;
            nl.ilosc_otwarta = Ilosc;
            nl.ilosc_zamowiona = Ilosc;
            nl.ilosc_zrealizowana = 0;
            nl.itm = ITM;
            nl.JM = JM;
            nl.litm = LITM;
            nl.lokalizacja_do = "SUPERMARKET3";
            nl.lokalizacja_z = "P -31 SZLIF - POL   ";
            nl.magazyn_do = "PROD";
            nl.magazyn_z = "PROD";
            nl.opis = Nazwa;
            nl.Opis_statusu = komentarz;
            nl.status_linii = "spakowane";

            if (nowy_transfer) { nl.nr_linii = 100; }
            else
            {
                try
                {
                    var nr = (from c in db.zlecenia_szczegoly
                              where c.id_zlecenia == nl.id_zlecenia
                              select c.nr_linii).Max();

                    nl.nr_linii = nr + 100;
                }
                //pierwsza linia sie nie załozyła - wywala bład i wtedy przypisujemy ręcznie....
                catch { nl.nr_linii = 100; }
            }
            db.zlecenia_szczegoly.InsertOnSubmit(nl);
            db.SubmitChanges();
            Ustaw_status_zlecenia(nl.id_zlecenia, 2);
            return nl.id_zlecenia.ToString();

        }

        [WebMethod]
        public List<zlecenia_szczegoly> Znajdz_otwarte_kompletacje(string LITM, string user_name)
        {

            dbtrans1DataContext tr = new dbtrans1DataContext();
            var zlec = from c in tr.zlecenia_szczegoly
                       where (c.status_linii == "spakowane" || c.status_linii == "oczekuje" ) && c.litm.Trim().ToLower() == LITM.Trim().ToLower()
                       select c;

            return zlec.ToList();
        }



        [WebMethod]
        public List<zlecenia_szczegoly> Znajdz_otwarte_transfery(string LITM, string user_name)
        {

            dbtrans1DataContext tr = new dbtrans1DataContext();
            var zlec = from c in tr.zlecenia_szczegoly
                       where c.status_linii.Trim().ToLower() == "spakowane" && (c.Status_ksiegowania ?? "") == "" &&  c.litm.Trim().ToLower() == LITM.Trim().ToLower()
                       select c;

            return zlec.ToList();
        }
        [WebMethod]
        public List<zlecenia_szczegoly> Znajdz_otwarte_transfery_na_lok(string lok, string user_name)
        {

            dbtrans1DataContext tr = new dbtrans1DataContext();
            var zlec = from c in tr.zlecenia_szczegoly
                       where c.status_linii.Trim().ToLower() == "spakowane" && (c.Status_ksiegowania ?? "") == "" &&  c.lokalizacja_do.Trim().ToLower() == lok.Trim().ToLower()
                       select c;

            return zlec.ToList();
        }


        [WebMethod]
        public List<string> Historia_indeksu(string magazyn, string LITM)
        {
            List<string> hist = new List<string>();
            var db = new DBDataContext();
            var histd = from c in db.V41021 where c.IMLITM == LITM && c.MAG == magazyn select c;
            foreach (var h in histd)
            {
                hist.Add($"{h.LOK} - {h.QTY.ToString()}");

            }

            return hist;

        }

        public class Stan_mag
        {
            public string mag { get; set; }
            public string lok { get; set; }
            public double qty { get; set; }
        }

        
        [WebMethod]
        public List<Stan_mag> Historia_stan_indeksu(string magazyn, string LITM)
        {
            List<Stan_mag> hist = new List<Stan_mag>();
            var db = new DBDataContext();
            var histd = from c in db.V41021 where c.IMLITM == LITM && c.MAG == magazyn select new { c.LOK, c.QTY };
            foreach (var h in histd)
            {
                Stan_mag st = new Stan_mag() { lok = h.LOK, mag = magazyn, qty = (double)h.QTY };
                hist.Add(st);
            }

            return hist;

        }






        [WebMethod]
        public string JDE_Zaksięguj_przesuniecie_wersja(string srodowisko, string token, string indeks_litm, string JM,
           string magazyn_z, string lokalizacja_z,
           string magazyn_do, string lokalizacja_do,
           string wersja_dok, double ilosc, string opis_transakcji)
        {


            JDE_REF.Logic2Client client = new JDE_REF.Logic2Client("BasicHttpBinding_ILogic2", "http://192.168.1.108:8731/XELCODE.Server.Valvex/Logic");

            JDE_REF.ROSessionData sd = new JDE_REF.ROSessionData();
            sd.environment = srodowisko;
            sd.userId = "PSFT";
            sd.password = "valvex01";
            sd.warehouse = "PROD";

            JDE_REF.ROInventoryTransferVI tr = new JDE_REF.ROInventoryTransferVI();
            tr.itemNumber2 = indeks_litm;
            tr.locationFrom = lokalizacja_z;
            tr.locationTo = lokalizacja_do;
            tr.quantity = ilosc;
            tr.warehouseFrom = magazyn_z;
            tr.warehouseTo = magazyn_do;
            tr.um = JM;
            tr.reasonCode = "";
            tr.transactionDate = DateTime.Now;
            tr.transactionExplanation = opis_transakcji;

            try
            {
                var db = new DBDataContext();
                var lok_z = (from c in db.IPO_magazyny_IPO2JDE
                             where c.LILOCN.Trim().ToLower() == lokalizacja_z.Trim().ToLower()
                             select c).FirstOrDefault();
                lokalizacja_z = lok_z.LILOCN;


                var lok_do = (from c in db.IPO_magazyny_IPO2JDE
                              where c.LILOCN.Trim().ToLower() == lokalizacja_do.Trim().ToLower()
                              select c).FirstOrDefault();
                lokalizacja_do = lok_do.LILOCN;



            }
            catch { }



            List<JDE_REF.ROInventoryTransferVI> trl = new List<JDE_REF.ROInventoryTransferVI>();
            trl.Add(tr);

            try
            {
                if (token == "cdaa") client.RO_VI_InventoryTransfer(sd, wersja_dok, trl.ToArray());
                else throw new Exception("Zły token!");
            }
            catch (Exception ex)
            {
                return ex.Message;


            }
            return "OK";



        }


        [WebMethod]
        public string JDE_Zaksięguj_dok_prosty(string srodowisko, string token, string indeks_litm, string JM, string magazyn, string lokalizacja, string wersja_dok, double ilosc, string opis_transakcji)
        {

            JDE_REF.Logic2Client client = new JDE_REF.Logic2Client("BasicHttpBinding_ILogic2", "http://192.168.1.108:8731/XELCODE.Server.Valvex/Logic");

            JDE_REF.ROSessionData sd = new JDE_REF.ROSessionData();
            sd.environment = srodowisko;
            sd.userId = "PSFT";
            sd.password = "valvex01";
            sd.warehouse = "PROD";

            JDE_REF.ROInventoryAdjustmentVI ad = new JDE_REF.ROInventoryAdjustmentVI();
            ad.warehouseTo = magazyn;
            ad.locationTo = lokalizacja.ToUpper();
            ad.lotNumber = "";
            ad.memoLot1 = "";
            ad.memoLot2 = "";

            ad.itemNumber2 = indeks_litm;
            ad.quantity = ilosc;
            ad.um = JM;
            ad.reasonCode = "";
            ad.transactionExplanation = opis_transakcji;
            ad.transactionDate = DateTime.Now;

            List<JDE_REF.ROInventoryAdjustmentVI> ial = new List<JDE_REF.ROInventoryAdjustmentVI>();
            ial.Add(ad);

            if ((wersja_dok == "IPOI3" && magazyn.Trim() != "PROD")) return ("NIE WOLNO KSIĘGOWAĆ I3 POZA PROD!!!");
            if ((wersja_dok == "IPOI2" && magazyn.Trim() != "PROD")) return ("NIE WOLNO KSIĘGOWAĆ I2 POZA PROD!!!");

            try
            {
                if (ilosc == 0) return "OK";
                if (token == "cdaa") client.RO_VI_InventoryAdjustment(sd, wersja_dok, ial.ToArray());
                else throw new Exception("Zły token!");
            }
            catch (Exception ex)
            {
                return ex.Message;


            }
            return "OK";
        }

        [WebMethod]
        public Vuser WezDaneUsera(string user_name)
        {
            dbtrans1DataContext db = new dbtrans1DataContext();
            var user = (from c in db.uzytkownicies
                        where c.login == user_name
                        select c).Single();

            Vuser u = new Vuser();

            u.aktywny = (bool)user.Aktywny;
            u.Imię = user.Imie;
            u.Nazwisko = user.Nazwisko;
            u.telefon = user.tel_kom;
            u.email = user.email;
            u.Id_usera = user.id;

            return u;

        }
        [WebMethod]
        public Vuser WezDaneUseraId(int user_id)
        {
            dbtrans1DataContext db = new dbtrans1DataContext();
            var user = (from c in db.uzytkownicies
                        where c.id == user_id
                        select c).Single();

            Vuser u = new Vuser();

            u.aktywny = (bool)user.Aktywny;
            u.Imię = user.Imie;
            u.Nazwisko = user.Nazwisko;
            u.telefon = user.tel_kom;
            u.email = user.email;
            u.Id_usera = user.id;

            return u;

        }
        [WebMethod]
        public string GraffBis_Poprawa_JM(string litm_z, string jm_z, string litm_do,string jm_do, double ilosc_z, double ilosc_do, string magazyn_z, string magazyn_do, string lok_z, string lok_do, string login)
        {

            //przesun na graff-bis - poprawa
            Service1 srv = new Service1();
            var token = GetUniqueKey(7);


           // var test = JDE_Zaksięguj_przesuniecie("PD810", "cdaa", litm_z, jm_z, magazyn_z, lok_z, "PROD", "GRAFF BIS - POPRAWA ", "", ilosc_z, "GraffBis/" + login + "/" + token);
            //zamień indeks na nowy za pomocą I2 i I3
            
           //tutaj // var test1 = JDE_Zaksięguj_dok_prosty("PD810", "cdaa", litm_z, jm_z, magazyn_z, lok_z, "IPOI2", -ilosc_z, "GraffBis/" + login + "/" + token);
          //tutaj //  var test2 = JDE_Zaksięguj_dok_prosty("PD810", "cdaa", litm_do, jm_do, magazyn_do, lok_do, "IPOI3", ilosc_do, "GraffBis/" + login + "/" + token);

            //przesun na lokalizację docelową
          //  var test3 = JDE_Zaksięguj_przesuniecie("PD810", "cdaa", litm_do, jm_do, "PROD", "GRAFF BIS - POPRAWA ", magazyn_do, lok_do, "", ilosc_do, "GraffBis/" + login + "/" + token);

            StringBuilder str = new StringBuilder();
            logger.Debug($"GraffBIS:{token} z: {litm_z} do: {litm_do} ilosc_z: {ilosc_z} mag_z {magazyn_z}_{lok_z} mag_do {magazyn_do}_{lok_do} login:{login}");
            str.AppendLine("TOKEN:" + token);
            str.AppendLine("INDEKS Z:" + litm_z);
            str.AppendLine("INDEKS DO:" + litm_do);

            //str.AppendLine("1 - " + test);
           // tutaj //str.AppendLine("I2 - " + test1);
           // tutaj  // str.AppendLine("I3 - " + test2);
            //str.AppendLine("4 - " + test3);
            logger.Debug($"GraffBIS:{token} z: {litm_z} do: {litm_do} ilosc_z: {ilosc_z} mag_z {magazyn_z}_{lok_z} mag_do {magazyn_do}_{lok_do} login:{login} wynik: {str.ToString()}");

            return "PROSZĘ DO ODWOŁANIA ROBIĆ AH W JDE ZAMIAST TEJ OPERACJI!!!"; //str.ToString();
        }


        [WebMethod]
        public string GraffBis_Poprawa(string litm_z, string litm_do, int ilosc, string magazyn_z, string magazyn_do, string lok_z, string lok_do, string login)
        {

            //przesun na graff-bis - poprawa
            Service1 srv = new Service1();
            var token = GetUniqueKey(7);


            var test = JDE_Zaksięguj_przesuniecie("PD810", "cdaa", litm_z, "SZ", magazyn_z, lok_z, "PROD", "GRAFF BIS - POPRAWA ", "", ilosc, "GraffBis/" + login + "/" + token);
            //zamień indeks na nowy za pomocą I2 i I3
            var test1 = JDE_Zaksięguj_dok_prosty("PD810", "cdaa", litm_z, "SZ", "PROD", "GRAFF BIS - POPRAWA ", "IPOI2", -ilosc, "GraffBis/" + login + "/" + token);
            var test2 = JDE_Zaksięguj_dok_prosty("PD810", "cdaa", litm_do, "SZ", "PROD", "GRAFF BIS - POPRAWA ", "IPOI3", ilosc, "GraffBis/" + login + "/" + token);

            //przesun na lokalizację docelową
            var test3 = JDE_Zaksięguj_przesuniecie("PD810", "cdaa", litm_do, "SZ", "PROD", "GRAFF BIS - POPRAWA ", magazyn_do, lok_do, "", ilosc, "GraffBis/" + login + "/" + token);

            StringBuilder str = new StringBuilder();
            str.AppendLine("TOKEN:" + token);
            str.AppendLine("INDEKS Z:" + litm_z);
            str.AppendLine("INDEKS DO:" + litm_do);

            str.AppendLine("1 - " + test);
            str.AppendLine("2 - " + test1);
            str.AppendLine("3 - " + test2);
            str.AppendLine("4 - " + test3);
            return str.ToString();
        }
        [WebMethod]
        public string Raportuj_golarki(int ilosc, string login)
        {
            var db = new dbtrans1DataContext();
            long NR_DOK = db.Next_nr_doco().First().Next_number;

            var test2 = JDE_Zaksięguj_dok_prosty("PD810", "cdaa", "9032830", "SZ", "PROD", "P -41 MONT.ARM.INST.", "IPOI3", ilosc, NR_DOK.ToString() + "[HG]");
            var db2 = new DBDataContext();

            var rozpis = from d in db2.IPO_Rozpis_mats
                         where d.wyrob_l == "9032830" && d.Typ_M == "M"
                         select d;

            foreach (var r in rozpis)
            {

                double nilosc = -ilosc * (double)r.ilosc;

                if (r.skladnik_l.Trim() == "9948885" ||
                    r.skladnik_l.Trim() == "9948354" ||
                    r.skladnik_l.Trim() == "9948352" ||
                    r.skladnik_l.Trim() == "9948353" ||
                    r.skladnik_l.Trim() == "9948242" ||
                    r.skladnik_l.Trim() == "9948320" ||
                    r.skladnik_l.Trim() == "9948371")
                {
                    var test3 = JDE_Zaksięguj_dok_prosty("PD810", "cdaa", r.skladnik_l, r.JM, "PROD", "P -41 MONT.ARM.INST.", "IPOI2", nilosc, NR_DOK.ToString() + "[HG]");
                }
                else
                {
                    var test1 = JDE_Zaksięguj_dok_prosty("PD810", "cdaa", r.skladnik_l, r.JM, "PROD", "P -31 SZLIFIE-POL AL", "IPOI2", nilosc, NR_DOK.ToString() + "[HG]");
                }

            }




            return "OK";
        }

        [WebMethod]
        public void H7Raport_dodaj_log(string litm_z, string litm_do, int ilosc, string magazyn_z, string lok_z, string login, string wynik)
        {
            var db = new dbtrans1DataContext();
            //przesun na graff-bis - poprawa
            H7_REKLASYFIKACJA rk = new H7_REKLASYFIKACJA();

            rk.Data_utw = DateTime.Now;
            rk.ilosc = ilosc;
            rk.indeks_do = litm_do;
            rk.indeks_z = litm_z;
            rk.lokalizacja_z = lok_z;
            rk.magazyn_z = magazyn_z;
            rk.User = login;
            rk.wynik = wynik;
            db.H7_REKLASYFIKACJAs.InsertOnSubmit(rk);
            db.SubmitChanges();


        }

        [WebMethod]
        public string H7Raport(string litm_z, string litm_do, int ilosc, string magazyn_z, string lok_z, string login)
        {
            var db = new dbtrans1DataContext();
            var db1 = new DBDataContext();
            //przesun na graff-bis - poprawa
            Service1 srv = new Service1();

            try
            {

                var test_lok_z = (from c in db1.IPO_magazyny_IPO2JDE
                                  where c.LILOCN.Trim().ToLower() == lok_z.Trim().ToLower()
                                  select c).First();
                lok_z = test_lok_z.LILOCN;



                var test_mag_z = (from c in db1.IPO_magazyny_IPO2JDE
                                  where c.LIMCU.Trim().ToLower().Trim() == magazyn_z.ToLower().Trim()
                                  select c).First();
                magazyn_z = test_mag_z.LIMCU;




            }
            catch (Exception ex) { return "Błąd lokalizacji/magazynu!!!"; }



            long NR_DOK = db.Next_nr_doco().First().Next_number;





            var test = JDE_Zaksięguj_przesuniecie("PD810", "cdaa", litm_z, "SZ", magazyn_z, lok_z, "PROD", "H7PROD", "", ilosc, NR_DOK.ToString() + "[HC]");
            //zamień indeks na nowy za pomocą I2 i I3

            if (test.Contains("3259") || test.Contains("0353") || test.Contains("0003")) return $"Błąd!!! {test}";

            var test1 = JDE_Zaksięguj_dok_prosty("PD810", "cdaa", litm_z, "SZ", "PROD", "H7PROD", "IPOI2", -ilosc, NR_DOK.ToString() + "[HC]");
            var test2 = JDE_Zaksięguj_dok_prosty("PD810", "cdaa", litm_do, "SZ", "PROD", "H7PROD", "IPOI3", ilosc, NR_DOK.ToString() + "[HC]");

            //przesun na lokalizację docelową


            StringBuilder str = new StringBuilder();
            str.AppendLine("NR:" + NR_DOK.ToString());
            str.AppendLine("INDEKS Z:" + litm_z);
            str.AppendLine("INDEKS DO:" + litm_do);

            str.AppendLine("1 - " + test);
            str.AppendLine("2 - " + test1);
            str.AppendLine("3 - " + test2);

            return str.ToString();
        }

        [WebMethod]
        public string H7Raport_tmp(string litm_z, string litm_do, double ilosc_z, string JM_z, double ilosc_do, string JM_do, string magazyn_z, string lok_z, string magazyn_do, string lok_do, string login)
        {
            var db = new dbtrans1DataContext();
            var db1 = new DBDataContext();
            //przesun na graff-bis - poprawa
            Service1 srv = new Service1();

            try
            {

                var test_lok_z = (from c in db1.IPO_magazyny_IPO2JDE
                                  where c.LILOCN.Trim().ToLower() == lok_z.Trim().ToLower()
                                  select c).First();
                lok_z = test_lok_z.LILOCN;



                var test_mag_z = (from c in db1.IPO_magazyny_IPO2JDE
                                  where c.LIMCU.Trim().ToLower().Trim() == magazyn_z.ToLower().Trim()
                                  select c).First();
                magazyn_z = test_mag_z.LIMCU;

                var test_lok_do = (from c in db1.IPO_magazyny_IPO2JDE
                                  where c.LILOCN.Trim().ToLower() == lok_do.Trim().ToLower()
                                  select c).First();
                lok_do = test_lok_do.LILOCN;



                var test_mag_do = (from c in db1.IPO_magazyny_IPO2JDE
                                  where c.LIMCU.Trim().ToLower().Trim() == magazyn_do.ToLower().Trim()
                                  select c).First();
                magazyn_do = test_mag_do.LIMCU;



            }
            catch (Exception ex) { return "Błąd lokalizacji/magazynu!!!"; }



            long NR_DOK = db.Next_nr_doco().First().Next_number;





            //var test = JDE_Zaksięguj_przesuniecie("PD810", "cdaa", litm_z, "SZ", magazyn_z, lok_z, "PROD", "H7PROD", "", ilosc, NR_DOK.ToString() + "[HC]");
            //zamień indeks na nowy za pomocą I2 i I3

            //if (test.Contains("3259") || test.Contains("0353") || test.Contains("0003")) return $"Błąd!!! {test}";

            var test1 = JDE_Zaksięguj_dok_prosty("PD810", "cdaa", litm_z, JM_z, magazyn_z, lok_z, "IPOI2", -ilosc_z, NR_DOK.ToString() + "[HC]");
            var test2 = JDE_Zaksięguj_dok_prosty("PD810", "cdaa", litm_do, JM_do, magazyn_do, lok_do, "IPOI3", ilosc_do, NR_DOK.ToString() + "[HC]");

            //przesun na lokalizację docelową


            StringBuilder str = new StringBuilder();
            str.AppendLine("NR:" + NR_DOK.ToString());
            str.AppendLine("INDEKS Z:" + litm_z);
            str.AppendLine("INDEKS DO:" + litm_do);

            //str.AppendLine("1 - " + test);
            str.AppendLine("2 - " + test1);
            str.AppendLine("3 - " + test2);

            return str.ToString();
        }



        [WebMethod]
        public bool Zaloguj(string login, string haslo)
        {
            dbtrans1DataContext db = new dbtrans1DataContext();


            try
            {
                var user = db.uzytkownicies.Where(o => o.login == login).First();

                if (!(bool)user.Aktywny) return false;

                bool test = false;
                string salt = user.salt;
                string sha1 = GetSHA1Hash(salt + haslo);



                test = user.password == sha1;

                //sprawdz czy konta nie są zablokowane datami OD DO
                if (!user.Konto_aktywne_do.Equals(null))
                {
                    if (((DateTime)user.Konto_aktywne_do < DateTime.Now))
                    {
                        user.Aktywny = false;
                        user.Komentarz = "Konto zablokowane " + DateTime.Now.ToString();
                        db.SubmitChanges();
                        logger.Info("Nie udana próba logowania: " + user);
                        return false;
                    }
                }
                if (!user.Konto_aktywne_od.Equals(null))
                {
                    if (((DateTime)user.Konto_aktywne_od > DateTime.Now))
                    {

                        user.Komentarz = "Konto jeszcze nie aktywne. Próba zalogowania " + DateTime.Now.ToString();
                        db.SubmitChanges();
                        logger.Info("Nie udana próba logowania: " + login);
                        return false;
                    }
                }


                user.Ostatnie_logowanie = DateTime.Now;
                db.SubmitChanges();
                if (!test) logger.Info("Nie udana próba logowania: " + login);
                else
                { logger.Info("Zalogowano: " + login); }

                return test;
            }
            catch

            { return false; }




        }

        [WebMethod]
        public DataTable DeserializeDataTable(string text)
        {
            DataTable dt = new DataTable();

            XmlSerializer xmlSerializer = new XmlSerializer(dt.GetType());
            dt = (DataTable)xmlSerializer.Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(text ?? "")));


            return dt;
        }

        [WebMethod]
        public void SerializeDataTable(DataTable tb)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(tb.GetType());
            string text = "";

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, tb);
                text = textWriter.ToString();
            }



        }


        [WebMethod]
        public string Resetuj_haslo_nie_nadzorowane(string login, string nowe_haslo)
        {

            dbtrans1DataContext db = new dbtrans1DataContext();
            try
            {
                var user = db.uzytkownicies.Where(o => o.login == login).First();
                string new_salt = Guid.NewGuid().ToString().Replace("-", string.Empty);
                string haslo = GetSHA1Hash(new_salt + nowe_haslo);
                user.salt = new_salt;
                user.password = haslo;

                db.SubmitChanges();
                Service1 srv = new Service1();
                if (user.tel_kom.Length == 9) srv.SendSMS(user.tel_kom, "Zmiana hasla uzytkownika " + login + ". Twoje haslo to " + nowe_haslo);



                return "OK";
            }
            catch { return "BŁĄD"; }
        }

        static string GetSHA1Hash(string input)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }



        public string GetUniqueKey(int maxSize)
        {
            char[] chars = new char[62];
            chars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            byte[] data = new byte[1];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetNonZeroBytes(data);
                data = new byte[maxSize];
                crypto.GetNonZeroBytes(data);
            }
            StringBuilder result = new StringBuilder(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }


        [WebMethod]
        public List<SLOWNIK_1> Rozwin_Norme_Materialowa(string Kod_Wejsciowy, double Ilosc, int? ANPL)
        {
            List<SLOWNIK_1> ret = new List<SLOWNIK_1>();

            DBDataContext db = new DBDataContext();
            var item_data = (from x in db.SLOWNIK_1s where x.IMLITM.Trim().ToLower() == Kod_Wejsciowy.Trim().ToLower() select x).FirstOrDefault();
            if (item_data == null) throw new Exception("Kod nie istnieje");
            if (item_data.IMANPL == ANPL)
            {
                List<SLOWNIK_1> st = new List<SLOWNIK_1>();
                st.Add(item_data);
                return st;
            }
            var Rozwiniecie = db.RozwiniecieNormy(Kod_Wejsciowy, Ilosc).ToList();
            ret = (from x in db.SLOWNIK_1s.ToList() join w in Rozwiniecie on x.IMITM equals (double)w.itm_podzesp select x).ToList();
            if (ANPL != null)
            {
                ret = ret.Where(x => x.IMANPL == ANPL).ToList();
            }
            return ret;
        }
        [WebMethod]
        public List<SLOWNIK_1> Pobierz_Norma_1_Poziom(int? ITM, String LITM)
        {
            DBDataContext db = new DBDataContext();
            return (from x in db.IPO_Rozpis_mats join w in db.SLOWNIK_1s on x.skladnik_s equals w.IMITM where x.wyrob_l == LITM && w.IMSHCN.Trim() != "CN" || x.wyrob_s == ITM && w.IMSHCN.Trim() != "CN" select w).ToList();
        }
        [WebMethod]
        public string Zaksieguj_Odciaganie(string LITM, double Ilosc, string login, string Magazyn_z, string Magazyn_do, string Lokalizacja_z, string Lokalizacja_do)
        {
            var do_kolor = Rozwin_Norme_Materialowa(LITM, Ilosc, 10004).FirstOrDefault();
            if (do_kolor == null) return "Coś poszło nie tak. Brak kodów do koloru";
            var obr = Pobierz_Norma_1_Poziom((int)do_kolor.IMITM, do_kolor.IMLITM).FirstOrDefault();
            if (obr == null) return "Coś poszło nie tak. Brak kodów poziomu -1";
            return GraffBis_Poprawa(LITM, obr.IMLITM, (int)Ilosc, Magazyn_z, Magazyn_do, Lokalizacja_z, Lokalizacja_do, login);


        }

        [WebMethod]
        public void NAGL_zmien_status_naglowka(Status_zlecenia stat, int id_usera, int nr_zlec)
        {
            try
            {
                dbtrans1DataContext db = new dbtrans1DataContext();
                var nagl = (from c in db.zlecenia_naglowkis where c.Nr_zlecenia == nr_zlec select c).FirstOrDefault();
                nagl.Status = stat.ToString();
                nagl.Data_ost_mod = DateTime.Now;
                nagl.Autor_ost_mod = id_usera;
                db.SubmitChanges();

            }
            catch { }


        }



        public System.Data.DataTable LINQToDataTable<T>(IEnumerable<T> varlist)
        {
            DataTable dtReturn = new DataTable();


            // column names
            PropertyInfo[] oProps = null;


            if (varlist == null) return dtReturn;


            foreach (T rec in varlist)
            {
                // Use reflection to get property names, to create table, Only first time, others will follow
                if (oProps == null)
                {
                    oProps = ((Type)rec.GetType()).GetProperties();
                    foreach (PropertyInfo pi in oProps)
                    {
                        Type colType = pi.PropertyType;


                        if ((colType.IsGenericType) && (colType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                        {
                            colType = colType.GetGenericArguments()[0];
                        }
                        dtReturn.Columns.Add(new DataColumn(pi.Name, colType));
                    }
                }


                DataRow dr = dtReturn.NewRow();


                foreach (PropertyInfo pi in oProps)
                {
                    dr[pi.Name] = pi.GetValue(rec, null) == null ? DBNull.Value : pi.GetValue
                    (rec, null);
                }


                dtReturn.Rows.Add(dr);
            }
            return dtReturn;
        }
        [WebMethod]
        public int TworzZlecenieVTrans(int NrDokIPO,int odpowiedzialny,bool? wyslijSMS,bool? BER)
        {
            try
            {
                /*Pobierz dane z IPO*/
                using (DB2DataContext Raporty = new DB2DataContext() )
                using (DBDataContext db = new DBDataContext())
                using (dbtrans1DataContext trans = new dbtrans1DataContext())
                {
                    var IPOData = (from x in Raporty.IPO_ZLECENIA where x.ipo_nr_zlec == NrDokIPO select x).FirstOrDefault();
                    if (IPOData == null) throw new Exception("Nie odnaleziono zlecenia IPO");
                    var F4101 = (from x in db.F4101s where x.IMLITM.Trim() == IPOData.Indeks_zlecenia.Trim() select x).FirstOrDefault();
                    if (F4101 == null) throw new Exception("Nie odnaleziono danych indeksu " + IPOData.Indeks_zlecenia);
                    var VTransUSer = trans.uzytkownicies.Where(x => x.id == 103).FirstOrDefault();
                    var Magazyn = (from x in db.IPO_MAGAZYN_PODSTAWOWY_PW where x.LIITM == F4101.IMITM select x).FirstOrDefault();
                    if (VTransUSer == null) throw new Exception("Brak kontekstu użytkownika do założenia zlecenia");

                    var Odpowiedzialny = (from x in trans.uzytkownicies where x.id == odpowiedzialny select x).FirstOrDefault();
                    
                    if (Odpowiedzialny == null && odpowiedzialny>0) throw new Exception("Osoba odpowiedzialna nie została odnaleziona");
                    double IloscZam = IPOData.ilosc_zam;
                    zlecenia_naglowki naglowek = new zlecenia_naglowki();
                    List<zlecenia_szczegoly> szczegoly = new List<zlecenia_szczegoly>();
                    naglowek.Autor_ost_mod = VTransUSer.id;
                    naglowek.BER = BER ?? false;
                    naglowek.Data_ost_mod = DateTime.Now;
                    naglowek.Data_utworzenia = DateTime.Now;
                    naglowek.data_wymagana = IPOData.dataGraniczna??DateTime.Now;
                    naglowek.LITM = F4101.IMLITM;
                    naglowek.odpowiedzialny = odpowiedzialny;
                    naglowek.Opis = IPOData.nr_zam_klienta;
                    naglowek.Status = "oczekuje";
                    naglowek.Typ = "Kompletacja";
                    naglowek.Utworzony_przez = VTransUSer.id;
                    List<Specyfikacja_Mont_SzczegResult>rozwiniecie =  db.Specyfikacja_Mont_Szczeg(F4101.IMLITM, IloscZam).ToList();
                    if (rozwiniecie.Count == 0) throw new Exception("Nie udało się rozwinąć wyrobu");
                    trans.zlecenia_naglowkis.InsertOnSubmit(naglowek);
                    trans.SubmitChanges();
                    int lineno = 100;
                    foreach (var item in rozwiniecie)
                    {
                        zlecenia_szczegoly sc = new zlecenia_szczegoly();
                        sc.autor_ost_oper = VTransUSer.id;
                        sc.autor_zlecenia = VTransUSer.id;
                        sc.data_ost_oper = DateTime.Now;
                        sc.data_utworzenia = DateTime.Now;
                        sc.id_zlecenia = naglowek.Nr_zlecenia;
                        sc.ilosc_otwarta = item.Ilosc;
                        sc.ilosc_zamowiona = item.Ilosc??0;
                        sc.ilosc_zrealizowana = 0;
                        sc.itm = item.itm_podzesp??0;
                        sc.JM = item.JM;
                        sc.litm = item.Kod_pozespol;
                        sc.lokalizacja_do = Magazyn.LILOCN;
                        sc.lokalizacja_z = "";
                        sc.magazyn_do = "PROD";
                        if (item.IMSTKT == "P")
                        {
                            sc.magazyn_z = "62";
                        } else
                        {
                            sc.magazyn_z = "PROD";
                        }
                        sc.nr_linii = lineno;
                        sc.opis = item.Opis_podzespol;
                        sc.PRP4 = item.IMPRP4;
                        sc.SRP3 = item.IMSRP3;
                        if (item.IMSTKT == "0")
                        {
                            sc.status_linii = "fantom";

                        } else
                        {
                            sc.status_linii = naglowek.Status;
                        }
                        szczegoly.Add(sc);
                        lineno += 100;
                    }
                    trans.zlecenia_szczegoly.InsertAllOnSubmit(szczegoly);
                    trans.SubmitChanges();
                    if (Odpowiedzialny!=null) 
                    {

                        Service1 serw = new Service1();
                        if (!(String.IsNullOrEmpty(Odpowiedzialny.tel_kom)) && (wyslijSMS??false)) 
                            {
                            serw.SendSMS(Odpowiedzialny.tel_kom, "wystawiono zlecenie kompletacji nr " + naglowek.Nr_zlecenia);
                        }
                        string body = "W systemie prostych przekazań wystawiono nowe zlecenie kompletacji i czeka ono na wydanie \n lokalizacja końcowa: "+Magazyn.LILOCN;
                        string subject = "Wystawiono zlecenie kompletacji nr "+naglowek.Nr_zlecenia;
                        serw.SendAlert(Odpowiedzialny.email, subject, body);

                    }


                    return naglowek.Nr_zlecenia;

                }
            }
            catch (Exception ex)
            {
                logger.Error("Funkcja TworzZlecenieVtrans zwróciła błąd"+ex);
                return 0;
            }
            return 0;
        }

    }


    public class Analiza
    {
        public string litm { get; set; }
        public string nazwa { get; set; }
        public double ilosc_otwarta { get; set; }
        public double stan_mag { get; set; }
        public string PRP4 { get; set; }
        public string SRP3 { get; set; }
        public string STATUS { get; set; }
        public char? IMSTKT { get;  set; }
    }



    public class Vuser
    {
        public int Id_usera;
        public string Nazwisko;
        public string Imię;
        public string login;
        public string email;
        public string telefon;
        public bool aktywny;
    }

    

}




