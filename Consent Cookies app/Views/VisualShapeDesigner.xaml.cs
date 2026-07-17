using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CookieConsentInserteer.Views
{
    public partial class VisualShapeDesigner : Window
    {
        private Border activeBanner = null;

        public VisualShapeDesigner()
        {
            InitializeComponent();
        }

        private void UpdateBannerPosition()
        {
            if (activeBanner == null || PreviewCanvas == null) return;

            double left = 0;
            double top = 0;

            int index = PositionCombo.SelectedIndex >= 0 ? PositionCombo.SelectedIndex : 0;

            switch (index)
            {
                case 0: // Bottom Right
                    left = PreviewCanvas.ActualWidth - activeBanner.Width - 20;
                    top = PreviewCanvas.ActualHeight - activeBanner.Height - 20;
                    break;
                case 1: // Bottom Left
                    left = 20;
                    top = PreviewCanvas.ActualHeight - activeBanner.Height - 20;
                    break;
                case 2: // Top Right
                    left = PreviewCanvas.ActualWidth - activeBanner.Width - 20;
                    top = 20;
                    break;
                case 3: // Top Left
                    left = 20;
                    top = 20;
                    break;
                case 4: // Bottom Center
                    left = (PreviewCanvas.ActualWidth - activeBanner.Width) / 2;
                    top = PreviewCanvas.ActualHeight - activeBanner.Height - 20;
                    break;
            }

            Canvas.SetLeft(activeBanner, left);
            Canvas.SetTop(activeBanner, top);
        }

        private void PreviewBanner_Click(object sender, RoutedEventArgs e)
        {
            // Clear any existing preview
            PreviewCanvas.Children.Clear();

            // Create a banner
            activeBanner = new Border
            {
                Width = 400,
                Height = 80,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BgColorText.Text)),
                CornerRadius = new CornerRadius(MorphShapeCorners.Value),
                Opacity = BannerOpacity.Value,
                Child = new TextBlock
                {
                    Text = "We use cookies!",
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14
                }
            };

            PreviewCanvas.Children.Add(activeBanner);
            UpdateBannerPosition();
        }

        private void MorphShapeCorners_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (activeBanner != null)
            {
                if (sender == MorphShapeCorners)
                {
                    activeBanner.CornerRadius = new CornerRadius(MorphShapeCorners.Value);
                }
                else if (sender == BannerOpacity)
                {
                    activeBanner.Opacity = BannerOpacity.Value;
                }
            }
        }

        private void BgColorText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (activeBanner != null)
            {
                try
                {
                    activeBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BgColorText.Text));
                }
                catch { }
            }
        }

        private void PositionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateBannerPosition();
        }
    }
}
