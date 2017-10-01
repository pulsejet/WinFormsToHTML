$(document).ready(function(){
    $('.tablinks:first').trigger('click');
    $("#TextBox1").val("JavaScript Works!");
    $("#TextBox1").focus();
    
    $("#Button1").click(function(){
        alert("I am JavaScript!");
    });
});