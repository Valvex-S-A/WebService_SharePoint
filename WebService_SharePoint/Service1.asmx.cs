using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using SP = Microsoft.SharePoint.Client;
using System.Net;
using System.Threading;
using System.Data.SqlClient;
using System.Data.Sql;
using System.Reflection;
using System.Data;
using DB = System.Data.SQLite;
using System.IO;
using System.Net.Cache;
using System.Text.RegularExpressions;
using NLog;
using System.Net.Mail;
using System.Web.Script.Serialization;
using System.Security.Principal;
using API;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.Drawing;
using System.Text;

namespace WebService_SharePoint
{
    /// <summary>
    /// Summary description for Service1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class Service1 : System.Web.Services.WebService
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        [WebMethod]
        public string GetNistTime()
        {
            DateTime dateTime = DateTime.MinValue;

        

            String output = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");


            return output;
        }


       [WebMethod]
        public void COPY_FROM_CRP2PROD_42199(int nr_zlecenia, string typ_zlecenia)
        {

            DBDataContext db = new DBDataContext();

            db.COPY_42199_FROM_CRP2PROD(nr_zlecenia, typ_zlecenia);




            return;
        }

        [WebMethod]
        public DataTable INW_z_lok_ind(string lok, string ind)
        {
            DB2DataContext db = new DB2DataContext();


            DataTable dt = new DataTable();


            if (!string.IsNullOrEmpty(lok) && string.IsNullOrEmpty(ind))
            {
                dt = new DataTable();
                var zap = (from c in db.INW_SPIs
                           where c.LOKALIZACJA == lok && c.ANULOWANY == false  
                           select new { c.NR_INDEKSU_LITM, c.ILOSC, c.NR_KOMISJI, c.LOKALIZACJA });
                dt = LINQToDataTable(zap);
                dt.TableName = "Spis";
                return dt;
            }
            if (string.IsNullOrEmpty(lok) && !string.IsNullOrEmpty(ind) )
            {
                dt = new DataTable();
                var zap = (from c in db.INW_SPIs
                           where c.ANULOWANY == false && c.NR_INDEKSU_LITM.Contains(ind)
                           select new { c.NR_INDEKSU_LITM, c.ILOSC, c.NR_KOMISJI, c.LOKALIZACJA });
                dt = LINQToDataTable(zap);
                dt.TableName = "Spis";
                return dt;
            }
            if (!string.IsNullOrEmpty(lok) && !string.IsNullOrEmpty(ind))
            {
                dt = new DataTable();
                var zap = (from c in db.INW_SPIs
                           where c.LOKALIZACJA == lok &&  c.ANULOWANY == false && c.NR_INDEKSU_LITM.Contains(ind)
                           select new { c.NR_INDEKSU_LITM, c.ILOSC, c.NR_KOMISJI, c.LOKALIZACJA });
                dt = LINQToDataTable(zap);
                dt.TableName = "Spis";
                return dt;
            }

            dt.TableName = "Spis";
            return dt;
        }



        [WebMethod]
        public DataTable INW_z_lok(string lok)
        {
            DB2DataContext db = new DB2DataContext();

            DataTable dt = new DataTable();
            var zap = (from c in db.INW_SPIs
                       where c.LOKALIZACJA == lok && c.ANULOWANY == false
                       select new { c.NR_INDEKSU_LITM, c.ILOSC, c.NR_KOMISJI });

            dt = LINQToDataTable(zap);
            dt.TableName = "Spis";
            return dt;
        }


        [WebMethod]
        public List<string> INW_komisje()
        {
            List<string> str = new List<string>();
            DB2DataContext db = new DB2DataContext();

            var komisje = (from c in db.INW_PRZYPISANIE_POLA_SPISOWEs select c.NR_KOMISJI).Distinct();

            if (komisje.Count() != 0) str = komisje.ToList<string>();






            return str;
        }


        [WebMethod]
        public DataTable INW_do_korekty(string nr_komisji)
        {
            DB2DataContext db = new DB2DataContext();

            DataTable dt = new DataTable();
            var zap = (from c in db.INW_SPIs
                      where c.ANULOWANY == true && c.KOMENTARZ == "kor"
                      select new { c.LOKALIZACJA, c.MAGAZYN, c.NR_INDEKSU_LITM, c.ILOSC, c.JM, c.KOMENTARZ,c.ID_ZAPISU }).Take(1);

            dt = LINQToDataTable(zap);
            dt.TableName = "Spis";

            if (zap.Count() == 1)
            {
                var kzap = (from c in db.INW_SPIs
                            where c.ID_ZAPISU == zap.First().ID_ZAPISU
                            select c).Take(1);


                foreach (var z in kzap)
                {
                    z.KOMENTARZ
                        = "POBRANE";


                }
                db.SubmitChanges();
            }
            return dt;
        }




        [WebMethod]
        public DataTable INW_Zapisy_komisji(string nr_komisji)
        {
            DB2DataContext db = new DB2DataContext();

            DataTable dt = new DataTable();
            var zap = from c in db.INW_SPIs
                      where c.NR_KOMISJI == nr_komisji
                      select c;

            dt = LINQToDataTable(zap);
            dt.TableName = "Spis";




            return dt;
        }

        [WebMethod]
        public string SendToZebra(string ip_address, int port, string ZPLString)
        {
            try
            {
                // Open connection
                System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient();
                client.Connect(ip_address, port);

                // Write ZPL String to connection
                System.IO.StreamWriter writer =
                new System.IO.StreamWriter(client.GetStream());
                writer.Write(ZPLString);
                writer.Flush();

                // Close Connection
                writer.Close();
                client.Close();
                return "OK";
            }
            catch (Exception ex)
            {
                return "ERROR!!!";
            }


        }



        [WebMethod]
        public string[] Get_Location(string comm_id)
        {
            DB2DataContext db = new DB2DataContext();
            string[] str = null;
            var t = from c in db.INW_PRZYPISANIE_POLA_SPISOWEs
                    where c.NR_KOMISJI == comm_id
                    select new { LOC = c.MAGAZYN + ";" + c.LOKALIZACJA + (c.KOLOR == "%" ? "" : ";" + c.KOLOR) };

            if (t.Count() != 0)
            {
                str = new string[t.Count()];

                int l = 0;

                foreach (var n in t)
                {
                    str[l] = n.LOC;
                    l++;
                }
            }
            return str;

        }





        [WebMethod]
        public string UpdateDB_stan()
        {

            string db_index = Path.GetTempPath() + @"\\db_index.sqlite";
            string index_source_string = "DataSource=" + db_index + ";Version=3;BinaryGUID=False";
            string sql;
            DB.SQLiteConnection conn;
            DB.SQLiteCommand cm;


            if (!File.Exists(db_index))
            {
                DB.SQLiteConnection.CreateFile(db_index);
                conn = new DB.SQLiteConnection(index_source_string);
                conn.Open();
                sql = "CREATE TABLE stany (ITM INT,mag NVARCHAR(50),lok NVARCHAR (50),ilosc DOUBLE,wartosc DOUBLE); ";
                cm = new DB.SQLiteCommand(sql, conn);
                cm.ExecuteNonQuery();
                conn.Close();


            }


            conn = new DB.SQLiteConnection(index_source_string);
            conn.Open();
            sql = "delete from stany";
            cm = new DB.SQLiteCommand(sql, conn);





            cm.ExecuteNonQuery();
            DB2DataContext db = new DB2DataContext();
            var sl = (from c in db.INW_STANY_SPIs
                      select c);
            sql = "INSERT INTO stany (ITM,mag,lok,ilosc,wartosc) VALUES (@ITM,@mag,@lok,@ilosc,@wartosc) ";
            cm = new DB.SQLiteCommand(sql, conn);
            cm.Parameters.Add("@ITM", DbType.Int32);
            cm.Parameters.Add("@mag", DbType.String);
            cm.Parameters.Add("@lok", DbType.String);
            cm.Parameters.Add("@ilosc", DbType.Double);
            cm.Parameters.Add("@wartosc", DbType.Double);
            int n = 0;

            foreach (var s in sl)
            {
                cm.Parameters["@ITM"].Value = s.PJITM;
                cm.Parameters["@mag"].Value = s.mag;
                cm.Parameters["@lok"].Value = s.lok;
                cm.Parameters["@ilosc"].Value = s.ILOSC;
                cm.Parameters["@wartosc"].Value = s.WARTOSC;
                cm.ExecuteNonQuery();


                n++;
            }





           conn.Close();



         return "OK";


          }


        [WebMethod]
        public string[] INW_KOMUNIKATY(string nr_kom)
        {
            DB2DataContext db = new DB2DataContext();
            var kom = from c in db.INW_KOMUNIKATies
                      where c.PRZECZYTANY == false && c.NR_KOMISJI == nr_kom
                      select c.TRESC;

            string[] kom_s = kom.ToArray();


            var koms = from c in db.INW_KOMUNIKATies
                       where c.PRZECZYTANY == false && c.NR_KOMISJI == nr_kom
                       select c;
            foreach (var k in koms)
            {

                k.PRZECZYTANY = true;
                k.DATA_ODCZYTU = DateTime.Now;
            }
            db.SubmitChanges();



            return kom_s;
        }



        [WebMethod]
        public List<Guid> INW_Insert_spis_table(DataTable rec)
        {
            List<Guid> gd = new List<Guid>();



            foreach (DataRow dr in rec.Rows)
            {
                Guid gt = Insert_spis(
                        (bool)dr["ANULOWANY"], ((DateTime)dr["DATA_ZAPISU"]).AddHours(-6), double.Parse(dr["ILOSC"].ToString()), (string)dr["JM"],
                        "ZAPISANE", 0, ((string)dr["LOKALIZACJA"]).Replace(".",""), (string)dr["MAGAZYN"], (int)dr["NR_INDEKSU_ITM"],
                        (string)dr["NR_INDEKSU_LITM"], (string)dr["NR_KOMISJI"], (Guid)dr["GUID"]);

                gd.Add(gt);
            }




            return gd;
        }

        [WebMethod]
        public string IPO_get_user(string id)
        {

            string nazwa = "BŁAD!!!";
            DB2DataContext db = new DB2DataContext();

            var prac = from c in db.IPO_pracownicy
                       where c.ID == id
                       select c;

            if (prac.Count() == 1) nazwa = prac.First().Nazwa;


            return nazwa;
        }



        [WebMethod]
        public string IPO_aktywuj_autoprodukcje()
        {
            DBDataContext db = new DBDataContext();
            DB2DataContext db2 = new DB2DataContext();
            API.IPO_API api = new API.IPO_API();
            var indeksy = from c in db2.IPO_autoprodukcja
                          select c;

            foreach (var indeks in indeksy)
            {

                this.IPOupdateItem_LITM(indeks.JDE_ITM);

            }

            return "ok";
        }


        [WebMethod]
        public string IPO_Zmien_autoprodukcje(bool status, bool autogrow)
        {
            DBDataContext db = new DBDataContext();
            DB2DataContext db2 = new DB2DataContext();
            API.IPO_API api = new API.IPO_API();
            var indeksy = from c in db2.IPO_autoprodukcja
                          select c;

            foreach (var indeks in indeksy)
            {
                int itm = (int)db.SLOWNIK_1.Where(x => x.IMLITM == indeks.JDE_ITM).First().IMITM;


                API.Item item = api.ITEM_GET(itm.ToString());
                item.auto_production = status;
                item.min_qty = indeks.Partia_min;
                item.production_qty = indeks.Partia_opt;
                item.auto_grow = autogrow;

                api.ITEM_POST(item);

            }



            return "OK";
        }


        [WebMethod]
        public Guid Insert_spis(bool anulowany, DateTime data_zapisu, double ilosc, string jm, string komentarz, double koszt, string loc, string mag, int litm, string indeks,
            string nr_komisji, Guid Gd)
        {
            DBDataContext d = new DBDataContext();
            DB2DataContext db = new DB2DataContext();

            var check = from c in db.INW_SPIs
                        where c.GUID == Gd
                        select c.ID_ZAPISU;

            if (indeks.Length>11)
            {
                var kod = (from c in d.INW_KODY_EAN1s
                          where c.IVCITM == indeks
                          select c).Take(1);
                if (kod.Count() == 1)
                {
                    var k = kod.Single();
                    indeks = k.indeks;
                }
                

            }




            if (litm == 0)
            {



                var itm = (from c in d.SLOWNIK_1
                           where c.IMLITM == indeks
                           select c.IMITM).Single();
                litm = (int)itm;


            }



            if (check.ToArray().Length == 0)
            {

                INW_SPI spis = new INW_SPI();
                var k = d.Koszt_Indeksu(litm).Single();
                koszt = (double)((double)ilosc * k.Koszt);
                spis.ANULOWANY = anulowany;
                spis.DATA_ZAPISU = data_zapisu;
                spis.ILOSC = ilosc;
                spis.JM = jm;
                spis.KOMENTARZ = komentarz;
                spis.KOSZT = koszt;
                spis.LOKALIZACJA = loc;
                spis.MAGAZYN = mag;
                spis.NR_INDEKSU_ITM = litm;
                spis.NR_INDEKSU_LITM = indeks;
                spis.NR_KOMISJI = nr_komisji;
                spis.GUID = Gd;

                db.INW_SPIs.InsertOnSubmit(spis);
                db.SubmitChanges();

            }

            return Gd;
        }

        [WebMethod]
        public DataTable INW_Dane_indeks(string litm)
        {

            DB2DataContext db = new DB2DataContext();
            litm = litm.ToUpper();
            if (litm.Length>11)
            {
                DBDataContext db2 = new DBDataContext();
                var kd = (from c in db2.INW_KODY_EAN1s
                          where c.IVCITM == litm
                          select c).Take(1);
                if (kd.Count() == 1)
                {
                   var  kod = kd.Single();
                    litm = kod.indeks;
                }
                        


            }







            DataTable st = new DataTable();
            st.TableName = "indeks";
            
            var i = from c in db.słownik_TKWs
                    where c.Indeks.ToUpper() == litm
                    select new { c.Nazwa, c.JM, c.Koszt, c.Marka, ITM = 0, c.Indeks };
            if (i.Count() == 1)
            {
                var item = i.Single();

                DBDataContext db1 = new DBDataContext();
                var itm = (from c in db1.SLOWNIK_1
                           where c.IMLITM == litm
                           select c.IMITM).Single();
                st = LINQToDataTable(i);

                st.Rows[0][4] = itm;
                st.TableName = "indeks";
                st.AcceptChanges();



                db1.Dispose();
            }



            
            db.Dispose();
            return st;
        }


        [WebMethod]
        public int INW_anuluj_zapis(string GUID)
        {
            DB2DataContext db = new DB2DataContext();
            Guid gd = Guid.Parse(GUID);


            var to_kor = from c in db.INW_SPIs
                         where c.GUID == gd
                         select c;
            foreach (var k in to_kor)
            {
                k.ANULOWANY = true;
                k.KOMENTARZ = "ANULOWANE";


            }
            db.SubmitChanges();

            return to_kor.Count();

        }

        [WebMethod]
        public DataTable INW_Pobierz_stany(string litm)
        {
            DB2DataContext db = new DB2DataContext();
            DataTable st = new DataTable();

            if (!litm.Contains("*"))
            {
               
                var s = from c in db.INW_STANY_SPIs
                        where c.PJLITM == litm
                        select new { c.mag, c.lok, c.ILOSC, c.WARTOSC };


                st = LINQToDataTable(s);
                st.TableName = "Stany";
            }
            else
            {
                string pattn = litm.Replace('*', '%');
                var s = (from c in db.INW_STANY_SPIs
                        where System.Data.Linq.SqlClient.SqlMethods.Like(c.PJLITM, pattn )
                        select new { Indeks=c.PJLITM,c.ILOSC,JM=c.IMUOM1,c.lok,c.mag }).Take(100);
                st = LINQToDataTable(s);
                st.TableName = "Indeksy";

            }




            return st;
        }

        [WebMethod]
        public string UpdateDB_indeksy()
        {

            string db_index = Path.GetTempPath() + @"\\db_index.sqlite";
            string index_source_string = "DataSource=" + db_index + ";Version=3;BinaryGUID=False";
            string sql;
            DB.SQLiteConnection conn;
            DB.SQLiteCommand cm;


            if (!File.Exists(db_index))
            {
                DB.SQLiteConnection.CreateFile(db_index);
                conn = new DB.SQLiteConnection(index_source_string);
                conn.Open();
                sql = "create table lista_indeksow (ID int,indeks nvarchar(50),nazwa nvarchar(100), JM nvarchar(10), LITM nvarchar(50))";
                cm = new DB.SQLiteCommand(sql, conn);
                cm.ExecuteNonQuery();
                conn.Close();


            }


            conn = new DB.SQLiteConnection(index_source_string);
            conn.Open();
            sql = "vacuum";
            cm = new DB.SQLiteCommand(sql, conn);
            cm.ExecuteNonQuery();
            sql = "delete from lista_indeksow";
            cm = new DB.SQLiteCommand(sql, conn);





            cm.ExecuteNonQuery();
            DBDataContext db = new DBDataContext();
            var sl = (from c in db.SLOWNIK_1

                      where c.IMSRP1 != "WYG"
                      select c);
            sql = "INSERT INTO lista_indeksow (ID,indeks,nazwa,JM,LITM) VALUES (@ID,@indeks,@nazwa,@JM,@LITM) ";
            cm = new DB.SQLiteCommand(sql, conn);
            cm.Parameters.Add("@ID", DbType.Int32);
            cm.Parameters.Add("@indeks", DbType.Int32);
            cm.Parameters.Add("@nazwa", DbType.String);
            cm.Parameters.Add("@JM", DbType.String);
            cm.Parameters.Add("@LITM", DbType.String);
            int n = 0;

            foreach (var s in sl)
            {
                cm.Parameters["@ID"].Value = n;
                cm.Parameters["@indeks"].Value = (int)s.IMITM;
                cm.Parameters["@nazwa"].Value = s.NAZWA;
                cm.Parameters["@JM"].Value = s.IMUOM1;
                cm.Parameters["@LITM"].Value = s.IMLITM;
                cm.ExecuteNonQuery();


                n++;
            }





            conn.Close();



            return "OK";
        }





        [WebMethod]
        public API.IPO_Order IPO_GET_ORDER(int IPO_ORDER_ID)
        {
            API.IPO_API api2 = new API.IPO_API();
            API.IPO_Order order = api2.GET_ORDER_BY_IPO_NO(IPO_ORDER_ID);

            return order;



        }

        [WebMethod]
        public API.Item IPO_GET_ITEM_BY_ITM(int itm)
        {

            API.Item i = null;

            API.IPO_API api = new API.IPO_API();
            i = api.ITEM_GET(itm.ToString());

            return i;
        }


        [WebMethod]
        public List<API.IPO_BOM_Line> IPO_BOM_ITEM(string litm)
        {
            DBDataContext db = new DBDataContext();


            var itm = from c in db.SLOWNIK_1
                      where c.IMLITM == litm
                      select c.IMITM;



            List<API.IPO_BOM_Line> i = null;
            if (itm.Count() == 1)
            {
                API.IPO_API api = new API.IPO_API();
                i = api.BOM_GET(itm.First().ToString());

            }

            



            return i;
        }

        
        [WebMethod]
        public API.Item IPO_GET_ITEM(string litm)
        {
            DBDataContext db = new DBDataContext();


            var itm = from c in db.SLOWNIK_1
                      where c.IMLITM == litm
                      select c.IMITM;



            API.Item i = null;

            if (itm.Count() == 1)
            {
                API.IPO_API api = new API.IPO_API();
                i = api.ITEM_GET(itm.First().ToString());
            }



            return i;
        }

        [WebMethod]
        public string IPO_Update_TASKS(int days_ago)
        {

            try
            {

                DateTime to = DateTime.Now;
                DateTime from = to.AddDays(-days_ago);

                API.IPO_API api2 = new API.IPO_API();
                List<string> active = api2.GET_ACTIVE_EMPLOYEES(from, to);

                int i = 1;

                foreach (string empl in active)
                {

                    UpdateEmployee(empl, from, to);
                    i++;

                }


                return "OK;" + "aktualizacja dla " + i.ToString() + " pracowników";
            }
            catch { return "BŁĄD;"; }
        }

        static void UpdateEmployee(string emp_id, DateTime from, DateTime to)
        {




            API.IPO_API api2 = new API.IPO_API();
            DB2DataContext db = new DB2DataContext();



            long _from = (from.Year * 10000) + (from.Month * 100) + (from.Day);
            long _to = (to.Year * 10000) + (to.Month * 100) + (to.Day);




            var itd = from c in db.IPO_Tasks
                      where c.Id_pracownika == emp_id &&
                      (c.Czas_start.Value.Year * 10000 + c.Czas_start.Value.Month * 100 + c.Czas_start.Value.Day) >= _from &&
                      (c.Czas_stop.Value.Year * 10000 + c.Czas_stop.Value.Month * 100 + c.Czas_stop.Value.Day) <= _to
                      select c;
            db.IPO_Tasks.DeleteAllOnSubmit(itd);
            db.SubmitChanges();
            API.EmployeeReport rep = api2.EMPLOYEE_REPORT(emp_id, from, to);

            if (!(rep.tasks == null))
            {
                foreach (API.Task ts in rep.tasks)
                {
                    int item_itm = 0;
                    API.IPO_Order ord = api2.GET_ORDER_BY_IPO_NO(ts.ipo_order_no);
                    IPO_Task tsb = new IPO_Task();

                    API.Item itm = new API.Item();

                    if (!string.IsNullOrEmpty(ord.item_id)) itm = api2.ITEM_GET(ord.item_id.ToString());




                    tsb.Pracownik = rep.name;
                    tsb.Opis_pracy = ts.description + "; " + ts.description_ex;
                    tsb.Id_maszyny = ts.device_id;
                    tsb.Id_pracownika = ts.employee_id;
                    tsb.Id_zlecenia = ts.ipo_order_no;
                    tsb.Nazwa_operacji = ts.operation_name;
                    tsb.Nr_zamowienia = ts.part_no;
                    tsb.Indeks = string.IsNullOrEmpty(itm.index) ? "" : itm.index;
                    tsb.Ilosc_planowana = ts.quantity_planned;
                    tsb.Ilosc_wykonana = ts.quantity_real;
                    tsb.Ilosc_brak = ts.quantity_short;
                    tsb.Task_Id = ts.task_id;
                    tsb.Czas_start = ts.task_start;
                    tsb.Czas_stop = ts.task_stop;
                    tsb.Czas_planowany = ts.time_planned;
                    tsb.Czas_realizacji = ts.time_real;

                    int.TryParse(ord.item_id, out item_itm);
                    tsb.Indeks_ITM = item_itm;

                    db.IPO_Tasks.InsertOnSubmit(tsb);
                    db.SubmitChanges();

                }
            }





        }

        [WebMethod]
        public string Wyprostuj_indeksy_zbiorczo()
        {
            DB2DataContext rap = new DB2DataContext();
            API.IPO_API api2 = new API.IPO_API();
            string komunikat = "indeks ok";
            DBDataContext db = new DBDataContext();
            var indeksy = from c in rap.IPO_INDEKSY_DO_WYPROSTOWANIA
                          select c;

            foreach (var i in indeksy)
            {
                Item it = api2.ITEM_GET(i.kodObcy);


                it.index = "ITM" + i.kodObcy; //zmień ten indeks na inny bo taki może być już w bazie;
                api2.ITEM_POST(it);
                IPOupdateItem_LITM(i.indeks);

            }




            return "OK";
        }



        [WebMethod]
        public string IPO_wyprostuj_dane_ITM_indeksu(int itm)
        {
         
            

            API.IPO_API api2 = new API.IPO_API();
            string komunikat = "indeks ok";
            DBDataContext db = new DBDataContext();
            DB2DataContext db2 = new DB2DataContext();


             

            var JDEindeks = from c in db.SLOWNIK_1
                            where c.IMITM == itm
                            select c.IMLITM;


            var ipo_litm = from c in db2.IPO_MATERIALY_ZOPs
                           where c.IPO_LITM == JDEindeks.First()
                           select c.IPO_ITM;
                            





                int inny_itm = int.Parse(ipo_litm.First());
                Item it = api2.ITEM_GET(inny_itm.ToString());
                string LITM = it.index;

                it.index = "ITM" + itm.ToString(); //zmień ten indeks na inny bo taki może być już w bazie;
                api2.ITEM_POST(it);
                IPOupdateItem_LITM(LITM);




            return   LITM + "  " +  komunikat;
        }

        [WebMethod]
        public int IPO_UPDATE_ITEM_INDEX(int item_id, string new_index)
        {
            API.IPO_API api2 = new API.IPO_API();

            Item it = api2.ITEM_GET(item_id.ToString());
            it.index = new_index;

            api2.ITEM_POST(it);

            return 0;
        }
        
        public List<string> IPO_Get_active_empl(DateTime from, DateTime to)
        {

            API.IPO_API api2 = new API.IPO_API();
            List<string> aktywni = api2.GET_ACTIVE_EMPLOYEES(from, to);


            return aktywni;

        }

        [WebMethod]
        public string[] IPO_GET_TASKS(DateTime from, DateTime to)
        {
            API.IPO_API api2 = new API.IPO_API();
            List<string> report_list = new List<string>();

            List<string> aktywni = IPO_Get_active_empl(from, to);
            foreach (string empl in aktywni)
            {
                API.EmployeeReport rep = api2.EMPLOYEE_REPORT(empl, from, to);
                if (rep != null) {
                    
                    foreach (Task t in rep.tasks)
                    {
                        if (t.operation_name == "USTAWIENIE MASZYNY")
                        {
                            string task_id = t.task_id.ToString();
                            string nr_zlec = t.ipo_order_no.ToString();
                            string prac = t.employee_id;
                            string oper = t.description + ";" + t.description_ex;
                            string masz = t.device_id; 
                                
                            report_list.Add(task_id + "@" + nr_zlec + "@" + prac + "@" + oper + "@" + masz );
                        }

                    }


                     }

            }
            return report_list.ToArray();
        }






        [WebMethod]
        public string IPO_Update_order_sql(int ipo_order_no)
        {
            API.IPO_API api2 = new API.IPO_API();

            DB2DataContext db = new DB2DataContext();

               
            var zlec = from c in db.IPO_ZLECENIAs
                       where c.ipo_nr_zlec == (int)ipo_order_no
                       select c;
            db.IPO_ZLECENIAs.DeleteAllOnSubmit(zlec);
            db.SubmitChanges();

            API.IPO_Order izlec = api2.GET_ORDER_BY_IPO_NO(ipo_order_no);


            IPO_ZLECENIA bzlec = new IPO_ZLECENIA();
            bzlec.contractor_id = izlec.contractor_id;
            bzlec.ilosc_brak = izlec.quantity_short;
            bzlec.ilosc_wypr = izlec.quantity_out;
            bzlec.ilosc_zam = izlec.quantity;
            bzlec.Indeks_zlecenia = izlec.client_order_no;
            bzlec.ipo_id = izlec.ipo_order_id;
            bzlec.ipo_nr_zlec = izlec.ipo_order_no;
            bzlec.koszt = izlec.cost;
            bzlec.magazyn_wej = izlec.warehouse_id;
            bzlec.nr_ITM = izlec.item_id;
            bzlec.nr_rysunku = izlec.drawing_no;
            bzlec.nr_serii = izlec.serial_no;
            bzlec.nr_zam_klienta = izlec.order_no_cust;
            bzlec.start_zam = izlec.order_start;
            bzlec.status_zam = izlec.state.ToString();
            bzlec.stop_zam = izlec.order_stop;
            bzlec.zlec_wew = izlec.inner;

            db.IPO_ZLECENIAs.InsertOnSubmit(bzlec);
            db.SubmitChanges();




            return "OK";
        }



        [WebMethod]
        public string IPO_Delete_item(int item_id_itm)
        {

            API.IPO_API api = new API.IPO_API();

            bool test = api.ITEM_DELETE(item_id_itm.ToString());
            if (test) logger.Info("Skasowano w ipo indeks (itm)" + item_id_itm);

            return (test)? "OK" : "BŁAD" ;
        }
        [WebMethod]
        public string IPO_Delete_Warehouse(string whid)
        {

            API.IPO_API api = new API.IPO_API();

            bool test = api.WAREHOUSE_DELETE(whid);
            if (test) logger.Info("Skasowano w ipo magazyn" + whid);

            return (test) ? "OK" : "BŁAD";
        }

        [WebMethod]
        public string IPO_Multi_update(string items)
        {
            string[] items_t = items.Split(',');
            foreach (string item in items_t)
            {
                
              //  IPOupdateItem(item);
            }
            return "...";



        }

        
        /// <summary>
        /// Aktualizuje dane indeksu w IPO
        /// </summary>
        /// <param name="litm">Długi indeks</param>
        /// <returns></returns>
        [WebMethod]
        public string IPOupdateItem_LITM(string litm)
        {
            DBDataContext db = new DBDataContext();


            var itm = from c in db.SLOWNIK_1
                      where c.IMLITM == litm
                      select c.IMITM;
            if (itm.Count() == 1)
            {
                int itm_ = (int)itm.First();

                this.IPOupdateItem(itm_);

                return "OK";
            }

            return "ZŁY INDEKS!!!";
        }


       /// <summary>
       /// Aktualizuje dane indeksu w IPO
       /// </summary>
       /// <param name="item_id_itm">Krótki numer indeksu</param>
        [WebMethod]
        public void IPOupdateItem(int item_id_itm)
        {
            API.IPO_API api = new API.IPO_API();

            DB2DataContext dbr = new DB2DataContext();
            DBDataContext db = new DBDataContext();
            db.CommandTimeout = 1000000;
            API.Warehouse_Settings mag_wej;

            int kol_mag = 1;
            List<API.Warehouse_Settings> wsl = new List<API.Warehouse_Settings>();

            if (!api.ITEM_HEAD(item_id_itm.ToString()))
            {
                //    logger.Debug("Nie ma ... zakładam.");
                //     new_item = true;
                var ITEM_JDE_CHECK = (from c in db.SLOWNIK_1
                                      where c.IMITM == item_id_itm
                                      select c);
                SLOWNIK_1 ITEM_JDE = new SLOWNIK_1();
                if (ITEM_JDE_CHECK.Count() == 1)
                {
                    ITEM_JDE = ITEM_JDE_CHECK.Single();
                }
                else { logger.Error("BRAK W F4101 indeksu itm:" + item_id_itm.ToString()); return; }

                var k1 = db.Koszt_Indeksu(item_id_itm).Single();
                // logger.Debug("Koszt zaktualizowany");

                API.Item n_it = new API.Item
                {
                    area = 0,
                    blocked = false,
                    cost = (decimal)k1.Koszt,
                    currency = "PLN",
                    def_warehouse = "PROD", //domyślnie - niżej i tak jest aktualizowane z JDE
                    delivery_day = 3,     // domyślnie - niżej i tak jest aktualizowane z JDE
                    description = ITEM_JDE.NAZWA,
                    ean_code = "",
                    group_id = "",
                    index = ITEM_JDE.IMLITM,
                    item_id = item_id_itm.ToString(),
                    manager_id = "",
                    max_qty = 0,
                    measure_unit = ITEM_JDE.IMUOM1,
                    min_qty = 0,
                    name = ITEM_JDE.NAZWA,
                    only_integer = false,
                    order_qty = 0,
                    production_qty = 0,
                    sell_price = 0,
                    service = false,
                    symbol = "",
                    weight = 0
                };
                if (n_it.measure_unit == "SZ") n_it.only_integer = true;

                if (api.ITEM_PUT(n_it)) logger.Log(LogLevel.Debug, "Założono indeks ");
                else logger.Log(LogLevel.Fatal, "Bład przy zakładaniu indeksu " + n_it.index);
            }

            var stany = from c in db.IPO_STANies
                        where item_id_itm == c.ITEM_ID
                        select c;

            List<API.ItemStock> item_stocks = new List<API.ItemStock>();
            string domyslny_magazyn_wejsciowy = "PROD";

            //tutuaj dodawać też inne aktualizację indeksów.
            API.Item item = api.ITEM_GET(item_id_itm.ToString());
            //  logger.Info("Aktualna JM dla itm " + stan.ITEM_ID.ToString() +  "  to " + item.measure_unit + ", zmieniono na " + stan.JM_PROD);
            //aktualizacja czasu realizacji zamówienia zakupowego
            var i_jde_ = from c in db.F4101s
                         where c.IMITM == item_id_itm
                         select new { c.IMLTCM, c.IMPRP8, c.IMSRP3, c.IMLITM };

            var mag_pw_jde = from c in db.IPO_MAGAZYN_PODSTAWOWY_PWs
                             where c.LIITM == item_id_itm
                             select c.mag_ipo;

            var ITEM_JDE_CHECK2 = (from c in db.SLOWNIK_1
                                   where c.IMITM == item_id_itm
                                   select c);
            if (ITEM_JDE_CHECK2.Count() == 1)
            {
                item.name = ITEM_JDE_CHECK2.Single().NAZWA.Trim() + "/" + ITEM_JDE_CHECK2.Single().KOD_PLAN.Trim() + "/" + ITEM_JDE_CHECK2.Single().KOLOR.Trim();
            }


            if (mag_pw_jde.Count() == 1)
            {

                domyslny_magazyn_wejsciowy = mag_pw_jde.Single();

                item.def_warehouse = domyslny_magazyn_wejsciowy;
                CheckCreateWH(api, item.def_warehouse);
                logger.Debug(item.index + " - dodano magazyn podstawowy:" + item.def_warehouse);
            }



            if (i_jde_.Count() == 1)
            {
                var i_jde = i_jde_.Single();
                int dni = 3;
                int.TryParse(i_jde.IMLTCM.ToString(), out dni);

                var k = db.Koszt_Indeksu(item_id_itm).Single();

                item.cost = (decimal)k.Koszt;
                item.delivery_day = (dni == 0 ? 3 : dni);
                item.index = i_jde.IMLITM;
                item.description = (string.IsNullOrEmpty(i_jde.IMPRP8) ? "" : i_jde.IMPRP8).ToString() + ";" + (string.IsNullOrEmpty(i_jde.IMSRP3) ? "" : i_jde.IMSRP3).ToString() + ";" + DateTime.Now.ToString();
                api.ITEM_POST(item);

                //   logger.Debug("ItemPOST z nową jednostką");
            }

            bool _zalozony_mag_wejsciowy = false;

            foreach (var stan in stany)
            {
                API.Item item1 = api.ITEM_GET(item_id_itm.ToString());
                item1.measure_unit = stan.JM_PROD;
                api.ITEM_POST(item1);


                string wh_name = stan.MAG + (string.IsNullOrEmpty(stan.LOK) ? "" : "_" + stan.LOK).Replace(".", "").Replace("-", "").Replace(",", "").Replace("/", "/").Replace("\\", "").Replace(" ", "");

                mag_wej = new API.Warehouse_Settings();
                mag_wej.direction = 1;

                if (wh_name.Contains("MSU")) mag_wej.direction = 3;
                if (wh_name.Contains("POCZEKALNIA")) mag_wej.direction = 3;
                if (wh_name.Contains("REKLAMACYJ")) mag_wej.direction = 3;
                if (wh_name.StartsWith("62")) mag_wej.direction = 3;
                if (wh_name.StartsWith("MWG")) mag_wej.direction = 3;
                if (wh_name.StartsWith("MK")) mag_wej.direction = 3;
                if (wh_name == "PROD") mag_wej.direction = 3;
                if (wh_name.Contains("H7PROD")) mag_wej.direction = 3;
                if (wh_name.Contains("BOX05")) mag_wej.direction = 1;
                if (wh_name.Contains("BOX07")) mag_wej.direction = 1;
                if (wh_name.Contains("BOX09")) mag_wej.direction = 1;
                if (wh_name.Contains("BOX15")) mag_wej.direction = 1;


                var mag = from c in dbr.IPO_LOKALIZACJE_SZLIFIERNIA_62
                          where c.Lokalizacja == wh_name
                          select c;

                if (mag.Count() != 0) mag_wej.direction = 1;


                mag_wej.warehouse_id = wh_name;
                mag_wej.order = kol_mag;


                if (mag_wej.warehouse_id == domyslny_magazyn_wejsciowy)
                {
                    _zalozony_mag_wejsciowy = true;
                    mag_wej.direction = 2;
                    // tutaj sprawdzenie wyjątków - np tam gdzie chcemy żeby sie zmienił na buforowy wejściowy
                    if (mag_wej.warehouse_id == "PROD_P2OBRÓBKA") mag_wej.direction = 4;
                    if (mag_wej.warehouse_id == "PROD_P31SZLIFPOL") mag_wej.direction = 4;
                    if (mag_wej.warehouse_id.Contains("MSU")) mag_wej.direction = 4;
                    if (mag_wej.warehouse_id == "62_") mag_wej.direction = 4;

                }


                // sprawdz czy taki magazyn juz jest na liście - mogą zdarzyć się duble jeżeli są dwa zakupy na ten sam magazyn
                var check = from c in wsl
                            where c.warehouse_id == mag_wej.warehouse_id
                            select c.warehouse_id;

                //jeżeli jest to nie dodawaj do dostępnych magazynów
                if (check.Count() == 0) wsl.Add(mag_wej);



                kol_mag++;
                //  logger.Debug("Sprawdzam czy jest taki magazyn");
                CheckCreateWH(api, wh_name);

                //  logger.Debug("Nowy ItemStock");

                if (!stan.QTY.HasValue)
                {
                    logger.Debug("Błąd jednostki prod: " + stan.ITEM_ID.Value.ToString());
                    stan.QTY = 0;
                    SendAlert("tomasz.hopcias@valvex.com", "Błąd JM produkcyjnej dla ITM" + stan.ITEM_ID.Value.ToString(), "Błąd api... wrzucam stan zerowy");


                }

                API.ItemStock stock = new API.ItemStock
                {

                    delivery_date = stan.DATA_DOST,
                    doc_no = "JDE_" + stan.NR_DOK.ToString(),
                    on_stock = true,
                    quantity = (decimal)stan.QTY < 0 ? 0 : (decimal)stan.QTY,    //jeżeli stan ujemny to daj zero.
                    stock_type = stan.MAG_ZAK,
                    warehouse_id = wh_name
                };
                item_stocks.Add(stock);
                // logger.Error("item_magazyn załatwione - teraz następny magazyn");
            }
            //nie było domyślnego magazynu wejściowego - załóż go sam na podstawie JDE;
            if (!_zalozony_mag_wejsciowy)
            {
                mag_wej = new API.Warehouse_Settings();
                mag_wej.direction = 2;
                mag_wej.warehouse_id = domyslny_magazyn_wejsciowy;
                mag_wej.order = kol_mag;
                wsl.Add(mag_wej);
            }

            //sprawdz czy lista ma chociaż jeden magazyn wyjściowy (1 lub 2) Jeżeli nie ma to dodaj magazyn TEMP i zrób go wyjściowym
            var check_12 = from c in wsl
                           where c.direction == 1 || c.direction == 2
                           select c;
            if (check_12.Count() == 0)
            {
                mag_wej = new API.Warehouse_Settings();
                mag_wej.direction = 1;
                mag_wej.warehouse_id = "TEMP";
                mag_wej.order = kol_mag;
                wsl.Add(mag_wej);
            }


            bool czy_ok = api.PUT_ITEM_WAREHOUSE_SET(wsl, item_id_itm);
            if (!czy_ok) SendAlert("andrzej.pawlowski@valvex.com", "Bład aktualizacji stanu dla indeksu (ITM) " + item_id_itm.ToString(), "Błąd");


            if (api.ITEM_STOCK_POST(item_stocks, item_id_itm.ToString()))

            { //logger.Info("Stany dla indeksu " + item_id_itm.ToString() + " zaktualizowane");



            }
            else
            {
                logger.Error("Stany dla indeksu " + item_id_itm.ToString() + " niezaktualizowane!!!");
            }



        }

        private static void CheckCreateWH(API.IPO_API api, string wh_name)
        {
            if (!api.WAREHOUSE_HEAD(wh_name))
            {

                API.Warehouse wh = new API.Warehouse
                { name = wh_name, blocked = false, code = wh_name, main_warehouse = false, parent_warehouse_id = "", warehouse_id = wh_name };
                if (api.WAREHOUSE_PUT(wh)) logger.Log(LogLevel.Debug, "Założono magazyn " + wh.warehouse_id);
                else logger.Error("Nieudana próba założenia magazynu " + wh.warehouse_id);
            }
        }



        [WebMethod]
        public void SendAlert(string to, string subject, string body)
        {
            using (SmtpClient cl = new SmtpClient("zimbra.valvex.com"))
            {
                cl.Credentials = new System.Net.NetworkCredential("alert@zimbra.valvex.com", "123QWEasd");

                MailMessage mail = new MailMessage("alert@zimbra.valvex.com", to);
                mail.Subject = "ALERT:  " + subject;
                mail.Body = body;
                cl.Send(mail);
            }
        }
        [WebMethod]
        public void IPO_popraw_indeksy_w_IPO()
        {
            DBDataContext db = new DBDataContext();
            API.IPO_API api = new API.IPO_API();
            var items = from c in db.ZZZ_INDEKSY_POPRs
                        select c.ITEMID;

            foreach (var item in items)
            {
                var i_jde = (from c in db.F4101s
                            where c.IMITM == item.Value
                            select c.IMLTCM).Single();

                int dni = 0;
                int.TryParse(i_jde.ToString(), out dni);
                if (dni == 0) dni = 1;
                API.Item it = new API.Item();
                it = api.ITEM_GET(item.Value.ToString());
                it.delivery_day = dni;
                api.ITEM_POST(it);



            }




   
        }



        [WebMethod]
        public System.Data.DataTable GetItems()
        {
            DBDataContext db = new DBDataContext();


            var t = (from c in db.SLOWNIK_1
                     select new {c.IMLITM,c.NAZWA,c.IMUOM1 });

            DataTable dt = this.LINQToDataTable(t);
            dt.TableName = "indeksy";

            return dt;

        }
         



        [WebMethod]
        public void local_New_Item_SP(string title, string kod, string user, string typ, string opis, string operacja)
        {
            string siteUrl = "http://SP2013/tech";
            
            SP.ClientContext clientContext = new SP.ClientContext(siteUrl);
             

            CredentialCache cc = new CredentialCache();
            cc.Add(new Uri(siteUrl), "NTLM", new NetworkCredential("apawlowski", "cbv3.560671bf", "valvex"));
            clientContext.Credentials = cc;
            clientContext.AuthenticationMode = SP.ClientAuthenticationMode.Default;
            
            var list = clientContext.Web.Lists.GetById(new System.Guid("33f1edb7-b412-485a-83cf-2cece9f361ed"));
            clientContext.ExecuteQuery();

            var itemCreateInfo = new SP.ListItemCreationInformation();

            var listItem = list.AddItem(itemCreateInfo);
            listItem["Title"] = title;
            listItem["Kod_detalu"] = kod;
            listItem["Zg_x0142_aszaj_x0105_cy"] = user;
            listItem["Typ_zg_x0142_oszenia"] = typ;
            listItem["Opis_zg_x0142_oszenia"] = opis;
            listItem["Operacja"] = operacja;



            listItem.Update();
         
            clientContext.ExecuteQuery();

            return;
        }

        [WebMethod]
        public string Aktualizuj_KG(int miesiac, int rok, string mail_komunikat)
        {
            string status = "";
            if (miesiac > 12 || miesiac < 1) return "POPRAW MIESIAC!!!";
            new Thread(() => {
                DB2DataContext db = new DB2DataContext();
                db.CommandTimeout = 1000000;
                int i = db.OdswiezKG(miesiac, rok);
                SendAlert(mail_komunikat, "Odświeżanie KG zakończone " + rok.ToString() + miesiac.ToString(),"");


            }).Start();



            return "START O " + DateTime.Now.ToString();
        }

        [WebMethod]
        public void _UPDATE_ALL_DOC()
        {
            DB2DataContext dbr = new DB2DataContext();

            var rec = from d in dbr.IPO_ZDAWKA_PW
                      where d.Zaksiegowany_JDE == false
                      select d;

            foreach (var r in rec)
            {
                _IPO_KSIEGUJ_RW_PW((int)r.ID, " ");
            }
        }
            
        [WebMethod]
        public string SendSMS(string nr, string txt)
        {

            string URI = @"http://192.168.1.125/send_sms.php?recipient=$rec&txt=$txt";

            URI = URI.Replace("$rec", "48"+ nr);
            URI = URI.Replace("$txt", txt);
            WebClient client = new WebClient();
            Stream str = client.OpenRead(URI);
            StreamReader rd = new StreamReader(str);

            return rd.ReadToEnd();


            
        }

        [WebMethod]
        public void _IPO_KSIEGUJ_RW_PW(int id, string hala)
        {
            DBDataContext db = new DBDataContext();
            DB2DataContext dbr = new DB2DataContext();

            try
            {

                double znak = 1;
                var rec = (from d in dbr.IPO_ZDAWKA_PW
                           where d.ID == id && d.Zaksiegowany_JDE == false
                           select d).Take(1).Single();
                //jeżeli to dokument PU to zaktualizuj magazyn wejściowy na domyślny dla indeksu wyrobu.
                if (rec.RW_PW == "PU")
                {
                    logger.Debug("DOK PU" + rec.Nr_zlecenia_IPO);
                    API.IPO_API api2 = new API.IPO_API();
                    API.IPO_Order order = api2.GET_ORDER_BY_IPO_NO((int)rec.Nr_zlecenia_IPO);

                    if (!string.IsNullOrEmpty(order.warehouse_id)) //Na zleceniu jest domyślny magazyn wejściowy dla indeksu wyrobu ze zlecenia
                    {


                        logger.Debug("DOK PU" + rec.Nr_zlecenia_IPO + "/ Było: " + rec.Magazyn_IPO + " ,item_id zlec = " + order.item_id);

                        {    //dla dokumentu PU zmieniamy na magazyn domyślny z PW
                            rec.Magazyn_IPO = order.warehouse_id;
                            logger.Debug("DOK PU" + rec.Nr_zlecenia_IPO + "/ Jest: " + rec.Magazyn_IPO);
                        }
                    }
                }

                var mag = (from c in db.IPO_magazyny_IPO2JDE where c.mag_ipo == rec.Magazyn_IPO select c).Single();


                string drct = "";
                string doctype = "";
                if (rec.RW_PW == "PW") { doctype = "I3"; drct = "D"; }
                if (rec.RW_PW == "RW" && rec.Magazyn_IPO.Contains("PROD")) { doctype = "I2"; drct = "O"; }
                if (rec.RW_PW == "RW" && !rec.Magazyn_IPO.Contains("PROD")) { doctype = "RW"; drct = "O"; }
                if (rec.RW_PW == "PU" && rec.typ == 1) { doctype = "I2"; drct = "O"; znak = -1; }


                db.ImportIPO(rec.Ilosc * znak, rec.Nr_indeksu, (int)rec.ID, drct, doctype, mag.LIMCU, mag.LILOCN, rec.Nr_zlecenia_IPO.ToString() + hala);


                //rec.HALA_PROD = hala;
                rec.Zaksiegowany_JDE = true;

                var rec_upd = (from d in dbr.IPO_ZDAWKA_PW
                               where d.ID == id
                               select d).Single();
                rec_upd.Zaksiegowany_JDE = true;
                rec_upd.Data_ksiegowania_JDE = DateTime.Now;
                dbr.SubmitChanges();

            }
            catch (Exception ex)
            {
                logger.Error("Nie udało się zaksięgować RWPW id" + id.ToString() + ". " + ex.Message);
            }


        }

        [WebMethod]
        public string IPO_akt_dane_zlec(int nr_zlec, string Nr_zlec_F, string Nr_zam, int priorytet, bool autopodzial, decimal ilosc)
        {

            string kom = "OK";
            API.IPO_API api = new API.IPO_API();
            var zl = api.GET_ORDER_BY_IPO_NO(nr_zlec);
            
            if (zl.ipo_order_id != 0)
            {

                Change_Order Ord = new Change_Order();

                if (!string.IsNullOrEmpty(Nr_zlec_F)) Ord.order_no_cust = Nr_zlec_F;
                if (!string.IsNullOrEmpty(Nr_zam)) Ord.client_order_no = Nr_zam;
                if (priorytet != -1) Ord.priority = priorytet;
                Ord.fast_start_enabled = autopodzial;
                Ord.fast_start_quantity = ilosc;

                kom = api.CHANGE_ORDER(zl.ipo_order_id, Ord);
            }
                return kom;
        }




        [WebMethod]
       public string ZB_AKT_F(bool dodanie, string tekst)
        {

            API.IPO_API api = new API.IPO_API();

            DB2DataContext db2 = new DB2DataContext();
            var zlec = from c in db2.IPO_zlecenia_do_zmiany
                       select c;
            int n = 0;
            foreach (var z in zlec)
            {
                try
                {
                    Aktualizacja_ZLEC_F((int)z.NR_zlec, dodanie, tekst);
                }
                catch { n++; }
            }
            return "OK - błedy" + n.ToString() ;
        }

       
        
        public string Aktualizacja_ZLEC_F(int nr_zlec_IPO, bool dodanie, string tekst)
        {
            API.IPO_API api = new API.IPO_API();

            var zl = api.GET_ORDER_BY_IPO_NO(nr_zlec_IPO);
           
                if (zl.ipo_order_id != 0)
                {

                    Change_Order Ord = new Change_Order();

                    if (dodanie)
                    { Ord.order_no_cust = zl.client_order_no + tekst; }
                    else
                    { Ord.order_no_cust = zl.client_order_no.Replace(tekst,""); }
                     
                    api.CHANGE_ORDER(zl.ipo_order_id, Ord);

                }

            return "OK";


        }

        [WebMethod]
        public void _Dodaj_reczne_zapisy_z_tabeli()
        {

            DBDataContext db2 = new DBDataContext();


            DB2DataContext db = new DB2DataContext();

            var do_utworzenia = from c in db.IPO_reczne_ksiegowanie
                                where c.zaksiegowane == false
                                select c;

            API.IPO_API api = new API.IPO_API();

            foreach (var rec in do_utworzenia)
            {
                var zlec = api.GET_ORDER_BY_IPO_NO((int)rec.NR_zlecenia_IPO);

                var slownik = (from g in db2.SLOWNIK_1 where g.IMLITM == rec.Kod_materialu select g).Single();
                var mag_podst = (from g in db2.IPO_MAGAZYN_PODSTAWOWY_PWs where g.LIITM == double.Parse(zlec.item_id) select g.mag_ipo).Single();

                var nrec = new IPO_ZDAWKA_PW();

                nrec.Czy_korygowany = true;
                nrec.Data_utworzenia_poz = DateTime.Now;
                nrec.Ilosc = (double)rec.Ilosc;
                nrec.IPO_ID_POZYCJI = -1;
                nrec.ITM = slownik.IMITM.ToString();
                nrec.typ = 0;
                nrec.Zaksiegowany_JDE = false;
                nrec.JM = "SZ";
                nrec.Kod_zlecenia_klienta = zlec.client_order_no;
                nrec.Koszt_IPO = 0;
                nrec.Koszt_mat_IPO = 0;
                nrec.Magazyn_IPO = mag_podst;
                nrec.Nazwa_pozycji = slownik.NAZWA;
                nrec.Nr_indeksu = rec.Kod_materialu;
                nrec.Nr_zam_klienta = rec.Kod_materialu;
                nrec.Nr_seryjny = "";
                nrec.Nr_zam_klienta = "";
                nrec.Nr_zlecenia_IPO = rec.NR_zlecenia_IPO;
                nrec.Powod_korekty = "DODANE " + DateTime.Now.ToString() + " RĘCZNIE - NA PODST P.KORBEL$$$$";
                nrec.RW_PW = rec.Typ_dok;

                db.IPO_ZDAWKA_PW.InsertOnSubmit(nrec);

                rec.zaksiegowane = true;
                db2.SubmitChanges();
                db.SubmitChanges();
                 
            }


        }



        public void _RAPORTY_temp_popraw_ZG_EP(DateTime _from, DateTime _to)
        {

            API.IPO_API api = new API.IPO_API();
            DB2DataContext db = new DB2DataContext();
            var zlec = from g in db.IPO_ZDAWKA_PW
                       where g.Data_utworzenia_poz >= _from && g.Data_utworzenia_poz <= _to
                       select g;
            foreach (var z in zlec)
            {
                var z_ipo = api.GET_ORDER_BY_IPO_NO((int)z.Nr_zlecenia_IPO);
                 if (z_ipo.ipo_main_order_id != z_ipo.ipo_order_id)
                {
                    var main_z_ipo = api.GET_ORDER(z_ipo.ipo_main_order_id);
                    z.Powod_korekty = " EP%" + z.Powod_korekty + z.Nr_zlecenia_IPO.ToString() + "%EP";
                    z.Nr_zlecenia_IPO = main_z_ipo.ipo_order_no;
                    db.SubmitChanges();

                }
            }
        }
       
        public string _F4111_temp_popraw_ZG_EP(int _from, int _to)
        {

            API.IPO_API api = new API.IPO_API();
            DBDataContext db = new DBDataContext();
            var zlec = from g in db.F4111
                       where  g.ILDGL >=_from && g.ILDGL <=_to && (g.ILDCT=="I2" || g.ILDCT == "I3" || g.ILDCT == "RW" ) && g.ILDOCO !=0
                       select g;
            foreach (var z in zlec)
            {
                var z_ipo = api.GET_ORDER_BY_IPO_NO((int)z.ILDOCO);
                if (z_ipo.ipo_main_order_id != z_ipo.ipo_order_id)
                {
                    var main_z_ipo = api.GET_ORDER(z_ipo.ipo_main_order_id);
                    //z.Powod_korekty = " EP%" + z.Powod_korekty + z.Nr_zlecenia_IPO.ToString() + "%EP";
                    z.ILDOCO = main_z_ipo.ipo_order_no;
                    db.SubmitChanges();

                }
            }

            return "OK";
        }

        [WebMethod]
        public string Aktualizuj_rozpisy_z_dok_I3(DateTime _od, DateTime _do)
        {
            DB2DataContext rap = new DB2DataContext();
            var I3s = from c in rap.IPO_ZDAWKA_PW
                      where c.RW_PW == "PW" && c.Data_ksiegowania_JDE.Value.Date >= _od && c.Data_ksiegowania_JDE.Value.Date <= _do && c.do_kontroli == 0
                      select c;


            foreach (var i3 in I3s)
            {

                string test = Dodaj_rozpis_do_Tabeli((int)i3.Nr_zlecenia_IPO, 0, (double)i3.Ilosc);

                if (test == "BŁĄD!!!") { i3.do_kontroli = -1; }
                else
                { i3.do_kontroli = 1; }

                rap.SubmitChanges();
               
            }




            return "OK";
        }


        [WebMethod]
        public string Dodaj_rozpis_do_Tabeli(int Nr_zlecenia_IPO, int ITM, double qty)
        {
            DBDataContext db2008 = new DBDataContext();
            DB2DataContext raporty = new DB2DataContext();

            if (ITM==0)
            {
                API.IPO_API api = new API.IPO_API();
                var zlec = api.GET_ORDER_BY_IPO_NO(Nr_zlecenia_IPO);
                ITM = int.Parse(zlec.item_id);

            }
            var indeksy = from c in db2008.SLOWNIK_1 where c.IMITM == ITM select c;
            if (indeksy.Count() != 1) { return "BŁĄD!!!";  }

            var dane = indeksy.Single();
            
            if (dane.KOD_PLAN == "MONT.VALVEX")
            {
                var rozpis = db2008.RozNormJdenPoziom_JedenWyrob(dane.IMLITM, qty);
                foreach (var linia in rozpis)
                {

                    IPO_ZDAWKA_PW_NORMA norma = new IPO_ZDAWKA_PW_NORMA();
                    norma.Data_utworzenia_poz = DateTime.Now;
                    norma.Data_ksiegowania_JDE = DateTime.Now;
                    norma.RW_PW = "RW";
                    if (qty>0 && linia.Ilość<0) norma.RW_PW = "PU";
                    if (qty < 0 && linia.Ilość > 0) norma.RW_PW = "PU";

                    norma.Nr_zlecenia_IPO = Nr_zlecenia_IPO;
                    norma.Nr_indeksu = linia.Kod_podzespołu;
                    norma.Ilosc = -linia.Ilość;
                    norma.Zaksiegowany_JDE = true;
                    norma.Nazwa_pozycji = linia.Opis_podzespołu;
                    raporty.IPO_ZDAWKA_PW_NORMA.InsertOnSubmit(norma);
                    raporty.SubmitChanges();

                }

            }
            else
            {

                var rozpis = from c in db2008.IPO_Rozpis_mat
                             where c.wyrob_s == dane.IMITM
                             select c;

                foreach (var l in rozpis)
                {

                    IPO_ZDAWKA_PW_NORMA norma = new IPO_ZDAWKA_PW_NORMA();
                    norma.Data_utworzenia_poz = DateTime.Now;
                    norma.Data_ksiegowania_JDE = DateTime.Now;
                    norma.RW_PW = "RW";
                    if (l.ilosc < 0) norma.RW_PW = "PU";


                    norma.Nr_zlecenia_IPO = Nr_zlecenia_IPO;
                    norma.Nr_indeksu = l.skladnik_l;
                    norma.Ilosc = -l.ilosc*qty;
                    norma.Zaksiegowany_JDE = true;
                    norma.Nazwa_pozycji = l.Nazwa_s;
                    raporty.IPO_ZDAWKA_PW_NORMA.InsertOnSubmit(norma);
                    raporty.SubmitChanges();

                }






            }




            return "OK";
        }



        [WebMethod]
        public string Popraw_zuzycie_montaz_valvex(DateTime od, DateTime _do, string magazyn)
        {
            DB2DataContext db = new DB2DataContext();
            db.CommandTimeout = 10000000;
            var zlecenia = (from c in db.IPO_ZDAWKA_PW_3
                            where c.Magazyn_IPO != "PROD_P41MONTARMINST" && c.Data_utworzenia_poz >= od && c.Data_utworzenia_poz <= _do && c.Hala == "[H4]" && c.Magazyn_IPO.StartsWith(magazyn) && c.RW_PW == "RW"
                            //where c.Magazyn_IPO != "PROD_P41MONTARMINST" && c.Data_utworzenia_poz >= od && c.Data_utworzenia_poz <= _do && c.Hala == "[H4]" && c.Magazyn_IPO.StartsWith(magazyn) && c.RW_PW == "RW"
                            select new { c.ID, c.Nr_zlecenia_IPO , c.Magazyn_IPO, c.Nr_indeksu, c.Ilosc }).Distinct();

            foreach (var zlec in zlecenia)
            {
                var suma = (from c in db.IPO_ZDAWKA_PW where c.Magazyn_IPO==zlec.Magazyn_IPO  && c.Nr_indeksu == zlec.Nr_indeksu && c.Nr_zlecenia_IPO == zlec.Nr_zlecenia_IPO select c.Ilosc).Sum();
                if (suma !=0)
                {
                    var rekord = (from c in db.IPO_ZDAWKA_PW where c.ID == zlec.ID select c).Single();
                    var rekord_usun = Clone<IPO_ZDAWKA_PW>(rekord);
                    var rekord_nowy = Clone<IPO_ZDAWKA_PW>(rekord);

                    rekord_usun.Ilosc = rekord_usun.Ilosc * -1;
                    rekord_usun.Koszt_IPO = rekord_usun.Koszt_IPO * -1;
                    rekord_usun.Zaksiegowany_JDE = false;
                    rekord_usun.Data_utworzenia_poz = DateTime.Now;
                    rekord_usun.Powod_korekty = "automatyczna korekta magazynu";

                    rekord_nowy.Zaksiegowany_JDE = false;
                    rekord_nowy.Data_utworzenia_poz = DateTime.Now;
                    rekord_nowy.Magazyn_IPO = "PROD_P41MONTARMINST";
                    rekord_nowy.Powod_korekty = "automatyczna korekta magazynu";

                    db.IPO_ZDAWKA_PW.InsertOnSubmit(rekord_usun);
                    db.IPO_ZDAWKA_PW.InsertOnSubmit(rekord_nowy);

                    db.SubmitChanges();

                }


            }




            return "0";
        }


        [WebMethod]
        public string AktualizacjaHaliNEW()
        {
            API.IPO_API api = new API.IPO_API();
            DBDataContext db = new DBDataContext();

            var lista = (from c in db.IPO_HALE_DO_POPRAWY
                        select new { c.Nr_zlec_IPO }).Distinct();

            foreach (var doc in lista)
            {

                IPO_Order order = api.GET_ORDER_BY_IPO_NO((int)doc.Nr_zlec_IPO);
                

                if (order.ipo_order_no != 0 && !(order.item_id is null))
                {
                    
                    var kody = from c in db.SLOWNIK_1
                               where c.IMITM == Double.Parse(order.item_id)
                               select c;
                    if (kody.Count() == 1)
                    {
                        var kod_planisty = kody.Single();
                        string hala = "[?]";
                        if (kod_planisty.KOD_PLAN == "OBRÓBKA") hala = "[H2]";
                        if (kod_planisty.KOD_PLAN == "PRASOWNIA") hala = "[H1]";
                        if (kod_planisty.KOD_PLAN == "MONT.VALVEX") hala = "[H4]";
                        if (kod_planisty.KOD_PLAN == "SZLIFIERNIA") hala = "[H5]";
                        if (kod_planisty.KOD_PLAN == "ODLEWNIA") hala = "[H1]";
                        if (kod_planisty.KOD_PLAN == "PO.USL") hala = "[H2]";

                        db.IPO_POPRAW_HALE_TREX((int)doc.Nr_zlec_IPO, hala, 0);
                    }
                }
            }




            return "ok";
        }

        

        
        public long AktualizacjaHaliPW(string Hala, DateTime od_daty, DateTime do_daty)
        {
           
            DBDataContext db2008 = new DBDataContext();
            var pw = from c in db2008.IPO_PWs
                     where c.Data_ks >= od_daty && c.Data_ks <= do_daty && c.HALA == Hala
                     select c;


            int od_daty_jde = (int)db2008.Date2JT(od_daty);

            foreach (var dpw in pw)
            {

                db2008.IPO_POPRAW_HALE_TREX((int)dpw.Nr_zlec_IPO, dpw.HALA, od_daty_jde );

            }



            return pw.LongCount();
        }
        // string[] Lines, int nitm
        [WebMethod]
        public byte[] IPO_GET_KKC(int nr_zlec, string id_prac, string oper, string id_masz)
        {
            DBDataContext dbx = new DBDataContext();


            string ARIALUNI_TFF = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "ARIAL.TTF");

            //Create a base font object making sure to specify IDENTITY-H
            BaseFont bf = BaseFont.CreateFont(ARIALUNI_TFF, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

            //Create a specific font object
            iTextSharp.text.Font f = new iTextSharp.text.Font(bf, 8, iTextSharp.text.Font.NORMAL);
            iTextSharp.text.Font fb = new iTextSharp.text.Font(bf, 13, iTextSharp.text.Font.NORMAL);
            iTextSharp.text.Font fn = new iTextSharp.text.Font(bf, 10, iTextSharp.text.Font.NORMAL);
            iTextSharp.text.Font fvb = new iTextSharp.text.Font(bf, 16, iTextSharp.text.Font.BOLDITALIC);




            MemoryStream ms = new MemoryStream();
            Document document = new Document(PageSize.A4, 10f, 10f, 10f, 20f);
            PdfWriter writer = PdfWriter.GetInstance(document, ms);

            IPO_Order ord = IPO_GET_ORDER(nr_zlec);
            Item itm = IPO_GET_ITEM_BY_ITM(int.Parse(ord.item_id));

            document.Open();
            document.SetPageSize(iTextSharp.text.PageSize.A4.Rotate());
            document.NewPage();


            Paragraph nri = new Paragraph("39WF01 rev. 27.10.2016", f);
            nri.Alignment = 2;
            document.Add(nri);
            nri = new Paragraph("KKC - KARTA KONTROLI CYKLU DO ZLECENIA IPO NR: " + nr_zlec.ToString(), fb);
            nri.Alignment = 1;

            document.Add(nri);
            document.Add(new Paragraph("NAZWA: " + itm.name, fb));
            document.Add(new Paragraph("INDEKS: " + itm.index, fb));
            document.Add(new Paragraph("OPERACJA:         " + oper, fvb));
            document.Add(new Paragraph("NR MASZYNY:       " + id_masz, fvb));
            var marsz = from c in dbx.IPO_Marszruties
                        orderby c.Typ, c.Nr_oper
                        where c.Indeks == itm.index && c.Typ == "M"
                        select c;


            string co_ile = "";

            co_ile = "Kontrolować zgodnie z instrukcją 39W.";

            document.Add(new Paragraph(" ", f));
            document.Add(new Paragraph("Wymiary kontrolne. " + co_ile, fb));
            document.Add(new Paragraph(" ", f));

            var kart = (from c in marsz.Where(x => x.Nr_Karty != "") select new { KARTA = c.Nr_Karty }).Distinct();

            PdfPTable kkc = new PdfPTable(23);
            float[] widths = new float[] { 10f, 25f, 40f, 25f, 25f, 25f, 25f, 25f, 25f, 25f, 25f, 25f, 25f, 25f, 25f, 25f, 25f, 25f, 25f, 25f, 25f, 25f, 25f };
            kkc.SetWidths(widths);
            kkc.WidthPercentage=100;
            PdfPCell cell;
            cell = new PdfPCell(new Phrase("Nr", f)); kkc.AddCell(cell);
            cell = new PdfPCell(new Phrase("Nr_karty", f)); kkc.AddCell(cell);
            cell = new PdfPCell(new Phrase("Wymiar kontr.", f)); kkc.AddCell(cell);
            cell = new PdfPCell(new Phrase("Pierw, szt 1-...", f)); kkc.AddCell(cell);
            for (int t = 1; t < 20; t++)
            {
                cell = new PdfPCell(new Phrase("Spr" + t.ToString(), f)); kkc.AddCell(cell);
            }


            if (kart.Count() > 0)
            {
                var cykl = from g in kart
                           join k in dbx.F00192s.Where(x => x.CFSY == "48" && x.CFRT == "SN") on g.KARTA equals k.CFKY
                           select new { g.KARTA, NR = k.CFLINS / 100, k.CFDS80 };



                foreach (var c in cykl)
                {
                    cell = new PdfPCell(new Phrase(c.NR.ToString(), f)); kkc.AddCell(cell);
                    cell = new PdfPCell(new Phrase(c.KARTA, f)); kkc.AddCell(cell);
                    cell = new PdfPCell(new Phrase(c.CFDS80, f)); kkc.AddCell(cell);
                    cell = new PdfPCell(new Phrase(".        .         .        ", f)); kkc.AddCell(cell);
                    for (int t = 1; t < 20; t++)
                    {
                        cell = new PdfPCell(new Phrase(" ", f)); kkc.AddCell(cell);
                    }

                }
                if (cykl.Count()== 0) // jeżęli nie ma wymiarów kontrolnych, to wstaw 5 pustych wierszy
                {
                    for (int w = 1; w < 8; w++)
                    {
                        cell = new PdfPCell(new Phrase(" ", f)); kkc.AddCell(cell);
                        cell = new PdfPCell(new Phrase(" ", f)); kkc.AddCell(cell);
                        cell = new PdfPCell(new Phrase(" ", f)); kkc.AddCell(cell);
                        cell = new PdfPCell(new Phrase(".        .         .        ", f)); kkc.AddCell(cell);
                        for (int t = 1; t < 20; t++)
                        {
                            cell = new PdfPCell(new Phrase(" ", f)); kkc.AddCell(cell);
                        }



                    }

                }




                document.Add(kkc);
                document.Add(new Paragraph(" ", f));
                document.Add(new Paragraph("Utworzono: " + DateTime.Now.ToString(), f));
                document.Add(new Paragraph("ZALECENIA I UWAGI USTAWIACZA:  ", f));

                document.Close();

                writer.Close();


                //Process.Start(file_path);


            }

            return ms.ToArray();

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

        public string RETURN_GFA_ZPL(Bitmap Bmp)
        { 

            var hex = new StringBuilder( );
            int Bytes = 0;
            Color c;
            decimal bytes_width = Math.Ceiling((decimal)Bmp.Width / 8m);
            int nwidth = (int)bytes_width * 8;


            for (int y = 0; y < Bmp.Height; y++)
                for (int x = 0; x < nwidth; x=x+8)
                {
                    int b = 0;
                    for (int cx = 0; cx < 8; cx++)
                    {

                        if ((x + cx) < Bmp.Width)
                        {
                            c = Bmp.GetPixel(x + cx, y);
                            
                            if (c.R==0)
                            {
                                b = b + (int)Math.Pow( 2,(7-cx));
                            }
                        }
                    }
                    hex.AppendFormat("{0:x2}", b);
                    Bytes++;

                }


            return "^GFA,"+Bytes.ToString()+","+ Bytes.ToString()+","+(nwidth/8).ToString()+","+hex.ToString();
        }

        public Bitmap ConvertToBitmap(string fileName)
        {
            Bitmap bitmap;
            using (Stream bmpStream = System.IO.File.Open(fileName, System.IO.FileMode.Open))
            {
                System.Drawing.Image image = System.Drawing.Image.FromStream(bmpStream);

                bitmap = new Bitmap(image);

            }
            return bitmap;
        }

        [WebMethod]
        public string RETURN_GFA_ZPL(string nr_rysunku)
        {
           



            Bitmap original = (Bitmap)GenPDF.GetImage(nr_rysunku);

             
            int nwidth = 450;
            int nheight = (original.Height * nwidth) / original.Width;
            Bitmap resized = new Bitmap(original, new Size( nwidth ,  nheight ));
             

            Bitmap Bmp = FloydSteinberg.FloydSteinbergDither.Process(resized, new Color[] { Color.FromArgb(0, 0, 0), Color.FromArgb(255, 255, 255) });


            var hex = new StringBuilder();
            int Bytes = 0;
            Color c;


            decimal bytes_width = Math.Ceiling((decimal)Bmp.Width / 8m);

            nwidth = (int)bytes_width * 8;


            for (int y = 0; y < Bmp.Height; y++)
                for (int x = 0; x < nwidth; x = x + 8)
                {
                    int b = 0;
                    for (int cx = 0; cx < 8; cx++)
                    {
                        //jezeli się mieści w faktycznym wymiarze rysunku to rysuj pixele
                        if ((x + cx) < Bmp.Width)
                        {
                            c = Bmp.GetPixel(x + cx, y);
                            if (c.R == 0) b = b + (int)Math.Pow(2, (7 - cx)); //sprawdzamy tylko czerwony kolor - bo 1BPP to 0,0,0 lub 255.255.255 
                            
                        }
                    }
                    hex.AppendFormat("{0:x2}", b);
                    Bytes++;

                }


            return "^GFA," + Bytes.ToString() + "," + Bytes.ToString() + "," + (nwidth / 8).ToString() + "," + hex.ToString();

             
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="nr_zlecenia"></param>
        /// <param name="_logged_user"></param>
        /// <returns></returns>
        [WebMethod]
        public byte[] IPO_OLD_METKI(int nr_zlecenia, string _logged_user, string[] Lines)
        {
            metka m = new metka();
            API.IPO_Order zlecenie = IPO_GET_ORDER(nr_zlecenia);
            if (zlecenie.ipo_order_id != 0)
            {
               
                double itm = double.Parse(zlecenie.item_id);
               // if (zlecenie.ipo_order_no == 0) itm = nitm;

                DBDataContext db = new DBDataContext();
                var idx = (from c in db.SLOWNIK_1
                           where c.IMITM == itm
                           select c).Single();
                string w_logged_user = WindowsIdentity.GetCurrent().Name;

                List<API.IPO_BOM_Line> bom = IPO_BOM_ITEM(idx.IMLITM);
                var dane = db.Dane_do_metki(idx.IMLITM).Take(1).Single();


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
                

               // jeżeli zalogowany user zawiera znak % to drukuj tylko poler wyk.
                //m.nr_zlec_galw = zlec_galw;
                m.komentarz = zlecenie.order_no_cust + " " + _logged_user;

                string kod_materialu = "BRAK";
                if (bom.Count() > 0)
                {
                    var idxm = (from c in db.SLOWNIK_1
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
                m.nr_zlec_szlif = nr_zlecenia;
                
            }
            return GenPDF.GenPDFFile_old(m);


        }
        [WebMethod]
        public byte[] IPO_Metki(int zlec_szlif, int zlec_galw, string _logged_user)
        {
            metka m = new metka();
            if (zlec_galw == 0) zlec_galw = zlec_szlif + 1;

            API.IPO_Order zlecenie = IPO_GET_ORDER(zlec_galw);
            if (zlecenie.ipo_order_id != 0)
            {

                double itm = double.Parse(zlecenie.item_id);

                DBDataContext db = new DBDataContext();
                var idx = (from c in db.SLOWNIK_1
                           where c.IMITM == itm
                           select c).Single();
                _logged_user = WindowsIdentity.GetCurrent().Name;

                List<API.IPO_BOM_Line> bom = IPO_BOM_ITEM(idx.IMLITM);

                var dane = db.Dane_do_metki(idx.IMLITM).Take(1).Single();


                m.kod_zlecenia = idx.IMLITM;
                m.ilosc_szt = new double[1] { zlecenie.quantity };
                m.il_stron = 1;
                m.nr_zlec_galw = zlec_galw;
                m.komentarz = zlecenie.order_no_cust;

                string kod_materialu = "BRAK";
                if (bom.Count() > 0)
                {
                    var idxm = (from c in db.SLOWNIK_1
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
                m.userid = _logged_user;

               





            }
            return GenPDF.GenMetka_GAL_SZL(m);

        }

        [WebMethod]
        public int JDE_Drukuj_prosta_metke(string ip_drukarki, int port, string tytul, string text)
        {
            string tekst = "^XA^LL1350^CI28^CF0,290^FO20,190^FD%TITLE%^FS^FS^CF0,80^FO20,700^TBN,700,350^FD%TEXT%^FS^XZ";
            tekst = tekst.Replace("%TITLE%", tytul).Replace("%TEXT%", text);


            //tekst = tekst.Replace("ż", "z").Replace("ó", "o").Replace("ł", "l").Replace("ć", "c").Replace("ś", "s").Replace("ą", "a").Replace("ź", "z").Replace("ń", "n").Replace("ę", "e");
            //tekst = tekst.Replace("Ż", "Z").Replace("Ó", "O").Replace("Ł", "L").Replace("Ć", "C").Replace("Ś", "S").Replace("Ą", "A").Replace("Ź", "Z").Replace("Ń", "N").Replace("Ę", "E");

            SendToZebra(ip_drukarki, port, tekst);

            return 0;
        }


        [WebMethod]
        public int ZPLDrukujText(string ip_drukarki,int port, string text)
        {
            string tekst = "^XA^LL1350^CI28^CF0,120^FO20,40^TBN,700,700^FD%TEXT%^FS^XZ";
            tekst = tekst.Replace("%TEXT%", text);


            //tekst = tekst.Replace("ż", "z").Replace("ó", "o").Replace("ł", "l").Replace("ć", "c").Replace("ś", "s").Replace("ą", "a").Replace("ź", "z").Replace("ń", "n").Replace("ę", "e");
            //tekst = tekst.Replace("Ż", "Z").Replace("Ó", "O").Replace("Ł", "L").Replace("Ć", "C").Replace("Ś", "S").Replace("Ą", "A").Replace("Ź", "Z").Replace("Ń", "N").Replace("Ę", "E");

            SendToZebra(ip_drukarki, port, tekst);

            return 0;



            
        }

        [WebMethod]
        public int ZPLDrukuj_tekst_3(string ip_drukarki, int port, string text)
        {
            string tekstm = "^XA^LL1350^CI28^FO50,550^GB700,1,3^FS^BY3,2,80" +
            "^CF0,80" +
            "^FO30,560^TBN,750,700^FD%TEXT%^FS" +
            "^FO50,290^GB700,1,3^FS^BY3,2,80" +
            "^CF0,80^FO30,300^TBN,750,500^FD%TEXT%^FS" +
            "^FO50,30^GB700,1,3^FS^BY3,2,80" +
            "^CF0,80^FO30,40^TBN,750,450^FD%TEXT%^FS" +
            "^XZ";
            tekstm = tekstm.Replace("%TEXT%", text);

            SendToZebra(ip_drukarki, port, tekstm);

            return 0;
        }
        [WebMethod]
        public int JDE_Drukuj_metkę(string ip_drukarki, int port, string litm, string typ_metki, int ilosc)
        {
            string nazwa = "BLAD!";
            string rys = "BRAK!";
            DBDataContext db = new DBDataContext();
            var nazwa_S = from g in db.SLOWNIK_1 where g.IMLITM == litm select new { g.NAZWA, g.IMLITM };
            var rys_S = from g in db.F4101s where g.IMLITM == litm select new { g.IMINMG };

            if (nazwa_S.Count() == 1) { nazwa = nazwa_S.First().NAZWA; litm = nazwa_S.First().IMLITM; }
            if (rys_S.Count() == 1) { rys = rys_S.First().IMINMG;}
            //nazwa = nazwa.Replace("ż", "z").Replace("ó", "o").Replace("ł", "l").Replace("ć", "c").Replace("ś", "s").Replace("ą", "a").Replace("ź", "z").Replace("ń", "n").Replace("ę", "e");
            //nazwa = nazwa.Replace("Ż", "Z").Replace("Ó", "O").Replace("Ł", "L").Replace("Ć", "C").Replace("Ś", "S").Replace("Ą", "A").Replace("Ź", "Z").Replace("Ń", "N").Replace("Ę", "E");

            if (nazwa.Length > 39) nazwa = nazwa.Substring(0, 38);

            string tekstm = "^XA^LL1350^CI28^FO50,550^GB700,1,3^FS^BY3,2,80 " +
            "^FO50,570^BC^FD%ITEMID%^FS^CF0,50" +
            "^FO30,690^FD%TEXT%^FS" +
            "^CF0,70^FO50,740^FD%ITEMID%^FS" +
            "^CF0,20^FO550,760^FD%DATE%^FS" +
            "^FO50,290^GB700,1,3^FS^BY3,2,80^FO50,310^BC^FD%ITEMID%^FS" +
            "^CF0,50^FO30,430^FD%TEXT%^FS" +
            "^CF0,70^FO50,480^FD%ITEMID%^FS" +
            "^CF0,20^FO550,500^FD%DATE%^FS" +
            "^FO50,30^GB700,1,3^FS^BY3,2,80^FO50,50^BC^FD%ITEMID%^FS" +
            "^CF0,50^FO30,170^FD%TEXT%^FS" +
            "^CF0,70^FO50,220^FD%ITEMID%^FS" +
            "^CF0,20^FO550,240^FD%DATE%^FS^PQ%QTY%^XZ";
            tekstm = tekstm.Replace("%TEXT%", nazwa);
            tekstm = tekstm.Replace("%ITEMID%", litm);
            tekstm = tekstm.Replace("%DATE%", DateTime.Now.ToString());
            tekstm = tekstm.Replace("%QTY%", ilosc.ToString());


            string zkod;
            if (litm.Length > 9)
            {
                zkod = "^CF0,100^FO20,40^FD%ITEMID%^FS";
            }
            else
            {
                zkod = "^CF0,170^FO20,40^FD%ITEMID%^FS";

            }

            string tekst = "^XA^LL1350^CI28" +
                zkod +
            "^FS^CF0,70" +
            "^FO40,265" +
            "^TBN,700,350" +
            "^FD%TEXT%^FS" +
            "^FO50,400^GB700,1,3^FS^FO30,410%GRAF%" +
            "^BY3,1,50" +
            "^FO50,175^BC^FD%ITEMID%^FS" +
            "^CF0,40" +
            "^FO550,480^FDSZTUKI:^FS" +
            "^FO520,755^FD%DATE%^FS" +
            "^PQ%QTY%^XZ";


            tekst = tekst.Replace("%QTY%", ilosc.ToString());
            tekst = tekst.Replace("%TEXT%", nazwa);
            tekst = tekst.Replace("%ITEMID%", litm);
            tekst = tekst.Replace("%DATE%", DateTime.Now.ToString());
            if (typ_metki == "m")
            { SendToZebra(ip_drukarki, port, tekstm); }
            else
            {
                //duza metka - dodaj grafikę


                tekst = tekst.Replace("%GRAF%", RETURN_GFA_ZPL(rys));

                SendToZebra(ip_drukarki, port, tekst);


            }

            return 0;

        }
        [WebMethod]
        



        /// <summary>
        /// Copies a bitmap into a 1bpp/8bpp bitmap of the same dimensions, fast
        /// </summary>
        /// <param name="b">original bitmap</param>
        /// <param name="bpp">1 or 8, target bpp</param>
        /// <returns>a 1bpp copy of the bitmap</returns>
        static System.Drawing.Bitmap CopyToBpp(System.Drawing.Bitmap b, int bpp)
        {
            if (bpp != 1 && bpp != 8) throw new System.ArgumentException("1 or 8", "bpp");

            // Plan: built into Windows GDI is the ability to convert
            // bitmaps from one format to another. Most of the time, this
            // job is actually done by the graphics hardware accelerator card
            // and so is extremely fast. The rest of the time, the job is done by
            // very fast native code.
            // We will call into this GDI functionality from C#. Our plan:
            // (1) Convert our Bitmap into a GDI hbitmap (ie. copy unmanaged->managed)
            // (2) Create a GDI monochrome hbitmap
            // (3) Use GDI "BitBlt" function to copy from hbitmap into monochrome (as above)
            // (4) Convert the monochrone hbitmap into a Bitmap (ie. copy unmanaged->managed)

            int w = b.Width, h = b.Height;
            IntPtr hbm = b.GetHbitmap(); // this is step (1)
                                         //
                                         // Step (2): create the monochrome bitmap.
                                         // "BITMAPINFO" is an interop-struct which we define below.
                                         // In GDI terms, it's a BITMAPHEADERINFO followed by an array of two RGBQUADs
            BITMAPINFO bmi = new BITMAPINFO();
            bmi.biSize = 40;  // the size of the BITMAPHEADERINFO struct
            bmi.biWidth = w;
            bmi.biHeight = h;
            bmi.biPlanes = 1; // "planes" are confusing. We always use just 1. Read MSDN for more info.
            bmi.biBitCount = (short)bpp; // ie. 1bpp or 8bpp
            bmi.biCompression = BI_RGB; // ie. the pixels in our RGBQUAD table are stored as RGBs, not palette indexes
            bmi.biSizeImage = (uint)(((w + 7) & 0xFFFFFFF8) * h / 8);
            bmi.biXPelsPerMeter = 1000000; // not really important
            bmi.biYPelsPerMeter = 1000000; // not really important
                                           // Now for the colour table.
            uint ncols = (uint)1 << bpp; // 2 colours for 1bpp; 256 colours for 8bpp
            bmi.biClrUsed = ncols;
            bmi.biClrImportant = ncols;
            bmi.cols = new uint[256]; // The structure always has fixed size 256, even if we end up using fewer colours
            if (bpp == 1) { bmi.cols[0] = MAKERGB(0, 0, 0); bmi.cols[1] = MAKERGB(255, 255, 255); }
            else { for (int i = 0; i < ncols; i++) bmi.cols[i] = MAKERGB(i, i, i); }
            // For 8bpp we've created an palette with just greyscale colours.
            // You can set up any palette you want here. Here are some possibilities:
            // greyscale: for (int i=0; i<256; i++) bmi.cols[i]=MAKERGB(i,i,i);
            // rainbow: bmi.biClrUsed=216; bmi.biClrImportant=216; int[] colv=new int[6]{0,51,102,153,204,255};
            //          for (int i=0; i<216; i++) bmi.cols[i]=MAKERGB(colv[i/36],colv[(i/6)%6],colv[i%6]);
            // optimal: a difficult topic: http://en.wikipedia.org/wiki/Color_quantization
            // 
            // Now create the indexed bitmap "hbm0"
            IntPtr bits0; // not used for our purposes. It returns a pointer to the raw bits that make up the bitmap.
            IntPtr hbm0 = CreateDIBSection(IntPtr.Zero, ref bmi, DIB_RGB_COLORS, out bits0, IntPtr.Zero, 0);
            //
            // Step (3): use GDI's BitBlt function to copy from original hbitmap into monocrhome bitmap
            // GDI programming is kind of confusing... nb. The GDI equivalent of "Graphics" is called a "DC".
            IntPtr sdc = GetDC(IntPtr.Zero);       // First we obtain the DC for the screen
                                                   // Next, create a DC for the original hbitmap
            IntPtr hdc = CreateCompatibleDC(sdc); SelectObject(hdc, hbm);
            // and create a DC for the monochrome hbitmap
            IntPtr hdc0 = CreateCompatibleDC(sdc); SelectObject(hdc0, hbm0);
            // Now we can do the BitBlt:
            BitBlt(hdc0, 0, 0, w, h, hdc, 0, 0, SRCCOPY);
            // Step (4): convert this monochrome hbitmap back into a Bitmap:
            System.Drawing.Bitmap b0 = System.Drawing.Bitmap.FromHbitmap(hbm0);
            //
            // Finally some cleanup.
            DeleteDC(hdc);
            DeleteDC(hdc0);
            ReleaseDC(IntPtr.Zero, sdc);
            DeleteObject(hbm);
            DeleteObject(hbm0);
            //
            return b0;

        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern int DeleteDC(IntPtr hdc);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern int BitBlt(IntPtr hdcDst, int xDst, int yDst, int w, int h, IntPtr hdcSrc, int xSrc, int ySrc, int rop);
        static int SRCCOPY = 0x00CC0020;

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO bmi, uint Usage, out IntPtr bits, IntPtr hSection, uint dwOffset);
        static uint BI_RGB = 0;
        static uint DIB_RGB_COLORS = 0;
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public uint biSize;
            public int biWidth, biHeight;
            public short biPlanes, biBitCount;
            public uint biCompression, biSizeImage;
            public int biXPelsPerMeter, biYPelsPerMeter;
            public uint biClrUsed, biClrImportant;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 256)]
            public uint[] cols;
        }

        static uint MAKERGB(int r, int g, int b)
        {
            return ((uint)(b & 255)) | ((uint)((r & 255) << 8)) | ((uint)((g & 255) << 16));
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


    }
}