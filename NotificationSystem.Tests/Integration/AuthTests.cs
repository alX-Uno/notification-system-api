using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NotificationSystem.Tests.Integration.Helpers;

namespace NotificationSystem.Tests.Integration;

// IClassFixture comparte una sola instancia de la factory entre todas las pruebas
// de esta clase — evita levantar la app una vez por prueba
public class AuthTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_ConCredencialesValidas_RetornaToken()
    {
        // Arrange
        var payload = new { Username = "admin", Password = "Password123$" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", payload);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.TryGetProperty("token", out var token));
        Assert.NotEmpty(token.GetString()!);
    }

    [Fact]
    public async Task Login_ConCredencialesInvalidas_Retorna401()
    {
        var payload = new { Username = "admin", Password = "wrongpassword" };

        var response = await _client.PostAsJsonAsync("/api/auth/login", payload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EndpointProtegido_SinToken_Retorna401()
    {
        // Sin configurar Authorization header
        var response = await _client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EndpointProtegido_ConTokenValido_Retorna200()
    {
        // Arrange — primero hacer login para obtener un token real
        var loginPayload = new { Username = "admin", Password = "Password123$" };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginPayload);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginBody.GetProperty("token").GetString()!;

        // Act — usar el token en el endpoint protegido
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}