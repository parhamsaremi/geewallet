namespace GWallet.Frontend.XF

open System.Linq

open Xamarin.Forms


module Initialization =

    let internal GlobalState = GlobalState()

    let internal LandingPage(): NavigationPage =
        let normalAccountsBalances = FrontendHelpers.CreateWidgetsForAccounts false
        let allNormalAccountBalancesJob = FrontendHelpers.UpdateBalancesAsync normalAccountsBalances

        let readOnlyAccountsBalances = FrontendHelpers.CreateWidgetsForAccounts true
        let readOnlyAccountBalancesJob =
            FrontendHelpers.UpdateBalancesAsync readOnlyAccountsBalances

        let landingPage:Page =
            (BalancesPage(GlobalState, allNormalAccountBalancesJob, readOnlyAccountBalancesJob,false))
                :> Page

        let navPage = NavigationPage landingPage
        NavigationPage.SetHasNavigationBar(landingPage, false)
        navPage
