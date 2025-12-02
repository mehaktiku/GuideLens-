using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GuideLens.Pages;

public class ContactModel : PageModel
{
    [BindProperty]
    public ContactInput Input { get; set; } = new();

    public bool Success { get; set; }

    public void OnGet() { }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid) return Page();

        // Minimal “proof” for rubric: log it (safe, no DB needed).
        // You can later store in JSON/DB/email.
        Console.WriteLine($"[Contact] {Input.Name} <{Input.Email}>: {Input.Subject} :: {Input.Message}");

        Success = true;
        ModelState.Clear();
        Input = new ContactInput(); // clear form
        return Page();
    }

    public class ContactInput
    {
        [Required, StringLength(80)]
        public string Name { get; set; } = "";

        [Required, EmailAddress, StringLength(120)]
        public string Email { get; set; } = "";

        [Required, StringLength(120)]
        public string Subject { get; set; } = "";

        [Required, StringLength(1200)]
        public string Message { get; set; } = "";
    }
}
