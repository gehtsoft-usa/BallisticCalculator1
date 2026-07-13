forfiles /m *.nupkg /c "cmd /c nuget sign -CertificateFingerprint %CERTUM_CERTIFICATE% -Timestamper http://time.certum.pl @path"
