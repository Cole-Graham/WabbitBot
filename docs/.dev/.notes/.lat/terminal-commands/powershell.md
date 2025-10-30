

cd "C:\Users\coleg\Projects\WabbitBot"; dotnet build -clp:ErrorsOnly -v:q -p:WarningLevel=0 -p:NoWarn=NU1701
dotnet build -clp:ErrorsOnly -v:q -p:WarningLevel=0 -p:NoWarn=NU1701

cd "C:\Users\coleg\Projects\WabbitBot"; dotnet build -clp:ErrorsOnly -v:q -p:WarningLevel=0 -p:NoWarn=NU1701
dotnet build -clp:ErrorsOnly -v:q -p:WarningLevel=0 -p:NoWarn=NU1701

cd "C:\Users\coleg\Projects\WabbitBot"; dotnet build 2>&1 | Where-Object { $_ -match ": error " }
dotnet build 2>&1 | Where-Object { $_ -match ": error " }


# DATABASE
# powershell:
dotnet ef migrations add MeaningfulName -p src/WabbitBot.Core -s src/WabbitBot.Host -o Migrations
dotnet ef database update -p src/WabbitBot.Core -s src/WabbitBot.Host
