rem @ECHO off
forfiles /m *.nupkg /c "cmd /c smctl sign --fingerprint %digicert-fingerprint% --input @path"
rem for /f %f in ('dir /b *.nupkg' ) do smctl sign --keypair-alias %digicert-alias% %f
