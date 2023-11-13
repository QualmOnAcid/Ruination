using DiscordRPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebviewAppShared.Utils
{
    public class DiscordRPC
    {
        private static DiscordRpcClient _client;
        private static ulong _id = 0;

        public static async Task Load()
        {
            Logger.Log("Loading Discord RPC");
            _client = new DiscordRpcClient("1048552044555927593");

            _client.OnReady += async delegate
            {
                Logger.Log("Discord RPC ready");
                _id = _client.CurrentUser.ID;
                Logger.Log("Found user id: " + GetID());
            };

            _client.Initialize();

            Logger.Log ("Loaded Discord RPC");

            for (int i = 0; i < 7; i++)
            {
                Logger.Log("Waiting for Discord RPC " + (i+1));
                if (GetID() != 0) break;
                await Task.Delay(1 * 1000);
            }
        }

        public static void UpdatePresence(string state)
        {
            Logger.Log("Updating RPC: " + state);
            _client.SetPresence(new()
            {
                Details = "Ruination Swapper",
                State = state,
                Buttons = new Button[] { new() { Label = "Discord", Url = API.GetApi().Other.Discord } },
                Assets = new()
                {
                    LargeImageKey = "ruination",
                    LargeImageText = "Fortnite Skinchanger"
                }
            });
        }

        public static ulong GetID() => _id;

    }
}
