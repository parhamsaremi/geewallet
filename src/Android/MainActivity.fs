namespace src.Android

open Android.App
open Android.Content.PM
open Android.Widget
open Xamarin.Forms.Platform.Android

open src

[<Activity(Label = "src", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = (ConfigChanges.ScreenSize ||| ConfigChanges.Orientation ||| ConfigChanges.UiMode ||| ConfigChanges.ScreenLayout ||| ConfigChanges.SmallestScreenSize) )>]
type MainActivity() =
  inherit FormsAppCompatActivity()
  
  override this.OnCreate(bundle) =
    Resource.UpdateIdValues()

    base.OnCreate(bundle)
    
    Xamarin.Forms.Forms.Init(this, bundle)
    this.LoadApplication(App())