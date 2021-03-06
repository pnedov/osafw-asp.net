﻿' Users model class
'
' Part of ASP.NET osa framework  www.osalabs.com/osafw/asp.net
' (c) 2009-2013 Oleg Savchuk www.osalabs.com

Imports BCrypt.Net.BCrypt

Public Class Users
    Inherits FwModel
    'ACL constants
    Public Const ACL_VISITOR As Integer = -1
    Public Const ACL_MEMBER As Integer = 0
    Public Const ACL_ADMIN As Integer = 100

    Private ReadOnly table_menu_items As String = "menu_items"

    Public Sub New()
        MyBase.New()
        table_name = "users"
        csv_export_fields = "id fname lname email add_time"
        csv_export_headers = "id,First Name,Last Name,Email,Registered"
    End Sub

    Public Function meId() As Integer
        Return Utils.f2int(fw.SESSION("user_id"))
    End Function

    Public Function oneByEmail(email As String) As Hashtable
        Dim where As Hashtable = New Hashtable
        where("email") = email
        Dim hU As Hashtable = db.row(table_name, where)
        Return hU
    End Function

    ''' <summary>
    ''' return full user name - First Name Last Name
    ''' </summary>
    ''' <param name="id">Object type because if upd_users_id could be null</param>
    ''' <returns></returns>
    Public Overloads Function iname(id As Object) As String
        Dim result As String = ""

        If Utils.f2int(id) > 0 Then
            Dim item = one(id)
            result = item("fname") & "  " & item("lname")
        End If

        Return result
    End Function

    'check if user exists for a given email
    Public Overrides Function isExists(uniq_key As Object, not_id As Integer) As Boolean
        Return isExistsByField(uniq_key, not_id, "email")
    End Function

    Public Overrides Function add(item As Hashtable) As Integer
        If Not item.ContainsKey("pwd") Then item("pwd") = Utils.getRandStr(8) 'generate password
        item("pwd") = Me.hashPwd(item("pwd"))
        Return MyBase.add(item)
    End Function

    Public Overrides Function update(id As Integer, item As Hashtable) As Boolean
        If item.ContainsKey("pwd") Then item("pwd") = Me.hashPwd(item("pwd"))
        Return MyBase.update(id, item)
    End Function

    ''' <summary>
    ''' performs any required password cleaning (for now - just limit pwd length at 32 and trim)
    ''' </summary>
    ''' <param name="plain_pwd">non-encrypted plain pwd</param>
    ''' <returns>clean plain pwd</returns>
    Public Function cleanPwd(plain_pwd As String) As String
        Return Trim(Left(plain_pwd, 32))
    End Function

    ''' <summary>
    ''' generate password hash from plain password
    ''' </summary>
    ''' <param name="plain_pwd">plain pwd</param>
    ''' <returns>hash using 'https://github.com/BcryptNet/bcrypt.net</returns>
    Public Function hashPwd(plain_pwd As String) As String
        Try
            Return EnhancedHashPassword(cleanPwd(plain_pwd))
        Catch ex As Exception
            'Invalid salt version
        End Try
        Return False
    End Function

    ''' <summary>
    ''' return true if plain password has the same hash as provided
    ''' </summary>
    ''' <param name="plain_pwd">plain pwd from user input</param>
    ''' <param name="pwd_hash">password hash previously generated by hashPwd</param>
    ''' <returns></returns>
    Public Function checkPwd(plain_pwd As String, pwd_hash As String) As Boolean
        Try
            Return EnhancedVerify(cleanPwd(plain_pwd), pwd_hash)
        Catch ex As Exception
            'Invalid salt version
        End Try
        Return False
    End Function

    ''' <summary>
    ''' generate reset token, save to users and send pwd reset link to the user
    ''' </summary>
    ''' <param name="id"></param>
    ''' <returns></returns>
    Public Function sendPwdReset(id As Integer) As Boolean
        Dim pwd_reset_token = Utils.getRandStr(50)

        Dim item As New Hashtable From {
                {"pwd_reset", Me.hashPwd(pwd_reset_token)},
                {"pwd_reset_time", Now()}
            }
        Me.update(id, item)

        Dim user = Me.one(id)
        user("pwd_reset_token") = pwd_reset_token

        Return fw.send_email_tpl(user("email"), "email_pwd.txt", user)
    End Function

    'fill the session and do all necessary things just user authenticated (and before redirect
    Public Function doLogin(id As Integer) As Boolean
        fw.SESSION.Clear()
        fw.SESSION("is_logged", True)
        fw.SESSION("XSS", Utils.getRandStr(16))

        reloadSession(id)

        fw.logEvent("login", id)
        'update login info
        Dim fields As New Hashtable
        fields("login_time") = Now()
        Me.update(id, fields)
        Return True
    End Function

    Public Function reloadSession(Optional id As Integer = 0) As Boolean
        If id = 0 Then id = meId()
        Dim hU As Hashtable = one(id)

        Dim access_level = Utils.f2int(hU("access_level"))
        fw.SESSION("user_id", id)
        fw.SESSION("login", hU("email"))
        fw.SESSION("access_level", access_level)
        fw.SESSION("lang", hU("lang"))
        'fw.SESSION("user", hU)
        Dim fname = Trim(hU("fname"))
        Dim lname = Trim(hU("lname"))
        fw.SESSION("user_name", fname & IIf(fname > "", " ", "") & lname) 'will be empty If no user name Set

        Return True
    End Function

    'return standard list of id,iname where status=0 order by iname
    Public Overrides Function list() As ArrayList
        Dim sql As String = "select id, fname+' '+lname as iname from " & table_name & " where status=0 order by fname, lname"
        Return db.array(sql)
    End Function
    Public Overrides Function listSelectOptions(Optional params As Object = Nothing) As ArrayList
        Dim sql As String = "select id, fname+' '+lname as iname from " & table_name & " where status=0 order by fname, lname"
        Return db.array(sql)
    End Function

    ''' <summary>
    ''' check if current user acl is enough. throw exception or return false if user's acl is not enough
    ''' </summary>
    ''' <param name="acl">minimum required access level</param>
    Public Function checkAccess(acl As Integer, Optional is_die As Boolean = True) As Boolean
        Dim users_acl As Integer = Utils.f2int(fw.SESSION("access_level"))

        'check access
        If users_acl < acl Then
            If is_die Then Throw New ApplicationException("Access Denied")
            Return False
        End If

        Return True
    End Function

    Sub loadMenuItems()
        Dim menu_items As ArrayList = FwCache.getValue("menu_items")

        If IsNothing(menu_items) Then
            'read main menu items for sidebar
            menu_items = db.array(table_menu_items, New Hashtable From {{"status", 0}}, "iname")
            FwCache.setValue("menu_items", menu_items)
        End If

        'only Menu items user can see per ACL
        Dim users_acl = Utils.f2int(fw.SESSION("access_level"))
        Dim result As New ArrayList
        For Each item As Hashtable In menu_items
            If Utils.f2int(item("access_level")) <= users_acl Then result.Add(item)
        Next

        fw.G("menu_items") = result
    End Sub

End Class
