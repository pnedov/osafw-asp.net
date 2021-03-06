# osafw-asp.net
ASP.NET web framework in pure VB.NET code optimized for creation of Business Applications

Created as simplified and lightweight alternative to other ASP.NET frameworks like ASP.NET MVC

![image](https://user-images.githubusercontent.com/1141095/75820467-0200b380-5d62-11ea-9340-e0942b460eb1.png)

## Features
- simple and straightforward in development and maintenance
- MVC-like - code, data, templates are split
  - code consists of: controllers, models, framework core and optional 3rd party libs
  - uses [ParsePage template engine](https://github.com/osalabs/parsepage)
  - data stored by default in SQL Server database [using db.net](https://github.com/osalabs/db.net)
- RESTful with some practical enhancements
- integrated auth - simple flat access levels auth
- UI based on [Bootstrap 4](http://getbootstrap.com) with minimal custom CSS - it's easy to customzie or apply your own theme
- use of well-known 3rd party libraries: [jQuery](http://jquery.com), [jQuery Form](https://github.com/malsup/form), jGrowl, markdown libs, etc...

## Demo
http://demo.engineeredit.com/ - this is how it looks in action right after installation before customizations

## Installation
1. put contents of `/www` into your webserver's public html folder
2. edit `/www/web.config` for database connection_string
3. create database from `/www/App_Data/sql/fwdatabase.sql`
4. open site in your browser and login with credentials as defined in database.sql (remember to change password)

## Documentation

### Development
1. in Visual Studio do `File -> Open -> Web Site...` (Shift+Alt+O) and open `/www` folder
2. press Ctrl+F5 to run (or F5 if you really need debugger)
3. review debug log in `/www/App_Data/main.log`
4. edit or create new controllers and models in `/www/App_Code/controllers` and `/www/App_Code/models`
5. modify templates in `/www/App_Data/template`

### Directory structure
```
/App_Code          - all the VB.NET code is here
  /controllers     - your controllers
  /fw              - framework core libs
  /models          - your models
/App_Data          - non-public directory
  /sql             - initial database.sql script and update sql scripts
  /template        - all the html templates
  /main.log        - application log (ensure to enable write rights for IIS)
/assets            - your web frontend assets
  /css
  /fonts
  /img
  /js
/bin               - additional .net dlls will be here if you install something from Nuget
/upload            - upload dir for public files
/error.html        - default error.html
/robots.txt        - default robots.txt (empty)
/favicon.ico       - change to your favicon!
/web.config        - settings for db connection, mail, logging and for IIS/.NET stuff too
```

### REST mappings
Controllers automatically directly mapped to URLs, so developer doesn't need to write routing rules:

  - `GET /Controller` - list view `IndexAction()`
  - `GET /Controller/ID` - one record view `ShowAction()`
  - `GET /Controller/ID/new` - one record new form `ShowFormAction()`
  - `GET /Controller/ID/edit` - one record edit form `ShowFormAction()`
  - `GET /Controller/ID/delete` - one record delete confirmation form `ShowDeleteAction()`
  - `POST /Controller` - insert new record `SaveAction()`
  - `PUT /Controller` - update multiple records `SaveMultiAction()`
  - `POST/PUT /Controller/ID` - update record `SaveAction()`
  - `POST/DELETE /Controller/ID` - delete record ($_POST should be empty) `DeleteAction()`
  - `GET/POST /Controller/(Something)[/ID]` - call for arbitrary action from the controller `SomethingAction()`

For example `GET /Products` will call `ProductsController.IndexAction()`
And this will cause rendering templates from `/www/App_Data/templates/products/index`

### Request Flow

highlighted as bold is where you could place your code.

- `FW.run()`
  - **`FwHooks.initRequest()`** - place code here which need to be run on request start
- `fw.dispatch()` - performs REST urls matching and calls controller/action, if no controller found calls `HomeController.NotFoundAction()`, if no requested action found in controller - calls controller action defined in contoller's `route_default_action` (either "index" or "show")
  - `fw._auth()`  - check if user can access requested controller/action, also performs basic CSRF validation
  - `fw.call_controller()`
    - **`SomeController.init()`** - place code here which need to be run every time request comes to this controller
    - **`SomeController.SomeAction()`** - your code for particular action
      - **`SomeModel.someMethod()`** - controllers may call model's methods, place most of your business logic in models
- `fw.Finalize()`

#### Examples:
- GET /Admin/Users
  - `FwHooks.initRequest()`
  - `AdminUsers.init()`
  - `AdminUsers.IndexAction()`
  - then ParsePage parses templates from `/template/admin/users/index/`

- GET /Admin/Users/123/edit
  - `FwHooks.initRequest()`
  - `AdminUsers.init()`
  - `AdminUsers.ShowFormAction(123)`
    - `Users.one(123)`
  - then ParsePage parses templates from `/template/admin/users/showform/`

- POST /Admin/Users/123
  - `FwHooks.initRequest()`
  - `AdminUsers.init()`
  - `AdminUsers.SaveAction(123)`
    - `Users.update(123)`
  - `fw.redirect("/Admin/Users/123/edit")` //redirect back to edit screen after db updated

#### Flow in IndexAction

Frequently asked details about flow for the `IndexAction()` (in controllers inherited from `FwAdminController` and `FwDynamicController`):

1. `initFilter()` - initializes `Me.list_filter` from query string filter params `&f[xxx]=...`, note, filters remembered in session
1. `setListSorting()` - initializes `Me.list_orderby` based on `list_filter("sortby")` and `list_filter("sortdir")`, also uses `Me.list_sortdef` and `Me.list_sortmap` which can be set in controller's `init()` or in `config.json`
1. `setListSearch()` - initializes `Me.list_where` based on `list_filter("s")` and `Me.search_fields`
1. `setListSearchStatus()` - add to `Me.list_where` filtering  by `status` field if such field defined in the controller's model
1. `getListRows()` - query database and save rows to `Me.list_rows` (only current page based on `Me.list_filter("pagenum")` and `Me.list_filter("pagesize")`). Also sets `Me.list_count` to total rows matched by filters and `Me.list_pager` for pagination if there are more than one page. Uses `Me.list_view`, `Me.list_where`, `Me.list_orderby`

You could either override these particular methods or whole `IndexAction()` in your specific controller.

The following controller fields used above can be defined in controller's `init()` or in `config.json`:
- `Me.list_sortdef` - default list sorting in format: "sort_name[ asc|desc]"
- `Me.list_sortmap` - mapping for sort names (from `list_filter["sortby"]`) to actual db fields, Hashtable `sort_name => db_field_name`
- `Me.search_fields` - search fields, space-separated
- `Me.list_view` - table/view to use in `getListRows()`, if empty model's `table_name` used

### How to Debug

Main and recommended approach - use `fw.logger()` function, which is available in controllers and models (so no prefix required).
Examples: `logger("some string to log", var_to_dump)`, `logger(LogLevel.WARN, "warning message")`
All logged messages and var content (complex objects will be dumped wit structure when possible) written on debug console as well as to log file (default `/App_Data/main.log`)
You could configure log level in `web.config` - search "log_level" in `appSettings`

Another debug function that might be helpful is `fw.rw()` - but it output it's parameter directly into response output (i.e. you will see output right in the browser)

### Best Practices / Recommendations
- naming conventions:
  - table name: `user_lists` (lowercase, underscore delimiters is optional)
  - model name: `UserLists` (UpperCamelCase)
  - controller name: `UserListsController` or `AdminUserListsController` (UpperCamelCase with "Controller" suffix)
  - template path: `/template/userlists`  
- keep all paths without trailing slash, use beginning slash where necessary
- db updates:
  - first, make changes in `/App_Data/sql/database.sql` - this file is used to create db from scratch
  - then create a file `/App_Data/sql/updateYYYY-MM-DD.sql` with all the CREATE, ALTER, UPDATE... - this will allow to apply just this update to existing database instances
- use `fw.route_redirect()` if you got request to one Controller.Action, but need to continue processing in another Controller.Action
  - for example, if for a logged user you need to show detailed data and always skip list view - in the `IndexAction()` just use `fw.routeRedirect("ShowForm")`
- uploads
  - save all public-readable uploads under `/www/upload` (default, see "UPLOAD_DIR" in `web.config`)
  - for non-public uploads use `/www/App_Data/upload`
  - or `S3` model and upload to the cloud
- put all validation code into controller's `Validate()`. See usage example in `AdminDemosController`
- use `logger()` and review `/App_Data/main.log` if you stuck
  - make sure you have "log_level" set to "DEBUG" in `web.config`

