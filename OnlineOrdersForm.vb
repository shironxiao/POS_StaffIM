Imports System.Collections.Generic
Imports System.Text

Public Class OnlineOrdersForm
    Private orderRepository As New OrderRepository()
    Private cmbFilterStatus As ComboBox

    ' Pagination variables
    Private currentPage As Integer = 1
    Private pageSize As Integer = 100 ' Updated to 100
    Private totalPages As Integer = 1
    Private totalRecords As Integer = 0

    ' Pagination controls
    Private pnlPagination As Panel
    Private btnPrevPage As Button
    Private btnNextPage As Button
    Private txtPageNumber As TextBox
    Private lblTotalPages As Label

    ''' <summary>
    ''' Loads online orders when form loads
    ''' </summary>
    Private Sub OnlineOrdersForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Hide the template
        ResTemplate.Visible = False

        ' Initialize pagination controls
        InitializePaginationControls()

        ' Create and add filter ComboBox
        CreateFilterComboBox()

        LoadOnlineOrders()
    End Sub

    Private Sub InitializePaginationControls()
        ' Create pagination panel
        pnlPagination = New Panel With {
            .Height = 50,
            .Dock = DockStyle.Bottom,
            .BackColor = Color.WhiteSmoke
        }

        ' Previous Button
        btnPrevPage = New Button With {
            .Text = "< Previous",
            .Size = New Size(100, 35),
            .Location = New Point(20, 8),
            .BackColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9)
        }
        AddHandler btnPrevPage.Click, AddressOf btnPrevPage_Click

        ' Next Button
        btnNextPage = New Button With {
            .Text = "Next >",
            .Size = New Size(100, 35),
            .Location = New Point(pnlPagination.Width - 140, 8), ' Initial guess
            .BackColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9)
        }
        AddHandler btnNextPage.Click, AddressOf btnNextPage_Click

        ' Page Number TextBox (Editable)
        txtPageNumber = New TextBox With {
            .Text = "1",
            .Size = New Size(50, 25),
            .TextAlign = HorizontalAlignment.Center,
            .Font = New Font("Segoe UI", 10),
            .Location = New Point((pnlPagination.Width - 120) \ 2, 8)
        }
        AddHandler txtPageNumber.KeyDown, AddressOf txtPageNumber_KeyDown
        AddHandler txtPageNumber.Leave, Sub(s, ev) ValidateAndJumpToPage()

        ' Total Pages Label
        lblTotalPages = New Label With {
            .Text = "of 1",
            .AutoSize = False,
            .Size = New Size(70, 25),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(txtPageNumber.Right + 5, 8)
        }

        ' Add Resize Handler to keep buttons in place
        AddHandler pnlPagination.Resize, Sub(s, ev)
                                             btnNextPage.Location = New Point(pnlPagination.Width - 120, 8)
                                             txtPageNumber.Location = New Point((pnlPagination.Width - 120) \ 2, 8)
                                             lblTotalPages.Location = New Point(txtPageNumber.Right + 5, 8)
                                         End Sub

        pnlPagination.Controls.AddRange({btnPrevPage, btnNextPage, txtPageNumber, lblTotalPages})
        Me.Controls.Add(pnlPagination)
        pnlPagination.BringToFront()

        ' Adjust Panel1 to not overlap with pagination
        Panel1.Height -= pnlPagination.Height
    End Sub

    ''' <summary>
    ''' Creates the filter ComboBox
    ''' </summary>
    Private Sub CreateFilterComboBox()
        cmbFilterStatus = New ComboBox With {
            .Location = New Point(1100, 38),
            .Size = New Size(200, 30),
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = New Font("Segoe UI", 10)
        }

        cmbFilterStatus.Items.AddRange({"All Orders", "Preparing", "Served", "Completed", "Cancelled"})
        cmbFilterStatus.SelectedIndex = 0

        AddHandler cmbFilterStatus.SelectedIndexChanged, AddressOf cmbFilterStatus_SelectedIndexChanged

        Panel1.Controls.Add(cmbFilterStatus)
    End Sub

    ''' <summary>
    ''' Loads all online orders from the database and displays them
    ''' </summary>
    Private Sub LoadOnlineOrders()
        Try
            Dim selectedStatus As String = "All Orders"
            If cmbFilterStatus.SelectedItem IsNot Nothing Then
                selectedStatus = cmbFilterStatus.SelectedItem.ToString()
            End If

            ' Calculate offset
            Dim offset As Integer = (currentPage - 1) * pageSize

            ' Fetch total count first
            totalRecords = orderRepository.GetTotalOnlineOrdersCount(selectedStatus)
            totalPages = Math.Max(1, CInt(Math.Ceiling(totalRecords / pageSize)))

            ' Fetch paged data
            Dim onlineOrders As List(Of OnlineOrder) = orderRepository.GetOnlineOrdersPaged(pageSize, offset, selectedStatus)

            ' Update UI
            DisplayOnlineOrders(onlineOrders)
            UpdatePaginationControls()

        Catch ex As Exception
            MessageBox.Show($"Error loading online orders: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub UpdatePaginationControls()
        If txtPageNumber IsNot Nothing Then
            txtPageNumber.Text = currentPage.ToString()
            lblTotalPages.Text = $"of {totalPages}"
            btnPrevPage.Enabled = (currentPage > 1)
            btnNextPage.Enabled = (currentPage < totalPages)
        End If
    End Sub

    Private Sub txtPageNumber_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            ' Prevent beep sound
            e.SuppressKeyPress = True
            e.Handled = True
            ValidateAndJumpToPage()
        End If
    End Sub

    Private Sub ValidateAndJumpToPage()
        Dim newPage As Integer
        If Integer.TryParse(txtPageNumber.Text, newPage) Then
            If newPage < 1 Then newPage = 1
            If newPage > totalPages Then newPage = totalPages
            
            If newPage <> currentPage Then
                currentPage = newPage
                LoadOnlineOrders()
            Else
                txtPageNumber.Text = currentPage.ToString()
            End If
        Else
            txtPageNumber.Text = currentPage.ToString()
        End If
    End Sub

    Private Sub btnPrevPage_Click(sender As Object, e As EventArgs)
        If currentPage > 1 Then
            currentPage -= 1
            LoadOnlineOrders()
        End If
    End Sub

    Private Sub btnNextPage_Click(sender As Object, e As EventArgs)
        If currentPage < totalPages Then
            currentPage += 1
            LoadOnlineOrders()
        End If
    End Sub

    ''' <summary>
    ''' Displays online orders using card templates
    ''' </summary>
    ''' <summary>
    ''' Displays online orders using card templates
    ''' </summary>
    Private Sub DisplayOnlineOrders(orders As List(Of OnlineOrder))
        ' Keep the template and other non-panel controls
        Dim controlsKeep As New List(Of Control)
        For Each ctrl As Control In Panel1.Controls
            If ctrl Is ResTemplate OrElse ctrl Is btnRefresh OrElse ctrl Is cmbFilterStatus Then
                controlsKeep.Add(ctrl)
            End If
        Next

        Panel1.Controls.Clear()

        ' Re-add kept controls
        For Each ctrl In controlsKeep
            Panel1.Controls.Add(ctrl)
        Next

        If orders.Count = 0 Then
            Dim lblEmpty As New Label With {
                .Text = "No online orders found",
                .Font = New Font("Segoe UI", 14, FontStyle.Italic),
                .ForeColor = Color.Gray,
                .Location = New Point(600, 300),
                .AutoSize = True
            }
            Panel1.Controls.Add(lblEmpty)
            Return
        End If

        ' Create cards for each order
        Dim xPos As Integer = 38
        Dim yPos As Integer = 119
        Dim cardsPerRow As Integer = 3
        Dim cardCount As Integer = 0

        For Each order In orders
            Dim orderCard As Panel = CreateOrderCard(order)
            orderCard.Location = New Point(xPos, yPos)
            Panel1.Controls.Add(orderCard)

            cardCount += 1
            If cardCount Mod cardsPerRow = 0 Then
                ' Move to next row
                xPos = 38
                yPos += 330
            Else
                ' Move to next column
                xPos += 450
            End If
        Next
    End Sub

    ''' <summary>
    ''' Creates an order card from the template
    ''' </summary>
    Private Function CreateOrderCard(order As OnlineOrder) As Panel
        ' Clone the ResTemplate panel
        Dim panel As New Panel With {
            .Size = ResTemplate.Size,
            .BackColor = ResTemplate.BackColor,
            .BorderStyle = ResTemplate.BorderStyle
        }

        ' Clone controls using helper methods
        Dim lblName As Label = CloneLabel(lblName2)
        lblName.Text = order.CustomerName

        Dim lblCode As Label = CloneLabel(lblCode2)
        lblCode.Text = $"ORD-{order.OrderID:D4}"

        Dim lblEmailClone As Label = CloneLabel(lblEmail)
        lblEmailClone.Text = If(String.IsNullOrEmpty(order.Email), "No email", order.Email)

        Dim lblPhone As Label = CloneLabel(lblPhone2)
        lblPhone.Text = If(String.IsNullOrEmpty(order.ContactNumber), "No phone", order.ContactNumber)

        Dim lblDate As Label = CloneLabel(lblDate2)
        lblDate.Text = order.OrderDate.ToString("MMM dd, yyyy")

        Dim lblTime As Label = CloneLabel(lblTime2)
        lblTime.Text = DateTime.Today.Add(order.OrderTime).ToString("h:mm tt")

        ' Status Button (Button2 in template)
        Dim btnStatus As Button = CloneButton(Button2)
        btnStatus.Text = order.OrderStatus
        btnStatus.BackColor = GetStatusColor(order.OrderStatus)
        btnStatus.ForeColor = Color.White
        
        ' Icons
        Dim iconEmail As PictureBox = ClonePictureBox(PictureBox8)
        Dim iconPhone As PictureBox = ClonePictureBox(PictureBox3)
        Dim iconDate As PictureBox = ClonePictureBox(PictureBox5)
        Dim iconTime As PictureBox = ClonePictureBox(PictureBox6)

        ' View Details Button (Button1 in template)
        Dim btnView As Button = CloneButton(Button1)
        btnView.Text = "View Details"
        AddHandler btnView.Click, Sub(s, ev) ShowOrderDetails(order)

        ' Receipt Preview Button (Button3 in template)
        Dim btnPreview As Button = CloneButton(Button3)
        btnPreview.Text = "Receipt Preview"
        AddHandler btnPreview.Click, Sub(s, ev) ShowReceiptPreview(order)

        ' Add controls to panel
        panel.Controls.AddRange({
            lblName, lblCode, iconEmail, lblEmailClone, iconPhone, lblPhone,
            iconDate, lblDate, iconTime, lblTime, btnStatus, btnView, btnPreview
        })

        Return panel
    End Function

    Private Sub ShowReceiptPreview(order As OnlineOrder)
        Try
            ' Get items for the receipt
            Dim items As List(Of OrderItem) = orderRepository.GetOrderItems(order.OrderID)
            
            ' Generate text content matching the image format
            Dim sb As New StringBuilder()
            Dim w As Integer = 60 ' Width for separators
            Dim line As String = New String("-"c, w)
            Dim dblLine As String = New String("="c, w)
            
            sb.AppendLine(dblLine)
            sb.AppendLine(CenterText("TABEYA RESTAURANT", w))
            sb.AppendLine(CenterText("Official Sales Receipt", w))
            sb.AppendLine(dblLine)
            sb.AppendLine()
            
            sb.AppendLine($"Order No.:   ORD-{order.OrderID:D4}")
            sb.AppendLine($"Date:        {DateTime.Now:yyyy-MM-dd}   |   Time: {DateTime.Now:HH:mm tt}")
            sb.AppendLine($"Cashier:     Online System")
            sb.AppendLine($"Customer:    {order.CustomerName}")
            sb.AppendLine()
            
            sb.AppendLine(line)
            sb.AppendLine(CenterText("ITEMS PURCHASED", w))
            sb.AppendLine(line)
            sb.AppendLine()
            
            Dim totalAmount As Decimal = 0
            
            For Each item In items
                Dim lineTotal As Decimal = item.UnitPrice * item.Quantity
                totalAmount += lineTotal
                
                ' Format: 1 x Item Name ............. P 100.00
                Dim itemStr As String = $"{item.Quantity} x {item.ProductName}"
                Dim priceStr As String = $"P {lineTotal:N2}"
                
                ' padding calculation
                Dim padding As Integer = w - itemStr.Length - priceStr.Length
                If padding < 1 Then padding = 1
                
                sb.AppendLine($"{itemStr}{New String(" "c, padding)}{priceStr}")
                
                ' Add fake batch info to match image style (optional, but requested to look like image)
                sb.AppendLine($"   - Batch: N/A") 
                sb.AppendLine($"   - Qty Deducted: {item.Quantity}")
                sb.AppendLine()
            Next
            
            sb.AppendLine(line)
            Dim subtotalStr As String = $"P {totalAmount:N2}"
            sb.AppendLine($"SUBTOTAL:{New String(" "c, w - 9 - subtotalStr.Length)}{subtotalStr}")
            sb.AppendLine(line)
            
            Dim totalStr As String = $"P {totalAmount:N2}"
            sb.AppendLine($"TOTAL:{New String(" "c, w - 6 - totalStr.Length)}{totalStr}")
            sb.AppendLine(line)
            sb.AppendLine()
            
            sb.AppendLine("Payment Method: ONLINE/PENDING")
            sb.AppendLine($"Amount Given:   P {totalAmount:N2}") ' Assuming exact payment for online
            sb.AppendLine($"Change:         P 0.00")
            sb.AppendLine()
            sb.AppendLine(line)
            sb.AppendLine()
            sb.AppendLine(CenterText("THANK YOU FOR YOUR PURCHASE!", w))
            sb.AppendLine(dblLine)
            
            ' Show Preview Form
            Dim previewForm As New ReceiptPreviewForm(order.OrderID, "OnlineOrder", sb.ToString())
            previewForm.ShowDialog()
            
        Catch ex As Exception
            MessageBox.Show($"Error generating preview: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function CenterText(text As String, width As Integer) As String
        If text.Length >= width Then Return text
        Dim leftPadding As Integer = (width - text.Length) \ 2
        Return New String(" "c, leftPadding) & text
    End Function

    ' Helper methods for cloning controls
    Private Function CloneLabel(template As Label) As Label
        Return New Label With {
            .Text = template.Text,
            .Font = template.Font,
            .ForeColor = template.ForeColor,
            .BackColor = template.BackColor,
            .Location = template.Location,
            .Size = template.Size,
            .AutoSize = template.AutoSize,
            .Padding = template.Padding
        }
    End Function

    Private Function ClonePictureBox(template As PictureBox) As PictureBox
        Return New PictureBox With {
            .Image = template.Image,
            .Location = template.Location,
            .Size = template.Size,
            .SizeMode = template.SizeMode
        }
    End Function

    Private Function CloneButton(template As Button) As Button
        Return New Button With {
            .Text = template.Text,
            .Font = template.Font,
            .BackColor = template.BackColor,
            .ForeColor = template.ForeColor,
            .FlatStyle = template.FlatStyle,
            .Location = template.Location,
            .Size = template.Size
        }
    End Function

    ''' <summary>
    ''' Returns color based on order status
    ''' </summary>
    Private Function GetStatusColor(status As String) As Color
        Select Case status.ToLower()
            Case "preparing"
                Return Color.FromArgb(255, 127, 39) ' Orange
            Case "served"
                Return Color.FromArgb(40, 167, 69) ' Green
            Case "completed"
                Return Color.FromArgb(40, 167, 69) ' Green
            Case "cancelled"
                Return Color.FromArgb(220, 53, 69) ' Red
            Case Else
                Return Color.Gray
        End Select
    End Function

    ''' <summary>
    ''' Shows order details and actions
    ''' </summary>
    Private Sub ShowOrderDetails(order As OnlineOrder)
        Try
            ' Load order items from database
            Dim items As List(Of OrderItem) = orderRepository.GetOrderItems(order.OrderID)

            ' Show the order items form
            Dim viewForm As New ViewOnlineOrderItemsForm(
                order.OrderID,
                $"ORD-{order.OrderID:D4}",
                order.CustomerName,
                items,
                order.TotalAmount
            )
            viewForm.ShowDialog()

        Catch ex As Exception
            MessageBox.Show($"Error loading order items: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Updates order status in the database
    ''' </summary>
    Private Sub UpdateOrderStatus(orderID As Integer, newStatus As String)
        Try
            orderRepository.UpdateOrderStatus(orderID, newStatus)
            MessageBox.Show($"Order #{orderID} status updated to {newStatus}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            LoadOnlineOrders() ' Refresh the list
        Catch ex As Exception
            MessageBox.Show($"Error updating order status: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Refresh button click handler
    ''' </summary>
    Private Sub btnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
        currentPage = 1
        LoadOnlineOrders()
    End Sub

    ''' <summary>
    ''' Filter by status
    ''' </summary>
    Private Sub cmbFilterStatus_SelectedIndexChanged(sender As Object, e As EventArgs)
        currentPage = 1
        LoadOnlineOrders()
    End Sub

    Private Sub FilterOrdersByStatus(status As String)
        ' Legacy method removed - replaced by DB filtering in LoadOnlineOrders
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

    End Sub
End Class
