Imports System.Collections.Generic

Public Class DashboardForm
    Private orderRepository As New OrderRepository()
    Private reservationRepository As New ReservationRepository()
    Private WithEvents dashboardTimer As New Timer()
    
    ' Active Orders Pagination
    Private activeOrdersPage As Integer = 1
    Private activeOrdersPageSize As Integer = 100 ' Updated to 100
    Private activeOrdersTotalPages As Integer = 1
    
    ' Reservations Pagination
    Private reservationsPage As Integer = 1
    Private reservationsPageSize As Integer = 100 ' Updated to 100
    Private reservationsTotalPages As Integer = 1
    
    ' Controls (Active Orders)
    Private btnPrevOrders As Button
    Private btnNextOrders As Button
    Private txtPageOrders As TextBox
    Private lblTotalPagesOrders As Label
    
    ' Controls (Reservations)
    Private btnPrevRes As Button
    Private btnNextRes As Button
    Private txtPageRes As TextBox
    Private lblTotalPagesRes As Label

    ''' <summary>
    ''' Loads dashboard statistics when form loads
    ''' </summary>
    ''' <summary>
    ''' Loads dashboard statistics when form loads
    ''' </summary>
    Private Async Sub DashboardForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        dashboardTimer.Interval = 1000
        dashboardTimer.Start()
        
        InitializePaginationControls()
        
        LoadDashboardStatistics()
        
        ' Load critical data in parallel
        Dim taskOrders = LoadActiveOrdersAsync()
        Dim taskReservations = LoadTodayReservationsAsync()
        
        Await Task.WhenAll(taskOrders, taskReservations)
    End Sub

    Private Sub InitializePaginationControls()
        ' Active Orders Pagination Controls
        Dim pnlOrdersPager As New Panel With {
            .Height = 40,
            .Dock = DockStyle.Bottom,
            .BackColor = Color.White
        }
        
        btnPrevOrders = CreatePaginationButton("<", 10, 5)
        AddHandler btnPrevOrders.Click, AddressOf btnPrevOrders_Click
        
        btnNextOrders = CreatePaginationButton(">", 160, 5)
        AddHandler btnNextOrders.Click, AddressOf btnNextOrders_Click
        
        txtPageOrders = New TextBox With {
            .Text = "1",
            .Size = New Size(40, 25),
            .TextAlign = HorizontalAlignment.Center,
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(60, 8)
        }
        AddHandler txtPageOrders.KeyDown, AddressOf txtPageOrders_KeyDown
        AddHandler txtPageOrders.Leave, Sub(s, ev) ValidateAndJumpOrders()

        lblTotalPagesOrders = New Label With {
            .Text = "/ 1",
            .AutoSize = False,
            .Size = New Size(50, 30),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Location = New Point(105, 5),
            .Font = New Font("Segoe UI", 9)
        }
        
        pnlOrdersPager.Controls.AddRange({btnPrevOrders, btnNextOrders, txtPageOrders, lblTotalPagesOrders})
        
        ' Add to TableLayoutPanel1's parent or ensure it sits below list
        ' Assuming TableLayoutPanel1 is inside a container, ideally we dock this to bottom
        ' If TableLayoutPanel1 is Dock.Fill, we need to adjust hierarchy.
        ' Strategy: Add panel to TableLayoutPanel1's Parent. 
        ' Important: Check if TableLayoutPanel1 is directly on Form or in a Panel.
        ' Assuming it's in a panel (e.g., pnlActiveOrders) based on standard design.
        If TableLayoutPanel1.Parent IsNot Nothing Then
             TableLayoutPanel1.Parent.Controls.Add(pnlOrdersPager)
             pnlOrdersPager.BringToFront()
             ' Adjust TLP height if needed or let Dock handle it if parent acts correctly
        End If

        ' Reservations Pagination Controls
        Dim pnlResPager As New Panel With {
            .Height = 40,
            .Dock = DockStyle.Bottom,
            .BackColor = Color.White
        }
        
        btnPrevRes = CreatePaginationButton("<", 10, 5)
        AddHandler btnPrevRes.Click, AddressOf btnPrevRes_Click
        
        btnNextRes = CreatePaginationButton(">", 160, 5)
        AddHandler btnNextRes.Click, AddressOf btnNextRes_Click
        
        txtPageRes = New TextBox With {
            .Text = "1",
            .Size = New Size(40, 25),
            .TextAlign = HorizontalAlignment.Center,
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(60, 8)
        }
        AddHandler txtPageRes.KeyDown, AddressOf txtPageRes_KeyDown
        AddHandler txtPageRes.Leave, Sub(s, ev) ValidateAndJumpReservations()

        lblTotalPagesRes = New Label With {
            .Text = "/ 1",
            .AutoSize = False,
            .Size = New Size(50, 30),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Location = New Point(105, 5),
            .Font = New Font("Segoe UI", 9)
        }
        
        pnlResPager.Controls.AddRange({btnPrevRes, btnNextRes, txtPageRes, lblTotalPagesRes})
        
        If TableLayoutPanel2.Parent IsNot Nothing Then
             TableLayoutPanel2.Parent.Controls.Add(pnlResPager)
             pnlResPager.BringToFront()
        End If
    End Sub
    
    Private Function CreatePaginationButton(text As String, x As Integer, y As Integer) As Button
        Return New Button With {
            .Text = text,
            .Location = New Point(x, y),
            .Size = New Size(40, 30),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.WhiteSmoke
        }
    End Function

    ''' <summary>
    ''' Loads and displays dashboard statistics (orders, reservations, feedback counts)
    ''' </summary>
    Private Sub LoadDashboardStatistics()
        Try
            ' Load today's orders count
            Dim todayOrdersCount As Integer = orderRepository.GetTodayOrdersCount()
            lblCardOrdersValue.Text = todayOrdersCount.ToString()

            ' Load today's reservations count
            Dim todayReservationsCount As Integer = reservationRepository.GetTodayReservationsCount()
            lblCardReservationsValue.Text = todayReservationsCount.ToString()

            ' Load current time
            Dim currentTime As String = DateTime.Now.ToString("h:mm tt")
            lblCardTimeValue.Text = currentTime

            ' Load feedback count (placeholder logic as per original)
            ' Assuming we might add FeedbackRepository later
            ' Dim feedbackQuery As String = "SELECT COUNT(*) FROM customers WHERE FeedbackCount > 0 AND DATE(LastTransactionDate) = CURDATE()"
            ' Dim feedbackCount As Object = modDB.ExecuteScalar(feedbackQuery)
            ' If feedbackCount IsNot Nothing Then
            '     lblCardFeedbackValue.Text = feedbackCount.ToString()
            ' Else
            '     lblCardFeedbackValue.Text = "0"
            ' End If
        Catch ex As Exception
            MessageBox.Show($"Error loading dashboard statistics: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Loads active orders (orders with status 'Preparing' or 'Served')
    ''' and displays them in the active orders panel
    ''' </summary>
    Private Async Function LoadActiveOrdersAsync() As Task
        Try
            Dim totalCount As Integer = Await orderRepository.GetTotalActiveOrdersCountAsync()
            activeOrdersTotalPages = Math.Max(1, CInt(Math.Ceiling(totalCount / activeOrdersPageSize)))
            
            ' Validate page
            If activeOrdersPage > activeOrdersTotalPages Then activeOrdersPage = activeOrdersTotalPages
            If activeOrdersPage < 1 Then activeOrdersPage = 1
            
            Dim offset As Integer = (activeOrdersPage - 1) * activeOrdersPageSize
            Dim activeOrders As List(Of Order) = Await orderRepository.GetActiveOrdersPagedAsync(activeOrdersPageSize, offset)
            
            DisplayActiveOrders(activeOrders)
            UpdateActiveOrdersControls()
            
        Catch ex As Exception
            ' Log error but don't crash dashboard
            Console.WriteLine($"Error loading active orders: {ex.Message}")
        End Try
    End Function


    
    Private Sub UpdateActiveOrdersControls()
        If txtPageOrders IsNot Nothing Then
            txtPageOrders.Text = activeOrdersPage.ToString()
            lblTotalPagesOrders.Text = $"/ {activeOrdersTotalPages}"
            btnPrevOrders.Enabled = (activeOrdersPage > 1)
            btnNextOrders.Enabled = (activeOrdersPage < activeOrdersTotalPages)
        End If
    End Sub

    Private Sub txtPageOrders_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            e.Handled = True
            ValidateAndJumpOrders()
        End If
    End Sub

    Private Async Sub ValidateAndJumpOrders()
        Dim newPage As Integer
        If Integer.TryParse(txtPageOrders.Text, newPage) Then
            If newPage < 1 Then newPage = 1
            If newPage > activeOrdersTotalPages Then newPage = activeOrdersTotalPages
            
            If newPage <> activeOrdersPage Then
                activeOrdersPage = newPage
                Await LoadActiveOrdersAsync()
            Else
                txtPageOrders.Text = activeOrdersPage.ToString()
            End If
        Else
            txtPageOrders.Text = activeOrdersPage.ToString()
        End If
    End Sub
    
    Private Async Sub btnPrevOrders_Click(sender As Object, e As EventArgs)
        If activeOrdersPage > 1 Then
            activeOrdersPage -= 1
            Await LoadActiveOrdersAsync()
        End If
    End Sub

    Private Async Sub btnNextOrders_Click(sender As Object, e As EventArgs)
        If activeOrdersPage < activeOrdersTotalPages Then
            activeOrdersPage += 1
            Await LoadActiveOrdersAsync()
        End If
    End Sub

    ''' <summary>
    ''' Displays active orders in the UI
    ''' </summary>
    Private Sub DisplayActiveOrders(orders As List(Of Order))
        TableLayoutPanel1.Controls.Clear()
        TableLayoutPanel1.RowStyles.Clear()
        TableLayoutPanel1.ColumnStyles.Clear()

        ' Setup main table
        TableLayoutPanel1.ColumnCount = 1
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        TableLayoutPanel1.RowCount = 0
        TableLayoutPanel1.Dock = DockStyle.Fill
        TableLayoutPanel1.AutoScroll = True

        If orders.Count = 0 Then
            TableLayoutPanel1.RowCount = 1
            TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 50.0F))

            Dim lblEmpty As New Label With {
            .Text = "No active orders",
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 10, FontStyle.Italic),
            .ForeColor = Color.Gray
        }

            TableLayoutPanel1.Controls.Add(lblEmpty, 0, 0)
            Return
        End If

        ' Add each order
        For Each order As Order In orders
            TableLayoutPanel1.RowCount += 1
            TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 65.0F))

            ' Parent panel for each order
            Dim itemPanel As New Panel With {
                .BackColor = Color.White,
                .Margin = New Padding(4),
                .Dock = DockStyle.Fill,
                .Tag = order, ' Store Order object
                .Padding = New Padding(15, 10, 15, 10)
            }

            ' Order ID (Top Left)
            Dim lblID As New Label With {
                .Text = $"#{order.OrderID}",
                .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                .ForeColor = Color.Black,
                .AutoSize = True,
                .Location = New Point(20, 5)
            }

            ' Status (Below ID)
            Dim lblStatus As New Label With {
                .Name = "lblStatus",
                .Text = order.OrderStatus,
                .Font = New Font("Segoe UI", 9, FontStyle.Bold),
                .ForeColor = If(order.OrderStatus = "Preparing", Color.Orange, Color.Green),
                .AutoSize = True,
                .Location = New Point(20, 30)
            }

            ' Time (Center Left)
            Dim lblTime As New Label With {
                .Text = DateTime.Today.Add(order.OrderTime).ToString("h:mm tt"),
                .Font = New Font("Segoe UI", 9),
                .ForeColor = Color.Black,
                .AutoSize = True,
                .Location = New Point(200, 10)
            }

            ' Type (Below Time)
            Dim lblType As New Label With {
                .Text = order.OrderType,
                .Font = New Font("Segoe UI", 9),
                .ForeColor = Color.Gray,
                .AutoSize = True,
                .Location = New Point(200, 30)
            }

            ' Countdown (Center Right)
            Dim lblCountdown As New Label With {
                .Name = "lblCountdown",
                .Text = "...",
                .Font = New Font("Segoe UI", 10),
                .ForeColor = Color.Black,
                .AutoSize = True,
                .Location = New Point(330, 20)
            }

            ' Amount (Top Right)
            Dim lblAmount As New Label With {
                .Text = $"₱{order.TotalAmount:F2}",
                .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                .ForeColor = Color.Black,
                .AutoSize = True,
                .Location = New Point(80, 15),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }

            ' Cancel Button (Bottom Right)
            Dim btnCancel As New Button With {
                .Name = "btnCancel",
                .Text = "Cancel Order",
                .Font = New Font("Segoe UI", 8, FontStyle.Bold),
                .BackColor = Color.FromArgb(255, 127, 39),
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .AutoSize = True,
                .Size = New Size(80, 30),
                .Location = New Point(-90, 15),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .Enabled = (order.OrderStatus = "Preparing")
            }
            btnCancel.FlatAppearance.BorderSize = 0

            AddHandler btnCancel.Click, Sub(s, ev) CancelOrder(order)

            ' Add controls to panel
            itemPanel.Controls.AddRange({lblID, lblStatus, lblTime, lblType, lblCountdown, lblAmount, btnCancel})

            ' Add final panel to list
            TableLayoutPanel1.Controls.Add(itemPanel)
        Next
    End Sub


    ''' <summary>
    ''' Loads today's reservations and displays them in the reservations panel
    ''' </summary>
    ''' <summary>
    ''' Loads today's reservations and displays them in the reservations panel
    ''' </summary>
    Private Async Function LoadTodayReservationsAsync() As Task
        Try
            Dim totalCount As Integer = Await reservationRepository.GetTotalTodayReservationsCountAsync()
            reservationsTotalPages = Math.Max(1, CInt(Math.Ceiling(totalCount / reservationsPageSize)))
            
            ' Validate page
            If reservationsPage > reservationsTotalPages Then reservationsPage = reservationsTotalPages
            If reservationsPage < 1 Then reservationsPage = 1
            
            Dim offset As Integer = (reservationsPage - 1) * reservationsPageSize
            Dim reservations As List(Of Reservation) = Await reservationRepository.GetTodayReservationsPagedAsync(reservationsPageSize, offset)
            
            DisplayTodayReservations(reservations)
            UpdateReservationsControls()
            
        Catch ex As Exception
            Console.WriteLine($"Error loading reservations: {ex.Message}")
        End Try
    End Function



    Private Sub UpdateReservationsControls()
        If txtPageRes IsNot Nothing Then
            txtPageRes.Text = reservationsPage.ToString()
            lblTotalPagesRes.Text = $"/ {reservationsTotalPages}"
            btnPrevRes.Enabled = (reservationsPage > 1)
            btnNextRes.Enabled = (reservationsPage < reservationsTotalPages)
        End If
    End Sub

    Private Sub txtPageRes_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            e.Handled = True
            ValidateAndJumpReservations()
        End If
    End Sub

    Private Async Sub ValidateAndJumpReservations()
        Dim newPage As Integer
        If Integer.TryParse(txtPageRes.Text, newPage) Then
            If newPage < 1 Then newPage = 1
            If newPage > reservationsTotalPages Then newPage = reservationsTotalPages
            
            If newPage <> reservationsPage Then
                reservationsPage = newPage
                Await LoadTodayReservationsAsync()
            Else
                txtPageRes.Text = reservationsPage.ToString()
            End If
        Else
            txtPageRes.Text = reservationsPage.ToString()
        End If
    End Sub
    
    Private Async Sub btnPrevRes_Click(sender As Object, e As EventArgs)
        If reservationsPage > 1 Then
            reservationsPage -= 1
            Await LoadTodayReservationsAsync()
        End If
    End Sub

    Private Async Sub btnNextRes_Click(sender As Object, e As EventArgs)
        If reservationsPage < reservationsTotalPages Then
            reservationsPage += 1
            Await LoadTodayReservationsAsync()
        End If
    End Sub

    ''' <summary>
    ''' Displays today's reservations in the UI
    ''' </summary>
    Private Sub DisplayTodayReservations(reservations As List(Of Reservation))
        TableLayoutPanel2.Controls.Clear()
        TableLayoutPanel2.RowStyles.Clear()
        TableLayoutPanel2.ColumnStyles.Clear()

        ' Setup main table
        TableLayoutPanel2.ColumnCount = 1
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        TableLayoutPanel2.RowCount = 0
        TableLayoutPanel2.Dock = DockStyle.Fill
        TableLayoutPanel2.AutoScroll = True

        If reservations.Count = 0 Then
            TableLayoutPanel2.RowCount = 1
            TableLayoutPanel2.RowStyles.Add(New RowStyle(SizeType.Absolute, 50.0F))

            Dim lblEmpty As New Label With {
            .Text = "No reservations today",
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 10, FontStyle.Italic),
            .ForeColor = Color.Gray
        }

            TableLayoutPanel2.Controls.Add(lblEmpty, 0, 0)
            Return
        End If

        ' Add each reservation row
        For Each res As Reservation In reservations
            TableLayoutPanel2.RowCount += 1
            TableLayoutPanel2.RowStyles.Add(New RowStyle(SizeType.Absolute, 65.0F))

            ' Row panel
            Dim itemPanel As New Panel With {
                .BackColor = Color.White,
                .Margin = New Padding(4),
                .Dock = DockStyle.Fill,
                .Padding = New Padding(15, 10, 15, 10),
                .Tag = res
            }

            ' Left: Guest Name
            Dim lblName As New Label With {
                .Text = res.CustomerName,
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .ForeColor = Color.Black,
                .AutoSize = True,
                .Location = New Point(20, 20)
            }

            ' Center: Time
            Dim lblTime As New Label With {
                .Text = DateTime.Today.Add(res.EventTime).ToString("h:mm tt"),
                .Font = New Font("Segoe UI", 9, FontStyle.Regular),
                .ForeColor = Color.Black,
                .AutoSize = True,
                .Location = New Point(330, 10)
            }

            ' Center: Guests
            Dim lblGuests As New Label With {
                .Text = $"{res.NumberOfGuests} Guests",
                .Font = New Font("Segoe UI", 8, FontStyle.Regular),
                .ForeColor = Color.Gray,
                .AutoSize = True,
                .Location = New Point(330, 30)
            }

            ' Right: Status with color
            Dim statusColor As Color = Color.Gray
            Select Case res.ReservationStatus.ToLower()
                Case "confirmed"
                    statusColor = Color.FromArgb(40, 167, 69)  ' Green
                Case "pending"
                    statusColor = Color.FromArgb(255, 127, 39) ' Orange
                Case "cancelled"
                    statusColor = Color.FromArgb(220, 53, 69)  ' Red
            End Select

            Dim lblStatus As New Label With {
                .Name = "lblStatus",
                .Text = res.ReservationStatus,
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .ForeColor = statusColor,
                .AutoSize = True,
                .Location = New Point(80, 20),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }

            ' Countdown timer (Center Right)
            Dim lblCountdown As New Label With {
                .Name = "lblCountdown",
                .Text = "...",
                .Font = New Font("Segoe UI", 10),
                .ForeColor = Color.Black,
                .AutoSize = True,
                .Location = New Point(200, 20)
            }

            ' Add labels to panel
            itemPanel.Controls.Add(lblName)
            itemPanel.Controls.Add(lblTime)
            itemPanel.Controls.Add(lblGuests)
            itemPanel.Controls.Add(lblStatus)
            itemPanel.Controls.Add(lblCountdown)

            ' Add panel to table
            TableLayoutPanel2.Controls.Add(itemPanel)
        Next
    End Sub



    Private Sub pnlTodayReservationsPlaceholder_Paint(sender As Object, e As PaintEventArgs) Handles pnlTodayReservationsPlaceholder.Paint

    End Sub
    Private Async Sub CancelOrder(order As Order)
        If MessageBox.Show($"Are you sure you want to cancel Order #{order.OrderID}?", "Confirm Cancel", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
            Try
                orderRepository.UpdateOrderStatus(order.OrderID, "Cancelled")
                Await LoadActiveOrdersAsync() ' Refresh list
            Catch ex As Exception
                MessageBox.Show($"Error cancelling order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub

    Private Sub dashboardTimer_Tick(sender As Object, e As EventArgs) Handles dashboardTimer.Tick
        For Each itemPanel As Control In TableLayoutPanel1.Controls
            If TypeOf itemPanel Is Panel AndAlso itemPanel.Tag IsNot Nothing Then
                Dim order As Order = TryCast(itemPanel.Tag, Order)
                If order Is Nothing Then Continue For

                Dim lblStatus As Label = itemPanel.Controls.OfType(Of Label).FirstOrDefault(Function(l) l.Name = "lblStatus")
                Dim lblCountdown As Label = itemPanel.Controls.OfType(Of Label).FirstOrDefault(Function(l) l.Name = "lblCountdown")
                Dim btnCancel As Button = itemPanel.Controls.OfType(Of Button).FirstOrDefault(Function(l) l.Name = "btnCancel")

                If lblStatus Is Nothing OrElse lblCountdown Is Nothing OrElse btnCancel Is Nothing Then Continue For

                Dim startTime As DateTime = order.OrderDate.Date + order.OrderTime
                Dim prepMinutes As Integer = 15 ' Default 15 minutes prep time
                Dim elapsed As TimeSpan = DateTime.Now - startTime

                ' Logic
                If elapsed.TotalMinutes < prepMinutes Then
                    ' Preparing
                    Dim remaining As TimeSpan = TimeSpan.FromMinutes(prepMinutes) - elapsed
                    If remaining.TotalHours >= 1 Then
                        lblCountdown.Text = remaining.ToString("h\:mm\:ss")
                    Else
                        lblCountdown.Text = remaining.ToString("mm\:ss")
                    End If

                    If lblStatus.Text <> "Preparing" Then
                        lblStatus.Text = "Preparing"
                        lblStatus.ForeColor = Color.Orange
                        btnCancel.Enabled = True
                        If order.OrderStatus <> "Preparing" Then
                            order.OrderStatus = "Preparing"
                        End If
                    End If

                ElseIf elapsed.TotalMinutes < prepMinutes + 1.5 Then
                    ' Serving (1 min 30 sec fixed)
                    Dim servingElapsed As TimeSpan = elapsed - TimeSpan.FromMinutes(prepMinutes)
                    Dim servingRemaining As TimeSpan = TimeSpan.FromMinutes(1.5) - servingElapsed
                    
                    lblCountdown.Text = servingRemaining.ToString("h\:mm\:ss")

                    If lblStatus.Text <> "Serving" Then
                        lblStatus.Text = "Serving"
                        lblStatus.ForeColor = Color.Green

                        If order.OrderStatus <> "Serving" Then
                            order.OrderStatus = "Serving"
                            orderRepository.UpdateOrderStatus(order.OrderID, "Serving")
                        End If
                    End If
                Else
                    ' Completed
                    lblCountdown.Text = "00:00"

                    If lblStatus.Text <> "Completed" Then
                        lblStatus.Text = "Completed"
                        lblStatus.ForeColor = Color.Gray
                        btnCancel.Enabled = False

                        If order.OrderStatus <> "Completed" AndAlso order.OrderStatus <> "Served" Then
                            order.OrderStatus = "Completed"
                            orderRepository.UpdateOrderStatus(order.OrderID, "Completed")
                        End If
                    End If
                End If
            End If
        Next

        ' Handle Reservation Timers (TableLayoutPanel2)
        For Each itemPanel As Control In TableLayoutPanel2.Controls
            If TypeOf itemPanel Is Panel AndAlso itemPanel.Tag IsNot Nothing Then
                Dim reservation As Reservation = TryCast(itemPanel.Tag, Reservation)
                If reservation Is Nothing Then Continue For

                Dim lblStatus As Label = itemPanel.Controls.OfType(Of Label).FirstOrDefault(Function(l) l.Name = "lblStatus")
                Dim lblCountdown As Label = itemPanel.Controls.OfType(Of Label).FirstOrDefault(Function(l) l.Name = "lblCountdown")

                If lblStatus Is Nothing OrElse lblCountdown Is Nothing Then Continue For

                ' Calculate times
                Dim eventDateTime As DateTime = DateTime.Today.Add(reservation.EventTime)
                Dim prepMinutes As Integer = If(reservation.PrepTime > 0, reservation.PrepTime, 15) ' Default 15 if no prep time
                Dim startTime As DateTime = eventDateTime.AddMinutes(-prepMinutes)
                Dim now As DateTime = DateTime.Now

                ' Timer logic
                If now < startTime Then
                    ' Before prep should start - show waiting time
                    Dim waitTime As TimeSpan = startTime - now
                    lblCountdown.Text = String.Format("Starts in {0:mm\:ss}", waitTime)
                    lblCountdown.ForeColor = Color.Gray
                ElseIf now >= startTime AndAlso now < eventDateTime Then
                    ' During prep - countdown to event time
                    Dim remaining As TimeSpan = eventDateTime - now
                    If remaining.TotalHours >= 1 Then
                        lblCountdown.Text = String.Format("{0:h\:mm\:ss}", remaining)
                    Else
                        lblCountdown.Text = String.Format("{0:mm\:ss}", remaining)
                    End If
                    lblCountdown.ForeColor = Color.Orange
                    lblStatus.ForeColor = Color.Orange
                Else
                    ' Event time reached or passed - ready to serve
                    lblCountdown.Text = "00:00"
                    lblCountdown.ForeColor = Color.Green
                    lblStatus.ForeColor = Color.FromArgb(40, 167, 69)
                End If
            End If
        Next
    End Sub
End Class
