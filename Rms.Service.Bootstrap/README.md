# RMS API Catalog
## Features

- Swagger / SwaggerUI      : /swagger, /swagger/v1/swagger.json
- OpenTelemetry            : /metrics
- REST API                 : /api/ping
- CIDP JWT token bearer authentication

# Security and authentication
## Secrets

You need to execute the following command in the project (not solution!) folder:

	dotnet user-secrets set "SecuritySettings:Oidc:Secret" "xxx"
	dotnet user-secrets set "SecuritySettings:CertificatePassword" "xxx"
	dotnet user-secrets set "SecuritySettings:FxAdmin:ConnectionPassword" "xxx"

Note that SecuritySettings:Oidc:Secret value here is an actual secret and should be either encrypted or
stored in some sort of secret manager such as Secret object in Kubernetes.

To check secrets values use this command in project folder:

	dotnet user-secrets list

## Certificates

If your service uses HTTPs and mTLS you have to set up server-side certificate. To do so use dbPKI to generate private key and a certificate,
download key storage in *PKCS12* format and set CertificateFileName/CertificatePassword in SecuritySectio in appsettings.json.

Note that neither .cer nor .crt files will do. Such files usually marked as "Certificate in DER (or PEM) format". They contain no private key - service will log an error and exit.
If you need to set up client-side certificate for mTLS, you have to set ClientCertificateFileName/ClientCertificatePassword in SecuritySection in appsettings.json.