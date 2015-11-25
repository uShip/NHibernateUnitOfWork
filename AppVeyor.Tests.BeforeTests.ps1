$startPath = "$($env:appveyor_build_folder)\UnitOfWorkTests\bin\Debug"
$instanceName = 'SQL2014'
$sqlInstance = "localhost\$instanceName"
$dbName = "UnitOfWorkTest"

# edit the .CONFIG file; alter the connection string
$dotConfigPath = join-path $startPath "UnitOfWorkTests.dll.config"
$dotConfigXml = (Get-Content $dotConfigPath) -as [xml]
$dotConfigXml.SelectSingleNode('//connectionStrings/add[@name="UnitOfWorkTest"]').connectionString = "Server=$sqlInstance; Database=$dbName; Trusted_connection=true"
$dotConfigXml.SelectSingleNode('//appSettings/add[@key="NHibernate.ShowSql"]').value = "False"
$dotConfigXml.Save($dotConfigPath)

# create the database
sqlcmd -S "$sqlInstance" -d "master" -Q "CREATE DATABASE [$dbName]" 

# test connection to the new database
#sqlcmd -S "$sqlInstance" -d "$dbName" -Q "select 1"
