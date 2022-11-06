namespace Multi_Window.Contracts.Services;

public interface IActivationService
{
    Task ActivateAsync(object activationArgs);
}
