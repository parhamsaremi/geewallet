using System;
using Gtk;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Hosting;

type App() = 
    inherit MauiGtkAppliction()

    override _.CreateMauiApp() = MauiProgram.CreateMauiApp()