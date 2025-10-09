@echo off
echo Running directory structure analysis...

python "%~dp0directory_print.py" "%~dp0..\..\..\src\WabbitBot.Analyzers" -o Analyzers.txt
python "%~dp0directory_print.py" "%~dp0..\..\..\src\WabbitBot.Analyzers.Tests" -o Analyzers.Tests.txt
python "%~dp0directory_print.py" "%~dp0..\..\..\src\WabbitBot.Common" -o Common.txt
python "%~dp0directory_print.py" "%~dp0..\..\..\src\WabbitBot.Common.Tests" -o Common.Tests.txt
python "%~dp0directory_print.py" "%~dp0..\..\..\src\WabbitBot.Core" -o Core.txt
python "%~dp0directory_print.py" "%~dp0..\..\..\src\WabbitBot.Core.Tests" -o Core.Tests.txt
python "%~dp0directory_print.py" "%~dp0..\..\..\src\WabbitBot.DiscBot" -o DiscBot.txt
python "%~dp0directory_print.py" "%~dp0..\..\..\src\WabbitBot.Events.Shared" -o Events.Shared.txt
python "%~dp0directory_print.py" "%~dp0..\..\..\src\WabbitBot.Generator.Shared" -o Generator.Shared.txt
python "%~dp0directory_print.py" "%~dp0..\..\..\src\WabbitBot.Generator.Shared.Tests" -o Generator.Shared.Tests.txt
python "%~dp0directory_print.py" "%~dp0..\..\..\src\WabbitBot.Host" -o Host.txt
python "%~dp0directory_print.py" "%~dp0..\..\..\src\WabbitBot.SourceGenerators" -o SourceGenerators.txt
python "%~dp0directory_print.py" "%~dp0..\..\..\src\WabbitBot.SourceGenerators.Tests" -o SourceGenerators.Tests.txt
python "%~dp0directory_print.py" "%~dp0..\..\..\src" -o src.txt ^
    --include-dirs ^
        WabbitBot.Analyzers ^
        WabbitBot.Analyzers.Tests ^
        WabbitBot.Common ^
        WabbitBot.Common.Tests ^
        WabbitBot.Core ^
        WabbitBot.Core.Tests ^
        WabbitBot.DiscBot ^
        WabbitBot.Events.Shared ^
        WabbitBot.Generator.Shared ^
        WabbitBot.Generator.Shared.Tests ^
        WabbitBot.Host ^
        WabbitBot.SourceGenerators ^
        WabbitBot.SourceGenerators.Tests
echo Done! Check the notes directory for the output files. 