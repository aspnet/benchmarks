// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Text.Json;
using System.Threading.Tasks;
#if NET6_0_OR_GREATER
using System.Text.Json.Serialization.Metadata;
#endif

namespace PlatformBenchmarks
{
    public partial class BenchmarkApplication
    {
        private async Task MultipleQueries(PipeWriter pipeWriter, int count)
        {
#if NET6_0_OR_GREATER
            OutputMultipleQueries(pipeWriter, await RawDb.LoadMultipleQueriesRows(count), SerializerContext.WorldArray);
#else
            OutputMultipleQueries(pipeWriter, await RawDb.LoadMultipleQueriesRows(count));
#endif
        }

#if NET6_0_OR_GREATER
        private static void OutputMultipleQueries<TWord>(PipeWriter pipeWriter, TWord[] rows, JsonTypeInfo<TWord[]> jsonTypeInfo)
#else
        private static void OutputMultipleQueries<TWord>(PipeWriter pipeWriter, TWord[] rows)
#endif
        {
            var writer = GetWriter(pipeWriter, sizeHint: 160 * rows.Length); // in reality it's 152 for one

            writer.Write(_dbPreamble);

            var lengthWriter = writer;
            writer.Write(_contentLengthGap);

            // Date header
            writer.Write(DateHeader.HeaderBytes);

            writer.Commit();

            Utf8JsonWriter utf8JsonWriter = t_writer ??= new Utf8JsonWriter(pipeWriter, new JsonWriterOptions { SkipValidation = true });
            utf8JsonWriter.Reset(pipeWriter);

            // Body
            JsonSerializer.Serialize(
                utf8JsonWriter,
                rows,
#if NET6_0_OR_GREATER
                jsonTypeInfo
#else
                SerializerOptions
#endif
                );

            // Content-Length
            lengthWriter.WriteNumeric((uint)utf8JsonWriter.BytesCommitted);
        }
    }
}
