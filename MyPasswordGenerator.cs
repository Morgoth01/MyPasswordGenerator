// MyPasswordGenerator
// Author: Claude Peters (https://github.com/Morgoth01)
// Copyright (c) 2025 Claude Peters
// License: MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Zxcvbn;

namespace MyPasswordGenerator
{
    public partial class MainWindow : Window
    {
        private readonly char[] Lowercase = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
        private readonly char[] Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private readonly char[] Digits = "0123456789".ToCharArray();
        private readonly char[] Symbols = "!@#$%^&*()_-+=[]{}|;:,.<>?".ToCharArray();
        private string[] EnglishWords;
        private string[] GermanWords;

        private DispatcherTimer _copyTimer = new DispatcherTimer();
        private DispatcherTimer _passphraseTimer = new DispatcherTimer();
        private bool _isDarkMode = false;

        public MainWindow()
        {
            InitializeComponent();
            var theme = LoadThemeSetting();
            if (theme == "Dark")
                SetDarkTheme_Click(null, null);
            else
                SetLightTheme_Click(null, null);

            InitializeTimers();
            LoadWordLists();
        }

        private void InitializeTimers()
        {
            _copyTimer.Interval = TimeSpan.FromSeconds(3);
            _copyTimer.Tick += (s, e) =>
            {
                txtCopiedMessage.Visibility = Visibility.Collapsed;
                _copyTimer.Stop();
            };

            _passphraseTimer.Interval = TimeSpan.FromSeconds(3);
            _passphraseTimer.Tick += (s, e) =>
            {
                txtCopiedMessagePassphrase.Visibility = Visibility.Collapsed;
                _passphraseTimer.Stop();
            };
        }

        private void LoadWordLists()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                EnglishWords = File.ReadAllLines(Path.Combine(basePath, "Resources", "english.txt"));
                GermanWords = File.ReadAllLines(Path.Combine(basePath, "Resources", "german.txt"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading word lists: {ex.Message}");
            }
        }

        private string ThemeConfigPath => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "MyPasswordGenerator_theme.txt");

        private void SaveThemeSetting(string theme)
        {
            try { File.WriteAllText(ThemeConfigPath, theme); } catch { }
        }

        private string LoadThemeSetting()
        {
            try { return File.Exists(ThemeConfigPath) ? File.ReadAllText(ThemeConfigPath) : null; } catch { return null; }
        }


        private string GenerateSecurePassword(char[] charset, int length)
        {
            var buffer = new byte[length];
            RandomNumberGenerator.Fill(buffer);

            var result = new StringBuilder(length);
            foreach (byte b in buffer)
            {
                result.Append(charset[b % charset.Length]);
            }
            return result.ToString();
        }

        private string GenerateSecurePassphrase(string[] wordList, int wordCount, string separator)
        {
            var result = new StringBuilder();
            for (int i = 0; i < wordCount; i++)
            {
                var indexBuffer = new byte[4];
                RandomNumberGenerator.Fill(indexBuffer);
                int index = Math.Abs(BitConverter.ToInt32(indexBuffer, 0)) % wordList.Length;
                result.Append(wordList[index] + separator);
            }
            return result.ToString().TrimEnd(separator.ToCharArray());
        }

        private Color GetStrengthColor(double entropy, bool isDarkMode)
        {
            if (isDarkMode)
            {
                if (entropy < 28) return Color.FromRgb(255, 120, 120);      // Soft Red
                if (entropy < 36) return Color.FromRgb(255, 200, 120);      // Soft Orange
                if (entropy < 60) return Color.FromRgb(255, 240, 180);      // Soft Yellow
                if (entropy < 80) return Color.FromRgb(130, 255, 170);      // Mint
                if (entropy < 100) return Color.FromRgb(120, 220, 200);     // Soft Green
                return Color.FromRgb(180, 240, 255);                        // Soft Cyan
            }
            else
            {
                if (entropy < 28) return Color.FromRgb(180, 0, 0);          // Dark Red
                if (entropy < 36) return Color.FromRgb(200, 120, 0);        // Dark Orange
                if (entropy < 60) return Color.FromRgb(160, 140, 0);        // Olive
                if (entropy < 80) return Color.FromRgb(0, 120, 60);         // Dark Green
                if (entropy < 100) return Color.FromRgb(0, 80, 120);        // Teal
                return Color.FromRgb(0, 60, 160);                           // Blue
            }
        }

        private Color GetCheckTabStrengthColor(double entropy, bool isDarkMode)
        {
            if (isDarkMode)
            {
                if (entropy < 28) return Color.FromRgb(255, 120, 120);      // Soft Red
                if (entropy < 36) return Color.FromRgb(255, 200, 120);      // Soft Orange
                if (entropy < 60) return Color.FromRgb(255, 240, 180);      // Soft Yellow
                if (entropy < 80) return Color.FromRgb(130, 255, 170);      // Mint
                if (entropy < 100) return Color.FromRgb(120, 220, 200);     // Soft Green
                return Color.FromRgb(180, 240, 255);                        // Soft Cyan
            }
            else
            {
                if (entropy < 28) return Color.FromRgb(180, 0, 0);          // Dark Red
                if (entropy < 36) return Color.FromRgb(200, 120, 0);        // Dark Orange
                if (entropy < 60) return Color.FromRgb(160, 140, 0);        // Olive
                if (entropy < 80) return Color.FromRgb(0, 120, 60);         // Dark Green
                if (entropy < 100) return Color.FromRgb(0, 80, 120);        // Teal
                return Color.FromRgb(0, 60, 160);                           // Blue
            }
        }


        private void UpdateStrengthDisplay(double entropy, ProgressBar bar, TextBlock textBlock)
        {
            bar.Maximum = 128;

            AnimateProgressBar(bar, entropy);

            string strength;
            if (entropy < 28)
                strength = "Very Weak";
            else if (entropy < 36)
                strength = "Weak";
            else if (entropy < 60)
                strength = "Moderate";
            else if (entropy < 80)
                strength = "Strong";
            else
                strength = "Very Strong";

            textBlock.Text = $"{entropy:F1} bits ({strength})";
            textBlock.Foreground = new SolidColorBrush(GetStrengthColor(entropy, _isDarkMode));
        }

        private string GetCheckTabStrengthText(double entropy)
        {

            if (entropy < 28)
                return "Congrats! Your password is so weak, a toddler mashing a keyboard could break in";
            else if (entropy < 36)
                return "Weak";
            else if (entropy < 60)
                return "Moderate";
            else if (entropy < 80)
                return "Strong";
            else if (entropy < 150)
                return "Very Strong";
            else
                return "YOU SHALL NOT PASS";
        }

        private void AnimateProgressBar(ProgressBar bar, double toValue)
        {
            DoubleAnimation anim = new DoubleAnimation
            {
                To = Math.Min(toValue, bar.Maximum),
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            bar.BeginAnimation(ProgressBar.ValueProperty, anim);
        }

        private void GeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            var charset = new List<char>();
            if (cbLower.IsChecked == true) charset.AddRange(Lowercase);
            if (cbUpper.IsChecked == true) charset.AddRange(Uppercase);
            if (cbDigits.IsChecked == true) charset.AddRange(Digits);
            if (cbSymbols.IsChecked == true) charset.AddRange(Symbols);

            if (charset.Count == 0)
            {
                MessageBox.Show("Please select at least one character type");
                return;
            }

            string password = GenerateSecurePassword(charset.ToArray(), (int)sliderLength.Value);
            txtPassword.Text = password;

            double entropy = password.Length * Math.Log(charset.Count, 2);
            UpdateStrengthDisplay(entropy, pbStrength, tbStrengthText);

            var zxcvbnResult = Core.EvaluatePassword(password);
            UpdateStrengthDisplay(entropy, pbStrength, tbStrengthText);
            txtPasswordDetails.Text = BuildAnalysisReport(zxcvbnResult, false);
        }

        private void GeneratePassphrase_Click(object sender, RoutedEventArgs e)
        {
            var selectedWords = new List<string>();
            if (cbEnglish.IsChecked == true) selectedWords.AddRange(EnglishWords);
            if (cbGerman.IsChecked == true) selectedWords.AddRange(GermanWords);

            if (selectedWords.Count == 0)
            {
                MessageBox.Show("Please select at least one language");
                return;
            }

            string passphrase = GenerateSecurePassphrase(
                selectedWords.ToArray(),
                (int)sliderWords.Value,
                txtSeparator.Text
            );
            txtPassphrase.Text = passphrase;

            var zxcvbnResult = Core.EvaluatePassword(passphrase);
            txtPassphraseDetails.Text = BuildAnalysisReport(zxcvbnResult, false);
        }

        private void CheckUserPassword_Click(object sender, RoutedEventArgs e)
        {
            string pw = userCheckPasswordBox.Password;
            if (string.IsNullOrWhiteSpace(pw))
            {
                MessageBox.Show("Please enter a password.");
                return;
            }

            double entropy = pw.Length > 0 ? pw.Length * Math.Log(94, 2) : 0;
            AnimateProgressBar(pbUserCheckStrength, entropy);

            var result = Core.EvaluatePassword(pw);

            // Wenn eine Warning existiert, NUR die Bits und die Warning anzeigen
            if (!string.IsNullOrEmpty(result.Feedback.Warning))
            {
                tbUserCheckStrengthText.Text = $"{entropy:F1} bits\n⚠️ {result.Feedback.Warning}";
            }
            else
            {
                tbUserCheckStrengthText.Text = $"{entropy:F1} bits — {GetCheckTabStrengthText(entropy)}";
            }
            tbUserCheckStrengthText.Foreground = new SolidColorBrush(GetCheckTabStrengthColor(entropy, _isDarkMode));

            txtUserCheckDetails.Text = BuildAnalysisReport(result, false); // Passwort nicht anzeigen
        }

        private string BuildAnalysisReport(Result result, bool showPassword = true)
        {
            var sb = new StringBuilder();
            if (showPassword)
                sb.AppendLine($"Password: {result.Password}");
            sb.AppendLine($"Strength Score: {result.Score}/4");
            sb.AppendLine($"Guesses (log10): {result.GuessesLog10:F1}");
            sb.AppendLine("\n[ Crack Time Estimates ]");
            sb.AppendLine($"Online (100 guesses/hour): {result.CrackTimeDisplay.OnlineThrottling100PerHour}");
            sb.AppendLine($"Offline (slow hash): {result.CrackTimeDisplay.OfflineSlowHashing1e4PerSecond}");
            sb.AppendLine($"Offline (fast hash): {result.CrackTimeDisplay.OfflineFastHashing1e10PerSecond}");

            if (!string.IsNullOrEmpty(result.Feedback.Warning))
                sb.AppendLine($"\nWarning: {result.Feedback.Warning}");

            return sb.ToString();
        }


        private void SetDarkTheme_Click(object sender, RoutedEventArgs e)
        {
            _isDarkMode = true;
            Resources["WindowBackgroundBrush"] = Resources["DarkWindowBackgroundBrush"];
            Resources["CardBrush"] = Resources["DarkCardBrush"];
            Resources["TextBrush"] = Resources["DarkTextBrush"];
            Resources["AccentBrush"] = Resources["DarkAccentBrush"];
            Resources["ButtonBrush"] = Resources["DarkButtonBrush"];
            Resources["ButtonHoverBrush"] = Resources["DarkButtonHoverBrush"];
            Resources["ButtonTextBrush"] = Resources["DarkButtonTextBrush"];
            Resources["DynamicBorderColor"] = Resources["DarkBorderBrush"];
            SaveThemeSetting("Dark");
        }

        private void SetLightTheme_Click(object sender, RoutedEventArgs e)
        {
            _isDarkMode = false;
            Resources["WindowBackgroundBrush"] = Resources["LightWindowBackgroundBrush"];
            Resources["CardBrush"] = Resources["LightCardBrush"];
            Resources["TextBrush"] = Resources["LightTextBrush"];
            Resources["AccentBrush"] = Resources["LightAccentBrush"];
            Resources["ButtonBrush"] = Resources["LightButtonBrush"];
            Resources["ButtonHoverBrush"] = Resources["LightButtonHoverBrush"];
            Resources["ButtonTextBrush"] = Resources["LightButtonTextBrush"];
            Resources["DynamicBorderColor"] = Resources["LightBorderBrush"];
            SaveThemeSetting("Light");
        }

        private void CopyPassword_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPassword.Text))
            {
                Clipboard.SetText(txtPassword.Text);
                txtCopiedMessage.Visibility = Visibility.Visible;
                _copyTimer.Start();
            }
        }
        private void CopyPassphrase_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPassphrase.Text))
            {
                Clipboard.SetText(txtPassphrase.Text);
                txtCopiedMessagePassphrase.Visibility = Visibility.Visible;
                _passphraseTimer.Start();
            }
        }

        private void OpenGitHub_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/Morgoth01/MyPasswordGenerator",
                UseShellExecute = true
            });
        }

        private void OpenLatestRelease_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/Morgoth01/MyPasswordGenerator/releases/latest",
                UseShellExecute = true
            });
        }

        // Damit Hyperlinks im TextBlock funktionieren:
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }

    }
}
