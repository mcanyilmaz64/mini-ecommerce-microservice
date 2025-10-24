using MiniETicaret.Orders.WebAPI.Context;
using MiniETicaret.Orders.WebAPI.Dtos;
using MiniETicaret.Orders.WebAPI.Models;
using MiniETicaret.Orders.WebAPI.Options;
using MongoDB.Driver;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

MongoDB.Bson.Serialization.BsonSerializer.RegisterSerializer(
    typeof(Guid),
    new MongoDB.Bson.Serialization.Serializers.GuidSerializer(MongoDB.Bson.BsonType.String)
);


builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<MongoDbContext>();

var app = builder.Build();

//app.MapGet("/", () => "Hello World!");
app.MapGet("/", async (IConfiguration configuration) =>
{
    HttpClient httpClient = new HttpClient();
    string productEndpoint = $"http://{configuration.GetSection("HttpRequest:Products").Value}/getall";

    var message = await httpClient.GetAsync(productEndpoint);
    var json = await message.Content.ReadAsStringAsync();

    return Results.Content(json, "application/json");
});
app.MapDelete("/delete-all", async (MongoDbContext context) =>
{
    var collection = context.GetCollection<Order>("Orders");
    var result = await collection.DeleteManyAsync(_ => true);

    return Results.Ok(new Result<string>($"{result.DeletedCount} adet kayıt silindi"));
});



app.MapGet("/getall",
    async (MongoDbContext context, IConfiguration configuration) =>
    {
        var items = context.GetCollection<Order>("Orders");
        var orders = await items.Find(item => true).ToListAsync();

        List<OrderDto> orderDtos = new List<OrderDto>();

        Result<List<ProductDto>>? products = new();

        HttpClient httpClient = new HttpClient();

        string productEndpoint =
            $"http://{configuration.GetSection("HttpRequest:Products").Value}/getall";

        var message = await httpClient.GetAsync(productEndpoint);

        if (message.IsSuccessStatusCode)
        {
            products = await message.Content
                .ReadFromJsonAsync<Result<List<ProductDto>>>();
        }

        foreach (var order in orders)
        {
            var product = products?.Data?
     .FirstOrDefault(p => p.Id == order.ProductId);

            OrderDto orderDto = new()
            {
                Id = order.Id,
                CreateAt = order.CreatAt,
                ProductId = order.ProductId,
                Quantity = order.Quantity,
                Price = order.Price,
                ProductName = product?.Name ?? "Ürün bulunamadı"
            };


            orderDtos.Add(orderDto);
        }

        return Results.Ok(new Result<List<OrderDto>>(orderDtos));
    });



app.MapPost("/create", async (MongoDbContext context, List<CreateOrderDto> request) =>
{
    var items = context.GetCollection<Order>("Orders");
    List<Order> orders = new List<Order>();
    foreach (var item in request)
    {
        orders.Add(new Order
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            Price = item.Price,
            CreatAt = DateTime.Now
        });
    }
    await items.InsertManyAsync(orders);
    return Results.Ok(new Result<string>("Sipariş başarıyla oluşturuldu"));
});
app.Run();
