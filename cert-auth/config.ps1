$certificateName = "CertAuthSample"

$certificate=New-SelfSignedCertificate -Subject $certificateName `
                                           -CertStoreLocation "Cert:\CurrentUser\My" `
                                           -KeyExportPolicy Exportable `
                                           -KeySpec Signature
# save thumbprint
$certificate.Thumbprint | Out-File "thumb.txt"

# save certificate
Export-Certificate -Type CERT -Cert $certificate -FilePath "$($certificateName).cer"
