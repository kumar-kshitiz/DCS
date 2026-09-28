$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
& (Join-Path $framework 'MSBuild.exe') (Join-Path $projectRoot 'RemoteFileManagement.sln') /p:Configuration=Release /verbosity:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
$run = Join-Path $PSScriptRoot ('Run-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run) | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'RemoteFileManagement.Server\bin\Release\RemoteFileManagement.Server.exe'), (Join-Path $projectRoot 'RemoteFileManagement.Client\bin\Release\RemoteFileManagement.Client.exe'), (Join-Path $projectRoot 'RemoteFileManagement.Shared\bin\Release\RemoteFileManagement.Shared.dll') -Destination $run
Copy-Item -LiteralPath (Join-Path $projectRoot 'RemoteFileManagement.Server\Server.config') -Destination $run
Copy-Item -LiteralPath (Join-Path $projectRoot 'RemoteFileManagement.Client\Web') -Destination $run -Recurse
& (Join-Path $framework 'csc.exe') /nologo /out:"$run\IntegrationTests.exe" /r:"$run\RemoteFileManagement.Server.exe" /r:"$run\RemoteFileManagement.Client.exe" /r:"$run\RemoteFileManagement.Shared.dll" /r:System.Runtime.Remoting.dll /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll (Join-Path $PSScriptRoot 'IntegrationTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
$process = Start-Process -FilePath "$run\IntegrationTests.exe" -WindowStyle Hidden -PassThru -RedirectStandardOutput "$run\results.log" -RedirectStandardError "$run\errors.log"
$process.Handle | Out-Null
if (!$process.WaitForExit(60000)) { $process.Kill(); throw "Tests timed out. Results: $run" }
Get-Content -LiteralPath "$run\results.log" -Tail 10
Get-Content -LiteralPath "$run\errors.log"
if ($process.ExitCode -ne 0) { throw "Integration tests failed. Results: $run" }
Write-Output "Test artifacts: $run"