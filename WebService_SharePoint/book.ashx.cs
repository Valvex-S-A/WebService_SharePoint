using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Xml.Serialization;
using SP = Microsoft.SharePoint.Client;

namespace WebService_SharePoint
{
    /// <summary>
    /// Summary description for book
    /// </summary>
    public class book : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            string tekst = "";
            string user = context.Request["user"];

            if (!string.IsNullOrEmpty(user))
            {
                string siteUrl = "http://SP2013/";
                
                ClientContext clientContext = new ClientContext(siteUrl);


                CredentialCache cc = new CredentialCache();
                cc.Add(new Uri(siteUrl), "NTLM", new NetworkCredential("apawlowski", "cbv3.560671bf", "valvex"));
                clientContext.Credentials = cc;
                clientContext.AuthenticationMode = SP.ClientAuthenticationMode.Default;
                SP.List oList = clientContext.Web.Lists.GetById(new Guid("FEB89E30-253D-45FF-A213-C6FCBD8FDC52"));

                ListItemCollection col = oList.GetItems(CamlQuery.CreateAllItemsQuery());
                clientContext.Load(col);

                clientContext.ExecuteQuery();

                AddressBook adb = new AddressBook();
                AddressBookPbgroup p = new AddressBookPbgroup();
                p.id = 4;
                p.name = "VALVEX";
                adb.pbgroup = p;
                AddressBookContact[] ctc = new AddressBookContact[1];

                if (col.Count != 0) ctc = new AddressBookContact[col.Count];
                int licznik = 0;
                foreach (ListItem l in col)
                {
                    if (l["_x0064_hf7"].ToString() =="publiczny" || l["_x0064_hf7"].ToString() == user)
                    {
                        AddressBookContact ct = new AddressBookContact();
                        ct.FirstName = l["Title"].ToString() ;
                        ct.LastName = (l["FirstName"] == null ? "" : l["FirstName"].ToString());
                        ct.IsPrimary = false;
                        ct.Primary = 0;
                        ct.Frequent = 0;
                        AddressBookContactPhone ph = new AddressBookContactPhone();
                        ph.accountindex = 1;
                        ph.phonenumber = l["WorkPhone"] == null ? "" : l["WorkPhone"].ToString();
                        ct.Phone = ph;
                        ctc[licznik] = ct;
                        licznik++;

                    }
                }
                adb.Contact = ctc;
               
                XmlSerializer ser = new XmlSerializer(typeof(AddressBook));
                StringWriter textWriter = new StringWriter();
                ser.Serialize(textWriter, adb);
                tekst = textWriter.ToString();
                tekst = tekst.Replace("ą", "a").Replace("ć", "c").Replace("ę", "e").Replace("ł", "l").Replace("ś", "s").Replace("ń", "n").Replace("ó", "o").Replace("ż", "z").Replace("ź", "z");
                tekst = tekst.Replace("Ą", "A").Replace("Ć", "C").Replace("Ę", "E").Replace("Ł", "L").Replace("Ś", "S").Replace("Ń", "N").Replace("Ó", "O").Replace("Ż", "Z").Replace("Ź", "Z");
            }
           

            context.Response.Write(tekst);
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}