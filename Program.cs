using HospitalSupply.Repositories;
using HospitalSupply.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Controllers
builder.Services.AddControllers();

// Services

// Repository
string? connectionString;
if (System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
    connectionString = builder.Configuration.GetConnectionString("LocalDatabase");
}
else
{
    connectionString = builder.Configuration["Secrets:ProdDatabase"];
    Console.WriteLine($"prod connection string: {connectionString}");
    // throw new NotImplementedException("Need to implement production environment connection string");
}

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("No connection string found.");
    System.Environment.Exit(1);
}

// Services
string? uiPathPAT = builder.Configuration["Secrets:UiPathPAT"];
string? apiTriggerSlug = builder.Configuration["Secrets:ApiTriggerSlug"];
if (string.IsNullOrEmpty(uiPathPAT) || string.IsNullOrEmpty(apiTriggerSlug))
{
    Console.WriteLine("No UiPath PAT found, or no ApiTriggerSlug found.");
    System.Environment.Exit(1);
}

builder.Services.AddScoped<IUiPathApiClient>(provider => new UiPathApiClient(uiPathPAT, apiTriggerSlug));

// Repository
builder.Services.AddScoped<IDatabase>(provider => new Database(connectionString));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IItemOrderRepository, ItemOrderRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(builder => builder
        .WithOrigins("http://localhost:5173")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    
}

app.UseStaticFiles(); 

app.UseSwagger(c =>
{
    c.RouteTemplate = "api/swagger/{documentName}/swagger.json"; // Sets the route for Swagger JSON
});

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/api/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = "api/swagger"; // Sets the prefix for Swagger UI
});

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();