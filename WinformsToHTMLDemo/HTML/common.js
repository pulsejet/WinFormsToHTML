function getJSONfromXML(data, rowno) {
    var ans = {};
    x = data.documentElement.childNodes;
    for (j = 0; j < x[rowno].childNodes.length; j++) {
        if (x[rowno].childNodes[j].firstChild == null) continue;
        
        name = x[rowno].childNodes[j].nodeName;
        if ((name.indexOf("Date") != -1) || (name.indexOf("Dt") != -1)){
            ans[x[rowno].childNodes[j].nodeName] = moment(x[rowno].childNodes[j].firstChild.nodeValue).format('YYYY-MM-DD');
        } else {
            ans[x[rowno].childNodes[j].nodeName] = x[rowno].childNodes[j].firstChild.nodeValue;
        }
    }
    return ans;
}

function setControlsDatafromJSON(dataJson, controllist) {
    for (key in dataJson) {
        if (key in controllist) {
            $('#'+controllist[key]).val(dataJson[key]);
        }
    }
    return;
}

function getXMLfromForm() {
    var xml = '<DocumentElement>\n';
    for (key in controls) {
        if (!$("#" + controls[key]).prop("disabled")) xml += "\t<" + key + ">" + encodeURI($("#" + controls[key]).val()) + "</" + key + ">\n   " 
    }
    xml += '</DocumentElement>';
    return xml;
}

function getJsonMapping(data, pkey) {
    var ans = {};
    x = data.documentElement.childNodes;
    var cpkey = "";
    for (i = 0; i < x.length ;i++) {
        if (x[i].childNodes.length > 0) {
            for (j = 0; j < x[i].childNodes.length ;j++) {
                if (x[i].childNodes[j].nodeName == pkey) cpkey = x[i].childNodes[j].firstChild.nodeValue;
            }
            ans[cpkey] = {};
            for (j = 0; j < x[i].childNodes.length ;j++) {
                if (x[i].childNodes[j].firstChild != null)
                ans[cpkey][x[i].childNodes[j].nodeName] = x[i].childNodes[j].firstChild.nodeValue;
            }
        }
    }
    return ans;
}

function fillListBoxJson(data, name, txtF) {
    for (key in data){
        $('#'+name).append($('<option>', {value: key,text: data[key][txtF]}));
    }
}

function addHandlerDataTable(key) {
    $('#' + controls[key]).keyup(function() {
        datatable[selectedImageID][key] = $('#' + controls[key]).val();
    });
}

function getXMLfromJsondt (json, tablename) {
    var xml = '<DocumentElement>\n';
    for (key in json) {
        xml += '\t<' + tablename + '>\n';
        for (key2 in json[key]) {
            xml += '\t\t<' + key2 + '>' + json[key][key2] + '</' + key2 + '>\n';
        }
        xml += '\t</' + tablename + '>\n';
    }
    xml += '</DocumentElement>';
    return xml
}

function selectFirst (control) {
    $(control + ' option[selected="selected"]').each(
        function() {
            $(this).removeAttr('selected');
        }
    );
    $(control + " option:first").attr('selected','selected');
    $(control).trigger('change');
}

function getParameterByName(name, url) {
    if (!url) url = window.location.href;
    name = name.replace(/[\[\]]/g, "\\$&");
    var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
        results = regex.exec(url);
    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, " "));
}

function fillList(data, datalist) {
    $('#'+datalist).empty();
    x = data.documentElement.childNodes;
    for (i = 0; i < x.length ;i++) {
        if (x[i].childNodes.length > 0) {
            txt = x[i].childNodes[1].firstChild.nodeValue;
            $('#'+datalist).append($('<option>', {value: txt,text: txt}));
        }
    }
}

function initGrids() {
    $('.tablecontainer2').append('<div class="add" style="padding:5px;padding-left:8px;">+</div>');
    $('.tabledatagrid2 > thead > tr').prepend('<th></th>');
    $(".tabledatagrid2, .tabledatagrid1").tablesorter();
    
    $(".tabledatagrid2").on("click", ".delete", function() {
        if ($(this).text() == "=") {
            $(this).text("X");
            $(this).closest('tr').find('td').each(function(){
                   $(this).css('text-decoration','none');
                   $(this).css('color','black');
            });
        } else if ($(this).text() == "X") {
            $(this).text("=");
            $(this).closest('tr').find('td').each(function(){
                   $(this).css('text-decoration','line-through');
                   $(this).css('color','silver');
            });
        } else if ($(this).text() == "*") {
            $(this).closest('tr').remove();
        }
    });
    
    $(".tablecontainer2").on("click", ".add", function() {
        var DataGridCols=[];
        $(this).parent().find('thead').find("th").each(function(){DataGridCols.push($(this).attr('datamember'))});
        var markup = "<tr>";
        for (x in DataGridCols){
            if (x==0) {markup += '<td class="delete">*</td>'; continue;}
            markup += '<td contenteditable="true" class="' + DataGridCols[x] + '"></td>';
        }
        
        $(this).parent().find('tbody').prepend(markup + "</tr>");
    });
}

function fillDataGrid(control, data) {
    var DataGridCols=[];
    $("#" + control + " thead").find("th").each(function(){DataGridCols.push($(this).attr('datamember'))});
    $("#" + control + " tbody").find("tr").remove();
    x = data.documentElement.childNodes;
    var markup = "";
    for (i = 0; i < x.length ;i++) {
        if (x[i].childNodes.length > 0) {
            markup += "<tr>";
            var hiddenmarkup = "";
            var toFill = {};
            for (j = 0; j < x[i].childNodes.length ;j++) {
                if (x[i].childNodes[j].childNodes.length > 0) {
                    var col = x[i].childNodes[j].nodeName;
                    var val = x[i].childNodes[j].firstChild.nodeValue;
                    if ((col.toLowerCase().indexOf("date") != -1) && (jQuery.inArray(col, DataGridCols) !== -1)) val = moment(val).format('DD-MMM-YYYY')
                    
                    if(jQuery.inArray(col, DataGridCols) !== -1)
                        toFill[col] = val;
                    else
                        hiddenmarkup += "<td class=\"" + col + "\" style=\"display:none;\">" + val + "</td>";
                }
            }
            
            for (key in DataGridCols)
            {
                var type2 = ($("#" + control).attr("class")=="tabledatagrid2")
                if ((key==0) && type2) {markup += "<td class=\"delete\">X</td>"; continue;}
                var val = "";
                if (DataGridCols[key] in toFill) val = toFill[DataGridCols[key]];
                markup += "<td contenteditable=\"" + (type2?"true":"false") +"\" class=\"" + DataGridCols[key] + "\">" + val + "</td>";
            }

            markup += hiddenmarkup + "</tr>";
        }
    }
    $("#" + control + " tbody").append(markup);
    $("#" + control).trigger("update");
}

function getDataGrid(control, tablename) {
    var xml = '<DocumentElement>\n';

    $("#" + control + " tr").each(function () {
        var cells = $("td", this);
        if (cells.length > 0) {
            xml += "\t<" + tablename + ">\n";
            
            for (var i = 0; i < cells.length; ++i) {
                var col = escapeXml(cells.eq(i).attr("class"));
                var val = escapeXml(cells.eq(i).text());
                if (col=="delete") {
                    if (val!='=') continue;
                    col='delete'; val='delete';
                }
                if (cells.eq(i).text() != "") xml += "\t\t<" + col + ">" + val + "</" + col + ">\n";
            }

            xml += "\t</" + tablename + ">\n";
         }
    });

    xml += '</DocumentElement>';
    return xml;
}

function escapeXml(unsafe) {
    return unsafe.replace(/[<>&'"]/g, function (c) {
        switch (c) {
            case '<': return '&lt;';
            case '>': return '&gt;';
            case '&': return '&amp;';
            case '\'': return '&apos;';
            case '"': return '&quot;';
        }
    });
}

function addctrlShortcut(shortcutkey, control) {
    $(window).keydown(function(event) {
        if(event.ctrlKey && event.keyCode == shortcutkey) {
            control.trigger("click");
            event.preventDefault();
            return false;
        }
    });
}

function addaltrShortcut(shortcutkey, control) {
    $(window).keydown(function(event) {
        if(event.altKey && event.keyCode == shortcutkey) {
            control.trigger("click");
            event.preventDefault();
            return false;
        }
    });
}

function closeModalOnClick(control) {
    control.click(function(){top.closeModal();});
}

function toAscii(str) {
    return str.charCodeAt(0);
}

var prevFocused = $("#FocusStub");
function showModal(URL) {
    var modal = document.getElementById('myModal');
    $("#modalframe").attr("src",URL);
    $("#modalframe").attr("onload", 'adjustModal();');
    modal.style.display = "block";
    prevFocused = top.$(":focus");
    top.$("#FocusStub").focus();
}

function adjustModal() {
    var modal = $('.modal-content');
    var mdlf = $("#modalframe");
    var width = mdlf.contents().find("head").find(".modaldim").attr("width");
    var height = mdlf.contents().find("head").find(".modaldim").attr("height");
    $(".modal").css("opacity", "1");
    modal.css("opacity", "1");
    if (width==null) {
        modal.css("width","90%");
        modal.css("height","90%");
    } else {
        modal.css("width",width);
        modal.css("height",height);
    }
}

function closeModal(){
    $(".modal").css("opacity", "0");
    $(".modal-content").css("opacity", "0");
    document.getElementById('myModal').style.display = "none";
    prevFocused.focus();
}

function addValuesChangedAlert() {
    window.valuesChanged = false;
    $("input, textarea").keydown(function(event) {
        if (((event.keyCode >= 48 && event.keyCode <= 57)
         || (event.keyCode >= 97 && event.keyCode <= 122)
         || (event.keyCode >= 65 && event.keyCode <= 90)
         || (event.keyCode == 8)) && !event.altKey && !event.ctrlKey){
            valuesChanged = true;
        }
    });
    
    $(window).bind('beforeunload', function(){
        if (valuesChanged) return 'Exit without saving changes?';
    });
}

var activeTab;
function openTab(evt, cityName) {
    // Declare all variables
    var i, tabcontent, tablinks;

    // Get all elements with class="tabcontent" and hide them
    tabcontent = document.getElementsByClassName("tabcontent");
    for (i = 0; i < tabcontent.length; i++) {
        tabcontent[i].style.display = "none";
    }

    // Get all elements with class="tablinks" and remove the class "active"
    tablinks = document.getElementsByClassName("tablinks");
    for (i = 0; i < tablinks.length; i++) {
        tablinks[i].className = tablinks[i].className.replace(" active", "");
    }

    // Show the current tab, and add an "active" class to the button that opened the tab
    document.getElementById(cityName).style.display = "block";
    if (evt.currentTarget != null) evt.currentTarget.className += " active";
    activeTab = cityName;
}

var extraControls = '';
/* Modal */
extraControls += '<div id="myModal" class="modal"><div class="modal-content"><span class="close">&times;</span><iframe id="modalframe" src=""></iframe></div></div>';
/* FocusStub */
extraControls += '<button id="FocusStub" class="sneaky_hidden"></button>';

$(document).ready(function(){
    $("body").append(extraControls);
    $(".close").click(function() {closeModal()});    
    
    $(document).keydown(function(e) {
        if (e.keyCode == 27) {
            if (($(":focus").attr("id")!="FocusStub") && $('.cancelbutton').length) 
                $('.cancelbutton').trigger('click'); 
            else 
                top.closeModal(); e.preventDefault();}
            
        if (e.keyCode == 13) {$('.acceptbutton').trigger('click'); e.preventDefault();}
    });

});