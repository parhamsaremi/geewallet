﻿namespace GWallet.Frontend.XF

open System
open System.Linq
open System.Threading.Tasks

open Xamarin.Forms
open Xamarin.Forms.Xaml

open GWallet.Backend

/// <param name="showLogoFirst">
/// true  if just the logo should be shown first, and title text and loading text after some seconds,
/// false if title text and loading text should be shown immediatly.
/// </param>
type LoadingPage() as this =
    inherit ContentPage()

    let _ = base.LoadFromXaml(typeof<LoadingPage>)

    let allAccounts = Account.GetAllActiveAccounts()
    let normalAccounts = allAccounts.OfType<NormalAccount>() |> List.ofSeq
                         |> List.map (fun account -> account :> IAccount)

    let CreateImage (currency: Currency) (readOnly: bool) =
        let colour =
            if readOnly then
                "grey"
            else
                "red"
        let currencyLowerCase = currency.ToString().ToLower()
        let imageSource = FrontendHelpers.GetSizedColoredImageSource currencyLowerCase colour 60
        let currencyLogoImg = Image(Source = imageSource, IsVisible = true)
        currencyLogoImg
    let GetAllCurrencyCases(): seq<Currency*bool> =
        seq {
            for currency in Currency.GetAll() do
                yield currency,true
                yield currency,false
        }
    let GetAllImages(): seq<(Currency*bool)*Image> =
        seq {
            for currency,readOnly in GetAllCurrencyCases() do
                yield (currency,readOnly),(CreateImage currency readOnly)
        }
    let PreLoadCurrencyImages(): Map<Currency*bool,Image> =
        GetAllImages() |> Map.ofSeq

    let logoImageSource = FrontendHelpers.GetSizedImageSource "logo" 512

    let Transition(): unit =
        let currencyImages = PreLoadCurrencyImages()

        let normalAccountsBalances = FrontendHelpers.CreateWidgetsForAccounts normalAccounts currencyImages false
        let _,allNormalAccountBalancesJob = FrontendHelpers.UpdateBalancesAsync normalAccountsBalances
                                                                                false
                                                                                ServerSelectionMode.Fast
        let allNormalAccountBalancesJobAugmented = async {
            let! normalAccountBalances = allNormalAccountBalancesJob
            return normalAccountBalances
        }

        async {

            let! allResolvedNormalAccountBalances = allNormalAccountBalancesJobAugmented

            let balancesPage () =
                BalancesPage(allResolvedNormalAccountBalances,
                             currencyImages, false)
                    :> Page
            FrontendHelpers.SwitchToNewPageDiscardingCurrentOne this balancesPage
        }
            |> FrontendHelpers.DoubleCheckCompletionAsync false

        ()

    do
        Transition()

