rem @ECHO off
set /p "nugetversion=" < _version.txt
nuget sign *.nupkg -CertificatePath %ng-nuget-certificate% -CertificatePassword "%ng-nuget-certificate-password%" -Timestamper http://sha256timestamp.ws.symantec.com/sha256/timestamp