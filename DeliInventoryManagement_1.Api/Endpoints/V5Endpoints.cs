namespace DeliInventoryManagement_1.Api.Endpoints;

public static class V5Endpoints
{
    public static void MapV5Endpoints(this WebApplication app)
    {
        var v5 = app.MapGroup("/api/v5");
        v5.MapV5Products();
        v5.MapV5Sales();
    }
}
