namespace GWallet.Frontend.XF

open System
open System.Linq
open System.Threading

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


type BalancesPage(state: FrontendHelpers.IGlobalAppState,
                  normalBalanceStates: seq<BalanceState>,
                  readOnlyBalanceStates: seq<BalanceState>,
                  startWithReadOnlyAccounts: bool)
                      as this =
    inherit ContentPage()

    let _ = base.LoadFromXaml(typeof<BalancesPage>)

    let normalAccountsBalanceSets = normalBalanceStates.Select(fun balState -> balState.BalanceSet)
    let readOnlyAccountsBalanceSets = readOnlyBalanceStates.Select(fun balState -> balState.BalanceSet)
    let mainLayout = base.FindByName<StackLayout>("mainLayout")
    let normalChartView = base.FindByName<HoopChartView> "normalChartView"
    let readonlyChartView = base.FindByName<HoopChartView> "readonlyChartView"

    // FIXME: should reuse code with FrontendHelpers.BalanceInUsdString
       
    let UpdateGlobalFiatBalanceLabel (balance: decimal) (totalFiatAmountLabel: Label) =
        let strBalance =
            sprintf "~ %s USD" (balance.ToString())
        totalFiatAmountLabel.Text <- strBalance

    let rec UpdateGlobalFiatBalance totalFiatAmountLabel
                                        : unit =
        UpdateGlobalFiatBalanceLabel 0m totalFiatAmountLabel
        

    let RedrawCircleView (readOnly: bool) =
        let chartView =
            if readOnly then
                readonlyChartView
            else
                normalChartView
        chartView.SetState()

    // default value of the below field is 'false', just in case there's an incoming payment which we don't want to miss
    let mutable noImminentIncomingPayment = false

    let lockObject = Object()

    do
        this.Init()

    [<Obsolete(DummyPageConstructorHelper.Warning)>]
    new() = BalancesPage(DummyPageConstructorHelper.GlobalFuncToRaiseExceptionIfUsedAtRuntime(),Seq.empty,Seq.empty,
                         false)

    member private this.NoImminentIncomingPayment
        with get() = lock lockObject (fun _ -> noImminentIncomingPayment)
         and set value = lock lockObject (fun _ -> noImminentIncomingPayment <- value)

    member private this.UpdateGlobalBalance (state: FrontendHelpers.IGlobalAppState)
                                            fiatLabel
                                            (readOnly: bool)
                                                : Option<bool> =
        
        Device.BeginInvokeOnMainThread(fun _ ->
            UpdateGlobalFiatBalance fiatLabel
            RedrawCircleView readOnly
        )
        None
        

    member private this.RefreshBalances (onlyReadOnlyAccounts: bool) =
        // we don't mind to be non-fast because it's refreshing in the background anyway

        this.UpdateGlobalBalance state readonlyChartView.BalanceLabel true |> ignore

        if (not onlyReadOnlyAccounts) then
            this.UpdateGlobalBalance state normalChartView.BalanceLabel false |> ignore
        ()

    member private this.ConfigureFiatAmountFrame (readOnly: bool): TapGestureRecognizer =
        let currentChartViewName,otherChartViewName =
            if readOnly then
                "readonlyChartView","normalChartView"
            else
                "normalChartView","readonlyChartView"

        let switchingToReadOnly = not readOnly

        let currentChartView,otherChartView =
            mainLayout.FindByName<HoopChartView> currentChartViewName,
            mainLayout.FindByName<HoopChartView> otherChartViewName

        let tapGestureRecognizer = TapGestureRecognizer()
        tapGestureRecognizer.Tapped.Add(fun _ ->

            let shouldNotOpenNewPage =
                if switchingToReadOnly then
                    readOnlyAccountsBalanceSets.Any()
                else
                    true

            if shouldNotOpenNewPage then
                Device.BeginInvokeOnMainThread(fun _ ->
                    currentChartView.IsVisible <- false
                    otherChartView.IsVisible <- true
                )
                this.AssignColorLabels switchingToReadOnly
                RedrawCircleView switchingToReadOnly
        )
        currentChartView.BalanceFrame.GestureRecognizers.Add tapGestureRecognizer
        tapGestureRecognizer

    member this.PopulateGridInitially () =

        let tapper = this.ConfigureFiatAmountFrame false
        this.ConfigureFiatAmountFrame true |> ignore

        RedrawCircleView false

        if startWithReadOnlyAccounts then
            tapper.SendTapped null

    member private this.AssignColorLabels (readOnly: bool) =
        let labels,color =
            if readOnly then
                let color = Color.DarkBlue
                readonlyChartView.BalanceLabel.TextColor <- color
                readOnlyAccountsBalanceSets,color
            else
                let color = Color.DarkRed
                normalChartView.BalanceLabel.TextColor <- color
                normalAccountsBalanceSets,color

        for balanceSet in labels do
            balanceSet.Widgets.CryptoLabel.TextColor <- color
            balanceSet.Widgets.FiatLabel.TextColor <- color

    member private this.Init () =
        Device.BeginInvokeOnMainThread(fun _ ->
            this.AssignColorLabels true
            if startWithReadOnlyAccounts then
                this.AssignColorLabels false

            this.PopulateGridInitially ()

            UpdateGlobalFiatBalance normalChartView.BalanceLabel
            UpdateGlobalFiatBalance readonlyChartView.BalanceLabel
        )

        this.RefreshBalances true
        this.RefreshBalances false

