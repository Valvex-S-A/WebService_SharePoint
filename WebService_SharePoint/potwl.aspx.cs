using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebService_SharePoint
{
    public partial class potwl : System.Web.UI.Page
    {

         

        protected void Page_PreInit(object sender, EventArgs e)
        {

            if (true)
            {
                string _nr = Context.Request["nr"] ?? "";
                string _user_name = Context.Request["user"] ?? "";




                if (true)
                {
                    dbtrans1DataContext tr = new dbtrans1DataContext();


                     
                    lb_login.Text = _user_name;
                    lb_nr.Text = _nr;
                    int nr_zlec = -1;
                    int.TryParse(_nr, out nr_zlec);

                    Wizard1.WizardSteps.Clear();
                    //Wizard1.Controls.Clear();
                    var linie = from c in tr.zlecenia_szczegoly
                                where c.id_zlecenia == nr_zlec
                                select c;
                    foreach (var l in linie)
                    {

                        WizardStepBase newStep = new WizardStep();

                        newStep.AllowReturn = true;
                        newStep.ID = l.litm;
                        newStep.Title = l.litm;
                        newStep.Controls.Add(new Label() { Text = "<b>"+ l.litm.Trim() +" - " + l.opis + "</b><br>" });
                        newStep.Controls.Add(new Label() { Text = "Z: " + l.magazyn_z.Trim() + "_" + l.lokalizacja_z.Trim() + "<br>" });
                        newStep.Controls.Add(new Label() { Text = "DO: " + l.magazyn_do.Trim() + "_" + l.lokalizacja_do.Trim() + "<br>" });
                        newStep.Controls.Add(new Label() { Text = "Ilosc: " + l.ilosc_zamowiona.ToString() + " " + l.JM + "<br>" });

                         
                        
                        Button btn = new Button();
                        btn.Text = "Potwiedź";
                        btn.ID = l.id.ToString();
                        btn.Click += Btn_Click;
                        newStep.Controls.Add(btn);
                        if (l.status_linii == "odrzucone                     "
                            || l.status_linii == "anulowane                     "
                            )
                        {
                            btn.Enabled = false;  

                        }

                        if (l.status_linii == "zakończone                    ")
                        {
                            btn.Enabled = false; btn.Text = "Potwierdzone!";

                        }


                        Button btn1 = new Button();
                        btn1.Text = "Odrzucone!";
                        btn1.ID = l.id.ToString()+"_niezg";
                        btn1.Click += Btn1_Click;

                        if (l.status_linii == "odrzucone                     "
                            || l.status_linii == "anulowane                     "
                            || l.status_linii == "zakończone                    "
                            )
                        {
                            btn1.Enabled = false;

                        }
                        newStep.Controls.Add(btn1);


                        Wizard1.WizardSteps.Add(newStep);

                    }




                }
            }



        }

        private void Btn1_Click(object sender, EventArgs e)
        {
            string ID = ((Button)sender).ID;

            string str_rec_id = ID.Split('_')[0];
            int rec_id = int.Parse(str_rec_id);
           trans tr = new trans();
            int test = tr.Odrzuc_Linie_transferu(str_rec_id, lb_login.Text);
            

            if (test == 0)
            {
                ((Button)sender).Enabled = false;
                var db = new dbtrans1DataContext();
                trans srv = new trans();
                Service1 srv1 = new Service1();
           
                

            }
            if (Wizard1.ActiveStepIndex != Wizard1.WizardSteps.Count - 1)
                Wizard1.ActiveStepIndex++;
            else
                Response.Redirect("~/potw.aspx");
        }

        protected void Page_Load(object sender, EventArgs e)
        {

            

         
        }

        private void Btn_Click(object sender, EventArgs e)
        {
            string ID = ((Button)sender).ID;

            ((Button)sender).Enabled = false;

            trans tr = new trans();
            int test = tr.Zaksieguj_Linie_transferu(ID, lb_login.Text);

            if (test == 0)
            {
                
                ((Button)sender).Text = "Potwierdzone!";
                
            }
            if (Wizard1.ActiveStepIndex != Wizard1.WizardSteps.Count - 1)
                Wizard1.ActiveStepIndex++;
            else
                Response.Redirect("~/potw.aspx");

        }

        protected void lb_nr_Load(object sender, EventArgs e)
        {

        }

        protected void Wizard1_FinishButtonClick(object sender, WizardNavigationEventArgs e)
        {
            Response.Redirect("~/potw.aspx");
        }
    }
}