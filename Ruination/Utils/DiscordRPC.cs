﻿using System.Threading.Tasks;
using DiscordRPC;

namespace Ruination_v2.Utils;

public class DiscordRPC
{
    private static DiscordRpcClient _client;
    private static ulong _id = 0;
    private static Timestamps stamp;

    public static async Task Load()
    {
        Logger.Log("Loading Discord RPC");
        _client = new DiscordRpcClient("1048552044555927593");

        _client.OnReady += async delegate
        {
            Logger.Log("Discord RPC ready");
            _id = _client.CurrentUser.ID;
            Logger.Log("Found user id: " + GetID());
            UpdatePresence("Idling");
        };

        stamp = Timestamps.Now; 

        _client.Initialize();

        Logger.Log ("Loaded Discord RPC");

        int waitime = 3;

        for (int i = 0; i < waitime; i++)
        {
            if (GetID() != 0) break;
            Logger.Log("Waiting for Discord RPC " + (i + 1));
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
            },
            Timestamps = stamp
        });
    }

    public static ulong GetID() => _id;
}