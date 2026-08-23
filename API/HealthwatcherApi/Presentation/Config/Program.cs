using HealthwatcherApi.Presentation.Config;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwagger();
builder.Services.AddProjectServices(builder.Configuration);

WebApplication app = builder.Build();

app.UseExceptionHandling();

app.UseSwaggerIfDev(app.Environment);

// Only in development. In the cluster the pod listens on plain HTTP behind the
// frontend's nginx, so redirecting to https would break every proxied call.
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors(ServicesConfig.SpaCorsPolicy);

// No provider is registered yet — add one in ServicesConfig and these start doing work.
app.UseAuthentication();
app.UseAuthorization();

app.UseRequestContext();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// so WebApplicationFactory<Program> can boot this in integration tests
public partial class Program;
