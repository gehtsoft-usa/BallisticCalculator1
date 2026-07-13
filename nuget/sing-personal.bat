forfiles /m *.nupkg /c "cmd /c nuget sign -CertificateFingerprint %SM_THUMBPRINT% -Timestamper http://time.certum.pl @path"
