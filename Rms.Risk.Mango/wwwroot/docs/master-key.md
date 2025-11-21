# Master Key Generation

dbMango uses a master key for encryption and decryption of sensitive data (passwords, connection strings). 
There is no default master key; you must generate and provide your own. Master keys for different environments **MUST NOT** be the same.
This document provides instructions on how to generate a master key in the form of a `.p12`.
To generate a `.p12` (PKCS#12) key, you can use either OpenSSL or .NET tools.

When master key is generated you need to provide its path and password in the `appsettings.json` file ( See [Configuration](config.md) ):
```json

"SecuritySettings": {
    "MasterKeyFileName": "test-MasterKey-encrypted.p12",
    "MasterKeyPassword": "",
    ...
    }
}

```

Note that for `Development` environment you can use a standard .NET "application secrets" approach by leaving password 
blank and executing the following command in the project directory:

```sh

dotnet user-secrets set "SecuritySettings:MasterKeyPassword" "actual value"

```

For UAT and Production environments you should use a strong password. It can be loaded from Kubernetes secret or environment variable.
To load it from a secret you must specify file name in the `MasterKeyPassword` property. It should start with `#` symbol
to indicate that it is a file name, not a clear text value. For example:

```json

"SecuritySettings": {
    "MasterKeyFileName": "test-MasterKey-encrypted.p12",
    "MasterKeyPassword": "#/app/secrets/MasterKeyPassword",
    ...
    }
}

```

The corresponding Kubernetes secret can be created as follows:

```sh

kubectl create secret generic MasterKeyPassword --from-literal=password=your-strong-password

```

Secret then can be mounted to the `/app/secrets` folder in the container. Here is the example of the relevant part of the deployment YAML:

```yaml
        volumeMounts:
        - name: secret-volume
          mountPath: /app/secrets
          readOnly: true
      volumes:
      - name: secret-volume
        secret:
          secretName: MasterKeyPassword
```

### Using OpenSSL

The following steps outline how to create a `.p12` file using OpenSSL:
1. Generate a Private Key
2. Create a Certificate Signing Request (CSR)
3. Generate a Self-Signed Certificate
4. Combine the Private Key and Certificate into a `.p12` file

You will be prompted to set an export password for the `.p12` file.

```sh

openssl genrsa -out private.key 2048
openssl req -new -key private.key -out request.csr
openssl x509 -req -days 365 -in request.csr -signkey private.key -out certificate.crt
openssl pkcs12 -export -out certificate.p12 -inkey private.key -in certificate.crt -name "MyCertificate"

```

### Using .NET Tools
You can use the `System.Security.Cryptography` namespace in .NET to generate a `.p12` file programmatically. Here's an example:

```csharp
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

class Program
{
    static void Main()
    {
        // Generate RSA Key Pair
        using RSA rsa = RSA.Create(2048);

        // Create Certificate Request
        var request = new CertificateRequest(
            "CN=MyCertificate",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Self-Signed Certificate
        var certificate = request.CreateSelfSigned(
            DateTimeOffset.Now,
            DateTimeOffset.Now.AddYears(1));

        // Export to PKCS#12 (.p12)
        var pfxBytes = certificate.Export(X509ContentType.Pkcs12, "export-password");

        // Save to File
        System.IO.File.WriteAllBytes("certificate.p12", pfxBytes);

        Console.WriteLine("PKCS#12 file generated: certificate.p12");
    }
}
```

- Replace `"export-password"` with your desired password.
- The `.p12` file will be saved in the application's working directory.
