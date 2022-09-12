namespace GWallet.Frontend.XF

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
type LoadingPage(state: FrontendHelpers.IGlobalAppState, showLogoFirst: bool) as this =
    inherit ContentPage()
    do
        this.Init()

    [<Obsolete(DummyPageConstructorHelper.Warning)>]
    new() = LoadingPage(DummyPageConstructorHelper.GlobalFuncToRaiseExceptionIfUsedAtRuntime(),false)

    member this.Transition(): unit =
        let normalAccountsBalances = FrontendHelpers.CreateWidgetsForAccounts false
        let _,allNormalAccountBalancesJob = FrontendHelpers.UpdateBalancesAsync normalAccountsBalances
                                                                                false
                                                                                ServerSelectionMode.Fast
                                                                                (None)

        let readOnlyAccountsBalances = FrontendHelpers.CreateWidgetsForAccounts true
        let _,readOnlyAccountBalancesJob =
            FrontendHelpers.UpdateBalancesAsync readOnlyAccountsBalances true ServerSelectionMode.Fast None

        async {
            let bothJobs = FSharpUtil.AsyncExtensions.MixedParallel2 allNormalAccountBalancesJob
                                                                     readOnlyAccountBalancesJob

            let! allResolvedNormalAccountBalances,allResolvedReadOnlyBalances = bothJobs

            let balancesPage () =
                BalancesPage(state, allResolvedNormalAccountBalances, allResolvedReadOnlyBalances,
                             Map.empty, false)
                    :> Page
            FrontendHelpers.SwitchToNewPageDiscardingCurrentOne this balancesPage
        }
            |> FrontendHelpers.DoubleCheckCompletionAsync false

        ()

    member this.Init (): unit =
        this.Transition()

