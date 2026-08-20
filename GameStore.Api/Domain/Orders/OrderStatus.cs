using System.Text.Json.Serialization;

namespace GameStore.Api.Domain.Orders;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Pending,
    Completed
}
