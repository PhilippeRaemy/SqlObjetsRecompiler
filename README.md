# SqlObjetsRecompiler
A necessary testing step when refactoring a database structure can be to recompile programmatic objects and views: 
if code quality is poor and not all tables are qualified by a schema, and not all columns are prefixed by a table name or alias, adding a column to a table can break code.

## Process
SqlObjetsRecompiler iterates teh stored procedures, user defined functions, table triggers and views of a selected SqlServer database, and tries to recompile them.
The status of the sql objects is not affected.

## Output
A list of the successfully recompiled objects is produced on the sysout, while failed compilations with error message and source code are echoed on the syserr.

## Remarks
So far, the connection to the database server is (only) trusted, and the connected user must have the appropriate rights to alter the programmatic objects targeted by this utility.

## Further developments
* Allow SQL security
* Filter / select targetted objects (by schema, by name, by objct type)
