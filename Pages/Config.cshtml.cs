using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mir_4_Server_Manager.Pages
{
    public class ConfigModel : PageModel
    {
        private readonly ILogger<ConfigModel> _logger;
        private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "serverconfig.json");

        public ConfigModel(ILogger<ConfigModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }

        public async Task<JsonResult> OnGetGetPathAsync()
        {
            if (!System.IO.File.Exists(ConfigPath))
            {
                var defaultConfig = new ConfigData { Path = "" };
                var defaultJson = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(ConfigPath, defaultJson);
                return new JsonResult(defaultConfig);
            }

            var json = await System.IO.File.ReadAllTextAsync(ConfigPath);
            var config = JsonSerializer.Deserialize<ConfigData>(json);
            return new JsonResult(config);
        }

        public async Task<IActionResult> OnPostSavePathAsync([FromBody] ConfigData data)
        {
            if (data?.Path == null || !Directory.Exists(data.Path))
                return BadRequest("Invalid or non-existent folder path.");

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(ConfigPath, json);
            return Content("Path saved successfully.");
        }

        public class ConfigData
        {
            public string Path { get; set; }
        }
    }
}