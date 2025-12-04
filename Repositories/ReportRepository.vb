Imports MySql.Data.MySqlClient
Imports System.Collections.Generic

Public Class ReportActivityItem
    Public Property ID As String
    Public Property Type As String
    Public Property Description As String
    Public Property Time As DateTime
    Public Property Amount As Decimal
    Public Property Status As String
End Class

Public Class ReportRepository
    ''' <summary>
    ''' Gets today's orders and reservations combined, paginated, and sorted by time descending
    ''' </summary>
    Public Function GetTodayActivityPaged(limit As Integer, offset As Integer) As List(Of ReportActivityItem)
        Dim items As New List(Of ReportActivityItem)
        
        Dim query As String = "SELECT 'Order' as Type, OrderID as ID, ItemsOrderedCount as Description, OrderTime as Time, TotalAmount as Amount, OrderStatus as Status " &
                              "FROM orders WHERE DATE(OrderDate) = CURDATE() AND OrderStatus != 'Cancelled' " &
                              "UNION ALL " &
                              "SELECT 'Reservation' as Type, ReservationID as ID, CONCAT(NumberOfGuests, ' Guests') as Description, EventTime as Time, " &
                              "COALESCE((SELECT SUM(ri.Quantity * ri.UnitPrice) FROM reservation_items ri WHERE ri.ReservationID = reservations.ReservationID), 0) as Amount, " &
                              "ReservationStatus as Status " &
                              "FROM reservations WHERE DATE(EventDate) = CURDATE() AND ReservationStatus IN ('Accepted', 'Confirmed') " &
                              "ORDER BY Time DESC LIMIT @limit OFFSET @offset"
                              
        Dim parameters As MySqlParameter() = {
            New MySqlParameter("@limit", limit),
            New MySqlParameter("@offset", offset)
        }
        
        Dim table As DataTable = modDB.ExecuteQuery(query, parameters)
        
        If table IsNot Nothing Then
            For Each row As DataRow In table.Rows
                items.Add(New ReportActivityItem With {
                    .Type = row("Type").ToString(),
                    .ID = row("ID").ToString(),
                    .Description = row("Description").ToString(),
                    .Time = DateTime.Today.Add(CType(row("Time"), TimeSpan)),
                    .Amount = Convert.ToDecimal(row("Amount")),
                    .Status = row("Status").ToString()
                })
            Next
        End If
        
        Return items
    End Function

    ''' <summary>
    ''' Gets total count of today's items (orders + reservations)
    ''' </summary>
    Public Function GetTotalTodayActivityCount() As Integer
        Dim query As String = "SELECT " &
                              "(SELECT COUNT(*) FROM orders WHERE DATE(OrderDate) = CURDATE() AND OrderStatus != 'Cancelled') + " &
                              "(SELECT COUNT(*) FROM reservations WHERE DATE(EventDate) = CURDATE() AND ReservationStatus IN ('Accepted', 'Confirmed'))"
        
        Dim result As Object = modDB.ExecuteScalar(query)
        If result IsNot Nothing AndAlso IsNumeric(result) Then
            Return CInt(result)
        End If
        Return 0
    End Function
End Class
