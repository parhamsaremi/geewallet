﻿namespace GWallet.Frontend.XF

open Xamarin.Forms

type App() =
    inherit Application(MainPage = (BalancesPage():> Page))

