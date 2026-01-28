using module .\SpiritusChannel.psm1

class GUIBridge
{
    hidden [SpiritusChannel]$pipe
    hidden [System.Diagnostics.Process]$process
    hidden [string]$pipeName

    GUIBridge()
    {
        $this.pipeName = "avapipe$([Guid]::NewGuid())"

        if([System.Environment]::Is64BitOperatingSystem -eq $false)
        {
            throw "Only 64-bit OS is supported"
        }

        if($global:IsWindows)
        {
            $exePath = './binaries/win-x64/GuiWorker.exe'
        }
        elseif($global:IsMacOS)
        {
            $exePath = './binaries/osx-x64/GuiWorker'
        }
        elseif($global:IsLinux)
        {
            $exePath = './binaries/linux-x64/GuiWorker'
        }
        else
        {
            throw "Unsupported OS"
        }

        if(-not (Test-Path -Path $exePath -PathType Leaf))
        {
            throw "AvaloniaNamedPipe executable not found at path: $exePath"
        }

        [string]$exePath = Resolve-Path -Path $exePath
        $this.pipe = [SpiritusChannel]::new($this.pipeName)
        $this.process = Start-Process $exePath -WorkingDirectory (Split-Path -Path $exePath -Parent) -PassThru -ArgumentList $this.pipeName
        $this.pipe.WaitForConnection([timespan]::FromSeconds(10))
    }

    [void] Dispose()
    {
        $this.pipe.Dispose()
        if(-not $this.process.HasExited)
        {
            if($false -eq $this.process.WaitForExit([timespan]::FromSeconds(10)))
            {
                # alternative: $this.process.Kill()
                $this.process | Stop-Process -Force
            }
        }
    }

    [void] PostCommand([string]$command, [object]$payload)
    {
        $message = @{
            Command = $command
            Payload = $payload
        }
        $messageJson = $message | ConvertTo-Json -Compress -Depth 10
        $this.pipe.PostMessage($messageJson)
    }

    [object] InvokeCommand([string]$command, [object]$payload)
    {
        $message = @{
            Command = $command
            Payload = $payload
        }
        $messageJson = $message | ConvertTo-Json -Compress -Depth 10
        $responseJson = $this.pipe.InvokeMessage($messageJson)
        return $responseJson | ConvertFrom-Json -Depth 10
    }
}

#Export-ModuleMember -Function Open-SpiritusStream
#Export-ModuleMember -Function Close-SpiritusStream  
#Export-ModuleMember -Function Wait-SpiritusConnection
#Export-ModuleMember -Function Write-SpiritusPostCommand
#Export-ModuleMember -Function Write-SpiritusInvokeCommand
#Export-ModuleMember -Class AvaloniaBridge
