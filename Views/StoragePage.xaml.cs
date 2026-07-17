using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NXG.Models;
using NXG.ViewModels;
using NXG.Services;
using Microsoft.Extensions.DependencyInjection;

namespace NXG.Views;

public sealed partial class StoragePage : Page
{
    public StorageViewModel ViewModel { get; }

    public StoragePage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<StorageViewModel>();
        
        // Listen to changes to TreemapRoot to draw
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StorageViewModel.TreemapRoot))
        {
            App.DispatcherQueue.TryEnqueue(() => RenderTreemap());
        }
    }

    private void TreemapCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderTreemap();
    }

    private void RenderTreemap()
    {
        if (TreemapCanvas == null) return;
        TreemapCanvas.Children.Clear();
        
        var root = ViewModel.TreemapRoot;
        if (root == null) return;

        double canvasWidth = TreemapCanvas.ActualWidth;
        double canvasHeight = TreemapCanvas.ActualHeight;
        if (canvasWidth <= 0 || canvasHeight <= 0)
        {
            canvasWidth = 600;
            canvasHeight = 400;
        }

        // Run coordinate mapper directly
        var storageService = App.Services.GetRequiredService<IStorageToolService>();
        storageService.LayoutTreemap(root, 0, 0, canvasWidth, canvasHeight);

        DrawNodes(root);
    }

    private void DrawNodes(DiskItem node)
    {
        if (node.Children.Count == 0)
        {
            if (node.Width < 12 || node.Height < 12) return;

            var border = new Border
            {
                Width = node.Width - 2,
                Height = node.Height - 2,
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DeepSkyBlue),
                Opacity = 0.85
            };

            Canvas.SetLeft(border, node.X + 1);
            Canvas.SetTop(border, node.Y + 1);

            var text = new TextBlock
            {
                Text = node.Name,
                FontSize = 10,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(4)
            };

            border.Child = text;
            TreemapCanvas.Children.Add(border);
        }
        else
        {
            foreach (var child in node.Children)
            {
                DrawNodes(child);
            }
        }
    }
}
