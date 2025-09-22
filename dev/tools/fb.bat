@echo off
powershell -ExecutionPolicy Bypass -Command "& { . '%~dp0findrun.ps1' %* }"
