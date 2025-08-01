using Duende.IdentityServer.Models;

namespace IdentityService;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        [
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
        ];

    public static IEnumerable<ApiScope> ApiScopes =>
        [
            new ApiScope("auctionApp", "Auction Application API Scope"),
        ];

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // m2m client credentials flow client
            new Client
            {
                ClientId = "postman",
                ClientName = "Postman Client",
                AllowedScopes = [ "openid", "profile", "auctionApp" ],

                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                ClientSecrets = [ new Secret("NotASecret".Sha256()) ]
            },

            // interactive client using code flow + pkce
            new Client
            {
                ClientId = "nextApp",
                ClientName = "nextjs client",
                ClientSecrets = { new Secret("secret".Sha256()) },
                AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                RedirectUris = [ "https://localhost:3000/api/auth/callback/id-server/" ],
                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "scope2" },
                RequirePkce = false
            },
        };
}
