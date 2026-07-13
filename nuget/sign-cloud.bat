rem @ECHO off
forfiles /m *.nupkg /c "cmd /c nuget sign -CertificateFingerprint %digicert-fingerprint% -Timestamper http://timestamp.digicert.com @path"
