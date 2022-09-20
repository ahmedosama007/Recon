#Region "References"

Imports System.Net

#End Region

Friend Class WebClientEx

    Inherits WebClient

#Region "Overridden Methods"

    Protected Overrides Function GetWebRequest(address As Uri) As WebRequest

        Dim wr As WebRequest = MyBase.GetWebRequest(address)
        wr.Timeout = 120000
        Return wr

    End Function

#End Region

End Class