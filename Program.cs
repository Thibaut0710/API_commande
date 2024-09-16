using API_Commande.Context;
using API_Commande.Service;
using ConsumerAPI;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddDbContext<CommandeContext>(options =>
    options.UseMySql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            new MariaDbServerVersion(new Version(10, 11, 6)),
            optionsBuilder => optionsBuilder.EnableRetryOnFailure()
        )
    );

builder.Services.AddControllers();
builder.Services.AddScoped<CommandeService>();
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();
builder.Services.AddSingleton<RabbitMQConsumer>();
builder.WebHost.UseUrls("http://0.0.0.0:5239");
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
app.Services.GetRequiredService<IRabbitMQService>().CreateConsumerCommandeID();
app.Services.GetRequiredService<IRabbitMQService>().CreateConsumerCommandeIDProduits();
app.Run();
