namespace GWallet.Frontend.XF

open System.Linq

open Xamarin.Forms

open GWallet.Backend

module Initialization =

    let internal GlobalState = GlobalState()

    let private GlobalInit () =
        Infrastructure.SetupExceptionHook ()

    let internal LandingPage(): NavigationPage =
        let state = GlobalInit ()
        let normalAccountsBalances = FrontendHelpers.CreateWidgetsForAccounts false
        let allNormalAccountBalancesJob = FrontendHelpers.UpdateBalancesAsync normalAccountsBalances

        let readOnlyAccountsBalances = FrontendHelpers.CreateWidgetsForAccounts true
        let readOnlyAccountBalancesJob =
            FrontendHelpers.UpdateBalancesAsync readOnlyAccountsBalances

        let landingPage:Page =
            (BalancesPage(GlobalState, allNormalAccountBalancesJob, readOnlyAccountBalancesJob,
            Map.empty, false))
                :> Page

        let navPage = NavigationPage landingPage
        NavigationPage.SetHasNavigationBar(landingPage, false)
        navPage
