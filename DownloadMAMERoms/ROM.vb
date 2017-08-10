Imports System.Net
Imports HtmlAgilityPack

Public Class ROM
    Public Enum States
        Ready
        Downloading
        Failed
    End Enum

    Private mHomeURL As String
    Private mDownloadURL As String
    Private mTitle As String
    Private mState As States = States.Downloading
    Private mSize As Long

    Public Sub New(title As String, homeURL As String)
        mTitle = title
        mHomeURL = homeURL

        Dim httpClient As New WebClient()
        Dim htmlDoc As New HtmlDocument()

        AddHandler httpClient.DownloadStringCompleted, Sub(sender As Object, e As DownloadStringCompletedEventArgs)
                                                           If e.Cancelled OrElse e.Error IsNot Nothing Then
                                                               mState = States.Failed
                                                           Else
                                                               Try
                                                                   htmlDoc.LoadHtml(e.Result)

                                                                   Dim entries = htmlDoc.DocumentNode.SelectNodes("//div[@class='download-link']/a")
                                                                   mTitle = entries(0).InnerText.Substring(entries(0).InnerText.LastIndexOf("(") + 1)
                                                                   mTitle = mTitle.Replace(")", "")
                                                                   mDownloadURL = mTitle ' CombinePath(baseURL, entries(0).GetAttributeValue("href", ""))
                                                                   Dim p1 = 10 ' entries(0).InnerText.IndexOf("""")
                                                                   Dim p2 = entries(0).InnerText.LastIndexOf("(")
                                                                   mTitle = entries(0).InnerText.Substring(p1, p2 - p1 - 1)

                                                                   Dim sizeNode = htmlDoc.DocumentNode.SelectNodes("//div[@class='download-link']/text()[1]")
                                                                   Dim tmpSize = sizeNode(0).InnerText.Replace("(", "").Replace(")", "").Trim()
                                                                   Dim mult = tmpSize(tmpSize.Length - 1)
                                                                   tmpSize = tmpSize.Substring(0, tmpSize.Length - 1)

                                                                   Select Case mult
                                                                       Case "K" : mSize = CLng(tmpSize) * 1024
                                                                       Case "M" : mSize = CLng(tmpSize) * 1024 * 1024
                                                                   End Select

                                                                   mState = States.Ready
                                                               Catch ex As Exception
                                                                   mState = States.Failed
                                                               End Try
                                                           End If
                                                       End Sub

        httpClient.DownloadStringAsync(New Uri(homeURL))
    End Sub

    Public ReadOnly Property Size As Double
        Get
            Return mSize
        End Get
    End Property

    Public ReadOnly Property State As States
        Get
            Return mState
        End Get
    End Property

    Public ReadOnly Property HomeURL As String
        Get
            Return mHomeURL
        End Get
    End Property

    Public ReadOnly Property DownloadURL As String
        Get
            Return mDownloadURL
        End Get
    End Property

    Public ReadOnly Property Title As String
        Get
            Return mTitle
        End Get
    End Property
End Class
