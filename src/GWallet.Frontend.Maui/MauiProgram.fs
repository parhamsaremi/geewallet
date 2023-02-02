namespace GWallet.Frontend.Maui


open Gdk
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection.Extensions
open Microsoft.Maui
open Microsoft.Maui.Controls
open Microsoft.Maui.Controls.Compatibility
open Microsoft.Maui.Controls.Compatibility.Hosting
open Microsoft.Maui.Controls.Hosting
open Microsoft.Maui.Hosting
open Microsoft.Maui.LifecycleEvents

type MauiProgram =
    static member CreateMauiApp() =
        MauiApp
            .CreateBuilder()
            .UseMauiApp<App>()
            .ConfigureFonts(fun fonts ->
                fonts
                    .AddFont("OpenSans-Regular.ttf", "OpenSansRegular")
                    .AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold")
                |> ignore
            )
            .Build()

    static member CreateMauiGtkApp() = 
        let appBuilder = 
            MauiApp
                .CreateBuilder()
                .UseMauiApp<App>()
                .UseMauiCompatibility()
        
        appBuilder
            .ConfigureFonts(fun fonts ->
                fonts
                    .AddFont("OpenSans-Regular.ttf", "OpenSansRegular")
                    .AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold")
                |> ignore
            ) |>ignore

        let services = appBuilder.Services
        services.AddTransient<IWindow, Window>() |> ignore

        appBuilder.Build()
        