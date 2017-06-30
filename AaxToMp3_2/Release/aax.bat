@ECHO OFF
IF "%1" == "" GOTO EXIT0
IF "%2" == "" GOTO EXIT0
cmd /K "AaxToMp3.exe -i ""%1"" | ffmpeg.exe -i pipe0 ""%2"""
:EXIT0