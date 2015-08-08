<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Page.cs" Inherits="PAge" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
    </div>
    </form>
    <script>
        function funStuff(x, y, z) {

            return x + y + z;
        }

        function boringStuff() {
            return 5;
        }

        function shortHypothetical(x,
                                    y) {

            return y;
        }

        function hypotheticalStuff(thing1,
                                    thing2) {
            foo: function (x, y) {
                return (x + y);
            }

            bar: function () { return false; }
    
            //return
            /*return*/
            /*
            return
            */ return false;

        }

        function ajaxStuff(x, y) {
            $.ajax({
                option: x,
                option2: y
            });
        }

        function doConditionalStuff() {
            if (5 == 4)
                return false;
            else
                return true;
        }

        function test2(a){
    </script>
</body>
</html>
