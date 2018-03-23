# Instructions

1. Generate self-signed certificate
```
$certificate = New-SelfSignedCertificate `
-Subject $certificateName `
-CertStoreLocation "Cert:\CurrentUser\My" `
-KeyExportPolicy Exportable `              
-KeySpec Signature
```
2. Get thumbprint and public key 
```
$certificate.Thumbprint

Export-Certificate -Type CERT -Cert $certificate -FilePath "certificate.cer"
```
4. Create web/api app in Azure, goto Keys
5. Add secret
6. Upload public key
7. Add app user in CRM, add role (no more role restrictions - all roles are custom roles anyway within the new solution system)
8. Modify code (app id, secret, thumbprint) 
