using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace VVooOverthrown.App.ViewModels;

public sealed class MultiplierInputViewModel : INotifyPropertyChanged
{
    public const float Maximum = 1000f;

    private string _text;
    private bool _isValid;
    private string _message = string.Empty;
    private bool _trainerEnabled;

    public MultiplierInputViewModel(string initialText)
    {
        _text = initialText;
        Validate();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            _text = value;
            OnPropertyChanged();
            Validate();
        }
    }

    public bool IsValid
    {
        get => _isValid;
        private set
        {
            if (_isValid == value)
            {
                return;
            }

            _isValid = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanApply));
        }
    }

    public string Message
    {
        get => _message;
        private set
        {
            if (_message == value)
            {
                return;
            }

            _message = value;
            OnPropertyChanged();
        }
    }

    public bool CanApply => _trainerEnabled && IsValid;

    public bool TryGetValue(out float value) => TryParse(Text, out value);

    public void SetTrainerEnabled(bool enabled)
    {
        if (_trainerEnabled == enabled)
        {
            return;
        }

        _trainerEnabled = enabled;
        OnPropertyChanged(nameof(CanApply));
    }

    private void Validate()
    {
        IsValid = TryParse(Text, out _);
        Message = IsValid
            ? "0~1,000x 숫자"
            : "입력 오류 · 0~1,000x 숫자만 사용할 수 있습니다.";
    }

    private static bool TryParse(string text, out float value)
    {
        var parsed = float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
                     float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        return parsed && float.IsFinite(value) && value is >= 0f and <= Maximum;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
