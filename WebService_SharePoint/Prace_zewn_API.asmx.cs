using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Services;

namespace WebService_SharePoint
{
    /// <summary>
    /// Summary description for Prace_zewn_API
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class Prace_zewn_API : System.Web.Services.WebService
    {
        [WebMethod]
        public void Log_operation(int order_id, int id_oper,int ilosc, string komentarz, string operacja, string opis_operacji)
        {
            var db = new DB2DataContext();
            IPO_LOG_OPERACJI_ZEWN log = new IPO_LOG_OPERACJI_ZEWN();
            log.Data_zdarzenia = DateTime.Now;
            log.id_operacji = id_oper;
            log.Ilosc = ilosc;
            log.Komentarz = komentarz;
            log.Nr_zlecenia = order_id;
            log.Operacja = operacja;
            log.Opis_operacji = opis_operacji;
            db.IPO_LOG_OPERACJI_ZEWNs.InsertOnSubmit(log);
            db.SubmitChanges();




        }




            [WebMethod]
        public List<API.external_tasks_list> GetTasksToDoByOrder(int order_id)
        {



            API.IPO_API api = new API.IPO_API();
            var lista = api.GET_EXTERNAL_TASKS_LIST();
            return lista.Where(x => x.ipo_order_id == order_id).ToList();
            
        }


        [WebMethod]
        public List<API.external_tasks_list> GetTasksToDo(string okod)
        {
            API.IPO_API api = new API.IPO_API();
            var lista = api.GET_EXTERNAL_TASKS_LIST();
            
            return lista;
        }

        [WebMethod]
        public string StartTask(int task_id, DateTime stop_time)
        {

            API.IPO_API api = new API.IPO_API();

            HttpResponseMessage resp =  api.PUT_START_EXTERNAL_TASK(task_id,stop_time);
             


            return resp.StatusCode.ToString();
        }

        [WebMethod]
        public bool EndTask(int task_id)
        {
            API.IPO_API api = new API.IPO_API();

            return api.POST_END_EXTERNAL_TASK(task_id);



        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="task_id"></param>
        /// <param name="qty"></param>
        /// <param name="report_type">Typ raportu (0 – raport ilości; 1 – brak wewnętrzny; 2 – brak zewnętrzny; 8 – zmiana pola odkładczego)</param>
        /// <returns></returns>
        [WebMethod]
        public string Report_task(int task_id, int qty,int report_type)
        {
            API.ExternalTaskReport rep = new API.ExternalTaskReport();
            rep.quantity = qty;
            rep.report_type = report_type;
            rep.report_time = DateTime.Now;
            rep.storage_place = "";
            rep.employee_id = "2";
            


            API.IPO_API api = new API.IPO_API();

            return api.PATCH_SEND_REPORT(task_id, rep);
        }
        [WebMethod]
        public string RaportPracy(int id_pracy, int typ_raportu, string empl_id, int qty, string storage_place)
        {
            //Typ raportu (0 – raport ilości; 1 – brak wewnętrzny; 2 – brak zewnętrzny; 8 – zmiana pola odkładczego)</param>
            const string _api_version = "1.0";
            const string _token = "ahp9zee4gi5Oi9Ae";
            
            const string _ContentType = "application/json";

            _FullQtyReport fqr = new _FullQtyReport();
            fqr.device_id = null;
            fqr.employee_id = empl_id;
            fqr.quantity = qty;
            fqr.report_type = typ_raportu;
            fqr.storage_place = storage_place;





            JsonSerializerSettings microsoftDateFormatSettings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
            };
            string tst_fqr = JsonConvert.SerializeObject(fqr, microsoftDateFormatSettings);
            var client = new System.Net.WebClient();

            client.Headers.Add("Authorization", _token);
            client.Headers.Add("API-Version", _api_version);
            client.Headers.Add("Content-Type", _ContentType);
            client.Headers.Add("Access-Control-Request-Method", "PUT");
            string addr = $@"http://192.168.1.129:13002/task/" + id_pracy + "/";

            try
            {
                return client.UploadString(addr, "PATCH", tst_fqr);
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public class _Partial_report
        {
            /// <summary>
            /// Nr ewidencyjny pracownika składającego raport lub jego RFID poprzedzony znakiem +
            /// </summary>
            public string employee_id { get; set; }
            public string device_id { get; set; }
            public DateTime start_time { get; set; }
            public DateTime stop_time { get; set; }
        }

        public class _FullQtyReport
        {
            /// <summary>
            /// Typ raportu (0 – raport ilości; 1 – brak wewnętrzny; 2 – brak zewnętrzny; 8 – zmiana pola odkładczego)
            /// </summary>
            public int report_type { get; set; }

            /// <summary>
            /// Liczba raportowanych elementów (wymagany, gdy report_type ≠ 8; ignorowany, gdy report_type = 8)
            /// </summary>
            public int quantity { get; set; }
            /// <summary>
            /// Nr ewidencyjny pracownika składającego raport (gdy pominięto, wstawiany jest „IPOsystem™”)
            /// </summary>
            public string employee_id { get; set; }
            public string device_id { get; set; }
            /// <summary>
            /// Kod pola odkładczego (wymagany, gdy report_type = 8; w pozostałych przypadkach ignorowany)
            /// </summary>
            public string storage_place { get; set; }


        }



    }
}





