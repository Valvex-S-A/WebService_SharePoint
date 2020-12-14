using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace WebService_SharePoint
{
    /// <summary>
    /// Summary description for IPO
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class IPO : System.Web.Services.WebService
    {

        [WebMethod] 
        public string HelloWorld()
        {
            API.IPO_API api = new API.IPO_API();
            API.Production_Order order = new API.Production_Order();



            order.order_id = RandomString(15);
            order.execution_date = new DateTime(2020, 10, 1); 

            order.use_optimal = false;
            order.creator_id = "2268";
            order.doc_no = "DOC_NO"; 
            order.doc_state = 1; 
            order.contractor_order_no = "contr_order_no";
            order.item_id = "22089";
            order.item_quantity = 5;
            order.warehouse_id = "PROD";
            order.execute_type = 1;
            order.description = "OPIS"; 
            order.order_no_cust = "NR_ZLEC_F";
            var test = api.PUT_PRODUCTION_ORDER(order);
            return test;
        }

        private Random random = new Random();
        public string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }


    }

    


}
