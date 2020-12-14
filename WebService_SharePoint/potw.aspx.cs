using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebService_SharePoint
{
    public partial class potw : System.Web.UI.Page
    {

        string nr  = "";
        string pakowacz = "";
        protected void Page_Load(object sender, EventArgs e)
        {
            nr = Context.Request["Nr"] ?? "";
            pakowacz = Context.Request["pakowacz"] ?? "";
            



        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            trans tr = new trans();
           


            bool test = tr.Zaloguj(this.tb_login.Text, this.tb_haslo.Text);
            if (test)
            {

                string _token = StringUtil.Crypt(this.tb_login.Text + "#" + nr + "#" + pakowacz);
                Response.Redirect("~/potwl.aspx?user="+this.tb_login.Text + "&nr="+nr);




                }
        }
    }
}