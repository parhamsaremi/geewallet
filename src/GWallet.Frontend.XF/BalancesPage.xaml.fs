namespace GWallet.Frontend.XF

open Xamarin.Forms
open Xamarin.Forms.Xaml

open GWallet.Frontend.XF.Controls

type BalancesPage()
                      as this =
    inherit ContentPage()

    let _ = base.LoadFromXaml(typeof<BalancesPage>)

    let normalChartView = base.FindByName<HoopChartView> "normalChartView"
    let readonlyChartView = base.FindByName<HoopChartView> "readonlyChartView"
    do
        this.Init()

    member private this.Init () =
        Device.BeginInvokeOnMainThread(fun _ ->
            normalChartView.SetState()
        )

        Device.BeginInvokeOnMainThread(fun _ ->
            readonlyChartView.SetState()
            normalChartView.SetState()
        )
