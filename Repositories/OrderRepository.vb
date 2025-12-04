Imports MySql.Data.MySqlClient
Imports System.Collections.Generic

Public Class OrderRepository
    Public Function CreateOrder(order As Order, items As List(Of OrderItem)) As Integer
        Dim query As String = "INSERT INTO orders (CustomerID, EmployeeID, OrderType, OrderSource, ReceiptNumber, NumberOfDiners, OrderDate, OrderTime, ItemsOrderedCount, TotalAmount, OrderStatus, Remarks) VALUES (@customerID, @employeeID, @orderType, @orderSource, @receiptNumber, @numberOfDiners, @orderDate, @orderTime, @itemsCount, @totalAmount, @orderStatus, @remarks)"
        
        Dim parameters As MySqlParameter() = {
            New MySqlParameter("@customerID", If(order.CustomerID.HasValue, order.CustomerID.Value, DBNull.Value)),
            New MySqlParameter("@employeeID", If(order.EmployeeID.HasValue, order.EmployeeID.Value, DBNull.Value)),
            New MySqlParameter("@orderType", order.OrderType),
            New MySqlParameter("@orderSource", order.OrderSource),
            New MySqlParameter("@receiptNumber", If(String.IsNullOrEmpty(order.ReceiptNumber), DBNull.Value, order.ReceiptNumber)),
            New MySqlParameter("@numberOfDiners", If(order.NumberOfDiners.HasValue, order.NumberOfDiners.Value, DBNull.Value)),
            New MySqlParameter("@orderDate", order.OrderDate),
            New MySqlParameter("@orderTime", order.OrderTime),
            New MySqlParameter("@itemsCount", order.ItemsOrderedCount),
            New MySqlParameter("@totalAmount", order.TotalAmount),
            New MySqlParameter("@orderStatus", order.OrderStatus),
            New MySqlParameter("@remarks", If(String.IsNullOrEmpty(order.Remarks), DBNull.Value, order.Remarks))
        }

        ' Use raw connection to handle potential errors gracefully
        Dim orderID As Integer = 0
        
        Try
            Using conn As New MySqlConnection(modDB.ConnectionString)
                conn.Open()
                
                ' 1. Insert Order
                Using cmd As New MySqlCommand(query, conn)
                    cmd.Parameters.AddRange(parameters)
                    cmd.ExecuteNonQuery()
                End Using
                
                ' 2. Get Last ID
                Using cmdID As New MySqlCommand("SELECT LAST_INSERT_ID()", conn)
                    Dim result = cmdID.ExecuteScalar()
                    If result IsNot Nothing AndAlso IsNumeric(result) Then
                        orderID = CInt(result)
                    End If
                End Using
                
                If orderID > 0 Then
                    ' 3. Insert Items
                    For Each item In items
                        Using cmdItem As New MySqlCommand("INSERT INTO order_items (OrderID, ProductName, Quantity, UnitPrice, ItemStatus) VALUES (@orderID, @productName, @quantity, @unitPrice, @itemStatus)", conn)
                            cmdItem.Parameters.AddWithValue("@orderID", orderID)
                            cmdItem.Parameters.AddWithValue("@productName", item.ProductName)
                            cmdItem.Parameters.AddWithValue("@quantity", item.Quantity)
                            cmdItem.Parameters.AddWithValue("@unitPrice", item.UnitPrice)
                            cmdItem.Parameters.AddWithValue("@itemStatus", "Pending")
                            cmdItem.ExecuteNonQuery()
                        End Using
                    Next
                End If
            End Using
            
            ' NOTE: Inventory deduction is handled by the database trigger `tr_order_completed`
            ' which fires when OrderStatus is changed to 'Completed'. 
            ' Do NOT manually call DeductInventoryForOrder here to avoid double-deduction.
            ' The trigger calls: DeductIngredientsForPOSOrder(OrderID)
            
            If orderID > 0 Then
                Return orderID
            End If
            
        Catch ex As Exception
            ' Debugging: Show the specific error to the user
            MessageBox.Show($"Order creation failed details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            System.Diagnostics.Debug.WriteLine($"Order creation error: {ex.Message}")
        End Try
        
        Return 0
    End Function

    Private Sub AddOrderItem(orderID As Integer, item As OrderItem)
        Dim query As String = "INSERT INTO order_items (OrderID, ProductName, Quantity, UnitPrice, ItemStatus) VALUES (@orderID, @productName, @quantity, @unitPrice, @itemStatus)"
        Dim parameters As MySqlParameter() = {
            New MySqlParameter("@orderID", orderID),
            New MySqlParameter("@productName", item.ProductName),
            New MySqlParameter("@quantity", item.Quantity),
            New MySqlParameter("@unitPrice", item.UnitPrice),
            New MySqlParameter("@itemStatus", "Pending")
        }
        modDB.ExecuteNonQuery(query, parameters)
    End Sub

    Public Function GetActiveOrders() As List(Of Order)
        Return GetActiveOrdersPaged(10, 0) ' Default to top 10 for backward compatibility if needed, or update usage
    End Function

    Public Function GetActiveOrdersPaged(limit As Integer, offset As Integer) As List(Of Order)
        Dim orders As New List(Of Order)
        Dim query As String = "SELECT OrderID, CustomerID, EmployeeID, OrderType, OrderSource, OrderDate, OrderTime, TotalAmount, OrderStatus FROM orders WHERE OrderStatus IN ('Preparing', 'Serving', 'Served') ORDER BY OrderDate DESC, OrderTime DESC LIMIT " & limit & " OFFSET " & offset
        
        Dim table As DataTable = modDB.ExecuteQuery(query)
        If table IsNot Nothing Then
            For Each row As DataRow In table.Rows
                orders.Add(New Order With {
                    .OrderID = Convert.ToInt32(row("OrderID")),
                    .CustomerID = If(IsDBNull(row("CustomerID")), Nothing, CInt(row("CustomerID"))),
                    .EmployeeID = If(IsDBNull(row("EmployeeID")), Nothing, CInt(row("EmployeeID"))),
                    .OrderType = row("OrderType").ToString(),
                    .OrderSource = row("OrderSource").ToString(),
                    .TotalAmount = Convert.ToDecimal(row("TotalAmount")),
                    .OrderDate = Convert.ToDateTime(row("OrderDate")),
                    .OrderTime = CType(row("OrderTime"), TimeSpan),
                    .OrderStatus = row("OrderStatus").ToString()
                })
            Next
        End If
        
        Return orders
    End Function
    
    Public Function GetTodayOrders() As List(Of Order)
        Dim orders As New List(Of Order)
        Dim query As String = "SELECT OrderID, CustomerID, EmployeeID, OrderType, OrderSource, OrderDate, OrderTime, TotalAmount, OrderStatus FROM orders WHERE DATE(OrderDate) = CURDATE() AND OrderStatus != 'Cancelled' ORDER BY OrderTime DESC"
        
        Dim table As DataTable = modDB.ExecuteQuery(query)
        If table IsNot Nothing Then
            For Each row As DataRow In table.Rows
                Dim order As New Order With {
                    .OrderID = Convert.ToInt32(row("OrderID")),
                    .CustomerID = If(IsDBNull(row("CustomerID")), Nothing, CInt(row("CustomerID"))),
                    .EmployeeID = If(IsDBNull(row("EmployeeID")), Nothing, CInt(row("EmployeeID"))),
                    .OrderType = row("OrderType").ToString(),
                    .OrderSource = row("OrderSource").ToString(),
                    .TotalAmount = Convert.ToDecimal(row("TotalAmount")),
                    .OrderDate = Convert.ToDateTime(row("OrderDate")),
                    .OrderTime = CType(row("OrderTime"), TimeSpan),
                    .OrderStatus = row("OrderStatus").ToString()
                }
                
                ' Load items for this order
                order.Items = GetOrderItems(order.OrderID)
                
                orders.Add(order)
            Next
        End If
        
        Return orders
    End Function
    
    Public Function GetOrderItems(orderID As Integer) As List(Of OrderItem)
        Dim items As New List(Of OrderItem)
        Dim query As String = "SELECT ProductName, Quantity, UnitPrice FROM order_items WHERE OrderID = @orderID"
        Dim parameters As MySqlParameter() = {
            New MySqlParameter("@orderID", orderID)
        }
        
        Dim table As DataTable = modDB.ExecuteQuery(query, parameters)
        If table IsNot Nothing Then
            For Each row As DataRow In table.Rows
                items.Add(New OrderItem With {
                    .ProductName = row("ProductName").ToString(),
                    .Quantity = Convert.ToInt32(row("Quantity")),
                    .UnitPrice = Convert.ToDecimal(row("UnitPrice"))
                })
            Next
        End If
        
        Return items
    End Function
    
    Public Function GetTodayOrdersCount() As Integer
        Dim query As String = "SELECT COUNT(*) FROM orders WHERE DATE(OrderDate) = CURDATE() AND OrderStatus != 'Cancelled'"
        Dim result As Object = modDB.ExecuteScalar(query)
        If result IsNot Nothing AndAlso IsNumeric(result) Then
            Return CInt(result)
        End If
        Return 0
    End Function

    Public Sub UpdateOrderStatus(orderID As Integer, newStatus As String)
        Dim query As String = "UPDATE orders SET OrderStatus = @status WHERE OrderID = @orderID"
        Dim parameters As MySqlParameter() = {
            New MySqlParameter("@status", newStatus),
            New MySqlParameter("@orderID", orderID)
        }
        modDB.ExecuteNonQuery(query, parameters)
    End Sub

    ''' <summary>
    ''' Gets all online orders with customer information
    ''' </summary>
    Public Function GetOnlineOrders() As List(Of OnlineOrder)
        Dim onlineOrders As New List(Of OnlineOrder)
        
        Dim query As String = "SELECT o.OrderID, o.CustomerID, o.OrderType, o.OrderSource, o.OrderDate, o.OrderTime, " &
                             "o.ItemsOrderedCount, o.TotalAmount, o.OrderStatus, o.Remarks, o.DeliveryAddress, " &
                             "o.SpecialRequests, o.CreatedDate, o.UpdatedDate, " &
                             "c.FirstName, c.LastName, c.Email, c.ContactNumber " &
                             "FROM orders o " &
                             "LEFT JOIN customers c ON o.CustomerID = c.CustomerID " &
                             "WHERE o.OrderType = 'Online' " &
                             "ORDER BY o.OrderDate DESC, o.OrderTime DESC"
        
        Dim table As DataTable = modDB.ExecuteQuery(query)
        
        Return GetOnlineOrdersPaged(50, 0)
    End Function

    ''' <summary>
    ''' Gets online orders with pagination and filtering
    ''' </summary>
    Public Function GetOnlineOrdersPaged(limit As Integer, offset As Integer, Optional statusFilter As String = "All Orders") As List(Of OnlineOrder)
        Dim onlineOrders As New List(Of OnlineOrder)
        
        Dim query As String = "SELECT o.OrderID, o.CustomerID, o.OrderType, o.OrderSource, o.OrderDate, o.OrderTime, " &
                              "o.ItemsOrderedCount, o.TotalAmount, o.OrderStatus, o.Remarks, o.DeliveryAddress, " &
                              "o.SpecialRequests, o.CreatedDate, o.UpdatedDate, " &
                              "c.FirstName, c.LastName, c.Email, c.ContactNumber " &
                              "FROM orders o " &
                              "LEFT JOIN customers c ON o.CustomerID = c.CustomerID " &
                              "WHERE o.OrderType = 'Online' "
        
        If statusFilter <> "All Orders" Then
            query &= "AND o.OrderStatus = '" & statusFilter & "' "
        End If
        
        query &= "ORDER BY o.OrderDate DESC, o.OrderTime DESC LIMIT " & limit & " OFFSET " & offset
        
        Dim table As DataTable = modDB.ExecuteQuery(query)
        
        If table IsNot Nothing Then
            For Each row As DataRow In table.Rows
                Dim onlineOrder As New OnlineOrder With {
                    .OrderID = Convert.ToInt32(row("OrderID")),
                    .CustomerID = If(IsDBNull(row("CustomerID")), 0, Convert.ToInt32(row("CustomerID"))),
                    .CustomerName = If(IsDBNull(row("FirstName")), "Unknown", row("FirstName").ToString() & " " & row("LastName").ToString()),
                    .Email = If(IsDBNull(row("Email")), "", row("Email").ToString()),
                    .ContactNumber = If(IsDBNull(row("ContactNumber")), "", row("ContactNumber").ToString()),
                    .OrderType = row("OrderType").ToString(),
                    .OrderSource = row("OrderSource").ToString(),
                    .OrderDate = Convert.ToDateTime(row("OrderDate")),
                    .OrderTime = CType(row("OrderTime"), TimeSpan),
                    .ItemsOrderedCount = Convert.ToInt32(row("ItemsOrderedCount")),
                    .TotalAmount = Convert.ToDecimal(row("TotalAmount")),
                    .OrderStatus = row("OrderStatus").ToString(),
                    .Remarks = If(IsDBNull(row("Remarks")), "", row("Remarks").ToString()),
                    .DeliveryAddress = If(IsDBNull(row("DeliveryAddress")), "", row("DeliveryAddress").ToString()),
                    .SpecialRequests = If(IsDBNull(row("SpecialRequests")), "", row("SpecialRequests").ToString()),
                    .CreatedDate = Convert.ToDateTime(row("CreatedDate")),
                    .UpdatedDate = Convert.ToDateTime(row("UpdatedDate"))
                }
                
                onlineOrders.Add(onlineOrder)
            Next
        End If
        
        Return onlineOrders
    End Function

    ''' <summary>
    ''' Gets total count of online orders for pagination
    ''' </summary>
    Public Function GetTotalOnlineOrdersCount(Optional statusFilter As String = "All Orders") As Integer
        Dim query As String = "SELECT COUNT(*) FROM orders WHERE OrderType = 'Online'"
        
        If statusFilter <> "All Orders" Then
            query &= " AND OrderStatus = '" & statusFilter & "'"
        End If
        
        Dim result As Object = modDB.ExecuteScalar(query)
        If result IsNot Nothing AndAlso IsNumeric(result) Then
            Return CInt(result)
        End If
        Return 0
    End Function
    Public Function GetTotalActiveOrdersCount() As Integer
        Dim query As String = "SELECT COUNT(*) FROM orders WHERE OrderStatus IN ('Preparing', 'Serving', 'Served')"
        Dim result As Object = modDB.ExecuteScalar(query)
        If result IsNot Nothing AndAlso IsNumeric(result) Then
            Return CInt(result)
        End If
        Return 0
    End Function

    ''' <summary>
    ''' Async version of GetActiveOrdersPaged
    ''' </summary>
    Public Async Function GetActiveOrdersPagedAsync(limit As Integer, offset As Integer) As Task(Of List(Of Order))
        Return Await Task.Run(Function() GetActiveOrdersPaged(limit, offset))
    End Function

    ''' <summary>
    ''' Async version of GetTotalActiveOrdersCount
    ''' </summary>
    Public Async Function GetTotalActiveOrdersCountAsync() As Task(Of Integer)
        Return Await Task.Run(Function() GetTotalActiveOrdersCount())
    End Function
End Class
