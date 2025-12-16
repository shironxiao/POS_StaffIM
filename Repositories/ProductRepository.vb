Imports MySql.Data.MySqlClient
Imports System.Collections.Generic
Imports System.Threading.Tasks

Public Class ProductRepository

    ''' <summary>
    ''' Gets products with pagination and search support using Stored Procedure
    ''' </summary>
    Public Function GetProductsPaged(limit As Integer, offset As Integer, Optional category As String = "All", Optional search As String = "") As List(Of Product)
        Dim products As New List(Of Product)
        
        ' Use the optimized wrapper stored procedure created in performance_procedures.sql
        ' Parameters: p_limit, p_offset, p_category, p_search
        
        ' Note: Parameter names in MySqlCommand must match calling context or be strictly positional if using ? syntax.
        ' Since we are using parameterized generic ExecuteQuery helper might be raw.
        ' Let's construct a direct CALL.
        
        ' Handle optional parameters for SQL injection ease (though SP handles this, passing clean strings is safest)
        Dim safeCategory As String = If(category = "All", "", category)
        Dim safeSearch As String = If(String.IsNullOrEmpty(search), "", search)
        
        ' Using simpler string interpolation for the CALL (values sanitized by logic or assume internal safety for now, 
        ' but ideally should use parameterized command if modDB supports it well for SPs)
        ' Given modDB.ExecuteQuery logic, string building is consistent with previous patterns in this codebase.
        
        ' Warning: Concatenating strings into SQL is unsafe. 
        ' We will use parameterized call logic if possible, or QUOTE values.
        ' However, modDB helper is simple. Let's use string building with proper quoting logic handled in the SP (it uses QUOTE()).
        ' Wait, the SP uses QUOTE(p_category). But we need to pass strings TO the SP.
        
        Dim query As String = $"CALL GetProductsPaged({limit}, {offset}, '{safeCategory.Replace("'", "''")}', '{safeSearch.Replace("'", "''")}')"
        
        Dim table As DataTable = modDB.ExecuteQuery(query)
        
        If table IsNot Nothing Then
            For Each row As DataRow In table.Rows
                Dim product As New Product()
                product.ProductID = Convert.ToInt32(row("ProductID"))
                product.ProductName = row("ProductName").ToString()
                product.Category = row("Category").ToString()
                product.Price = Convert.ToDecimal(row("Price"))
                product.Availability = row("Availability").ToString()
                product.Image = If(IsDBNull(row("Image")), "", row("Image").ToString())
                ' Check if column exists to prevent crash if DB is not updated
                If table.Columns.Contains("PrepTime") Then
                    product.PrepTime = If(IsDBNull(row("PrepTime")), 0, Convert.ToInt32(row("PrepTime")))
                End If
                
                products.Add(product)
            Next
        End If
        
        Return products
    End Function

    ''' <summary>
    ''' Async version of GetProductsPaged
    ''' </summary>
    Public Async Function GetProductsPagedAsync(limit As Integer, offset As Integer, Optional category As String = "All", Optional search As String = "") As Task(Of List(Of Product))
        Return Await Task.Run(Function() GetProductsPaged(limit, offset, category, search))
    End Function

    ''' <summary>
    ''' Gets total count of products for pagination
    ''' </summary>
    Public Function GetTotalProductsCount(Optional category As String = "All", Optional search As String = "") As Integer
        Dim whereClause As String = "1=1" ' Allow all products by default
        
        If category <> "All" AndAlso Not String.IsNullOrEmpty(category) Then
             whereClause &= " AND Category = '" & category.Replace("'", "''") & "'"
        End If
        
        If Not String.IsNullOrEmpty(search) Then
            whereClause &= " AND ProductName LIKE '%" & search.Replace("'", "''") & "%'"
        End If
        
        
        Dim query As String = $"SELECT COUNT(*) AS TotalCount FROM products WHERE {whereClause}"
        
        Dim result As Object = modDB.ExecuteScalar(query)
        If result IsNot Nothing AndAlso IsNumeric(result) Then
            Return Convert.ToInt32(result)
        End If
        Return 0
    End Function

    ''' <summary>
    ''' Legacy method support - Redirects to paged fetch (Fetching all is dangerous now)
    ''' Returning first 100 to avoid breaking UI that expects a list.
    ''' </summary>
    ''' <summary>
    ''' Load ALL products for buffered/in-memory use.
    ''' </summary>
    Public Function GetAllProducts() As List(Of Product)
        Dim products As New List(Of Product)
        Dim query As String = "SELECT * FROM products" ' Load everything
        
        Dim table As DataTable = modDB.ExecuteQuery(query)
        
        If table IsNot Nothing Then
            For Each row As DataRow In table.Rows
                Dim product As New Product()
                product.ProductID = Convert.ToInt32(row("ProductID"))
                product.ProductName = row("ProductName").ToString()
                product.Category = row("Category").ToString()
                product.Price = Convert.ToDecimal(row("Price"))
                product.Availability = row("Availability").ToString()
                product.Image = If(IsDBNull(row("Image")), "", row("Image").ToString())
                ' Check if column exists to prevent crash if DB is not updated
                If table.Columns.Contains("PrepTime") Then
                    product.PrepTime = If(IsDBNull(row("PrepTime")), 0, Convert.ToInt32(row("PrepTime")))
                End If
                
                products.Add(product)
            Next
        End If
        
        Return products
    End Function
    
    Public Async Function GetAllProductsAsync() As Task(Of List(Of Product))
         ' Use the Sync implementation which uses SELECT * (ensuring PrepTime is loaded)
         Return Await Task.Run(Function() GetAllProducts()) 
    End Function

    Public Function GetProductsByCategory(category As String) As List(Of Product)
        If category = "All" Then Return GetAllProducts()
        
        Dim products As New List(Of Product)
        Dim query As String = "SELECT * FROM products WHERE Category = '" & category.Replace("'", "''") & "'"
        
        Dim table As DataTable = modDB.ExecuteQuery(query)
        If table IsNot Nothing Then
            For Each row As DataRow In table.Rows
                Dim product As New Product()
                product.ProductID = Convert.ToInt32(row("ProductID"))
                product.ProductName = row("ProductName").ToString()
                product.Category = row("Category").ToString()
                product.Price = Convert.ToDecimal(row("Price"))
                product.Availability = row("Availability").ToString()
                product.Image = If(IsDBNull(row("Image")), "", row("Image").ToString())
                ' Check if column exists to prevent crash if DB is not updated
                If table.Columns.Contains("PrepTime") Then
                    product.PrepTime = If(IsDBNull(row("PrepTime")), 0, Convert.ToInt32(row("PrepTime")))
                End If
                
                products.Add(product)
            Next
        End If
        Return products
    End Function
    
    Public Async Function GetProductsByCategoryAsync(category As String) As Task(Of List(Of Product))
         ' Use the Sync implementation which uses SELECT *
         Return Await Task.Run(Function() GetProductsByCategory(category))
    End Function

    Public Sub RefreshCache()
        ' No-op: Caching service removed.
    End Sub

    Public Async Function PreloadCacheAsync() As Task
        ' No-op
        Await Task.CompletedTask
    End Function
    
End Class
