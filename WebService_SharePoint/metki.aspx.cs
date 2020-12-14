using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebService_SharePoint
{
    public partial class metki : System.Web.UI.Page
    {

        
        string druk_h7 = "192.168.3.24";
        string druk_koop = "192.168.3.14";
        string druk_tech = "192.168.3.15"; //3.26
        string druk_koop_new = "192.168.3.17";
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Button_lok_Click(object sender, EventArgs e)
        {
            string ip_druk = "";
            ip_druk = ChoosePrinter(ip_druk);
            Service1 srv = new Service1();
            srv.JDE_Drukuj_metkę_lokalizacja(ip_druk, 9100, TextBox_lok.Text);


        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            string ip_druk = "";
            ip_druk = ChoosePrinter(ip_druk);
            string typ = "d";
            if (CheckBox1.Checked) typ = "m";
            Service1 srv = new Service1();

            try
            {
                int t = 0;
                t = int.Parse(this.select.Value);
                string sztuki = this.tbSztuki.Value;


                srv.JDE_Drukuj_metkę(ip_druk, 9100, this.tbKod1.Value, typ, t, sztuki);

            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);

            }
            this.tbKod1.Value = "";
            this.tbSztuki.Value = "";
        }

        private string ChoosePrinter(string ip_druk)
        {
            if (RadioButton1.Checked) ip_druk = druk_koop;
            if (RadioButton2.Checked) ip_druk = druk_h7;
            if (RadioButton3.Checked) ip_druk = druk_tech;
            if (RadioButton4.Checked) ip_druk = druk_koop_new;
            return ip_druk;
        }




        protected void Button3_Click(object sender, EventArgs e)
        {
            int _od;
            int _do;
            int.TryParse(tb_od.Text, out _od);
            int.TryParse(tb_do.Text, out _do);

            string ip_druk = "";
            ip_druk = ChoosePrinter(ip_druk);


            Service1 srv = new Service1();

            for (int n=_od; n<=_do; n++)
            {
                srv.JDE_Drukuj_prosta_metke(ip_druk, 9100, n.ToString() + "/" + tb_rok.Text, tb_text.Text);
                Thread.Sleep(700);
            }

            
        }

        protected void Button4_Click(object sender, EventArgs e)
        {
            string ip_druk = "";
            ip_druk = ChoosePrinter(ip_druk);


            Service1 srv = new Service1();
            if (cb_trzy.Checked)
            {
                srv.ZPLDrukuj_tekst_3(ip_druk, 9100, this.TextBox1.Text);
            } else
            srv.ZPLDrukujText(ip_druk, 9100, this.TextBox1.Text);
        }
    }
}