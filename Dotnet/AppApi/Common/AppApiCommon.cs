using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using System.Text.RegularExpressions;


namespace VRCX
{
    public partial class AppApi
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public void Init()
        {
        }

        public JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            Error = delegate (object _, Newtonsoft.Json.Serialization.ErrorEventArgs args)
            {
                args.ErrorContext.Handled = true;
            }
        };

        public int GetColourFromUserID(string userId)
        {
            using var hasher = MD5.Create();
            var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(userId));
            return (hash[3] << 8) | hash[4];
        }
        private static string smol_link_fix(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            string key = null;

            var matchStream = Regex.Match(
                url,
                @"^(rtmp|rtspt)://stream\.vrcdn\.live/live/(.+)$",
                RegexOptions.IgnoreCase
            );

            if (matchStream.Success)
            {
                key = matchStream.Groups[2].Value;
            }
            else
            {
                var matchHttp = Regex.Match(
                    url,
                    @"^https?://stream\.vrcdn\.live/live/(.+)$",
                    RegexOptions.IgnoreCase
                );

                if (!matchHttp.Success)
                    return null;

                key = matchHttp.Groups[1].Value;
            }

            if (key.EndsWith(".live.ts", StringComparison.OrdinalIgnoreCase))
            {
                key = key.Substring(0, key.Length - ".live.ts".Length);
            }
            else if (key.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
            {
                key = key.Substring(0, key.Length - ".ts".Length);
            }

            return $"https://panel.vrcdn.live/preview/{key}";
        }

        public void OpenLink(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            var fixedUrl = smol_link_fix(url) ?? url;

            if (fixedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                fixedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(new ProcessStartInfo(fixedUrl)
                {
                    UseShellExecute = true
                });
            }
        }

        public string GetLaunchCommand()
        {
            var command = StartupArgs.LaunchArguments.LaunchCommand;
            StartupArgs.LaunchArguments.LaunchCommand = string.Empty;
            return command;
        }

        public void IPCAnnounceStart()
        {
            IPCServer.Send(new IPCPacket
            {
                Type = "VRCXLaunch",
                MsgType = "VRCXLaunch"
            });
        }

        public void SendIpc(string type, string data)
        {
            IPCServer.Send(new IPCPacket
            {
                Type = "VrcxMessage",
                MsgType = type,
                Data = data
            });
        }

        public string CustomCss()
        {
            var filePath = Path.Join(Program.AppDataDirectory, "custom.css");
            if (File.Exists(filePath))
                return File.ReadAllText(filePath);

            return string.Empty;
        }

        public string CustomScript()
        {
            var filePath = Path.Join(Program.AppDataDirectory, "custom.js");
            if (File.Exists(filePath))
                return File.ReadAllText(filePath);

            return string.Empty;
        }

        public string CurrentCulture()
        {
            var culture = CultureInfo.CurrentCulture.ToString();
            if (string.IsNullOrEmpty(culture))
                culture = "en-US";

            return culture;
        }

        public string CurrentLanguage()
        {
            return CultureInfo.InstalledUICulture.Name;
        }

        public string GetVersion()
        {
            return Program.Version;
        }

        public bool VrcClosedGracefully()
        {
            return LogWatcher.Instance.VrcClosedGracefully;
        }

        public Dictionary<string, int> GetColourBulk(List<object> userIds)
        {
            var output = new Dictionary<string, int>();
            foreach (string userId in userIds)
            {
                output.Add(userId, GetColourFromUserID(userId));
            }

            return output;
        }

        public void SetAppLauncherSettings(bool enabled, bool killOnExit, bool runProcessOnce)
        {
            AutoAppLaunchManager.Instance.Enabled = enabled;
            AutoAppLaunchManager.Instance.KillChildrenOnExit = killOnExit;
            AutoAppLaunchManager.Instance.RunProcessOnce = runProcessOnce;
        }

        public string GetFileBase64(string path)
        {
            if (File.Exists(path))
            {
                return Convert.ToBase64String(File.ReadAllBytes(path));
            }

            return null;
        }

        public Task<bool> TryOpenInstanceInVrc(string launchUrl)
        {
            return VRCIPC.Send(launchUrl);
        }
    }
}