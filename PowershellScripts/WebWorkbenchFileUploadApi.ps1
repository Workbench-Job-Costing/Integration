<#
.SYNOPSIS
    Upload file to Workbench API
.DESCRIPTION
    This script make a call to Workbench API and uploads a file
.INPUTS
    Web Address and Authorization key
.OUTPUTS
    True/False file upload
.EXAMPLE
    .\WebWorkbenchUploadFileApi.ps1
.LINK
#>

#Using Web Workbench sandbox solution with basic authentication 
$Settings = @{
	BaseUrl = "https://web.workbench.co.nz/WorkbenchV4";
	Token = "Basic ZGVtbzE6dGVzdA==";
}

#This is in case if you use auto-gen HTTPS certificates on your site.
add-type @"
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    public class TrustAllCertsPolicy : ICertificatePolicy {
        public bool CheckValidationResult(
            ServicePoint srvPoint, X509Certificate certificate,
            WebRequest request, int certificateProblem) {
            return true;
        }
    }
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy


$FilePath = 'Dummy.txt';
$Uri = "$($Settings.BaseUrl)/api/UploadFileApi?fileName=Dummy1.txt&relatedRecordId=701&relatedRecordType=LogHeader";

$FileBytes = [System.IO.File]::ReadAllBytes($FilePath);
$FileEnc = [System.Text.Encoding]::GetEncoding('UTF-8').GetString($FileBytes);
$Boundary = [System.Guid]::NewGuid().ToString(); 
$LF = "`r`n";

$Headers = @{ "Authorization" = $Settings.Token; "Content-Type" = "multipart/form-data; boundary=`"$Boundary`""}
$BodyLines = ( 
    "--$Boundary",
    "Content-Disposition: form-data; name=`"file`"; filename=`"Dummy.txt`"",
    "Content-Type: application/octet-stream$LF",
    $FileEnc,
    "--$Boundary--$LF" 
) -join $LF

$fileUploadResult = Invoke-RestMethod -Uri $Uri -Method Post -Headers $Headers -Body $BodyLines


if($fileUploadResult.StoredFileId) {
	Write-Host "All good, a file was uploaded."
} else {
    Write-Host "File Upload Test has failed."
}
