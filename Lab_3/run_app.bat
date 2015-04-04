@setlocal enableextensions
@cd /d "%~dp0"

rem session server
cd SessionServer\bin\debug
start sessionserver.exe
cd ..\..\..\
rem clients server
cd ClientsServer\bin\debug
start clientsserver.exe
cd ..\..\..\
rem photos server
cd PhotosServer\bin\debug
start photosserver.exe
cd ..\..\..\
rem frontend server
cd FrontendServer\bin\debug
start frontendserver.exe
exit