Imports System.Text
Imports System.Drawing.Printing

Public Class ReceiptPreviewForm
    Public Property OrderID As Integer
    Public Property OrderType As String ' "OnlineOrder" or "Reservation"
    Public Property ReceiptText As String

    Public Sub New(id As Integer, type As String, content As String)
        InitializeComponent()
        OrderID = id
        OrderType = type
        ReceiptText = content
        rtbPreview.Text = content
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Close()
    End Sub

    Private Sub btnGenerate_Click(sender As Object, e As EventArgs) Handles btnGenerate.Click
        ' Logic to actually "Generate" the receipt (e.g., Save to DB, Print)
        ' For now, we simulate success
        Try
            ' TODO: Call ReceiptRepository to save official receipt record if not exists
            
            Dim pd As New PrintDocument()
            AddHandler pd.PrintPage, AddressOf PrintPageHandler
            
            Dim printDialog As New PrintDialog()
            printDialog.Document = pd
            
            If printDialog.ShowDialog() = DialogResult.OK Then
                pd.Print()
                MessageBox.Show("Receipt generated and sent to printer.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Me.DialogResult = DialogResult.OK
                Me.Close()
            End If
        Catch ex As Exception
            MessageBox.Show($"Error generating receipt: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub PrintPageHandler(sender As Object, e As PrintPageEventArgs)
        Dim font As New Font("Consolas", 10)
        Dim leftMargin As Single = e.MarginBounds.Left
        Dim topMargin As Single = e.MarginBounds.Top
        Dim printAreaHeight As Single = e.MarginBounds.Height
        
        e.Graphics.DrawString(ReceiptText, font, Brushes.Black, leftMargin, topMargin)
    End Sub
End Class
