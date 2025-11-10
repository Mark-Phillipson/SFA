using SFA_WebAPI.Services;
var builder = WebApplication.CreateBuilder(args);

// Add CORS policy for Blazor frontend
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		   policy.WithOrigins(
			   "https://localhost:5158",
			   "https://polite-sand-0eb4b4703.3.azurestaticapps.net"		   )
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
app.UseSwaggerUI(c =>
{
	c.SwaggerEndpoint("/swagger/v1/swagger.json", "SFA WebAPI V1");
	c.RoutePrefix = string.Empty;
	c.ConfigObject.AdditionalItems["validatorUrl"] = null;
	c.ConfigObject.AdditionalItems["url"] = "https://localhost:7289/swagger/v1/swagger.json";
});

// Enable CORS before routing/authorization
app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
