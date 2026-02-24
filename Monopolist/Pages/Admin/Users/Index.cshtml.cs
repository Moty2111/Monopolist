using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;

namespace Monoplist.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public IList<UserIndexViewModel> Users { get; set; } = new List<UserIndexViewModel>();

    public async Task OnGetAsync()
    {
        Users = await _context.Users
            .OrderBy(u => u.Username)
            .Select(u => new UserIndexViewModel
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role
            })
            .ToListAsync();
    }
}