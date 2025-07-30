using System.Net;
using Lestaly;
#nullable enable

var vwSettings = new
{
    // Vaultwarden service
    Service = new
    {
        // Vaultwarden URL
        Url = "http://localhost:8190",
    },

    Setup = new
    {
        Admin = new
        {
            Password = "admin-pass",
        },

        TestUser = new
        {
            Mail = "tester@myserver.home",
            Password = "tester-password",
        },

        TestOrg = new
        {
            Name = "TestOrg",
            Collections = new[]
            {
                "Collec1",
                "Collec2",
            },
        },
    },

    TestEntitiesFile = ThisSource.RelativeFile(".vw-test-entities.json"),
};

record TestOrganization(string Id, string ClientId, string ClientSecret);
record TestConfirmer(string Id, string ClientId, string ClientSecret);
record TestCollection(string Id, string Name);
record TestEntities(TestOrganization Organization, TestCollection[] Collections, TestConfirmer Confirmer);
