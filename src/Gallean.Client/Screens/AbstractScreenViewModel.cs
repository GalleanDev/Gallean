using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal abstract partial class AbstractScreenViewModel : ObservableObject, IDisposable
{
    public ScreenStack? Host { get; private set; }

    public bool IsOpen { get; private set; }

    [ObservableProperty]
    public partial AssetAnimationKey.CursorType CursorType { get; protected set; } = AssetAnimationKey.CursorType.Default;

    public virtual ScreenMusic Music => ScreenMusic.Keep;
    public virtual AssetSoundKey? OpenSound => null;
    public virtual AssetSoundKey? CloseSound => null;
    public virtual bool PaintsOwnBackground => false;
    public virtual bool BlurBackdrop => false;
    public virtual TimeSpan RevealDelay => TimeSpan.Zero;
    public virtual TimeSpan RevealDuration => TimeSpan.Zero;

    public void AttachHost(ScreenStack host)
    {
        Host = host;
        IsOpen = true;

        OnOpened();
    }

    public void DetachHost()
    {
        IsOpen = false;
    }

    public virtual void Dispose()
    {
    }

    [RelayCommand]
    public void Close()
    {
        TryClose();
    }

    protected virtual void OnOpened()
    {
    }

    protected virtual bool TryClose()
    {
        return false;
    }

    protected void CloseSelf()
    {
        Host?.Close(this);
    }
}
