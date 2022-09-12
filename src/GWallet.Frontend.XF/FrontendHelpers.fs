namespace GWallet.Frontend.XF

open System
open System.Linq
open System.Threading
open System.Threading.Tasks

open Xamarin.Forms
open ZXing
open ZXing.Mobile

open GWallet.Backend
open GWallet.Backend.FSharpUtil.UwpHacks

type BalanceWidgets =
    {
        CryptoLabel: Label
        FiatLabel: Label
        Frame: Frame
    }

type BalanceSet = {
    Widgets: BalanceWidgets
}

type BalanceState = {
    BalanceSet: BalanceSet;
    FiatAmount: decimal;
    ImminentIncomingPayment: Option<bool>;
    UsdRate: decimal
}

module FrontendHelpers =

    type IGlobalAppState =
        [<CLIEvent>]
        abstract member Resumed: IEvent<unit> with get
        [<CLIEvent>]
        abstract member GoneToSleep: IEvent<unit> with get

    type IAugmentablePayPage =
        abstract member AddTransactionScanner: unit -> unit

    let IsDesktop() =
        match Device.RuntimePlatform with
        | Device.Android | Device.iOS ->
            false
        | Device.macOS | Device.GTK | Device.UWP | Device.WPF ->
            true
        | _ ->
            // TODO: report a sentry warning
            false

    let internal BigFontSize = 22.

    let internal MediumFontSize = 20.

    let internal MagicGtkNumber = 20.

    let private defaultFiatCurrency = "USD"

    let internal ExchangeRateUnreachableMsg = SPrintF1 " (~ ? %s)" defaultFiatCurrency

    //FIXME: right now the UI doesn't explain what the below element means when it shows it, we should add a legend...
    let internal ExchangeOutdatedVisualElement = "*"

    // these days cryptos are not so volatile, so 30mins should be good...
    let internal TimeSpanToConsiderExchangeRateOutdated = TimeSpan.FromMinutes 30.0

    let MaybeReturnOutdatedMarkForOldDate (date: DateTime) =
        if (date + TimeSpanToConsiderExchangeRateOutdated < DateTime.UtcNow) then
            ExchangeOutdatedVisualElement
        else
            String.Empty

    // FIXME: share code between Frontend.Console and Frontend.XF
    let BalanceInUsdString (balance: decimal) (maybeUsdValue: MaybeCached<decimal>)
                               : MaybeCached<decimal>*string =
        match maybeUsdValue with
        | NotFresh(NotAvailable) ->
            NotFresh(NotAvailable),ExchangeRateUnreachableMsg
        | Fresh(usdValue) ->
            let fiatBalance = usdValue * balance
            Fresh(fiatBalance),SPrintF2 "~ %s %s"
                                   (Formatting.DecimalAmountRounding CurrencyType.Fiat fiatBalance)
                                   defaultFiatCurrency
        | NotFresh(Cached(usdValue,time)) ->
            let fiatBalance = usdValue * balance
            NotFresh(Cached(fiatBalance,time)),SPrintF3 "~ %s %s%s"
                                                    (Formatting.DecimalAmountRounding CurrencyType.Fiat fiatBalance)
                                                    defaultFiatCurrency
                                                    (MaybeReturnOutdatedMarkForOldDate time)

    
    let UpdateBalance () : decimal =

        10.0m

    let UpdateBalanceWithoutCacheAsync (balanceSet: BalanceSet)
                                           : BalanceState =
        {
            BalanceSet = balanceSet
            FiatAmount = 10.0m
            ImminentIncomingPayment = None
            UsdRate = 10.0m
        }

    let UpdateBalanceAsync (balanceSet: BalanceSet)
                               : BalanceState =
        let job = UpdateBalanceWithoutCacheAsync balanceSet
        
        job

    let UpdateBalancesAsync accountBalances
                                : array<BalanceState> =
        let sourcesAndJobs = seq {
            for balanceSet in accountBalances do
                let balanceJob = UpdateBalanceAsync balanceSet
                yield balanceJob
        } 
        sourcesAndJobs |> Seq.toArray

    let private MaybeCrash (canBeCanceled: bool) (ex: Exception) =
        let LastResortBail() =
            // this is just in case the raise(throw) doesn't really tear down the program:
            Infrastructure.LogError ("FATAL PROBLEM: " + ex.ToString())
            Infrastructure.LogError "MANUAL FORCED SHUTDOWN NOW"
            Device.PlatformServices.QuitApplication()

        if null = ex then
            ()
        else
            let shouldCrash =
                if not canBeCanceled then
                    true
                elif (FSharpUtil.FindException<TaskCanceledException> ex).IsSome then
                    false
                else
                    true
            if shouldCrash then
                Device.BeginInvokeOnMainThread(fun _ ->
                    raise ex
                    LastResortBail()
                )
                raise ex
                LastResortBail()

    // when running Task<unit> or Task<T> where we want to ignore the T, we should still make sure there is no exception,
    // & if there is, bring it to the main thread to fail fast, report to Sentry, etc, otherwise it gets ignored
    let DoubleCheckCompletion<'T> (task: Task<'T>) =
        task.ContinueWith(fun (t: Task<'T>) ->
            MaybeCrash false t.Exception
        , TaskContinuationOptions.OnlyOnFaulted) |> ignore
    let DoubleCheckCompletionNonGeneric (task: Task) =
        task.ContinueWith(fun (t: Task) ->
            MaybeCrash false t.Exception
        , TaskContinuationOptions.OnlyOnFaulted) |> ignore

    let DoubleCheckCompletionAsync<'T> (canBeCanceled: bool) (work: Async<'T>): unit =
        async {
            try
                let! _ = work
                ()
            with
            | ex ->
                MaybeCrash canBeCanceled ex
            return ()
        } |> Async.Start

    let SwitchToNewPage (currentPage: Page) (createNewPage: unit -> Page) (navBar: bool): unit =
        Device.BeginInvokeOnMainThread(fun _ ->
            let newPage = createNewPage ()
            NavigationPage.SetHasNavigationBar(newPage, false)
            let navPage = NavigationPage newPage
            NavigationPage.SetHasNavigationBar(navPage, navBar)

            currentPage.Navigation.PushAsync navPage
                |> DoubleCheckCompletionNonGeneric
        )

    let SwitchToNewPageDiscardingCurrentOne (currentPage: Page) (createNewPage: unit -> Page): unit =
        Device.BeginInvokeOnMainThread(fun _ ->
            let newPage = createNewPage ()
            NavigationPage.SetHasNavigationBar(newPage, false)

            currentPage.Navigation.InsertPageBefore(newPage, currentPage)
            currentPage.Navigation.PopAsync()
                |> DoubleCheckCompletion
        )

    let SwitchToNewPageDiscardingCurrentOneAsync (currentPage: Page) (createNewPage: unit -> Page) =
        async {
            let newPage = createNewPage ()
            NavigationPage.SetHasNavigationBar(newPage, false)

            currentPage.Navigation.InsertPageBefore(newPage, currentPage)
            let! _ =
                currentPage.Navigation.PopAsync()
                |> Async.AwaitTask
            return ()
        }

    let ChangeTextAndChangeBack (button: Button) (newText: string) =
        let initialText = button.Text
        button.IsEnabled <- false
        button.Text <- newText
        Task.Run(fun _ ->
            Task.Delay(TimeSpan.FromSeconds(2.0)).Wait()
            Device.BeginInvokeOnMainThread(fun _ ->
                button.Text <- initialText
                button.IsEnabled <- true
            )
        ) |> DoubleCheckCompletionNonGeneric

    let private CreateLabelWidgetForAccount horizontalOptions =
        let label = Label(Text = "...",
                          VerticalOptions = LayoutOptions.Center,
                          HorizontalOptions = horizontalOptions)
        label

    let private normalCryptoBalanceClassId = "normalCryptoBalanceFrame"
    let private readonlyCryptoBalanceClassId = "readonlyCryptoBalanceFrame"
    let GetActiveAndInactiveCurrencyClassIds readOnly =
        if readOnly then
            readonlyCryptoBalanceClassId,normalCryptoBalanceClassId
        else
            normalCryptoBalanceClassId,readonlyCryptoBalanceClassId

    let CreateCurrencyBalanceFrame (cryptoLabel: Label) (fiatLabel: Label) classId =
        let colorBoxWidth = 10.

        let stackLayout = StackLayout(Orientation = StackOrientation.Horizontal,
                                      Padding = Thickness(20., 20., colorBoxWidth + 10., 20.))

        stackLayout.Children.Add cryptoLabel
        stackLayout.Children.Add fiatLabel

        let colorBox = BoxView(Color = Color.FromRgb(245, 146, 47))

        let absoluteLayout = AbsoluteLayout(Margin = Thickness(0., 1., 3., 1.))
        absoluteLayout.Children.Add(stackLayout, Rectangle(0., 0., 1., 1.), AbsoluteLayoutFlags.All)
        absoluteLayout.Children.Add(colorBox, Rectangle(1., 0., colorBoxWidth, 1.), AbsoluteLayoutFlags.PositionProportional ||| AbsoluteLayoutFlags.HeightProportional)

        let frame = Frame(HasShadow = false,
                          ClassId = classId,
                          Content = absoluteLayout,
                          Padding = Thickness(0.),
                          BorderColor = Color.SeaShell)
        frame

    let private CreateWidgetsForAccount classId: BalanceWidgets =
        let accountBalanceLabel = CreateLabelWidgetForAccount LayoutOptions.Start
        let fiatBalanceLabel = CreateLabelWidgetForAccount LayoutOptions.EndAndExpand

        {
            CryptoLabel = accountBalanceLabel
            FiatLabel = fiatBalanceLabel
            Frame = CreateCurrencyBalanceFrame accountBalanceLabel fiatBalanceLabel classId
        }

    let CreateWidgetsForAccounts readOnly
                                    : List<BalanceSet> =
        let classId,_ = GetActiveAndInactiveCurrencyClassIds readOnly
        seq {
            let balanceWidgets = CreateWidgetsForAccount classId
            yield {
                Widgets = balanceWidgets
            }
        } |> List.ofSeq

    let BarCodeScanningOptions = MobileBarcodeScanningOptions(
                                     TryHarder = Nullable<bool> true,
                                     DisableAutofocus = false,
                                     // TODO: stop using Sys.Coll.Gen when this PR is accepted: https://github.com/Redth/ZXing.Net.Mobile/pull/800
                                     PossibleFormats = System.Collections.Generic.List<BarcodeFormat>(
                                         [ BarcodeFormat.QR_CODE ]
                                     ),
                                     UseNativeScanning = true
                                 )

    let GetImageSource name =
        let thisAssembly = typeof<BalanceState>.Assembly
        let thisAssemblyName = thisAssembly.GetName().Name
        let fullyQualifiedResourceNameForLogo = SPrintF2 "%s.img.%s.png"
                                                        thisAssemblyName name
        ImageSource.FromResource(fullyQualifiedResourceNameForLogo, thisAssembly)

    let GetSizedImageSource name size =
        let sizedName = SPrintF3 "%s_%ix%i" name size size
        GetImageSource sizedName

    let GetSizedColoredImageSource name color size =
        let sizedColoredName = SPrintF2 "%s_%s" name color
        GetSizedImageSource sizedColoredName size

