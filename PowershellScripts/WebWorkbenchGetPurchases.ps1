<#
.SYNOPSIS
    Dummy call to Workbench API
.DESCRIPTION
    This script calls Company stub API method to check connectivity
.INPUTS
    Web Address and Authorization key
.OUTPUTS
    True/False connection avalibility
.EXAMPLE
    .\WebWorkbenchGetPurchasesApi.ps1
.LINK
#>

#Use this method to find a relative path. When you run powershell script from the Windows Scheduler or as Administrator you can have a different working directory.  
function Get-ScriptDirectory
{
    Split-Path $script:MyInvocation.MyCommand.Path
}

#Check if you have admin permissin for Application log events. 
$AdministratorCheck = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
$ScriptPath = $myInvocation.MyCommand.Definition

#Using Web Workbench sandbox solution with basic authentication 
$Settings = @{
	BaseUrl = "https://web.workbench.co.nz/WorkbenchV4";
	Token = "Basic ZGVtbzE6dGVzdA==";
}

#hardcoded HTTP slugs
$WBGET = "GET"
$WBPUT = "PUT"
$WBPOST = "POST"

#converts PSObjkect to URI string
function ObjectToUri ($data) {
    if($data){
        $str = ""
        $data.Keys | foreach { $str = "$($str)$($_)=$($data[$_])&" }
        $str = $str.Substring(0, $str.Length - 1)
        return $str
    } else {
        return ""
    }
}

#overrides a value in PSObject by name
function SetPropertyValue($prop, $name, $val){
    $prop | Add-Member -Name $name -Value $val -MemberType NoteProperty -Force
}

#creates a record in Event log, really usefull for background scheduled tasks
function EventLogSafe($message){
    if($AdministratorCheck) {
        Write-EventLog -LogName "Application" -Source "Web Workbench" -EntryType "Warning" -EventID 1 -Message $message;
    }
}

#invoke REST JSON API. This could be your system or Web Workbench
function InvokeApi($path, $action, $data) {

    $resource = "$($Settings.BaseUrl)$($path)"
    $result = ""
    $header = @{ "Authorization" = $Settings.Token; "Content-Type" = "application/json; charset=utf-8" }
    
    if($action -eq $WBGET){
        if($data) {
            $resource = "$($resource)?$(ObjectToUri($data))"
        }
        $result = Invoke-RestMethod -Method Get -Uri $resource -Header $header
    } elseif ($action -eq $WBPOST){
        $jsonData = $data | ConvertTo-Json -Depth 10
        $jsonDataUtf = [system.Text.Encoding]::UTF8.GetString([system.Text.Encoding]::ASCII.GetBytes($jsonData));
        $result = Invoke-RestMethod -Method Post -Uri $resource -Header $header -Body $jsonDataUtf
    } elseif ($action -eq $WBPUT){
        $jsonData = $data | ConvertTo-Json -Depth 10
        $jsonDataUtf = [system.Text.Encoding]::UTF8.GetString([system.Text.Encoding]::ASCII.GetBytes($jsonData));
        $result = Invoke-RestMethod -Method Put -Uri $resource -Header $header -Body $jsonDataUtf
    } else {
        Throw "This action is not supported: $($action)"
    }

    return $result
}

#Make a call to PO list api and fileter POs by date
$purchaseOrders = InvokeApi -path "/api/PurchaseOrderListApi" -action $WBPOST -data @{
        "predicate" = @{
            "PredicateRows" = @(
                @{
                    "LeftOperand" = "UpdatedDate";
                    "Operator" = "Ge";
                    "RightOperand" = @( "2021-01-01" );
                    "Display" = $True
                }
            )
        };
        "sidx" = "UpdatedDate"; #sort result by updated date
        "sord" = "desc";
        "page" = 1; #get first page only
        "rows" = 10; #total number of rows
    }


if($purchaseOrders) {
	Write-Host "Retrieving firs PO only"

    $purchaseOrder = InvokeApi -path "/api/PurchaseOrderDetailApi" -action $WBGET -data @{
        "PONumber" = $purchaseOrders.rows[0].PONumber.CellValue
    }

    Write-Host "Save PO as XML or make API call to other system"

} else {
    Write-Host "There is no new purchase orders"
}
