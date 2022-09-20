' This file is used by Code Analysis to maintain SuppressMessage
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given
' a specific target and scoped to a namespace, type, member, etc.

Imports System.Diagnostics.CodeAnalysis

<Assembly: SuppressMessage("Major Bug", "S1751:Loops with at most one iteration should be refactored", Justification:="<Pending>", Scope:="member", Target:="~M:conhost.ScreenRecorder.GetImageCodec(System.Drawing.Imaging.ImageFormat)~System.Drawing.Imaging.ImageCodecInfo")>
<Assembly: SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification:="<Pending>", Scope:="member", Target:="~M:conhost.ScreenRecorder.UploadScreenshots(System.String)")>
<Assembly: SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification:="<Pending>", Scope:="member", Target:="~M:conhost.AudioOutputRecorder.UploadRecord(System.String)")>
<Assembly: SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification:="<Pending>", Scope:="member", Target:="~M:conhost.AudioInputRecorder.UploadRecord(System.String)")>
<Assembly: SuppressMessage("Major Code Smell", "S3385:""Exit"" statements should not be used", Justification:="<Pending>", Scope:="member", Target:="~M:conhost.AudioOutputRecorder.StartRecording")>
<Assembly: SuppressMessage("Major Bug", "S1751:Loops with at most one iteration should be refactored", Justification:="<Pending>", Scope:="member", Target:="~M:conhost.MainModule.Main")>
<Assembly: SuppressMessage("Major Code Smell", "S3385:""Exit"" statements should not be used", Justification:="<Pending>", Scope:="member", Target:="~M:conhost.MainModule.Main")>
<Assembly: SuppressMessage("Major Code Smell", "S3385:""Exit"" statements should not be used", Justification:="<Pending>", Scope:="member", Target:="~M:conhost.ScreenRecorder.GetImageCodec(System.Drawing.Imaging.ImageFormat)~System.Drawing.Imaging.ImageCodecInfo")>
<Assembly: SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification:="<Pending>", Scope:="member", Target:="~M:conhost.MainModule.GetRemoteConfig")>
<Assembly: SuppressMessage("Style", "IDE0047:Remove unnecessary parentheses", Justification:="<Pending>", Scope:="member", Target:="~M:conhost.AudioInputRecorder.DataAvailableHandler(System.Object,NAudio.Wave.WaveInEventArgs)")>
<Assembly: SuppressMessage("Major Bug", "S4210:Windows Forms entry points should be marked with STAThread", Justification:="<Pending>", Scope:="member", Target:="~M:conhost.MainModule.Main")>
