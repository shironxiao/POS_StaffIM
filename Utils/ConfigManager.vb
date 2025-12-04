Imports System.IO
Imports Newtonsoft.Json

''' <summary>
''' Manages configuration file operations for database settings
''' Handles loading, saving, and validation of config.json
''' </summary>
Public Class ConfigManager
    Private Shared ReadOnly ConfigFileName As String = "config.json"

    ''' <summary>
    ''' Gets the full path to the config.json file
    ''' Located in the application's executable directory
    ''' </summary>
    Public Shared Function GetConfigPath() As String
        Return Path.Combine(Application.StartupPath, ConfigFileName)
    End Function

    ''' <summary>
    ''' Checks if the config.json file exists
    ''' </summary>
    ''' <returns>True if config file exists, False otherwise</returns>
    Public Shared Function ConfigExists() As Boolean
        Return File.Exists(GetConfigPath())
    End Function

    ''' <summary>
    ''' Loads database configuration from config.json
    ''' Automatically decrypts the password
    ''' </summary>
    ''' <returns>DatabaseConfig object, or Nothing if file doesn't exist or is invalid</returns>
    Public Shared Function LoadConfig() As DatabaseConfig
        Try
            If Not ConfigExists() Then
                Return Nothing
            End If

            Dim configPath As String = GetConfigPath()
            Dim jsonContent As String = File.ReadAllText(configPath)

            If String.IsNullOrWhiteSpace(jsonContent) Then
                Return Nothing
            End If

            ' Deserialize JSON to DatabaseConfig object
            Dim config As DatabaseConfig = JsonConvert.DeserializeObject(Of DatabaseConfig)(jsonContent)

            If config Is Nothing Then
                Return Nothing
            End If

            ' Decrypt password if it exists
            If Not String.IsNullOrEmpty(config.Password) Then
                Try
                    config.Password = EncryptionHelper.Decrypt(config.Password)
                Catch ex As Exception
                    ' If decryption fails, password might be corrupted or in plain text
                    System.Diagnostics.Debug.WriteLine($"Password decryption failed: {ex.Message}")
                    ' Keep the password as-is (might be plain text from old config)
                End Try
            End If

            Return config
        Catch ex As Exception
            MessageBox.Show($"Failed to load configuration: {ex.Message}", "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Saves database configuration to config.json
    ''' Automatically encrypts the password before saving
    ''' </summary>
    ''' <param name="config">DatabaseConfig object to save</param>
    ''' <returns>True if save was successful, False otherwise</returns>
    Public Shared Function SaveConfig(config As DatabaseConfig) As Boolean
        Try
            If config Is Nothing Then
                Throw New ArgumentNullException(NameOf(config), "Configuration cannot be null")
            End If

            If Not config.IsValid() Then
                Throw New InvalidOperationException("Configuration is invalid. Please check all required fields.")
            End If

            ' Create a copy to avoid modifying the original object
            Dim configToSave As New DatabaseConfig() With {
                .ServerIP = config.ServerIP,
                .BackupServerIP = config.BackupServerIP,
                .DatabaseName = config.DatabaseName,
                .Username = config.Username,
                .Port = config.Port,
                .Password = If(String.IsNullOrEmpty(config.Password), "", EncryptionHelper.Encrypt(config.Password))
            }

            ' Serialize to JSON with formatting
            Dim jsonContent As String = JsonConvert.SerializeObject(configToSave, Formatting.Indented)

            ' Save to file
            Dim configPath As String = GetConfigPath()
            File.WriteAllText(configPath, jsonContent)

            Return True
        Catch ex As Exception
            MessageBox.Show($"Failed to save configuration: {ex.Message}", "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Deletes the config.json file
    ''' Useful for resetting configuration
    ''' </summary>
    ''' <returns>True if deletion was successful, False otherwise</returns>
    Public Shared Function DeleteConfig() As Boolean
        Try
            If ConfigExists() Then
                File.Delete(GetConfigPath())
            End If
            Return True
        Catch ex As Exception
            MessageBox.Show($"Failed to delete configuration: {ex.Message}", "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function
End Class
