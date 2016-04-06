﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Repository;

namespace Benchmarks.ServerJob
{
    public class ServerJob : IIdentifiable
    {
        public int Id { get; set; }

        public string BenchmarksRepoUrl { get; set; }

        public string BenchmarksBranch { get; set; }

        public string KestrelRepoUrl { get; set; }

        public string KestrelBranch { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Scenario Scenario { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ServerState State { get; set; }

        public string Url { get; set; }
    }
}
