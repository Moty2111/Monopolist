using System.Collections.Generic;

namespace Monopolist.ViewModels.User
{
    public class UserWithRolesViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
    }
}