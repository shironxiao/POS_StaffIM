# Quick Reference: Dynamic Server Configuration

## 🎯 Key Files

| File | Purpose |
|------|---------|
| `Models\DatabaseConfig.vb` | Configuration model |
| `Utils\EncryptionHelper.vb` | Password encryption (AES-256) |
| `Utils\ConfigManager.vb` | config.json management |
| `modDB.vb` | Dynamic connection string |
| `ServerConfig.vb` | Server configuration UI logic |
| `LogIn.vb` | Connection validation |
| `config.json` | Server settings (created at runtime) |

## 🔑 Key Methods

### **ConfigManager**
```vb
ConfigManager.ConfigExists() As Boolean
ConfigManager.LoadConfig() As DatabaseConfig
ConfigManager.SaveConfig(config As DatabaseConfig) As Boolean
ConfigManager.GetConfigPath() As String
```

### **EncryptionHelper**
```vb
EncryptionHelper.Encrypt(plainText As String) As String
EncryptionHelper.Decrypt(encryptedText As String) As String
```

### **modDB**
```vb
modDB.ConnectionString As String  ' Dynamic, loaded from config
modDB.TestConnectionWithConfig(config, errorMessage) As Boolean
modDB.TestConnectionWithFallback(config, usedBackup, errorMessage) As Boolean
modDB.ReloadConnectionString()  ' Call after saving new config
```

## 📋 Common Tasks

### **Get Current Connection String**
```vb
Dim connStr As String = modDB.ConnectionString
```

### **Test Connection Before Saving**
```vb
Dim config As New DatabaseConfig() With {
    .ServerIP = "192.168.1.100",
    .DatabaseName = "tabeya_system",
    .Username = "root",
    .Password = "mypassword",
    .Port = "3306"
}

Dim errorMsg As String = ""
If modDB.TestConnectionWithConfig(config, errorMsg) Then
    ' Connection successful
    MessageBox.Show("Connected!")
Else
    ' Connection failed
    MessageBox.Show(errorMsg)
End If
```

### **Save Configuration**
```vb
Dim config As New DatabaseConfig() With {
    .ServerIP = "localhost",
    .BackupServerIP = "192.168.1.101",
    .DatabaseName = "tabeya_system",
    .Username = "root",
    .Password = "mypassword",
    .Port = "3306"
}

If ConfigManager.SaveConfig(config) Then
    modDB.ReloadConnectionString()  ' Important!
    MessageBox.Show("Configuration saved!")
End If
```

### **Load Existing Configuration**
```vb
If ConfigManager.ConfigExists() Then
    Dim config As DatabaseConfig = ConfigManager.LoadConfig()
    If config IsNot Nothing Then
        ' Use config
        Console.WriteLine($"Server: {config.ServerIP}")
        Console.WriteLine($"Database: {config.DatabaseName}")
    End If
End If
```

### **Encrypt/Decrypt Password**
```vb
' Encrypt
Dim encrypted As String = EncryptionHelper.Encrypt("mypassword")

' Decrypt
Dim decrypted As String = EncryptionHelper.Decrypt(encrypted)
```

## 🚨 Error Handling

### **MySQL Error Codes**
| Code | Meaning | User Message |
|------|---------|--------------|
| 0 | Cannot connect | "Cannot connect to server. Please check the server IP address." |
| 1042 | Connection refused | "Unable to connect to server. Server may be offline or IP address is incorrect." |
| 1045 | Access denied | "Access denied. Please check your username and password." |
| 1049 | Unknown database | "Unknown database. Please verify the database name." |

### **Example Error Handling**
```vb
Try
    Dim config As DatabaseConfig = ConfigManager.LoadConfig()
    If config Is Nothing Then
        ' Handle missing config
        MessageBox.Show("Please configure server settings")
        Dim configForm As New ServerConfig()
        configForm.ShowDialog()
    End If
Catch ex As Exception
    MessageBox.Show($"Error: {ex.Message}")
End Try
```

## 🔄 Application Flow

```
Startup
  ↓
Check config.json exists?
  ├─ NO → Open ServerConfig form
  │         ↓
  │       User enters details
  │         ↓
  │       Test connection
  │         ↓
  │       Save config (encrypted)
  │         ↓
  └─ YES → Load config
            ↓
          Test connection
            ├─ Primary fails → Try backup
            │                    ↓
            │                  Success? → Continue
            │                    ↓
            │                  Fail? → Open ServerConfig
            │
            └─ Success → Open Login form
```

## 💡 Best Practices

1. **Always test connection before saving**
   ```vb
   If modDB.TestConnectionWithConfig(config, errorMsg) Then
       ConfigManager.SaveConfig(config)
   End If
   ```

2. **Reload connection string after saving**
   ```vb
   ConfigManager.SaveConfig(config)
   modDB.ReloadConnectionString()  ' Don't forget!
   ```

3. **Validate config before using**
   ```vb
   If config IsNot Nothing AndAlso config.IsValid() Then
       ' Use config
   End If
   ```

4. **Handle errors gracefully**
   ```vb
   Try
       ' Database operation
   Catch ex As MySqlException
       ' Show user-friendly message
       MessageBox.Show($"Database error: {ex.Message}")
   End Try
   ```

## 🔐 Security Notes

- Passwords are encrypted with AES-256
- Encryption key is embedded in `EncryptionHelper.vb`
- **Not secure** against attackers with executable access
- For production: Consider Windows DPAPI or Azure Key Vault

## 📦 Dependencies

- **MySql.Data** (9.1.0) - MySQL connector
- **Newtonsoft.Json** (13.0.3) - JSON serialization
- **System.Security.Cryptography** - AES encryption

## 🎯 Quick Checklist

- [ ] config.json exists in application directory
- [ ] Password is encrypted in config.json
- [ ] Connection string is dynamic (not hardcoded)
- [ ] Test connection before saving
- [ ] Reload connection string after saving
- [ ] Handle connection errors gracefully
- [ ] Validate config before using
- [ ] Backup server IP configured (optional)
