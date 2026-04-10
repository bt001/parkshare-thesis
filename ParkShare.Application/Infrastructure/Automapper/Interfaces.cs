using AutoMapper;

namespace ParkShare.Application.Infrastructure.Automapper
{
    public interface IHaveCustomMapping
    {
        void CreateMappings(Profile cfg);
    }

    public interface IMapFrom<TEntity> { }

    public interface IMapTo<TEntity> { }
}

