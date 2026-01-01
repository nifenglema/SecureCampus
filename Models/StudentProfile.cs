namespace SecureCampusApp.Models
{
    public class StudentProfile
    {
        public string StudentID { get; set; }
        public string UserID { get; set; }
        public string MatricNo { get; set; }
        public string Programme { get; set; }
        public int IntakeYear { get; set; }
        public string Address { get; set; }
        public string IC { get; set; } // input only (never displayed)
    }
}
