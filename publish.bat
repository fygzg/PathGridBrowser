@echo off
chcp 65001 >nul
setlocal

set PROJECT=DirectoryGridBrowser.csproj
set CONFIG=Release
set OUT=publish

echo ========================================
echo  自包含发布 - 打包 .NET 运行时
echo ========================================
echo.

if not exist "%OUT%" mkdir "%OUT%"

for %%R in (win-x64 win-x86 win-arm64) do (
    echo [发布] %%R ...
    dotnet publish "%PROJECT%" -c %CONFIG% -r %%R -o "%OUT%\%%R"
    if errorlevel 1 (
        echo [失败] %%R 发布出错
        exit /b 1
    )
    echo [完成] %%R -^> %OUT%\%%R\DirectoryGridBrowser.exe
    echo.
)

echo ========================================
echo  全部发布完成，输出目录: %OUT%
echo  - publish\win-x64   64位 Windows
echo  - publish\win-x86   32位 Windows
echo  - publish\win-arm64 ARM Windows
echo ========================================

endlocal
