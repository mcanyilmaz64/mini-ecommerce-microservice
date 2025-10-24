using Bogus;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using MiniETicaret.Products.WebAPI.Context;
using MiniETicaret.Products.WebAPI.Dtos;
using MiniETicaret.Products.WebAPI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
}

);

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/seedData", (ApplicationDbContext context) =>
{
    for (int i = 0; i < 100; i++)
    {
        Faker faker = new();
        Product product = new()
        {
            Name = faker.Commerce.ProductName(),
            Price = Convert.ToDecimal(faker.Commerce.Price()),
            Stock = faker.Commerce.Random.Int(1, 100)
        };

        context.Products.Add(product);
    }

    context.SaveChanges();

    var response = new Result<string>("Data baþarýyla oluþturuldu");
    return Results.Ok(response);
});


app.MapGet("/getall", async (ApplicationDbContext context, CancellationToken cancellationToken) =>
{
    var products = await context.Products.OrderBy(p => p.Name).ToListAsync(cancellationToken);

    Result<List<Product>> response = new Result<List<Product>>
    {
        Data = products
    };

    return Results.Ok(response);
});
app.MapDelete("/delete-all", async (ApplicationDbContext context, CancellationToken cancellationToken) =>
{
    var products = await context.Products.ToListAsync(cancellationToken);

    if (!products.Any())
    {
        return Results.Ok(new Result<string>("Silinecek ürün bulunamadý."));
    }

    context.Products.RemoveRange(products);
    await context.SaveChangesAsync(cancellationToken);

    return Results.Ok(new Result<string>($"{products.Count} ürün silindi."));
});

app.MapPost("/create", async (CreateProductDto request, ApplicationDbContext context, CancellationToken cancellationToken) =>
{
    bool isNameExist = await context.Products.AnyAsync(p => p.Name == request.Name, cancellationToken);

    if (isNameExist)
    {
        // Hatalarý da Result<string> ile döndürmek istersen:
        var errorResponse = new Result<string>("Bu isimde bir ürün zaten var.");
        return Results.BadRequest(errorResponse);
    }

    Product product = new()
    {
        Name = request.Name,
        Price = request.Price,
        Stock = request.Stock,
    };

    await context.AddAsync(product, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);

    // Burada ürün eklenince Result<Product> ile sarýp dönüyoruz
    var response = new Result<Product>(product);
    return Results.Ok(response);
});

app.MapPost("/change-product-stock", async (List<ChangeProductStockDtos> request, ApplicationDbContext context, CancellationToken cancellationToken) =>
{
    foreach (var item in request)
    {
        Product? product = await context.Products.FindAsync(item.ProductId, cancellationToken);
        if (product is not null)

        {
            product.Stock -= item.Quantity;

        
    }
    }
    await context.SaveChangesAsync(cancellationToken);
    return Results.NoContent();


});


using (var scoped = app.Services.CreateScope())
{
    var srv = scoped.ServiceProvider;
    var context = srv.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}
app.Run();
