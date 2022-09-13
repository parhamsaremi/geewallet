namespace GWallet.Frontend.XF

open System
open System.Linq
open System.Threading
open System.Threading.Tasks

open Xamarin.Forms
open ZXing
open ZXing.Mobile


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

    let internal ExchangeRateUnreachableMsg = sprintf " (~ ? %s)" defaultFiatCurrency

    //FIXME: right now the UI doesn't explain what the below element means when it shows it, we should add a legend...
    let internal ExchangeOutdatedVisualElement = "*"

    // these days cryptos are not so volatile, so 30mins should be good...
    let internal TimeSpanToConsiderExchangeRateOutdated = TimeSpan.FromMinutes 30.0

    // FIXME: share code between Frontend.Console and Frontend.XF
    let BalanceInUsdString (balance: decimal) (maybeUsdValue: decimal)
                               : decimal*string =
        
        let fiatBalance = maybeUsdValue * balance
        fiatBalance,sprintf "~ %s %s"
                                (fiatBalance.ToString())
                                defaultFiatCurrency
    
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
