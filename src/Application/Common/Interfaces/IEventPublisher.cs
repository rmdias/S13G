using System.Threading.Tasks;

namespace S13G.Application.Common.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(string routingKey, T @event);
    }
}