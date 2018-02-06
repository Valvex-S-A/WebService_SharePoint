using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Services;

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
        [WebMethod]
        public string Nowy_uzytkownik(string login, string haslo, string imie, string nazwisko, string nr_tel, string email, string komentarz, bool aktywny)
        {
            dbtransDataContext db = new dbtransDataContext();

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

                db.uzytkownicy.InsertOnSubmit(user);
                db.SubmitChanges();

                Service1 srv = new Service1();
                if (nr_tel.Length == 9) srv.SendSMS(nr_tel, "Utworzono uzytkownika " + login + ". Twoje haslo to " + haslo);

            }
            catch { return "BLAD!!!"; }



            return  "OK";
        }

        [WebMethod]
        public bool Zaloguj(string login, string haslo)
        {
            dbtransDataContext db = new dbtransDataContext();


            try
            {
                var user = db.uzytkownicy.Where(o => o.login == login).First();

                if (!(bool)user.Aktywny) return false;

                bool test = false;
                string salt = user.salt;
                string sha1 = GetSHA1Hash(salt + haslo);
                


                test = user.password == sha1;

                //sprawdz czy konta nie są zablokowane datami OD DO
                if (!user.Konto_aktywne_do.Equals(null))
                {
                    if (   ((DateTime)user.Konto_aktywne_do < DateTime.Now ))
                        {
                        user.Aktywny = false;
                        user.Komentarz = "Konto zablokowane " + DateTime.Now.ToString();
                        db.SubmitChanges();

                        return false;
                    }
                }
                if (!user.Konto_aktywne_od.Equals(null))
                {
                    if (((DateTime)user.Konto_aktywne_od > DateTime.Now))
                    {
                        
                        user.Komentarz = "Konto jeszcze nie aktywne. Próba zalogowania " + DateTime.Now.ToString();
                        db.SubmitChanges();

                        return false;
                    }
                }


                user.Ostatnie_logowanie = DateTime.Now;
                db.SubmitChanges();

                return test;
            }
            catch

            { return false; }
            


           
        }

        [WebMethod]
        public string Resetuj_haslo_nie_nadzorowane(string login, string nowe_haslo)
        {

            dbtransDataContext db = new dbtransDataContext();
            try
            {
                var user = db.uzytkownicy.Where(o => o.login == login).First();
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
    }

    



}




