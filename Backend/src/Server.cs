using System.Diagnostics;

namespace WebApp;
public static class Server
{
    public static void Start()
    {
        var builder = WebApplication.CreateBuilder();
        App = builder.Build();
        Middleware();
        DebugLog.Start();
        Acl.Start();
        ErrorHandler.Start();
        FileServer.Start();
        LoginRoutes.Start();
        RestApi.Start();
        Session.Start();
        // Start the server on port 3001
        var runUrl = "http://localhost:" + Globals.port;
        Log("Server running on:", runUrl);
        Log("With these settings:", Globals);
        App.Run(runUrl);
    }

    // A basic middleware that changes the server response header,
    // initiates the debug logging for the request,
    // keep sessions alive and stop a route if acl does not approve of it
    public static void Middleware()
    {
        App.Use(async (context, next) =>
        {
            context.Response.Headers.Append("Server", (string)Globals.serverName);
            DebugLog.Register(context);
            Session.Touch(context);
            if (!Acl.Allow(context))
            {
                // Acl says the route is not allowed
                context.Response.StatusCode = 405;
                var error = "Not allowed.";
                await context.Response.WriteAsJsonAsync(new { error });
            }
            else { await next(context); }
            DebugLog.Add(context, new { statusCode = context.Response.StatusCode });
        });
    }
}