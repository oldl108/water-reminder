# 蓝奏云上传脚本（早睡早起多喝水 发版用）
# 用法: .\lanzou-upload.ps1 -Uid <手机号> -Pwd <密码> -Folder 喝水提醒 -FilePath <zip路径>
# 凭据只经参数传入，不存盘。走蓝奏云网页版的移动端登录接口（mlogin.php，无验证码）。
param(
    [Parameter(Mandatory)][string]$Uid,
    [Parameter(Mandatory)][string]$Pwd,
    [Parameter(Mandatory)][string]$Folder,
    [Parameter(Mandatory)][string]$FilePath
)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

$handler = New-Object System.Net.Http.HttpClientHandler
$handler.CookieContainer = New-Object System.Net.CookieContainer
$handler.UseCookies = $true
$handler.AutomaticDecompression = [Net.DecompressionMethods]::GZip -bor [Net.DecompressionMethods]::Deflate
$client = New-Object System.Net.Http.HttpClient($handler)
$client.Timeout = [TimeSpan]::FromMinutes(15)
$client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36")
$client.DefaultRequestHeaders.Referrer = "https://up.woozooo.com/"

function Post-Form([string]$url, [hashtable]$fields) {
    $pairs = New-Object 'System.Collections.Generic.List[System.Collections.Generic.KeyValuePair[string,string]]'
    foreach ($k in $fields.Keys) {
        $pairs.Add([System.Collections.Generic.KeyValuePair[string,string]]::new($k, [string]$fields[$k]))
    }
    $content = New-Object System.Net.Http.FormUrlEncodedContent -ArgumentList (, $pairs)
    $resp = $client.PostAsync($url, $content).Result
    $resp.Content.ReadAsStringAsync().Result
}

# 1) 登录（移动端接口，无滑块验证码）
$loginRaw = Post-Form "https://up.woozooo.com/mlogin.php" @{
    task = "3"; uid = $Uid; pwd = $Pwd
    setSessionId = ""; setSig = ""; setScene = ""; setTocen = ""
}
if ($loginRaw -notmatch '^\s*\{') { throw "登录被反爬拦截（返回 JS 挑战），需用真实浏览器登录态。原始响应开头: $($loginRaw.Substring(0,80))" }
$login = $loginRaw | ConvertFrom-Json
if ($login.zt -ne 1) { throw "登录失败: $loginRaw" }
Write-Output "LOGIN OK (id=$($login.id))"

# 2) 找目标文件夹 id
$folRaw = Post-Form "https://up.woozooo.com/doupload.php" @{ task = "47"; folder_id = "-1" }
$fol = $folRaw | ConvertFrom-Json
$folderId = $null
foreach ($arr in @($fol.info, $fol.text)) {
    if ($null -eq $arr) { continue }
    foreach ($f in $arr) {
        $name = if ($f.PSObject.Properties["name"]) { $f.name } elseif ($f.PSObject.Properties["folder_name"]) { $f.folder_name } else { $null }
        $id = if ($f.PSObject.Properties["folder_id"]) { $f.folder_id } elseif ($f.PSObject.Properties["fol_id"]) { $f.fol_id } else { $null }
        if ($name -eq $Folder -and $id) { $folderId = $id }
    }
}
if (-not $folderId) { throw "找不到文件夹「$Folder」，接口返回: $folRaw" }
Write-Output "FOLDER OK ($Folder -> id=$folderId)"

# 3) 上传（multipart，免费账户单文件上限 100MB）
$fileName = [IO.Path]::GetFileName($FilePath)
$mp = New-Object System.Net.Http.MultipartFormDataContent
foreach ($kv in @(
    @("task", "1"), @("vie", "2"), @("ve", "2"),
    @("id", "WU_FILE_0"), @("name", $fileName),
    @("folder_id_bb_n", "$folderId")
)) {
    $sc = New-Object System.Net.Http.StringContent($kv[1])
    $mp.Add($sc, $kv[0])
}
$fs = [IO.File]::OpenRead($FilePath)
try {
    $fc = New-Object System.Net.Http.StreamContent($fs)
    $fc.Headers.ContentType = "application/octet-stream"
    $mp.Add($fc, "upload_file", $fileName)
    $upResp = $client.PostAsync("https://up.woozooo.com/html5up.php", $mp).Result
    $upRaw = $upResp.Content.ReadAsStringAsync().Result
}
finally { $fs.Dispose() }
$up = $upRaw | ConvertFrom-Json
if ($up.zt -ne 1) { throw "上传失败: $upRaw" }
Write-Output "UPLOAD OK: $fileName"
if ($up.text) { Write-Output ("share: https://gg999.lanzouv.com/" + $up.text[0].f_id) }
