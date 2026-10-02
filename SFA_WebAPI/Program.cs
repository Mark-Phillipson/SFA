using SFA_WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add CORS policy for Blazor frontend
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		   policy.WithOrigins(
			   "https://localhost:5158",
			   "http://localhost:5158",
			   "http://localhost:7289",
			   "https://polite-sand-0eb4b4703.3.azurestaticapps.net",
			   "https://www.fairieslittlehelper.online",
			   "https://fairieslittlehelper.online"
		 		   )
		.AllowAnyHeader()
		.AllowAnyMethod();
	});
});

// Add services to the container.

builder.Services.AddControllers();
// Add OpenAPI support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Register HttpClient factory for proxy requests
builder.Services.AddHttpClient();
// Register HttpClient for OpenAI bot service with webpage fetching
builder.Services.AddHttpClient<SFA_WebAPI.Services.OpenAIBotService>();
// Register StartPoint repository (JSON file backed)
builder.Services.AddSingleton<SFA_WebAPI.Services.IStartPointRepository, SFA_WebAPI.Services.StartPointRepository>();
builder.Services.AddSingleton<SFA_WebAPI.Services.ILinksCatalogService, SFA_WebAPI.Services.LinksCatalogService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS before routing/authorization
app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
