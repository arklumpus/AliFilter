@echo off

setlocal EnableDelayedExpansion

if "%~1"=="" (
	echo.
	echo [91mThis tool requires the following parameters:[0m
	echo [91m    Code signing certificate subject name[0m
	echo [91m    Smart card PIN[0m
	
	exit /b 64
)

if "%~2"=="" (
	echo.
	echo [91mThis tool requires the following parameters:[0m
	echo [91m    Code signing certificate subject name[0m
	echo [91m    Smart card PIN[0m
	
	exit /b 64
)

set certsub=%1
set certpin=%2

echo Building with target [94mWin-x64[0m

echo.
echo [104;97mDeleting previous build...[0m

for /f %%i in ('dir /a:d /b Release\win-x64\*') do rd /s /q Release\win-x64\%%i
del Release\win-x64\* /s /f /q 1>nul

echo.
echo [104;97mBuilding AliFilter...[0m

cd AliFilter
dotnet publish -c Release /p:PublishProfile=Properties\PublishProfiles\win-x64.pubxml
cd ..

echo.
echo [104;97mRemoving additional files...[0m

del Release\win-x64\AliFilter.pdb
del Release\win-x64\AliFilter.xml
del Release\win-x64\Accord.dll.config

echo.
echo [104;97mSigning executable...[0m

scsigntool -pin %certpin% sign /fd sha256 /n %certsub% /tr "http://ts.ssl.com" /td sha256 /v /a /d "AliFilter: a machine learning approach to alignment filtering" /du "https://github.com/arklumpus/AliFilter" Release/win-x64/AliFilter.exe

echo.
echo [94mDone![0m