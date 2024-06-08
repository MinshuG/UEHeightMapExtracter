using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HeightMapExtractorGUI.Internet;

// https://fortnitecentral.genxgames.gg/api/v1/aes
public class GenxGames
{
    static readonly string _baseUrl = "https://fortnitecentral.genxgames.gg/api/v1";
    static readonly string _aesEndpoint = "/aes";
    static readonly string _mappingsEndpoint = "/mappings";
    
    public static async Task<AesKeyResponse> GetAesKeysAsync(string? version)
    {
        using var client = new HttpClient(new RetryHandler(new HttpClientHandler()));
        client.Timeout = TimeSpan.FromSeconds(5);

        var url = $"{_baseUrl}{_aesEndpoint}";
        if (version != null) url += $"?version={version}";
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<AesKeyResponse>(content) ?? throw new IOException("received invalid response from server.");
    }
    
    public partial class AesKeyResponse
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("mainKey")]
        public string MainKey { get; set; }

        [JsonProperty("dynamicKeys")]
        public DynamicKey[] DynamicKeys { get; set; }

        [JsonProperty("unloaded")]
        public Unloaded[] Unloaded { get; set; }
    }

    public partial class DynamicKey
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("guid")]
        public string Guid { get; set; }

        [JsonProperty("keychain")]
        public string Keychain { get; set; }

        [JsonProperty("fileCount")]
        public long FileCount { get; set; }

        [JsonProperty("hasHighResTextures")]
        public bool HasHighResTextures { get; set; }

        [JsonProperty("size")]
        public Size Size { get; set; }
    }

    public partial class Size
    {
        [JsonProperty("raw")]
        public long Raw { get; set; }

        [JsonProperty("formatted")]
        public string Formatted { get; set; }
    }

    public partial class Unloaded
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("guid")]
        public string Guid { get; set; }

        [JsonProperty("fileCount")]
        public long FileCount { get; set; }

        [JsonProperty("hasHighResTextures")]
        public bool HasHighResTextures { get; set; }

        [JsonProperty("size")]
        public Size Size { get; set; }
    }
}