using HealthwatcherApi.Presentation.Config;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwagger();
builder.Services.AddProjectServices(builder.Configuration);

WebApplication app = builder.Build();

// First in, last out: this has to wrap every middleware below it to catch them.
app.UseExceptionHandling();

app.UseSwaggerIfDev(app.Environment);
app.UseHttpsRedirection();
app.UseCors(ServicesConfig.SpaCorsPolicy);

// No provider is registered yet — add one in ServicesConfig and these start doing work.
app.UseAuthentication();
app.UseAuthorization();

// After authentication, so there is an identity for the audit columns to record.
app.UseRequestContext();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>Exposed so WebApplicationFactory&lt;Program&gt; can boot the app in tests.</summary>
public partial class Program;
