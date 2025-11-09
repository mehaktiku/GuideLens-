using Microsoft.AspNetCore.Mvc.RazorPages;
using GuideLens.Data;
using GuideLens.Models;

namespace GuideLens.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        public List<Recommendation> Items { get; set; } = new();

        public IndexModel(IWebHostEnvironment env)
        {
            _env = env;
        }

        public void OnGet()
        {
            Items = DataLoader.LoadFromContentRoot(_env.ContentRootPath);
        }
    }
}