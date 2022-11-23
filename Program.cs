using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using tourmaline.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Add services to the container.

services.AddControllersWithViews();
services.Configure<FormOptions>(options => { options.MemoryBufferThreshold = int.MaxValue; });
services.AddCors(o => o.AddPolicy("AllowLocalDebug",
	builder =>
	{
		builder.WithOrigins("https://localhost:3000")
				.AllowAnyMethod()
				.AllowAnyHeader()
				.SetIsOriginAllowed((host) => true)
				.AllowCredentials();
	}));

#region Setup connection

var connString = configuration["ConnectionStrings:DefaultConnection"];

var connHostNameEnv = Environment.GetEnvironmentVariable("MYSQL_SERVICE_HOST");
var connHostPortEnv = Environment.GetEnvironmentVariable("MYSQL_SERVICE_PORT");

if ((connHostNameEnv != null) && (connHostPortEnv != null))
{
    connString = $"Server={connHostNameEnv};User ID=root;Password=qwertyuiop;Port={connHostPortEnv};Database=tourmaline";
}

services.AddSingleton(_ => new DbConnection(connString));

#endregion

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer((o) =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});
services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
if (!app.Environment.IsDevelopment())
{
    app.UseCors("AllowLocalDebug");
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();
