﻿namespace GWallet.Frontend.XF

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
    let contentLayout = base.FindByName<StackLayout> "contentLayout"
    let normalChartView = base.FindByName<HoopChartView> "normalChartView"
    let readonlyChartView = base.FindByName<HoopChartView> "readonlyChartView"

    // FIXME: should reuse code with FrontendHelpers.BalanceInUsdString
       
    let UpdateGlobalFiatBalanceLabel (balance: decimal) (totalFiatAmountLabel: Label) =
        let strBalance =
            sprintf "~ %s USD" (balance.ToString())
        totalFiatAmountLabel.Text <- strBalance

    let rec UpdateGlobalFiatBalance (acc: Option<decimal>)
                                    (fiatBalances: List<decimal>)
                                    totalFiatAmountLabel
                                        : unit =
        let updateGlobalFiatBalanceFromFreshAcc accAmount head tail =
            UpdateGlobalFiatBalanceLabel accAmount totalFiatAmountLabel

        match acc with
        | None ->
            match fiatBalances with
            | [] ->
                failwith "unexpected: accumulator should be Some(thing) or coming balances shouldn't be List.empty"
            | head::tail ->
                let accAmount = 0.0m
                updateGlobalFiatBalanceFromFreshAcc accAmount head tail
        | Some(accAmount) ->
            match fiatBalances with
            | [] ->
                UpdateGlobalFiatBalanceLabel accAmount totalFiatAmountLabel
            | head::tail ->
                updateGlobalFiatBalanceFromFreshAcc accAmount head tail
        
    let rec FindCryptoBalances (cryptoBalanceClassId: string) (layout: StackLayout) 
                               (elements: List<View>) (resultsSoFar: List<Frame>): List<Frame> =
        match elements with
        | [] -> resultsSoFar
        | head::tail ->
            match head with
            | :? Frame as frame ->
                let newResults =
                    if frame.ClassId = cryptoBalanceClassId then
                        frame::resultsSoFar
                    else
                        resultsSoFar
                FindCryptoBalances cryptoBalanceClassId layout tail newResults
            | _ ->
                FindCryptoBalances cryptoBalanceClassId layout tail resultsSoFar

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

    member this.PopulateBalances (readOnly: bool) (balances: seq<BalanceState>) =
        let activeCurrencyClassId,inactiveCurrencyClassId =
            FrontendHelpers.GetActiveAndInactiveCurrencyClassIds readOnly

        let contentLayoutChildrenList = (contentLayout.Children |> List.ofSeq)

        let activeCryptoBalances = FindCryptoBalances activeCurrencyClassId 
                                                      contentLayout 
                                                      contentLayoutChildrenList
                                                      List.Empty

        let inactiveCryptoBalances = FindCryptoBalances inactiveCurrencyClassId 
                                                        contentLayout 
                                                        contentLayoutChildrenList
                                                        List.Empty

        contentLayout.BatchBegin()                      

        for inactiveCryptoBalance in inactiveCryptoBalances do
            inactiveCryptoBalance.IsVisible <- false

        //We should create new frames only once, then just play with IsVisible(True|False) 
        if activeCryptoBalances.Any() then
            for activeCryptoBalance in activeCryptoBalances do
                activeCryptoBalance.IsVisible <- true

        contentLayout.BatchCommit()

    member this.UpdateGlobalFiatBalanceSum (fiatBalancesList: List<decimal>) totalFiatAmountLabel =
        if fiatBalancesList.Any() then
            UpdateGlobalFiatBalance None fiatBalancesList totalFiatAmountLabel

    member private this.UpdateGlobalBalance (state: FrontendHelpers.IGlobalAppState)
                                            (balancesJob: array<BalanceState>)
                                            fiatLabel
                                            (readOnly: bool)
                                                : Option<bool> =
        
        let fiatBalances = balancesJob.Select(fun balanceState ->
                                                                balanceState.FiatAmount)
                            |> List.ofSeq
        Device.BeginInvokeOnMainThread(fun _ ->
            this.UpdateGlobalFiatBalanceSum fiatBalances fiatLabel
            RedrawCircleView readOnly
        )
        balancesJob.Any(fun balanceState ->

            // ".IsNone" means: we don't know if there's an incoming payment (absence of info)
            // so the whole `".IsNone" || "yes"` means: maybe there's an imminent incoming payment?
            // as in "it's false that we know for sure that there's no incoming payment"
            balanceState.ImminentIncomingPayment.IsNone ||
                Option.exists id balanceState.ImminentIncomingPayment

        // we can(SOME) answer: either there's no incoming payment (false) or that maybe there is (true)
        ) |> Some
        

    member private this.RefreshBalances (onlyReadOnlyAccounts: bool) =
        // we don't mind to be non-fast because it's refreshing in the background anyway
        let readOnlyBalancesJob =
            FrontendHelpers.UpdateBalancesAsync readOnlyAccountsBalanceSets

        let readOnlyAccountsBalanceUpdate =
            this.UpdateGlobalBalance state readOnlyBalancesJob readonlyChartView.BalanceLabel true

        let allBalanceUpdates =
            if (not onlyReadOnlyAccounts) then

                let normalBalancesJob =
                    FrontendHelpers.UpdateBalancesAsync normalAccountsBalanceSets

                let normalAccountsBalanceUpdate =
                    this.UpdateGlobalBalance state normalBalancesJob normalChartView.BalanceLabel false

                let allJobs = [normalAccountsBalanceUpdate; readOnlyAccountsBalanceUpdate]
                allJobs
            else
                [readOnlyAccountsBalanceUpdate]

        async {
            let balanceUpdates = allBalanceUpdates
            if balanceUpdates.Any(fun maybeImminentIncomingPayment ->
                Option.exists id maybeImminentIncomingPayment
            ) then
                this.NoImminentIncomingPayment <- false
            elif (not onlyReadOnlyAccounts) &&
                    balanceUpdates.All(fun maybeImminentIncomingPayment ->
                Option.exists not maybeImminentIncomingPayment
            ) then
                this.NoImminentIncomingPayment <- true
        }

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
                let balancesStatesToPopulate =
                    if switchingToReadOnly then
                        readOnlyBalanceStates
                    else
                        normalBalanceStates
                this.AssignColorLabels switchingToReadOnly
                this.PopulateBalances switchingToReadOnly balancesStatesToPopulate
                RedrawCircleView switchingToReadOnly
        )
        currentChartView.BalanceFrame.GestureRecognizers.Add tapGestureRecognizer
        tapGestureRecognizer

    member this.PopulateGridInitially () =

        let tapper = this.ConfigureFiatAmountFrame false
        this.ConfigureFiatAmountFrame true |> ignore

        this.PopulateBalances false normalBalanceStates
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
        let allNormalAccountFiatBalances =
            normalBalanceStates.Select(fun balanceState -> balanceState.FiatAmount) |> List.ofSeq
        let allReadOnlyAccountFiatBalances =
            readOnlyBalanceStates.Select(fun balanceState -> balanceState.FiatAmount) |> List.ofSeq

        Device.BeginInvokeOnMainThread(fun _ ->
            this.AssignColorLabels true
            if startWithReadOnlyAccounts then
                this.AssignColorLabels false

            this.PopulateGridInitially ()

            this.UpdateGlobalFiatBalanceSum allNormalAccountFiatBalances normalChartView.BalanceLabel
            this.UpdateGlobalFiatBalanceSum allReadOnlyAccountFiatBalances readonlyChartView.BalanceLabel
        )

        this.RefreshBalances true |> FrontendHelpers.DoubleCheckCompletionAsync false
        this.RefreshBalances false |> FrontendHelpers.DoubleCheckCompletionAsync false

