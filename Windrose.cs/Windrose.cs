using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.GameServer.Query;

namespace WindowsGSM.Plugins
{
    public class Windrose : SteamCMDAgent
    {
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.Windrose",
            author = "Shenniko",
            description = "WindowsGSM plugin for supporting Windrose Dedicated Server",
            version = "2.0",
            url = "https://github.com/shenniko/WindowsGSM.Windrose",
            color = "#1E8449"
        };

        public Windrose(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;

        public override bool loginAnonymous => true;
        public override string AppId => "4129620";
        public override string StartPath => Path.Combine("R5", "Binaries", "Win64", "WindroseServer-Win64-Shipping.exe");

        public string FullName = "Windrose Dedicated Server";
        public bool AllowsEmbedConsole = true;
        public int PortIncrements = 0;
        public object QueryMethod = new A2S();

        // WGSM fallback defaults
        public string ServerName = "WGSM Windrose";
        public string Defaultmap = "";
        public string Maxplayers = "4";
        public string Port = "7777";
        public string QueryPort = "7778";
        public string Additional = "";

        // Windrose-specific plugin settings
        public string MultiHome = "0.0.0.0";
        public string InviteCode = "";
        public string PasswordEnabled = "false";
        public string Password = "";
        public string WorldId = "";

        public void CreateServerCFG()
        {
            // Windrose generates its own config files.
        }

        public async Task<Process> Start()
        {
            string exePath = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(exePath))
            {
                Error = $"{Path.GetFileName(exePath)} not found ({exePath})";
                return null;
            }

            try
            {
                if (File.Exists(ServerDescriptionPath))
                {
                    UpdateServerDescription();
                }
                else
                {
                    Notice = "ServerDescription.json not found yet. Start the server once so Windrose can generate its config files.";
                }

                Process p = BuildProcess(exePath);
                p.Start();

                if (_serverData.EmbedConsole)
                {
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                }

                return p;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                return null;
            }
        }

        public async Task Stop(Process p)
        {
            if (p == null)
            {
                return;
            }

            try
            {
                if (!p.HasExited && SendStopSignal(p))
                {
                    await Task.Delay(1000);
                }

                if (!p.HasExited && _serverData.EmbedConsole && p.StartInfo.RedirectStandardInput)
                {
                    try { await p.StandardInput.WriteLineAsync("quit"); } catch { }
                    await Task.Delay(1500);
                }

                if (!p.HasExited)
                {
                    p.Kill();
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }

        public new async Task<Process> Update(bool validate = false, string custom = null)
        {
            var (p, error) = await Installer.SteamCMD.UpdateEx(
                serverData.ServerID,
                AppId,
                validate,
                custom: custom,
                loginAnonymous: loginAnonymous
            );

            Error = error;
            await Task.Run(() => p.WaitForExit());
            return p;
        }

        public new bool IsInstallValid()
        {
            return File.Exists(ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }

        public new bool IsImportValid(string path)
        {
            string exePath = Path.Combine(path, "R5", "Binaries", "Win64", "WindroseServer-Win64-Shipping.exe");
            Error = $"Invalid Path! Failed to find {Path.GetFileName(exePath)}";
            return File.Exists(exePath);
        }

        public new string GetLocalBuild()
        {
            return new Installer.SteamCMD().GetLocalBuild(_serverData.ServerID, AppId);
        }

        public new async Task<string> GetRemoteBuild()
        {
            return await new Installer.SteamCMD().GetRemoteBuild(AppId);
        }

        private string ServerDescriptionPath =>
            Path.Combine(ServerPath.GetServersServerFiles(_serverData.ServerID, "R5"), "ServerDescription.json");

        private string RocksDbRoot =>
            Path.Combine(ServerPath.GetServersServerFiles(_serverData.ServerID, "R5"), "Saved", "SaveProfiles", "Default", "RocksDB");

        private string EffectiveServerName =>
            string.IsNullOrWhiteSpace(_serverData.ServerName) ? ServerName : _serverData.ServerName;

        private string EffectivePort =>
            string.IsNullOrWhiteSpace(_serverData.ServerPort) ? Port : _serverData.ServerPort;

        private string EffectiveQueryPort =>
            string.IsNullOrWhiteSpace(_serverData.ServerQueryPort) ? QueryPort : _serverData.ServerQueryPort;

        private string EffectiveAdditional =>
            string.IsNullOrWhiteSpace(_serverData.ServerParam) ? Additional : _serverData.ServerParam;

        private string EffectiveServerIP =>
            string.IsNullOrWhiteSpace(_serverData.ServerIP) ? "" : _serverData.ServerIP;

        private int EffectiveMaxPlayers
        {
            get
            {
                string value = Maxplayers;
                try
                {
                    if (!string.IsNullOrWhiteSpace(_serverData.ServerMaxPlayer))
                    {
                        value = _serverData.ServerMaxPlayer;
                    }
                }
                catch
                {
                }

                return ParseInt(value, 4);
            }
        }

        private Process BuildProcess(string exePath)
        {
            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = exePath,
                    Arguments = BuildLaunchArguments(),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            if (_serverData.EmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;

                ServerConsole serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;
            }

            return p;
        }

        private string BuildLaunchArguments()
        {
            List<string> args = new List<string>
            {
                $"-MULTIHOME={SanitizeMultiHome(MultiHome)}",
                $"-PORT={ParseInt(EffectivePort, 7777)}",
                $"-QUERYPORT={ParseInt(EffectiveQueryPort, 7778)}"
            };

            if (!string.IsNullOrWhiteSpace(EffectiveAdditional))
            {
                args.Add(EffectiveAdditional.Trim());
            }

            return string.Join(" ", args);
        }

        private void UpdateServerDescription()
        {
            string json;
            try
            {
                json = File.ReadAllText(ServerDescriptionPath);
            }
            catch (Exception ex)
            {
                Error = "Failed to read ServerDescription.json: " + ex.Message;
                return;
            }

            bool passwordEnabled = ParseBool(PasswordEnabled, false);

            json = SetPersistentValue(json, "ServerName", EffectiveServerName);
            json = SetPersistentValue(json, "MaxPlayerCount", EffectiveMaxPlayers.ToString(), false);
            json = SetPersistentValue(json, "IsPasswordProtected", passwordEnabled ? "true" : "false", false);
            json = SetPersistentValue(json, "Password", passwordEnabled ? (Password ?? "") : "");

            if (!string.IsNullOrWhiteSpace(InviteCode))
            {
                json = SetPersistentValue(json, "InviteCode", InviteCode.Trim());
            }

            if (!string.IsNullOrWhiteSpace(EffectiveServerIP))
            {
                json = SetPersistentValue(json, "P2pProxyAddress", EffectiveServerIP.Trim());
            }

            if (!string.IsNullOrWhiteSpace(WorldId) && TryGetWorldInfo(WorldId.Trim(), out WindroseWorldSummary world))
            {
                json = SetPersistentValue(json, "WorldIslandId", world.IslandId);
            }

            try
            {
                File.WriteAllText(ServerDescriptionPath, json);
                Notice = BuildNotice(json);
            }
            catch (Exception ex)
            {
                Error = "Failed to write ServerDescription.json: " + ex.Message;
            }
        }

        private string BuildNotice(string serverDescriptionJson)
        {
            List<string> lines = new List<string>
            {
                "Applied Windrose config:",
                "ServerName = " + EffectiveServerName,
                "MaxPlayerCount = " + EffectiveMaxPlayers,
                "Port = " + EffectivePort,
                "QueryPort = " + EffectiveQueryPort,
                "P2pProxyAddress = " + (string.IsNullOrWhiteSpace(EffectiveServerIP) ? "not set" : EffectiveServerIP),
                "Invite Code = " + EmptyToFallback(GetJsonString(serverDescriptionJson, "InviteCode"), "not set")
            };

            List<WindroseWorldSummary> worlds = GetAllWorlds();
            if (worlds.Count > 0)
            {
                lines.Add("Detected Windrose worlds:");
                foreach (WindroseWorldSummary world in worlds)
                {
                    lines.Add($"- {world.IslandId} | {world.WorldName} | {world.WorldPresetType}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private List<WindroseWorldSummary> GetAllWorlds()
        {
            List<WindroseWorldSummary> worlds = new List<WindroseWorldSummary>();
            string worldsRoot = GetWorldsRoot();

            if (string.IsNullOrWhiteSpace(worldsRoot) || !Directory.Exists(worldsRoot))
            {
                return worlds;
            }

            foreach (string dir in Directory.GetDirectories(worldsRoot))
            {
                string worldId = Path.GetFileName(dir);
                if (TryGetWorldInfo(worldId, out WindroseWorldSummary world))
                {
                    worlds.Add(world);
                }
            }

            return worlds;
        }

        private bool TryGetWorldInfo(string worldId, out WindroseWorldSummary worldInfo)
        {
            worldInfo = null;

            string worldsRoot = GetWorldsRoot();
            if (string.IsNullOrWhiteSpace(worldsRoot))
            {
                return false;
            }

            string worldFile = Path.Combine(worldsRoot, worldId, "WorldDescription.json");
            if (!File.Exists(worldFile))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(worldFile);
                string islandId = GetJsonString(json, "IslandId");

                if (string.IsNullOrWhiteSpace(islandId) || !string.Equals(islandId, worldId, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                worldInfo = new WindroseWorldSummary
                {
                    IslandId = islandId,
                    WorldName = EmptyToFallback(GetJsonString(json, "WorldName"), worldId),
                    WorldPresetType = EmptyToFallback(GetJsonString(json, "WorldPresetType"), "Unknown")
                };

                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetWorldsRoot()
        {
            if (!Directory.Exists(RocksDbRoot))
            {
                return null;
            }

            string[] versionDirs = Directory.GetDirectories(RocksDbRoot);
            if (versionDirs.Length == 0)
            {
                return null;
            }

            string latestVersionDir = versionDirs
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Last();

            string worldsPath = Path.Combine(latestVersionDir, "Worlds");
            return Directory.Exists(worldsPath) ? worldsPath : null;
        }

        private string SetPersistentValue(string json, string key, string value, bool isString = true)
        {
            Match blockMatch = Regex.Match(
                json,
                @"(""ServerDescription_Persistent""\s*:\s*\{)([\s\S]*?)(\n\s*\})",
                RegexOptions.IgnoreCase
            );

            if (!blockMatch.Success)
            {
                return json;
            }

            string prefix = blockMatch.Groups[1].Value;
            string body = blockMatch.Groups[2].Value;
            string suffix = blockMatch.Groups[3].Value;

            string pattern = isString
                ? "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\""
                : "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false|-?[0-9]+)";

            string replacement = isString
                ? "\"" + key + "\": \"" + EscapeJson(value) + "\""
                : "\"" + key + "\": " + value;

            string updatedBody = Regex.IsMatch(body, pattern, RegexOptions.IgnoreCase)
                ? Regex.Replace(body, pattern, replacement, RegexOptions.IgnoreCase)
                : InsertJsonProperty(body, replacement);

            return json.Substring(0, blockMatch.Index)
                 + prefix
                 + updatedBody
                 + suffix
                 + json.Substring(blockMatch.Index + blockMatch.Length);
        }

        private string InsertJsonProperty(string body, string propertyText)
        {
            string trimmed = body.TrimEnd();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return Environment.NewLine + "        " + propertyText + Environment.NewLine;
            }

            return trimmed + "," + Environment.NewLine + "        " + propertyText + Environment.NewLine;
        }

        private string GetJsonString(string json, string key)
        {
            Match match = Regex.Match(
                json,
                "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"",
                RegexOptions.IgnoreCase
            );

            return match.Success ? UnescapeJson(match.Groups[1].Value) : "";
        }

        private int ParseInt(string value, int fallback)
        {
            int parsed;
            return int.TryParse(value, out parsed) ? parsed : fallback;
        }

        private bool ParseBool(string value, bool fallback)
        {
            bool parsed;
            if (bool.TryParse(value, out parsed))
            {
                return parsed;
            }

            if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "y", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "n", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return fallback;
        }

        private string SanitizeMultiHome(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "0.0.0.0" : value.Trim();
        }

        private string EscapeJson(string value)
        {
            return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private string UnescapeJson(string value)
        {
            return (value ?? "").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        private string EmptyToFallback(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        internal const int CTRL_C_EVENT = 0;

        [DllImport("kernel32.dll")]
        internal static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handlerRoutine, bool add);

        delegate bool ConsoleCtrlDelegate(uint ctrlType);

        private static bool SendStopSignal(Process p)
        {
            if (!AttachConsole((uint)p.Id))
            {
                return false;
            }

            SetConsoleCtrlHandler(null, true);

            try
            {
                if (!GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0))
                {
                    return false;
                }

                p.WaitForExit(10000);
                return true;
            }
            finally
            {
                SetConsoleCtrlHandler(null, false);
                FreeConsole();
            }
        }

        private class WindroseWorldSummary
        {
            public string IslandId { get; set; }
            public string WorldName { get; set; }
            public string WorldPresetType { get; set; }
        }
    }
}