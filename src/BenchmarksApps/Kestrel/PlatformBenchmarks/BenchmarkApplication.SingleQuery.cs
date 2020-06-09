﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlatformBenchmarks
{
    public partial class BenchmarkApplication
    {
        private async Task SingleQuery(PipeWriter pipeWriter)
        {
            OutputSingleQuery(pipeWriter, await Db.LoadSingleQueryRow());
        }

        private static void OutputSingleQuery(PipeWriter pipeWriter, World row)
        {
            var writer = GetWriter(pipeWriter, sizeHint: 180); // in reality it's 150

            writer.Write(_dbPreamble);

            // Content-Length
            var jsonPayload = JsonSerializer.SerializeToUtf8Bytes(row, SerializerOptions);
            writer.WriteNumeric((uint)jsonPayload.Length);

            // Date header
            writer.Write(DateHeader.HeaderBytes);

            // Body
            writer.Write(jsonPayload);
            writer.Commit();
        }
    }
}
