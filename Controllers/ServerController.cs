using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Mir4ServerWebApp.Models;

namespace Mir4ServerWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServerController : ControllerBase
    {
        private const string ServerExeName = "YourServerExecutable.exe"; // Replace with your actual EXE name
        private const string ServerExePath = @"C:\Path\To\Your\EXE\"; // Replace with full path

        [HttpPost("start")]
        public IActionResult StartServer()
        {
            if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ServerExeName)).Any())
                return Ok("Server is already running.");

            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(ServerExePath, ServerExeName),
                WorkingDirectory = ServerExePath
            });
            return Ok("Server started.");
        }

        [HttpPost("stop")]
        public IActionResult StopServer()
        {
            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ServerExeName));
            foreach (var process in processes)
                process.Kill();

            return Ok("Server stopped.");
        }

        [HttpGet("status")]
        public ActionResult<ServerStatus> GetStatus()
        {
            var isRunning = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ServerExeName)).Any();
            return new ServerStatus { IsRunning = isRunning };
        }
    }
}
