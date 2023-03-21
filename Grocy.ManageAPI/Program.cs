using Grocy.ManageAPI;
using Grocy.ManageAPI.Services;
using Grocy.RestAPI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("Grocy", httpClient =>
{
    httpClient.BaseAddress = new Uri(AppSettings.GrocyInstanceUrl);
    httpClient.DefaultRequestHeaders.Add("GROCY-API-KEY", AppSettings.GrocyApiKey);
});

builder.Services.AddScoped<ChoesApi>();
builder.Services.AddScoped<CategoryChoreManager>();

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
