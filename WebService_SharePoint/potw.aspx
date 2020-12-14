<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="potw.aspx.cs" Inherits="WebService_SharePoint.potw" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            Login:<asp:TextBox ID="tb_login" runat="server"></asp:TextBox>
            <br />
            Hasło:<asp:TextBox ID="tb_haslo" TextMode="Password" runat="server"></asp:TextBox>
            <asp:Button ID="Button1" runat="server" Text="Zaloguj" OnClick="Button1_Click" />
        </div>
    </form>
</body>
</html>
