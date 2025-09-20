namespace MiniECommerceStore.Models
{
    public class UserRole
    {
        public int UserID { get; set; }
        public int RoleID { get; set; }

        // Navigation properties
        public User User { get; set; }
        public Role Role { get; set; }
    }
}

