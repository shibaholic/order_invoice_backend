using HospitalSupply.Repositories;

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
    throw new NotImplementedException("Need to implement production environment connection string");
}

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("No connection string found.");
    System.Environment.Exit(1);
}

builder.Services.AddScoped<IDatabase>(provider => new Database(connectionString));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IItemOrderRepository, ItemOrderRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();