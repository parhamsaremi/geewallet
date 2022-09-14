namespace GWallet.Frontend.XF

open System

open Xamarin.Forms
open Xamarin.Forms.Xaml

open GWallet.Frontend.XF.Controls

// this type allows us to represent the idea that if we have, for example, 3 LTC and an unknown number of ETC (might
// be because all ETC servers are unresponsive), then it means we have AT LEAST 3LTC; as opposed to when we know for
// sure all balances of all currencies because all servers are responsive
type TotalBalance =
    | ExactBalance of decimal
    | AtLeastBalance of decimal
    static member (+) (x: TotalBalance, y: decimal) =
        match x with
        | ExactBalance exactBalance -> ExactBalance (exactBalance + y)
        | AtLeastBalance exactBalance -> AtLeastBalance (exactBalance + y)
    static member (+) (x: decimal, y: TotalBalance) =
        y + x


type BalancesPage(someBool)
                      as this =
    inherit ContentPage()

    let _ = base.LoadFromXaml(typeof<BalancesPage>)

    let normalChartView = base.FindByName<HoopChartView> "normalChartView"
    let readonlyChartView = base.FindByName<HoopChartView> "readonlyChartView"

    // FIXME: should reuse code with FrontendHelpers.BalanceInUsdString
       
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

    new() = BalancesPage(false)

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

    member this.PopulateGridInitially () =
        RedrawCircleView false

    member private this.Init () =
        Device.BeginInvokeOnMainThread(fun _ ->
            this.PopulateGridInitially ()

            UpdateGlobalFiatBalanceLabel 0m normalChartView.BalanceLabel
            UpdateGlobalFiatBalanceLabel 0m readonlyChartView.BalanceLabel
        )

        this.RefreshBalances true
        this.RefreshBalances false

