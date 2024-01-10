using OtpNet;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace Flow.Launcher.Plugin.OneTimePassword
{
    public record Setting
    {
        [JsonPropertyName("otps")]
        public List<Otp> Otps { get; set; } = new();
    }

    public record Otp(
        [property: JsonPropertyName("uri")] string Uri,
        [property: JsonPropertyName("label")] string Label,
        [property: JsonPropertyName("secret")] string Secret,
        [property: JsonPropertyName("algorithm")][property: JsonConverter(typeof(AlgorithmConverter))] OtpHashMode Algorithm,
        [property: JsonPropertyName("digits")] int Digits,
        [property: JsonPropertyName("period")] int Period,
        [property: JsonPropertyName("icon")] string? Icon
    )
    {
        public static bool ValidateUri(string uri)
        {
            if (string.IsNullOrEmpty(uri)) return false;

            var u = new Uri(uri);
            if (u.Scheme != "otpauth") return false;

            var queries = HttpUtility.ParseQueryString(u.Query);
            if (queries == null || queries.Count == 0) return false;

            var type = u.Host.ToLower();
            var label = u.Segments[1].TrimEnd('/');
            var secret = queries["secret"];

            if (type != "totp") return false;
            if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(secret)) return false;

            return true;
        }

        public static string GetOtpLabel(string uri) => new Uri(uri).Segments[1].TrimEnd('/');

        public static OtpHashMode? StringToOtpHashMode(string? value) => value?.ToLower() switch
        {
            "sha1" => OtpHashMode.Sha1,
            "sha256" => OtpHashMode.Sha256,
            "sha512" => OtpHashMode.Sha512,
            _ => null,
        };

        public static string? OtpHashModeToString(OtpHashMode value) => value switch
        {
            OtpHashMode.Sha1 => "sha1",
            OtpHashMode.Sha256 => "sha256",
            OtpHashMode.Sha512 => "sha512",
            _ => null,
        };

        public Otp() : this("", "", "", OtpHashMode.Sha1, 6, 30, null) { }

        public Otp(string uri) : this(uri, "", "", OtpHashMode.Sha1, 6, 30, null)
        {
            var u = new Uri(uri);
            var queries = HttpUtility.ParseQueryString(u.Query)!;

            Label = u.Segments[1].TrimEnd('/');
            Secret = queries["secret"]!;
            Algorithm = StringToOtpHashMode(queries["algorithm"]) ?? OtpHashMode.Sha1;

            if (int.TryParse(queries["digits"], out int digits))
                Digits = digits;

            if (int.TryParse(queries["period"], out int period))
                Period = period;
        }

        public (string, int) Compute()
        {
            var totp = new Totp(Base32Encoding.ToBytes(Secret), Period, Algorithm, Digits);

            return (totp.ComputeTotp(), totp.RemainingSeconds());
        }
    }

    public class AlgorithmConverter : JsonConverter<OtpHashMode>
    {
        public override OtpHashMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            return Otp.StringToOtpHashMode(str) ?? throw new JsonException($"Cannot convert string `{str}` to enum.");
        }

        public override void Write(Utf8JsonWriter writer, OtpHashMode mode, JsonSerializerOptions options)
        {
            var str = Otp.OtpHashModeToString(mode) ?? throw new JsonException($"Cannot convert enum `{mode}` to string.");
            writer.WriteStringValue(str);
        }
    }
}
