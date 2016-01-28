﻿' Home Page controller
'
' Part of ASP.NET osa framework  www.osalabs.com/osafw/asp.net
' (c) 2009-2013 Oleg Savchuk www.osalabs.com

Public Class HomeController
    Inherits FwController
    Public Shared Shadows route_default_action As String = "show"

    Public Overrides Sub init(fw As FW)
        MyBase.init(fw)
    End Sub

    'CACHED as home_page
    Public Function IndexAction() As Hashtable
        Dim ps As Hashtable = FwCache.get_value("home_page")

        If IsNothing(ps) OrElse ps.Count = 0 Then
            'CACHE MISS
            ps = New Hashtable

			'create home page with heavy queries

            FwCache.set_value("home_page", ps)
        End If
        ps("hide_sidebar") = True
        Return ps
    End Function

    Public Sub ShowAction(Optional ByVal id As String = "")
        Dim hf As Hashtable = New Hashtable

        fw.parser("/home/" & Utils.route_fix_chars(LCase(id)), fw.config("PAGE_TPL_USER"), hf)
    End Sub

    'called if fw.dispatch can't find controller
    Public Sub NotFoundAction()
        fw.model(Of Spages).show_page_by_full_url(fw.request_url)
    End Sub


    Public Sub InitAction(Optional ByVal id As String = "")
        Dim hf As Hashtable = New Hashtable

        rw("initializing db [" & fw.config("db_name") & "]")
        db.check_create_mdb(fw.config("db_name"))
        rw("done")

        'parser("/index", hf)
    End Sub

    Public Sub TestAction(Optional ByVal id As String = "")
        Dim hf As Hashtable = New Hashtable
        logger("in the TestAction")
        rw("here it is Test")
        rw("id=" & id)
        rw("more_action_name=" & fw.cur_action_more)

        'parser("/index", hf)
    End Sub
    
End Class

