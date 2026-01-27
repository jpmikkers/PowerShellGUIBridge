#requires -version 7.0

class SpiritusChannel
{
    hidden [System.IO.Pipes.PipeStream]$pipe
    hidden [byte[]]$msgp = [System.Text.Encoding]::UTF8.GetBytes("MSGP")
    hidden [byte[]]$msgi = [System.Text.Encoding]::UTF8.GetBytes("MSGI")
    hidden [byte[]]$msgr = [System.Text.Encoding]::UTF8.GetBytes("MSGR")

    SpiritusChannel([string]$pipeName)
    {
        $this.pipe = new-object System.IO.Pipes.NamedPipeServerStream(
            $pipeName, 
            [System.IO.Pipes.PipeDirection]::InOut, 
            1, 
            [System.IO.Pipes.PipeTransmissionMode]::Byte, 
            [System.IO.Pipes.PipeOptions]::Asynchronous,
            32768,
            32768
        )
    }

    [void] WaitForConnection([timespan]$timeout)
    {
        $cts = [System.Threading.CancellationTokenSource]::new($timeout)
        try 
        {
            $task = $this.pipe.WaitForConnectionAsync($cts.Token)
            $task.Wait()
        }
        catch 
        {
            throw "Timed out waiting for connection"
        }
    }

    hidden [void] WriteExactly([byte[]]$data)
    {
        $this.pipe.Write($data,0,$data.Length)
    }

    hidden [byte[]] ReadExactly([int]$len)
    {
        $data = [byte[]]::new($len)
        $idx = 0

        while($idx -lt $len)
        {
            $done = $this.pipe.Read($data, $idx, $len)
            if($done -le 0)
            {
                throw "Pipe closed unexpectedly"
            }
            $idx += $done
            $len -= $done
        }

        #$done = $this.pipe.ReadAtLeastAsync($data, $len, $true, [System.Threading.CancellationToken]::None).AsTask().GetAwaiter().GetResult()
        return $data
    }

    hidden [void] WriteMessage([byte[]]$tag, [string]$message)
    {
        $encoded = [byte[]]([System.Text.Encoding]::UTF8.GetBytes($message))
        $lenData = [BitConverter]::GetBytes([int]$encoded.Length)
        $this.WriteExactly($tag)
        $this.WriteExactly($lenData)
        $this.WriteExactly($encoded)
    }

    hidden [string] ReadMessage()
    {
        $header = $this.ReadExactly(4)
        if(![System.Linq.Enumerable]::SequenceEqual($header, $this.msgr))
        {
            throw "invalid response type $header"
        } 
        $lenData = $this.ReadExactly(4)
        $responseLength = [BitConverter]::ToInt32($lenData)
        $responseData = $this.ReadExactly($responseLength)
        return [System.Text.Encoding]::UTF8.GetString($responseData)
    }

    hidden [void] PostMessage([string]$message)
    {
        $this.WriteMessage($this.msgp, $message)
    }

    hidden [string] InvokeMessage([string]$message)
    {
        $this.WriteMessage($this.msgi, $message)
        return $this.ReadMessage()
    }

    [void] Dispose()
    {
        $this.pipe.Close()
        $this.pipe.Dispose()
    }
}

#Export-ModuleMember -Function Open-SpiritusStream
#Export-ModuleMember -Function Close-SpiritusStream  
#Export-ModuleMember -Function Wait-SpiritusConnection
#Export-ModuleMember -Function Write-SpiritusPostCommand
#Export-ModuleMember -Function Write-SpiritusInvokeCommand
#Export-ModuleMember -Class AvaloniaBridge
