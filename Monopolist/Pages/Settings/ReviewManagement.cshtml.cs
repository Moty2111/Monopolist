using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.Security.Claims;

namespace Monoplist.Pages.Settings;

[Authorize(Roles = "Admin,Manager")]
public class ReviewManagementModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReviewManagementModel> _logger;

    public ReviewManagementModel(AppDbContext context, ILogger<ReviewManagementModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public List<ReviewWithDetails> Reviews { get; set; } = new();

    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    [BindProperty(SupportsGet = true)]
    public string? FilterStatus { get; set; }

    public async Task OnGetAsync()
    {
        await LoadUserSettings();
        await LoadReviews();
    }

    private async Task LoadReviews()
    {
        var query = _context.Reviews
            .Include(r => r.Product)
            .Include(r => r.Customer)
            .AsQueryable();

        query = FilterStatus switch
        {
            "pending" => query.Where(r => !r.IsApproved),
            "approved" => query.Where(r => r.IsApproved && string.IsNullOrEmpty(r.AdminReply)),
            "replied" => query.Where(r => !string.IsNullOrEmpty(r.AdminReply)),
            _ => query
        };

        var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

        Reviews = reviews.Select(r => new ReviewWithDetails
        {
            Id = r.Id,
            ProductName = r.Product?.Name ?? "Товар удалён",
            CustomerName = r.Customer?.FullName ?? "Клиент удалён",
            Rating = r.Rating,
            Text = r.Text,
            CreatedAt = r.CreatedAt,
            IsApproved = r.IsApproved,
            AdminReply = r.AdminReply,
            RepliedAt = r.RepliedAt
        }).ToList();
    }

    public async Task<IActionResult> OnPostApproveAsync(int reviewId)
    {
        var review = await _context.Reviews.FindAsync(reviewId);
        if (review == null) return NotFound();

        if (!review.IsApproved)
        {
            review.IsApproved = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Отзыв одобрен.";
        }
        return RedirectToPage(new { FilterStatus });
    }

    public async Task<IActionResult> OnPostRejectAsync(int reviewId)
    {
        var review = await _context.Reviews.FindAsync(reviewId);
        if (review == null) return NotFound();

        // Снимаем одобрение и удаляем ответ
        review.IsApproved = false;
        review.AdminReply = null;
        review.RepliedAt = null;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Отзыв отклонён.";
        return RedirectToPage(new { FilterStatus });
    }

    public async Task<IActionResult> OnPostReplyAsync(int reviewId, string adminReply)
    {
        if (string.IsNullOrWhiteSpace(adminReply))
        {
            TempData["Error"] = GetLocalizedMessage("Введите текст ответа.", "Enter reply text.", "Жауап мәтінін енгізіңіз.");
            return RedirectToPage(new { FilterStatus });
        }

        var review = await _context.Reviews.FindAsync(reviewId);
        if (review == null) return NotFound();

        // Если отзыв не одобрен, одобряем одновременно
        if (!review.IsApproved)
        {
            review.IsApproved = true;
        }

        // Разрешаем только один ответ — перезаписываем
        review.AdminReply = adminReply;
        review.RepliedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Ответ сохранён.";
        return RedirectToPage(new { FilterStatus });
    }

    private async Task LoadUserSettings()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            Language = user.Language ?? "ru";
            CompactMode = user.CompactMode;
            Animations = user.Animations;
            Theme = user.Theme ?? "light";
            CustomColor = user.CustomColor ?? "#FF6B00";
        }
    }

    private string GetLocalizedMessage(string ru, string en, string kk)
    {
        return Language switch
        {
            "en" => en,
            "kk" => kk,
            _ => ru
        };
    }
}

public class ReviewWithDetails
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsApproved { get; set; }
    public string? AdminReply { get; set; }
    public DateTime? RepliedAt { get; set; }
}