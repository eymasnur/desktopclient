using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Desktop_client_api_kod.Models
{
    public sealed class JobHistoryResponse
    {
        public List<JobHistoryItem> data { get; set; }
        public bool error { get; set; }
        public string message { get; set; }
        public int total_count { get; set; }
    }

    public sealed class JobHistoryItem
    {
        public UserJobInfo user_job_info { get; set; }
    }

    public sealed class UserJobInfo
    {
        public string username { get; set; }
        public string created_at { get; set; }
        public string status { get; set; }
        public string user_job_id { get; set; }
        public string file_name { get; set; }
        public long file_size { get; set; }
        public string file_extension { get; set; }
    }
}