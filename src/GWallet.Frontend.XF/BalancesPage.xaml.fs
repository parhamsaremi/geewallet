namespace GWallet.Frontend.XF

open System

open Xamarin.Forms
open Xamarin.Forms.Xaml

open GWallet.Frontend.XF.Controls

type BalancesPage()
                      as this =
    inherit ContentPage()

    let _ = base.LoadFromXaml(typeof<BalancesPage>)

    let normalChartView = base.FindByName<HoopChartView> "normalChartView"
    let readonlyChartView = base.FindByName<HoopChartView> "readonlyChartView"

    let UpdateGlobalFiatBalanceLabel (balance: decimal) (totalFiatAmountLabel: Label) =
        let strBalance =
            sprintf "%s" (balance.ToString())
        totalFiatAmountLabel.Text <- strBalance

    let RedrawCircleView (readOnly: bool) =
        let chartView =
            if readOnly then
                readonlyChartView
            else
                normalChartView
        chartView.SetState()
    do
        this.Init()

    member private this.RefreshBalances (onlyReadOnlyAccounts: bool) =
        // we don't mind to be non-fast because it's refreshing in the background anyway

        Device.BeginInvokeOnMainThread(fun _ ->
            UpdateGlobalFiatBalanceLabel 0m  readonlyChartView.BalanceLabel
            RedrawCircleView true
        )

        if (not onlyReadOnlyAccounts) then
            Device.BeginInvokeOnMainThread(fun _ ->
                UpdateGlobalFiatBalanceLabel 0m  normalChartView.BalanceLabel
                RedrawCircleView false
            )
        ()

    member private this.Init () =
        Device.BeginInvokeOnMainThread(fun _ ->
            RedrawCircleView false

            UpdateGlobalFiatBalanceLabel 0m normalChartView.BalanceLabel
            UpdateGlobalFiatBalanceLabel 0m readonlyChartView.BalanceLabel
        )

        this.RefreshBalances true
        this.RefreshBalances false
