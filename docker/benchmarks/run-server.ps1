 param(
 [string]$ServerIp="",
 [string]$HardwareVersion="",
 [string]$Hardware="",
 [string]$Url="",
 [string]$Name="" 
 )

$PostgreSql='--postgresql "Server=TFB-database;Database=hello_world;User Id=benchmarkdbuser;Password=benchmarkdbpass;Maximum Pool Size=1024;NoResetOnClose=true;Max Auto Prepare=3"'
$MySql='--mysql "Server=TFB-database;Database=hello_world;User Id=benchmarkdbuser;Password=benchmarkdbpass;Maximum Pool Size=1024;SslMode=None;ConnectionReset=false"'
$MsSql='--mssql "Server=TFB-database;Database=hello_world;User Id=sa;Password=Benchmarkdbp@55;Max Pool Size=100;"'
$MongoDb='--mongodb "mongodb://TFB-database:27017?maxPoolSize=1024"'

# "--network host" - Better performance than the default "bridge" driver
# "-v /var/run/docker.sock" - Give container access to the host docker daemon 
docker run `
     -d `
     --log-opt max-size=10m `
     --log-opt max-file=3 `
     --name benchmarks-server `
     -p 5001:5001 `
     --restart always `
     -v /var/run/docker.sock:/var/run/docker.sock `
     benchmarks `
     bash -c `
     "dotnet run -c Debug --project src/BenchmarksServer/BenchmarksServer.csproj -n 10.0.75.0 --url http://*:5001 --hardware physical --hardware-version laptop"
