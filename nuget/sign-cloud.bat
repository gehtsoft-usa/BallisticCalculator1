rem @ECHO off
forfiles /m *.nupkg /c "cmd /c nuget sign -CertificateFingerprint %DIGICERT_CERTIFICATE% -Timestamper http://timestamp.digicert.com @path"
