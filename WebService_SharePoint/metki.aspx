<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="metki.aspx.cs" Inherits="WebService_SharePoint.metki" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Drukowanie metek</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1, shrink-to-fit=no" />
    <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/4.0.0-beta.2/css/bootstrap.min.css" integrity="sha384-PsH8R72JQ3SOdhVi3uxftmaW6Vc51MKb0q5P2rRUpPvrszuE4W1povHYgTpBfshb" crossorigin="anonymous" />
       <script src="https://code.jquery.com/jquery-3.2.1.slim.min.js" integrity="sha384-KJ3o2DKtIkvYIK3UENzmM7KCkRr/rE9/Qpg6aAZGJwFDMVNA/GpGFF93hXpG5KkN" crossorigin="anonymous"></script>
        <script src="https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.12.3/umd/popper.min.js" integrity="sha384-vFJXuSJphROIrBnz7yo7oB41mKfc8JzQZiCq4NCceLEaO4IHwicKwpJf9c9IpFgh" crossorigin="anonymous"></script>
        <script src="https://maxcdn.bootstrapcdn.com/bootstrap/4.0.0-beta.2/js/bootstrap.min.js" integrity="sha384-alpBpkh1PFOepccYVYDB4do5UnbKysX5WZXm3XxPqe5iKTfUKjNkCk9SaVuEZflJ" crossorigin="anonymous"></script> 

</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <div class="jumbotron">


                <asp:RadioButton ID="RadioButton1" runat="server" Font-Size="X-Large" GroupName="druk" Text="Drukarka kooperacja" />
                <br />
                <br />
                <asp:RadioButton ID="RadioButton2" runat="server" Font-Size="X-Large" GroupName="druk" Text="Drukarka H7" />
                <br />
                <br />
                <asp:RadioButton ID="RadioButton3" runat="server" Font-Size="X-Large" GroupName="druk" Text="Drukarka M.techn." />
                <br />
                <br />
            </div>


          

            <div class="panel-group" id="accordion">
  <div class="panel panel-default">
    <div class="panel-heading">
      <h4 class="panel-title">
        <a data-toggle="collapse" data-parent="#accordion" href="#collapseOne">
          DRUKUJ METKĘ PODSTAWOWĄ
        </a>
      </h4>
    </div>
    <div id="collapseOne" class="panel-collapse collapse in">
      <div class="panel-body">
       
            <label for="tbKod1">Indeks:</label>
            <input class="form-control input-sm" runat="server" id="tbKod1" type="text" />
            <div class="form-group">
                <label for="select" class="col-lg-2 control-label">Wybierz ilość etykiet</label>
                <div class="col-lg-2">
                    <select class="form-control" id="select" runat="server">
                        <option>1</option>
                        <option>2</option>
                        <option>3</option>
                        <option>4</option>
                        <option>5</option>
                        <option>6</option>
                        <option>7</option>
                        <option>8</option>
                        <option>9</option>
                        <option>10</option>
                        <option>15</option>
                        <option>20</option>
                    </select>
                </div>
            </div>
            <asp:CheckBox ID="CheckBox1" runat="server" Font-Size="X-Large" Text="Mała wersja etykiety" />                  
            <br/>
            <asp:Button ID="Button1" class="btn btn-danger" OnClick="Button1_Click" runat="server" Text="DRUKUJ METKI" />
             <br/>
      </div>
    </div>
  </div>
  <div class="panel panel-default">
    <div class="panel-heading">
      <h4 class="panel-title">
        <a data-toggle="collapse" data-parent="#accordion" href="#collapseTwo">
          WYDRUKUJ PONUMEROWANE METKI
        </a>
      </h4>
    </div>
    <div id="collapseTwo" class="panel-collapse collapse">
      <div class="panel-body">
         <tr>
                <td class="auto-style24">Numery od</td>
                <td class="auto-style25">
                    <asp:TextBox ID="tb_od" runat="server"></asp:TextBox>
                </td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style24">Numery do</td>
                <td class="auto-style25">
                    <asp:TextBox ID="tb_do" runat="server"></asp:TextBox>
                </td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style24">ROK</td>
                <td class="auto-style25">
                    <asp:TextBox ID="tb_rok" runat="server"></asp:TextBox>
                </td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style24">TEKST POD </td>
                <td class="auto-style25">
                    <asp:TextBox ID="tb_text" runat="server"></asp:TextBox>
                </td>
                <td>
                    <asp:Button ID="Button3" runat="server" OnClick="Button3_Click" Text="Drukuj metki" Width="245px" />
                </td>
            </tr>


      </div>
    </div>
  </div>
  <div class="panel panel-default">
    <div class="panel-heading">
      <h4 class="panel-title">
        <a data-toggle="collapse" data-parent="#accordion" href="#collapseThree">
          WYDRUKUJ METKĘ Z TEKSTEM

        </a>
      </h4>
    </div>
    <div id="collapseThree" class="panel-collapse collapse">
      <div class="panel-body">
       <tr>
                <td class="auto-style24">&nbsp;</td>
                <td class="auto-style25">&nbsp;</td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style24">Tekst do druku</td>
                <td class="auto-style25">
                    <asp:CheckBox ID="cb_trzy" runat="server" Text="Wydrukuj małe metki" />
                    <asp:TextBox ID="TextBox1" runat="server" Width="185px"></asp:TextBox>
                </td>
                <td>
                    <asp:Button ID="Button4" runat="server" OnClick="Button4_Click" Text="Drukuj tekst" Width="240px" />
                </td>
            </tr>
      </div>
    </div>
  </div>
</div>

               </div>




           

          
          

        

   


    </form>
</body>
</html>
