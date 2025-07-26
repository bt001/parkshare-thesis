using AutoMapper;
using System.Reflection;

namespace ParkShare.Application.Infrastructure.Automapper
{
    public class AutoMapperProfile : Profile
    {
        private readonly Assembly[] _supportedAssemblies;

        public AutoMapperProfile()
        {
            var generalNamespace = Assembly.GetExecutingAssembly()?.FullName?.Split('.').FirstOrDefault()
                ?? throw new ArgumentNullException("Wrong Namespace");

            _supportedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(asm => asm.FullName.Contains(generalNamespace)).ToArray();

            LoadStandardMappings();
            LoadCustomMappings();
            LoadConverters();
            // Ensure automapper profile is valid
            ConfigurationValid();
        }

        private void LoadConverters()
        {
        }

        private void LoadCustomMappings()
        {
            var mapsFrom = MapperProfileHelper.LoadCustomMappings(_supportedAssemblies);

            foreach (var map in mapsFrom)
                map.CreateMappings(this);
        }

        private void LoadStandardMappings()
        {
            var mapsFrom = MapperProfileHelper.LoadSourceMappings(_supportedAssemblies);

            foreach (var map in mapsFrom)
                CreateMap(map.Source, map.Destination).ReverseMap();

            var mapsTo = MapperProfileHelper.LoadDestinationMappings(_supportedAssemblies);

            foreach (var map in mapsTo)
                CreateMap(map.Source, map.Destination).ReverseMap();
        }

        private void ConfigurationValid()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile(this));
            config.AssertConfigurationIsValid();
        }
    }
}
