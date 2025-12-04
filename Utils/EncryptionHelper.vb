Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' Provides AES-256 encryption and decryption for sensitive data
''' Used primarily for encrypting database passwords in config.json
''' </summary>
Public Class EncryptionHelper
    ' Fixed encryption key - In production, consider using Windows DPAPI or Azure Key Vault
    ' This key should be kept secure and not shared publicly
    Private Shared ReadOnly EncryptionKey As String = "TabeyaPOS2024!SecureKey#9876"
    Private Shared ReadOnly IV As String = "1234567890123456" ' 16 bytes for AES

    ''' <summary>
    ''' Encrypts a plain text string using AES-256 encryption
    ''' </summary>
    ''' <param name="plainText">The text to encrypt</param>
    ''' <returns>Base64-encoded encrypted string</returns>
    Public Shared Function Encrypt(plainText As String) As String
        If String.IsNullOrEmpty(plainText) Then
            Return String.Empty
        End If

        Try
            Using aes As Aes = Aes.Create()
                aes.Key = GetKey()
                aes.IV = GetIV()
                aes.Mode = CipherMode.CBC
                aes.Padding = PaddingMode.PKCS7

                Using encryptor As ICryptoTransform = aes.CreateEncryptor()
                    Dim plainBytes As Byte() = Encoding.UTF8.GetBytes(plainText)
                    Dim encryptedBytes As Byte() = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length)
                    Return Convert.ToBase64String(encryptedBytes)
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception($"Encryption failed: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Decrypts an encrypted string using AES-256 decryption
    ''' </summary>
    ''' <param name="encryptedText">Base64-encoded encrypted string</param>
    ''' <returns>Decrypted plain text string</returns>
    Public Shared Function Decrypt(encryptedText As String) As String
        If String.IsNullOrEmpty(encryptedText) Then
            Return String.Empty
        End If

        Try
            Using aes As Aes = Aes.Create()
                aes.Key = GetKey()
                aes.IV = GetIV()
                aes.Mode = CipherMode.CBC
                aes.Padding = PaddingMode.PKCS7

                Using decryptor As ICryptoTransform = aes.CreateDecryptor()
                    Dim encryptedBytes As Byte() = Convert.FromBase64String(encryptedText)
                    Dim decryptedBytes As Byte() = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length)
                    Return Encoding.UTF8.GetString(decryptedBytes)
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception($"Decryption failed: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Converts the encryption key to a 32-byte array for AES-256
    ''' </summary>
    Private Shared Function GetKey() As Byte()
        Dim keyBytes As Byte() = Encoding.UTF8.GetBytes(EncryptionKey)
        
        ' Ensure key is exactly 32 bytes for AES-256
        Using sha256 As SHA256 = SHA256.Create()
            Return sha256.ComputeHash(keyBytes)
        End Using
    End Function

    ''' <summary>
    ''' Converts the IV to a 16-byte array for AES
    ''' </summary>
    Private Shared Function GetIV() As Byte()
        Return Encoding.UTF8.GetBytes(IV.Substring(0, 16))
    End Function
End Class
