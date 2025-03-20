using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;

namespace LagerClient.Blazor.Client.Services
{
    // Wrapper für den Blazored Modal Service, der Standardwerte für Parameter bereitstellt 
    // und eine zusätzliche Abstraktionsebene für einfachere Modal-Aufrufe bietet.

    public class ModalServiceWrapper
    {
        private readonly IModalService _modalService;

        public ModalServiceWrapper(IModalService modalService)
        {
            _modalService = modalService;
        }

        public IModalReference Show<T>(string title = "", ModalParameters parameters = null, ModalOptions options = null) where T : IComponent
        {
            return _modalService.Show<T>(title, parameters ?? new ModalParameters(), options ?? new ModalOptions());
        }
    }
}