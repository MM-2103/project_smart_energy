using InfluxDB.Client;
using SmartEnergy.Client.Components;
using SmartEnergy.Library.Measurements.Models;
using SmartEnergy.Library.Measurements.Repository;

namespace SmartEnergy.Client;

public static class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        /* TODO: update the value to the ID of your meter. Please note that this meterId is a decimal number and your
        P1 meter is a Hexadecimal number. Thus you need to convert the ID from HEX to Decimal and put the number here. */
        var smartMeterConfiguration = new SmartMeterConfiguration
        {
            MeterId = 5341726
        };

        // Add services to the container.
        builder.Services
            .AddSingleton<IInfluxDBClient, InfluxDBClient>(o =>
            {
                var options = new InfluxDBClientOptions(builder.Configuration.AssertAndGetConfigurationStringValue("InfluxDb:Url"))
                {
                    Token = builder.Configuration.AssertAndGetConfigurationStringValue("InfluxDb:Token"),
                    Org = builder.Configuration.AssertAndGetConfigurationStringValue("InfluxDb:Org")
                };

                return new InfluxDBClient(options);
            })
            .AddSingleton<SmartMeterConfiguration>(o => smartMeterConfiguration)
            .AddTransient<InfluxMeasurementRepository>()
            .AddRazorComponents()
            .AddInteractiveServerComponents();



        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }




        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();
        app.Run();


    }

    /// <summary>
    /// Helps detecting unconfigured application secrets. Throws exception if an unconfigured key is found.
    /// </summary>
    private static string AssertAndGetConfigurationStringValue(this ConfigurationManager configurationManager, string key)
    {
        var value = configurationManager.GetValue<string>(key);

        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException($"{key} not configured, did you read the readme and configure your application secrets?");

        return value;
    }
}
