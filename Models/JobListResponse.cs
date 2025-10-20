using System;
using System.Collections.Generic;

namespace Desktop_client_api_kod.Models
{
    public sealed class JobListResponse
    {
        public List<JobItem> data { get; set; } = new List<JobItem>();
        public bool error { get; set; }
        public string message { get; set; }
    }

    public sealed class JobItem
    {
        public string id { get; set; }
        public string user_job_id { get; set; }
        public string file_name { get; set; }
        public string status { get; set; }
        public long file_size { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public string batch_id { get; set; }
        public string batch_name { get; set; }
    }
}