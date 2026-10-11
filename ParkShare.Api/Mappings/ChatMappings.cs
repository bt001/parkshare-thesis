using AutoMapper;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Application.Commands;
using ParkShare.Application.Queries;
using ParkShare.Application.Dto.Application;
using ParkShare.Shared.Requests.Chat;
using ParkShare.Shared.Results.Chat;
namespace ParkShare.Api.Mappings
{
    /// <summary>Maps booking chat API contracts.</summary>
    public class ChatMappings : IHaveCustomMapping
    {
        /// <inheritdoc/>
        public void CreateMappings(Profile cfg)
        {
            cfg.CreateMap<GetBookingMessagesRequest, GetBookingMessagesQuery>();
            cfg.CreateMap<SendBookingMessageRequest, SendBookingMessageCommand>();
            cfg.CreateMap<GetBookingMessagesResponse, GetBookingMessagesResult>();
            cfg.CreateMap<ChatMessageResponse, ChatMessageResult>();
        }
    }
}
