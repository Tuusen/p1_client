@echo off
chcp 65001 >nul
echo ============================================
echo Qoder .csproj 更新工具
echo ============================================
echo.

python "%~dp0update_csproj.py"

echo.
pause
