using ConferenceBooking.Api.AspNetCore.DependencyInjection;
using ConferenceBooking.Api.AspNetCore.Hosting;
using ConferenceBooking.Core.Application.DependencyInjection;
using ConferenceBooking.Data.Postgresql.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddApi();
services.AddApplication();
services.AddApiAuthentication(configuration, builder.Environment);
services.AddDbContext(configuration);

var app = builder.Build();

app.UseApi();

await app.RunAsync();

namespace ConferenceBooking.Api.AspNetCore
{
    /// <summary>
    /// Application entry point, exposed for integration tests.
    /// </summary>
    public partial class Program;
}
