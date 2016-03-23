﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Benchmarks.ClientJob;
using Benchmarks.ServerJob;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BenchmarkDriver
{
    public class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static int Main(string[] args)
        {
            var app = new CommandLineApplication()
            {
                Name = "benchmark-driver",
                FullName = "ASP.NET Benchmark Driver",
                Description = "Driver for ASP.NET Benchmarks"
            };

            app.HelpOption("-?|-h|--help");

            var scenarioOption = app.Option("-n|--scenario", "Benchmark scenario to run", CommandOptionType.SingleValue);
            var pullRequestOption = app.Option("-p|--pullRequest", "ID of pull request to test", CommandOptionType.SingleValue);
            var serverOption = app.Option("-s|--server", "URL of benchmark server", CommandOptionType.SingleValue);
            var clientOption = app.Option("-c|--client", "URL of benchmark client", CommandOptionType.SingleValue);
            var sqlConnectionStringOption = app.Option("-q|--sql", "Connection string of SQL Database to store results", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                var scenario = scenarioOption.Value();

                var pullRequest = 0;
                if (!string.IsNullOrWhiteSpace(pullRequestOption.Value()))
                {
                    pullRequest = int.Parse(pullRequestOption.Value());
                }

                var server = serverOption.Value();
                var client = clientOption.Value();
                var sqlConnectionString = sqlConnectionStringOption.Value();

                if (string.IsNullOrWhiteSpace(scenario) || string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(client))
                {
                    app.ShowHelp();
                    return 2;
                }
                else
                {
                    return Run(scenario, pullRequest, new Uri(server), new Uri(client), sqlConnectionString).Result;
                }
            });

            return app.Execute(args);
        }

        private static async Task<int> Run(string scenario, int pullRequest, Uri serverUri, Uri clientUri, string sqlConnectionString)
        {
            var serverJobsUri = new Uri(serverUri, "/jobs");
            Uri serverJobUri = null;
            HttpResponseMessage response = null;
            string responseContent = null;

            try
            {
                Log($"Starting scenario {scenario} on benchmark server...");

                var serverJob = new ServerJob()
                {
                    Scenario = "plaintext",
                };
                if (pullRequest != 0)
                {
                    serverJob.BenchmarksBranch = $"pull/{pullRequest}/head";
                }

                var content = JsonConvert.SerializeObject(serverJob);
                LogVerbose($"POST {serverJobsUri} {content}...");

                response = await _httpClient.PostAsync(serverJobsUri, new StringContent(content, Encoding.UTF8, "application/json"));
                responseContent = await response.Content.ReadAsStringAsync();
                LogVerbose($"{(int)response.StatusCode} {response.StatusCode}");
                response.EnsureSuccessStatusCode();

                serverJobUri = new Uri(serverUri, response.Headers.Location);

                var serverBenchmarkUri = string.Empty;
                while (true)
                {
                    LogVerbose($"GET {serverJobUri}...");
                    response = await _httpClient.GetAsync(serverJobUri);
                    responseContent = await response.Content.ReadAsStringAsync();

                    LogVerbose($"{(int)response.StatusCode} {response.StatusCode} {responseContent}");

                    serverJob = JsonConvert.DeserializeObject<ServerJob>(responseContent);

                    if (serverJob.State == ServerState.Running)
                    {
                        serverBenchmarkUri = serverJob.Url;
                        break;
                    }
                    else if (serverJob.State == ServerState.Failed)
                    {
                        throw new InvalidOperationException("Server job failed");
                    }
                    else
                    {
                        await Task.Delay(1000);
                    }
                }

                Uri clientJobUri = null;
                try
                {
                    var threads = 32;
                    var connections = 256;
                    var duration = 10;
                    var pipelineDepth = 16;
                    var command = $"wrk -c {connections} -t {threads} -d {duration} -s $BENCHMARKS_REPO/scripts/pipeline.lua {serverBenchmarkUri}/{scenario} -- {pipelineDepth}";

                    Log($"Starting scenario {scenario} on benchmark client...");
                    Log($"Command: {command}");

                    var clientJobsUri = new Uri(clientUri, "/jobs");

                    var clientContent = JsonConvert.SerializeObject(new ClientJob()
                    {
                        Command = command
                    });

                    LogVerbose($"POST {clientJobsUri} {clientContent}...");
                    response = await _httpClient.PostAsync(clientJobsUri, new StringContent(clientContent, Encoding.UTF8, "application/json"));
                    responseContent = await response.Content.ReadAsStringAsync();
                    LogVerbose($"{(int)response.StatusCode} {response.StatusCode}");
                    response.EnsureSuccessStatusCode();

                    clientJobUri = new Uri(clientUri, response.Headers.Location);

                    while (true)
                    {
                        LogVerbose($"GET {clientJobUri}...");
                        response = await _httpClient.GetAsync(clientJobUri);
                        responseContent = await response.Content.ReadAsStringAsync();

                        LogVerbose($"{(int)response.StatusCode} {response.StatusCode} {responseContent}");

                        var clientJob = JsonConvert.DeserializeObject<ClientJob>(responseContent);

                        if (clientJob.State == ClientState.Running)
                        {
                            break;
                        }
                        else
                        {
                            await Task.Delay(1000);
                        }
                    }

                    while (true)
                    {
                        LogVerbose($"GET {clientJobUri}...");
                        response = await _httpClient.GetAsync(clientJobUri);
                        responseContent = await response.Content.ReadAsStringAsync();

                        LogVerbose($"{(int)response.StatusCode} {response.StatusCode} {responseContent}");

                        var clientJob = JsonConvert.DeserializeObject<ClientJob>(responseContent);

                        if (clientJob.State == ClientState.Completed)
                        {
                            Log($"Scenario {scenario} completed on benchmark client");
                            LogVerbose($"Output: {clientJob.Output}");
                            LogVerbose($"Error: {clientJob.Error}");

                            double rps = -1;
                            var match = Regex.Match(clientJob.Output, @"Requests/sec:\s*([\d.]*)");
                            if (match.Success && match.Groups.Count == 2)
                            {
                                double.TryParse(match.Groups[1].Value, out rps);
                            }

                            Log($"RPS: {rps}");

                            if (!string.IsNullOrWhiteSpace(sqlConnectionString))
                            {
                                await WriteResultsToSql(sqlConnectionString, scenario, threads, connections, duration, pipelineDepth, rps);
                            }

                            break;
                        }
                        else
                        {
                            await Task.Delay(1000);
                        }
                    }
                }
                finally
                {
                    if (clientJobUri != null)
                    {
                        Log($"Stopping scenario {scenario} on benchmark client...");

                        LogVerbose($"DELETE {clientJobUri}...");
                        response = _httpClient.DeleteAsync(clientJobUri).Result;
                        LogVerbose($"{(int)response.StatusCode} {response.StatusCode}");
                        response.EnsureSuccessStatusCode();
                    }
                }
            }
            finally
            {
                Log($"Stopping scenario {scenario} on benchmark server...");

                LogVerbose($"DELETE {serverJobUri}...");
                response = _httpClient.DeleteAsync(serverJobUri).Result;
                LogVerbose($"{(int)response.StatusCode} {response.StatusCode}");
                response.EnsureSuccessStatusCode();
            }

            return 0;
        }

        private static async Task WriteResultsToSql(string connectionString, string scenario, int threads, int connections, int duration, int? pipelineDepth, double rps)
        {
            Log("Writing results to SQL...");

            const string createCmd =
                @"
                IF OBJECT_ID(N'dbo.AspNetBenchmarks', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[AspNetBenchmarks](
                        [Id] [int] IDENTITY(1,1) NOT NULL,
                        [DateTime] [datetimeoffset](7) NOT NULL,
                        [Scenario] [nvarchar](max) NOT NULL,
                        [Threads] [int] NOT NULL,
                        [Connections] [int] NOT NULL,
                        [Duration] [int] NOT NULL,
                        [PipelineDepth] [int] NULL,
                        [RequestsPerSecond] [float] NOT NULL
                    )
                END
                ";

            const string insertCmd =
                @"
                INSERT INTO [dbo].[AspNetBenchmarks]
                           ([DateTime]
                           ,[Scenario]
                           ,[Threads]
                           ,[Connections]
                           ,[Duration]
                           ,[PipelineDepth]
                           ,[RequestsPerSecond])
                     VALUES
                           (@DateTime
                           ,@Scenario
                           ,@Threads
                           ,@Connections
                           ,@Duration
                           ,@PipelineDepth
                           ,@RequestsPerSecond)
                ";

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(createCmd, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = new SqlCommand(insertCmd, connection))
                {
                    var p = command.Parameters;
                    p.AddWithValue("@DateTime", DateTimeOffset.UtcNow);
                    p.AddWithValue("@Scenario", scenario);
                    p.AddWithValue("@Threads", threads);
                    p.AddWithValue("@Connections", connections);
                    p.AddWithValue("@Duration", duration);
                    p.AddWithValue("@PipelineDepth", ((object)pipelineDepth) ?? DBNull.Value);
                    p.AddWithValue("@RequestsPerSecond", rps);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private static void Log(string message)
        {
            Log(message, Reporter.Output);
        }

        private static void LogVerbose(string message)
        {
            Log(message, Reporter.Verbose);
        }

        private static void Log(string message, Reporter reporter)
        {
            var time = DateTime.Now.ToString("hh:mm:ss.fff");
            reporter.WriteLine($"[{time}] {message}");
        }
    }
}