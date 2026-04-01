using HealthChecks.UI.Client;
using LinkSummary.Api.AppStart;
using LinkSummary.Api.AppStart.Extensions;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

/*builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // The app is behind a reverse proxy, so do not restrict forwarded headers to loopback only.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});*/

var startup = new Startup(builder);
startup.Initialize();

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{    
    app.ApplyCors();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions 
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
