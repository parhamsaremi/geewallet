﻿namespace GWallet.Frontend.XF

open System
open System.Linq
open System.Threading.Tasks
open System.Threading

open Xamarin.Forms
open Xamarin.Forms.Xaml

open GWallet.Backend

/// <param name="showLogoFirst">
/// true  if just the logo should be shown first, and title text and loading text after some seconds,
/// false if title text and loading text should be shown immediatly.
/// </param>
type LoadingPage(state: FrontendHelpers.IGlobalAppState, showLogoFirst: bool) as this =
    inherit ContentPage()

    let _ = base.LoadFromXaml(typeof<LoadingPage>)

    let mainLayout = base.FindByName<StackLayout> "mainLayout"
    let titleLabel = mainLayout.FindByName<Label> "titleLabel"
    let progressBarLayout = base.FindByName<StackLayout> "progressBarLayout"
    let loadingLabel = mainLayout.FindByName<Label> "loadingLabel"
    let dotsMaxCount = 3
    let loadingTextNoDots = loadingLabel.Text

    let allAccounts = Account.GetAllActiveAccounts()
    let normalAccounts = allAccounts.OfType<NormalAccount>() |> List.ofSeq
                         |> List.map (fun account -> account :> IAccount)
    let readOnlyAccounts = allAccounts.OfType<ReadOnlyAccount>() |> List.ofSeq
                           |> List.map (fun account -> account :> IAccount)

    let CreateImage (currency: Currency) (readOnly: bool) =
        let colour =
            if readOnly then
                "grey"
            else
                "red"
        let currencyLowerCase = currency.ToString().ToLower()
        let img = 
            async {
                let! mainThreadSynchContext =
                    Async.AwaitTask <| Device.GetMainThreadSynchronizationContextAsync()
                do! Async.SwitchToContext mainThreadSynchContext
                let imageSource = FrontendHelpers.GetSizedColoredImageSource currencyLowerCase colour 60
                let currencyLogoImg = Image(Source = imageSource, IsVisible = true)
                return currencyLogoImg
            }
        img
        
        
    let GetAllCurrencyCases(): seq<Currency*bool> =
        seq {
            for currency in Currency.GetAll() do
                yield currency,true
                yield currency,false
        }
    let GetAllImages(): Async<array<(Currency*bool)*Image>> =
        seq {
            for currency,readOnly in GetAllCurrencyCases() do
                yield
                    async {
                        let! img = (CreateImage currency readOnly)
                        return (currency,readOnly),img
                    }
                
        } |> Async.Parallel

        
    let PreLoadCurrencyImages(): Async<Map<Currency*bool,Image>> =
        let mapOfImages =
            async {
                let! images = GetAllImages()
                return images |> Map.ofArray
            }
        mapOfImages

    let logoImageSource = FrontendHelpers.GetSizedImageSource "logo" 512
    let logoImg = Image(Source = logoImageSource, IsVisible = true)

    let mutable keepAnimationTimerActive = true

    let UpdateDotsLabel() =
        Device.BeginInvokeOnMainThread(fun _ ->
            let currentCountPlusOne = loadingLabel.Text.Count(fun x -> x = '.') + 1
            let dotsCount =
                if currentCountPlusOne > dotsMaxCount then
                    0
                else
                    currentCountPlusOne
            let dotsSeq = Enumerable.Repeat('.', dotsCount)
            loadingLabel.Text <- loadingTextNoDots + String(dotsSeq.ToArray())
        )
        keepAnimationTimerActive

    let ShowLoadingText() =
        Device.BeginInvokeOnMainThread(fun _ ->
            mainLayout.VerticalOptions <- LayoutOptions.Center
            mainLayout.Padding <- Thickness(20.,0.,20.,50.)
            logoImg.IsVisible <- false
            titleLabel.IsVisible <- true
            progressBarLayout.IsVisible <- true
            loadingLabel.IsVisible <- true
        )

        let dotsAnimationLength = TimeSpan.FromMilliseconds 500.
        Device.StartTimer(dotsAnimationLength, Func<bool> UpdateDotsLabel)
    do
        this.Init()

    [<Obsolete(DummyPageConstructorHelper.Warning)>]
    new() = LoadingPage(DummyPageConstructorHelper.GlobalFuncToRaiseExceptionIfUsedAtRuntime(),false)

    member this.Transition(): unit =
        async {
            let! currencyImagesJob = 
                (fun _ -> 
                    PreLoadCurrencyImages()
                )
                |> Device.InvokeOnMainThreadAsync
                |> Async.AwaitTask
            let! currencyImages = currencyImagesJob
            let! normalAccountsBalances = FrontendHelpers.CreateWidgetsForAccounts normalAccounts currencyImages false
            let _,allNormalAccountBalancesJob = FrontendHelpers.UpdateBalancesAsync normalAccountsBalances
                                                                                    false
                                                                                    ServerSelectionMode.Fast
                                                                                    (Some progressBarLayout)

            let! readOnlyAccountsBalances = FrontendHelpers.CreateWidgetsForAccounts readOnlyAccounts currencyImages true
            let _,readOnlyAccountBalancesJob =
                FrontendHelpers.UpdateBalancesAsync readOnlyAccountsBalances true ServerSelectionMode.Fast None

            let bothJobs = FSharpUtil.AsyncExtensions.MixedParallel2 allNormalAccountBalancesJob
                                                                     readOnlyAccountBalancesJob

            let! allResolvedNormalAccountBalances,allResolvedReadOnlyBalances = bothJobs

            keepAnimationTimerActive <- false

            let balancesPage () =
                BalancesPage(state, allResolvedNormalAccountBalances, allResolvedReadOnlyBalances,
                             currencyImages, false)
                    :> Page
            FrontendHelpers.SwitchToNewPageDiscardingCurrentOne this balancesPage
        }
            |> FrontendHelpers.DoubleCheckCompletionAsync false

        ()

    member this.Init (): unit =
        if showLogoFirst then
            Device.BeginInvokeOnMainThread(fun _ ->
                mainLayout.Children.Add logoImg
                this.Transition()
            )

            Device.StartTimer(TimeSpan.FromSeconds 5.0, fun _ ->
                ShowLoadingText()

                false // do not run timer again
            )
        else
            Device.BeginInvokeOnMainThread(fun _ ->
                ShowLoadingText()
                this.Transition()
            )
        
