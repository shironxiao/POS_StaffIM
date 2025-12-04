Imports MySql.Data.MySqlClient
Imports System.Collections.Generic
Imports System.Threading.Tasks

Public Class ProductRepository
    ' Cache keys
    Private Const CACHE_KEY_PRODUCTS As String = "AllProducts"


    ''' <summary>
    ''' Gets all products with caching support
    ''' </summary>
    ''' <summary>
    ''' Gets all products with caching support
    ''' </summary>
    Public Function GetAllProducts() As List(Of Product)
        Dim products As List(Of Product) = DataCacheService.GetItem(Of List(Of Product))(CACHE_KEY_PRODUCTS)
        
        If products Is Nothing Then
            ' Cache is invalid or empty, fetch from database
            products = GetProducts("SELECT ProductID, ProductName, Category, Description, Price, Availability, ServingSize, ProductCode, PopularityTag, MealTime, OrderCount, Image, PrepTime FROM products WHERE Availability = 'Available' ORDER BY ProductName")
            
            ' Cache for 5 minutes
            DataCacheService.SetItem(CACHE_KEY_PRODUCTS, products, 5)
        End If

        ' Return a copy to prevent external modification of cached list
        Return New List(Of Product)(products)
    End Function

    ''' <summary>
    ''' Async version of GetAllProducts for non-blocking UI
    ''' </summary>
    Public Async Function GetAllProductsAsync() As Task(Of List(Of Product))
        Return Await Task.Run(Function() GetAllProducts())
    End Function

    ''' <summary>
    ''' Gets products by category from cache (faster)
    ''' </summary>
    Public Function GetProductsByCategory(category As String) As List(Of Product)
        ' Get from cache if available
        Dim allProducts = GetAllProducts()
        
        If category = "All" Then
            Return allProducts
        End If

        ' Filter in memory (much faster than DB query)
        Return allProducts.Where(Function(p) p.Category = category).ToList()
    End Function

    ''' <summary>
    ''' Async version of GetProductsByCategory
    ''' </summary>
    Public Async Function GetProductsByCategoryAsync(category As String) As Task(Of List(Of Product))
        Return Await Task.Run(Function() GetProductsByCategory(category))
    End Function

    ''' <summary>
    ''' Forces cache refresh (call when products are updated)
    ''' </summary>
    ''' <summary>
    ''' Forces cache refresh (call when products are updated)
    ''' </summary>
    Public Sub RefreshCache()
        DataCacheService.Invalidate(CACHE_KEY_PRODUCTS)
    End Sub

    ''' <summary>
    ''' Preloads product cache in background
    ''' </summary>
    Public Async Function PreloadCacheAsync() As Task
        Await GetAllProductsAsync()
    End Function

    Private Function GetProducts(query As String, Optional parameters As MySqlParameter() = Nothing) As List(Of Product)
        Dim products As New List(Of Product)
        Dim table As DataTable = modDB.ExecuteQuery(query, parameters)

        If table IsNot Nothing Then
            For Each row As DataRow In table.Rows
                Dim product As New Product With {
                    .ProductID = Convert.ToInt32(row("ProductID")),
                    .ProductName = row("ProductName").ToString(),
                    .Category = row("Category").ToString(),
                    .Description = If(IsDBNull(row("Description")), "", row("Description").ToString()),
                    .Price = Convert.ToDecimal(row("Price")),
                    .Availability = row("Availability").ToString(),
                    .ServingSize = If(IsDBNull(row("ServingSize")), "", row("ServingSize").ToString()),
                    .ProductCode = If(IsDBNull(row("ProductCode")), "", row("ProductCode").ToString()),
                    .PopularityTag = If(IsDBNull(row("PopularityTag")), "Regular", row("PopularityTag").ToString()),
                    .MealTime = If(IsDBNull(row("MealTime")), "", row("MealTime").ToString()),
                    .OrderCount = If(IsDBNull(row("OrderCount")), 0, Convert.ToInt32(row("OrderCount"))),
                    .Image = If(IsDBNull(row("Image")), "", row("Image").ToString()),
                    .PrepTime = If(IsDBNull(row("PrepTime")), 0, Convert.ToInt32(row("PrepTime")))
                }

                products.Add(product)
            Next
        End If

        ' Check inventory availability for all fetched products
        Dim inventoryService As New InventoryService()
        inventoryService.CheckInventoryForProducts(products)

        Return products
    End Function
End Class
