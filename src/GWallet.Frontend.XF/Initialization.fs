﻿namespace GWallet.Frontend.XF

open System.Linq

open Xamarin.Forms

open GWallet.Backend

module Initialization =

    let internal GlobalState = GlobalState()

    let private GlobalInit () =
        Infrastructure.SetupExceptionHook ()

    let internal LandingPage(): NavigationPage =
        let state = GlobalInit ()

        let accounts = Account.GetAllActiveAccounts()
        let landingPage:Page =
            (LoadingPage (GlobalState, true)) :> Page

        let navPage = NavigationPage landingPage
        NavigationPage.SetHasNavigationBar(landingPage, false)
        navPage
