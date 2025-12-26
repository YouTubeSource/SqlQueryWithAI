using Azure;
using Azure.AI.OpenAI;
using Microsoft.Data.SqlClient;
using SqlQueryWithAi.WebApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
// Configure CORS for Blazor WebAssembly
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7001", 
                "http://localhost:5001",
                "https://localhost:5173", // Vite default port
                "http://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configure Azure OpenAI Client
var openAIEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
var openAIKey = builder.Configuration["AzureOpenAI:ApiKey"];

builder.Services.AddSingleton(new OpenAIClient(
    new Uri(openAIEndpoint!),
    new AzureKeyCredential(openAIKey!)));

// Configure SQL Connection
var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddScoped<SqlConnection>(_ => new SqlConnection(sqlConnectionString));



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazor");

app.MapHub<QueryHub>("/queryhub");

app.Run();
