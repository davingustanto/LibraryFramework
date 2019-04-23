using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryFramework.Models
{
    public partial class PermissionBinding
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public partial class UserPermissionBinding
    {
        public int Id { get; set; }
        public int PermissionId { get; set; }
        public string UserId { get; set; }
    }

    public partial class ChangePasswordBinding
    {
        public string UserName { get; set; }
        public string Token { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }
    }

    public partial class RegisterBinding
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string EmailAddress { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
    }

    public partial class LoginBinding
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public partial class UserProfile
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime RegisterDate { get; set; }
    }

    public partial class OldTempUser
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public partial class OldTempRole
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public string UserId { get; set; }
    }
}
