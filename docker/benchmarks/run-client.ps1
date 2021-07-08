docker run `
    -d `
    --log-opt max-size=10m `
    --log-opt max-file=3 `
    --name benchmarks-client `
	-p 5002:5002 `
    --restart always `
    benchmarks `
    dotnet run -c Debug --project src/BenchmarksClient/BenchmarksClient.csproj
