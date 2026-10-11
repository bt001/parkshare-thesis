using AutoMapper;
using ParkShare.Application.Commands;
using ParkShare.Application.Queries;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Shared.Requests.Notifications;
using ParkShare.Shared.Results.Notifications;
namespace ParkShare.Api.Mappings
{
    /// <summary>Maps notification API contracts.</summary>
    public class NotificationMappings : IHaveCustomMapping
    {
        /// <inheritdoc/>
        public void CreateMappings(Profile cfg)
        {
            cfg.CreateMap<NotificationItemResponse, NotificationItemResult>();
            cfg.CreateMap<GetNotificationsRequest, GetNotificationsQuery>();
            cfg.CreateMap<GetNotificationsResponse, GetNotificationsResult>();
            cfg.CreateMap<GetUnreadNotificationCountRequest, GetUnreadNotificationCountQuery>();
            cfg.CreateMap<UnreadNotificationCountResponse, UnreadNotificationCountResult>();
            cfg.CreateMap<MarkNotificationReadRequest, MarkNotificationReadCommand>();
            cfg.CreateMap<MarkNotificationReadResponse, MarkNotificationReadResult>();
        }
    }
}
