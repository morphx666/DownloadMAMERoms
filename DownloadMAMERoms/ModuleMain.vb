Imports System.Net
Imports System.Threading
Imports HtmlAgilityPack

Module MainModule
    ' \\Media-center\d\Emulators\mame\roms
    Private files() As String
    Private dstFolder As String
    Private dstFile As String
    Private romsList As New Dictionary(Of Char, List(Of ROM))
    Private httpClient As New WebClient()
    Private waitEvent As AutoResetEvent = New AutoResetEvent(False)
    Private dlRom As ROM
    Private userSections As New List(Of String)
    Private sw As New Stopwatch()
    Private syncObj As New Object()
    Private abortThreads As Boolean

    ' http://www.emuparadise.me/roms/get-download.php?gid=10944&token=fe1cbde50e0d9859193c2fbb06944a3c&mirror_available=true
    ' http://50.7.136.26/happyxhJ1ACmlTrxJQpol71nBc/MAME/roms/88games.zip
    ' http://50.7.161.122/happyxhJ1ACmlTrxJQpol71nBc/MAME/roms/j6gforce.zip

    Public Const baseURL = "http://www.emuparadise.me"

    Sub Main(args() As String)
        Console.Title = "Download MAME ROMs"

        If args.Length < 1 OrElse args.Length > 2 Then
            ShowUsage()
        Else
            If args.Length = 1 Then
                dstFolder = args(0)
            Else
                For i As Integer = 0 To args(0).Length - 1
                    userSections.Add(args(0)(i))
                Next
                dstFolder = args(1)
            End If

            If Not IO.Directory.Exists(dstFolder) Then
                ShowUsage("Invalid destination folder.")
                Exit Sub
            End If

            Try
                Dim monitorThread As Thread = New Thread(AddressOf MonitorSub)
                monitorThread.Start()

                Dim downloadThread As Thread = New Thread(AddressOf DownloadSub)
                downloadThread.Start()
            Catch ex As Exception
                ShowUsage(ex.Message)
            End Try
        End If
    End Sub

    Private Sub ShowUsage(Optional errorMessage As String = "")
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("Download MAME ROMs v" + My.Application.Info.Version.ToString())
        Console.ForegroundColor = ConsoleColor.Gray
        Console.WriteLine()

        If errorMessage <> "" Then
            Console.ForegroundColor = ConsoleColor.Red
            Console.WriteLine("ERROR: " + errorMessage)
            Console.ForegroundColor = ConsoleColor.Gray
            Console.WriteLine()
        End If

        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("    Usage:")
        Console.ForegroundColor = ConsoleColor.Gray
        Console.WriteLine("    dmr [Sections] DestinationFolder")
        Console.WriteLine()
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("    Examples:")
        Console.ForegroundColor = ConsoleColor.Gray
        Console.WriteLine("     1) Download all to c:\mame\roms")
        Console.ForegroundColor = ConsoleColor.Green
        Console.WriteLine("    dmr c:\mame\roms")
        Console.ForegroundColor = ConsoleColor.Gray
        Console.WriteLine()
        Console.WriteLine("     2) Download roms from sections 'Numbers', 'F' and 'G' to e:\emulators\mame\roms")
        Console.ForegroundColor = ConsoleColor.Green
        Console.WriteLine("    dmr #FG c:\emulators\mame\roms")
        Console.ForegroundColor = ConsoleColor.Gray
    End Sub

    Private Sub PrintHeader(txt As String)
        Console.Clear()
        Dim padding As String = New String(" "c, ((Console.WindowWidth) - txt.Length) / 2)
        Console.BackgroundColor = ConsoleColor.Blue
        Console.ForegroundColor = ConsoleColor.White
        Console.Write("{0}{1}{2}", padding, txt, padding)
        Console.BackgroundColor = ConsoleColor.Black
        Console.ForegroundColor = ConsoleColor.Gray
    End Sub

    Private Sub DownloadSub()
        Dim blankLine As String = StrDup(Console.WindowWidth - 1, " ")

        Dim SizeIsEqual = Function(fileSize As Long, romSize As Long) As Boolean
                              If romSize >= 1024 * 1024 Then
                                  Return Math.Abs(fileSize - romSize) < 1 * 1024 * 1024
                              Else
                                  Return Math.Abs(fileSize - romSize) < 4 * 1024
                              End If
                          End Function

        Dim ClearLine = Sub()
                            Console.CursorLeft = 0
                            Console.CursorTop = 1
                            Console.Write(blankLine)
                            Console.CursorLeft = 0
                            Console.CursorTop = 1
                        End Sub

        Dim sections As New List(Of String)
        If userSections.Count = 0 OrElse userSections.Contains("#") Then sections.Add("#")
        For i = Asc("A") To Asc("Z")
            If userSections.Count = 0 OrElse userSections.Contains(Chr(i)) Then sections.Add(Chr(i))
        Next

        Console.CursorVisible = False

        For Each section In sections
            If userSections.Count > 0 AndAlso Not userSections.Contains(section) Then Continue For
            Dim romsList = GetROMsList(section).Where(Function(r) r.DownloadURL <> "" AndAlso r.State = ROM.States.Ready)

            PrintHeader($"ROMs for section '{section}'")

            Dim n As Integer = 0
            For Each rom In romsList
                While rom.State = ROM.States.Downloading
                    Thread.Sleep(100)
                End While

                Try
                    sw.Reset()
                    dstFile = GetDestinationFileName(rom)
                    dlRom = rom
                    waitEvent.Set()

                    ClearLine()

                    If IO.File.Exists(dstFile) AndAlso SizeIsEqual(My.Computer.FileSystem.GetFileInfo(dstFile).Length, rom.Size) Then
                        SyncLock syncObj
                            Console.ForegroundColor = ConsoleColor.Green
                            Console.Write("Skipped    ")
                            Console.ForegroundColor = ConsoleColor.Gray
                            Thread.Sleep(250)
                        End SyncLock
                    ElseIf rom.Size = 0 Then
                        SyncLock syncObj
                            Console.ForegroundColor = ConsoleColor.DarkYellow
                            Console.Write("Invalid    ")
                            Console.ForegroundColor = ConsoleColor.Gray
                            Thread.Sleep(250)
                        End SyncLock
                    Else
                        SyncLock syncObj
                            Console.WriteLine("Downloading [{0}%]: {1}", (n / romsList.Count * 100).ToString("N0").PadLeft(3),
                                  rom.Title.Substring(0, Math.Min(rom.Title.Length, Console.WindowWidth - 30 - 20 - 1)))
                        End SyncLock
                        sw.Restart()
                        httpClient.DownloadFile("http://50.7.161.234/998ajxYxajs13jAKhdca/MAME/roms/" + rom.DownloadURL + ".zip", dstFile)
                    End If
                    dstFile = ""
                    waitEvent.WaitOne()
                Catch ex As Exception
                    SyncLock syncObj
                        ClearLine()

                        Console.ForegroundColor = ConsoleColor.Red
                        Console.WriteLine($"Download Error: {rom.Title} -> {ex.Message}")
                        If ex.InnerException IsNot Nothing Then Debug.WriteLine(ex.InnerException.Message)
                        Console.ForegroundColor = ConsoleColor.Gray

                        Thread.Sleep(3000)
                    End SyncLock
                End Try

                sw.Stop()
                n += 1
            Next
        Next

        Console.CursorVisible = True

        waitEvent.Set()
        abortThreads = True
        httpClient.Dispose()
    End Sub

    Private Function GetDestinationFileName(rom As ROM) As String
        Return IO.Path.Combine(dstFolder, rom.DownloadURL + ".Zip")
    End Function

    Private Sub MonitorSub()
        Do
            waitEvent.WaitOne()
            If abortThreads Then Exit Do

            Dim currentFileName = dstFile
            Dim currentROM = dlRom

            Do
                SyncLock syncObj
                    Console.CursorLeft = 0
                    Console.CursorTop = 1
                    PrintFileSize(currentFileName, currentROM)
                    Console.CursorLeft = 0
                    Console.CursorTop = 1
                End SyncLock
                waitEvent.WaitOne(100)
            Loop Until dstFile = ""

            waitEvent.Set()
        Loop
    End Sub

    Private Sub PrintFileSize(currentFileName As String, currentROM As ROM)
        If IO.File.Exists(currentFileName) Then
            Try
                Dim size As Double = My.Computer.FileSystem.GetFileInfo(currentFileName).Length
                Dim sizeStr As String = String.Format("{0} ({1}/s)", FormatFileSize(size),
                                                      FormatFileSize(size / (sw.ElapsedMilliseconds / 1000)))

                Console.BackgroundColor = ConsoleColor.Black
                Console.CursorLeft = Console.WindowWidth - 30 - 1
                Console.Write("|" + StrDup(28, " "))
                Console.CursorLeft = Console.WindowWidth - (sizeStr.Length + 2)
                Console.Write(sizeStr)

                If currentROM IsNot Nothing Then
                    Dim s = Console.WindowWidth - 30 - 20 - 1
                    Dim mw As Integer = Math.Min(s, size / currentROM.Size * s)
                    Dim title As String = currentROM.Title.Substring(0, Math.Min(mw, currentROM.Title.Length))

                    Console.BackgroundColor = ConsoleColor.DarkBlue
                    For x As Integer = 0 To mw - 1
                        Console.CursorLeft = x + 20
                        Console.Write(If(x < title.Length, currentROM.Title.Substring(x, 1), " "))
                    Next

                    Console.BackgroundColor = ConsoleColor.Black
                    For x As Integer = mw To s - 1
                        Console.CursorLeft = x + 20
                        Console.Write(If(x  < currentROM.Title.Length, currentROM.Title.Substring(x, 1), " "))
                    Next
                End If
            Catch ex As Exception
                Console.BackgroundColor = ConsoleColor.Black
            End Try
        End If
    End Sub

    Private Function FormatFileSize(size As Double) As String
        Dim unit As String = "B "
        Select Case size
            Case Is <= 1024 * 1024
                unit = "KB"
                size /= 1024
            Case Is <= 1024 * 1024 * 1024
                unit = "MB"
                size /= 1024 * 1024
            Case Else
                unit = "GB"
                size /= 1024 * 1024 * 1024
        End Select

        Return String.Format("{0:N2} {1}", size, unit)
    End Function

    Private Function GetROMsList(initial As String) As List(Of ROM)
        If romsList.ContainsKey(initial) Then Return romsList(initial)

        PrintHeader(String.Format("Processing section '{0}'", initial))

        Const maxConnections As Integer = 10
        Dim romList As New List(Of ROM)
        Dim htmlDoc As New HtmlDocument()
        Try
            htmlDoc.LoadHtml(httpClient.DownloadString(CombinePath(baseURL, String.Format("/M.A.M.E._-_Multiple_Arcade_Machine_Emulator_ROMs/Games-Starting-With-{0}/7", If(initial = "#", "Numbers", initial)))))
        Catch ex As Exception
            Console.CursorTop = 1
            Console.WriteLine("Section '{0}' Error: {1}", initial, ex.Message)
            Thread.Sleep(500)
            Return romList
        End Try

        Dim lastTxt As String = ""
        Dim entries = htmlDoc.DocumentNode.SelectNodes("//a[@class='index gamelist']")
        Dim RomsReady = Function() romList.Where(Function(r) r.State <> ROM.States.Downloading).Count
        Dim PrintStatus = Sub()
                              Dim rr As Integer = RomsReady()
                              Dim txt As String = String.Format("Preparing ROMs list [{0}% | {1:N0} ROMs | {2}]".PadRight(Console.WindowWidth - 1, " "),
                                                (rr / entries.Count * 100).ToString("N0").PadLeft(3),
                                                rr,
                                                FormatFileSize(romList.Sum(Function(r) r.Size)))
                              If lastTxt <> txt Then
                                  Console.CursorTop = 1
                                  Console.CursorLeft = 0
                                  Console.Write(txt)
                                  lastTxt = txt
                              End If
                          End Sub

        PrintStatus()
        For i As Integer = 0 To entries.Count - 1
            romList.Add(New ROM(entries(i).InnerText, CombinePath(baseURL, entries(i).GetAttributeValue("href", ""))))

            If romList.Count < entries.Count Then
                If i Mod maxConnections = 0 Then
                    Do
                        Thread.Sleep(10)
                        PrintStatus()
                    Loop While RomsReady() < i
                End If
            End If
        Next

        romsList.Add(initial, romList)
        Return romList
    End Function

    Public Function CombinePath(p1 As String, p2 As String) As String
        If p1.Contains("/") Then
            ' TODO: Is there a native .NET function to combine web paths?
            If p2.StartsWith(p1) Then
                Return p2
            Else
                If Not p1.EndsWith("/") Then p1 += "/"
                If p2.StartsWith("/") Then p2 = p2.Substring(1)
                Return p1 + p2
            End If
        Else
            Return IO.Path.Combine(p1, p2)
        End If
    End Function
End Module
