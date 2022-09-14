﻿namespace GWallet.Frontend.XF.Controls


open Xamarin.Forms
open Xamarin.Forms.Shapes


type HoopChartView() =
    inherit Layout<View>()
    // Child UI elements
    let balanceLabel = Label(HorizontalTextAlignment = TextAlignment.Center, FontSize = 25.0, MaxLines=1)
    let balanceTagLabel = 
        Label( 
            Text = "Some label:", 
            FontSize = 15.0, 
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = Thickness(0.0, -7.5)
        )

    let balanceFrame = 
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
        stackLayout.Children.Add balanceTagLabel
        stackLayout.Children.Add balanceLabel
        frame.Content <- stackLayout

        frame

    let hoop = Grid()
    // Properties
    member this.BalanceLabel = balanceLabel
    member this.BalanceFrame = balanceFrame

    // Layout properties
    member this.HoopStrokeThickness = 7.5
    
    // Chart shapes
    member private this.GetHoopShapes(radius: float) : Shape=
        let deg2rad angle = System.Math.PI * (angle / 180.0)
        let thickness = this.HoopStrokeThickness
        let minorRadius = thickness/2.0
        let circleRadius = radius - minorRadius
        let angleToPoint angle =
            Point(cos (deg2rad angle) * circleRadius + radius, sin (deg2rad angle) * circleRadius + radius)
        let startPoint = angleToPoint (0.0)
        let endPoint = angleToPoint (360.0)
        let arcAngle = 360.0
        let geom = PathGeometry()
        let figure = PathFigure(StartPoint = startPoint)
        let segment = ArcSegment(endPoint, Size(circleRadius, circleRadius), arcAngle, SweepDirection.Clockwise, arcAngle > 180.0)
        figure.Segments.Add segment
        geom.Figures.Add figure
        Path(
            Data = geom, 
            Stroke = SolidColorBrush (Color.FromRgb(245, 146, 47)), 
            StrokeThickness = thickness, 
            StrokeLineCap = PenLineCap.Round
        ) :> Shape
           
    member private this.RepopulateHoop(sideLength) =
        hoop.Children.Clear()
        this.GetHoopShapes(sideLength / 2.0) |> hoop.Children.Add

    // Layout
    override this.LayoutChildren(xCoord, yCoord, width, height) = 
        
        let smallerSide = min width height
        let xOffset = (max 0.0 (width - smallerSide)) / 2.0
        let yOffset = (max 0.0 (height - smallerSide)) / 2.0
        let bounds = Rectangle.FromLTRB(xCoord + xOffset, yCoord + yOffset, xCoord + xOffset + smallerSide, yCoord + yOffset + smallerSide)

        balanceFrame.Layout bounds

        if abs(hoop.Height - smallerSide) > 0.1 then
            this.RepopulateHoop(smallerSide)
            
        hoop.Layout bounds

    // Updates
    member this.SetState() =
        this.Children.Clear()
        if this.Width > 0.0 && this.Height > 0.0 then
            this.RepopulateHoop(min this.Width this.Height)
        this.Children.Add hoop
        this.Children.Add balanceFrame