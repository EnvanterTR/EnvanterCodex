Set-StrictMode -Version Latest

function Add-FileToHmac([Security.Cryptography.HMAC]$Hmac, [string]$Path, [long]$Length = -1) {
    $stream = [IO.File]::OpenRead($Path)
    try {
        $buffer = New-Object byte[] 65536
        $remaining = $Length
        while ($Length -lt 0 -or $remaining -gt 0) {
            $wanted = if ($Length -lt 0 -or $remaining -ge $buffer.Length) { $buffer.Length } else { [int]$remaining }
            $read = $stream.Read($buffer, 0, $wanted)
            if ($read -eq 0) { break }
            [void]$Hmac.TransformBlock($buffer, 0, $read, $buffer, 0)
            if ($Length -ge 0) { $remaining -= $read }
        }
        if ($Length -ge 0 -and $remaining -ne 0) { throw "Encrypted archive ended unexpectedly." }
    } finally { $stream.Dispose() }
}

function Protect-Backup([string]$SourceZip, [string]$Destination, [string]$Password, [int]$Rounds,
    [byte[]]$HeaderMagic, [string]$CipherPath) {
    $salt = New-Object byte[] 16
    $iv = New-Object byte[] 16
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($salt); $rng.GetBytes($iv) } finally { $rng.Dispose() }
    $roundBytes = [BitConverter]::GetBytes($Rounds)
    $header = New-Object byte[] ($HeaderMagic.Length + $roundBytes.Length + $salt.Length + $iv.Length)
    [Buffer]::BlockCopy($HeaderMagic,0,$header,0,$HeaderMagic.Length)
    [Buffer]::BlockCopy($roundBytes,0,$header,$HeaderMagic.Length,$roundBytes.Length)
    [Buffer]::BlockCopy($salt,0,$header,$HeaderMagic.Length + $roundBytes.Length,$salt.Length)
    [Buffer]::BlockCopy($iv,0,$header,$HeaderMagic.Length + $roundBytes.Length + $salt.Length,$iv.Length)

    $derive = [Security.Cryptography.Rfc2898DeriveBytes]::new($Password,$salt,$Rounds,[Security.Cryptography.HashAlgorithmName]::SHA256)
    $keys = $derive.GetBytes(64)
    $derive.Dispose()
    $aes = [Security.Cryptography.Aes]::Create()
    $aes.KeySize = 256
    $aes.Mode = [Security.Cryptography.CipherMode]::CBC
    $aes.Padding = [Security.Cryptography.PaddingMode]::PKCS7
    $aes.Key = [byte[]]$keys[0..31]
    $aes.IV = $iv
    $encryptor = $aes.CreateEncryptor()
    $inputStream = [IO.File]::OpenRead($SourceZip)
    $cipherStream = [IO.File]::Create($CipherPath)
    $crypto = [Security.Cryptography.CryptoStream]::new($cipherStream,$encryptor,[Security.Cryptography.CryptoStreamMode]::Write)
    try { $inputStream.CopyTo($crypto) } finally { $crypto.Dispose(); $inputStream.Dispose(); $aes.Dispose() }

    $hmac = [Security.Cryptography.HMACSHA256]::new([byte[]]$keys[32..63])
    [void]$hmac.TransformBlock($header,0,$header.Length,$header,0)
    Add-FileToHmac $hmac $CipherPath
    [void]$hmac.TransformFinalBlock([byte[]]::new(0),0,0)
    $mac = $hmac.Hash
    $hmac.Dispose()
    $output = [IO.File]::Create($Destination)
    $cipherInput = [IO.File]::OpenRead($CipherPath)
    try {
        $output.Write($header,0,$header.Length)
        $cipherInput.CopyTo($output)
        $output.Write($mac,0,$mac.Length)
    } finally { $cipherInput.Dispose(); $output.Dispose() }
    [Array]::Clear($keys,0,$keys.Length)
}

function Verify-And-Decrypt([string]$Source, [string]$Destination, [string]$Password,
    [byte[]]$HeaderMagic, [string]$CipherPath) {
    $headerLength = $HeaderMagic.Length + 4 + 16 + 16
    $file = [IO.File]::OpenRead($Source)
    try {
        if ($file.Length -le ($headerLength + 32)) { throw "Encrypted backup is truncated." }
        $header = New-Object byte[] $headerLength
        if ($file.Read($header,0,$header.Length) -ne $header.Length) { throw "Encrypted backup header is incomplete." }
        for ($index = 0; $index -lt $HeaderMagic.Length; $index++) {
            if ($header[$index] -ne $HeaderMagic[$index]) { throw "Unrecognized backup format." }
        }
        $rounds = [BitConverter]::ToInt32($header,$HeaderMagic.Length)
        if ($rounds -lt 200000 -or $rounds -gt 2000000) { throw "Backup key-derivation parameters are outside accepted limits." }
        $salt = New-Object byte[] 16
        $iv = New-Object byte[] 16
        [Buffer]::BlockCopy($header,$HeaderMagic.Length + 4,$salt,0,16)
        [Buffer]::BlockCopy($header,$HeaderMagic.Length + 20,$iv,0,16)
        $cipherLength = $file.Length - $headerLength - 32
        $file.Position = $file.Length - 32
        $expectedMac = New-Object byte[] 32
        if ($file.Read($expectedMac,0,32) -ne 32) { throw "Backup authentication code is missing." }
        $file.Position = $headerLength
        $cipherOut = [IO.File]::Create($CipherPath)
        $hmac = $null
        $keys = $null
        try {
            $derive = [Security.Cryptography.Rfc2898DeriveBytes]::new($Password,$salt,$rounds,[Security.Cryptography.HashAlgorithmName]::SHA256)
            $keys = $derive.GetBytes(64)
            $derive.Dispose()
            $hmac = [Security.Cryptography.HMACSHA256]::new([byte[]]$keys[32..63])
            [void]$hmac.TransformBlock($header,0,$header.Length,$header,0)
            $buffer = New-Object byte[] 65536
            $remaining = $cipherLength
            while ($remaining -gt 0) {
                $wanted = [int][Math]::Min($buffer.Length,$remaining)
                $read = $file.Read($buffer,0,$wanted)
                if ($read -eq 0) { throw "Encrypted backup ended unexpectedly." }
                [void]$hmac.TransformBlock($buffer,0,$read,$buffer,0)
                $cipherOut.Write($buffer,0,$read)
                $remaining -= $read
            }
            [void]$hmac.TransformFinalBlock([byte[]]::new(0),0,0)
            $actualMac = $hmac.Hash
            $difference = 0
            for ($index = 0; $index -lt 32; $index++) { $difference = $difference -bor ($actualMac[$index] -bxor $expectedMac[$index]) }
            if ($difference -ne 0) { throw "Backup password is incorrect or the archive has been modified." }
            $cipherOut.Dispose()
            $aes = [Security.Cryptography.Aes]::Create()
            $aes.KeySize = 256
            $aes.Mode = [Security.Cryptography.CipherMode]::CBC
            $aes.Padding = [Security.Cryptography.PaddingMode]::PKCS7
            $aes.Key = [byte[]]$keys[0..31]
            $aes.IV = $iv
            $decryptor = $aes.CreateDecryptor()
            $cipherIn = [IO.File]::OpenRead($CipherPath)
            $plainOut = [IO.File]::Create($Destination)
            $crypto = [Security.Cryptography.CryptoStream]::new($cipherIn,$decryptor,[Security.Cryptography.CryptoStreamMode]::Read)
            try { $crypto.CopyTo($plainOut) } finally { $crypto.Dispose(); $plainOut.Dispose(); $aes.Dispose() }
        } finally {
            if ($hmac) { $hmac.Dispose() }
            if ($cipherOut) { $cipherOut.Dispose() }
            if ($keys) { [Array]::Clear($keys,0,$keys.Length) }
        }
    } finally { $file.Dispose() }
}

Export-ModuleMember -Function Add-FileToHmac,Protect-Backup,Verify-And-Decrypt
