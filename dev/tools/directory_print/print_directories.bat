@echo off
echo Running directory structure analysis...

python "%~dp0directory_print.py" ..\..\src\WabbitBot.Common -o Common.txt
python "%~dp0directory_print.py" ..\..\src\WabbitBot.Core -o Core.txt
python "%~dp0directory_print.py" ..\..\src\WabbitBot.DiscBot -o DiscBot.txt
python "%~dp0directory_print.py" ..\..\src\WabbitBot.SourceGenerators -o SourceGenerators.txt
python "%~dp0directory_print.py" ..\..\src -o src.txt --include-dirs WabbitBot.Common WabbitBot.Core WabbitBot.DiscBot WabbitBot.SourceGenerators

echo Done! Check the notes directory for the output files. 