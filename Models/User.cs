namespace SecureCampusApp.Models
{
    public class User
    {
        public string UserID { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
    }
}
