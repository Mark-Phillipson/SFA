using SFA_WebAPI.Services;
var builder = WebApplication.CreateBuilder(args);

// Add CORS policy for Blazor frontend
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy.WithOrigins("http://localhost:5158") // Blazor PWA dev URL
			  .AllowAnyHeader()
			  .AllowAnyMethod();
	});
});

// Add services to the container.

builder.Services.AddControllers();
// Add Swagger/OpenAPI support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<SFA_WebAPI.Services.OpenAIBotService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI();

// Enable CORS before routing/authorization
app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
