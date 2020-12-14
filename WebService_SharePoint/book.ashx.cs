using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Xml.Serialization;
using SP = Microsoft.SharePoint.Client;
using GrandStream_AddressBook;
using System.Text;
using GS = GrandStream_AddressBook;
using System.Web.UI.WebControls;
using System.Xml;

namespace WebService_SharePoint
{
    /// <summary>
    /// Summary description for book
    /// </summary>
    public class book : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {

            StringBuilder sb = new StringBuilder();
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

                Microsoft.SharePoint.Client.ListItemCollection col = oList.GetItems(CamlQuery.CreateAllItemsQuery());
                clientContext.Load(col);

                clientContext.ExecuteQuery();

                int licznik = 5000;

                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?><AddressBook>\t<pbgroup>\t\t<id>4</id>\t<name>Blacklist</name>\t\t</pbgroup>");
                sb.Append("<pbgroup>\t<id>5</id>\t<name>Whitelist</name>\t</pbgroup>\t<pbgroup>\t<id>6</id>\t<name>Work</name>\t</pbgroup>\t<pbgroup>\t");
                sb.Append("<id>7</id>\t<name>Friends</name>\t</pbgroup>\t<pbgroup>\t<id>8</id>\t<name>Family</name>\t</pbgroup>\t");

                foreach (Microsoft.SharePoint.Client.ListItem l in col)
                {
                    if (l["_x0064_hf7"].ToString() =="publiczny" || l["_x0064_hf7"].ToString() == user)
                    {

                        sb.Append($"<Contact>\t<id>{licznik++}</id>\t<FirstName>{l["Title"].ToString()}</FirstName>\t<LastName>{(l["FirstName"] == null ? "" : l["FirstName"].ToString())}</LastName>\t<Frequent>0</Frequent>");
                        sb.Append($"<Phone type=\"Work\"><phonenumber>{(l["WorkPhone"] == null ? "" : l["WorkPhone"].ToString())}</phonenumber>\t<accountindex>1</accountindex>");
                        sb.Append($"</Phone>\t<Group>6</Group>\t<Primary>0</Primary>\t<Company>valvex</Company>\t</Contact>\t");

                    }
                }
                sb.Append("\t</AddressBook>"); 

                
             
                
                string tekst16 = sb.ToString();
                byte[] bytes = Encoding.Default.GetBytes(tekst16);
                tekst = Encoding.UTF8.GetString(bytes);
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


        public string RemoveWhitespace(string input)
        {
            int j = 0, inputlen = input.Length;
            char[] newarr = new char[inputlen];

            for (int i = 0; i < inputlen; ++i)
            {
                char tmp = input[i];

                if (!char.IsWhiteSpace(tmp))
                {
                    newarr[j] = tmp;
                    ++j;
                }
            }
            return new String(newarr, 0, j);
        }

        public class Utf8StringWriter : StringWriter
        {
            public Utf8StringWriter(StringBuilder sb) : base(sb)
            {
            }
            public override Encoding Encoding { get { return Encoding.UTF8; } }
        }

    }
}