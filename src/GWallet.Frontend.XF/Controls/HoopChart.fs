namespace GWallet.Frontend.XF.Controls


open Xamarin.Forms


type HoopChartView() =
    inherit Layout<View>()
    // Child UI elements
    let tagLabel = 
        Label( 
            Text = "Some label:", 
            FontSize = 15.0, 
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = Thickness(0.0, -7.5)
        )

    let mainFrame = 
        let frame = 
            Frame(
                HasShadow = false,
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent,
                Padding = Thickness(0.0),
                HorizontalOptions = LayoutOptions.CenterAndExpand
            )
        let stackLayout = 
            StackLayout(
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.CenterAndExpand,
                HorizontalOptions = LayoutOptions.Center
            )
        stackLayout.Children.Add tagLabel
        frame.Content <- stackLayout

        frame

    // Properties
    member this.MainFrame = mainFrame

    // Layout
    override this.LayoutChildren(xCoord, yCoord, width, height) = 
        
        let smallerSide = min width height
        let xOffset = (max 0.0 (width - smallerSide)) / 2.0
        let yOffset = (max 0.0 (height - smallerSide)) / 2.0
        let bounds = Rectangle.FromLTRB(xCoord + xOffset, yCoord + yOffset, xCoord + xOffset + smallerSide, yCoord + yOffset + smallerSide)

        mainFrame.Layout bounds

    // Updates
    member this.SetState() =
        this.Children.Clear()
        this.Children.Add mainFrame