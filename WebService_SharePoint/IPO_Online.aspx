<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="IPO_Online.aspx.cs" Inherits="WebService_SharePoint.IPO_Online" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
     <asp:UpdatePanel ID="UpdatePanel1" runat="server">
            <ContentTemplate>
                <asp:ScriptManager ID="ScriptManager1" runat="server">
                </asp:ScriptManager>
                <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" CellPadding="4" DataSourceID="SqlDataSource1" ForeColor="#333333" GridLines="None" Font-Names="Arial">
                    <AlternatingRowStyle BackColor="White" />
                    <Columns>
                        <asp:BoundField DataField="Status" HeaderText="Status" ReadOnly="True" SortExpression="Status" />
                        <asp:BoundField DataField="Numer2" HeaderText="Numer2" SortExpression="Numer2" />
                        <asp:BoundField DataField="Nazwa" HeaderText="Nazwa" SortExpression="Nazwa" />
                        <asp:BoundField DataField="Ilosc" HeaderText="Ilosc" SortExpression="Ilosc" />
                        <asp:BoundField DataField="popis" HeaderText="popis" SortExpression="popis" />
                        <asp:BoundField DataField="Nr_zlecenia" HeaderText="Nr_zlecenia" SortExpression="Nr_zlecenia" />
                        <asp:BoundField DataField="gniazdo" HeaderText="gniazdo" SortExpression="gniazdo" />
                        <asp:BoundField DataField="nazwa1" HeaderText="nazwa1" SortExpression="nazwa1" />
                        <asp:BoundField DataField="poczatek" HeaderText="poczatek" SortExpression="poczatek" />
                        <asp:BoundField DataField="koniec" HeaderText="koniec" SortExpression="koniec" />
                        <asp:BoundField DataField="Narzedziowiec" HeaderText="Narzedziowiec" ReadOnly="True" SortExpression="Narzedziowiec" />
                    </Columns>
                    <EditRowStyle BackColor="#7C6F57" />
                    <FooterStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
                    <HeaderStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
                    <PagerStyle BackColor="#666666" ForeColor="White" HorizontalAlign="Center" />
                    <RowStyle BackColor="#E3EAEB" />
                    <SelectedRowStyle BackColor="#C5BBAF" Font-Bold="True" ForeColor="#333333" />
                    <SortedAscendingCellStyle BackColor="#F8FAFA" />
                    <SortedAscendingHeaderStyle BackColor="#246B61" />
                    <SortedDescendingCellStyle BackColor="#D4DFE1" />
                    <SortedDescendingHeaderStyle BackColor="#15524A" />
                </asp:GridView>
                <asp:SqlDataSource ID="SqlDataSource1" runat="server" ConnectionString="<%$ ConnectionStrings:RAPORTYConnectionString %>" SelectCommand="SELECT 'Narzędzia w trakcie' as Status, zlec.Numer2, zlec.Nazwa, Zlec.Ilosc, popis, zlec.numer as Nr_zlecenia, masz.gniazdo,masz.nazwa,  poczatek, koniec, prcw.Nazwisko+' '+ prcw.Imie as Narzedziowiec
  FROM [IPO-SERWER].[zop].[dbo].[prace] as prac 
  INNER JOIN [IPO-SERWER].[zop].dbo.zlecenia as zlec on prac.zlecenie = zlec.id
  INNER JOIN [IPO-SERWER].[zop].dbo.maszyny as masz on prac.maszyna = masz.id
  INNER JOIN [IPO-SERWER].[zop].dbo.pracownicy as prcw on prac.pracownik = prcw.id
  
  where prac.czynnosc = -6 and prac.stan = 2"></asp:SqlDataSource>
            </ContentTemplate>
        </asp:UpdatePanel>
    </div>
    </form>
</body>
</html>
