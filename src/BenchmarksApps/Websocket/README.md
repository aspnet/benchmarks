## Purpose

This project is to assist in Benchmarking Websockets.
It makes it easier to test local changes than having the App in the Benchmarks repo by letting us make changes in websocket branches and using the example commandline below to run the benchmarks against our branches.

The WebsocketWorker that runs against this server is located at https://github.com/aspnet/benchmarks/blob/main/src/WebsocketClient/Program.cs.

## Usage

1. Push changes you would like to test to a branch on GitHub
2. Clone aspnet/benchmarks repo to your machine or install the global BenchmarksDriver tool https://www.nuget.org/packages/BenchmarksDriver/
3. If cloned go to the BenchmarksDriver project
4. Use the following command as a guideline for running a test using your changes

`benchmarks --config benchmarks.websocket.yml --scenario echo --variable serverUri=http://10.0.0.102 --load.endpoints http://10.0.0.102:5001

5. For more info/commands see https://github.com/aspnet/benchmarks/blob/main/src/BenchmarksDriver2/README.md
