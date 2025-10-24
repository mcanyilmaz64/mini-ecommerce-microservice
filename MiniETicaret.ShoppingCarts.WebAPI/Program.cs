using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniETicaret.ShoppingCarts.WebAPI.Context;
using MiniETicaret.ShoppingCarts.WebAPI.Dtos;
using MiniETicaret.ShoppingCarts.WebAPI.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL.ValueGeneration.Internal;
using System.Text;
using System.Text.Json;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"));
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/getall", async (ApplicationDbContext context,IConfiguration configuration, CancellationToken cancellationToken) =>
{
    List<ShoppingCart> shoppingCarts = await context.ShoppingCarts.ToListAsync(cancellationToken);

    HttpClient httpClient = new HttpClient();
    string productEndpoint =
        $"http://{configuration.GetSection("HttpRequest:Products").Value}/getall";
    var message = await httpClient.GetAsync(productEndpoint);


    Result<List<ProductDto>>? products=new();
    if (message.IsSuccessStatusCode)
    {
        products = await message.Content.ReadFromJsonAsync<Result<List<ProductDto>>>();
        
    }

    List<ShoppingCartDto> response = shoppingCarts.Select(s =>
    {
        var product = products?.Data?.FirstOrDefault(p => p.Id == s.ProductId);

        return new ShoppingCartDto()
        {
            Id = s.Id,
            ProductId = s.ProductId,
            Quantity = s.Quantity,
            ProductName = product?.Name ?? "Ürün Bulunamadý",
            ProductPrice = product?.Price ?? 0
        };
    }).ToList();

    return new Result<List<ShoppingCartDto>>(response);
});
app.MapPost("/create", async (CreateShoppingCartDto request, ApplicationDbContext context, CancellationToken cancellationToken) =>
{
    ShoppingCart shoppingCart = new()
    {
        ProductId = request.ProductId,
        Quantity = request.Quantity
    };

    await context.AddAsync(shoppingCart, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);

    return Results.Ok(new Result<string>("Ürün Sepete Baþarýyla Eklendi"));
});
app.MapGet("/createOrder", async (ApplicationDbContext context, IConfiguration configuration, CancellationToken cancellationToken) =>
{
List<ShoppingCart> shoppingCarts = await context.ShoppingCarts.ToListAsync(cancellationToken);

HttpClient httpClient = new HttpClient();
string productEndpoint =
    $"http://{configuration.GetSection("HttpRequest:Products").Value}/getall";
var message = await httpClient.GetAsync(productEndpoint);


Result<List<ProductDto>>? products = new();
if (message.IsSuccessStatusCode)
{
    products = await message.Content.ReadFromJsonAsync<Result<List<ProductDto>>>();

}

    List<CreateOrderDto> response = shoppingCarts.Select(s =>
    {
        var product = products?.Data?.FirstOrDefault(p => p.Id == s.ProductId);

        return new CreateOrderDto()
        {
            ProductId = s.ProductId,
            Quantity = s.Quantity,
            Price = product?.Price ?? 0
        };
    }).ToList();

    string ordersEndpoint =
        $"http://{configuration.GetSection("HttpRequest:Orders").Value}/create";

    string stringJson = JsonSerializer.Serialize(response);

    var content = new StringContent(stringJson,Encoding.UTF8,"application/json");

    var orderMessage = await httpClient.PostAsync(ordersEndpoint, content);

    if (orderMessage.IsSuccessStatusCode)
    {
        List<ChangeProductStockDto> changeProductStockDtos = shoppingCarts.Select(s => new ChangeProductStockDto(s.ProductId, s.Quantity)).ToList();
   
        productEndpoint =$"http://{configuration.GetSection("HttpRequest:Products").Value}/change-product-stock";

        string productsStringJson = JsonSerializer.Serialize(changeProductStockDtos);

        var productsContent = new StringContent(productsStringJson, Encoding.UTF8, "application/json");

        await httpClient.PostAsync(productEndpoint, productsContent);

        context.RemoveRange(shoppingCarts);

        await context.SaveChangesAsync(cancellationToken);
    }
    return Results.Ok(new Result<string>("Sipariþ baþarýyla oluþturuldu"));

});

using (var scoped = app.Services.CreateScope())
{
    var srv = scoped.ServiceProvider;
    var context = srv.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

    app.Run();
