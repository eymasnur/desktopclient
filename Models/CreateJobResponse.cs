using System.Collections.Generic;

namespace Desktop_client_api_kod.Models
{
    public sealed class CreateJobResponse
    {
        public CreateJobData data { get; set; }
        public bool error { get; set; }
        public string message { get; set; }
    }

    public sealed class CreateJobData
    {
        public string id { get; set; }
        public List<string> user_job_ids { get; set; }
        public string user_id { get; set; }
        public string batch_name { get; set; }
        public string client_type { get; set; }
        public int in_progress_user_job_count { get; set; }
        public int failed_user_job_count { get; set; }
        public int not_sanitizable_user_job_count { get; set; }
        public int blocked_user_job_count { get; set; }
        public int sanitized_user_job_count { get; set; }
        public int halted_user_job_count { get; set; }
    }
}