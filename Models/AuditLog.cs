using System;

namespace SecureCampusApp.Models
{
    public class AuditLog
    {
        public string LogID { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string ActorRole { get; set; } = "";
        public string ActorUserID { get; set; } = "";
        public string Action { get; set; } = "";
        public string TargetEntity { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
