$startPath = "$($env:appveyor_build_folder)\bin\Debug"
$instanceName = 'SQL2014'
$sqlInstance = "localhost\$instanceName"
$dbName = "UnitOfWorkTest"

# edit the .CONFIG file; alter the connection string
$dotConfigPath = join-path $startPath "UOW.dll.config"
$dotConfigXml = (Get-Content $dotConfigPath) -as [xml]
$dotConfigXml.SelectSingleNode('//connectionStrings/add[@name="UnitOfWorkTest"]').connectionString = "Server=$sqlInstance; Database=$dbName; Trusted_connection=true"
$dotConfigXml.SelectSingleNode('//appSettings/add[@name="NHibernate.ShowSql"]').value = "False"
$dotConfigXml.Save($dotConfigPath)

# create the database
sqlcmd -S "$sqlInstance" -d "master" -Q "CREATE DATABASE [$dbName]" 
    # ON (FILENAME = '$mdfFile'),(FILENAME = '$ldfFile') for ATTACH"

# test connection to the new database
sqlcmd -S "$sqlInstance" -d "$dbName" -Q "select 1"
