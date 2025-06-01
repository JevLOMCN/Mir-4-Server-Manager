namespace Mir_4_Server_Manager.Controllers
{
    public class Configs
    {
        public FrontConfig FrontConfig { get; set; }
        public ConnectionStrings ConnectionStrings { get; set; }
    }

    public class ConnectionStrings
    {
        public string mm_user_db { get; set; }
        public string mm_front_db { get; set; }
        public string mm_game_db { get; set; }
        public string redis { get; set; }
    }
    public class FrontConfig
    {
        public string FrontServerIPAddress { get; set; }
        public int FrontServerListenPort { get; set; }
        public int APIListenPort { get; set; }
        public string APIIPAddress { get; set; }
        public bool APIUseTls { get; set; }
        public int APITlsListenPort { get; set; }
        public string APITlsPFXCertificateFile { get; set; }
        public string APITlsPFXPassword { get; set; }
        public string AllowedHosts { get; set; }
        public bool UseStatusServer { get; set; }
        public string StatusServerIPAddress { get; set; }
        public int StatusServerPort { get; set; }
        public string LogsPath { get; set; }
    }

}
