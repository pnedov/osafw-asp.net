' Fw Cache class
' Application-level cache
'
' Part of ASP.NET osa framework  www.osalabs.com/osafw/asp.net
' (c) 2009-2015 Oleg Savchuk www.osalabs.com

Public Class FwCache
    Public Shared cache As New Hashtable 'app level cache
    Private Shared ReadOnly locker As New Object

    Public request_cache As New Hashtable 'request level cache

    Public Shared Function getValue(key As String) As Object
        Return cache(key)
    End Function

    Public Shared Sub setValue(key As String, value As Object)
        SyncLock locker
            cache(key) = value
        End SyncLock
    End Sub

    'remove one key from cache
    Public Shared Sub remove(key As String)
        SyncLock locker
            cache.Remove(key)
        End SyncLock
    End Sub

    'clear whole cache
    Public Shared Sub clear()
        SyncLock locker
            cache.Clear()
        End SyncLock
    End Sub

    '******** request-level cache ***********

    Public Function getRequestValue(key As String) As Object
        Return request_cache(key)
    End Function
    Public Sub setRequestValue(key As String, value As Object)
        request_cache(key) = value
    End Sub
    'remove one key from request cache
    Public Sub requestRemove(key As String)
        request_cache.Remove(key)
    End Sub
    'clear whole request cache
    Public Sub requestClear()
        request_cache.Clear()
    End Sub

End Class
