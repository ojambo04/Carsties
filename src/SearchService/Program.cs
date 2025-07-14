using Polly;
using Polly.Extensions.Http;
using SearchService.Apis;
using SearchService.Data;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddHttpClient<AuctionSvcHttpClient>(options =>
{
    var auctionServiceUrl = builder.Configuration["AuctionServiceUrl"] ??
        throw new InvalidOperationException("AuctionServiceUrl configuration is missing or empty.");;

    options.BaseAddress = new Uri(auctionServiceUrl);
})
.AddPolicyHandler(GetPolicy());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

// try
// {
//     await DbInitialiser.InitDbAsync(app);
// }
// catch (Exception ex)
// {
//     Console.WriteLine($"An error occurred while initialising the database: {ex.Message}");
// }

app.Lifetime.ApplicationStarted.Register(() =>
{
    Policy.Handle<HttpRequestException>()
        .WaitAndRetryAsync(
            5,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (exception, timeSpan, retryCount, context) =>
            {
                Console.WriteLine($"Timeout occurred. Retry {retryCount} in {timeSpan.TotalSeconds} seconds. \nException: {exception.Message}");
            }
        )
        .ExecuteAndCaptureAsync(async () => await DbInitialiser.InitDbAsync(app));
});

app.MapSearchApi();

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetPolicy()
    => HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(10));
