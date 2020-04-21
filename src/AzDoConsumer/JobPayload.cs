﻿namespace AzDoConsumer
{
    public class JobPayload
    {
        public string[] Args { get; set; }

        public string Message { get; set; }

        public static JobPayload Deserialize(byte[] data)
        {
            var str = System.Text.Encoding.UTF8.GetString(data);
            str = str.Substring(str.IndexOf("{"));
            str = str.Substring(0, str.LastIndexOf("}") + 1);
            return System.Text.Json.JsonSerializer.Deserialize<JobPayload>(str);
        }
    }
}
