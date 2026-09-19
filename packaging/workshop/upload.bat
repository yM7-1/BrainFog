@echo off
chcp 65001 >nul
title BrainFog - Steam Workshop Upload
D:\steamcmd\steamcmd.exe +login lwh4646 +workshop_build_item "D:\0_git\BrainFog\packaging\workshop\BrainFog_workshop.vdf" +quit
echo.
echo Done. PublishedFileID is shown above.
pause
