''' <summary>
''' Model class representing MySQL server configuration
''' Used for storing and loading server connection settings
''' </summary>
Public Class DatabaseConfig
    ''' <summary>
    ''' Primary MySQL server IP address or hostname
    ''' </summary>
    Public Property ServerIP As String

    ''' <summary>
    ''' Backup MySQL server IP address or hostname (used for failover)
    ''' </summary>
    Public Property BackupServerIP As String

    ''' <summary>
    ''' Name of the MySQL database to connect to
    ''' </summary>
    Public Property DatabaseName As String

    ''' <summary>
    ''' MySQL username for authentication
    ''' </summary>
    Public Property Username As String

    ''' <summary>
    ''' MySQL password (stored encrypted in config.json, decrypted in memory)
    ''' </summary>
    Public Property Password As String

    ''' <summary>
    ''' MySQL server port (default: 3306)
    ''' </summary>
    Public Property Port As String

    ''' <summary>
    ''' Default constructor with standard MySQL port
    ''' </summary>
    Public Sub New()
        Port = "3306"
        ServerIP = ""
        BackupServerIP = ""
        DatabaseName = ""
        Username = ""
        Password = ""
    End Sub

    ''' <summary>
    ''' Validates that all required fields are populated
    ''' </summary>
    ''' <returns>True if configuration is valid, False otherwise</returns>
    Public Function IsValid() As Boolean
        Return Not String.IsNullOrWhiteSpace(ServerIP) AndAlso
               Not String.IsNullOrWhiteSpace(DatabaseName) AndAlso
               Not String.IsNullOrWhiteSpace(Username) AndAlso
               Not String.IsNullOrWhiteSpace(Port)
    End Function

    ''' <summary>
    ''' Returns a user-friendly description of the configuration
    ''' </summary>
    Public Overrides Function ToString() As String
        Return $"Server: {ServerIP}:{Port}, Database: {DatabaseName}, User: {Username}"
    End Function
End Class
