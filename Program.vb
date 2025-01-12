Imports SteamKit2
Imports System.IO
Imports System.Reflection
Imports System.Security.Cryptography
Imports System.Text

Module Program
    Dim GameDepotFolder As String
    Dim GameFilesFolder As String

    Sub Main(args As String())
        If args.Length = 2 Then
            GameDepotFolder = args(0)
            GameFilesFolder = args(1)
            HandleManifest()
        Else
            Console.WriteLine("Usage: SteamDepotCheck.exe <GameDepotFolder> <GameFilesFolder>")
            Console.WriteLine("")
            Console.WriteLine("Press key to exit!")
            Console.ReadKey()
            Return
        End If
    End Sub

    Private Function GetDepotManifestObject(FilePath As String) As DepotManifest
        Dim DepotManifestFlags = BindingFlags.NonPublic Or BindingFlags.Instance
        Dim DepotManifestFileBytes = File.ReadAllBytes(FilePath)
        Dim DepotManifest = CType(Activator.CreateInstance(GetType(DepotManifest), DepotManifestFlags, Nothing, New Object() {DepotManifestFileBytes}, Globalization.CultureInfo.InvariantCulture), DepotManifest)
        Return DepotManifest
    End Function

    Private Sub HandleManifest()
        Dim DepotManifestDictionary As New Dictionary(Of String, List(Of String))
        Dim OrphanGameFilesList As New List(Of String)
        Dim UnmatchedGameFilesList As New List(Of String)
        For Each CurrentDepotManifestFilePath In Directory.GetFiles(GameDepotFolder, "*.*", SearchOption.AllDirectories)
            If CurrentDepotManifestFilePath.ToLower.EndsWith(".manifest") Then
                Dim CurrentDepotManifest = GetDepotManifestObject(CurrentDepotManifestFilePath)
                For Each CurrentFileFromCurrentDepotManifest As DepotManifest.FileData In CurrentDepotManifest.Files
                    Dim FileName = CurrentFileFromCurrentDepotManifest.FileName.Replace("\", "/")
                    Dim FileHash = CurrentFileFromCurrentDepotManifest.FileHash.Aggregate(New StringBuilder(), Function(sb, v) sb.Append(v.ToString("x2"))).ToString().ToUpper()
                    If DepotManifestDictionary.ContainsKey(FileName) Then
                        DepotManifestDictionary(FileName).Add(FileHash)
                    Else
                        DepotManifestDictionary.Add(FileName, New List(Of String))
                        DepotManifestDictionary(FileName).Add(FileHash)
                    End If
                Next
            End If
        Next
        For Each CurrentGameFilePath In Directory.GetFiles(GameFilesFolder, "*.*", SearchOption.AllDirectories)
            CurrentGameFilePath = CurrentGameFilePath.Replace("\", "/")
            Dim IsolatedGameFilePath = CurrentGameFilePath.Remove(0, CurrentGameFilePath.IndexOf(GameFilesFolder.Replace("\", "/")) + GameFilesFolder.Length)
            If IsolatedGameFilePath.StartsWith("/") Then
                IsolatedGameFilePath = IsolatedGameFilePath.Remove(0, 1)
            End If
            If DepotManifestDictionary.ContainsKey(IsolatedGameFilePath) Then
                If DepotManifestDictionary(IsolatedGameFilePath).Contains(GetFileSHA1(CurrentGameFilePath)) = False Then
                    UnmatchedGameFilesList.Add(IsolatedGameFilePath)
                End If
            Else
                OrphanGameFilesList.Add(IsolatedGameFilePath)
            End If
        Next
        Dim CheckOK As Boolean = True
        If OrphanGameFilesList.Count > 0 OrElse UnmatchedGameFilesList.Count > 0 Then
            Console.WriteLine("")
            Console.WriteLine(">> CHECK FAILED!")
        End If
        If OrphanGameFilesList.Count > 0 Then
            CheckOK = False
            Console.WriteLine("")
            Console.WriteLine("Orphan game files detected: (files not included in manifest)")
            Console.WriteLine("***")
            For Each OrphanGameFile In OrphanGameFilesList
                Console.WriteLine(OrphanGameFile)
            Next
            Console.WriteLine("***")
        End If
        If UnmatchedGameFilesList.Count > 0 Then
            CheckOK = False
            Console.WriteLine("")
            Console.WriteLine("Unmatched game files detected: (files with a hash not included in the manifest)")
            Console.WriteLine("*")
            For Each UnmatchedGameFile In UnmatchedGameFilesList
                Console.WriteLine(UnmatchedGameFile)
            Next
            Console.WriteLine("*")
        End If
        If CheckOK = True Then
            Console.WriteLine("")
            Console.WriteLine(">> CHECK OK!")
            Console.WriteLine("")
            Console.WriteLine("All game files match with manifest files.")
        End If
        Console.WriteLine("")
        Console.WriteLine("Press key to exit!")
        Console.ReadKey()
    End Sub

    Function GetFileSHA1(FilePath As String) As String
        Try
            Using fileStream As FileStream = File.OpenRead(FilePath)
                Using sha1 As SHA1 = SHA1.Create()
                    Dim hashBytes As Byte() = sha1.ComputeHash(fileStream)
                    Dim hashString As New StringBuilder()
                    For Each b As Byte In hashBytes
                        hashString.Append(b.ToString("x2"))
                    Next
                    Return hashString.ToString.ToUpper()
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"Error: {ex.Message}")
            Return Nothing
        End Try
    End Function
End Module