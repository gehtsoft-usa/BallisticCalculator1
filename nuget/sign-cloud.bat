rem @ECHO off
rem forfiles /m *.nupkg /c "cmd /c smctl sign --tool nuget --verbose --keypair-alias key_525680920 --timestamp true --input @path"
forfiles /m *.nupkg /c "cmd /c nuget sign -CertificateFingerprint 0D96BC4CC55412DAE19410FB208FFC550C32A20EA1E337388FF0776B358744C0 -Timestamper http://timestamp.digicert.com @path"
