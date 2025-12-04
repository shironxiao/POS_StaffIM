Imports System.Collections.Generic

Public Class DataCacheService
    ' Dictionary to store cache items: Key -> (Value, ExpirationTime)
    Private Shared _cache As New Dictionary(Of String, Tuple(Of Object, DateTime))
    Private Shared ReadOnly _lock As New Object()

    ''' <summary>
    ''' Retrieves an item from the cache. Returns Nothing if not found or expired.
    ''' </summary>
    Public Shared Function GetItem(Of T)(key As String) As T
        SyncLock _lock
            If _cache.ContainsKey(key) Then
                Dim item = _cache(key)
                ' Check if expired
                If DateTime.Now < item.Item2 Then
                    Return CType(item.Item1, T)
                Else
                    ' Remove expired item
                    _cache.Remove(key)
                End If
            End If
        End SyncLock
        Return Nothing
    End Function

    ''' <summary>
    ''' Sets an item in the cache with a specified duration in minutes.
    ''' </summary>
    Public Shared Sub SetItem(key As String, value As Object, durationMinutes As Integer)
        SyncLock _lock
            Dim expiration As DateTime = DateTime.Now.AddMinutes(durationMinutes)
            If _cache.ContainsKey(key) Then
                _cache(key) = Tuple.Create(value, expiration)
            Else
                _cache.Add(key, Tuple.Create(value, expiration))
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Removes an item from the cache.
    ''' </summary>
    Public Shared Sub Invalidate(key As String)
        SyncLock _lock
            If _cache.ContainsKey(key) Then
                _cache.Remove(key)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Clears specific cache keys starting with a prefix (e.g., "Product_" to clear all product related caches)
    ''' </summary>
    Public Shared Sub InvalidatePrefix(prefix As String)
        SyncLock _lock
            Dim keysToRemove = _cache.Keys.Where(Function(k) k.StartsWith(prefix)).ToList()
            For Each k In keysToRemove
                _cache.Remove(k)
            Next
        End SyncLock
    End Sub

    ''' <summary>
    ''' Clears the entire cache.
    ''' </summary>
    Public Shared Sub ClearAll()
        SyncLock _lock
            _cache.Clear()
        End SyncLock
    End Sub
End Class
