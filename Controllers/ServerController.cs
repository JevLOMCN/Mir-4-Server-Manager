using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Mir_4_Server_Manager.Controllers;

namespace Mir4ServerWebApp.Controllers
{
    [Route("[controller]")]
    [Controller]
    public class ServerController : ControllerBase
    {
        private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "serverconfig.json");

        private string GetServerPath()
        {
            if (!System.IO.File.Exists(ConfigPath))
                return null;

            var json = System.IO.File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<ConfigData>(json);
            return config?.Path;
        }
        private int GetUserCountFromFrontLog()
        {
            string baseDir = GetServerPath();
            if (string.IsNullOrEmpty(baseDir)) return 0;

            string frontLogDir = Path.Combine(baseDir, "Front", "logs");

            if (!Directory.Exists(frontLogDir)) return 0;

            var latestLogFile = new DirectoryInfo(frontLogDir)
                .GetFiles("*.txt")
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            if (latestLogFile == null) return 0;

            try
            {
                string[] lines = System.IO.File.ReadAllLines(latestLogFile.FullName);
                foreach (string line in lines.Reverse())
                {
                    if (line.Contains("Redis get usercount"))
                    {
                        int index = line.LastIndexOf(':');
                        if (index != -1 && int.TryParse(line[(index + 1)..].Trim(), out int count))
                        {
                            return count;
                        }
                    }
                }
            }
            catch
            {
            }

            return 0;
        }

        [HttpPost("start")]
        public IActionResult StartServer()
        {
            var serverPath = GetServerPath();

            if (string.IsNullOrEmpty(serverPath) || !Directory.Exists(serverPath))
                return BadRequest("Server path is not configured or invalid.");

            var startBat = Path.Combine(serverPath, "start_servers.bat");

            if (!System.IO.File.Exists(startBat))
                return BadRequest("start_servers.bat not found in the configured server directory.");

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = startBat,
                    WorkingDirectory = serverPath,
                    UseShellExecute = true,
                    Verb = "runas"
                });

                return Ok("Server started.");
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Failed to start server: {ex.Message}");
            }
        }

        [HttpPost("stop")]
        public IActionResult StopServer()
        {
            var serverPath = GetServerPath();

            if (string.IsNullOrEmpty(serverPath) || !Directory.Exists(serverPath))
                return BadRequest("Server path is not configured or invalid.");

            var stopBat = Path.Combine(serverPath, "stop_servers.bat");

            if (!System.IO.File.Exists(stopBat))
                return BadRequest("stop_servers.bat not found in the configured server directory.");

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = stopBat,
                    WorkingDirectory = serverPath,
                    UseShellExecute = true,
                    Verb = "runas"
                });

                return Ok("Server stopped.");
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Failed to stop server: {ex.Message}");
            }
        }

        [HttpGet("status")]
        public async Task<ActionResult<ServerStatus>> GetStatus()
        {
             var serverExeNames = new[]
            {
                "WorldServer.exe",
                "GameServer.exe",
                "ChattingServer.exe",
                "FrontServer.exe",
                "GatewayServer.exe"
            };

            var statusList = serverExeNames.Select(exe => new IndividualServerStatus
            {
                ServerName = Path.GetFileNameWithoutExtension(exe),
                IsRunning = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exe)).Any()
            }).ToList();

            int userCount = 0;
            try
            {
                using TcpClient client = new();
                await client.ConnectAsync("127.0.0.1", 3000);
                using NetworkStream stream = client.GetStream();

                byte[] buffer = new byte[256];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                var parts = response.Split(';');
                var userCountSegment = parts.FirstOrDefault(p => p.StartsWith("MIR4/0/"));
                if (userCountSegment != null)
                {
                    var segments = userCountSegment.Split('/');
                    if (segments.Length >= 3 && int.TryParse(segments[2], out int count))
                        userCount = count;
                }
            }
            catch
            {
            }

            return new ServerStatus
            {
                Servers = statusList,
                UserCount = userCount
            };
        }


        [Route("/server/logs/{server}")]
        public IActionResult Logs(string server)
        {
            string baseDir = GetServerPath();
            if (string.IsNullOrEmpty(baseDir))
                return BadRequest("Server path is not configured or invalid.");

            string logDir = server.ToLower() switch
            {
                "chatting" => Path.Combine(baseDir, "Chatting", "logs", "ChattingServer"),
                "front" => Path.Combine(baseDir, "Front", "logs"),
                "game" => Path.Combine(baseDir, "Game", "logs", "GameServer"),
                "gateway" => Path.Combine(baseDir, "Gateway", "logs", "GatewayServer"),
                "world" => Path.Combine(baseDir, "World", "logs", "WorldServer"),
                _ => null
            };

            if (logDir == null || !Directory.Exists(logDir))
                return NotFound($"Log directory not found: {logDir}");

            string searchPattern = server.Equals("front", StringComparison.OrdinalIgnoreCase) ? "*.txt" : "*.log";

            var lastFile = new DirectoryInfo(logDir)
                .GetFiles(searchPattern)
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            if (lastFile == null)
                return NotFound("No log file found.");

            try
            {
                using var stream = new FileStream(lastFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();
                return Content(content, "text/plain");
            }
            catch (IOException)
            {
                return Content("Log file is currently in use. Try again shortly.", "text/plain");
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Failed to read log file: {ex.Message}");
            }
        }

        [HttpGet("usercount")]
        public IActionResult GetUserCount()
        {
            int userCount = GetUserCountFromFrontLog();
            return Content(userCount.ToString());
        }

        public class IndividualServerStatus
        {
            public string ServerName { get; set; }
            public bool IsRunning { get; set; }
        }

        private class ConfigData
        {
            public string Path { get; set; }
        }

        public class ServerStatus
        {
            public List<IndividualServerStatus> Servers { get; set; }
            public int UserCount { get; set; }
        }
    }
}
