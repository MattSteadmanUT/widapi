param(
    [string] $server = "127.0.0.1",
    [string] $url = "https://download.bls.gov/pub/time.series/la/la.data.40.NorthCarolina"
    )

Function Get-Data-From-BLS($url)
{
    $tempfile=$env:TEMP+"\bls_series.txt";
    Write-Host "Downloading $url to $tempfile" (Get-Date)
    Invoke-WebRequest -Uri $url -outfile $tempfile -UserAgent "your.email@youragency.gov"; # Replace with your contact email as required by BLS
    Write-Host "Building data table " (Get-Date);
    $reader = New-Object System.IO.StreamReader::new($tempfile);
    $table= new-object System.Data.DataTable;
    $newcol = New-Object system.Data.DataColumn seriesId,([string]); $newcol.maxlength=30; $table.columns.add($newcol)
    $newcol = New-Object system.Data.DataColumn year,([string]); $newcol.maxlength=4; $table.columns.add($newcol)
    $newcol = New-Object system.Data.DataColumn period,([string]); $newcol.maxlength=3; $table.columns.add($newcol)
    $newcol = New-Object system.Data.DataColumn value,([Decimal]); $newcol.AllowDBNull=$true; $table.columns.add($newcol)
    $newcol = New-Object system.Data.DataColumn footnote_codes,([string]); $newcol.maxlength=10; $table.columns.add($newcol)
    $i=0;
    $bulkCopy = new-object ("Data.SqlClient.SqlBulkCopy") $sqlConn
    $bulkCopy.DestinationTableName = "blsseries"
    $bulkcopy.BulkCopyTimeout=0
    $reader.ReadLine() > $null;
    while($null -ne ($line = $reader.ReadLine()))
    {
        $r=$table.NewRow();
        $v=$line -split "\t"
        $r=$v[0,1,2,4];
        if ($v[3].Trim() -eq "-")    # Not available
            {$r[3] =[System.DBNull]::Value;} # Convert "-" to NULL for bulk insert
        else
            {$r[3]=$v[3];}
        $table.Rows.Add($r) > $null;
        $i++;
        if ($i%100000 -eq 0) {
            $bulkCopy.WriteToServer($table);
            $table.Rows.Clear();
            Write-Host "Loaded " $i " records" (Get-Date)
        }
    }
    $reader.Close();
    Write-Host "Finished loading " $i "records " (Get-Date);
    $bulkCopy.WriteToServer($table);
    ExecuteSQL "INSERT import_history(name, run_date) VALUES('$url',GETDATE())" > $null;
}
Function Get-Last-Update
{
    $cmd = new-object System.Data.SqlClient.SqlCommand
    $cmd.Connection=$sqlConn
    $cmd.CommandText = "SELECT top 1 run_date FROM import_history WHERE name='$url' ORDER BY id desc";
    return $cmd.ExecuteScalar();
}

Function Get-File-Date
{
    $segments = ([uri]$url).segments;
    $l=$segments.Length-2;
    $folder="https://download.bls.gov"+[String]::Join('', $segments[0..$l]);
    $file=$segments[-1];
    $r=Invoke-WebRequest $folder -UseBasicParsing -UserAgent "your.email@youragency.gov"; # Replace with your contact email as required by BLS
    $p =$r.RawContent -split "<br>";
    $x = $p | Select-String -InputObject {$_} -Pattern $file
    $timestamp=([string]$x).Substring(0,20).Trim();
    return [datetime]::ParseExact("$timestamp", 'M/d/yyyy h:mm tt',
        [System.Globalization.CultureInfo]::InvariantCulture,'AllowInnerWhite');
}

Function ConnectToWid($server)
{
    $sqlConn = New-Object System.Data.SqlClient.SqlConnection;
    $sqlConn.ConnectionString = "Server=$server;Integrated Security=true;Initial Catalog=WID";
    $sqlConn.Open();
    return $sqlConn;
}

Function ExecuteSQL($sql)
{
    $cmd = new-object system.data.sqlclient.sqlcommand
    $cmd.CommandText=$sql
    $cmd.Connection=$sqlConn;
    $cmd.ExecuteNonQuery() > $null;
}

try {
    $sqlConn=ConnectToWid $server;
    Write-Host "Clearing bls series.";
    ExecuteSQL "truncate table blsseries" > $null;
    $lastUpdate=Get-Last-Update;
    $fileDate=Get-File-Date;
    if ($fileDate -gt $lastUpdate)
    {
        Write-Host "Getting data from BLS" (Get-Date);
        Get-Data-From-BLS $url;
    }
    else {Write-Host "Timestamp is $fileDate processed on $lastUpdate. Bypassing...";}
    $sqlConn.Close();
}
catch {
    Write-Error $_;
    Exit -1;
}
