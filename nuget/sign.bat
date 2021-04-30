rem @ECHO off
set /p "nugetversion=" < _version.txt
nuget sign *.nupkg -CertificatePath %gs-nuget-certificate% -CertificatePassword "%gs-nuget-certificate-password%" -Timestamper http://sha256timestamp.ws.symantec.com/sha256/timestamp