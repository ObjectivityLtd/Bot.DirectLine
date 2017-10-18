namespace Objectivity.Bot.DirectLine.Config.IoC.Installers
{
    using System;
    using System.Configuration;
    using Castle.MicroKernel.Registration;
    using Castle.MicroKernel.SubSystems.Configuration;
    using Castle.Windsor;
    using Microsoft.Bot.Connector.DirectLine;
    using Microsoft.Rest.TransientFaultHandling;

    public class DirectLineClassLibraryInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // TODO: Replace with Check.NotNull (after preparing nuget package)
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            container.Register(
                Classes.FromThisAssembly()
                    .Pick()
                    .WithServiceDefaultInterfaces()
                    .LifestyleScoped());

            var directLineClient = new DirectLineClient(
                new DirectLineClientCredentials(ConfigurationManager.AppSettings.Get("DirectLineSecret")));
            directLineClient.HttpClient.Timeout = TimeSpan.FromMinutes(5);
            var directLineTimeoutSetting = ConfigurationManager.AppSettings.Get("DirectLineMinutesForTimeout");
            int directLineTimeout;
            if (!int.TryParse(directLineTimeoutSetting, out directLineTimeout))
            {
                directLineTimeout = 5;
            }

            directLineClient.SetRetryPolicy(
                new RetryPolicy(
                    new TransientErrorIgnoreStrategy(),
                    0,
                    TimeSpan.FromMinutes(directLineTimeout)));

            container.Register(
                Component.For<IDirectLineClient>()
                    .Instance(directLineClient));
        }
    }
}
