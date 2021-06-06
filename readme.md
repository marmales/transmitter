To run this application you will need new json configuration (filename: _appsettings.secrets.json_) with encrypted values.
```
{
  "Thumbprint": "your_thumbprint",
  "Credentials": {
    "ImapUsername": "encrypted_imap_login",
    "SmtpUsername": "encrypted_smtp_login",
    "Password": "encrypted_server_password"
  },
  "Smtp": {
    "Recipients": ["encrypted_recipients"]
  }
}
```
