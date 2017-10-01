Imports System.Text.RegularExpressions
Namespace FormToHTML
    Public Module FormToHTML
        Private Const CurlyBracketRegexPattern As String = "{{([^}]+)}}"
        Public Sub GenerateFiles(FilePathPrefix As String, parent As Form)
            ' Get file names
            Dim FILE_NAME As String = FilePathPrefix + parent.Name + ".html"
            Dim MAP_NAME As String = parent.Name + "map.js"
            Dim TEMPLATE_HEADER_FILENAME As String = FilePathPrefix + "headtemplate.html"
            Dim FILE_NAME_MAP As String = FilePathPrefix + MAP_NAME

            ' Ensure Files Exist
            If Ensure_File_Exists(FILE_NAME) Then
                ' Open up streams
                Dim objWriter As New System.IO.StreamWriter(FILE_NAME)
                Dim objWriterMapping As New System.IO.StreamWriter(FILE_NAME_MAP)

                ' Scale according to your resolution
                Dim scalefactor As Double = 0.97 / GetScaleFactor()

                ' Start writing
                objWriterMapping.WriteLine("var controls = {")
                objWriter.WriteLine("<!DOCTYPE html><html lang=""en""><head>")

                ' Put in the header
                Dim MatchE As MatchEvaluator = New MatchEvaluator(Function(m As Match) As String
                                                                      Dim item As String = m.ToString.Substring(2, m.ToString.Length - 4)
                                                                      Select Case item
                                                                          Case "name"
                                                                              Return parent.Name
                                                                          Case "height"
                                                                              Return CStr(CInt(parent.Height * scalefactor)) + "px"
                                                                          Case "width"
                                                                              Return CStr(CInt(parent.Width * scalefactor)) + "px"
                                                                      End Select
                                                                      Return m.ToString
                                                                  End Function)

                Using templateReader As IO.StreamReader = New IO.StreamReader(TEMPLATE_HEADER_FILENAME)
                    objWriter.WriteLine(Regex.Replace(templateReader.ReadToEnd, CurlyBracketRegexPattern, MatchE, RegexOptions.Singleline))
                End Using

                objWriter.WriteLine("</head><body>")

                ' Check for menustrips
                Dim menu As MenuStrip
                For Each c As Control In parent.Controls
                    If c.GetType = GetType(MenuStrip) Then
                        Menu = CType(c, MenuStrip)
                        Exit For
                    End If
                Next

                ' Add menu if present
                ' FIXME: This is HORRIBLE
                If Not IsNothing(Menu) Then
                    objWriter.WriteLine(" <div class= ""dropdowncontainer"">")
                    For Each x As ToolStripMenuItem In Menu.Items
                        objWriter.WriteLine(" <div Class=""dropdown"">")
                        objWriter.WriteLine("<button id=""" + x.Name + """ Class=""dropbtn"">" + x.Text.Replace("&", "") + "</button>")
                        If x.DropDownItems.Count > 0 Then
                            objWriter.WriteLine("<div Class=""dropdown-content"">")
                            For Each y As Object In x.DropDownItems
                                If y.GetType = GetType(ToolStripMenuItem) Then
                                    If CType(y, ToolStripMenuItem).HasDropDownItems Then
                                        For Each z As Object In CType(y, ToolStripMenuItem).DropDownItems
                                            If z.GetType = GetType(ToolStripMenuItem) Then _
                                            objWriter.WriteLine("<a href=""#"" id=""" + CType(z, ToolStripMenuItem).Name + """>" + CType(z, ToolStripMenuItem).Text.Replace("&", "") + "</a>")
                                        Next
                                    Else
                                        objWriter.WriteLine("<a href=""#"" id=""" + CType(y, ToolStripMenuItem).Name + """>" + CType(y, ToolStripMenuItem).Text.Replace("&", "") + "</a>")
                                    End If
                                End If
                            Next
                            objWriter.WriteLine("</div>")
                        End If
                        objWriter.WriteLine("</div>")
                    Next
                    objWriter.WriteLine("</div>")
                End If

                ' Start writing the form
                objWriter.WriteLine("<div id=""mainform"">")

                Dim OutsideFormList As New Stack(Of String)
                For Each currControl As Control In parent.Controls
                    Addit(currControl, objWriter, objWriterMapping, OutsideFormList, parent, False)
                Next

                objWriter.WriteLine("</div>")

                ' Add controls that are outside the form
                While OutsideFormList.Count > 0
                    Dim currControl As String = OutsideFormList.Pop()
                    objWriter.WriteLine(currControl)
                End While

                objWriter.WriteLine("</body></html>")
                objWriter.Close()

                objWriterMapping.WriteLine("}")
                objWriterMapping.Close()
            End If
        End Sub

        Private Sub Addit(currControl As Control, objWriter As IO.StreamWriter, objWriterMapping As IO.StreamWriter, ByRef outsideformlist As Stack(Of String), parent As Form, ignoreHidden As Boolean)
            If currControl Is Nothing Then Exit Sub

            ' IGNOREHTML tag removes the control
            If CStr(currControl.Tag) = "IGNOREHTML" Then Exit Sub

            ' top, left and tab indices
            Dim scalefactor As Double = (1 / GetScaleFactor())
            Dim t As Double = currControl.Top * scalefactor
            Dim l As Double = currControl.Left * scalefactor
            Dim currWidth As Double = currControl.Width * scalefactor
            Dim currHeight As Double = currControl.Height * scalefactor

            ' Add up the tab indices
            Dim tabin As Integer = currControl.TabIndex
            Dim ImmediateParent As Control = currControl.Parent

            Dim factor As Integer = 50
            While ImmediateParent IsNot Nothing AndAlso ImmediateParent IsNot parent
                tabin += ImmediateParent.TabIndex * factor
                factor *= 100
                ImmediateParent = ImmediateParent.Parent
            End While
            ' Add 1 to be safe from 0
            tabin += 1
            Dim TabIndexString As String = " tabindex=""" + CStr(tabin) + """"

            Dim currWidthS As String = CStr(CInt(currWidth)) + "px"
            Dim currHeightS As String = CStr(CInt(currHeight)) + "px"

            ' Account for docking
            If currControl.Dock = DockStyle.Top Or currControl.Dock = DockStyle.Bottom Then currWidthS = "100%"

            ' You may want to modify this slightly
            If currControl.Dock = DockStyle.Fill Then currWidthS = "100%"

            Dim currentName As String = currControl.Name
            Dim currentId As String = currControl.Name

            If currControl.DataBindings.Count > 0 Then
                currentName = currControl.DataBindings(0).BindingMemberInfo.BindingMember
                objWriterMapping.WriteLine("'" + currentName + "':'" + currentId + "',")
            End If

            Dim autosizeEnabled As Boolean = currControl.GetType = GetType(Label) AndAlso CType(currControl, Label).AutoSize
            Dim borderBox As Boolean = False

            ' Add top and left directives
            Dim styles As String = "style=""top:" + CStr(CInt(t)) + "px; left:" + CStr(CInt(l)) + "px;"

            ' Add height and width if autosize not enabled
            ' If enabled, then prevent word-wrapping
            If Not autosizeEnabled Then
                styles += "height:" + currHeightS + "; width:" + currWidthS + ";"
            Else
                styles += "white-space:nowrap;"
            End If

            ' Add font directives
            styles += "font-size:" + CStr(Math.Round(currControl.Font.SizeInPoints, 1)) + "pt;"
            If currControl.Font.Bold Then styles += "font-weight:bold;"
            Dim c As Color = currControl.ForeColor
            Dim colorString As String = c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2")
            ' TODO: Change this to the default color defined in external CSS
            If colorString <> "000000" Then styles += "color:#" + colorString + ";"

            ' Add z-index from display order
            ' FIXME: High z-indices and negative are both problematic; Find a solution for this.
            styles += "z-index:" + CStr(5000 - currControl.Parent.Controls.GetChildIndex(currControl)) + ";"

            ' Add text alignment for labels if necessary
            ' FIXME: Add this for other controls and make this better
            If currControl.GetType = GetType(Label) Then
                Dim currLabel As Label = CType(currControl, Label)
                If currLabel.TextAlign = ContentAlignment.BottomRight OrElse currLabel.TextAlign = ContentAlignment.MiddleRight OrElse currLabel.TextAlign = ContentAlignment.TopRight Then
                    styles += "text-align:right;"
                ElseIf currLabel.TextAlign = ContentAlignment.BottomCenter OrElse currLabel.TextAlign = ContentAlignment.MiddleCenter OrElse currLabel.TextAlign = ContentAlignment.TopCenter Then
                    styles += "text-align:center;"
                End If
            End If

            ' Finish adding styles
            styles += """"

            ' Disable not-enabled controls
            If Not currControl.Enabled Then styles += " disabled"

            ' Make invisible controls hidden if not on a tabpage
            If (Not currControl.Visible) AndAlso (Not ignoreHidden) Then styles += " hidden"

            ' === Start adding controls ===

            ' GroupBox
            ' FIXME: Find a better equivalent
            If currControl.GetType = GetType(GroupBox) Or currControl.GetType = GetType(Panel) Then
                objWriter.WriteLine("<div class=""borderbox"" id=""" + currentId + """ " + styles + "><legend>" + currControl.Text + "</legend>")
                For Each child As Control In currControl.Controls
                    Addit(child, objWriter, objWriterMapping, outsideformlist, parent, ignoreHidden)
                Next
                objWriter.WriteLine("</div>")
            End If

            ' TabPage
            If currControl.GetType = GetType(TabPage) Then
                objWriter.WriteLine("<div class=""tabcontent"" id=""" + currentId + """ " + styles + ">")
                For Each child As Control In CType(currControl, TabPage).Controls
                    Addit(child, objWriter, objWriterMapping, outsideformlist, parent, True)
                Next
                objWriter.WriteLine("</div>")
            End If

            ' TabControl
            If currControl.GetType = GetType(TabControl) Then
                objWriter.WriteLine("<div class=""tab"" " + styles + ">")
                ' Add the top navigator
                For Each child As TabPage In CType(currControl, TabControl).TabPages
                    objWriter.WriteLine("<button class=""tablinks"" onclick=""openTab(event, '" + child.Name + "')"">" + child.Text + "</button>")
                Next
                ' Add the tab pages
                For Each child As TabPage In CType(currControl, TabControl).TabPages
                    Addit(child, objWriter, objWriterMapping, outsideformlist, parent, True)
                Next
                objWriter.WriteLine("</div>")
            End If

            ' TableLayoutPanel
            If currControl.GetType = GetType(TableLayoutPanel) Then
                objWriter.WriteLine("<table class=""tablepanel"" id=""" + currentId + """ " + styles + "><legend>" + currControl.Text + "</legend>")
                For i As Integer = 0 To CType(currControl, TableLayoutPanel).RowCount - 1
                    objWriter.WriteLine("<tr>")
                    For j As Integer = 0 To CType(currControl, TableLayoutPanel).ColumnCount - 1
                        objWriter.WriteLine("<td>")
                        Addit(CType(currControl, TableLayoutPanel).GetControlFromPosition(j, i), objWriter, objWriterMapping, outsideformlist, parent, ignoreHidden)
                        objWriter.WriteLine("</td>")
                    Next
                    objWriter.WriteLine("</tr>")
                Next
                objWriter.WriteLine("</table>")
            End If

            ' Label
            If currControl.GetType = GetType(Label) Then
                Dim classStr As String = ""
                If CType(currControl, Label).BorderStyle = BorderStyle.Fixed3D Or CType(currControl, Label).BorderStyle = BorderStyle.FixedSingle Then
                    classStr = " class=""borderbox"""
                End If
                objWriter.WriteLine("<div id=""" + currentId + """ " + styles + classStr + ">" + currControl.Text + "</div>")
            End If

            ' Button
            If currControl.GetType = GetType(Button) Then
                styles += TabIndexString
                Dim extraClass As String = ""
                If parent.AcceptButton Is currControl Then
                    extraClass = " class=""acceptbutton"" "
                ElseIf parent.CancelButton Is currControl Then
                    extraClass = " class=""cancelbutton"" "
                End If
                objWriter.WriteLine("<button id=""" + currentId + """ name=""" + currentName + """ " + styles + extraClass + ">" + currControl.Text.Replace("&", "") + "</button>")
            End If

            ' PictureBox
            ' FIXME: Add the picture as datauri when statically set
            If currControl.GetType = GetType(PictureBox) Then
                objWriter.WriteLine("<img id=""" + currentId + """ " + styles + ">")
            End If

            ' TextBox
            If currControl.GetType = GetType(TextBox) Then
                styles += TabIndexString
                If CType(currControl, TextBox).Multiline Then
                    objWriter.WriteLine("<textarea id=""" + currentId + """ " + styles + ">" + "</textarea>")
                Else
                    objWriter.WriteLine("<input type=""text"" id=""" + currentId + """ " + styles + " " + If(CType(currControl, TextBox).ReadOnly, "readonly", "") + ">")
                End If
            End If

            ' DateTimePicker
            ' TODO: Replace this with a jQuery date box if better compatibility is necessary
            If currControl.GetType = GetType(DateTimePicker) Then
                styles += TabIndexString
                objWriter.WriteLine("<input type=""date"" id=""" + currentId + """ " + styles + ">")
            End If

            ' CheckBox
            If currControl.GetType = GetType(CheckBox) Then
                objWriter.WriteLine("<div " + styles + ">")
                objWriter.WriteLine("<input type=""checkbox"" style=""position:relative"" tabindex=""" + CStr(tabin) + """ id=""" + currentId + """ name=""" + currentName + """ " + If(CType(currControl, CheckBox).Checked, "checked", "") + " >&nbsp;" + currControl.Text + "</div>")
            End If

            ' ComboBox
            ' FIXME: This is not a select, but an input with suggestions
            ' Change if necessary
            If currControl.GetType = GetType(ComboBox) Then
                styles += TabIndexString
                objWriter.WriteLine("<input type=""text"" id=""" + currentId + """ list=""" + currentName + "List"" " + styles + "/>")
                objWriter.WriteLine("<datalist id=""" + currentName + "List"">")
                'Add the list if not bound to a DataSource
                If CType(currControl, ComboBox).DataSource Is Nothing Then
                    For Each item As Object In CType(currControl, ComboBox).Items
                        Dim sitem As String = CType(currControl, ComboBox).GetItemText(item)
                        objWriter.WriteLine("<option value = """ + sitem + """>" + sitem + "</option>")
                    Next
                End If
                objWriter.WriteLine("</datalist>")
            End If

            ' ListBox
            If currControl.GetType = GetType(ListBox) Then
                styles += TabIndexString
                objWriter.WriteLine("<select type=""list"" id=""" + currentId + """ list=""" + currentName + "List"" " + styles + " size=""2"">")
                If CType(currControl, ListBox).DataSource Is Nothing Then
                    For Each item As Object In CType(currControl, ListBox).Items
                        Dim sitem As String = CType(currControl, ListBox).GetItemText(item)
                        objWriter.WriteLine("<option value = """ + sitem + """>" + sitem + "</option>")
                    Next
                End If
                objWriter.WriteLine("</select>")
            End If

            ' DataGridView
            ' FIXME: Implement this better
            If currControl.GetType = GetType(DataGridView) Then
                ' This is just an example. classid = 1 will make the grid read only
                Dim classid As String = "2"
                If CStr(currControl.Name).Contains("Profile") Then classid = "1"

                objWriter.WriteLine("<div class=""tablecontainer" + classid + """ " + styles + ">")
                objWriter.WriteLine("<table id=""" + currentId + """ class=""tabledatagrid" + classid + """>")
                objWriter.WriteLine("<thead><tr>")
                For Each col As DataGridViewColumn In CType(currControl, DataGridView).Columns
                    objWriter.WriteLine("<th datamember=""" + col.DataPropertyName + """ style=""width:" + CStr(CInt(col.Width / GetScaleFactor())) + "px;"">" + col.HeaderText + "</th>")
                Next
                objWriter.WriteLine("</tr></thead>")
                objWriter.WriteLine("<tbody></tbody>")
                objWriter.WriteLine("</table>")
                objWriter.WriteLine("</div>")
            End If
        End Sub

        Public Function Ensure_File_Exists(FILE_NAME As String) As Boolean
            If System.IO.File.Exists(FILE_NAME) = True Then
                Return True
            Else
                System.IO.File.Create(FILE_NAME).Dispose()
                If System.IO.File.Exists(FILE_NAME) = True Then
                    Return True
                Else
                    MsgBox("Failed to create report file. Check for permission errors.", vbCritical)
                    Return False
                End If
            End If
        End Function

        Private Function GetScaleFactor() As Double
            Return Math.Min(Screen.PrimaryScreen.Bounds.Height / 768, Screen.PrimaryScreen.Bounds.Width / 1024)
        End Function
    End Module
End Namespace