using API_Commande.Context;
using API_Commande.Service;
using ConsumerAPI;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddHttpClient<CommandeService>(commande =>
{
    commande.BaseAddress = new Uri("https://localhost:7118/api/");
});


builder.Services.AddDbContext<CommandeContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
    new MySqlServerVersion(new Version(8, 0, 26))));
builder.Services.AddScoped<CommandeService>();
builder.Services.AddControllers();
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();
builder.Services.AddSingleton<RabbitMQConsumer>();
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
app.Services.GetRequiredService<IRabbitMQService>().CreateConsumer();
app.Run();
