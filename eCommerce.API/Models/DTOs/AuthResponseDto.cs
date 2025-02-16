using System.Text.Json.Serialization;

public class AuthResponseDto
{
    public string IdToken { get; set; }
    public string RefreshToken { get; set; }
    public string ExpiresIn { get; set; }
}

public class FirebaseAuthResponse
{
    [JsonPropertyName("idToken")]
    public string IdToken { get; set; }

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; }

    [JsonPropertyName("expiresIn")]
    public string ExpiresIn { get; set; }

    [JsonPropertyName("localId")]
    public string LocalId { get; set; }
}

public class FirebaseErrorResponse
{
    public FirebaseError Error { get; set; }
}

public class FirebaseError
{
    public string Message { get; set; }
}