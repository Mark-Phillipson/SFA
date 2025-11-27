using SFA_WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add CORS policy for Blazor frontend
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		   policy.WithOrigins(
			   "https://localhost:5158",
			   "https://polite-sand-0eb4b4703.3.azurestaticapps.net"
		 		   )
		.AllowAnyHeader()
		.AllowAnyMethod();
	});
});

// Add services to the container.

builder.Services.AddControllers();
// Add Swagger/OpenAPI support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<SFA_WebAPI.Services.OpenAIBotService>();
// Register StartPoint repository (JSON file backed)
builder.Services.AddSingleton<SFA_WebAPI.Services.IStartPointRepository, SFA_WebAPI.Services.StartPointRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.

// Enable OpenAPI
app.MapOpenApi();

// Enable CORS before routing/authorization
app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
