using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace eCommerce.Tests.Helpers;

public static class TestTokenHelper
{
    private static readonly FirebaseAuth _auth;

    static TestTokenHelper()
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile("firebase-config.json")
            });
        }
        _auth = FirebaseAuth.DefaultInstance;
    }

    public static async Task<string> GenerateTestToken(string uid = "test-user-id")
    {
        var customClaims = new Dictionary<string, object>
        {
            { "user_id", uid },
            { "email", "test@example.com" }
        };

        return await _auth.CreateCustomTokenAsync(uid, customClaims);
    }
}