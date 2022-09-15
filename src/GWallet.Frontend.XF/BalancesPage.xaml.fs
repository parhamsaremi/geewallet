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

    let UpdateLabel (label: Label) =
        label.Text <- sprintf "%A" 0
    do
        this.Init()

    member private this.Init () =
        Device.BeginInvokeOnMainThread(fun _ ->
            normalChartView.SetState()

            UpdateLabel normalChartView.BalanceLabel
            UpdateLabel readonlyChartView.BalanceLabel
        )

        Device.BeginInvokeOnMainThread(fun _ ->
            UpdateLabel readonlyChartView.BalanceLabel
            readonlyChartView.SetState()
            UpdateLabel normalChartView.BalanceLabel
            normalChartView.SetState()
        )
