using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ruination_v2.Utils;

public class DiscordOAuth2
{
    
    public string CLIENT_ID { get; set; }
    public string SECRET_ID { get; set; }
    public string GUILD_ID { get; set; }
    public List<string> Roles { get; set; }
    private TokenObject _tokenObject { get; set; }

    public bool finished = false;

    public DiscordOAuth2(string client_id, string secret_id, string guild_id, List<string> roles)
    {
        this.CLIENT_ID = client_id;
        this.SECRET_ID = secret_id;
        this.GUILD_ID = guild_id;
        this.Roles = roles;
        _tokenObject = new();
    }

    public void Authenticate()
    {
        string url =
            "https://discord.com/api/oauth2/authorize?client_id=1046483495096160306&redirect_uri=http%3A%2F%2Flocalhost%3A9431%2Fauth%2Fredirect&response_type=code&scope=guilds.members.read";
        Process process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = url
        };
        process.Start();
        HttpListener httpListener = new();
        httpListener.Prefixes.Add("http://localhost:9431/auth/redirect/");
        httpListener.Start();
        while (_tokenObject.access_token == null)
        {
            var context = httpListener.GetContext();
            if (!string.IsNullOrEmpty(context.Request.Url.Query))
            {
                NameValueCollection v = HttpUtility.ParseQueryString(context.Request.Url.Query);
                if (v["code"] == null) continue;
                string code = v["code"];
                _tokenObject.access_token = GetAccessToken(code);
                finished = true;
            }
        }

        httpListener.Stop();
    }

    public bool HasRole()
    {
        bool flag = false;
        var guildMember = GetGuildMember();
        if (guildMember == null)
        {
            Utils.StartUrl(API.GetApi().Other.Discord);
            return false;
        }

        Utils.CurrentUser = new()
        {
            Name = guildMember.user.global_name,
            AvatarUrl = $"https://cdn.discordapp.com/avatars/{guildMember.user.id}/{guildMember.user.avatar}.png"
        };
        
        foreach (var role in Roles)
        {
            if (guildMember.roles.Contains(role))
            {
                flag = true;
                break;
            }
        }
        return flag;
    }

    private string GetAccessToken(string code)
    {
        HttpClient httpClient = new HttpClient();
        KeyValuePair<string, string>[] nameValueCollection = new KeyValuePair<string, string>[5]
        {
            new KeyValuePair<string, string>("client_id", CLIENT_ID),
            new KeyValuePair<string, string>("client_secret", SECRET_ID),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", "http://localhost:9431/auth/redirect"),
            new KeyValuePair<string, string>("grant_type", "authorization_code")
        };
        HttpResponseMessage result = httpClient.PostAsync("https://discord.com/api/oauth2/token", new FormUrlEncodedContent(nameValueCollection)).GetAwaiter().GetResult();
        string result2 = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        dynamic d = JObject.Parse(result2);
        return d.access_token.ToString();
    }

    private GuildMemberObject GetGuildMember()
    {
        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokenObject.access_token);
        string requestUri = "https://discord.com/api/users/@me/guilds/" + GUILD_ID + "/member";
        HttpResponseMessage result = httpClient.GetAsync(requestUri).GetAwaiter().GetResult();
        string result2 = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (!result.IsSuccessStatusCode)
        {
            return null;
        }
        return JsonConvert.DeserializeObject<GuildMemberObject>(result2);
    }
    
}

public class TokenObject
{
    public string access_token { get; set; }
}

public class GuildMemberObject
{
    public string[] roles { get; set; }
    public GuildMemberUserObject user { get; set; }
}

public class GuildMemberUserObject
{
    public string id;
    public string avatar;
    public string global_name { get; set; }
}