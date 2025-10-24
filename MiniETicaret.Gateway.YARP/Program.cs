using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MiniETicaret.Gateway.YARP.Context;
using MiniETicaret.Gateway.YARP.Dtos;
using MiniETicaret.Gateway.YARP.Models;
using MiniETicaret.Gateway.YARP.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSql"));
});

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration.GetSection("JWT:Issuer").Value,
        ValidAudience = builder.Configuration.GetSection("JWT:Audience").Value,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("JWT:SecretKey").Value ?? "") ),
        ValidateLifetime = true
    };


});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors(x=>x.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());

app.MapGet("/", () => "Hello World!");

app.MapPost("/auth/register", async (RegisterDto request ,ApplicationDbContext context, CancellationToken cancellationToken) =>
{
    bool isUserNameExist = await context.Users.AnyAsync(p => p.UserName == request.UserName, cancellationToken);
    if (isUserNameExist)
    {
        return Results.BadRequest(new Result<string>("Kullanýcý adý daha önce alýnmýþ"));
    }
    User user = new()
    {
        UserName = request.UserName,
        Password = request.Password
    };
    await context.AddAsync(user, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);
    return Results.Ok( new Result<string>("Kullancý kaydý baþarýlý"));
});

app.MapPost("/auth/login", async (LoginDto request, ApplicationDbContext context, CancellationToken cancellationToken) =>
{
   User? user = await context.Users.FirstOrDefaultAsync(p => p.UserName == request.UserName, cancellationToken);

    if (user is null)
    {
        return Results.BadRequest(new Result<string>("Kullanýcý bulunamadý"));
    }
    JwtProvider jwtProvider = new(builder.Configuration);

    string token = jwtProvider.CreateToken(user);

    return Results.Ok(new Result<string>(token));
});

app.UseAuthentication();

app.UseAuthorization();

app.MapReverseProxy();



using (var scope = app.Services.CreateScope())
{
    var srv = scope.ServiceProvider;
    var context = srv.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
