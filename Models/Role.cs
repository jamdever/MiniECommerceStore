using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiniECommerceStore.Models
{
    public class Role
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
