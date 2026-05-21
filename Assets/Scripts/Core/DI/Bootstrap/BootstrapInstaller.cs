using Utils.Requests;
using Zenject;

namespace Core.DI.Bootstrap
{
    public sealed class BootstrapInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<RequestQueue>().AsSingle();
            Container.BindInterfacesTo<BootstrapEntryPoint>().AsSingle();
        }
    }
}
