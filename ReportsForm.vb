Imports System.Collections.Generic

Public Class ReportsForm
    Private orderRepository As New OrderRepository()
    Private reservationRepository As New ReservationRepository()
    Private reportRepository As New ReportRepository()
    
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

    Private Sub ReportsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        InitializePaginationControls()
        LoadDailyReports()
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
        ' pnlMain.Controls.Add(pnlPagination) ' Removed as pnlMain does not exist
        Me.Controls.Add(pnlPagination)
        pnlPagination.BringToFront()
    End Sub

    ''' <summary>
    ''' Loads all daily report data
    ''' </summary>
    Private Sub LoadDailyReports()
        Try
            LoadTodaySales()
            LoadOrdersHandled()
            LoadReservationsHandled()
            LoadTodayOrders()
        Catch ex As Exception
            MessageBox.Show($"Error loading daily reports: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Loads today's total sales from orders
    ''' </summary>
    Private Sub LoadTodaySales()
        Try
            ' Calculate orders total
            Dim ordersQuery As String = "SELECT COALESCE(SUM(TotalAmount), 0) FROM orders WHERE DATE(OrderDate) = CURDATE() AND OrderStatus != 'Cancelled'"
            Dim ordersTotal As Decimal = CDec(modDB.ExecuteScalar(ordersQuery))

            ' Calculate reservations total
            Dim reservationsQuery As String = "SELECT COALESCE(SUM(ri.Quantity * ri.UnitPrice), 0) " &
                                              "FROM reservation_items ri " &
                                              "JOIN reservations r ON ri.ReservationID = r.ReservationID " &
                                              "WHERE DATE(r.EventDate) = CURDATE() AND r.ReservationStatus IN ('Accepted', 'Confirmed')"
            Dim reservationsTotal As Decimal = CDec(modDB.ExecuteScalar(reservationsQuery))

            ' Update label with combined total
            Dim totalSales As Decimal = ordersTotal + reservationsTotal
            lblSalesValue.Text = $"₱ {totalSales:N0}"

        Catch ex As Exception
            lblSalesValue.Text = "₱ 0"
            Console.WriteLine($"Error loading sales: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Loads count of today's orders
    ''' </summary>
    Private Sub LoadOrdersHandled()
        Try
            Dim query As String = "SELECT COUNT(*) FROM orders WHERE DATE(OrderDate) = CURDATE() AND OrderStatus != 'Cancelled'"
            Dim result As Object = modDB.ExecuteScalar(query)

            If result IsNot Nothing AndAlso IsNumeric(result) Then
                lblOrdersValue.Text = result.ToString()
            Else
                lblOrdersValue.Text = "0"
            End If
        Catch ex As Exception
            lblOrdersValue.Text = "0"
            Console.WriteLine($"Error loading orders count: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Loads count of reservations confirmed/accepted today
    ''' </summary>
    Private Sub LoadReservationsHandled()
        Try
            ' Count reservations that were confirmed today
            Dim query As String = "SELECT COUNT(*) FROM reservations WHERE DATE(EventDate) = CURDATE() AND ReservationStatus IN ('Accepted', 'Confirmed')"
            Dim result As Object = modDB.ExecuteScalar(query)

            If result IsNot Nothing AndAlso IsNumeric(result) Then
                lblReservationsValue.Text = result.ToString()
            Else
                lblReservationsValue.Text = "0"
            End If
        Catch ex As Exception
            lblReservationsValue.Text = "0"
            Console.WriteLine($"Error loading reservations count: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Loads today's orders and reservations, and displays them in table
    ''' </summary>
    ''' <summary>
    ''' Loads today's orders and reservations, and displays them in table
    ''' </summary>
    Private Sub LoadTodayOrders()
        Try
            ' Calculate offset
            Dim offset As Integer = (currentPage - 1) * pageSize
            
            ' Get totals
            totalRecords = reportRepository.GetTotalTodayActivityCount()
            totalPages = Math.Max(1, CInt(Math.Ceiling(totalRecords / pageSize)))
            
            ' Get combined paged data
            Dim items As List(Of ReportActivityItem) = reportRepository.GetTodayActivityPaged(pageSize, offset)
            
            DisplayTodayActivity(items)
            UpdatePaginationControls()
            
        Catch ex As Exception
            Console.WriteLine($"Error loading today's activity: {ex.Message}")
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
                LoadTodayOrders()
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
            LoadTodayOrders()
        End If
    End Sub

    Private Sub btnNextPage_Click(sender As Object, e As EventArgs)
        If currentPage < totalPages Then
            currentPage += 1
            LoadTodayOrders()
        End If
    End Sub

    ''' <summary>
    ''' Displays today's orders and reservations in the table layout
    ''' </summary>
    ''' <summary>
    ''' Displays today's activity items in the table layout
    ''' </summary>
    Private Sub DisplayTodayActivity(items As List(Of ReportActivityItem))
        ' Clear all containers
        tlpOrdersRows.Controls.Clear()
        tlpOrdersRows.RowStyles.Clear()
        tlpOrdersRows.RowCount = 0
        pnlTableHeader.Controls.Clear()
        pnlTableTotal.Controls.Clear()
        
        If items.Count = 0 Then
            tlpOrdersRows.RowCount = 1
            tlpOrdersRows.RowStyles.Add(New RowStyle(SizeType.Absolute, 50.0F))

            Dim lblEmpty As New Label With {
                .Text = "No orders or reservations today",
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Font = New Font("Segoe UI", 10, FontStyle.Italic),
                .ForeColor = Color.Gray
            }

            tlpOrdersRows.Controls.Add(lblEmpty, 0, 0)
            Return
        End If

        ' Add header row to fixed panel
        PopulateHeaderPanel()

        ' Add activity rows
        For Each item In items
            AddActivityRow(item)
        Next
        
        ' Populate total panel (Requires re-calculating full totals independently of page)
        ' We can just reuse LoadTodaySales's logic or recalc sum here?
        ' Actually the requirement for "Total" row usually implies total of displayed items OR total for day.
        ' Reports usually show "Total for Day". The bottom panel shows "Total". 
        ' Since we have pagination now, showing the *Page Total* might be confusing if it says "Total".
        ' However, the prompt asked for pagination. I'll make it show specific total if needed, 
        ' but sticking to the LoadTodaySales logic for the top card is mostly enough.
        ' The bottom total panel previously summed the *list*. 
        ' I will remove the bottom total panel logic from *here* and let the top card handle the "Total Sales".
        ' OR I can display "Page Total". Let's omit the footer total for now as it's redundant with the top card "Today's Sales".
        ' Wait, visual consistency. I'll show Page Total.
        
        Dim pageTotal As Decimal = items.Sum(Function(i) i.Amount)
        PopulateTotalPanel(pageTotal)
    End Sub

    ''' <summary>
    ''' Populates the fixed header panel with column headers
    ''' </summary>
    Private Sub PopulateHeaderPanel()
        pnlTableHeader.Controls.Clear()

        ' Use resize handler for responsive column positions
        AddHandler pnlTableHeader.Resize, Sub(sender, e)
                                              Dim panel As Panel = CType(sender, Panel)
                                              Dim width As Integer = panel.Width - 40 ' Account for padding

                                              ' Update label positions based on panel width
                                              If panel.Controls.Count >= 5 Then
                                                  panel.Controls(0).Left = 20  ' Order ID - 0%
                                                  panel.Controls(1).Left = CInt(width * 0.15) + 20  ' Type - 15%
                                                  panel.Controls(2).Left = CInt(width * 0.35) + 20  ' Items - 35%
                                                  panel.Controls(3).Left = CInt(width * 0.55) + 20  ' Time - 55%
                                                  panel.Controls(4).Left = CInt(width * 0.75) + 20  ' Amount - 75%
                                              End If
                                          End Sub

        ' Column headers
        Dim lblOrderID As New Label With {
            .Text = "Order ID",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(20, 10)
        }

        Dim lblType As New Label With {
            .Text = "Type",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(20, 10)
        }

        Dim lblItems As New Label With {
            .Text = "Items",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(20, 10)
        }

        Dim lblTime As New Label With {
            .Text = "Time",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(20, 10)
        }

        Dim lblAmount As New Label With {
            .Text = "Amount",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(20, 10)
        }

        pnlTableHeader.Controls.AddRange({lblOrderID, lblType, lblItems, lblTime, lblAmount})
    End Sub

    ''' <summary>
    ''' Adds order data row
    ''' </summary>
    ''' <summary>
    ''' Adds activity data row
    ''' </summary>
    Private Sub AddActivityRow(item As ReportActivityItem)
        tlpOrdersRows.RowCount += 1
        tlpOrdersRows.RowStyles.Add(New RowStyle(SizeType.Absolute, 45.0F))

        Dim rowPanel As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .Padding = New Padding(20, 10, 20, 10)
        }

        ' ID
        Dim idPrefix As String = If(item.Type = "Reservation", "#RES-", "#")
        Dim lblID As New Label With {
            .Text = $"{idPrefix}{item.ID}",
            .Font = New Font("Segoe UI", 9),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Location = New Point(20, 12)
        }

        ' Type
        Dim lblType As New Label With {
            .Text = item.Type,
            .Font = New Font("Segoe UI", 9),
            .AutoSize = True,
            .Location = New Point(20, 12)
        }

        ' Description (Item count or Guests)
        Dim lblDesc As New Label With {
            .Text = item.Description,
            .Font = New Font("Segoe UI", 9),
            .AutoSize = True,
            .Location = New Point(20, 12)
        }

        ' Time
        Dim lblTime As New Label With {
            .Text = item.Time.ToString("h:mm tt"),
            .Font = New Font("Segoe UI", 9),
            .AutoSize = True,
            .Location = New Point(20, 12)
        }

        ' Amount
        Dim lblAmount As New Label With {
            .Text = $"₱ {item.Amount:N0}",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(20, 12)
        }

        ' Use resize handler for responsive column positions
        AddHandler rowPanel.Resize, Sub(sender, e)
                                        Dim panel As Panel = CType(sender, Panel)
                                        Dim width As Integer = panel.Width - 40

                                        If panel.Controls.Count >= 5 Then
                                            panel.Controls(0).Left = 20
                                            panel.Controls(1).Left = CInt(width * 0.15) + 20
                                            panel.Controls(2).Left = CInt(width * 0.35) + 20
                                            panel.Controls(3).Left = CInt(width * 0.55) + 20
                                            panel.Controls(4).Left = CInt(width * 0.75) + 20
                                        End If
                                    End Sub

        rowPanel.Controls.AddRange({lblID, lblType, lblDesc, lblTime, lblAmount})
        tlpOrdersRows.Controls.Add(rowPanel, 0, tlpOrdersRows.RowCount - 1)
    End Sub

    ''' <summary>
    ''' Populates the fixed total panel
    ''' </summary>
    ''' <summary>
    ''' Populates the fixed total panel
    ''' </summary>
    Private Sub PopulateTotalPanel(amount As Decimal)
        pnlTableTotal.Controls.Clear()

        Dim lblTotalLabel As New Label With {
            .Text = "Page Total",
            .Font = New Font("Segoe UI", 13, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(20, 15)
        }

        Dim lblTotalAmount As New Label With {
            .Text = $"₱ {amount:N0}",
            .Font = New Font("Segoe UI", 13, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(20, 13)
        }

        ' Use resize handler for responsive positioning
        AddHandler pnlTableTotal.Resize, Sub(sender, e)
                                             Dim panel As Panel = CType(sender, Panel)
                                             Dim width As Integer = panel.Width - 40

                                             If panel.Controls.Count >= 2 Then
                                                 panel.Controls(0).Left = 20
                                                 panel.Controls(1).Left = CInt(width * 0.75) + 20  ' Align with Amount column
                                             End If
                                         End Sub

        pnlTableTotal.Controls.AddRange({lblTotalLabel, lblTotalAmount})
    End Sub

    ''' <summary>
    ''' Adds reservation data row
    ''' </summary>
    ' Legacy AddReservationRow removed

End Class
